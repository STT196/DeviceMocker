using System.Threading;
using System.Threading.Tasks;
using DeviceMocker.Core;
using DeviceMocker.Interfaces;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.Scanner
{
    public class ScannerDevice : IDeviceModule
    {
        private readonly InputRouter _router;

        public string Id => "scanner";
        public string Name => "Barcode / QR Scanner";
        public DeviceType DeviceType => DeviceType.Scanner;

        public ScannerDevice(InputRouter router)
        {
            _router = router;
        }

        public async Task<OutputResult> SendAsync(DeviceAction action, CancellationToken cancellationToken = default)
        {
            action.DeviceId = Id;
            action.DeviceName = Name;
            action.DeviceType = DeviceType;
            action.ActionType = ActionType.Text;
            return await _router.RouteAsync(action, cancellationToken);
        }
    }
}
