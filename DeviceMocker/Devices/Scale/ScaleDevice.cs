using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.Scale
{
    public class ScaleDevice : IDeviceModule
    {
        private readonly InputRouter _router;
        public string Id => "scale";
        public string Name => "Weighing Scale";
        public DeviceType DeviceType => DeviceType.Scale;

        public ScaleDevice(InputRouter router) => _router = router;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
