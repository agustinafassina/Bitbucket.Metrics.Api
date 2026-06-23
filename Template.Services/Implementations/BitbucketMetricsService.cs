using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Template.Models.Configuration;
using Template.Models.Dto.Bitbucket;
using Template.Repository.Interfaces;
using Template.Services.Interfaces;

namespace Template.Services.Implementations
{
    public class BitbucketMetricsService : IBitbucketMetricsService
    {
        private static readonly string[] AllPullRequestStates = { "OPEN", "MERGED", "DECLINED" };
        private static readonly Regex IssueKeyRegex = new(@"\b[A-Z][A-Z0-9]+-\d+\b", RegexOptions.Compiled);
        private readonly IBitbucketClient _client;
        private readonly ILogger<BitbucketMetricsService> _logger;
        private readonly int _maxDiffCommits;

        public BitbucketMetricsService(
            IBitbucketClient client,
            IOptions<BitbucketOptions> options,
            ILogger<BitbucketMetricsService> logger)
        {
            _client = client;
            _logger = logger;
            _maxDiffCommits = options.Value.MaxDiffCommits > 0 ? options.Value.MaxDiffCommits : 300;
        }

        public Task<IReadOnlyList<BitbucketUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
            => _client.GetUsersAsync(cancellationToken);

        public Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default)
            => _client.GetRepositoriesAsync(cancellationToken);

        public Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
            => _client.GetCommitsAsync(repoSlug, since, cancellationToken);

