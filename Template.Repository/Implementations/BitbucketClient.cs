using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.Models.Configuration;
using Template.Models.Dto.Bitbucket;
using Template.Repository.Implementations.ApiModels;
using Template.Repository.Interfaces;

namespace Template.Repository.Implementations
{
    public class BitbucketClient : IBitbucketClient
    {
        private readonly HttpClient _httpClient;
        private readonly BitbucketOptions _options;
        private readonly ILogger<BitbucketClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BitbucketClient(HttpClient httpClient, IOptions<BitbucketOptions> options, ILogger<BitbucketClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
            ConfigureClient();
        }

        private void ConfigureClient()
        {
            if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                string? baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
                _httpClient.BaseAddress = new Uri(baseUrl);
            }

            if (_httpClient.DefaultRequestHeaders.Authorization is not null)
                return;

            if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _options.AccessToken);
            }
        }

        public async Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
        {
            EnsureWorkspaceConfigured();

            string? url = $"repositories/{_options.Workspace}?pagelen={_options.PageLength}&sort=-updated_on";
            List<RepoApi>? repos = await GetAllPagesAsync<RepoApi>(url, cancellationToken: cancellationToken);

            return repos
                .Where(r => r is not null)
                .Select(r => new BitbucketRepositoryDto
                {
                    Slug = r.Slug ?? string.Empty,
                    Name = r.Name ?? string.Empty,
                    FullName = r.FullName ?? string.Empty,
                    Language = string.IsNullOrWhiteSpace(r.Language) ? null : r.Language,
                    UpdatedOn = r.UpdatedOn
                })
                .ToList();
        }

        public async Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            EnsureWorkspaceConfigured();
            if (string.IsNullOrWhiteSpace(repoSlug))
                throw new ArgumentException("Repository slug is required.", nameof(repoSlug));

            string? url = $"repositories/{_options.Workspace}/{repoSlug}/commits?pagelen={_options.PageLength}";

            List<CommitApi>? commits = await GetAllPagesAsync<CommitApi>(
                url,
                stopWhen: since.HasValue ? c => c.Date < since.Value : null,
                cancellationToken: cancellationToken);

            return commits
                .Where(c => c is not null && (!since.HasValue || c.Date >= since.Value))
                .Select(c =>
                {
                    (string name, string? email) = ResolveAuthor(c.Author);
                    return new BitbucketCommitDto
                    {
                        Hash = c.Hash ?? string.Empty,
                        Message = (c.Message ?? string.Empty).Trim(),
                        Date = c.Date,
                        AuthorName = name,
                        AuthorEmail = email,
                        RepositorySlug = repoSlug
                    };
                })
                .ToList();
        }

        private static (string Name, string? Email) ResolveAuthor(CommitAuthorApi? author)
        {
            if (author is null)
                return ("Unknown", null);

            string? email = null;
            string? raw = author.Raw;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                int start = raw.IndexOf('<');
                int end = raw.IndexOf('>');
                if (start >= 0 && end > start)
                    email = raw.Substring(start + 1, end - start - 1).Trim();
            }

            string? name = author.User?.DisplayName
                       ?? author.User?.Nickname;

            if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(raw))
            {
                int idx = raw.IndexOf('<');
                name = (idx > 0 ? raw[..idx] : raw).Trim();
            }

            return (string.IsNullOrWhiteSpace(name) ? (email ?? "Unknown") : name, email);
        }

        private async Task<List<T>> GetAllPagesAsync<T>(
            string startUrl,
            Func<T, bool>? stopWhen = null,
            CancellationToken cancellationToken = default)
        {
            List<T>? results = new List<T>();
            string? url = startUrl;
            int page = 0;

            while (!string.IsNullOrEmpty(url) && page < _options.MaxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string? body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Bitbucket API call to {Url} failed with {StatusCode}: {Body}", url, (int)response.StatusCode, body);
                    throw new HttpRequestException($"Bitbucket API returned {(int)response.StatusCode} for '{url}'.");
                }

                PagedResponse<T>? paged = await response.Content.ReadFromJsonAsync<PagedResponse<T>>(JsonOptions, cancellationToken) ?? new PagedResponse<T>();
                if (paged.Values is { Count: > 0 })
                {
                    foreach (T? value in paged.Values)
                    {
                        if (stopWhen is not null && stopWhen(value))
                            return results;
                        results.Add(value);
                    }
                }

                url = paged.Next;
                page++;
            }

            if (page >= _options.MaxPages && !string.IsNullOrEmpty(url))
                _logger.LogWarning("Reached MaxPages ({MaxPages}); results may be truncated.", _options.MaxPages);

            return results;
        }

        private void EnsureWorkspaceConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.Workspace))
                throw new InvalidOperationException("Bitbucket workspace is not configured. Set 'Bitbucket:Workspace'.");
            if (string.IsNullOrWhiteSpace(_options.AccessToken))
                throw new InvalidOperationException("Bitbucket access token is not configured. Set 'Bitbucket:AccessToken'.");
        }

    }
}
