namespace Bitbucket.Metrics.Models.Dto.Bitbucket
{
    public class CommitHeatmapPointDto
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int Hour { get; set; }
        public int CommitCount { get; set; }
    }
}
