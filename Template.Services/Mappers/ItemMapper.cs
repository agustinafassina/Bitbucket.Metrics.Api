using Riok.Mapperly.Abstractions;
using Template.Models.Dto;

namespace Template.Services.Mappers
{
    [Mapper]
    public partial class ItemMapper
    {
        [MapperIgnoreTarget(nameof(ItemDto.Id))]
        public partial ItemDto ToItemDto(ItemCreateDto source);
    }
}
