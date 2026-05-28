using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Interfaces
{
    public interface IDeviceModule
    {
        string Id { get; }
        string Name { get; }
        DeviceType DeviceType { get; }
        Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default);
    }
}
