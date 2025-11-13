using MiniDashboard.Shared.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace MiniDashboard.App.Services
{
    public class BooksApiService
    {
        private readonly HttpClient _httpClient;

        public BooksApiService(string baseUrl)
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/Books");
                response.EnsureSuccessStatusCode();

                var books = await response.Content.ReadFromJsonAsync<List<Book>>();
                return books ?? new List<Book>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading books: {ex.Message}");
                return new List<Book>();
            }
        }

        public async Task<Book> AddBookAsync(Book book)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Books", book);
            response.EnsureSuccessStatusCode(); // throw if the call failed
            return await response.Content.ReadFromJsonAsync<Book>();
        }

        public async Task UpdateBookAsync(Book book) =>
            await _httpClient.PutAsJsonAsync($"api/Books/{book.Id}", book);

        public async Task DeleteBookAsync(int id) =>
            await _httpClient.DeleteAsync($"api/Books/{id}");
    }
}
