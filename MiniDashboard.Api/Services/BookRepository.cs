using System.Text.Json;
using MiniDashboard.Shared.Models;

namespace MiniDashboard.Api.Services
{
    public class BookRepository
    {
        private readonly string _filePath;

        public BookRepository(string filePath)
        {
            _filePath = filePath;
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
        }

        public List<Book> GetAllBooks()
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
        }

        public Book AddBook(Book book)
        {
            var books = GetAllBooks();
            book.Id = books.Count > 0 ? books.Max(b => b.Id) + 1 : 1;
            books.Add(book);
            SaveBooks(books);
            return book;
        }

        public bool UpdateBook(int id, Book updatedBook)
        {
            var books = GetAllBooks();
            var existing = books.FirstOrDefault(b => b.Id == id);
            if (existing == null) return false;

            existing.Title = updatedBook.Title;
            existing.Author = updatedBook.Author;
            existing.Year = updatedBook.Year;
            SaveBooks(books);
            return true;
        }

        public bool DeleteBook(int id)
        {
            var books = GetAllBooks();
            var toRemove = books.FirstOrDefault(b => b.Id == id);
            if (toRemove == null) return false;

            books.Remove(toRemove);
            SaveBooks(books);
            return true;
        }

        private void SaveBooks(List<Book> books)
        {
            var json = JsonSerializer.Serialize(books, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
