namespace BitbucketApi.Models.Dto.Bitbucket
{
    public class BitbucketApiResponse<T>
    {
        public List<T> Values { get; set; } = new();
        public int Size { get; set; }
        public int Page { get; set; }
        public int Pagelen { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
    }
}
