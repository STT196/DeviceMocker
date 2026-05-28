using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.RfidReader
{
    public class RfidReaderDevice : IDeviceModule
    {
        private readonly InputRouter _router;
        public string Id => "rfid-reader";
        public string Name => "RFID / NFC Reader";
        public DeviceType DeviceType => DeviceType.RfidReader;

        public RfidReaderDevice(InputRouter router) => _router = router;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
