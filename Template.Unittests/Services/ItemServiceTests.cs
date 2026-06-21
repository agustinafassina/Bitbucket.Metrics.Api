using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Template.Models.Dto;
using Template.Repository.Interfaces;
using Template.Services.Implementations;
using Template.Services.Mappers;

namespace Template.UnitTests.Services;

public class ItemServiceTests
{
    [Fact]
    public void GetAllItems_returns_items_from_repository()
    {
        var items = new List<ItemDto> { new() { Id = 1, Name = "Alpha" }, new() { Id = 2, Name = "Beta" } };
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetAll()).Returns(items);

        var sut = new ItemService(mockRepo.Object, new ItemMapper(), NullLogger<ItemService>.Instance);

        var result = sut.GetAllItems().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        mockRepo.Verify(r => r.GetAll(), Times.Exactly(2));
    }

    [Fact]
    public void GetItemById_returns_item_when_repository_finds_it()
    {
        var expected = new ItemDto { Id = 5, Name = "Found" };
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetById(5)).Returns(expected);

        var sut = new ItemService(mockRepo.Object, new ItemMapper(), NullLogger<ItemService>.Instance);

        var result = sut.GetItemById(5);

        Assert.NotNull(result);
        Assert.Equal(5, result.Id);
        Assert.Equal("Found", result.Name);
        mockRepo.Verify(r => r.GetById(5), Times.Once);
    }

    [Fact]
    public void GetItemById_returns_null_when_missing()
    {
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetById(It.IsAny<int>())).Returns((ItemDto?)null);

        var sut = new ItemService(mockRepo.Object, new ItemMapper(), NullLogger<ItemService>.Instance);

        Assert.Null(sut.GetItemById(999));
    }

    [Fact]
    public void CreateItem_maps_dto_and_persists_via_repository()
    {
        var mockRepo = new Mock<IItemRepository>();
        mockRepo
            .Setup(r => r.Add(It.Is<ItemDto>(d => d.Name == "New item")))
            .Returns((ItemDto dto) => new ItemDto { Id = 42, Name = dto.Name });

        var sut = new ItemService(mockRepo.Object, new ItemMapper(), NullLogger<ItemService>.Instance);

        var created = sut.CreateItem(new ItemCreateDto { Name = "New item" });

        Assert.Equal(42, created.Id);
        Assert.Equal("New item", created.Name);
        mockRepo.Verify(r => r.Add(It.Is<ItemDto>(d => d.Name == "New item")), Times.Once);
    }
}
