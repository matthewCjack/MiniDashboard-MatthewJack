using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiniDashboard.App.Services
{
    public interface IApiService
    {
        Task<List<string>> GetDatasetsAsync();
        Task<List<Dictionary<string, object>>> GetDataAsync(string dataset);
        Task AddItemAsync(string dataset, Dictionary<string, object> item);
        Task UpdateItemAsync(string dataset, int id, Dictionary<string, object> item);
        Task DeleteItemAsync(string dataset, int id);
        Task<List<Dictionary<string, object>>> SearchDataAsync(string dataset, string query);
    }

    public class DataApiService : IApiService
    {
        private readonly HttpClient _httpClient;

        public DataApiService(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        // --- Helper for GET requests with proper error handling ---
        private async Task<T> GetJsonCheckedAsync<T>(string url)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"GET {url} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
            }

            T? result = await response.Content.ReadFromJsonAsync<T>();
            return result ?? throw new InvalidOperationException($"Empty response for {url}");
        }

        // --- Helper for requests that return no body (POST, PUT, DELETE) ---
        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string url)
        {
            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"{response.RequestMessage?.Method} {url} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
            }
        }

        // ---------------------------------------------------------------
        // ENDPOINT METHODS
        // ---------------------------------------------------------------

        public async Task<List<string>> GetDatasetsAsync() =>
            await GetJsonCheckedAsync<List<string>>("api/data/datasets");

        public async Task<List<Dictionary<string, object>>> GetDataAsync(string dataset)
        {
            string url = $"api/data/{dataset}";
            using HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException(
                    $"GET {url} failed ({(int)response.StatusCode} {response.ReasonPhrase}): {body}");
            }

            string json = await response.Content.ReadAsStringAsync();
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            List<Dictionary<string, JsonElement>> raw = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json, options)
                      ?? [];

            List<Dictionary<string, object>> result = [];

            foreach (Dictionary<string, JsonElement> row in raw)
            {
                Dictionary<string, object> dict = [];
                foreach (KeyValuePair<string, JsonElement> kvp in row)
                {
                    dict[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        JsonValueKind.String => kvp.Value.GetString(),
                        JsonValueKind.Number => kvp.Value.TryGetInt32(out int i)
                            ? i
                            : kvp.Value.TryGetDecimal(out decimal d)
                                ? d
                                : kvp.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null!,
                        _ => kvp.Value.ToString()
                    };
                }
                result.Add(dict);
            }

            return result;
        }

        public async Task AddItemAsync(string dataset, Dictionary<string, object> item)
        {
            string url = $"api/data/{dataset}";
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(url, item);
            await EnsureSuccessAsync(response, url);
        }

        public async Task UpdateItemAsync(string dataset, int id, Dictionary<string, object> item)
        {
            string url = $"api/data/{dataset}/{id}";
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(url, item);
            await EnsureSuccessAsync(response, url);
        }

        public async Task DeleteItemAsync(string dataset, int id)
        {
            string url = $"api/data/{dataset}/{id}";
            using HttpResponseMessage response = await _httpClient.DeleteAsync(url);
            await EnsureSuccessAsync(response, url);
        }

        public async Task<List<Dictionary<string, object>>> SearchDataAsync(string dataset, string query)
        {
            string url = $"api/data/{dataset}/search?query={Uri.EscapeDataString(query)}";
            return await GetJsonCheckedAsync<List<Dictionary<string, object>>>(url);
        }

    }
}
