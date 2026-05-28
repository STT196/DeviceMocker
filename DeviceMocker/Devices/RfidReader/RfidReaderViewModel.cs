using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.RfidReader
{
    public class SampleCard
    {
        public string Label { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class RfidReaderViewModel : ViewModelBase
    {
        private string _cardUid = string.Empty;
        private string _selectedFormat = "Hex (uppercase)";
        private string _selectedSuffix = "Enter";
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isSending;
        private readonly Random _rng = new();
        private CancellationTokenSource? _cts;

        public string CardUid { get => _cardUid; set => SetProperty(ref _cardUid, value); }
        public string SelectedFormat { get => _selectedFormat; set => SetProperty(ref _selectedFormat, value); }
        public string SelectedSuffix { get => _selectedSuffix; set => SetProperty(ref _selectedSuffix, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string CountdownText { get => _countdownText; set => SetProperty(ref _countdownText, value); }
        public bool IsSending { get => _isSending; set => SetProperty(ref _isSending, value); }

        public string[] FormatOptions { get; } = { "Hex (uppercase)", "Hex (lowercase)", "Decimal", "With colons", "With spaces" };
        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public ObservableCollection<SampleCard> SampleCards { get; } = new();
        public ObservableCollection<string> TapHistory { get; } = new();

        public ICommand SendNowCommand { get; }
        public ICommand SendAfterCountdownCommand { get; }
        public ICommand GenerateRandomCommand { get; }
        public ICommand TapSampleCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public RfidReaderViewModel()
        {
            SendNowCommand = new AsyncRelayCommand(SendNowAsync, () => !IsSending && !string.IsNullOrEmpty(CardUid));
            SendAfterCountdownCommand = new AsyncRelayCommand(SendAfterCountdownAsync, () => !IsSending && !string.IsNullOrEmpty(CardUid));
            GenerateRandomCommand = new RelayCommand(GenerateRandom);
            TapSampleCommand = new AsyncRelayCommand(TapSampleAsync);
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsSending);
            ClearHistoryCommand = new RelayCommand(() => TapHistory.Clear());
            LoadSamples();
        }

        private void LoadSamples()
        {
            SampleCards.Add(new SampleCard { Label = "Employee Badge #1", Uid = "A1B2C3D4", Type = "MIFARE Classic" });
            SampleCards.Add(new SampleCard { Label = "Employee Badge #2", Uid = "E5F6A7B8", Type = "MIFARE Classic" });
            SampleCards.Add(new SampleCard { Label = "Access Card Gold", Uid = "04A3B2C1D5E6F7", Type = "MIFARE Ultralight" });
            SampleCards.Add(new SampleCard { Label = "Visitor Pass", Uid = "1234ABCD", Type = "NTAG213" });
            SampleCards.Add(new SampleCard { Label = "Parking Tag", Uid = "DEADBEEF", Type = "MIFARE Classic" });
            SampleCards.Add(new SampleCard { Label = "Student ID", Uid = "0011223344556677", Type = "MIFARE DESFire" });
            SampleCards.Add(new SampleCard { Label = "Library Card", Uid = "CAFEBABE", Type = "NTAG216" });
            SampleCards.Add(new SampleCard { Label = "Gym Membership", Uid = "F0E1D2C3", Type = "MIFARE Classic" });
        }

        private void GenerateRandom()
        {
            var bytes = new byte[4];
            _rng.NextBytes(bytes);
            CardUid = BitConverter.ToString(bytes).Replace("-", "");
            StatusMessage = $"Generated: {CardUid}";
        }

        private string FormatUid(string uid)
        {
            return SelectedFormat switch
            {
                "Hex (lowercase)" => uid.ToLower(),
                "Decimal" => long.TryParse(uid, System.Globalization.NumberStyles.HexNumber, null, out var dec) ? dec.ToString() : uid,
                "With colons" => string.Join(":", SplitPairs(uid.ToUpper())),
                "With spaces" => string.Join(" ", SplitPairs(uid.ToUpper())),
                _ => uid.ToUpper()
            };
        }

        private static string[] SplitPairs(string s)
        {
            var list = new System.Collections.Generic.List<string>();
            for (int i = 0; i < s.Length; i += 2)
                list.Add(s.Substring(i, Math.Min(2, s.Length - i)));
            return list.ToArray();
        }

        private DeviceAction CreateAction()
        {
            return new DeviceAction
            {
                DeviceId = "rfid-reader",
                DeviceName = "RFID / NFC Reader",
                DeviceType = DeviceType.RfidReader,
                ActionType = ActionType.Text,
                OutputChannelType = OutputChannelType.Keyboard,
                Payload = FormatUid(CardUid),
                Suffix = SelectedSuffix,
                DelayPerCharacterMs = 5
            };
        }

        private void AddHistory(string uid) { TapHistory.Remove(uid); TapHistory.Insert(0, uid); if (TapHistory.Count > 15) TapHistory.RemoveAt(TapHistory.Count - 1); }

        private async Task TapSampleAsync(object? p)
        {
            if (p is not SampleCard card) return;
            CardUid = card.Uid;
            await SendAfterCountdownAsync();
        }

        private async Task SendNowAsync()
        {
            if (string.IsNullOrEmpty(CardUid)) return;
            IsSending = true;
            try
            {
                _cts = new CancellationTokenSource();
                var result = await ServiceLocator.DeviceManager.GetDevice("rfid-reader")!.SendAsync(CreateAction(), _cts.Token);
                StatusMessage = result.Success ? $"Tapped: {FormatUid(CardUid)}" : $"Error: {result.ErrorMessage}";
                if (result.Success) AddHistory(CardUid);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }

        private async Task SendAfterCountdownAsync()
        {
            if (string.IsNullOrEmpty(CardUid)) return;
            IsSending = true; _cts = new CancellationTokenSource();
            try
            {
                var cd = new CountdownSendService();
                var secs = ServiceLocator.Settings.Current.CountdownSeconds; if (secs <= 0) secs = 3;
                cd.CountdownTick += r => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = $"Tapping in {r}...");
                cd.CountdownCompleted += () => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = "Tapping...");
                await cd.StartCountdownAsync(secs, async () =>
                {
                    var result = await ServiceLocator.DeviceManager.GetDevice("rfid-reader")!.SendAsync(CreateAction(), _cts!.Token);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = result.Success ? $"Tapped: {FormatUid(CardUid)}" : $"Error: {result.ErrorMessage}";
                        CountdownText = string.Empty;
                        if (result.Success) AddHistory(CardUid);
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException) { StatusMessage = "Cancelled."; CountdownText = string.Empty; }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; CountdownText = string.Empty; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }
    }
}
