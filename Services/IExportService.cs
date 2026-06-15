using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public enum ExportFormat { Jsonl, Csv, FineTuningMessages, FineTuningSimple }

public interface IExportService
{
    string ExportFolder { get; }
    string BuildContent(DataStructure structure, List<DataRecord> records, ExportFormat format, string? promptField, string? completionField);
    Task<string> SaveExportAsync(string content, string baseName, ExportFormat format);
}
