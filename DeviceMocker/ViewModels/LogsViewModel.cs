using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using DeviceMocker.Core;
using DeviceMocker.Helpers;
using DeviceMocker.Models;
using Microsoft.Win32;

namespace DeviceMocker.ViewModels
{
    public class LogsViewModel : ViewModelBase
    {
        private string _statusMessage = string.Empty;

        public ObservableCollection<DeviceLog> Logs { get; } = new();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportJsonCommand { get; }

        public LogsViewModel()
        {
            RefreshCommand = new RelayCommand(RefreshLogs);
            ClearCommand = new RelayCommand(ClearLogs);
            ExportCsvCommand = new RelayCommand(ExportCsv, () => Logs.Count > 0);
            ExportJsonCommand = new RelayCommand(ExportJson, () => Logs.Count > 0);

            ServiceLocator.Logger.LogsUpdated += () =>
            {
                System.Windows.Application.Current?.Dispatcher?.Invoke(RefreshLogs);
            };
        }

        public void RefreshLogs()
        {
            Logs.Clear();
            foreach (var log in ServiceLocator.Logger.GetLogs())
                Logs.Add(log);
            StatusMessage = $"{Logs.Count} log entries.";
        }

        private void ClearLogs()
        {
            ServiceLocator.Logger.Clear();
            Logs.Clear();
            StatusMessage = "Logs cleared.";
        }

        private void ExportCsv()
        {
            var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = $"DeviceMocker_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv" };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Timestamp,Device,DeviceType,OutputChannel,Payload,Success,ErrorMessage");
                foreach (var log in Logs)
                {
                    var payload = log.Payload.Replace("\"", "\"\"");
                    var error = log.ErrorMessage.Replace("\"", "\"\"");
                    sb.AppendLine($"\"{log.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{log.DeviceName}\",\"{log.DeviceType}\",\"{log.OutputChannelType}\",\"{payload}\",{log.Success},\"{error}\"");
                }
                File.WriteAllText(dialog.FileName, sb.ToString());
                StatusMessage = $"Exported {Logs.Count} logs to CSV.";
            }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }

        private void ExportJson()
        {
            var dialog = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = $"DeviceMocker_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json" };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
                var json = JsonSerializer.Serialize(Logs.ToList(), options);
                File.WriteAllText(dialog.FileName, json);
                StatusMessage = $"Exported {Logs.Count} logs to JSON.";
            }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }
}
