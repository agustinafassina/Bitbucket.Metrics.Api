using Template.Models.Dto.Bitbucket;

namespace Template.Repository.Interfaces
{
    public interface IBitbucketClient
    {
        Task<IReadOnlyList<BitbucketUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketPullRequestDto>> GetPullRequestsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ReviewerMetricDto>> GetReviewerStatsAsync(
            string repoSlug,
            IEnumerable<string> states,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);

        Task<(int LinesAdded, int LinesRemoved)> GetCommitDiffStatAsync(
            string repoSlug,
            string commitHash,
            CancellationToken cancellationToken = default);
    }
}
