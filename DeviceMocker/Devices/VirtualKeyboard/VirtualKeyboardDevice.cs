using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.VirtualKeyboard
{
    public class VirtualKeyboardDevice : IDeviceModule
    {
        private readonly InputRouter _router;

        public string Id => "virtual-keyboard";
        public string Name => "Virtual Keyboard";
        public DeviceType DeviceType => DeviceType.VirtualKeyboard;

        public VirtualKeyboardDevice(InputRouter router)
        {
            _router = router;
        }

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
