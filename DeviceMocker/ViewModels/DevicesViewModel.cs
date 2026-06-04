using DeviceMocker.Devices.CashDrawer;
using System.Windows.Input;
using DeviceMocker.Devices.MagstripeReader;
using DeviceMocker.Devices.PosPanel;
using DeviceMocker.Devices.RfidReader;
using DeviceMocker.Devices.Scale;
using DeviceMocker.Devices.Scanner;
using DeviceMocker.Devices.SequenceBuilder;
using DeviceMocker.Devices.SerialDevice;
using DeviceMocker.Devices.VirtualKeyboard;
using DeviceMocker.Helpers;

namespace DeviceMocker.ViewModels
{
    public class DevicesViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;
        public ICommand OpenDeviceCommand { get; }

        public DevicesViewModel(MainViewModel mainVm)
        {
            _mainVm = mainVm;
            OpenDeviceCommand = new RelayCommand(OpenDevice);
        }

        private void OpenDevice(object? parameter)
        {
            if (parameter is not string deviceId) return;

            switch (deviceId)
            {
                case "scanner":
                    _mainVm.NavigateToDevice(new ScannerViewModel(), "Barcode / QR Scanner");
                    break;
                case "virtual-keyboard":
                    _mainVm.NavigateToDevice(new VirtualKeyboardViewModel(), "Virtual Keyboard");
                    break;
                case "custom-panel":
                    _mainVm.NavigateToDevice(new PosPanelViewModel(), "Custom Button Panel");
                    break;
                case "serial-device":
                    _mainVm.NavigateToDevice(new SerialDeviceViewModel(), "Serial Text Sender");
                    break;
                case "scale":
                    _mainVm.NavigateToDevice(new ScaleViewModel(), "Weighing Scale");
                    break;
                case "rfid-reader":
                    _mainVm.NavigateToDevice(new RfidReaderViewModel(), "RFID / NFC Reader");
                    break;
                case "magstripe-reader":
                    _mainVm.NavigateToDevice(new MagstripeReaderViewModel(), "Magstripe Card Reader");
                    break;
                case "sequence-builder":
                    _mainVm.NavigateToDevice(new SequenceBuilderViewModel(), "Test Sequence Builder");
                    break;
                case "cash-drawer":
                    _mainVm.NavigateToDevice(new CashDrawerViewModel(), "Cash Drawer");
                    break;
            }
        }
    }
}
