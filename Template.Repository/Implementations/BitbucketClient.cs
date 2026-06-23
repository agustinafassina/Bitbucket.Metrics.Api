using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
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

            if (string.IsNullOrWhiteSpace(_options.AccessToken))
                return;

            if (!string.IsNullOrWhiteSpace(_options.Email))
            {
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.Email}:{_options.AccessToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
            }
            else
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

        public async Task<IReadOnlyList<BitbucketPullRequestDto>> GetPullRequestsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<PullRequestApi> prs = await GetPullRequestApisAsync(repoSlug, states, since, cancellationToken);

            return prs
                .Where(p => p is not null && (!since.HasValue || p.CreatedOn >= since.Value))
                .Select(p => new BitbucketPullRequestDto
                {
                    Id = p.Id,
                    Title = p.Title ?? string.Empty,
                    State = p.State ?? string.Empty,
                    Author = ResolveActor(p.Author),
                    CreatedOn = p.CreatedOn,
                    UpdatedOn = p.UpdatedOn,
                    CommentCount = p.CommentCount,
                    HoursToMerge = string.Equals(p.State, "MERGED", StringComparison.OrdinalIgnoreCase)
                        ? Math.Round((p.UpdatedOn - p.CreatedOn).TotalHours, 2)
                        : null,
                    RepositorySlug = repoSlug
                })
                .ToList();
        }

        public async Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerStatsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<PullRequestApi> prs = await GetPullRequestApisAsync(repoSlug, states, since, cancellationToken);

            Dictionary<string, ReviewerMetricDto> reviewers = new(StringComparer.OrdinalIgnoreCase);

            foreach (PullRequestApi pr in prs)
            {
                if (since.HasValue && pr.CreatedOn < since.Value)
                    continue;
                if (pr.Participants is null)
                    continue;

                foreach (ParticipantApi participant in pr.Participants)
                {
                    bool isReviewer = string.Equals(participant.Role, "REVIEWER", StringComparison.OrdinalIgnoreCase);
                    if (!isReviewer && !participant.Approved)
                        continue;

                    string name = ResolveActor(participant.User);
                    if (!reviewers.TryGetValue(name, out ReviewerMetricDto? metric))
                    {
                        metric = new ReviewerMetricDto { Reviewer = name };
                        reviewers[name] = metric;
                    }

                    if (isReviewer)
                        metric.PullRequestsReviewed++;
                    if (participant.Approved)
                        metric.Approvals++;
                }
            }

            return reviewers.Values
                .OrderByDescending(r => r.Approvals)
                .ThenByDescending(r => r.PullRequestsReviewed)
                .ToList();
        }

        public async Task<(int LinesAdded, int LinesRemoved)> GetCommitDiffStatAsync(
            string repoSlug,
            string commitHash,
            CancellationToken cancellationToken = default)
        {
            EnsureWorkspaceConfigured();
            if (string.IsNullOrWhiteSpace(repoSlug))
                throw new ArgumentException("Repository slug is required.", nameof(repoSlug));
            if (string.IsNullOrWhiteSpace(commitHash))
                throw new ArgumentException("Commit hash is required.", nameof(commitHash));

            string url = $"repositories/{_options.Workspace}/{repoSlug}/diffstat/{commitHash}?pagelen={_options.PageLength}";
            List<DiffStatApi> files = await GetAllPagesAsync<DiffStatApi>(url, cancellationToken: cancellationToken);

            return (files.Sum(f => f.LinesAdded), files.Sum(f => f.LinesRemoved));
        }

        private async Task<List<PullRequestApi>> GetPullRequestApisAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since,
            CancellationToken cancellationToken)
        {
            EnsureWorkspaceConfigured();
            if (string.IsNullOrWhiteSpace(repoSlug))
                throw new ArgumentException("Repository slug is required.", nameof(repoSlug));

            string url = $"repositories/{_options.Workspace}/{repoSlug}/pullrequests?pagelen={_options.PageLength}&sort=-created_on";

            foreach (string state in states ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(state))
                    url += $"&state={Uri.EscapeDataString(state)}";
            }

            if (since.HasValue)
            {
                string iso = since.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
                url += $"&q={Uri.EscapeDataString($"created_on>={iso}")}";
            }

            return await GetAllPagesAsync<PullRequestApi>(
                url,
                stopWhen: since.HasValue ? p => p.CreatedOn < since.Value : null,
                cancellationToken: cancellationToken);
        }

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value[..max] + "...";

        private static string ResolveActor(ActorApi? actor)
        {
            if (actor is null)
                return "Unknown";
            string? name = actor.DisplayName ?? actor.Nickname;
            return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
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

                using HttpResponseMessage? response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    string? body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Bitbucket API call to {Url} failed with {StatusCode}: {Body}", url, (int)response.StatusCode, body);
                    string detail = string.IsNullOrWhiteSpace(body) ? string.Empty : $" Response: {Truncate(body, 500)}";
                    throw new HttpRequestException($"Bitbucket API returned {(int)response.StatusCode} for '{url}'.{detail}");
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
