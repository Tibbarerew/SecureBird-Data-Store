using System.Text.Json;
using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public class JsonDataService : IJsonDataService
{
    private readonly string _dataRoot;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonDataService()
    {
        _dataRoot = Path.Combine(FileSystem.AppDataDirectory, "SecureBirdData");
        Directory.CreateDirectory(Path.Combine(_dataRoot, "structures"));
    }

    private string StructurePath(string id) =>
        Path.Combine(_dataRoot, "structures", $"{id}.json");

    private string RecordsDir(string structureId) =>
        Path.Combine(_dataRoot, "records", structureId);

    private string RecordPath(string structureId, string recordId) =>
        Path.Combine(RecordsDir(structureId), $"{recordId}.json");

    public async Task<List<DataStructure>> GetStructuresAsync()
    {
        var dir = Path.Combine(_dataRoot, "structures");
        if (!Directory.Exists(dir)) return [];

        var results = new List<DataStructure>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var s = JsonSerializer.Deserialize<DataStructure>(json, _jsonOptions);
                if (s is not null) results.Add(s);
            }
            catch { /* skip malformed files */ }
        }
        return results.OrderBy(s => s.Name).ToList();
    }

    public async Task<DataStructure?> GetStructureAsync(string id)
    {
        var path = StructurePath(id);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<DataStructure>(json, _jsonOptions);
    }

    public async Task SaveStructureAsync(DataStructure structure)
    {
        structure.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(structure, _jsonOptions);
        await File.WriteAllTextAsync(StructurePath(structure.Id), json);
    }

    public async Task DeleteStructureAsync(string id)
    {
        var path = StructurePath(id);
        if (File.Exists(path)) File.Delete(path);

        var recordsDir = RecordsDir(id);
        if (Directory.Exists(recordsDir)) Directory.Delete(recordsDir, recursive: true);

        await Task.CompletedTask;
    }

    public async Task<List<DataRecord>> GetRecordsAsync(string structureId)
    {
        var dir = RecordsDir(structureId);
        if (!Directory.Exists(dir)) return [];

        var results = new List<DataRecord>();
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var r = JsonSerializer.Deserialize<DataRecord>(json, _jsonOptions);
                if (r is not null) results.Add(r);
            }
            catch { /* skip malformed files */ }
        }
        return results.OrderByDescending(r => r.CreatedAt).ToList();
    }

    public async Task<List<DataRecord>> GetAllRecordsAsync()
    {
        var recordsRoot = Path.Combine(_dataRoot, "records");
        if (!Directory.Exists(recordsRoot)) return [];

        var results = new List<DataRecord>();
        foreach (var structureDir in Directory.GetDirectories(recordsRoot))
        {
            foreach (var file in Directory.GetFiles(structureDir, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var r = JsonSerializer.Deserialize<DataRecord>(json, _jsonOptions);
                    if (r is not null) results.Add(r);
                }
                catch { /* skip malformed files */ }
            }
        }
        return results;
    }

    public async Task<List<DataRecord>> GetChildRecordsAsync(string parentRecordId)
    {
        var all = await GetAllRecordsAsync();
        return all.Where(r => r.ParentRecordId == parentRecordId).ToList();
    }

    public async Task<DataRecord?> GetRecordAsync(string structureId, string recordId)
    {
        var path = RecordPath(structureId, recordId);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<DataRecord>(json, _jsonOptions);
    }

    public async Task SaveRecordAsync(DataRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;
        var dir = RecordsDir(record.StructureId);
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(record, _jsonOptions);
        await File.WriteAllTextAsync(RecordPath(record.StructureId, record.Id), json);
    }

    public async Task DeleteRecordAsync(string structureId, string recordId)
    {
        var path = RecordPath(structureId, recordId);
        if (File.Exists(path)) File.Delete(path);
        await Task.CompletedTask;
    }

    public async Task ImportRecordsAsync(string structureId, List<Dictionary<string, string>> rows)
    {
        foreach (var row in rows)
        {
            var record = new DataRecord
            {
                StructureId = structureId,
                Fields = row.ToDictionary(k => k.Key, v => (string?)v.Value)
            };
            await SaveRecordAsync(record);
        }
    }
}
