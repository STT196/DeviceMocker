using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.Devices.PosPanel
{
    public class PosPanelViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;
        private bool _isSending;
        private string _countdownText = string.Empty;
        private bool _sendWithCountdown;

        public ObservableCollection<PosButton> Buttons { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSending
        {
            get => _isSending;
            set => SetProperty(ref _isSending, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        public bool SendWithCountdown
        {
            get => _sendWithCountdown;
            set => SetProperty(ref _sendWithCountdown, value);
        }

        public ICommand SendButtonCommand { get; }

        public PosPanelViewModel()
        {
            SendButtonCommand = new AsyncRelayCommand(SendButtonAsync);
            LoadDefaultButtons();
        }

        private void LoadDefaultButtons()
        {
            Buttons.Clear();
            Buttons.Add(new PosButton { Id = "cash", Label = "Cash", ActionType = ActionType.Key, Value = "F1" });
            Buttons.Add(new PosButton { Id = "card", Label = "Card", ActionType = ActionType.Key, Value = "F2" });
            Buttons.Add(new PosButton { Id = "discount", Label = "Discount", ActionType = ActionType.Shortcut, Value = "Ctrl+D" });
            Buttons.Add(new PosButton { Id = "void", Label = "Void", ActionType = ActionType.Key, Value = "F4" });
            Buttons.Add(new PosButton { Id = "search", Label = "Search", ActionType = ActionType.Shortcut, Value = "Ctrl+F" });
            Buttons.Add(new PosButton { Id = "submit", Label = "Submit", ActionType = ActionType.Key, Value = "Enter" });
            Buttons.Add(new PosButton { Id = "cancel", Label = "Cancel", ActionType = ActionType.Key, Value = "Escape" });
            Buttons.Add(new PosButton { Id = "f5", Label = "F5", ActionType = ActionType.Key, Value = "F5" });
            Buttons.Add(new PosButton { Id = "f6", Label = "F6", ActionType = ActionType.Key, Value = "F6" });
            Buttons.Add(new PosButton { Id = "tab", Label = "Tab", ActionType = ActionType.Key, Value = "Tab" });
            Buttons.Add(new PosButton { Id = "backspace", Label = "Backspace", ActionType = ActionType.Key, Value = "Backspace" });
            Buttons.Add(new PosButton { Id = "copy", Label = "Copy", ActionType = ActionType.Shortcut, Value = "Ctrl+C" });
            Buttons.Add(new PosButton { Id = "paste", Label = "Paste", ActionType = ActionType.Shortcut, Value = "Ctrl+V" });
            Buttons.Add(new PosButton { Id = "selectall", Label = "Select All", ActionType = ActionType.Shortcut, Value = "Ctrl+A" });
            Buttons.Add(new PosButton { Id = "undo", Label = "Undo", ActionType = ActionType.Shortcut, Value = "Ctrl+Z" });
            Buttons.Add(new PosButton { Id = "save", Label = "Save", ActionType = ActionType.Shortcut, Value = "Ctrl+S" });
        }

        private async Task SendButtonAsync(object? parameter)
        {
            if (parameter is not PosButton button) return;

            IsSending = true;

            try
            {
                var action = new DeviceAction
                {
                    DeviceId = "custom-panel",
                    DeviceName = "Custom Button Panel",
                    DeviceType = DeviceType.CustomButtonPanel,
                    ActionType = button.ActionType,
                    OutputChannelType = OutputChannelType.Keyboard,
                    Payload = button.Value,
                    Prefix = button.Prefix,
                    Suffix = button.Suffix,
                    DelayPerCharacterMs = button.DelayMs
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
                        var result = await ServiceLocator.PosPanelDevice.SendAsync(action);
                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            StatusMessage = result.Success ? $"Sent: {button.Label}" : $"Error: {result.ErrorMessage}";
                            CountdownText = string.Empty;
                        });
                    });
                }
                else
                {
                    var result = await ServiceLocator.PosPanelDevice.SendAsync(action);
                    StatusMessage = result.Success ? $"Sent: {button.Label}" : $"Error: {result.ErrorMessage}";
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
