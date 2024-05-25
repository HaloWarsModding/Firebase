using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWM.Tools.Firebase.WPF.Core;
using HWM.Tools.Firebase.WPF.Core.Versioning;
using HWM.Tools.Firebase.WPF.Models;
using HWM.Tools.Firebase.WPF.Windows;
using Ookii.Dialogs.Wpf;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using static HWM.Tools.Firebase.WPF.Core.Serialization.ConfigSerializer;

namespace HWM.Tools.Firebase.WPF.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        #region Bindings

        [ObservableProperty]
        private HWDEMod? _selectedMod;

        [ObservableProperty]
        private bool _enablePlayButton;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ProgressVisibility))]
        private double _progressBarValue;

        [ObservableProperty]
        private int _progressBarMaximum;

        [ObservableProperty]
        private string _progressBarLabelContent;
        
        
        private OptionsWindow? _optionsWindow;
        public ObservableCollection<HWDEMod> InstalledMods { get; set; } = [];
        public Visibility ProgressVisibility => ProgressBarValue > 0 ? Visibility.Visible : Visibility.Hidden;

        #endregion

        private readonly List<(Func<Task> task, string? displayText)> LaunchTasks = [];

        public MainViewModel()
        {
            // Set defaults
            (EnablePlayButton, ProgressBarValue, ProgressBarMaximum, ProgressBarLabelContent) = (true, 0, 6, string.Empty);

            // Config loading
            if (!Load())
            {
                Application.Current.Shutdown();
                return;
            }

            // Mod loading and setting
            LoadModsFromUserModsFolder();

            // Check to prevent extra code from executing during design time
            var dep = new DependencyObject();
            if (!DesignerProperties.GetIsInDesignMode(dep))
            {
                // If launched from shortcut, the manager should remain minimized
                if (GlobalData.WasLaunchedFromShortcut)
                {
                    // Grab mod from list based on mod ID
                    SelectedMod = InstalledMods.FirstOrDefault(mod => mod.ModID == GlobalData.StartupArguments[GlobalData.StartupArguments.IndexOf("-mod") + 1]);
                    SelectedMod ??= InstalledMods[0];

                    // Check if desired mod is found
                    if (SelectedMod != InstalledMods[0] || GlobalData.StartupArguments[GlobalData.StartupArguments.IndexOf("-mod") + 1] == "vanilla")
                        Application.Current.Dispatcher.Invoke(PlayGame); // Wait for the task to complete before proceeding
                }
                else if (AutoUpdater.GetUpdatesFromGitHub())
                {
                    Application.Current.Shutdown();
                }
            }
        }

        #region Commands

        [RelayCommand]
        private void OpenOptionsWindow()
        {
            _optionsWindow = new OptionsWindow();
            _optionsWindow.ShowDialog();

            // Reload mods
            LoadModsFromUserModsFolder();
        }

        [RelayCommand]
        private void OpenModsFolder()
        {
            try
            {
                var dialog = new VistaOpenFileDialog
                {
                    Title = "Mod Folder Browser",
                    InitialDirectory = GlobalData.UserConfig.UserModsFolder
                };
                dialog.ShowDialog();
                LoadModsFromUserModsFolder();
                SelectedMod = InstalledMods[0];
            }
            catch (Win32Exception ex)
            {
                Log.Error(ex, "Could not open mods folder");
            }
        }

        [RelayCommand]
        private void CreateModShortcut()
        {
            if (SelectedMod == null)
                return;

            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Launch {SelectedMod.Title}.{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "lnk" : "desktop")}");

            // Ensure shortcut doesn't already exist
            if (File.Exists(shortcutPath))
            {
                MessageBox.Show($"Shortcut for \"{SelectedMod.Title}\" already exists on your desktop!");
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Set up IWsh objects
                var shell    = new IWshRuntimeLibrary.WshShell();
                var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutPath);

                // Set shortcut properties
                shortcut.TargetPath       = GlobalData.AppPath.FullName;
                shortcut.Description      = $"Shortcut for \"{SelectedMod.Title}\"";
                shortcut.WorkingDirectory = GlobalData.AppDirectory.FullName;
                shortcut.Arguments        = $"-mod {(SelectedMod.IsVanilla ? "vanilla" : SelectedMod.ModID)}";
                if (SelectedMod.ShortcutIcon != null) shortcut.IconLocation = SelectedMod.ShortcutIcon;

                // Save the shortcut and inform the user
                shortcut.Save();
            }
            // TODO
            //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            //{
            //    // Write out a shortcut file
            //    using (var writer = new StreamWriter(shortcutPath))
            //    {
            //        writer.WriteLine("[Desktop Entry]");
            //        writer.WriteLine($"Version=1.0");
            //        writer.WriteLine($"Type=Application");
            //        writer.WriteLine($"Terminal=false");
            //        writer.WriteLine($"Name={GlobalData.AppName}");
            //        writer.WriteLine($"Exec={GlobalData.AppPath.FullName} -mod {(SelectedMod.IsVanilla ? "vanilla" : SelectedMod.ModID)}");
            //        writer.WriteLine($"Icon={SelectedMod.ShortcutIcon ?? GlobalData.AppPath.FullName}");
            //        writer.WriteLine($"Comment=Shortcut for \"{SelectedMod.Title}\"");
            //    }
            //}
            else
            {
                MessageBox.Show($"Invalid platform type!", "Shortcut Creation Failed");
                return;
            }

            MessageBox.Show($"Desktop shortcut created for \"{SelectedMod.Title}\".", "Shortcut Creation Successful");
        }

        [RelayCommand]
        private async Task PlayGame()
        {
            // Load tasks
            LaunchTasks.Add((MainLaunchTasks.CleanLocalAppData    , "Pre-cleaning game LocalAppData folder..."));
            LaunchTasks.Add((MainLaunchTasks.LinkModToLocalAppData, SelectedMod!.IsVanilla ? "Loading vanilla game..." : $"Linking mod \"{SelectedMod.Title}\" information to LocalAppData folder..."));
            LaunchTasks.Add((MainLaunchTasks.LaunchGameProcess    , "Launching Halo Wars: Definitive Edition..."));
            LaunchTasks.Add((MainLaunchTasks.CatchGameProcess     , "Trying to catch game process..."));
            LaunchTasks.Add((MainLaunchTasks.NotifyAndWait        , "Caught game process! Minimizing GUI..."));
            LaunchTasks.Add((MainLaunchTasks.CleanAfterExit       , "Tidying up LocalAppData..."));

            // Iterate through tasks, then reset
            foreach (var (task, message) in LaunchTasks)
                await RunTaskAsync(task, message);
            ProgressBarValue = 0; LaunchTasks.Clear();
        }

        private async Task RunTaskAsync(Func<Task> taskFunc, string? message)
        {
            ProgressBarLabelContent = message ?? ProgressBarLabelContent;
            ProgressBarValue += 1;
            await Application.Current.Dispatcher.InvokeAsync(taskFunc, System.Windows.Threading.DispatcherPriority.Background); // Wait for the task to complete before proceeding
        }

        #endregion

        #region Events

        partial void OnSelectedModChanged(HWDEMod? value)
        {
            if (value != null)
                GlobalData.SelectedMod = value;
        }

        #endregion

        #region Helper Methods

        private void LoadModsFromUserModsFolder()
        {
            try
            {
                Log.Information($"Loading mods from {GlobalData.UserConfig.UserModsFolder}...");

                // Clear and add Vanilla entry to installed mods
                InstalledMods.Clear(); InstalledMods.Add(new HWDEMod());

                // Ensure the mods folder exists. If it doesn't, default to the original
                if (!Directory.Exists(GlobalData.UserConfig.UserModsFolder))
                {
                    Directory.CreateDirectory(GlobalData.DefaultUserModsFolder.FullName);
                    GlobalData.UserConfig.UserModsFolder = GlobalData.DefaultUserModsFolder.FullName;
                    GlobalData.UserConfig.Save();
                }

                // Find all "*.hwmod" files in specified mods folder and wrap them into HWDEMod objects
                var manifests = Directory.EnumerateFiles(GlobalData.UserConfig.UserModsFolder, "*.hwmod", SearchOption.AllDirectories);
                manifests.ToList().ForEach(filepath => InstalledMods.Add(new HWDEMod(filepath)));

                // Ensure a mod is always selected
                SelectedMod = InstalledMods[0];

                var modCount = InstalledMods.Count - 1;
                Log.Information($"{modCount} mod{(modCount != 1 ? "s" : string.Empty)} detected and loaded.");
            }
            catch (Exception ex)
            {
                Log.Error($"An unknown error has occurred while loading installed mods!\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
            }
        }

        #endregion
    }
}
