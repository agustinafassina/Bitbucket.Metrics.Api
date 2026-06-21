using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Template.Models.Dto;
using Template.Repository.Interfaces;
using Template.Services.Implementations;

namespace Template.UnitTests.Services;

public class ItemCatalogServiceTests
{
    [Fact]
    public void GetItemsOrderedByName_sorts_by_name_ordinal()
    {
        var items = new List<ItemDto>
        {
            new() { Id = 1, Name = "Zebra" },
            new() { Id = 2, Name = "apple" },
            new() { Id = 3, Name = "Banana" }
        };
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetAll()).Returns(items);

        var sut = new ItemCatalogService(mockRepo.Object, NullLogger<ItemCatalogService>.Instance);

        var result = sut.GetItemsOrderedByName();

        Assert.Equal(3, result.Count);
        Assert.Equal("Banana", result[0].Name);
        Assert.Equal("Zebra", result[1].Name);
        Assert.Equal("apple", result[2].Name);
    }

    [Fact]
    public void GetItemCount_returns_number_of_items()
    {
        var items = new List<ItemDto>
        {
            new() { Id = 1, Name = "One" },
            new() { Id = 2, Name = "Two" }
        };
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetAll()).Returns(items);

        var sut = new ItemCatalogService(mockRepo.Object, NullLogger<ItemCatalogService>.Instance);

        Assert.Equal(2, sut.GetItemCount());
        mockRepo.Verify(r => r.GetAll(), Times.Once);
    }

    [Fact]
    public void GetItemCount_empty_repository_returns_zero()
    {
        var mockRepo = new Mock<IItemRepository>();
        mockRepo.Setup(r => r.GetAll()).Returns(Array.Empty<ItemDto>());

        var sut = new ItemCatalogService(mockRepo.Object, NullLogger<ItemCatalogService>.Instance);

        Assert.Equal(0, sut.GetItemCount());
    }
}
