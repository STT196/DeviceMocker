using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;

namespace DeviceMocker.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;

        public string AppName => AppConstants.AppName;
        public string AppVersion => $"v{AppConstants.AppVersion}";
        public string AppDescription => AppConstants.AppDescription;
        public string AppAuthor => $"by {AppConstants.AppAuthor}";
        public string AppGitHub => AppConstants.AppGitHub;

        public ObservableCollection<DeviceLog> RecentLogs { get; } = new();

        public ICommand QuickActionCommand { get; }

        public DashboardViewModel(MainViewModel mainVm)
        {
            _mainVm = mainVm;

            QuickActionCommand = new RelayCommand(p =>
            {
                _mainVm.NavigateCommand.Execute("Devices");
            });

            ServiceLocator.Logger.LogsUpdated += RefreshRecentLogs;
            RefreshRecentLogs();
        }

        private void RefreshRecentLogs()
        {
            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
            {
                RecentLogs.Clear();
                foreach (var log in ServiceLocator.Logger.GetLogs().Take(5))
                    RecentLogs.Add(log);
            });
        }
    }
}
