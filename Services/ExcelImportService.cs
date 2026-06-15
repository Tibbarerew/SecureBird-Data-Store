using ClosedXML.Excel;
using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public class ExcelImportService : IExcelImportService
{
    private const int SampleRowCount = 5;

    public Task<ExcelImportResult> ReadExcelAsync(string filePath)
    {
        try
        {
            using var workbook = new XLWorkbook(filePath);
            var result = new ExcelImportResult { Success = true };

            foreach (var worksheet in workbook.Worksheets)
            {
                var preview = BuildSheetPreview(worksheet);
                result.Sheets.Add(preview);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ExcelImportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    private static ExcelSheetPreview BuildSheetPreview(IXLWorksheet sheet)
    {
        var preview = new ExcelSheetPreview { SheetName = sheet.Name };
        var usedRange = sheet.RangeUsed();
        if (usedRange is null) return preview;

        var rows = usedRange.RowsUsed().ToList();
        if (rows.Count == 0) return preview;

        // First row = headers
        preview.Headers = rows[0].Cells().Select(c => c.GetValue<string>().Trim()).ToList();
        preview.TotalRows = rows.Count - 1;

        for (int i = 1; i < Math.Min(rows.Count, SampleRowCount + 1); i++)
        {
            var row = new Dictionary<string, string>();
            var cells = rows[i].Cells().ToList();
            for (int j = 0; j < preview.Headers.Count; j++)
            {
                var value = j < cells.Count ? cells[j].GetValue<string>() : string.Empty;
                row[preview.Headers[j]] = value;
            }
            preview.SampleRows.Add(row);
        }

        return preview;
    }

    public DataStructure SuggestStructure(ExcelSheetPreview sheet, string structureName)
    {
        var structure = new DataStructure
        {
            Name = structureName,
            Description = $"Imported from Excel sheet: {sheet.SheetName}"
        };

        foreach (var header in sheet.Headers)
        {
            var field = new FieldDefinition
            {
                Name = header,
                Type = InferFieldType(header, sheet.SampleRows.Select(r =>
                    r.TryGetValue(header, out var v) ? v : string.Empty))
            };
            structure.Fields.Add(field);
        }

        return structure;
    }

    private static FieldType InferFieldType(string header, IEnumerable<string> samples)
    {
        var lowerHeader = header.ToLowerInvariant();

        if (lowerHeader.Contains("email")) return FieldType.Email;
        if (lowerHeader.Contains("url") || lowerHeader.Contains("website") || lowerHeader.Contains("link"))
            return FieldType.Url;
        if (lowerHeader.Contains("date") || lowerHeader.Contains("time"))
            return FieldType.Date;
        if (lowerHeader.Contains("active") || lowerHeader.Contains("enabled") || lowerHeader.Contains("flag"))
            return FieldType.Boolean;

        var nonEmpty = samples.Where(s => !string.IsNullOrWhiteSpace(s)).Take(10).ToList();
        if (nonEmpty.Count == 0) return FieldType.Text;

        if (nonEmpty.All(s => decimal.TryParse(s, out _))) return FieldType.Number;
        if (nonEmpty.All(s => bool.TryParse(s, out _) || s is "0" or "1" or "yes" or "no"))
            return FieldType.Boolean;
        if (nonEmpty.All(s => DateTime.TryParse(s, out _))) return FieldType.Date;

        return FieldType.Text;
    }

    public Task<List<DataRecord>> ImportSheetAsRecordsAsync(
        string filePath, string sheetName, string structureId, List<string> headers)
    {
        using var workbook = new XLWorkbook(filePath);
        var sheet = workbook.Worksheet(sheetName);
        var usedRange = sheet.RangeUsed();
        if (usedRange is null) return Task.FromResult(new List<DataRecord>());

        var rows = usedRange.RowsUsed().Skip(1).ToList(); // skip header row
        var records = new List<DataRecord>();

        foreach (var row in rows)
        {
            var cells = row.Cells().ToList();
            var fields = new Dictionary<string, string?>();
            for (int j = 0; j < headers.Count; j++)
            {
                var value = j < cells.Count ? cells[j].GetValue<string>() : string.Empty;
                fields[headers[j]] = value;
            }
            records.Add(new DataRecord { StructureId = structureId, Fields = fields });
        }

        return Task.FromResult(records);
    }
}
