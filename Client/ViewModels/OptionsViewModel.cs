using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWM.Tools.Firebase.WPF.Core;
using Ookii.Dialogs.Wpf;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using static HWM.Tools.Firebase.WPF.Core.Serialization.ConfigSerializer;

namespace HWM.Tools.Firebase.WPF.ViewModels
{
    public partial class OptionsViewModel : ViewModelBase
    {
        #region Bindings

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDirty))]
        private int _selectedInstallation;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDirty))]
        private string _launchTimeoutDelay;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDirty))]
        private string _userModsFolder;

        public static string VersionText => $"Version: {GlobalData.AppVersion}";
        public ObservableCollection<string> InstallationTypes => ["Steam", "Microsoft Store"];
        public bool IsDirty => InstallationTypes[SelectedInstallation] != GlobalData.UserConfig.InstallationType
                            || LaunchTimeoutDelay                      != GlobalData.UserConfig.TimeoutDelay
                            || UserModsFolder                          != GlobalData.UserConfig.UserModsFolder;

        #endregion

        public OptionsViewModel()
        {
            // Set initial values
            SelectedInstallation = InstallationTypes.IndexOf(GlobalData.UserConfig.InstallationType);
            LaunchTimeoutDelay   = GlobalData.UserConfig.TimeoutDelay;
            UserModsFolder       = GlobalData.UserConfig.UserModsFolder;
        }

        #region Commands

        [RelayCommand]
        private void ChangeUserModsFolder()
        {
            var modFolder = new VistaFolderBrowserDialog
            {
                //SelectedPath = UserConfig.UserModsFolder,
                Description = "Please select your mods folder",
                UseDescriptionForTitle = true
            };

            if (modFolder.ShowDialog() == true)
                UserModsFolder = modFolder.SelectedPath;
        }

        [RelayCommand]
        private void OpenModManifestMaker()
        {
            // Configure game process info
            var gameStartInfo = new ProcessStartInfo()
            {
                CreateNoWindow  = true,
                UseShellExecute = false,
                FileName        = Path.Combine(GlobalData.AppDirectory.FullName, "ModManifestMaker.exe")
            };

            #region Start the app

            try
            {
                using var mmm = new Process { StartInfo = gameStartInfo };
                mmm.Start();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to launch Mod Manifest Maker. Reason: {ex.Message}");
            }

            #endregion
        }

        #endregion
    }
}