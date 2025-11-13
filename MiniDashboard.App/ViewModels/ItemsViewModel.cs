using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using MiniDashboard.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Input;

namespace MiniDashboard.App.ViewModels
{
    public class ItemsViewModel : BaseViewModel
    {
        private readonly IApiService _apiService;
        private readonly int _searchDebounceMs;

        public ObservableCollection<DynamicItem> Items { get; set; } = [];
        public ObservableCollection<string> Datasets { get; set; } = [];
        public ObservableCollection<ColumnDefinition> Columns { get; set; } = [];

        private string? _selectedDataset;
        public string? SelectedDataset
        {
            get => _selectedDataset;
            set
            {
                _selectedDataset = value;
                OnPropertyChanged();
                SearchText = string.Empty;
                if (!string.IsNullOrEmpty(_selectedDataset))
                    _ = LoadItems(_selectedDataset);
            }
        }

        private DynamicItem? _selectedItem;
        public DynamicItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        private ICollectionView _itemsView;
        public ICollectionView ItemsView
        {
            get => _itemsView;
            set { _itemsView = value; OnPropertyChanged(); }
        }

        private string _searchText;
        private CancellationTokenSource _searchCts;

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();

                    // Trigger search
                    _ = OnSearchTextChangedAsync(_searchText);
                }
            }
        }

        private async Task OnSearchTextChangedAsync(string query)
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            CancellationToken token = _searchCts.Token;

            try
            {
                // Debounce delay
                await Task.Delay(_searchDebounceMs, token);

                if (!token.IsCancellationRequested)
                {
                    if (string.IsNullOrWhiteSpace(query))
                        await LoadItems(SelectedDataset); // show all
                    else
                        await SearchItems(SelectedDataset, query);
                }
            }
            catch (TaskCanceledException)
            {
                // ignored: user kept typing
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        // Commands
        public ICommand LoadDatasetsCommand { get; }
        public IAsyncRelayCommand AddItemCommand { get; }
        public IAsyncRelayCommand UpdateItemCommand { get; }
        public IAsyncRelayCommand DeleteItemCommand { get; }

        public ItemsViewModel()
        {
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string? baseUrl = config["ApiBaseUrl"];
            _apiService = new DataApiService(baseUrl);

            LoadDatasetsCommand = new AsyncRelayCommand(LoadDatasets);
            AddItemCommand = new AsyncRelayCommand(AddItem);
            UpdateItemCommand = new AsyncRelayCommand(UpdateItem);
            DeleteItemCommand = new AsyncRelayCommand(DeleteItem);

            _ = LoadDatasets();
        }

        public ItemsViewModel(IApiService apiService, int searchDebounceMs = 300)
        {
            _apiService = apiService; // injected mock
            _searchDebounceMs = searchDebounceMs;

            // Initialize commands
            LoadDatasetsCommand = new AsyncRelayCommand(LoadDatasets);
            AddItemCommand = new AsyncRelayCommand(AddItem);
            UpdateItemCommand = new AsyncRelayCommand(UpdateItem);
            DeleteItemCommand = new AsyncRelayCommand(DeleteItem);
        }

        private async Task LoadDatasets()
        {
            List<string> datasets = await _apiService.GetDatasetsAsync();
            Datasets.Clear();
            foreach (string ds in datasets)
                Datasets.Add(ds);

            if (Datasets.Count > 0)
                SelectedDataset = Datasets[0];
        }

        internal async Task LoadItems(string dataset)
        {
            ErrorMessage = string.Empty;

            try
            {
                IsLoading = true;

                List<Dictionary<string, object>> data = await _apiService.GetDataAsync(dataset);
                Items.Clear();
                Columns.Clear();

                if (data.Count > 0)
                {
                    // Build column headers
                    foreach (string key in data[0].Keys)
                        Columns.Add(new ColumnDefinition { Header = key });

                    // Create DynamicItem objects
                    foreach (Dictionary<string, object> dict in data)
                    {
                        DynamicItem item = new DynamicItem();
                        item.InitializeKeys(dict.Keys);
                        foreach (KeyValuePair<string, object> kvp in dict)
                            item[kvp.Key] = kvp.Value;
                        Items.Add(item);
                    }
                }
                //ErrorMessage = "Test error: something went wrong!";

                if (System.Windows.Application.Current != null)
                    ItemsView = CollectionViewSource.GetDefaultView(Items);
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Failed to load items: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                //await Task.Delay(3000);
                IsLoading = false;
            }
        }

        private async Task SearchItems(string dataset, string query)
        {
            ErrorMessage = string.Empty;

            try
            {
                IsLoading = true;

                List<Dictionary<string, object>> data = await _apiService.SearchDataAsync(dataset, query);
                Items.Clear();
                Columns.Clear();

                if (data.Count > 0)
                {
                    foreach (string key in data[0].Keys)
                        Columns.Add(new ColumnDefinition { Header = key });

                    foreach (Dictionary<string, object> dict in data)
                    {
                        DynamicItem item = new DynamicItem();
                        item.InitializeKeys(dict.Keys);
                        foreach (KeyValuePair<string, object> kvp in dict)
                            item[kvp.Key] = kvp.Value;
                        Items.Add(item);
                    }
                }

                ItemsView = CollectionViewSource.GetDefaultView(Items);
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Search failed: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task AddItem()
        {
            if (string.IsNullOrEmpty(SelectedDataset)) return;

            ErrorMessage = string.Empty;

            try
            {
                IsLoading = true;

                Dictionary<string, object> newItem = [];

                // Use the column headers
                foreach (ColumnDefinition col in Columns)
                    newItem[col.Header] = null;

                await _apiService.AddItemAsync(SelectedDataset, newItem);

                // Refresh after successful add
                await LoadItems(SelectedDataset);
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Failed to add item: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateItem()
        {
            if (SelectedItem == null || string.IsNullOrEmpty(SelectedDataset)) return;

            ErrorMessage = string.Empty;

            try
            {
                IsLoading = true;

                // Convert DynamicItem to Dictionary using Columns
                Dictionary<string, object> dict = [];
                foreach (ColumnDefinition col in Columns)
                    dict[col.Header] = SelectedItem[col.Header];

                if (!dict.ContainsKey("Id"))
                {
                    ErrorMessage = "Cannot update: item has no Id field.";
                    return;
                }

                int id = Convert.ToInt32(dict["Id"]);
                await _apiService.UpdateItemAsync(SelectedDataset, id, dict);

                // Reload after update
                await LoadItems(SelectedDataset);
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Failed to update item: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteItem()
        {
            if (SelectedItem == null || string.IsNullOrEmpty(SelectedDataset)) return;

            ErrorMessage = string.Empty;

            try
            {
                IsLoading = true;

                // Convert DynamicItem to Dictionary using Columns
                Dictionary<string, object> dict = [];
                foreach (ColumnDefinition col in Columns)
                    dict[col.Header] = SelectedItem[col.Header];

                if (!dict.ContainsKey("Id"))
                {
                    ErrorMessage = "Cannot delete: item has no Id field.";
                    return;
                }

                int id = Convert.ToInt32(dict["Id"]);
                await _apiService.DeleteItemAsync(SelectedDataset, id);

                // Remove locally to keep UI in sync
                var itemToRemove = Items.FirstOrDefault(i => Convert.ToInt32(i["Id"]) == id);
                if (itemToRemove != null)
                    Items.Remove(itemToRemove);
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"Failed to delete item: {ex.Message}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public class ColumnDefinition
    {
        public string Header { get; set; } = string.Empty;
    }


    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class DynamicItem : DynamicObject, INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> _values = [];

        public event PropertyChangedEventHandler? PropertyChanged;

        // Indexer for easy access
        public object this[string key]
        {
            get => _values.ContainsKey(key) ? _values[key] : null;
            set
            {
                _values[key] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
            }
        }

        public IEnumerable<string> GetDynamicMemberNames()
        {
            return _values.Keys;
        }

        // Expose keys for ViewModel
        public IEnumerable<string> Keys => _values.Keys;

        // DynamicObject overrides for WPF binding
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _values.ContainsKey(binder.Name) ? _values[binder.Name] : null;
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _values[binder.Name] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(binder.Name));
            return true;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not DynamicItem other) return false;
            // compare by reference or by Id if present
            if (_values.ContainsKey("Id") && other._values.ContainsKey("Id"))
                if (_values["Id"] is JsonElement idElem && other._values["Id"] is JsonElement otherIdElem)
                    return idElem.GetInt32() == otherIdElem.GetInt32();
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            if (_values.ContainsKey("Id"))
            {
                object idValue = _values["Id"];
                if (idValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetInt32().GetHashCode();
            }
            return base.GetHashCode();
        }

        // Optional: add a method to initialize all keys
        public void InitializeKeys(IEnumerable<string> keys)
        {
            foreach (string key in keys)
                if (!_values.ContainsKey(key))
                    _values[key] = null;
        }
    }

    // helper to make properties visible to WPF
    public class DynamicPropertyDescriptor : PropertyDescriptor
    {
        private readonly Type _propertyType;

        public DynamicPropertyDescriptor(string name, Type propertyType)
            : base(name, null)
        {
            _propertyType = propertyType;
        }

        public override Type PropertyType => _propertyType;
        public override void SetValue(object component, object value) => ((DynamicItem)component)[Name] = value;
        public override object GetValue(object component) => ((DynamicItem)component)[Name];
        public override bool IsReadOnly => false;
        public override Type ComponentType => typeof(DynamicItem);
        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => true;
    }


}
