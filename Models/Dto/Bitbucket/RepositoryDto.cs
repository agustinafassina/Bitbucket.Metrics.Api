namespace BitbucketApi.Models.Dto.Bitbucket
{
    public class RepositoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Workspace { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
