using Microsoft.Extensions.Logging;
using Template.Models.Dto;
using Template.Repository.Interfaces;
using Template.Services.Interfaces;
using Template.Services.Mappers;

namespace Template.Services.Implementations
{
    public class ItemService : IItemService
    {
        private readonly IItemRepository _itemRepository;
        private readonly ItemMapper _itemMapper;
        private readonly ILogger<ItemService> _logger;

        public ItemService(IItemRepository itemRepository, ItemMapper itemMapper, ILogger<ItemService> logger)
        {
            _itemRepository = itemRepository;
            _itemMapper = itemMapper;
            _logger = logger;
        }

        public IEnumerable<ItemDto> GetAllItems()
        {
            _logger.LogInformation("GetAllItems called, returning {Count} items", _itemRepository.GetAll().Count());
            return _itemRepository.GetAll();
        }

        public ItemDto? GetItemById(int id)
        {
            _logger.LogInformation("GetItemById called with id: {ItemId}", id);
            var item = _itemRepository.GetById(id);
            if (item == null)
                _logger.LogWarning("Item with id {ItemId} not found", id);
            else
                _logger.LogInformation("Item with id {ItemId} found: {ItemName}", id, item.Name);
            return item;
        }

        public ItemDto CreateItem(ItemCreateDto newItem)
        {
            _logger.LogInformation("CreateItem called with name: {ItemName}", newItem.Name);
            var item = _itemRepository.Add(_itemMapper.ToItemDto(newItem));
            _logger.LogInformation("Item created successfully with id: {ItemId} and name: {ItemName}", item.Id, item.Name);
            return item;
        }
    }
}
