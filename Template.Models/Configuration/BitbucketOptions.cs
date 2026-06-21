namespace Template.Models.Configuration
{
    public class BitbucketOptions
    {
        public const string SectionName = "Bitbucket";
        public string BaseUrl { get; set; } = "https://api.bitbucket.org/2.0";
        public string Workspace { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        // Safety cap so a workspace with huge history doesn't hammer the API.
        public int MaxPages { get; set; } = 20;
        public int PageLength { get; set; } = 100;
    }
}
