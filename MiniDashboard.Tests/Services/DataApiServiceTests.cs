using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;
using MiniDashboard.App.Services;
using System.Reflection;

public class DataApiServiceTests
{
    private DataApiService CreateServiceWithMockHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
                responseFactory(request));

        var httpClient = new HttpClient(handler.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        return new DataApiService("http://localhost/")
        {
            // override private field via reflection
        };
    }

    private DataApiService CreateService(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        return new DataApiService("http://localhost/");
    }

    private static HttpMessageHandler MockHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) => responseFactory(req));

        return handler.Object;
    }

    // ------------------------------------------------------------
    // GET DATA
    // ------------------------------------------------------------
    [Fact]
    public async Task GetDataAsync_ShouldConvertJsonTypesCorrectly()
    {
        string json = """
        [
            {
                "Name": "Apple",
                "Count": 5,
                "Price": 3.14,
                "IsActive": true,
                "NullableVal": null
            }
        ]
        """;

        var handler = MockHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var svc = new DataApiService("http://localhost/")
        {
            // Replace HttpClient via internal hack:
        };

        // Inject HttpClient via reflection:
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost/")
            });

        var result = await svc.GetDataAsync("fruit");

        var row = result.Single();

        row["Name"].Should().Be("Apple");
        row["Count"].Should().Be(5);
        row["Price"].Should().Be(3.14);
        row["IsActive"].Should().Be(true);
        row["NullableVal"].Should().Be(null);
    }

    // ------------------------------------------------------------
    // GET DATA ERROR BEHAVIOUR
    // ------------------------------------------------------------
    [Fact]
    public async Task GetDataAsync_ShouldThrow_On404()
    {
        var handler = MockHandler(req =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("Not found")
            });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        Func<Task> act = async () => await svc.GetDataAsync("missing");

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("*404*Not Found*");
    }

    // ------------------------------------------------------------
    // DATASETS LIST
    // ------------------------------------------------------------
    [Fact]
    public async Task GetDatasetsAsync_ShouldReturnStringList()
    {
        string json = """ ["a", "b", "c"] """;

        var handler = MockHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await svc.GetDatasetsAsync();

        result.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    // ------------------------------------------------------------
    // POST ADD
    // ------------------------------------------------------------
    [Fact]
    public async Task AddItemAsync_ShouldCallCorrectUrl()
    {
        HttpRequestMessage? captured = null;

        var handler = MockHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await svc.AddItemAsync("fruit", new() { ["Name"] = "Apple" });

        captured.Should().NotBeNull();
        captured!.RequestUri!.ToString().Should().Be("http://localhost/api/data/fruit");
        captured.Method.Should().Be(HttpMethod.Post);
    }

    // ------------------------------------------------------------
    // PUT UPDATE
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateItemAsync_ShouldCallCorrectUrl()
    {
        HttpRequestMessage? request = null;

        var handler = MockHandler(req =>
        {
            request = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await svc.UpdateItemAsync("fruit", 10, new() { ["Name"] = "Pear" });

        request.Should().NotBeNull();
        request!.RequestUri!.ToString().Should().Be("http://localhost/api/data/fruit/10");
        request.Method.Should().Be(HttpMethod.Put);
    }

    // ------------------------------------------------------------
    // DELETE
    // ------------------------------------------------------------
    [Fact]
    public async Task DeleteItemAsync_ShouldCallCorrectUrl()
    {
        HttpRequestMessage? request = null;

        var handler = MockHandler(req =>
        {
            request = req;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await svc.DeleteItemAsync("fruit", 33);

        request.Should().NotBeNull();
        request!.RequestUri!.ToString().Should().Be("http://localhost/api/data/fruit/33");
        request.Method.Should().Be(HttpMethod.Delete);
    }

    // ------------------------------------------------------------
    // SEARCH QUERY ESCAPING
    // ------------------------------------------------------------
    [Fact]
    public async Task SearchDataAsync_ShouldEscapeQuery()
    {
        // Arrange
        var dataset = "test";
        var query = "a value with spaces & symbols";
        var expectedQuery = "?query=a%20value%20with%20spaces%20%26%20symbols";

        HttpRequestMessage? capturedRequest = null;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            // Setup the SendAsync method
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var service = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(service, httpClient);

        // Act
        var result = await service.SearchDataAsync(dataset, query);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.Should().Be($"/api/data/{dataset}/search");
        capturedRequest.RequestUri!.Query.Should().Be(expectedQuery);

        result.Should().BeEmpty();
    }

    // ------------------------------------------------------------
    // EMPTY JSON HANDLING
    // ------------------------------------------------------------
    [Fact]
    public async Task GetDataAsync_ShouldThrow_OnEmptyJson()
    {
        var handler = MockHandler(req =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("")
            });

        var svc = new DataApiService("http://localhost/");
        typeof(DataApiService)
            .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(svc, new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        Func<Task> act = async () => await svc.GetDataAsync("fruit");

        await act.Should().ThrowAsync<JsonException>();
    }
}
