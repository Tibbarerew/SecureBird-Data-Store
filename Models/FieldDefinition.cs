namespace SecureBird_Data_Store.Models;

public enum FieldType
{
    Text,
    Number,
    Boolean,
    Date,
    Email,
    Url
}

public class FieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public FieldType Type { get; set; } = FieldType.Text;
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}
