namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class RepositoryActivityDto
    {
        public string RepositorySlug { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public int ContributorCount { get; set; }
        public DateTimeOffset? FirstCommit { get; set; }
        public DateTimeOffset? LastCommit { get; set; }
        public string? TopContributor { get; set; }
    }
}