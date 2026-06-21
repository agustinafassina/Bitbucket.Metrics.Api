using Microsoft.Extensions.Logging;
using Template.Models.Dto;
using Template.Repository.Interfaces;
using Template.Services.Interfaces;

namespace Template.Services.Implementations
{
    public class ItemCatalogService : IItemCatalogService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ItemCatalogService> _logger;

        public ItemCatalogService(IItemRepository itemRepository, ILogger<ItemCatalogService> logger)
        {
            _itemRepository = itemRepository;
            _logger = logger;
        }

        public IReadOnlyList<ItemDto> GetItemsOrderedByName()
        {
            var list = _itemRepository.GetAll().OrderBy(i => i.Name, StringComparer.Ordinal).ToList();
            _logger.LogDebug("Ordered {Count} items by name", list.Count);
            return list;
        }

        public int GetItemCount()
        {
            int count = _itemRepository.GetAll().Count();
            _logger.LogDebug("Item count: {Count}", count);
            return count;
        }
    }
}
