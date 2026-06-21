using Template.Models.Dto;

namespace Template.Repository.Interfaces
{
    public interface IItemRepository
    {
        IEnumerable<ItemDto> GetAll();
        ItemDto? GetById(int id);
        ItemDto Add(ItemDto item);
    }
}
