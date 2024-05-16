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
        [NotifyPropertyChangedFor(nameof(RequiredFieldsSet))]
        private string _name;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RequiredFieldsSet))]
        private string _author;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RequiredFieldsSet))]
        private string _version;

        public string ModDataDirectory => Path.Combine(RootDirectory, "ModData");

        #endregion

        #region Optional Fields

        [ObservableProperty]
        private string _customBannerPath;

        [ObservableProperty]
        private string _customIconPath;

        [ObservableProperty]
        private string _description;

        public bool RequiredFieldsSet => Name             != ""
                                      && Author           != ""
                                      && Version          != ""
                                      && ModDataDirectory != "";

        #endregion

        //public string RootDirectory => new DirectoryInfo(string.IsNullOrEmpty(ModDataDirectory) ? Directory.GetCurrentDirectory() : ModManifestFilePath).Parent?.FullName;

        private string RootDirectory { get; set; }  = string.Empty;
        private string ModManifestFilename { get; set; } = string.Empty;

        #endregion

        public MainViewModel()
        {
            // Required defaults
            (Name, Author, Version) = ("Vanilla", "Ensemble Studios", "1.12185.2.0");

            // Optional defaults
            (CustomBannerPath, CustomIconPath, Description) = ("", "", "The classic Halo Wars: Definitive Edition experience.\n\nFinish the fight!");
        }

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
                var selectedFolder = new DirectoryInfo(modDataFolderSelector.SelectedPath);
                if (selectedFolder.Name != "ModData")
                {
                    MessageBox.Show("The selected folder must be named \"ModData\".", "Incorrect Folder Name", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Set the new root and generate a custom filename
                RootDirectory = Directory.GetParent(selectedFolder.FullName)!.FullName;
                ModManifestFilename = $"{Name} v{Version}.hwmod";

                // Clear custom folder paths since the data will no longer be in a relative root
                (CustomBannerPath, CustomIconPath) = (string.Empty, string.Empty);
            }
        }

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
                    CustomIconPath   = FileBrowserPrompt(customResourceMessage, "Icon Files (*.ico)|*.ico");
                    break;
                case "Manifest":
                    var modManifestFilePath = FileBrowserPrompt("Select an existing Halo Wars mod manifest to edit", "Halo Wars Mods (*.hwmod)|*.hwmod", false);
                    if (modManifestFilePath == string.Empty) return;

                    // Since this is an existing manifest, store the original name
                    RootDirectory       = Path.GetDirectoryName(modManifestFilePath)!;
                    ModManifestFilename = Path.GetFileName(modManifestFilePath);

                    // Map data from manifest to UI
                    var modManifestData = ManifestSerializer.DeserializeManifest(modManifestFilePath) ?? new ModManifest();
                    switch (modManifestData.ManifestVersion)
                    {
                        case "1":
                            Directory.CreateDirectory(ModDataDirectory);

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

        [RelayCommand]
        private void SaveManifest()
        {
            // Delete existing mod manifest
            if (File.Exists(Path.Combine(RootDirectory, ModManifestFilename)))
                File.Delete(Path.Combine(RootDirectory, ModManifestFilename));

            // Make temporary manifest file
            var tempModManifestData = new ModManifest();
            tempModManifestData.Required.Title               = Name;
            tempModManifestData.Required.Author              = Author;
            tempModManifestData.Required.Version             = Version;
            tempModManifestData.Optional.Banner.RelativePath = CustomBannerPath;
            tempModManifestData.Optional.Icon.RelativePath   = CustomIconPath;
            tempModManifestData.Optional.Desc.Text           = Description;

            // Serialize data
            ManifestSerializer.SerializeManifest(tempModManifestData, Path.Combine(RootDirectory, string.IsNullOrEmpty(ModManifestFilename) ? $"{Name} v{Version.Replace(".", "_")}.hwmod" : ModManifestFilename));

            // Notify user
            MessageBox.Show($"Saved manifest at {Path.Combine(RootDirectory, ModManifestFilename)}.", "Mod Manifest Saved", MessageBoxButton.OK, MessageBoxImage.Information);
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

            // Validation pass, return item
            return trimRoot ? fileDialog.FileName.Replace(RootDirectory + Environment.NewLine, string.Empty) : fileDialog.FileName;
        }

        #endregion
    }
}
