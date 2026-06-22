namespace Template.Models.Dto.Bitbucket
{
    public class ReviewerMetricDto
    {
        public string Reviewer { get; set; } = string.Empty;
        public int PullRequestsReviewed { get; set; }
        public int Approvals { get; set; }
    }
}
