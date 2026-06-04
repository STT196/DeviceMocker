using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.CashDrawer
{
    public class CashDrawerViewModel : ViewModelBase
    {
        private const int MaxHistoryItems = 12;

        private bool _isDrawerOpen;
        private string _selectedOutput = "Keyboard";
        private string _statusMessage = "Ready. Use Open Drawer or No Sale to trigger the drawer.";
        private string _countdownText = string.Empty;
        private bool _isSending;
        private CancellationTokenSource? _cts;

        public bool IsDrawerOpen
        {
            get => _isDrawerOpen;
            set
            {
                if (SetProperty(ref _isDrawerOpen, value))
                {
                    OnPropertyChanged(nameof(DrawerState));
                    OnPropertyChanged(nameof(DrawerStateDescription));
                }
            }
        }

        public string SelectedOutput
        {
            get => _selectedOutput;
            set => SetProperty(ref _selectedOutput, value);
        }

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

        public string DrawerState => IsDrawerOpen ? "Open" : "Closed";
        public string DrawerStateDescription => IsDrawerOpen
            ? "The simulated drawer is currently open."
            : "The simulated drawer is currently closed.";

        public string[] OutputOptions { get; } =
        {
            "Keyboard",
            "Serial",
            "TCP Client",
            "UDP",
            "HTTP Webhook"
        };

        public ObservableCollection<string> History { get; } = new();

        public ICommand OpenDrawerCommand { get; }
        public ICommand SendAfterCountdownCommand { get; }
        public ICommand SendStatusCommand { get; }
        public ICommand QuickCommandCommand { get; }
        public ICommand MarkClosedCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public CashDrawerViewModel()
        {
            IsDrawerOpen = ServiceLocator.CashDrawerEmulator.IsDrawerOpen;
            OpenDrawerCommand = new AsyncRelayCommand(() => SendCommandNowAsync("OPEN_DRAWER"), () => !IsSending);
            SendAfterCountdownCommand = new AsyncRelayCommand(() => SendWithCountdownAsync("OPEN_DRAWER"), () => !IsSending);
            SendStatusCommand = new AsyncRelayCommand(() => SendCommandNowAsync("STATUS"), () => !IsSending);
            QuickCommandCommand = new AsyncRelayCommand(SendQuickCommandAsync, _ => !IsSending);
            MarkClosedCommand = new RelayCommand(MarkClosed);
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsSending);
            ClearHistoryCommand = new RelayCommand(() => History.Clear());

            AddHistory("READY -> Drawer simulator loaded");
        }

        private async Task SendQuickCommandAsync(object? parameter)
        {
            if (parameter is not string payload || string.IsNullOrWhiteSpace(payload))
                return;

            await SendCommandNowAsync(payload);
        }

        private async Task SendCommandNowAsync(string payload)
        {
            IsSending = true;

            try
            {
                _cts = new CancellationTokenSource();
                await SendPayloadAsync(payload, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
                CountdownText = string.Empty;
                AddHistory("CANCELLED");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                AddHistory($"ERROR -> {ex.Message}");
            }
            finally
            {
                IsSending = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task SendWithCountdownAsync(string payload)
        {
            IsSending = true;
            _cts = new CancellationTokenSource();

            try
            {
                var countdown = new CountdownSendService();
                var seconds = ServiceLocator.Settings.Current.CountdownSeconds;
                if (seconds <= 0) seconds = 3;

                countdown.CountdownTick += remaining =>
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        CountdownText = $"Sending in {remaining}...");

                countdown.CountdownCompleted += () =>
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        CountdownText = "Sending...");

                await countdown.StartCountdownAsync(seconds, async () =>
                {
                    await SendPayloadAsync(payload, _cts.Token);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = string.Empty);
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
                CountdownText = string.Empty;
                AddHistory("CANCELLED");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                CountdownText = string.Empty;
                AddHistory($"ERROR -> {ex.Message}");
            }
            finally
            {
                IsSending = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task SendPayloadAsync(string payload, CancellationToken cancellationToken)
        {
            var result = await ServiceLocator.DeviceManager.GetDevice("cash-drawer")!.SendAsync(CreateAction(payload), cancellationToken);

            if (!result.Success)
            {
                StatusMessage = $"Error: {result.ErrorMessage}";
                AddHistory($"FAIL {payload}");
                return;
            }

            if (payload == "OPEN_DRAWER" || payload == "NO_SALE")
            {
                ServiceLocator.CashDrawerEmulator.OpenManual($"Manual simulator sent: {payload}.");
                IsDrawerOpen = ServiceLocator.CashDrawerEmulator.IsDrawerOpen;
            }

            StatusMessage = payload switch
            {
                "STATUS" => $"Status sent. Drawer is {DrawerState}.",
                "OPEN_DRAWER" => "Drawer opened.",
                "NO_SALE" => "No-sale trigger sent. Drawer opened.",
                _ => $"Sent: {payload}"
            };

            AddHistory($"{payload} -> {DrawerState}");
        }

        private DeviceAction CreateAction(string payload)
        {
            return new DeviceAction
            {
                DeviceId = "cash-drawer",
                DeviceName = "Cash Drawer",
                DeviceType = DeviceType.CashDrawer,
                ActionType = ActionType.Text,
                OutputChannelType = MapOutputChannel(),
                Payload = payload,
                Suffix = payload == "STATUS" ? string.Empty : "Enter",
                DelayPerCharacterMs = 5
            };
        }

        private OutputChannelType MapOutputChannel()
        {
            return SelectedOutput switch
            {
                "Serial" => OutputChannelType.Serial,
                "TCP Client" => OutputChannelType.TcpClient,
                "UDP" => OutputChannelType.Udp,
                "HTTP Webhook" => OutputChannelType.HttpWebhook,
                _ => OutputChannelType.Keyboard
            };
        }

        private void MarkClosed()
        {
            ServiceLocator.CashDrawerEmulator.MarkClosed("Cash drawer simulator marked the drawer closed locally.");
            IsDrawerOpen = ServiceLocator.CashDrawerEmulator.IsDrawerOpen;
            StatusMessage = "Drawer marked closed locally.";
            AddHistory("LOCAL MARK_CLOSED -> Closed");
        }

        private void AddHistory(string entry)
        {
            var line = $"{DateTime.Now:HH:mm:ss}  {entry}";
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                History.Insert(0, line);
                while (History.Count > MaxHistoryItems)
                    History.RemoveAt(History.Count - 1);
            });
        }
    }
}
