namespace Template.Models.Dto.Bitbucket
{
    public class CommitActivityPointDto
    {
        public string Period { get; set; } = string.Empty;
        public int CommitCount { get; set; }
        public int ContributorCount { get; set; }
    }
}
