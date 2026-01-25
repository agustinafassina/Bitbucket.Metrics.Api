using BitbucketApi.Models.Dto.Metrics;

namespace BitbucketApi.Services.Interfaces
{
    public interface IMetricsService
    {
        Task<List<CommitsByPersonDto>> GetCommitsByPersonAsync(string workspace, string repository, string? branch = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<CommitsByPersonDto?> GetCommitsByPersonEmailAsync(string workspace, string repository, string email, string? branch = null, DateTime? startDate = null, DateTime? endDate = null);
    }
}
