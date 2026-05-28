using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.MagstripeReader
{
    public class SampleSwipe
    {
        public string Label { get; set; } = string.Empty;
        public string TrackData { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
    }

    public class MagstripeReaderViewModel : ViewModelBase
    {
        private string _trackData = string.Empty;
        private string _selectedTrack = "Track 1+2";
        private string _selectedSuffix = "Enter";
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isSending;
        private readonly Random _rng = new();
        private CancellationTokenSource? _cts;

        public string TrackData { get => _trackData; set => SetProperty(ref _trackData, value); }
        public string SelectedTrack { get => _selectedTrack; set => SetProperty(ref _selectedTrack, value); }
        public string SelectedSuffix { get => _selectedSuffix; set => SetProperty(ref _selectedSuffix, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public string CountdownText { get => _countdownText; set => SetProperty(ref _countdownText, value); }
        public bool IsSending { get => _isSending; set => SetProperty(ref _isSending, value); }

        public string[] TrackOptions { get; } = { "Track 1", "Track 2", "Track 1+2", "Track 3" };
        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public ObservableCollection<SampleSwipe> SampleSwipes { get; } = new();
        public ObservableCollection<string> SwipeHistory { get; } = new();

        public ICommand SendNowCommand { get; }
        public ICommand SendAfterCountdownCommand { get; }
        public ICommand GenerateRandomCommand { get; }
        public ICommand SwipeSampleCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearHistoryCommand { get; }

        public MagstripeReaderViewModel()
        {
            SendNowCommand = new AsyncRelayCommand(SendNowAsync, () => !IsSending && !string.IsNullOrEmpty(TrackData));
            SendAfterCountdownCommand = new AsyncRelayCommand(SendAfterCountdownAsync, () => !IsSending && !string.IsNullOrEmpty(TrackData));
            GenerateRandomCommand = new RelayCommand(GenerateRandom);
            SwipeSampleCommand = new AsyncRelayCommand(SwipeSampleAsync);
            CancelCommand = new RelayCommand(() => _cts?.Cancel(), () => IsSending);
            ClearHistoryCommand = new RelayCommand(() => SwipeHistory.Clear());
            LoadSamples();
        }

        private void LoadSamples()
        {
            SampleSwipes.Add(new SampleSwipe { Label = "Visa Test Card", TrackData = "%B4111111111111111^DOE/JOHN^2512101000000000000000000000000?;4111111111111111=25121010000000000000?", CardType = "Visa" });
            SampleSwipes.Add(new SampleSwipe { Label = "MasterCard Test", TrackData = "%B5500000000000004^SMITH/JANE^2612101000000000000000000000000?;5500000000000004=26121010000000000000?", CardType = "MasterCard" });
            SampleSwipes.Add(new SampleSwipe { Label = "Amex Test Card", TrackData = "%B378282246310005^WILLIAMS/BOB^2712101000000000000000000000000?;378282246310005=27121010000000000000?", CardType = "Amex" });
            SampleSwipes.Add(new SampleSwipe { Label = "Gift Card #1", TrackData = "%B6011000000000004^GIFTCARD/STORE^2812101000000000000000000000000?;6011000000000004=28121010000000000000?", CardType = "Gift Card" });
            SampleSwipes.Add(new SampleSwipe { Label = "Loyalty Card", TrackData = "%B9999000012345678^MEMBER/LOYALTY^3012101000000000000000000000000?;9999000012345678=30121010000000000000?", CardType = "Loyalty" });
            SampleSwipes.Add(new SampleSwipe { Label = "Employee ID Card", TrackData = ";1234567890=9912?", CardType = "ID Card" });
        }

        private void GenerateRandom()
        {
            var num = $"4{_rng.Next(100, 999)}{_rng.Next(1000, 9999)}{_rng.Next(1000, 9999)}{_rng.Next(1000, 9999)}";
            var exp = $"{_rng.Next(25, 30)}{_rng.Next(1, 13):D2}";
            TrackData = SelectedTrack switch
            {
                "Track 1" => $"%B{num}^TEST/CARD^{exp}101000000000000000000000000000?",
                "Track 2" => $";{num}={exp}10100000000000?",
                "Track 3" => $";{num}={exp}?",
                _ => $"%B{num}^TEST/CARD^{exp}101000000000000000000000000000?;{num}={exp}10100000000000?"
            };
            StatusMessage = $"Generated {SelectedTrack} data.";
        }

        private DeviceAction CreateAction()
        {
            return new DeviceAction
            {
                DeviceId = "magstripe-reader",
                DeviceName = "Magstripe Card Reader",
                DeviceType = DeviceType.MagstripeReader,
                ActionType = ActionType.Text,
                OutputChannelType = OutputChannelType.Keyboard,
                Payload = TrackData,
                Suffix = SelectedSuffix,
                DelayPerCharacterMs = 3
            };
        }

        private void AddHistory(string data) { var short_ = data.Length > 40 ? data[..40] + "..." : data; SwipeHistory.Remove(short_); SwipeHistory.Insert(0, short_); if (SwipeHistory.Count > 10) SwipeHistory.RemoveAt(SwipeHistory.Count - 1); }

        private async Task SwipeSampleAsync(object? p)
        {
            if (p is not SampleSwipe s) return;
            TrackData = s.TrackData;
            await SendAfterCountdownAsync();
        }

        private async Task SendNowAsync()
        {
            if (string.IsNullOrEmpty(TrackData)) return;
            IsSending = true;
            try
            {
                _cts = new CancellationTokenSource();
                var result = await ServiceLocator.DeviceManager.GetDevice("magstripe-reader")!.SendAsync(CreateAction(), _cts.Token);
                StatusMessage = result.Success ? "Card swiped!" : $"Error: {result.ErrorMessage}";
                if (result.Success) AddHistory(TrackData);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }

        private async Task SendAfterCountdownAsync()
        {
            if (string.IsNullOrEmpty(TrackData)) return;
            IsSending = true; _cts = new CancellationTokenSource();
            try
            {
                var cd = new CountdownSendService();
                var secs = ServiceLocator.Settings.Current.CountdownSeconds; if (secs <= 0) secs = 3;
                cd.CountdownTick += r => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = $"Swiping in {r}...");
                cd.CountdownCompleted += () => System.Windows.Application.Current?.Dispatcher?.Invoke(() => CountdownText = "Swiping...");
                await cd.StartCountdownAsync(secs, async () =>
                {
                    var result = await ServiceLocator.DeviceManager.GetDevice("magstripe-reader")!.SendAsync(CreateAction(), _cts!.Token);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = result.Success ? "Card swiped!" : $"Error: {result.ErrorMessage}";
                        CountdownText = string.Empty;
                        if (result.Success) AddHistory(TrackData);
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException) { StatusMessage = "Cancelled."; CountdownText = string.Empty; }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; CountdownText = string.Empty; }
            finally { IsSending = false; _cts?.Dispose(); _cts = null; }
        }
    }
}
