namespace BitbucketApi.Models.Dto.Bitbucket
{
    public class CommitDto
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AuthorDto Author { get; set; } = new();
        public DateTime Date { get; set; }
        public string Repository { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}
