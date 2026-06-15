using System.Text.Json;
using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public class ProfileService : IProfileService
{
    private readonly string _profilesDir;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ProfileService()
    {
        _profilesDir = Path.Combine(FileSystem.AppDataDirectory, "SecureBirdData", "profiles");
        Directory.CreateDirectory(_profilesDir);
    }

    private string ProfilePath(string id) => Path.Combine(_profilesDir, $"{id}.json");

    public async Task<List<ImportProfile>> GetProfilesAsync()
    {
        var results = new List<ImportProfile>();
        foreach (var file in Directory.GetFiles(_profilesDir, "*.json"))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var p = JsonSerializer.Deserialize<ImportProfile>(json, _jsonOptions);
                if (p is not null) results.Add(p);
            }
            catch { /* skip malformed */ }
        }
        return results.OrderBy(p => p.Name).ToList();
    }

    public async Task<ImportProfile?> GetProfileAsync(string id)
    {
        var path = ProfilePath(id);
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<ImportProfile>(json, _jsonOptions);
    }

    public async Task SaveProfileAsync(ImportProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(profile, _jsonOptions);
        await File.WriteAllTextAsync(ProfilePath(profile.Id), json);
    }

    public async Task DeleteProfileAsync(string id)
    {
        var path = ProfilePath(id);
        if (File.Exists(path)) File.Delete(path);
        await Task.CompletedTask;
    }
}
