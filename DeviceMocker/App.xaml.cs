using System.Windows;
using DeviceMocker.Core;

namespace DeviceMocker
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ServiceLocator.Initialize();
        }
    }
}
