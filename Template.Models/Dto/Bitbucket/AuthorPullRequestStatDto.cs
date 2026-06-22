namespace Template.Models.Dto.Bitbucket
{
    public class AuthorPullRequestStatDto
    {
        public string Author { get; set; } = string.Empty;
        public int Opened { get; set; }
        public int Merged { get; set; }
        public int Declined { get; set; }
        public double? AverageHoursToMerge { get; set; }
    }
}
