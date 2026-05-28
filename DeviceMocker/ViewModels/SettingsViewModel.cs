using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private int _defaultDelay = 10;
        private string _defaultSuffix = "Enter";
        private OutputChannelType _defaultOutputChannel = OutputChannelType.Keyboard;
        private string _theme = "Dark";
        private int _countdownSeconds = 3;
        private bool _logToFile;
        private int _maxLogEntries = 1000;
        private string _statusMessage = string.Empty;
        private bool _isError;
        private bool _isLoaded;

        private readonly DispatcherTimer _statusTimer;

        public int DefaultDelay { get => _defaultDelay; set { if (SetProperty(ref _defaultDelay, value)) MarkDirty(); } }
        public string DefaultSuffix { get => _defaultSuffix; set { if (SetProperty(ref _defaultSuffix, value)) MarkDirty(); } }
        public OutputChannelType DefaultOutputChannel { get => _defaultOutputChannel; set { if (SetProperty(ref _defaultOutputChannel, value)) MarkDirty(); } }
        public string Theme { get => _theme; set { if (SetProperty(ref _theme, value)) { ApplyTheme(value); MarkDirty(); } } }
        public int CountdownSeconds { get => _countdownSeconds; set { if (SetProperty(ref _countdownSeconds, value)) MarkDirty(); } }
        public bool LogToFile { get => _logToFile; set { if (SetProperty(ref _logToFile, value)) MarkDirty(); } }
        public int MaxLogEntries { get => _maxLogEntries; set { if (SetProperty(ref _maxLogEntries, value)) MarkDirty(); } }

        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsError { get => _isError; set => SetProperty(ref _isError, value); }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public string[] ThemeOptions { get; } = { "Dark", "Light" };
        public Array OutputChannelOptions { get; } = Enum.GetValues(typeof(OutputChannelType));

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
            LoadCommand = new AsyncRelayCommand(LoadSettingsAsync);
            ResetDefaultsCommand = new RelayCommand(ResetDefaults);

            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _statusTimer.Tick += (_, _) => { _statusTimer.Stop(); StatusMessage = string.Empty; IsError = false; };

            _ = LoadSettingsAsync();
        }

        private void MarkDirty()
        {
            if (!_isLoaded) return;
            // Light cue that there are unsaved changes
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage == "Settings saved.")
            {
                StatusMessage = "Unsaved changes...";
                IsError = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                var settings = await ServiceLocator.Settings.LoadAsync();
                _defaultDelay = settings.DefaultDelayPerCharacterMs;
                _defaultSuffix = string.IsNullOrEmpty(settings.DefaultSuffix) ? "None" : settings.DefaultSuffix;
                _defaultOutputChannel = settings.DefaultOutputChannel;
                _theme = settings.Theme;
                _countdownSeconds = settings.CountdownSeconds;
                _logToFile = settings.LogToFile;
                _maxLogEntries = settings.MaxLogEntries;

                OnPropertyChanged(nameof(DefaultDelay));
                OnPropertyChanged(nameof(DefaultSuffix));
                OnPropertyChanged(nameof(DefaultOutputChannel));
                OnPropertyChanged(nameof(Theme));
                OnPropertyChanged(nameof(CountdownSeconds));
                OnPropertyChanged(nameof(LogToFile));
                OnPropertyChanged(nameof(MaxLogEntries));

                ApplyTheme(_theme);
                _isLoaded = true;
            }
            catch (Exception ex) { ShowStatus($"Load error: {ex.Message}", true); }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var settings = new AppSettings
                {
                    DefaultDelayPerCharacterMs = Math.Max(0, DefaultDelay),
                    DefaultSuffix = DefaultSuffix == "None" ? string.Empty : DefaultSuffix,
                    DefaultOutputChannel = DefaultOutputChannel,
                    Theme = Theme,
                    CountdownSeconds = Math.Max(0, CountdownSeconds),
                    LogToFile = LogToFile,
                    MaxLogEntries = Math.Max(1, MaxLogEntries)
                };
                await ServiceLocator.Settings.SaveAsync(settings);
                ShowStatus("Settings saved.", false);
            }
            catch (Exception ex) { ShowStatus($"Save error: {ex.Message}", true); }
        }

        private void ResetDefaults()
        {
            var d = new AppSettings();
            DefaultDelay = d.DefaultDelayPerCharacterMs;
            DefaultSuffix = string.IsNullOrEmpty(d.DefaultSuffix) ? "None" : d.DefaultSuffix;
            DefaultOutputChannel = d.DefaultOutputChannel;
            Theme = d.Theme;
            CountdownSeconds = d.CountdownSeconds;
            LogToFile = d.LogToFile;
            MaxLogEntries = d.MaxLogEntries;
            ShowStatus("Defaults restored. Click Save to apply.", false);
        }

        private void ShowStatus(string msg, bool isError)
        {
            StatusMessage = msg;
            IsError = isError;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void ApplyTheme(string theme)
        {
            var res = Application.Current?.Resources;
            if (res == null) return;

            (string p, string s, string surf, string tp, string ts, string b) palette = theme == "Light"
                ? ("#F5F5F8", "#EAEAEF", "#FFFFFF", "#1A1A2E", "#6B6B80", "#D0D0DD")
                : ("#1E1E2E", "#2D2D44", "#252540", "#F0F0F5", "#A0A0B8", "#3D3D5C");

            void SetColorAndBrush(string colorKey, string brushKey, string hex)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex)!;
                res[colorKey] = color;
                if (res[brushKey] is SolidColorBrush brush && !brush.IsFrozen)
                {
                    brush.Color = color;
                }
                else
                {
                    res[brushKey] = new SolidColorBrush(color);
                }
            }

            SetColorAndBrush("PrimaryColor", "PrimaryBrush", palette.p);
            SetColorAndBrush("SecondaryColor", "SecondaryBrush", palette.s);
            SetColorAndBrush("SurfaceColor", "SurfaceBrush", palette.surf);
            SetColorAndBrush("TextPrimaryColor", "TextPrimaryBrush", palette.tp);
            SetColorAndBrush("TextSecondaryColor", "TextSecondaryBrush", palette.ts);
            SetColorAndBrush("BorderColor", "BorderBrush", palette.b);
        }
    }
}
