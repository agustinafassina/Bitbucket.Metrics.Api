using System.Net.Http.Headers;
using System.Text.Json;
using BitbucketApi.Models.Dto.Bitbucket;
using BitbucketApi.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BitbucketApi.Services.Implementations
{
    public class BitbucketService : IBitbucketService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BitbucketService> _logger;
        private readonly string _baseUrl;

        public BitbucketService(HttpClient httpClient, IConfiguration configuration, ILogger<BitbucketService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            
            _baseUrl = _configuration["Bitbucket:BaseUrl"] ?? "https://api.bitbucket.org/2.0";
            var username = _configuration["Bitbucket:Username"];
            var apiToken = _configuration["Bitbucket:ApiToken"];
            var appPassword = _configuration["Bitbucket:AppPassword"];

            // Priority: API Token > App Password
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(apiToken))
            {
                // Use Atlassian API Token (email as username, token as password)
                var authValue = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{username}:{apiToken}"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", authValue);
                _logger.LogInformation("Atlassian API Token authentication configured for user: {Username}", username);
            }
            else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(appPassword))
            {
                // Fallback to App Password
                var authValue = Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes($"{username}:{appPassword}"));
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", authValue);
                _logger.LogInformation("App Password authentication configured for user: {Username}", username);
            }
            else
            {
                _logger.LogWarning("No Bitbucket credentials configured. API calls may fail.");
            }

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<CommitDto>> GetCommitsAsync(string workspace, string repository, string? branch = null, int? limit = null)
        {
            try
            {
                var commits = new List<CommitDto>();
                var url = $"{_baseUrl}/repositories/{workspace}/{repository}/commits";
                
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(branch))
                {
                    queryParams.Add($"include={branch}");
                }
                if (limit.HasValue)
                {
                    queryParams.Add($"pagelen={limit.Value}");
                }
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                _logger.LogInformation("Requesting commits from: {Url}", url);

                var allCommits = new List<CommitDto>();
                string? nextUrl = url;

                while (!string.IsNullOrEmpty(nextUrl))
                {
                    var response = await _httpClient.GetAsync(nextUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("Bitbucket API error. Status: {StatusCode}, Response: {ErrorContent}", 
                            response.StatusCode, errorContent);
                        throw new HttpRequestException(
                            $"Bitbucket API returned {response.StatusCode}: {errorContent}");
                    }

                    var jsonString = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<BitbucketApiResponse<BitbucketCommitResponse>>(
                        jsonString, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (apiResponse?.Values != null)
                    {
                        foreach (var commit in apiResponse.Values)
                        {
                            allCommits.Add(MapToCommitDto(commit, workspace, repository, branch ?? "main"));
                        }
                    }

                    nextUrl = apiResponse?.Next;
                    
                    // If a limit was specified, stop paginating
                    if (limit.HasValue && allCommits.Count >= limit.Value)
                    {
                        break;
                    }
                }

                return limit.HasValue ? allCommits.Take(limit.Value).ToList() : allCommits;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commits for workspace: {Workspace}, repository: {Repository}", 
                    workspace, repository);
                throw;
            }
        }

        public async Task<List<RepositoryDto>> GetRepositoriesAsync(string workspace)
        {
            try
            {
                var url = $"{_baseUrl}/repositories/{workspace}";
                _logger.LogInformation("Requesting repositories from: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Bitbucket API error. Status: {StatusCode}, Response: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException(
                        $"Bitbucket API returned {response.StatusCode}: {errorContent}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<BitbucketApiResponse<BitbucketRepositoryResponse>>(
                    jsonString, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var repositories = new List<RepositoryDto>();
                if (apiResponse?.Values != null)
                {
                    foreach (var repo in apiResponse.Values)
                    {
                        repositories.Add(new RepositoryDto
                        {
                            Name = repo.Name ?? string.Empty,
                            Slug = repo.Slug ?? string.Empty,
                            Workspace = workspace,
                            FullName = repo.FullName ?? string.Empty
                        });
                    }
                }

                return repositories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repositories for workspace: {Workspace}", workspace);
                throw;
            }
        }

        public async Task<CommitDto?> GetCommitByIdAsync(string workspace, string repository, string commitHash)
        {
            try
            {
                var url = $"{_baseUrl}/repositories/{workspace}/{repository}/commit/{commitHash}";
                _logger.LogInformation("Requesting commit from: {Url}", url);
                
                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Bitbucket API error. Status: {StatusCode}, Response: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException(
                        $"Bitbucket API returned {response.StatusCode}: {errorContent}");
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var commit = JsonSerializer.Deserialize<BitbucketCommitResponse>(
                    jsonString, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (commit == null)
                {
                    return null;
                }

                return MapToCommitDto(commit, workspace, repository, "main");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commit {CommitHash} for workspace: {Workspace}, repository: {Repository}", 
                    commitHash, workspace, repository);
                throw;
            }
        }

        private CommitDto MapToCommitDto(BitbucketCommitResponse commit, string workspace, string repository, string branch)
        {
            var author = ParseAuthor(commit.Author);
            
            return new CommitDto
            {
                Hash = commit.Hash,
                Message = commit.Message ?? string.Empty,
                Author = author,
                Date = commit.Date,
                Repository = repository,
                Branch = branch
            };
        }

        private AuthorDto ParseAuthor(BitbucketAuthorResponse authorResponse)
        {
            var author = new AuthorDto();
            
            // Parse the Raw field which has format "Name <email>"
            if (!string.IsNullOrEmpty(authorResponse.Raw))
            {
                var parts = authorResponse.Raw.Split('<');
                if (parts.Length == 2)
                {
                    author.Name = parts[0].Trim();
                    author.Email = parts[1].TrimEnd('>').Trim();
                }
                else
                {
                    author.Name = authorResponse.Raw.Trim();
                }
            }

            // If there is Bitbucket user information, use it
            if (authorResponse.User != null)
            {
                author.DisplayName = authorResponse.User.DisplayName ?? author.Name;
                author.UserId = authorResponse.User.Uuid ?? string.Empty;
            }
            else
            {
                author.DisplayName = author.Name;
            }

            return author;
        }
    }

    // Helper class to deserialize repository response
    internal class BitbucketRepositoryResponse
    {
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? FullName { get; set; }
    }
}
