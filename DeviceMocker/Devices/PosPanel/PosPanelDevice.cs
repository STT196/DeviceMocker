using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.PosPanel
{
    public class PosPanelDevice : IDeviceModule
    {
        private readonly InputRouter _router;

        public string Id => "custom-panel";
        public string Name => "Custom Button Panel";
        public DeviceType DeviceType => DeviceType.CustomButtonPanel;

        public PosPanelDevice(InputRouter router)
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
