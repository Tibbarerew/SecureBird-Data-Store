namespace SecureBird_Data_Store.Models;

public class SheetProfileEntry
{
    public string SheetName { get; set; } = string.Empty;
    public bool Include { get; set; } = true;
    public bool CreateNew { get; set; } = true;
    public string StructureName { get; set; } = string.Empty;
    public string? StructureId { get; set; }
    public string? ParentStructureId { get; set; }
    public string? ParentRecordId { get; set; }
}

public class ImportProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? SourceFileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<SheetProfileEntry> Entries { get; set; } = [];

    public string Summary => $"{Entries.Count(e => e.Include)} sheets · {Entries.Count(e => e.Include && e.ParentRecordId != null)} with parent rules";
}
