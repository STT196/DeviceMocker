using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.VirtualKeyboard
{
    public class VirtualKeyboardViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isSending;
        private bool _sendWithCountdown;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        public bool IsSending
        {
            get => _isSending;
            set => SetProperty(ref _isSending, value);
        }

        public bool SendWithCountdown
        {
            get => _sendWithCountdown;
            set => SetProperty(ref _sendWithCountdown, value);
        }

        public ICommand SendKeyCommand { get; }

        public VirtualKeyboardViewModel()
        {
            SendKeyCommand = new AsyncRelayCommand(SendKeyAsync);
        }

        private async Task SendKeyAsync(object? parameter)
        {
            if (parameter is not string keyValue) return;

            IsSending = true;

            try
            {
                ActionType actionType;
                if (keyValue.Contains('+'))
                    actionType = ActionType.Shortcut;
                else if (keyValue.Length > 1) // Special key names like "Enter", "Tab", etc.
                    actionType = ActionType.Key;
                else
                    actionType = ActionType.Text;

                var action = new DeviceAction
                {
                    DeviceId = "virtual-keyboard",
                    DeviceName = "Virtual Keyboard",
                    DeviceType = DeviceType.VirtualKeyboard,
                    ActionType = actionType,
                    OutputChannelType = OutputChannelType.Keyboard,
                    Payload = keyValue,
                    DelayPerCharacterMs = 0
                };

                if (SendWithCountdown)
                {
                    var countdown = new Services.CountdownSendService();
                    var seconds = ServiceLocator.Settings.Current.CountdownSeconds;
                    if (seconds <= 0) seconds = 3;

                    countdown.CountdownTick += (remaining) =>
                    {
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            CountdownText = $"Sending in {remaining}...";
                        });
                    };

                    await countdown.StartCountdownAsync(seconds, async () =>
                    {
                        var result = await ServiceLocator.VirtualKeyboardDevice.SendAsync(action);
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            StatusMessage = result.Success ? $"Sent: {keyValue}" : $"Error: {result.ErrorMessage}";
                            CountdownText = string.Empty;
                        });
                    });
                }
                else
                {
                    var result = await ServiceLocator.VirtualKeyboardDevice.SendAsync(action);
                    StatusMessage = result.Success ? $"Sent: {keyValue}" : $"Error: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSending = false;
            }
        }
    }
}
