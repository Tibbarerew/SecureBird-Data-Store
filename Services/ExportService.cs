using System.Text;
using System.Text.Json;
using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public class ExportService : IExportService
{
    public string ExportFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "SecureBird Exports");

    public string BuildContent(DataStructure structure, List<DataRecord> records, ExportFormat format, string? promptField, string? completionField)
        => format switch
        {
            ExportFormat.Csv => BuildCsv(structure, records),
            ExportFormat.FineTuningMessages => BuildFineTuningMessages(records, promptField, completionField),
            ExportFormat.FineTuningSimple => BuildFineTuningSimple(records, promptField, completionField),
            _ => BuildJsonl(records)
        };

    private static string BuildJsonl(List<DataRecord> records)
    {
        var sb = new StringBuilder();
        foreach (var r in records)
        {
            var obj = new Dictionary<string, object?> { ["id"] = r.Id, ["created_at"] = r.CreatedAt };
            foreach (var (k, v) in r.Fields) obj[k] = v;
            sb.AppendLine(JsonSerializer.Serialize(obj));
        }
        return sb.ToString();
    }

    private static string BuildCsv(DataStructure structure, List<DataRecord> records)
    {
        var headers = structure.Fields.Select(f => f.Name).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", new[] { "id", "created_at" }.Concat(headers).Select(CsvQuote)));
        foreach (var r in records)
        {
            var values = new List<string> { CsvQuote(r.Id), CsvQuote(r.CreatedAt.ToString("o")) };
            foreach (var h in headers)
                values.Add(CsvQuote(r.Fields.TryGetValue(h, out var v) ? v ?? "" : ""));
            sb.AppendLine(string.Join(",", values));
        }
        return sb.ToString();
    }

    private static string BuildFineTuningMessages(List<DataRecord> records, string? promptField, string? completionField)
    {
        var sb = new StringBuilder();
        foreach (var r in records)
        {
            var prompt = GetField(r, promptField);
            var completion = GetField(r, completionField);
            var obj = new
            {
                messages = new[]
                {
                    new { role = "user", content = prompt },
                    new { role = "assistant", content = completion }
                }
            };
            sb.AppendLine(JsonSerializer.Serialize(obj));
        }
        return sb.ToString();
    }

    private static string BuildFineTuningSimple(List<DataRecord> records, string? promptField, string? completionField)
    {
        var sb = new StringBuilder();
        foreach (var r in records)
            sb.AppendLine(JsonSerializer.Serialize(new
            {
                prompt = GetField(r, promptField),
                completion = GetField(r, completionField)
            }));
        return sb.ToString();
    }

    private static string GetField(DataRecord r, string? field)
        => field is not null && r.Fields.TryGetValue(field, out var v) ? v ?? "" : "";

    private static string CsvQuote(string? value)
    {
        value ??= "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<string> SaveExportAsync(string content, string baseName, ExportFormat format)
    {
        Directory.CreateDirectory(ExportFolder);
        var ext = format == ExportFormat.Csv ? "csv" : "jsonl";
        var safe = string.Concat(baseName.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var fileName = $"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
        var path = Path.Combine(ExportFolder, fileName);
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        return path;
    }
}
