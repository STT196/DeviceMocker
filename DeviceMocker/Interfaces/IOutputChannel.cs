using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Interfaces
{
    public interface IOutputChannel
    {
        string Id { get; }
        string Name { get; }
        OutputChannelType ChannelType { get; }
        Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default);
    }
}
