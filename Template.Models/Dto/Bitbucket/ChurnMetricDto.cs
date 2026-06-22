namespace Template.Models.Dto.Bitbucket
{
    public class ChurnMetricDto
    {
        public string Author { get; set; } = string.Empty;
        public int Commits { get; set; }
        public int LinesAdded { get; set; }
        public int LinesRemoved { get; set; }
        public int NetLines => LinesAdded - LinesRemoved;
        public int TotalChanges => LinesAdded + LinesRemoved;
    }
}
