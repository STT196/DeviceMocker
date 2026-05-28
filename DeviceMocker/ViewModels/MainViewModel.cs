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
        public ProfilesViewModel ProfilesVm { get; }
        public LogsViewModel LogsVm { get; }
        public SettingsViewModel SettingsVm { get; }

        public ICommand NavigateCommand { get; }
        public ICommand GoBackCommand { get; }

        private ViewModelBase? _previousPage;
        private string? _previousPageTitle;

        public MainViewModel()
        {
            DashboardVm = new DashboardViewModel(this);
            DevicesVm = new DevicesViewModel(this);
            ProfilesVm = new ProfilesViewModel();
            LogsVm = new LogsViewModel();
            SettingsVm = new SettingsViewModel();

            NavigateCommand = new RelayCommand(Navigate);
            GoBackCommand = new RelayCommand(GoBack, () => _previousPage != null);

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
    }
}
