using BitbucketApi.Models.Dto;

namespace BitbucketApi.Services.Interfaces
{
    public interface IItemService
    {
        IEnumerable<ItemDto> GetAllItems();
        ItemDto? GetItemById(int id);
        ItemDto CreateItem(ItemCreateDto newItem);
    }
}