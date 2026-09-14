namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class PullRequestMetricsDto
    {
        public int TotalOpen { get; set; }
        public int TotalMerged { get; set; }
        public int TotalDeclined { get; set; }
        public double? AverageHoursToMerge { get; set; }
        public double? MedianHoursToMerge { get; set; }
        public IReadOnlyList<AuthorPullRequestStatDto> ByAuthor { get; set; } = new List<AuthorPullRequestStatDto>();
    }
}
