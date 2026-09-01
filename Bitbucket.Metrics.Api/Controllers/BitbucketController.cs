using Microsoft.AspNetCore.Mvc;
using Bitbucket.Metrics.Models.Dto.Bitbucket;
using Bitbucket.Metrics.Services.Interfaces;

namespace Bitbucket.Metrics.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BitbucketController : ControllerBase
    {
        private readonly IBitbucketMetricsService _metricsService;
        private readonly ILogger<BitbucketController> _logger;

        public BitbucketController(IBitbucketMetricsService metricsService, ILogger<BitbucketController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        [HttpGet("repositories")]
        public async Task<IActionResult> GetRepositories(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetRepositories endpoint called");
            IReadOnlyList<BitbucketRepositoryDto>? repos = await _metricsService.GetRepositoriesAsync(cancellationToken);
            return Ok(repos);
        }

        [HttpGet("repositories/{repoSlug}/commits")]
        public async Task<IActionResult> GetCommits(string repoSlug, [FromQuery] int? sinceDays, [FromQuery] string? authorId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetCommits endpoint called for {RepoSlug}", repoSlug);
            IReadOnlyList<BitbucketCommitDto>? commits = await _metricsService.GetCommitsAsync(repoSlug, ResolveSince(sinceDays), authorId, cancellationToken);
            return Ok(commits);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetUsers endpoint called");
            IReadOnlyList<BitbucketUserDto>? result = await _metricsService.GetUsersAsync(cancellationToken);
            return Ok(result);
        }

        [HttpGet("contributors")]
        public async Task<IActionResult> GetContributors(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetContributors endpoint called (repo: {RepoSlug})", repoSlug);
            IReadOnlyList<ContributorDto>? result = await _metricsService.GetContributorsAsync(repoSlug, ResolveSince(sinceDays), cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/top-committers")]
        public async Task<IActionResult> GetTopCommitters(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] int top,
            [FromQuery] string? authorId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetTopCommitters endpoint called (repo: {RepoSlug}, top: {Top})", repoSlug, top);
            IReadOnlyList<CommitterMetricDto>? result = await _metricsService.GetTopCommittersAsync(repoSlug, ResolveSince(sinceDays), top == 0 ? 10 : top, authorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/repository-activity")]
        public async Task<IActionResult> GetRepositoryActivity([FromQuery] int? sinceDays, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetRepositoryActivity endpoint called");
            IReadOnlyList<RepositoryActivityDto>? result = await _metricsService.GetRepositoryActivityAsync(ResolveSince(sinceDays), cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/frequency")]
        public async Task<IActionResult> GetCommitFrequency(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] string interval = "day",
            [FromQuery] string? authorId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GetCommitFrequency endpoint called (repo: {RepoSlug}, interval: {Interval})", repoSlug, interval);
            IReadOnlyList<Models.Dto.Bitbucket.CommitActivityPointDto>? result = await _metricsService.GetCommitFrequencyAsync(repoSlug, ResolveSince(sinceDays), interval, authorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/pull-requests")]
        public async Task<IActionResult> GetPullRequestMetrics(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetPullRequestMetrics endpoint called (repo: {RepoSlug})", repoSlug);
            PullRequestMetricsDto? result = await _metricsService.GetPullRequestMetricsAsync(repoSlug, ResolveSince(sinceDays), cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/reviewers")]
        public async Task<IActionResult> GetReviewerLeaderboard(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] int top,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetReviewerLeaderboard endpoint called (repo: {RepoSlug})", repoSlug);
            IReadOnlyList<ReviewerMetricDto>? result = await _metricsService.GetReviewerLeaderboardAsync(repoSlug, ResolveSince(sinceDays), top == 0 ? 10 : top, cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/churn")]
        public async Task<IActionResult> GetChurn(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] int top,
            [FromQuery] string? authorId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetChurn endpoint called (repo: {RepoSlug})", repoSlug);
            IReadOnlyList<ChurnMetricDto>? result = await _metricsService.GetChurnAsync(repoSlug, ResolveSince(sinceDays), top == 0 ? 10 : top, authorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/heatmap")]
        public async Task<IActionResult> GetActivityHeatmap(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] string? authorId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetActivityHeatmap endpoint called (repo: {RepoSlug})", repoSlug);
            IReadOnlyList<CommitHeatmapPointDto>? result = await _metricsService.GetActivityHeatmapAsync(repoSlug, ResolveSince(sinceDays), authorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("metrics/issues")]
        public async Task<IActionResult> GetIssueActivity(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] string? authorId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetIssueActivity endpoint called (repo: {RepoSlug})", repoSlug);
            IReadOnlyList<IssueActivityDto>? result = await _metricsService.GetIssueActivityAsync(repoSlug, ResolveSince(sinceDays), authorId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("dashboard/summary")]
        public async Task<IActionResult> GetWorkspaceSummary(
            [FromQuery] int? sinceDays,
            [FromQuery] int top,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetWorkspaceSummary endpoint called");
            WorkspaceSummaryDto? result = await _metricsService.GetWorkspaceSummaryAsync(ResolveSince(sinceDays), top == 0 ? 5 : top, cancellationToken);
            return Ok(result);
        }

        private static DateTimeOffset? ResolveSince(int? sinceDays)
            => sinceDays is > 0 ? DateTimeOffset.UtcNow.AddDays(-sinceDays.Value) : null;
    }
}
