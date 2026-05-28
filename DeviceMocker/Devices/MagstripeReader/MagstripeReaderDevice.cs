using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.MagstripeReader
{
    public class MagstripeReaderDevice : IDeviceModule
    {
        private readonly InputRouter _router;
        public string Id => "magstripe-reader";
        public string Name => "Magstripe Card Reader";
        public DeviceType DeviceType => DeviceType.MagstripeReader;

        public MagstripeReaderDevice(InputRouter router) => _router = router;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
