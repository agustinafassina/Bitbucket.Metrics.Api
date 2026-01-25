namespace BitbucketApi.Models.Dto.Metrics
{
    public class CommitSummaryDto
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Repository { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}
