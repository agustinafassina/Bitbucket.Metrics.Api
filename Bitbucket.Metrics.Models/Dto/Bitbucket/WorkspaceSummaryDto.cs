namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class WorkspaceSummaryDto
    {
        public int? SinceDays { get; set; }
        public int RepositoryCount { get; set; }
        public int CommitCount { get; set; }
        public int ContributorCount { get; set; }
        public int LinkedIssueCount { get; set; }
        public string? BusiestDay { get; set; }
        public int? BusiestHour { get; set; }
        public IReadOnlyList<CommitterMetricDto> TopCommitters { get; set; } = new List<CommitterMetricDto>();
        public IReadOnlyList<RepositoryActivityDto> RepositoryActivity { get; set; } = new List<RepositoryActivityDto>();
    }
}
