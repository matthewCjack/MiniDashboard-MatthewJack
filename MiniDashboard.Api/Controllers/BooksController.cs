using Microsoft.AspNetCore.Mvc;
using MiniDashboard.Shared.Models;
using MiniDashboard.Api.Services;

namespace MiniDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly BookRepository _repository;

        public BooksController(BookRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public ActionResult<List<Book>> Get() => _repository.GetAllBooks();

        [HttpPost]
        public ActionResult<Book> Post(Book book) => _repository.AddBook(book);

        [HttpPut("{id}")]
        public IActionResult Put(int id, Book updatedBook)
        {
            var updated = _repository.UpdateBook(id, updatedBook);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var deleted = _repository.DeleteBook(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}


//using Microsoft.AspNetCore.Mvc;
//using MiniDashboard.Shared.Models;
//using System.Text.Json;

//namespace MiniDashboard.Api.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class BooksController : ControllerBase
//    {
//        private readonly string _filePath = "C:\\books.json";
//        private List<Book> _books;

//        public BooksController()
//        {
//            LoadBooks();
//        }

//        private void LoadBooks()
//        {
//            Console.WriteLine("filepath1=" + _filePath);
//            if (System.IO.File.Exists(_filePath))
//            {
//                Console.WriteLine("filepath2=" + _filePath);
//                var json = System.IO.File.ReadAllText(_filePath);
//                _books = JsonSerializer.Deserialize<List<Book>>(json) ?? new List<Book>();
//            }
//            else
//            {
//                _books = new List<Book>();
//            }
//        }

//        private void SaveBooks()
//        {
//            var json = JsonSerializer.Serialize(_books, new JsonSerializerOptions { WriteIndented = true });
//            System.IO.File.WriteAllText(_filePath, json);
//        }


//        [HttpGet]
//        public ActionResult<List<Book>> Get() => _books;

//        [HttpPost]
//        public ActionResult<Book> Post(Book book)
//        {
//            book.Id = _books.Count > 0 ? _books.Max(b => b.Id) + 1 : 1;
//            _books.Add(book);
//            SaveBooks();
//            return book;
//        }

//        [HttpPut("{id}")]
//        public IActionResult Put(int id, Book updatedBook)
//        {
//            var book = _books.FirstOrDefault(b => b.Id == id);
//            if (book == null) return NotFound();
//            book.Title = updatedBook.Title;
//            book.Author = updatedBook.Author;
//            book.Year = updatedBook.Year;
//            SaveBooks();
//            return NoContent();
//        }

//        [HttpDelete("{id}")]
//        public IActionResult Delete(int id)
//        {
//            var book = _books.FirstOrDefault(b => b.Id == id);
//            if (book == null) return NotFound();
//            _books.Remove(book);
//            SaveBooks();
//            return NoContent();
//        }
//    }
//}

