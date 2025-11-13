using MiniDashboard.Shared.Models;
using MiniDashboard.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using static System.Net.WebRequestMethods;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace MiniDashboard.App.ViewModels
{
    //public class BookViewModel : BaseViewModel
    //{
    //    private readonly HttpClient _http;

    //    public ObservableCollection<Book> Books { get; set; } = new ObservableCollection<Book>();

    //    private Book? _selectedBook;
    //    public Book? SelectedBook
    //    {
    //        get => _selectedBook;
    //        set
    //        {
    //            _selectedBook = value;
    //            OnPropertyChanged();
    //        }
    //    }

    //    public ICommand LoadBooksCommand { get; }
    //    public ICommand AddBookCommand { get; }
    //    public ICommand EditBookCommand { get; }
    //    public ICommand DeleteBookCommand { get; }

    //    public BookViewModel()
    //    {
    //        _http = new HttpClient { BaseAddress = new Uri("https://localhost:5001/") }; // API URL

    //        LoadBooksCommand = new RelayCommand(async () => await LoadBooks());
    //        AddBookCommand = new RelayCommand(async () => await AddBook(SelectedBook));
    //        EditBookCommand = new RelayCommand(async () => await EditBook(SelectedBook));
    //        DeleteBookCommand = new RelayCommand(async () => await DeleteBook(SelectedBook));

    //        // Load books initially
    //        _ = LoadBooks();
    //    }

    //    public async Task LoadBooks()
    //    {
    //        try
    //        {
    //            var response = await _http.GetAsync("api/books");
    //            response.EnsureSuccessStatusCode();

    //            var json = await response.Content.ReadAsStringAsync();
    //            var books = JsonSerializer.Deserialize<ObservableCollection<Book>>(json,
    //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    //            Books = books ?? new ObservableCollection<Book>();
    //            OnPropertyChanged(nameof(Books));
    //        }
    //        catch (Exception ex)
    //        {
    //            // Handle/log error
    //        }
    //    }

    //    public async Task AddBook(Book? book)
    //    {
    //        if (book == null) return;

    //        var json = JsonSerializer.Serialize(book);
    //        var content = new StringContent(json, Encoding.UTF8, "application/json");

    //        var response = await _http.PostAsync("api/books", content);
    //        if (response.IsSuccessStatusCode) await LoadBooks();
    //    }

    //    public async Task EditBook(Book? book)
    //    {
    //        if (book == null) return;

    //        var json = JsonSerializer.Serialize(book);
    //        var content = new StringContent(json, Encoding.UTF8, "application/json");

    //        var response = await _http.PutAsync($"api/books/{book.Id}", content);
    //        if (response.IsSuccessStatusCode) await LoadBooks();
    //    }

    //    public async Task DeleteBook(Book? book)
    //    {
    //        if (book == null) return;

    //        var response = await _http.DeleteAsync($"api/books/{book.Id}");
    //        if (response.IsSuccessStatusCode) await LoadBooks();
    //    }
    //}

    public class BookViewModel : BaseViewModel
    {
        private readonly BooksApiService _booksApiService; // service to call Web API

        public ObservableCollection<Book> Books { get; set; } = new ObservableCollection<Book>();

        private Book? _selectedBook;
        public Book? SelectedBook
        {
            get => _selectedBook;
            set
            {
                _selectedBook = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoadBooksCommand { get; }
        public ICommand AddBookCommand { get; }
        public ICommand UpdateBookCommand { get; }
        public ICommand DeleteBookCommand { get; }

        public BookViewModel()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var baseUrl = config["ApiBaseUrl"];

            _booksApiService = new BooksApiService(baseUrl);

            LoadBooksCommand = new AsyncRelayCommand(LoadBooks);
            AddBookCommand = new AsyncRelayCommand(AddBook);
            UpdateBookCommand = new AsyncRelayCommand(UpdateBook);
            DeleteBookCommand = new AsyncRelayCommand(DeleteBook);

            _ = LoadBooks();
        }

        // Load books from API
        private async Task LoadBooks()
        {
            var booksFromApi = await _booksApiService.GetBooksAsync();
            Books.Clear();
            foreach (var book in booksFromApi)
                Books.Add(book);
        }

        private async Task AddBook()
        {
            // Example: add the first new book (replace with UI binding logic)
            var newBook = new Book { Title = "New Book", Author = "Unknown" };
            var addedBook = await _booksApiService.AddBookAsync(newBook);
            Books.Add(addedBook);
        }

        private async Task UpdateBook()
        {
            // Example: update the first book (replace with UI selection logic)
            if (Books.Count == 0) return;

            var book = Books[0];
            book.Title += " (Updated)";
            await _booksApiService.UpdateBookAsync(book);
        }

        private async Task DeleteBook()
        {
            // Example: delete the first book (replace with UI selection logic)
            if (Books.Count == 0) return;

            var book = Books[0];
            await _booksApiService.DeleteBookAsync(book.Id);
            Books.Remove(book);
        }
    }
    
    //public class Book : INotifyPropertyChanged
    //{
    //    private string _title;
    //    public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }

    //    private string _author;
    //    public string Author { get => _author; set { _author = value; OnPropertyChanged(nameof(Author)); } }

    //    private int _year;
    //    public int Year { get => _year; set { _year = value; OnPropertyChanged(nameof(Year)); } }

    //    public event PropertyChangedEventHandler PropertyChanged;
    //    protected void OnPropertyChanged(string propertyName) =>
    //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //}

    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
