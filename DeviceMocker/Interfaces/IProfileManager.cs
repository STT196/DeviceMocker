using System.Collections.Generic;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Interfaces
{
    public interface IProfileManager
    {
        Task<List<DeviceProfile>> GetAllProfilesAsync();
        Task<DeviceProfile?> GetProfileAsync(string id);
        Task SaveProfileAsync(DeviceProfile profile);
        Task DeleteProfileAsync(string id);
        Task<DeviceProfile> DuplicateProfileAsync(string id);
        Task ExportProfileAsync(string id, string exportPath);
        Task<DeviceProfile> ImportProfileAsync(string importPath);
    }
}
