using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.SerialDevice
{
    public class SerialDeviceSimulator : IDeviceModule
    {
        private readonly InputRouter _router;

        public string Id => "serial-device";
        public string Name => "Serial Text Sender";
        public DeviceType DeviceType => DeviceType.SerialDevice;

        public SerialDeviceSimulator(InputRouter router)
        {
            _router = router;
        }

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            action.OutputChannelType = OutputChannelType.Serial;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
