
using BitbucketApi.Models.Dto;
using BitbucketApi.Services.Interfaces;

namespace BitbucketApi.Services.Implementations
{
    public class ItemService : IItemService
    {
        private readonly List<ItemDto> _items = new();
        private readonly ILogger<ItemService> _logger;

        public ItemService(ILogger<ItemService> logger)
        {
            _logger = logger;
            _items.Add(new ItemDto { Id = 1, Name = "Item 1" });
            _items.Add(new ItemDto { Id = 2, Name = "Item 2" });
            _logger.LogInformation("ItemService initialized with {Count} default items", _items.Count);
        }

        public IEnumerable<ItemDto> GetAllItems()
        {
            _logger.LogInformation("GetAllItems called, returning {Count} items", _items.Count);
            return _items;
        }

        public ItemDto? GetItemById(int id)
        {
            _logger.LogInformation("GetItemById called with id: {ItemId}", id);
            ItemDto? item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null)
            {
                _logger.LogWarning("Item with id {ItemId} not found", id);
            }
            else
            {
                _logger.LogInformation("Item with id {ItemId} found: {ItemName}", id, item.Name);
            }
            return item;
        }

        public ItemDto CreateItem(ItemCreateDto newItem)
        {
            _logger.LogInformation("CreateItem called with name: {ItemName}", newItem.Name);
            int newId = _items.Any() ? _items.Max(i => i.Id) + 1 : 1;
            ItemDto item = new ItemDto { Id = newId, Name = newItem.Name };
            _items.Add(item);
            _logger.LogInformation("Item created successfully with id: {ItemId} and name: {ItemName}", newId, item.Name);
            return item;
        }
    }
}