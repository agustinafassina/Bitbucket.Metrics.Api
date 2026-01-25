using BitbucketApi.Models.Dto.Bitbucket;
using BitbucketApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BitbucketApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class BitbucketController : ControllerBase
    {
        private readonly IBitbucketService _bitbucketService;
        private readonly ILogger<BitbucketController> _logger;

        public BitbucketController(IBitbucketService bitbucketService, ILogger<BitbucketController> logger)
        {
            _bitbucketService = bitbucketService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all commits from a repository
        /// </summary>
        /// <param name="workspace">Bitbucket workspace</param>
        /// <param name="repository">Repository name</param>
        /// <param name="branch">Specific branch (optional)</param>
        /// <param name="limit">Limit of commits to return (optional)</param>
        /// <returns>List of commits</returns>
        [HttpGet("commits")]
        public async Task<IActionResult> GetCommits(
            [FromQuery] string workspace,
            [FromQuery] string repository,
            [FromQuery] string? branch = null,
            [FromQuery] int? limit = null)
        {
            _logger.LogInformation(
                "GetCommits endpoint called - Workspace: {Workspace}, Repository: {Repository}, Branch: {Branch}",
                workspace, repository, branch);

            try
            {
                if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repository))
                {
                    return BadRequest("Workspace and repository are required");
                }

                var commits = await _bitbucketService.GetCommitsAsync(workspace, repository, branch, limit);
                _logger.LogInformation("Retrieved {Count} commits", commits.Count);
                return Ok(commits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commits");
                return StatusCode(500, "An error occurred while retrieving commits");
            }
        }

        /// <summary>
        /// Gets a specific commit by its hash
        /// </summary>
        /// <param name="workspace">Bitbucket workspace</param>
        /// <param name="repository">Repository name</param>
        /// <param name="commitHash">Commit hash</param>
        /// <returns>Commit information</returns>
        [HttpGet("commits/{commitHash}")]
        public async Task<IActionResult> GetCommitById(
            [FromQuery] string workspace,
            [FromQuery] string repository,
            [FromRoute] string commitHash)
        {
            _logger.LogInformation(
                "GetCommitById endpoint called - Workspace: {Workspace}, Repository: {Repository}, Hash: {Hash}",
                workspace, repository, commitHash);

            try
            {
                if (string.IsNullOrWhiteSpace(workspace) || string.IsNullOrWhiteSpace(repository) || string.IsNullOrWhiteSpace(commitHash))
                {
                    return BadRequest("Workspace, repository and commitHash are required");
                }

                var commit = await _bitbucketService.GetCommitByIdAsync(workspace, repository, commitHash);
                
                if (commit == null)
                {
                    _logger.LogWarning("Commit not found: {Hash}", commitHash);
                    return NotFound($"Commit with hash {commitHash} not found");
                }

                _logger.LogInformation("Retrieved commit: {Hash}", commitHash);
                return Ok(commit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving commit");
                return StatusCode(500, "An error occurred while retrieving the commit");
            }
        }

        /// <summary>
        /// Gets all repositories from a workspace
        /// </summary>
        /// <param name="workspace">Bitbucket workspace</param>
        /// <returns>List of repositories</returns>
        [HttpGet("repositories")]
        public async Task<IActionResult> GetRepositories([FromQuery] string workspace)
        {
            _logger.LogInformation("GetRepositories endpoint called - Workspace: {Workspace}", workspace);

            try
            {
                if (string.IsNullOrWhiteSpace(workspace))
                {
                    return BadRequest("Workspace is required");
                }

                var repositories = await _bitbucketService.GetRepositoriesAsync(workspace);
                _logger.LogInformation("Retrieved {Count} repositories", repositories.Count);
                return Ok(repositories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving repositories");
                return StatusCode(500, "An error occurred while retrieving repositories");
            }
        }
    }
}
