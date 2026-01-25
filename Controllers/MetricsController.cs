using BitbucketApi.Models.Dto.Metrics;
using BitbucketApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BitbucketApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(IMetricsService metricsService, ILogger<MetricsController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Gets commits grouped by person for a specific repository
        /// </summary>
        /// <param name="workspace">Bitbucket workspace</param>
        /// <param name="repository">Repository name</param>
        /// <param name="branch">Specific branch (optional)</param>
        /// <param name="startDate">Start date to filter commits (optional)</param>
        /// <param name="endDate">End date to filter commits (optional)</param>
        /// <returns>List of commits grouped by person</returns>
        [HttpGet("commits-by-person")]
        public async Task<IActionResult> GetCommitsByPerson(
            [FromQuery] string workspace,
            [FromQuery] string repository,
            [FromQuery] string? branch = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            _logger.LogInformation(
                "GetCommitsByPerson endpoint called - Workspace: {Workspace}, Repository: {Repository}, Branch: {Branch}",
                workspace, repository, branch);

            try
            {
                if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repository))
                {
                    return BadRequest("Workspace and repository are required");
                }

                var result = await _metricsService.GetCommitsByPersonAsync(
                    workspace, repository, branch, startDate, endDate);

                _logger.LogInformation("Retrieved commits by person: {Count} persons", result.Count);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commits by person");
                return StatusCode(500, "An error occurred while retrieving commits by person");
            }
        }

        /// <summary>
        /// Gets commits from a specific person by their email
        /// </summary>
        /// <param name="workspace">Bitbucket workspace</param>
        /// <param name="repository">Repository name</param>
        /// <param name="email">Person's email</param>
        /// <param name="branch">Specific branch (optional)</param>
        /// <param name="startDate">Start date to filter commits (optional)</param>
        /// <param name="endDate">End date to filter commits (optional)</param>
        /// <returns>Commits from the specified person</returns>
        [HttpGet("commits-by-person/{email}")]
        public async Task<IActionResult> GetCommitsByPersonEmail(
            [FromQuery] string workspace,
            [FromQuery] string repository,
            [FromRoute] string email,
            [FromQuery] string? branch = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            _logger.LogInformation(
                "GetCommitsByPersonEmail endpoint called - Workspace: {Workspace}, Repository: {Repository}, Email: {Email}",
                workspace, repository, email);

            try
            {
                if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest("Workspace, repository and email are required");
                }

                var result = await _metricsService.GetCommitsByPersonEmailAsync(
                    workspace, repository, email, branch, startDate, endDate);

                if (result == null)
                {
                    _logger.LogWarning("No commits found for email: {Email}", email);
                    return NotFound($"No commits found for email: {email}");
                }

                _logger.LogInformation("Retrieved {Count} commits for email: {Email}", result.TotalCommits, email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commits by person email");
                return StatusCode(500, "An error occurred while retrieving commits by person email");
            }
        }
    }
}
