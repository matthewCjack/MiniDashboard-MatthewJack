using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace MiniDashboard.Api.IntegrationTests
{
    public class DataControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public DataControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetDatasets_ShouldReturnEmptyInitially()
        {
            HttpResponseMessage response = await _client.GetAsync("/api/data/datasets");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            List<string>? datasets = await response.Content.ReadFromJsonAsync<List<string>>();
            datasets.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Post_Then_Get_ShouldReturnAddedItem()
        {
            string dataset = "testDataset";
            Dictionary<string, object> newItem = new Dictionary<string, object>
            {
                ["Name"] = "Item1",
                ["Value"] = 123
            };

            // POST new item
            HttpResponseMessage postResponse = await _client.PostAsJsonAsync($"/api/data/{dataset}", newItem);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // GET dataset
            HttpResponseMessage getResponse = await _client.GetAsync($"/api/data/{dataset}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            List<Dictionary<string, object>>? items = await getResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            items.Should().HaveCount(1);
            items[0]["Name"].ToString().Should().Be("Item1");
        }

        [Fact]
        public async Task Put_ShouldUpdateExistingItem()
        {
            string dataset = "updateTest";
            Dictionary<string, object> newItem = new Dictionary<string, object> { ["Name"] = "OldName" };

            // Add item
            HttpResponseMessage postResponse = await _client.PostAsJsonAsync($"/api/data/{dataset}", newItem);
            postResponse.EnsureSuccessStatusCode();
            Dictionary<string, object>? createdItem = await postResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            int id = Convert.ToInt32(createdItem["Id"].ToString());

            // Update item
            Dictionary<string, object> updatedItem = new Dictionary<string, object> { ["Id"] = id, ["Name"] = "NewName" };
            HttpResponseMessage putResponse = await _client.PutAsJsonAsync($"/api/data/{dataset}/{id}", updatedItem);

            putResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Verify update
            HttpResponseMessage getResponse = await _client.GetAsync($"/api/data/{dataset}");
            List<Dictionary<string, object>>? items = await getResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            items![0]["Name"].ToString().Should().Be("NewName");
        }

        [Fact]
        public async Task Delete_ShouldRemoveItem()
        {
            string dataset = "deleteTest";
            Dictionary<string, object> newItem = new Dictionary<string, object> { ["Name"] = "ToDelete" };

            // Add item
            HttpResponseMessage postResponse = await _client.PostAsJsonAsync($"/api/data/{dataset}", newItem);
            Dictionary<string, object>? created = await postResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            int id = Convert.ToInt32(created["Id"].ToString());

            // Delete item
            HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/api/data/{dataset}/{id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Confirm deletion
            HttpResponseMessage getResponse = await _client.GetAsync($"/api/data/{dataset}");
            List<Dictionary<string, object>>? items = await getResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            items.Should().BeEmpty();
        }

        [Fact]
        public async Task Search_ShouldReturnMatchingItem()
        {
            string dataset = "searchTest";

            // Add items
            await _client.PostAsJsonAsync($"/api/data/{dataset}", new Dictionary<string, object> { ["Name"] = "Apple" });
            await _client.PostAsJsonAsync($"/api/data/{dataset}", new Dictionary<string, object> { ["Name"] = "Banana" });
            await _client.PostAsJsonAsync($"/api/data/{dataset}", new Dictionary<string, object> { ["Name"] = "Cherry" });

            // Search
            HttpResponseMessage searchResponse = await _client.GetAsync($"/api/data/{dataset}/search?query=ban");
            searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            List<Dictionary<string, object>>? results = await searchResponse.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
            results.Should().ContainSingle();
            results![0]["Name"].ToString().Should().Be("Banana");
        }
    }
}
