using System.Globalization;
using Microsoft.Extensions.Logging;
using Template.Models.Dto.Bitbucket;
using Template.Repository.Interfaces;
using Template.Services.Interfaces;

namespace Template.Services.Implementations
{
    public class BitbucketMetricsService : IBitbucketMetricsService
    {
        private readonly IBitbucketClient _client;
        private readonly ILogger<BitbucketMetricsService> _logger;

        public BitbucketMetricsService(IBitbucketClient client, ILogger<BitbucketMetricsService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
            => _client.GetRepositoriesAsync(cancellationToken);

        public Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => _client.GetCommitsAsync(repoSlug, since, cancellationToken);

        public async Task<IReadOnlyList<CommitterMetricDto>> GetTopCommittersAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            var commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            var metrics = commits
                .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var ordered = g.OrderBy(c => c.Date).ToList();
                    var first = ordered.First().Date;
                    var last = ordered.Last().Date;
                    var avg = ordered.Count > 1
                        ? (last - first).TotalDays / (ordered.Count - 1)
                        : 0d;

                    return new CommitterMetricDto
                    {
                        Author = g.Key,
                        Email = ordered.Select(c => c.AuthorEmail).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                        CommitCount = ordered.Count,
                        FirstCommit = first,
                        LastCommit = last,
                        AverageDaysBetweenCommits = Math.Round(avg, 2),
                        Repositories = ordered.Select(c => c.RepositorySlug).Distinct().OrderBy(s => s).ToList()
                    };
                })
                .OrderByDescending(m => m.CommitCount)
                .ThenBy(m => m.Author)
                .Take(top <= 0 ? 10 : top)
                .ToList();

            _logger.LogInformation("Computed top committers: {Count} authors over {Commits} commits", metrics.Count, commits.Count);
            return metrics;
        }

        public async Task<IReadOnlyList<RepositoryActivityDto>> GetRepositoryActivityAsync(
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            var repos = await _client.GetRepositoriesAsync(cancellationToken);
            var activity = new List<RepositoryActivityDto>(repos.Count);

            foreach (var repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var commits = await _client.GetCommitsAsync(repo.Slug, since, cancellationToken);

                var topContributor = commits
                    .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                activity.Add(new RepositoryActivityDto
                {
                    RepositorySlug = repo.Slug,
                    CommitCount = commits.Count,
                    ContributorCount = commits.Select(c => c.AuthorName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    FirstCommit = commits.Count > 0 ? commits.Min(c => c.Date) : null,
                    LastCommit = commits.Count > 0 ? commits.Max(c => c.Date) : null,
                    TopContributor = topContributor
                });
            }

            return activity
                .OrderByDescending(a => a.CommitCount)
                .ThenBy(a => a.RepositorySlug)
                .ToList();
        }

        public async Task<IReadOnlyList<CommitActivityPointDto>> GetCommitFrequencyAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            string interval = "day",
            CancellationToken cancellationToken = default)
        {
            var commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);
            var bucket = NormalizeInterval(interval);

            return commits
                .GroupBy(c => BuildPeriodKey(c.Date, bucket))
                .Select(g => new CommitActivityPointDto
                {
                    Period = g.Key,
                    CommitCount = g.Count(),
                    ContributorCount = g.Select(c => c.AuthorName).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                })
                .OrderBy(p => p.Period, StringComparer.Ordinal)
                .ToList();
        }

        private async Task<List<BitbucketCommitDto>> GetCommitsForScopeAsync(
            string? repoSlug,
            DateTimeOffset? since,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(repoSlug))
            {
                var single = await _client.GetCommitsAsync(repoSlug, since, cancellationToken);
                return single.ToList();
            }

            var repos = await _client.GetRepositoriesAsync(cancellationToken);
            var all = new List<BitbucketCommitDto>();
            foreach (var repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var commits = await _client.GetCommitsAsync(repo.Slug, since, cancellationToken);
                all.AddRange(commits);
            }

            return all;
        }

        private static string NormalizeInterval(string interval)
        {
            return interval?.Trim().ToLowerInvariant() switch
            {
                "week" => "week",
                "month" => "month",
                _ => "day"
            };
        }

        private static string BuildPeriodKey(DateTimeOffset date, string interval)
        {
            var utc = date.UtcDateTime;
            return interval switch
            {
                "month" => utc.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                "week" => $"{ISOWeek.GetYear(utc)}-W{ISOWeek.GetWeekOfYear(utc):D2}",
                _ => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }
    }
}
