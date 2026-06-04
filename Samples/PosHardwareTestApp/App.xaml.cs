using System;
using System.IO;
using System.Windows;

namespace PosHardwareTestApp
{
    public partial class App : Application
    {
        private static readonly string StartupTraceFile = Path.Combine(Path.GetTempPath(), "PosHardwareTestApp-startup.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            Trace("OnStartup:begin");
            try
            {
                base.OnStartup(e);
                Trace("OnStartup:base-complete");
                MainWindow = new MainWindow();
                Trace("OnStartup:mainwindow-constructed");
                MainWindow.Show();
                Trace($"OnStartup:mainwindow-show-called visible={MainWindow.IsVisible}");
            }
            catch (Exception ex)
            {
                Trace($"OnStartup:exception {ex}");
                throw;
            }
        }

        private static void Trace(string message)
        {
            try
            {
                File.AppendAllText(StartupTraceFile, $"{DateTime.Now:O} {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }
    }
}
