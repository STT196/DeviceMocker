using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DeviceMocker.Helpers;

namespace DeviceMocker.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ViewModelBase _currentPage = null!;
        private string _currentPageTitle = "Dashboard";

        public ViewModelBase CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        public DashboardViewModel DashboardVm { get; }
        public DevicesViewModel DevicesVm { get; }
        public EmulatorsViewModel EmulatorsVm { get; }
        public ProfilesViewModel ProfilesVm { get; }
        public LogsViewModel LogsVm { get; }
        public SettingsViewModel SettingsVm { get; }

        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }
        public ICommand LaunchHardwareTestCommand { get; }

        private ViewModelBase? _previousPage;
        private string? _previousPageTitle;

        public MainViewModel()
        {
            DashboardVm = new DashboardViewModel(this);
            DevicesVm = new DevicesViewModel(this);
            EmulatorsVm = new EmulatorsViewModel();
            ProfilesVm = new ProfilesViewModel();
            LogsVm = new LogsViewModel();
            SettingsVm = new SettingsViewModel();

            NavigateCommand = new RelayCommand(Navigate);
            GoBackCommand = new RelayCommand(GoBack, () => _previousPage != null);
            LaunchHardwareTestCommand = new RelayCommand(LaunchHardwareTestApp);

            CurrentPage = DashboardVm;
        }

        private void Navigate(object? parameter)
        {
            if (parameter is not string page) return;

            _previousPage = CurrentPage;
            _previousPageTitle = CurrentPageTitle;

            switch (page)
            {
                case "Dashboard":
                    CurrentPage = DashboardVm;
                    CurrentPageTitle = "Dashboard";
                    break;
                case "Devices":
                    CurrentPage = DevicesVm;
                    CurrentPageTitle = "Devices";
                    break;
                case "Profiles":
                    CurrentPage = ProfilesVm;
                    CurrentPageTitle = "Profiles";
                    ProfilesVm.LoadProfilesCommand.Execute(null);
                    break;
                case "Emulators":
                    CurrentPage = EmulatorsVm;
                    CurrentPageTitle = "Emulators";
                    EmulatorsVm.RefreshProfilesCommand.Execute(null);
                    break;
                case "Logs":
                    CurrentPage = LogsVm;
                    CurrentPageTitle = "Logs";
                    LogsVm.RefreshLogs();
                    break;
                case "Settings":
                    CurrentPage = SettingsVm;
                    CurrentPageTitle = "Settings";
                    break;
                default:
                    // Handle device-specific navigation
                    if (parameter is ViewModelBase vm)
                    {
                        CurrentPage = vm;
                    }
                    break;
            }
        }

        public void NavigateToDevice(ViewModelBase deviceVm, string title)
        {
            _previousPage = CurrentPage;
            _previousPageTitle = CurrentPageTitle;
            CurrentPage = deviceVm;
            CurrentPageTitle = title;
        }

        private void GoBack()
        {
            if (_previousPage != null)
            {
                CurrentPage = _previousPage;
                CurrentPageTitle = _previousPageTitle ?? "Dashboard";
                _previousPage = null;
                _previousPageTitle = null;
            }
        }

        private void LaunchHardwareTestApp()
        {
            try
            {
                var executablePath = ResolveHardwareTestExecutablePath();
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    MessageBox.Show(
                        "POS Hardware Test App was not found.\n\nBuild or publish DeviceMocker with the bundled hardware test app, or build the sample project first.",
                        "Hardware Test App Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to launch POS Hardware Test App.\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string? ResolveHardwareTestExecutablePath()
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new List<string>
            {
                Path.Combine(baseDirectory, "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "PosHardwareTestApp", "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "Tools", "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "Tools", "PosHardwareTestApp", "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "..", "..", "..", "Samples", "PosHardwareTestApp", "bin", "Debug", "net8.0-windows", "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "..", "..", "..", "Samples", "PosHardwareTestApp", "bin", "Release", "net8.0-windows", "PosHardwareTestApp.exe"),
                Path.Combine(baseDirectory, "..", "..", "..", "Samples", "PosHardwareTestApp", "bin", "Release", "net8.0-windows", "win-x64", "publish", "PosHardwareTestApp.exe")
            };

            return candidates
                .Select(Path.GetFullPath)
                .FirstOrDefault(File.Exists);
        }
    }
}
