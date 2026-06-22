namespace Template.Models.Dto.Bitbucket
{
    public class BitbucketPullRequestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset UpdatedOn { get; set; }
        public int CommentCount { get; set; }
        public double? HoursToMerge { get; set; }
        public string RepositorySlug { get; set; } = string.Empty;
    }
}
