using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public interface IExcelImportService
{
    Task<ExcelImportResult> ReadExcelAsync(string filePath);
    DataStructure SuggestStructure(ExcelSheetPreview sheet, string structureName);
    Task<List<DataRecord>> ImportSheetAsRecordsAsync(string filePath, string sheetName, string structureId, List<string> headers);
}
