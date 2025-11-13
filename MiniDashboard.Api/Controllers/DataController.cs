using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace MiniDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly string _dataFolder;

        public DataController(IWebHostEnvironment env)
        {
            _dataFolder = Path.Combine(env.ContentRootPath, "Data");
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);
        }

        // GET: api/data/datasets
        [HttpGet("datasets")]
        public ActionResult<List<string>> GetDatasets()
        {
            List<string> files = Directory.GetFiles(_dataFolder, "*.json")
                                 .Select(f => Path.GetFileNameWithoutExtension(f))
                                 .ToList();
            return files;
        }

        // GET: api/data/{dataset}
        [HttpGet("{dataset}")]
        public ActionResult<List<Dictionary<string, object>>> GetData(string dataset)
        {
            string filePath = Path.Combine(_dataFolder, dataset + ".json");
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            string json = System.IO.File.ReadAllText(filePath);
            List<Dictionary<string, object>> data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json)
                       ?? [];
            return data;
        }

        // POST: api/data/{dataset}
        [HttpPost("{dataset}")]
        public IActionResult AddItem(string dataset, [FromBody] Dictionary<string, object> item)
        {
            string filePath = Path.Combine(_dataFolder, dataset + ".json");
            List<Dictionary<string, object>> data;
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json)
                       ?? [];
            }
            else
            {
                data = [];
            }

            // Assign an ID
            int id = data.Count > 0 && data[0].ContainsKey("Id")
                ? data.Max(d => d["Id"] is JsonElement je ? je.GetInt32() : Convert.ToInt32(d["Id"])) + 1
                : 1;

            item["Id"] = id;
            data.Add(item);

            string newJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, newJson);

            return CreatedAtAction(nameof(GetData), new { dataset }, item);
        }

        // PUT: api/data/{dataset}/{id}
        [HttpPut("{dataset}/{id}")]
        public IActionResult UpdateItem(string dataset, int id, [FromBody] Dictionary<string, object> updatedItem)
        {
            string filePath = Path.Combine(_dataFolder, dataset + ".json");
            if (!System.IO.File.Exists(filePath)) return NotFound();

            string json = System.IO.File.ReadAllText(filePath);
            List<Dictionary<string, object>> data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json)
                       ?? [];

            Dictionary<string, object>? existingItem = data.FirstOrDefault(d =>
                d.ContainsKey("Id") &&
                d["Id"] is JsonElement je &&
                je.ValueKind == JsonValueKind.Number &&
                je.GetInt32() == id);

            if (existingItem == null) return NotFound();

            data[data.IndexOf(existingItem)] = updatedItem;

            System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            return NoContent();
        }

        // DELETE: api/data/{dataset}/{id}
        [HttpDelete("{dataset}/{id}")]
        public IActionResult DeleteItem(string dataset, int id)
        {
            string filePath = Path.Combine(_dataFolder, dataset + ".json");
            if (!System.IO.File.Exists(filePath)) return NotFound();

            string json = System.IO.File.ReadAllText(filePath);
            List<Dictionary<string, object>> data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json)
                       ?? [];

            Dictionary<string, object>? item = data.FirstOrDefault(d =>
                d.ContainsKey("Id") &&
                d["Id"] is JsonElement je &&
                je.ValueKind == JsonValueKind.Number &&
                je.GetInt32() == id);
            if (item == null) return NotFound();

            data.Remove(item);
            System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            return NoContent();
        }

        // GET: api/data/{dataset}/search?query=term
        [HttpGet("{dataset}/search")]
        public ActionResult<List<Dictionary<string, object>>> SearchData(string dataset, [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query parameter is required.");

            string filePath = Path.Combine(_dataFolder, dataset + ".json");
            if (!System.IO.File.Exists(filePath))
                return NotFound($"Dataset '{dataset}' not found.");

            string json = System.IO.File.ReadAllText(filePath);
            List<Dictionary<string, object>> data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json)
                       ?? [];

            // Case-insensitive search in all string fields
            List<Dictionary<string, object>> results = data
                .Where(record =>
                    record.Values.Any(value =>
                        value != null &&
                        value.ToString()!.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return results;
        }
    }
}
