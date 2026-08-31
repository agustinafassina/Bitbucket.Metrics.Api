namespace Template.Models.Dto.Bitbucket
{
    public class BitbucketCommitDto
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Date { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string? AuthorAccountId { get; set; }
        public string? AuthorUuid { get; set; }
        public string RepositorySlug { get; set; } = string.Empty;
    }
}
