using CommunityToolkit.Mvvm.Input;
using FluentAssertions;
using MiniDashboard.App.Services;
using MiniDashboard.App.ViewModels;
using Moq;
using System.Diagnostics;

namespace MiniDashboard.Tests.ViewModels
{
    public class ItemsViewModelTests
    {
        private readonly Mock<IApiService> _mockApiService;
        private readonly ItemsViewModel _vm;

        public ItemsViewModelTests()
        {
            _mockApiService = new Mock<IApiService>();

            // Default mock for GetDataAsync: returns a single item
            _mockApiService.Setup(s => s.GetDataAsync(It.IsAny<string>()))
                .ReturnsAsync((string dataset) =>
                {
                    // Default single item
                    return
                    [
                        new() { ["Id"] = 1, ["Name"] = "Item1" }
                    ];
                });

            // Default mock for SearchDataAsync: returns a single search result
            _mockApiService.Setup(s => s.SearchDataAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string dataset, string query) =>
                {
                    return
                    [
                        new() { ["Id"] = 1, ["Name"] = "SearchResult" }
                    ];
                });

            _vm = new ItemsViewModel(_mockApiService.Object, searchDebounceMs: 1);

            // Initialize columns to match the default data
            _vm.Columns.Add(new ColumnDefinition { Header = "Id" });
            _vm.Columns.Add(new ColumnDefinition { Header = "Name" });
        }

        [Fact]
        public async Task LoadItems_ShouldPopulateItems()
        {
            await _vm.LoadItems("testDataset");

            _vm.Items.Should().HaveCount(1);
            _vm.Items[0]["Name"].Should().Be("Item1");
        }

        [Fact]
        public void ChangingDataset_ShouldClearSearchText()
        {
            _vm.SearchText = "Some search";
            _vm.SelectedDataset = "testDataset";

            _vm.SearchText.Should().BeEmpty();
        }

        [Fact]
        public async Task AddItem_ShouldCallApiServiceAndReload()
        {
            // Arrange
            _vm.SelectedDataset = "testDataset";

            // Columns must exist so AddItem builds the new dictionary
            _vm.Columns.Add(new ColumnDefinition { Header = "Name" });

            // Setup mock for AddItemAsync
            _mockApiService.Setup(s => s.AddItemAsync(
                    "testDataset",
                    It.IsAny<Dictionary<string, object>>()))
                .Returns(Task.CompletedTask);

            // Setup mock for GetDataAsync to return the "new" item after add
            _mockApiService.Setup(s => s.GetDataAsync("testDataset"))
                .ReturnsAsync(
                [
            new() { ["Name"] = "NewItem" }
                ]);

            // Act
            await _vm.AddItemCommand.ExecuteAsync(null);

            // Assert: AddItemAsync was called once
            _mockApiService.Verify(s => s.AddItemAsync(
                "testDataset",
                It.IsAny<Dictionary<string, object>>()), Times.Once);

            // Assert: LoadItems (GetDataAsync) was called at least once
            _mockApiService.Verify(s => s.GetDataAsync("testDataset"), Times.AtLeastOnce);

            // Assert: Items collection was updated with new item
            _vm.Items.Should().HaveCount(1);
            _vm.Items[0]["Name"].Should().Be("NewItem");
        }


        [Fact]
        public async Task UpdateItem_ShouldCallApiServiceAndReload()
        {
            // Arrange
            DynamicItem item = new DynamicItem { ["Id"] = 1, ["Name"] = "Original" };
            _vm.Items.Clear();
            _vm.Items.Add(item);
            _vm.SelectedDataset = "testDataset";
            _vm.SelectedItem = item;

            // Override GetDataAsync to return updated value
            _mockApiService.Setup(s => s.GetDataAsync("testDataset"))
                .ReturnsAsync([new() { ["Id"] = 1, ["Name"] = "Updated" }]);

            // Act
            await _vm.UpdateItemCommand.ExecuteAsync(null);

            // Assert
            _mockApiService.Verify(s => s.UpdateItemAsync("testDataset", 1, It.IsAny<Dictionary<string, object>>()), Times.Once);
            _vm.Items.Should().HaveCount(1);
            _vm.Items[0]["Name"].Should().Be("Updated");
        }

        [Fact]
        public async Task DeleteItem_ShouldCallApiServiceAndRemoveItem()
        {
            // Arrange
            var item = new DynamicItem { ["Id"] = 1, ["Name"] = "ToDelete" };
            _vm.Items.Clear();
            _vm.Items.Add(item);      // <-- add to Items
            _vm.SelectedDataset = "testDataset";
            _vm.SelectedItem = item;  // <-- same reference

            _mockApiService.Setup(s => s.DeleteItemAsync("testDataset", 1))
                           .Returns(Task.CompletedTask);
            // Act
            await ((IAsyncRelayCommand)_vm.DeleteItemCommand).ExecuteAsync(null);

            // Assert
            _mockApiService.Verify(s => s.DeleteItemAsync("testDataset", 1), Times.Once);
            _vm.Items.Should().BeEmpty();  // now passes
        }

        [Fact]
        public async Task SearchText_WhenSet_ShouldCallSearchAndUpdateItems()
        {
            // Arrange
            _vm.SelectedDataset = "testDataset";

            // Act
            _vm.SearchText = "SearchResult";
            await Task.Delay(50); // debounce in ViewModel

            // Assert
            _vm.Items.Should().ContainSingle();
            _vm.Items[0]["Name"].Should().Be("SearchResult");
            _mockApiService.Verify(s => s.SearchDataAsync("testDataset", "SearchResult"), Times.AtLeastOnce);
        }
    }
}
