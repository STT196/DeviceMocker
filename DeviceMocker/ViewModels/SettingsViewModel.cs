using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private int _defaultDelay = 10;
        private string _defaultSuffix = "Enter";
        private string _theme = "Dark";
        private int _countdownSeconds = 3;
        private string _statusMessage = string.Empty;

        public int DefaultDelay { get => _defaultDelay; set => SetProperty(ref _defaultDelay, value); }
        public string DefaultSuffix { get => _defaultSuffix; set => SetProperty(ref _defaultSuffix, value); }
        public string Theme { get => _theme; set { if (SetProperty(ref _theme, value)) ApplyTheme(value); } }
        public int CountdownSeconds { get => _countdownSeconds; set => SetProperty(ref _countdownSeconds, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public string[] SuffixOptions { get; } = { "None", "Enter", "Tab", "CR", "LF", "CRLF" };
        public string[] ThemeOptions { get; } = { "Dark", "Light" };

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }

        public SettingsViewModel()
        {
            SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
            LoadCommand = new AsyncRelayCommand(LoadSettingsAsync);
            _ = LoadSettingsAsync();
        }

        private async Task LoadSettingsAsync()
        {
            var settings = await ServiceLocator.Settings.LoadAsync();
            DefaultDelay = settings.DefaultDelayPerCharacterMs;
            DefaultSuffix = settings.DefaultSuffix;
            _theme = settings.Theme; OnPropertyChanged(nameof(Theme));
            CountdownSeconds = settings.CountdownSeconds;
        }

        private async Task SaveSettingsAsync()
        {
            var settings = new AppSettings
            {
                DefaultDelayPerCharacterMs = DefaultDelay,
                DefaultSuffix = DefaultSuffix,
                Theme = Theme,
                CountdownSeconds = CountdownSeconds
            };
            await ServiceLocator.Settings.SaveAsync(settings);
            StatusMessage = "Settings saved.";
        }

        private void ApplyTheme(string theme)
        {
            var res = Application.Current.Resources;
            if (theme == "Light")
            {
                res["PrimaryColor"] = (Color)ColorConverter.ConvertFromString("#F5F5F8");
                res["SecondaryColor"] = (Color)ColorConverter.ConvertFromString("#EAEAEF");
                res["SurfaceColor"] = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                res["TextPrimaryColor"] = (Color)ColorConverter.ConvertFromString("#1A1A2E");
                res["TextSecondaryColor"] = (Color)ColorConverter.ConvertFromString("#6B6B80");
                res["BorderColor"] = (Color)ColorConverter.ConvertFromString("#D0D0DD");
            }
            else
            {
                res["PrimaryColor"] = (Color)ColorConverter.ConvertFromString("#1E1E2E");
                res["SecondaryColor"] = (Color)ColorConverter.ConvertFromString("#2D2D44");
                res["SurfaceColor"] = (Color)ColorConverter.ConvertFromString("#252540");
                res["TextPrimaryColor"] = (Color)ColorConverter.ConvertFromString("#F0F0F5");
                res["TextSecondaryColor"] = (Color)ColorConverter.ConvertFromString("#A0A0B8");
                res["BorderColor"] = (Color)ColorConverter.ConvertFromString("#3D3D5C");
            }
        }
    }
}
