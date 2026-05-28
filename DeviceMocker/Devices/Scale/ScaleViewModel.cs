using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.Scale
{
    public class ScaleViewModel : ViewModelBase
    {
        private double _weight = 0.0;
        private string _unit = "kg";
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isSending;
        private string _selectedOutput = "Keyboard";
        private string _selectedFormat = "Standard";
        private readonly Random _rng = new();
        private CancellationTokenSource? _cts;

        public double Weight { get => _weight; set => SetProperty(ref _weight, value); }
        public string Unit { get => _unit; set => SetProperty(ref _unit, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string CountdownText { get => _countdownText; set => SetProperty(ref _countdownText, value); }
        public bool IsSending { get => _isSending; set => SetProperty(ref _isSending, value); }
        public string SelectedOutput { get => _selectedOutput; set => SetProperty(ref _selectedOutput, value); }
        public string SelectedFormat { get => _selectedFormat; set => SetProperty(ref _selectedFormat, value); }

        public string[] UnitOptions { get; } = { "kg", "lb", "g", "oz" };
        public string[] OutputOptions { get; } = { "Keyboard", "Serial" };
        public string[] FormatOptions { get; } = { "Standard", "Simple", "CSV", "Raw Number" };

        public ICommand SendWeightCommand { get; }
        public ICommand SendAfterCountdownCommand { get; }
        public ICommand TareCommand { get; }
        public ICommand RandomWeightCommand { get; }
        public ICommand PresetCommand { get; }
        public ICommand CancelCommand { get; }

        public ScaleViewModel()
        {
            SendWeightCommand = new AsyncRelayCommand(SendWeightAsync, () => !IsSending);
            SendAfterCountdownCommand = new AsyncRelayCommand(SendAfterCountdownAsync, () => !IsSending);
            TareCommand = new RelayCommand(() => { Weight = 0; StatusMessage = "Tared to zero."; });
            RandomWeightCommand = new RelayCommand(() => { Weight = Math.Round(_rng.NextDouble() * 500 + 0.1, 2); StatusMessage = $"Random: {Weight} {Unit}"; });
            PresetCommand = new RelayCommand(p => { if (p is string s && double.TryParse(s, out var w)) { Weight = w; StatusMessage = $"Set: {w} {Unit}"; } });
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsSending);
        }

        private string FormatWeight()
        {
            return SelectedFormat switch
            {
                "Standard" => $"ST,GS,+  {Weight,8:F2}  {Unit}",
                "Simple" => $"{Weight:F2} {Unit}",
                "CSV" => $"{Weight:F2},{Unit},{DateTime.Now:HH:mm:ss}",
                "Raw Number" => $"{Weight:F2}",
                _ => $"{Weight:F2} {Unit}"
            };
        }

        private DeviceAction CreateAction()
        {
            return new DeviceAction
            {
                DeviceId = "scale",
                DeviceName = "Weighing Scale",
                DeviceType = DeviceType.Scale,
                ActionType = ActionType.Text,
                OutputChannelType = SelectedOutput == "Serial" ? OutputChannelType.Serial : OutputChannelType.Keyboard,
                Payload = FormatWeight(),
                Suffix = "Enter",
                DelayPerCharacterMs = 5
            };
        }

        private async Task SendWeightAsync()
        {
            IsSending = true;
            try
            {
                _cts = new CancellationTokenSource();
                var action = CreateAction();
                var result = await ServiceLocator.DeviceManager.GetDevice("scale")!.SendAsync(action, _cts.Token);
                StatusMessage = result.Success ? $"Sent: {FormatWeight()}" : $"Error: {result.ErrorMessage}";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }

        private async Task SendAfterCountdownAsync()
        {
            IsSending = true;
            _cts = new CancellationTokenSource();
            try
            {
                var cd = new CountdownSendService();
                var secs = ServiceLocator.Settings.Current.CountdownSeconds;
                if (secs <= 0) secs = 3;
                cd.CountdownTick += r => System.Windows.Application.Current?.Dispatcher?.Invoke(() => { CountdownText = $"Sending in {r}..."; });
                cd.CountdownCompleted += () => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = "Sending...");
                await cd.StartCountdownAsync(secs, async () =>
                {
                    var action = CreateAction();
                    var result = await ServiceLocator.DeviceManager.GetDevice("scale")!.SendAsync(action, _cts!.Token);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = result.Success ? $"Sent: {FormatWeight()}" : $"Error: {result.ErrorMessage}";
                        CountdownText = string.Empty;
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException) { StatusMessage = "Cancelled."; CountdownText = string.Empty; }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; CountdownText = string.Empty; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }
    }
}
