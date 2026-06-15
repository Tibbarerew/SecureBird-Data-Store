namespace SecureBird_Data_Store.Models;

public class DataRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StructureId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string?> Fields { get; set; } = [];
}
