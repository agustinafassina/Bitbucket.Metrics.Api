namespace Template.Models.Configuration
{
    public class BitbucketOptions
    {
        public const string SectionName = "Bitbucket";
        public string BaseUrl { get; set; } = "https://api.bitbucket.org/2.0";
        public string Workspace { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public int MaxPages { get; set; } = 20;
        public int PageLength { get; set; } = 100;
        public int MaxDiffCommits { get; set; } = 300;
    }
}
