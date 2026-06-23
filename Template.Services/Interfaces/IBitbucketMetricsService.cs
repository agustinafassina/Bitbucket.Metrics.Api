using Template.Models.Dto.Bitbucket;

namespace Template.Services.Interfaces
{
    public interface IBitbucketMetricsService
    {
        // Workspace members (real Bitbucket users) for the user selector.
        Task<IReadOnlyList<BitbucketUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // Distinct commit authors (for user filters). When repoSlug is null, spans the whole workspace.
        Task<IReadOnlyList<ContributorDto>> GetContributorsAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // Who commits the most. When repoSlug is null, aggregates across the whole workspace.
        Task<IReadOnlyList<CommitterMetricDto>> GetTopCommittersAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default);

        // Where the activity happens: commits per repository.
        Task<IReadOnlyList<RepositoryActivityDto>> GetRepositoryActivityAsync(
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // How often commits happen, bucketed by day/week/month.
        Task<IReadOnlyList<CommitActivityPointDto>> GetCommitFrequencyAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            string interval = "day",
            CancellationToken cancellationToken = default);

        // Pull request throughput and time-to-merge.
        Task<PullRequestMetricsDto> GetPullRequestMetricsAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // Reviewer leaderboard (who reviews/approves the most).
        Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerLeaderboardAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default);

        // Code churn (lines added/removed) per author. Bounded by MaxDiffCommits.
        Task<IReadOnlyList<ChurnMetricDto>> GetChurnAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            int top = 10,
            CancellationToken cancellationToken = default);

        // Commit activity by day-of-week and hour.
        Task<IReadOnlyList<CommitHeatmapPointDto>> GetActivityHeatmapAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // Work grouped by Jira issue key found in commit messages (Jira cross-reference, Bitbucket side).
        Task<IReadOnlyList<IssueActivityDto>> GetIssueActivityAsync(
            string? repoSlug = null,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        // Aggregated overview for the front-end dashboard.
        Task<WorkspaceSummaryDto> GetWorkspaceSummaryAsync(
            DateTimeOffset? since = null,
            int top = 5,
            CancellationToken cancellationToken = default);
    }
}
