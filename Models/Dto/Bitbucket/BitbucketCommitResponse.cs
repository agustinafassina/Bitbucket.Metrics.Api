namespace BitbucketApi.Models.Dto.Bitbucket
{
    public class BitbucketCommitResponse
    {
        public string Hash { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public BitbucketAuthorResponse Author { get; set; } = new();
        public DateTime Date { get; set; }
        public List<BitbucketParentResponse> Parents { get; set; } = new();
    }
}
