using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public interface IJsonDataService
{
    Task<List<DataStructure>> GetStructuresAsync();
    Task<DataStructure?> GetStructureAsync(string id);
    Task SaveStructureAsync(DataStructure structure);
    Task DeleteStructureAsync(string id);

    Task<List<DataRecord>> GetRecordsAsync(string structureId);
    Task<List<DataRecord>> GetAllRecordsAsync();
    Task<List<DataRecord>> GetChildRecordsAsync(string parentRecordId);
    Task<DataRecord?> GetRecordAsync(string structureId, string recordId);
    Task SaveRecordAsync(DataRecord record);
    Task DeleteRecordAsync(string structureId, string recordId);

    Task ImportRecordsAsync(string structureId, List<Dictionary<string, string>> rows);
}
