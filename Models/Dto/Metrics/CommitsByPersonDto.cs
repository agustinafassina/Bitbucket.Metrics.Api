namespace BitbucketApi.Models.Dto.Metrics
{
    public class CommitsByPersonDto
    {
        public string PersonName { get; set; } = string.Empty;
        public string PersonEmail { get; set; } = string.Empty;
        public int TotalCommits { get; set; }
        public List<CommitSummaryDto> Commits { get; set; } = new();
    }
}
