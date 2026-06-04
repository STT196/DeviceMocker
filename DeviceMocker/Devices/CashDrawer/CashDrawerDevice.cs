using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.CashDrawer
{
    public class CashDrawerDevice : IDeviceModule
    {
        private readonly InputRouter _router;

        public string Id => "cash-drawer";
        public string Name => "Cash Drawer";
        public DeviceType DeviceType => DeviceType.CashDrawer;

        public CashDrawerDevice(InputRouter router) => _router = router;

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
