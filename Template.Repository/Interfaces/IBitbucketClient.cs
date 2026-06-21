using Template.Models.Dto.Bitbucket;

namespace Template.Repository.Interfaces
{
    public interface IBitbucketClient
    {
        Task<IReadOnlyList<BitbucketRepositoryDto>> GetRepositoriesAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BitbucketCommitDto>> GetCommitsAsync(
            string repoSlug,
            DateTimeOffset? since = null,
            CancellationToken cancellationToken = default);
    }
}
