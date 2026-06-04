using System;
using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Models;

namespace DeviceMocker.Interfaces
{
    public interface IEmulatorModule
    {
        string Id { get; }
        string Name { get; }
        string ReceiptPreview { get; }
        bool IsDrawerOpen { get; }

        event Action<EmulatorSessionLog>? LogProduced;
        event Action? StateChanged;

        void Start(EmulatorProfileSettings settings);
        void Stop();
        Task HandleBytesAsync(byte[] bytes, CancellationToken cancellationToken = default);
    }
}
