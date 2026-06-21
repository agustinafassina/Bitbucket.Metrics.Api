using Microsoft.AspNetCore.Mvc;
using Template.Services.Interfaces;

namespace Template.Api.Controllers
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
        public async Task<IActionResult> GetCommits(string repoSlug, [FromQuery] int? sinceDays, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetCommits endpoint called for {RepoSlug}", repoSlug);
            IReadOnlyList<BitbucketCommitDto>? commits = await _metricsService.GetCommitsAsync(repoSlug, ResolveSince(sinceDays), cancellationToken);
            return Ok(commits);
        }

        [HttpGet("metrics/top-committers")]
        public async Task<IActionResult> GetTopCommitters(
            [FromQuery] string? repoSlug,
            [FromQuery] int? sinceDays,
            [FromQuery] int top,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetTopCommitters endpoint called (repo: {RepoSlug}, top: {Top})", repoSlug, top);
            IReadOnlyList<CommitterMetricDto>? result = await _metricsService.GetTopCommittersAsync(repoSlug, ResolveSince(sinceDays), top == 0 ? 10 : top, cancellationToken);
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
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("GetCommitFrequency endpoint called (repo: {RepoSlug}, interval: {Interval})", repoSlug, interval);
            IReadOnlyList<Models.Dto.Bitbucket.CommitActivityPointDto>? result = await _metricsService.GetCommitFrequencyAsync(repoSlug, ResolveSince(sinceDays), interval, cancellationToken);
            return Ok(result);
        }

        private static DateTimeOffset? ResolveSince(int? sinceDays)
            => sinceDays is > 0 ? DateTimeOffset.UtcNow.AddDays(-sinceDays.Value) : null;
    }
}
