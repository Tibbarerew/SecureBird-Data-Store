namespace SecureBird_Data_Store.Models;

public class ExcelSheetPreview
{
    public string SheetName { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];
    public List<Dictionary<string, string>> SampleRows { get; set; } = [];
    public int TotalRows { get; set; }
}

public class ExcelImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ExcelSheetPreview> Sheets { get; set; } = [];
}
