using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using DeviceMocker.Services;

namespace DeviceMocker.Devices.Scanner
{
    public class SampleBarcode
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ScannerViewModel : ViewModelBase
    {
        private string _payload = string.Empty;
        private string _prefix = string.Empty;
        private string _selectedSuffix = "Enter";
        private int _delayPerCharacter = 10;
        private string _statusMessage = string.Empty;
        private string _countdownText = string.Empty;
        private bool _isSending;
        private bool _isCountingDown;
        private bool _isBatchMode;
        private int _batchCount = 5;
        private int _batchIntervalMs = 1000;
        private string _selectedBarcodeType = "EAN-13";
        private CancellationTokenSource? _cts;
        private readonly Random _rng = new();

        public string Payload
        {
            get => _payload;
            set => SetProperty(ref _payload, value);
        }

        public string Prefix
        {
            get => _prefix;
            set => SetProperty(ref _prefix, value);
        }

        public string SelectedSuffix
        {
            get => _selectedSuffix;
            set => SetProperty(ref _selectedSuffix, value);
        }

        public int DelayPerCharacter
        {
            get => _delayPerCharacter;
            set => SetProperty(ref _delayPerCharacter, value);
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

        public bool IsCountingDown
        {
            get => _isCountingDown;
            set => SetProperty(ref _isCountingDown, value);
        }

        public bool IsBatchMode
        {
            get => _isBatchMode;
            set => SetProperty(ref _isBatchMode, value);
        }

        public int BatchCount
        {
            get => _batchCount;
            set => SetProperty(ref _batchCount, value);
        }

        public int BatchIntervalMs
        {
            get => _batchIntervalMs;
            set => SetProperty(ref _batchIntervalMs, value);
        }

        public string SelectedBarcodeType
        {
            get => _selectedBarcodeType;
            set => SetProperty(ref _selectedBarcodeType, value);
        }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public string[] BarcodeTypeOptions { get; } = { "EAN-13", "UPC-A", "Code128", "QR Code", "EAN-8", "Custom" };

        public ObservableCollection<SampleBarcode> SampleBarcodes { get; } = new();
        public ObservableCollection<string> ScanHistory { get; } = new();

        public ICommand SendNowCommand { get; }
        public ICommand SendAfterCountdownCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ScanSampleCommand { get; }
        public ICommand GenerateRandomCommand { get; }
        public ICommand RescanHistoryCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand BatchScanCommand { get; }

        public ScannerViewModel()
        {
            SendNowCommand = new AsyncRelayCommand(SendNowAsync, () => !IsSending && !string.IsNullOrEmpty(Payload));
            SendAfterCountdownCommand = new AsyncRelayCommand(SendAfterCountdownAsync, () => !IsSending && !string.IsNullOrEmpty(Payload));
            CancelCommand = new RelayCommand(Cancel, () => IsSending || IsCountingDown);
            ScanSampleCommand = new AsyncRelayCommand(ScanSampleAsync);
            GenerateRandomCommand = new RelayCommand(GenerateRandom);
            RescanHistoryCommand = new AsyncRelayCommand(RescanHistoryAsync);
            ClearHistoryCommand = new RelayCommand(() => ScanHistory.Clear());
            BatchScanCommand = new AsyncRelayCommand(BatchScanAsync, () => !IsSending);

            LoadSampleBarcodes();
        }

        private void LoadSampleBarcodes()
        {
            SampleBarcodes.Clear();

            // EAN-13 samples (real product-like barcodes)
            SampleBarcodes.Add(new SampleBarcode { Label = "Coca-Cola 330ml", Value = "5449000000996", Type = "EAN-13" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Pepsi 500ml", Value = "4060800001015", Type = "EAN-13" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Milk 1L", Value = "8850999220017", Type = "EAN-13" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Bread Loaf", Value = "4801234567890", Type = "EAN-13" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Rice 5kg", Value = "8991102720106", Type = "EAN-13" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Shampoo 200ml", Value = "8999999529154", Type = "EAN-13" });

            // UPC-A samples
            SampleBarcodes.Add(new SampleBarcode { Label = "Chips Bag", Value = "012345678905", Type = "UPC-A" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Juice Box", Value = "036000291452", Type = "UPC-A" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Candy Bar", Value = "040000422327", Type = "UPC-A" });

            // Code128 / SKU samples
            SampleBarcodes.Add(new SampleBarcode { Label = "SKU Laptop", Value = "SKU-LAPTOP-001", Type = "Code128" });
            SampleBarcodes.Add(new SampleBarcode { Label = "SKU Mouse", Value = "SKU-MOUSE-042", Type = "Code128" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Warehouse Bin", Value = "WH-A3-SHELF-07", Type = "Code128" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Invoice #", Value = "INV-2026-00451", Type = "Code128" });

            // QR Code samples
            SampleBarcodes.Add(new SampleBarcode { Label = "Website URL", Value = "https://example.com/product/123", Type = "QR Code" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Payment QR", Value = "PAY:AMT=99.50;REF=TXN20260528001", Type = "QR Code" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Ticket QR", Value = "TICKET:EVT=CONF2026;SEAT=A12;ID=8847", Type = "QR Code" });
            SampleBarcodes.Add(new SampleBarcode { Label = "WiFi QR", Value = "WIFI:T:WPA;S:OfficeNet;P:pass1234;;", Type = "QR Code" });

            // EAN-8 samples
            SampleBarcodes.Add(new SampleBarcode { Label = "Gum Pack", Value = "96385074", Type = "EAN-8" });
            SampleBarcodes.Add(new SampleBarcode { Label = "Small Candy", Value = "55123457", Type = "EAN-8" });
        }

        private void GenerateRandom()
        {
            Payload = SelectedBarcodeType switch
            {
                "EAN-13" => GenerateEAN13(),
                "UPC-A" => GenerateUPCA(),
                "EAN-8" => GenerateEAN8(),
                "Code128" => $"SKU-{_rng.Next(10000, 99999):D5}-{(char)('A' + _rng.Next(0, 26))}{_rng.Next(0, 9)}",
                "QR Code" => $"https://example.com/item/{_rng.Next(100000, 999999)}",
                _ => $"{_rng.Next(100000000, 999999999)}"
            };
            StatusMessage = $"Generated random {SelectedBarcodeType}: {Payload}";
        }

        private string GenerateEAN13()
        {
            var digits = new int[13];
            for (int i = 0; i < 12; i++)
                digits[i] = _rng.Next(0, 10);

            // Calculate check digit
            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += digits[i] * (i % 2 == 0 ? 1 : 3);
            digits[12] = (10 - (sum % 10)) % 10;

            return string.Join("", digits);
        }

        private string GenerateUPCA()
        {
            var digits = new int[12];
            for (int i = 0; i < 11; i++)
                digits[i] = _rng.Next(0, 10);

            int sum = 0;
            for (int i = 0; i < 11; i++)
                sum += digits[i] * (i % 2 == 0 ? 3 : 1);
            digits[11] = (10 - (sum % 10)) % 10;

            return string.Join("", digits);
        }

        private string GenerateEAN8()
        {
            var digits = new int[8];
            for (int i = 0; i < 7; i++)
                digits[i] = _rng.Next(0, 10);

            int sum = 0;
            for (int i = 0; i < 7; i++)
                sum += digits[i] * (i % 2 == 0 ? 3 : 1);
            digits[7] = (10 - (sum % 10)) % 10;

            return string.Join("", digits);
        }

        private DeviceAction CreateAction(string? payloadOverride = null)
        {
            return new DeviceAction
            {
                DeviceId = "scanner",
                DeviceName = "Barcode / QR Scanner",
                DeviceType = DeviceType.Scanner,
                ActionType = ActionType.Text,
                OutputChannelType = OutputChannelType.Keyboard,
                Payload = payloadOverride ?? Payload,
                Prefix = Prefix,
                Suffix = SelectedSuffix,
                DelayPerCharacterMs = DelayPerCharacter
            };
        }

        private void AddToHistory(string barcode)
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                ScanHistory.Remove(barcode);
                ScanHistory.Insert(0, barcode);
                if (ScanHistory.Count > 20)
                    ScanHistory.RemoveAt(ScanHistory.Count - 1);
            });
        }

        private async Task ScanSampleAsync(object? parameter)
        {
            if (parameter is not SampleBarcode sample) return;

            Payload = sample.Value;
            await SendAfterCountdownAsync();
        }

        private async Task RescanHistoryAsync(object? parameter)
        {
            if (parameter is not string barcode) return;

            Payload = barcode;
            await SendAfterCountdownAsync();
        }

        private async Task BatchScanAsync()
        {
            if (BatchCount <= 0)
            {
                StatusMessage = "Batch count must be greater than 0.";
                return;
            }

            IsSending = true;
            IsCountingDown = true;
            _cts = new CancellationTokenSource();

            try
            {
                // Countdown first
                var countdown = new CountdownSendService();
                var seconds = ServiceLocator.Settings.Current.CountdownSeconds;
                if (seconds <= 0) seconds = 3;

                countdown.CountdownTick += (remaining) =>
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        CountdownText = $"Batch starts in {remaining}...";
                        StatusMessage = $"Switch to target window! Batch scan starts in {remaining}s...";
                    });
                };

