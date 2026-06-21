using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Models.Dto;
using Template.Services.Interfaces;

namespace Template.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;
        private readonly IValidator<ItemCreateDto> _createValidator;
        private readonly ILogger<ItemController> _logger;

        public ItemController(
            IItemService itemService,
            IValidator<ItemCreateDto> createValidator,
            ILogger<ItemController> logger)
        {
            _itemService = itemService;
            _createValidator = createValidator;
            _logger = logger;
        }

        [Authorize(AuthenticationSchemes = "Auth0App1")]
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            _logger.LogInformation("GetVersion endpoint called");
            return Ok("v.1.0.0");
        }

        [HttpGet("test")]
        public IActionResult GetItems()
        {
            _logger.LogInformation("GetItems endpoint called");
            try
            {
                var items = _itemService.GetAllItems();
                _logger.LogInformation("Retrieved {Count} items", items?.Count() ?? 0);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(500, "An error occurred while retrieving items");
            }
        }

        [HttpGet("test/{id}")]
        public IActionResult GetById(int id)
        {
            _logger.LogInformation("GetById endpoint called with id: {ItemId}", id);
            try
            {
                var item = _itemService.GetItemById(id);
                if (item == null)
                {
                    _logger.LogWarning("Item with id {ItemId} not found", id);
                    return NotFound();
                }
                _logger.LogInformation("Item with id {ItemId} retrieved successfully", id);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item with id: {ItemId}", id);
                return StatusCode(500, "An error occurred while retrieving the item");
            }
        }

        [HttpPost("test")]
        public async Task<IActionResult> Create([FromBody] ItemCreateDto dto, CancellationToken cancellationToken = default)
        {
            var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Create item validation failed: {Errors}", string.Join(", ", validation.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
            }

            _logger.LogInformation("Create item endpoint called with name: {Name}", dto.Name);
            try
            {
                var item = _itemService.CreateItem(dto);
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                return StatusCode(500, "An error occurred while creating the item");
            }
        }
    }
}
