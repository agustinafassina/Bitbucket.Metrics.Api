namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class CommitterMetricDto
    {
        public string Author { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int CommitCount { get; set; }
        public DateTimeOffset FirstCommit { get; set; }
        public DateTimeOffset LastCommit { get; set; }
        public double AverageDaysBetweenCommits { get; set; }
        public IReadOnlyList<string> Repositories { get; set; } = new List<string>();
    }
}