                countdown.CountdownCompleted += () =>
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        IsCountingDown = false;
                    });
                };

                await countdown.StartCountdownAsync(seconds, async () =>
                {
                    for (int i = 0; i < BatchCount; i++)
                    {
                        _cts!.Token.ThrowIfCancellationRequested();

                        // Generate a random barcode for each scan
                        var barcode = GenerateEAN13();

                        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                        {
                            CountdownText = $"Scanning {i + 1} of {BatchCount}...";
                            Payload = barcode;
                        });

                        var action = CreateAction(barcode);
                        await ServiceLocator.ScannerDevice.SendAsync(action, _cts.Token);
                        AddToHistory(barcode);

                        if (i < BatchCount - 1 && BatchIntervalMs > 0)
                            await Task.Delay(BatchIntervalMs, _cts.Token);
                    }

                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = $"Batch complete! Sent {BatchCount} barcodes.";
                        CountdownText = string.Empty;
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Batch cancelled.";
                CountdownText = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                CountdownText = string.Empty;
            }
            finally
            {
                IsSending = false;
                IsCountingDown = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task SendNowAsync()
        {
            if (string.IsNullOrEmpty(Payload))
            {
                StatusMessage = "Please enter a payload value.";
                return;
            }

            IsSending = true;
            StatusMessage = "Sending...";

            try
            {
                _cts = new CancellationTokenSource();
                var action = CreateAction();
                var result = await ServiceLocator.ScannerDevice.SendAsync(action, _cts.Token);
                StatusMessage = result.Success ? $"Sent: {Payload}" : $"Error: {result.ErrorMessage}";
                if (result.Success) AddToHistory(Payload);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsSending = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task SendAfterCountdownAsync()
        {
            if (string.IsNullOrEmpty(Payload))
            {
                StatusMessage = "Please enter a payload value.";
                return;
            }

            IsCountingDown = true;
            IsSending = true;
            _cts = new CancellationTokenSource();

            try
            {
                var countdown = new CountdownSendService();
                var seconds = ServiceLocator.Settings.Current.CountdownSeconds;
                if (seconds <= 0) seconds = 3;

                countdown.CountdownTick += (remaining) =>
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        CountdownText = $"Sending in {remaining}...";
                        StatusMessage = $"Switch to target window! Sending in {remaining}s...";
                    });
                };

                countdown.CountdownCompleted += () =>
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        CountdownText = "Scanning...";
                        IsCountingDown = false;
                    });
                };

                await countdown.StartCountdownAsync(seconds, async () =>
                {
                    var action = CreateAction();
                    var result = await ServiceLocator.ScannerDevice.SendAsync(action, _cts!.Token);
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        StatusMessage = result.Success ? $"Scanned: {Payload}" : $"Error: {result.ErrorMessage}";
                        CountdownText = string.Empty;
                        if (result.Success) AddToHistory(Payload);
                    });
                }, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Cancelled.";
                CountdownText = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                CountdownText = string.Empty;
            }
            finally
            {
                IsSending = false;
                IsCountingDown = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void Cancel()
        {
            _cts?.Cancel();
            StatusMessage = "Cancelling...";
        }
    }
}
