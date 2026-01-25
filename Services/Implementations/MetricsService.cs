using BitbucketApi.Models.Dto.Bitbucket;
using BitbucketApi.Models.Dto.Metrics;
using BitbucketApi.Services.Interfaces;

namespace BitbucketApi.Services.Implementations
{
    public class MetricsService : IMetricsService
    {
        private readonly IBitbucketService _bitbucketService;
        private readonly ILogger<MetricsService> _logger;

        public MetricsService(IBitbucketService bitbucketService, ILogger<MetricsService> logger)
        {
            _bitbucketService = bitbucketService;
            _logger = logger;
        }

        public async Task<List<CommitsByPersonDto>> GetCommitsByPersonAsync(
            string workspace, 
            string repository, 
            string? branch = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Getting commits by person for workspace: {Workspace}, repository: {Repository}", 
                    workspace, repository);

                var commits = await _bitbucketService.GetCommitsAsync(workspace, repository, branch);

                // Filter by dates if provided
                if (startDate.HasValue || endDate.HasValue)
                {
                    commits = commits.Where(c =>
                    {
                        if (startDate.HasValue && c.Date < startDate.Value)
                            return false;
                        if (endDate.HasValue && c.Date > endDate.Value)
                            return false;
                        return true;
                    }).ToList();
                }

                // Group commits by person (using email as unique identifier)
                var commitsByPerson = commits
                    .GroupBy(c => c.Author.Email, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new CommitsByPersonDto
                    {
                        PersonName = g.First().Author.DisplayName ?? g.First().Author.Name,
                        PersonEmail = g.Key,
                        TotalCommits = g.Count(),
                        Commits = g.Select(c => new CommitSummaryDto
                        {
                            Hash = c.Hash,
                            Message = c.Message,
                            Date = c.Date,
                            Repository = c.Repository,
                            Branch = c.Branch
                        }).OrderByDescending(c => c.Date).ToList()
                    })
                    .OrderByDescending(x => x.TotalCommits)
                    .ToList();

                _logger.LogInformation("Found {Count} unique persons with commits", commitsByPerson.Count);

                return commitsByPerson;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commits by person for workspace: {Workspace}, repository: {Repository}", 
                    workspace, repository);
                throw;
            }
        }

        public async Task<CommitsByPersonDto?> GetCommitsByPersonEmailAsync(
            string workspace, 
            string repository, 
            string email, 
            string? branch = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Getting commits for person with email: {Email} in workspace: {Workspace}, repository: {Repository}", 
                    email, workspace, repository);

                var commitsByPerson = await GetCommitsByPersonAsync(workspace, repository, branch, startDate, endDate);
                
                var personCommits = commitsByPerson
                    .FirstOrDefault(c => c.PersonEmail.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (personCommits == null)
                {
                    _logger.LogWarning("No commits found for email: {Email}", email);
                }

                return personCommits;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commits by person email: {Email} for workspace: {Workspace}, repository: {Repository}", 
                    email, workspace, repository);
                throw;
            }
        }
    }
}
