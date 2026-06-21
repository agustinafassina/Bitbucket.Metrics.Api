using Template.Models.Dto;

namespace Template.Services.Interfaces
{
    public interface IItemService
    {
        IEnumerable<ItemDto> GetAllItems();
        ItemDto? GetItemById(int id);
        ItemDto CreateItem(ItemCreateDto newItem);
    }
}
