using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.SequenceBuilder
{
    public class SequenceBuilderDevice : IDeviceModule
    {
        private readonly InputRouter _router;
        public string Id => "sequence-builder";
        public string Name => "Test Sequence";
        public DeviceType DeviceType => DeviceType.CustomScripted;

        public SequenceBuilderDevice(InputRouter router) => _router = router;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
