using HWM.Tools.Firebase.WPF.Windows;
using Medstar.CodeSnippets;
using Monitor.Core.Utilities;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Windows;
using static Medstar.CodeSnippets.PermissionsManager;

namespace HWM.Tools.Firebase.WPF.Core
{
    internal static class MainLaunchTasks
    {
        /// <summary>
        /// Clear out any traces of mod data from LocalAppData.
        /// </summary>
        public static Task CleanLocalAppData()
        {
            Log.Information("Cleaning LocalAppData directory for Halo Wars: Definitive Edition...");

            try
            {
                // Remove ModManifest.txt
                if (GlobalData.ModManifestFilePath.Exists)
                    GlobalData.ModManifestFilePath.Delete();

                // Delete any leftover ModData junctions
                if (JunctionPoint.Exists(GlobalData.ModDataJunction.FullName))
                    JunctionPoint.Delete(GlobalData.ModDataJunction.FullName);

                // Double check on directory cleanup
                if (JunctionPoint.Exists(GlobalData.ModDataJunction.FullName) && GlobalData.ModManifestFilePath.Exists)
                    throw new IOException("An unknown error has prevented the deletion of either the ModData junction point, the ModManifest.txt file, or both.");

                // All OK
                Log.Information("Successfully cleaned the LocalAppData directory!");
            }
            catch (IOException ex)
            {
                // Display error message
                Log.Error($"Failed to clean the game's LocalAppData folder. User will have to clean it manually.\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                MessageBox.Show("Failed to clean the game's LocalAppData folder!", "Launch Failure");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Adds a directory junction to the game's LocalAppData folder
        /// </summary>
        public static Task LinkModToLocalAppData()
        {
            Log.Information($"Loading {GlobalData.SelectedMod.Title}...");

            try
            {
                // No edits, normal game
                if (GlobalData.SelectedMod.IsVanilla)
                    return Task.CompletedTask;

                // Selected mod is invalid
                if (!GlobalData.SelectedMod.IsValid)
                {
                    Log.Error($"Failed to link mod to the game's LocalAppData folder. Selected mod is invalid/corrupt. Please use the embedded Mod Manifest Maker to allow Firebase to load it.");
                    MessageBox.Show("Failed to link mod to the game's LocalAppData folder. Selected mod is invalid/corrupt. Please use the embedded Mod Manifest Maker to allow Firebase to load it.", "Launch Failure");
                    return Task.CompletedTask;
                }

                // Mainly for Microsoft Store distros so that data can be accessed
                AddDirectorySecurity(GlobalData.SelectedMod.ModDataFolder!, SID.AllApplicationPackages, FileSystemRights.Read, AccessControlType.Allow);

                // Create folder junctiona and create ModManifest.txt file
                JunctionPoint.Create(GlobalData.ModDataJunction.FullName, GlobalData.SelectedMod.ModDataFolder!, true);

                // Create ModManifest.txt pointing to the ModData directory junction
                using (var manifest = File.CreateText(GlobalData.ModManifestFilePath.FullName))
                    manifest.WriteLine(GlobalData.ModDataJunction.FullName);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to link mod to the game's LocalAppData folder. An unknown error has occurred.\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                MessageBox.Show("Failed to link mod to the game's LocalAppData folder. See logs for error details.", "Launch Failure");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Kicks off the game and waits for it to become active.
        /// 
        /// Will wait for the amount of time in seconds specified by the specified Timeout Threshold.
        /// </summary>
        public static Task LaunchGameProcess()
        {
            Log.Information($"Launching game...");

            try
            {
                // Configure game process info
                var gameStartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow  = true,
                    UseShellExecute = false,
                    FileName        = "cmd.exe",
                    Arguments       = GlobalData.LaunchCommand
                };

                #region Start the game and time how long it takes to launch

                // Set up process object and stopwatch
                using var gameProc = new Process() { StartInfo = gameStartInfo };
                var timeout = new Stopwatch();

                // Start game and timer
                gameProc.Start(); timeout.Start();

                // Wait for process to start
                while (!gameProc.HasExited)
                    if (timeout.ElapsedMilliseconds > int.Parse(GlobalData.UserConfig.TimeoutDelay) * 1000)
                        throw new TimeoutException("Game took too long to start!");

                // Stop timer if process successfully launched
                timeout.Stop();

                #endregion
            }
            catch (TimeoutException)
            {
                Log.Error("Launch timeout theshold exceeded. The game took too long to start.");
                MessageBox.Show("Launch timeout theshold exceeded. The game took too long to start. Increase your 'Timeout Delay' setting in Options if it takes longer for your game to start.", "Launch Failure");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to launch game. An unknown error has occurred.\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                MessageBox.Show("Failed to launch the game. See logs for error details.", "Launch Failure");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Scans for the game's process and stores it
        /// </summary>
        public static Task CatchGameProcess()
        {
            Log.Information("Trying to catch game process...");

            try
            {
                GlobalData.GameProcess = Process.GetProcessById(GetGameProcessId());
                if (GlobalData.GameProcess == null)
                    throw new Exception("Could not catch game process!");
            }
            catch (TimeoutException)
            {
                Log.Error("Launch timeout theshold exceeded. The game took too long to start.");
                MessageBox.Show("Launch timeout theshold exceeded. The game took too long to start. Increase your 'Timeout Delay' setting in Options if it takes longer for your game to start.", "Launch Failure");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to catch game process. An unknown error has occurred.\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                MessageBox.Show("Failed to catch game process. See logs for error details.", "Launch Failure");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Posts a toast notification, then waits for the game to exit
        /// </summary>
        public static Task NotifyAndWait()
        {
            Log.Information("Game process caught! Notifying user and waiting for game to exit...");

            // Minimize main window
            MainWindow.Instance!.WindowState = WindowState.Minimized;

            // Pop out a fancy notification showing that the game was detected
            Utilities.PopToastNotification("Game Process Caught!", $"Firebase {(GlobalData.WasLaunchedFromShortcut ? "running in background." : "minimized to taskbar.")}");

            // Wait for the game to exit
            GlobalData.GameProcess.WaitForExit();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Does some cleanup work whenever the game is closed
        /// </summary>
        public static async Task CleanAfterExit()
        {
            Log.Information("Game has exited. Tidying up LocalAppData...");

            // Try cleaning the LocalAppData folder
            await CleanLocalAppData();

            // Bring the manager back into view if it wasn't launched with a pre-selected mod
            if (!GlobalData.WasLaunchedFromShortcut)
            {
                MainWindow.Instance!.WindowState = WindowState.Normal;
                Utilities.SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
                return;
            }

            // If it was launched with a shortcut, shut down the manager
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Fetches the game's process ID
        /// </summary>
        /// <returns>The game's process ID, or -1 if not found in the allotted time period. </returns>
        /// <exception cref="TimeoutException">Occurs when the TimeoutDelay threshold is exceeded. </exception>
        public static int GetGameProcessId()
        {
            var (gameProcessId, sw) = (-1, new Stopwatch());

            sw.Start();
            while (gameProcessId == -1)
            {
                // Stop searching when time elapses
                if (sw.ElapsedMilliseconds > int.Parse(GlobalData.UserConfig.TimeoutDelay) * 1000)
                    throw new TimeoutException("Could not get handle to game process!");

                // Determine how to grab the running process
                gameProcessId = GlobalData.UserConfig.InstallationType switch
                {
                    "Microsoft Store" => UWPProcFetcher.GetProcessID(GlobalData.TargetStartupApplicationName),
                    "Steam"           => Process.GetProcessesByName(GlobalData.TargetStartupApplicationName).FirstOrDefault()?.Id ?? -1,
                    _                 => -1
                };
            }
            sw.Stop();

            return gameProcessId;
        }
    }
}
