namespace BitbucketApi.Models.Dto.Bitbucket
{
    public class BitbucketAuthorResponse
    {
        public string Raw { get; set; } = string.Empty;
        public BitbucketUserResponse? User { get; set; }
    }
}
