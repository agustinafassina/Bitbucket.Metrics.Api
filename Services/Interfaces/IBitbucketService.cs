using BitbucketApi.Models.Dto.Bitbucket;

namespace BitbucketApi.Services.Interfaces
{
    public interface IBitbucketService
    {
        Task<List<CommitDto>> GetCommitsAsync(string workspace, string repository, string? branch = null, int? limit = null);
        Task<List<RepositoryDto>> GetRepositoriesAsync(string workspace);
        Task<CommitDto?> GetCommitByIdAsync(string workspace, string repository, string commitHash);
    }
}
