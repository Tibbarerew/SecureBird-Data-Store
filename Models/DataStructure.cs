namespace SecureBird_Data_Store.Models;

public class DataStructure
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ParentStructureId { get; set; }
    public string? DefaultParentRecordId { get; set; }
    public string? PromptFieldName { get; set; }
    public string? CompletionFieldName { get; set; }
    public List<FieldDefinition> Fields { get; set; } = [];
}
