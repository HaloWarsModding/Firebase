using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Windows;

namespace HWM.Tools.Firebase.WPF.Core
{
    internal static partial class Utilities
    {
        [System.Runtime.InteropServices.LibraryImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        public static void EnsureSingleInstance()
        {
            // Delay starting briefly if starting right after an auto-update
            if (GlobalData.StartupArguments.Contains("-delayStart"))
                Task.Delay(TimeSpan.FromSeconds(7)).Wait();

            // Ensure single instance
            if (!AppInstanceManager.CanAcquireLock())
            {
                var result = MessageBox.Show($"More than one instance of {GlobalData.AppName} is currently running. Would you like to terminate the other instance?",
                                              "Initialization Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                // Kill current instance
                if (result != MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                    return;
                }

                // Kill existing instance
                var targets = Process.GetProcessesByName(GlobalData.AppName).Where(target => target.Id != Environment.ProcessId);
                foreach (var target in targets) target.Kill(true);
            }
        }

        public static void PopToastNotification(string caption, string message, int secondsToDisplayFor = 5)
        {
            new ToastContentBuilder()
                .AddText(caption)
                .AddText(message)
                .Show(toast => { toast.ExpirationTime = DateTime.Now.AddSeconds(secondsToDisplayFor); });
        }
    }
}
