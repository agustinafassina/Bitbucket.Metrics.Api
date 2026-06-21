using Template.Models.Dto.Bitbucket;

namespace Template.Services.Interfaces
{
    public interface IBitbucketMetricsService
    {
        Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
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
    }
}
