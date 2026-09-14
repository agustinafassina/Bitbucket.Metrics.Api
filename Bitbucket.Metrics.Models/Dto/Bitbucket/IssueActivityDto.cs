namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class IssueActivityDto
    {
        public string IssueKey { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public DateTimeOffset FirstCommit { get; set; }
        public DateTimeOffset LastCommit { get; set; }
        public IReadOnlyList<string> Authors { get; set; } = new List<string>();
        public IReadOnlyList<string> Repositories { get; set; } = new List<string>();
    }
}
