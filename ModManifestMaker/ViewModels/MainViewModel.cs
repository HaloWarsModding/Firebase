using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HWM.Tools.Firebase.Shared.Serialization;
using Ookii.Dialogs.Wpf;
using System.IO;
using System.Windows;

namespace HWM.Tools.Firebase.ModManifestMaker.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        #region Bindings

        #region Required Fields

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnableSaving), nameof(ManifestFile))]
        private string _name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnableSaving), nameof(ManifestFile))]
        private string _author;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnableSaving), nameof(ManifestFile))]
        private string _version;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnableSaving), nameof(ModDataDirectory))]
        private string _rootDirectory;

        public string ModDataDirectory => RootDirectory != "" ? Path.Combine(RootDirectory, "ModData") : "";

        #endregion

        #region Optional Fields

        [ObservableProperty]
        private string _customBannerPath;

        [ObservableProperty]
        private string _customIconPath;

        [ObservableProperty]
        private string _description;

        #endregion

        private bool ModDataDirectoryValid => ModDataDirectory != "" && Directory.Exists(ModDataDirectory);
        public  bool EnableSaving => Name != "" && Author != "" && Version != "" && ModDataDirectoryValid;
        private FileInfo ManifestFile => new(Path.Combine(RootDirectory, ReplaceInvalidChars($"{Name} v{Version}.hwmod")));

        #endregion

        public MainViewModel()
        {
            // Required defaults
            (Name, Author, Version, RootDirectory) = ("Vanilla", "Ensemble Studios", "1.12185.2.0", "");

            // Optional defaults
            (CustomBannerPath, CustomIconPath, Description) = ("", "", "The classic Halo Wars: Definitive Edition experience.\n\nFinish the fight!");
        }

        /// <summary>
        /// Prompts the user to select a folder containing their mod's data.
        /// Selected folder must be named "ModData" or must have a "ModData" folder inside it.
        /// </summary>
        [RelayCommand]
        private void GetModDataFolder()
        {
            var modDataFolderSelector = new VistaFolderBrowserDialog
            {
                Description            = "Select your mod's ModData folder",
                SelectedPath           = Directory.GetCurrentDirectory(),
                UseDescriptionForTitle = true,
                Multiselect            = false
            };

            if (modDataFolderSelector.ShowDialog() == true)
            {
                // Create a DirectoryInfo on the selected path
                var selectedFolder = new DirectoryInfo(modDataFolderSelector.SelectedPath);

                // If the actual "ModData" folder was selected instead of the containing folder, this trims off the "ModData" part
                RootDirectory = (selectedFolder.Name != "ModData") ? selectedFolder.FullName : (selectedFolder.Parent?.FullName ?? string.Empty);
                
                // Ensure that either the selected folder is named "ModData" or there is a folder inside the selected one named "ModData"
                if (!ModDataDirectoryValid)
                {
                    MessageBox.Show("The selected folder must be named \"ModData\", or there must be a \"ModData\" folder in the selected directory.", "Incorrect Folder Name", MessageBoxButton.OK, MessageBoxImage.Error);
                    RootDirectory = "";
                }

                // Clear custom folder paths since the data will no longer be in a relative root
                (CustomBannerPath, CustomIconPath) = (string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Prompts the user to select a provided resource, whether that resource be a custom artwork or an existing mod manifest
        /// </summary>
        /// <param name="component"></param>
        [RelayCommand]
        private void GetCustomResourcePath(string component)
        {
            var customResourceMessage = $"Select a custom {component.ToLower()} for your mod";

            switch (component)
            {
                case "Banner":
                    CustomBannerPath = FileBrowserPrompt(customResourceMessage, "PNG Files (*.png)|*.png|Bitmap Files (*.bmp)|*.bmp");
                    break;

                case "Icon":
                    CustomIconPath = FileBrowserPrompt(customResourceMessage, "Icon Files (*.ico)|*.ico");
                    break;

                case "Manifest":
                    // Retrieve existing manifest's fully-qualified path
                    var modManifestFilePath = FileBrowserPrompt("Select an existing Halo Wars mod manifest to edit", "Halo Wars Mods (*.hwmod)|*.hwmod", false);
                    if (modManifestFilePath == string.Empty) return;

                    // Set root directory
                    RootDirectory = Path.GetDirectoryName(modManifestFilePath)!;
                    
                    // Extract manifest data and populate fields
                    var modManifestData = ManifestSerializer.DeserializeManifest(modManifestFilePath) ?? new ModManifest();
                    switch (modManifestData.ManifestVersion)
                    {
                        case "1":
                            // Required data
                            Name    = modManifestData.Required.Title!;
                            Author  = modManifestData.Required.Author!;
                            Version = modManifestData.Required.Version!;

                            // Optional data
                            CustomBannerPath = modManifestData.Optional.Banner.RelativePath!;
                            CustomIconPath   = modManifestData.Optional.Icon.RelativePath!;
                            Description      = modManifestData.Optional.Desc.Text!;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Saves the new manifest to a .hwmod file.
        /// </summary>
        [RelayCommand]
        private void SaveManifest()
        {
            // Delete existing file
            if (ManifestFile.Exists)
                ManifestFile.Delete();

            // Create manifest to serialize
            var tempModManifestData = new ModManifest();
            tempModManifestData.Required.Title               = Name;
            tempModManifestData.Required.Author              = Author;
            tempModManifestData.Required.Version             = Version;
            tempModManifestData.Optional.Banner.RelativePath = CustomBannerPath;
            tempModManifestData.Optional.Icon.RelativePath   = CustomIconPath;
            tempModManifestData.Optional.Desc.Text           = Description;

            // Serialize data
            ManifestSerializer.SerializeManifest(tempModManifestData, ManifestFile.FullName);

            // Check if file actually saved
            if (ManifestFile.Exists)
                MessageBox.Show($"Saved manifest at {ManifestFile.FullName}.", "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show($"Could not save mod manifest!.", "Save Unsuccessful", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #region Helpers

        private string FileBrowserPrompt(string title, string filter, bool trimRoot = true)
        {
            var fileDialog = new VistaOpenFileDialog
            {
                Title       = title,
                Multiselect = false,
                Filter      = filter,
                FileName    = RootDirectory
            };

            // Cancelled
            if (fileDialog.ShowDialog() != true)
                return string.Empty;

            // File isn't relative
            if (trimRoot && !fileDialog.FileName.Contains(RootDirectory))
            {
                MessageBox.Show($"File must be in a relative directory to the following path:\n\n{RootDirectory}", "Relative Path Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return string.Empty;
            }

            // Format path
            var formattedPath = trimRoot ? fileDialog.FileName.Replace(RootDirectory + "\\", string.Empty) : fileDialog.FileName;
            if (formattedPath.StartsWith('\\') || formattedPath.StartsWith('/')) formattedPath = formattedPath[1..];

            return formattedPath;
        }

        private static string ReplaceInvalidChars(string filePath)
            => new(filePath.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());

        #endregion
    }
}
