using Template.Models.Dto;

namespace Template.Services.Interfaces
{
    public interface IItemCatalogService
    {
        IReadOnlyList<ItemDto> GetItemsOrderedByName();
        int GetItemCount();
    }
}
