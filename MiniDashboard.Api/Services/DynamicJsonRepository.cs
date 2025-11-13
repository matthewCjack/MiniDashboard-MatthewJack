using System.Text.Json;

namespace MiniDashboard.Api.Services
{
    public class DynamicJsonRepository
    {
        private readonly string _filePath;
        private List<DynamicItem> _items;

        public DynamicJsonRepository(string filePath)
        {
            _filePath = filePath;
            _items = LoadItems();
        }

        private List<DynamicItem> LoadItems()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<DynamicItem>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            }
            return [];
        }

        private void SaveItems()
        {
            string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public List<DynamicItem> GetAll() => _items;

        public DynamicItem Add(DynamicItem item)
        {
            item.Id = _items.Count > 0 ? _items.Max(i => i.Id) + 1 : 1;
            _items.Add(item);
            SaveItems();
            return item;
        }

        public void Update(int id, DynamicItem updated)
        {
            DynamicItem? existing = _items.FirstOrDefault(i => i.Id == id);
            if (existing == null) return;
            _items.Remove(existing);
            updated.Id = id;
            _items.Add(updated);
            SaveItems();
        }

        public void Delete(int id)
        {
            DynamicItem? item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null) return;
            _items.Remove(item);
            SaveItems();
        }
    }

    public class DynamicItem : Dictionary<string, object>
    {
        public int Id
        {
            get => TryGetValue("Id", out object? id) ? Convert.ToInt32(id) : 0;
            set => this["Id"] = value;
        }
    }
}
