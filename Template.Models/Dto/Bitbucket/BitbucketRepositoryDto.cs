namespace Template.Models.Dto.Bitbucket
{
    public class BitbucketRepositoryDto
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Language { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
    }
}
