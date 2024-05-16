using HWM.Tools.Firebase.WPF.Core;
using Serilog;
using System.Windows;

namespace HWM.Tools.Firebase.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Create static logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("Data\\Logs\\Firebase.log",
                            rollingInterval: RollingInterval.Day,
                            rollOnFileSizeLimit: true)
                .CreateLogger();

            Log.Information("**********APP START**********");

            // Resume normal startup
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Release lock on lock file
            AppInstanceManager.ReleaseLock();

            base.OnExit(e);
        }
    }
}
