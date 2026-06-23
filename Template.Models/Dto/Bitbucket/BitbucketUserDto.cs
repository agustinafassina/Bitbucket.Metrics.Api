namespace Template.Models.Dto.Bitbucket
{
    public class BitbucketUserDto
    {
        public string? AccountId { get; set; }
        public string? Uuid { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? Nickname { get; set; }
    }
}
