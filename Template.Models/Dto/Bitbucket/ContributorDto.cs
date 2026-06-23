namespace Template.Models.Dto.Bitbucket
{
    public class ContributorDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int CommitCount { get; set; }
        public IReadOnlyList<string> Repositories { get; set; } = new List<string>();
    }
}