        public async Task<IReadOnlyList<ContributorDto>> GetContributorsAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketCommitDto> commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            return commits
                .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ContributorDto
                {
                    Name = g.Key,
                    Email = g.Select(c => c.AuthorEmail).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                    CommitCount = g.Count(),
                    Repositories = g.Select(c => c.RepositorySlug).Distinct().OrderBy(s => s).ToList()
                })
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<IReadOnlyList<CommitterMetricDto>> GetTopCommittersAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketCommitDto>? commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            List<CommitterMetricDto>? metrics = commits
                .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    List<BitbucketCommitDto>? ordered = g.OrderBy(c => c.Date).ToList();
                    DateTimeOffset first = ordered.First().Date;
                    DateTimeOffset last = ordered.Last().Date;
                    double avg = ordered.Count > 1
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
            IReadOnlyList<BitbucketRepositoryDto> repos = await _client.GetRepositoriesAsync(cancellationToken);
            List<RepositoryActivityDto>? activity = new(repos.Count);

            foreach (BitbucketRepositoryDto? repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<BitbucketCommitDto> commits = await _client.GetCommitsAsync(repo.Slug, since, cancellationToken);

                string? topContributor = commits
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
            List<BitbucketCommitDto>? commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);
            string? bucket = NormalizeInterval(interval);

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

        public async Task<PullRequestMetricsDto> GetPullRequestMetricsAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketPullRequestDto>? prs = await GetPullRequestsForScopeAsync(repoSlug, since, cancellationToken);

            bool IsState(BitbucketPullRequestDto p, string state) => string.Equals(p.State, state, StringComparison.OrdinalIgnoreCase);

            List<double>? mergeHours = prs
                .Where(p => p.HoursToMerge.HasValue)
                .Select(p => p.HoursToMerge!.Value)
                .ToList();

            List<AuthorPullRequestStatDto>? byAuthor = prs
                .GroupBy(p => p.Author, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    List<double>? merged = g.Where(p => p.HoursToMerge.HasValue).Select(p => p.HoursToMerge!.Value).ToList();
                    return new AuthorPullRequestStatDto
                    {
                        Author = g.Key,
                        Opened = g.Count(),
                        Merged = g.Count(p => IsState(p, "MERGED")),
                        Declined = g.Count(p => IsState(p, "DECLINED")),
                        AverageHoursToMerge = merged.Count > 0 ? Math.Round(merged.Average(), 2) : null
                    };
                })
                .OrderByDescending(a => a.Opened)
                .ThenBy(a => a.Author)
                .ToList();

            return new PullRequestMetricsDto
            {
                TotalOpen = prs.Count(p => IsState(p, "OPEN")),
                TotalMerged = prs.Count(p => IsState(p, "MERGED")),
                TotalDeclined = prs.Count(p => IsState(p, "DECLINED")),
                AverageHoursToMerge = mergeHours.Count > 0 ? Math.Round(mergeHours.Average(), 2) : null,
                MedianHoursToMerge = mergeHours.Count > 0 ? Math.Round(Median(mergeHours), 2) : null,
                ByAuthor = byAuthor
            };
        }

        public async Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerLeaderboardAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            List<ReviewerMetricDto>? perRepo = new();

            if (!string.IsNullOrWhiteSpace(repoSlug))
            {
                perRepo.AddRange(await _client.GetReviewerStatsAsync(repoSlug, AllPullRequestStates, since, cancellationToken));
            }
            else
            {
                IReadOnlyList<BitbucketRepositoryDto>? repos = await _client.GetRepositoriesAsync(cancellationToken);
                foreach (BitbucketRepositoryDto? repo in repos)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    perRepo.AddRange(await _client.GetReviewerStatsAsync(repo.Slug, AllPullRequestStates, since, cancellationToken));
                }
            }

            return perRepo
                .GroupBy(r => r.Reviewer, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ReviewerMetricDto
                {
                    Reviewer = g.Key,
                    PullRequestsReviewed = g.Sum(r => r.PullRequestsReviewed),
                    Approvals = g.Sum(r => r.Approvals)
                })
                .OrderByDescending(r => r.Approvals)
                .ThenByDescending(r => r.PullRequestsReviewed)
                .Take(top <= 0 ? 10 : top)
                .ToList();
        }

        public async Task<IReadOnlyList<ChurnMetricDto>> GetChurnAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketCommitDto>? commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            List<BitbucketCommitDto>? scoped = commits.OrderByDescending(c => c.Date).Take(_maxDiffCommits).ToList();
            if (scoped.Count < commits.Count)
                _logger.LogWarning("Churn limited to {Limit} of {Total} commits (MaxDiffCommits).", scoped.Count, commits.Count);

            var byAuthor = new Dictionary<string, ChurnMetricDto>(StringComparer.OrdinalIgnoreCase);

            foreach (BitbucketCommitDto? commit in scoped)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var (added, removed) = await _client.GetCommitDiffStatAsync(commit.RepositorySlug, commit.Hash, cancellationToken);

                if (!byAuthor.TryGetValue(commit.AuthorName, out var metric))
                {
                    metric = new ChurnMetricDto { Author = commit.AuthorName };
                    byAuthor[commit.AuthorName] = metric;
                }

                metric.Commits++;
                metric.LinesAdded += added;
                metric.LinesRemoved += removed;
            }

            return byAuthor.Values
                .OrderByDescending(c => c.TotalChanges)
                .ThenBy(c => c.Author)
                .Take(top <= 0 ? 10 : top)
                .ToList();
        }

        public async Task<IReadOnlyList<CommitHeatmapPointDto>> GetActivityHeatmapAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketCommitDto>? commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            return commits
                .GroupBy(c => new { Day = (int)c.Date.UtcDateTime.DayOfWeek, c.Date.UtcDateTime.Hour })
                .Select(g => new CommitHeatmapPointDto
                {
                    DayOfWeek = g.Key.Day,
                    DayName = ((DayOfWeek)g.Key.Day).ToString(),
                    Hour = g.Key.Hour,
                    CommitCount = g.Count()
                })
                .OrderBy(p => p.DayOfWeek)
                .ThenBy(p => p.Hour)
                .ToList();
        }

        public async Task<IReadOnlyList<IssueActivityDto>> GetIssueActivityAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default)
        {
            List<BitbucketCommitDto>? commits = await GetCommitsForScopeAsync(repoSlug, since, cancellationToken);

            Dictionary<string, List<BitbucketCommitDto>>? byIssue = new(StringComparer.OrdinalIgnoreCase);

            foreach (BitbucketCommitDto? commit in commits)
            {
                foreach (Match match in IssueKeyRegex.Matches(commit.Message))
                {
                    string key = match.Value.ToUpperInvariant();
                    if (!byIssue.TryGetValue(key, out List<BitbucketCommitDto>? list))
                    {
                        list = new();
                        byIssue[key] = list;
                    }
                    list.Add(commit);
                }
            }

            return byIssue
                .Select(kvp => new IssueActivityDto
                {
                    IssueKey = kvp.Key,
                    CommitCount = kvp.Value.Count,
                    FirstCommit = kvp.Value.Min(c => c.Date),
                    LastCommit = kvp.Value.Max(c => c.Date),
                    Authors = kvp.Value.Select(c => c.AuthorName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).ToList(),
                    Repositories = kvp.Value.Select(c => c.RepositorySlug).Distinct().OrderBy(r => r).ToList()
                })
                .OrderByDescending(i => i.CommitCount)
                .ThenBy(i => i.IssueKey)
                .ToList();
        }

        public async Task<WorkspaceSummaryDto> GetWorkspaceSummaryAsync(
            DateTimeOffset? since = null,
            int top = 5,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<BitbucketRepositoryDto>? repos = await _client.GetRepositoriesAsync(cancellationToken);
            List<BitbucketCommitDto>? allCommits = new();
            List<RepositoryActivityDto>? repoActivity = new(repos.Count);

            foreach (BitbucketRepositoryDto? repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<BitbucketCommitDto> commits = await _client.GetCommitsAsync(repo.Slug, since, cancellationToken);
                allCommits.AddRange(commits);

                repoActivity.Add(new RepositoryActivityDto
                {
                    RepositorySlug = repo.Slug,
                    CommitCount = commits.Count,
                    ContributorCount = commits.Select(c => c.AuthorName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    FirstCommit = commits.Count > 0 ? commits.Min(c => c.Date) : null,
                    LastCommit = commits.Count > 0 ? commits.Max(c => c.Date) : null,
                    TopContributor = commits
                        .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .FirstOrDefault()
                });
            }

            List<CommitterMetricDto>? topCommitters = allCommits
                .GroupBy(c => c.AuthorName, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    List<BitbucketCommitDto>? ordered = g.OrderBy(c => c.Date).ToList();
                    double avg = ordered.Count > 1 ? (ordered[^1].Date - ordered[0].Date).TotalDays / (ordered.Count - 1) : 0d;
                    return new CommitterMetricDto
                    {
                        Author = g.Key,
                        Email = ordered.Select(c => c.AuthorEmail).FirstOrDefault(e => !string.IsNullOrWhiteSpace(e)),
                        CommitCount = ordered.Count,
                        FirstCommit = ordered[0].Date,
                        LastCommit = ordered[^1].Date,
                        AverageDaysBetweenCommits = Math.Round(avg, 2),
                        Repositories = ordered.Select(c => c.RepositorySlug).Distinct().OrderBy(s => s).ToList()
                    };
                })
                .OrderByDescending(m => m.CommitCount)
                .ThenBy(m => m.Author)
                .Take(top <= 0 ? 5 : top)
                .ToList();

            List<(int Day, int Hour, int Count)> busiestRanked = allCommits
                .GroupBy(c => (Day: (int)c.Date.UtcDateTime.DayOfWeek, Hour: c.Date.UtcDateTime.Hour))
                .Select(g => (g.Key.Day, g.Key.Hour, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();
            (int Day, int Hour, int Count)? busiest = busiestRanked.Count > 0 ? busiestRanked[0] : null;

            int linkedIssues = allCommits
                .SelectMany(c => IssueKeyRegex.Matches(c.Message).Select(m => m.Value.ToUpperInvariant()))
                .Distinct()
                .Count();

            return new WorkspaceSummaryDto
            {
                SinceDays = since.HasValue ? (int)Math.Round((DateTimeOffset.UtcNow - since.Value).TotalDays) : null,
                RepositoryCount = repos.Count,
                CommitCount = allCommits.Count,
                ContributorCount = allCommits.Select(c => c.AuthorName).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                LinkedIssueCount = linkedIssues,
                BusiestDay = busiest.HasValue ? ((DayOfWeek)busiest.Value.Day).ToString() : null,
                BusiestHour = busiest?.Hour,
                TopCommitters = topCommitters,
                RepositoryActivity = repoActivity.OrderByDescending(a => a.CommitCount).ThenBy(a => a.RepositorySlug).ToList()
            };
        }

        private async Task<List<BitbucketCommitDto>> GetCommitsForScopeAsync(
            string? repoSlug,
            DateTimeOffset? since,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(repoSlug))
            {
                IReadOnlyList<BitbucketCommitDto>? single = await _client.GetCommitsAsync(repoSlug, since, cancellationToken);
                return single.ToList();
            }

            IReadOnlyList<BitbucketRepositoryDto>? repos = await _client.GetRepositoriesAsync(cancellationToken);
            List<BitbucketCommitDto>? all = new();
            foreach (BitbucketRepositoryDto? repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<BitbucketCommitDto> commits = await _client.GetCommitsAsync(repo.Slug, since, cancellationToken);
                all.AddRange(commits);
            }

            return all;
        }

        private async Task<List<BitbucketPullRequestDto>> GetPullRequestsForScopeAsync(
            string? repoSlug,
            DateTimeOffset? since,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(repoSlug))
            {
                IReadOnlyList<BitbucketPullRequestDto> single = await _client.GetPullRequestsAsync(repoSlug, AllPullRequestStates, since, cancellationToken);
                return single.ToList();
            }

            IReadOnlyList<BitbucketRepositoryDto>? repos = await _client.GetRepositoriesAsync(cancellationToken);
            List<BitbucketPullRequestDto>? all = new();
            foreach (BitbucketRepositoryDto? repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<BitbucketPullRequestDto> prs = await _client.GetPullRequestsAsync(repo.Slug, AllPullRequestStates, since, cancellationToken);
                all.AddRange(prs);
            }

            return all;
        }

        private static double Median(List<double> values)
        {
            List<double>? sorted = values.OrderBy(v => v).ToList();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 0
                ? (sorted[mid - 1] + sorted[mid]) / 2d
                : sorted[mid];
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
            DateTime utc = date.UtcDateTime;
            return interval switch
            {
                "month" => utc.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                "week" => $"{ISOWeek.GetYear(utc)}-W{ISOWeek.GetWeekOfYear(utc):D2}",
                _ => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
        }
    }
}
