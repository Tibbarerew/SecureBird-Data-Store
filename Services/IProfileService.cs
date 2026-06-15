using SecureBird_Data_Store.Models;

namespace SecureBird_Data_Store.Services;

public interface IProfileService
{
    Task<List<ImportProfile>> GetProfilesAsync();
    Task<ImportProfile?> GetProfileAsync(string id);
    Task SaveProfileAsync(ImportProfile profile);
    Task DeleteProfileAsync(string id);
}
