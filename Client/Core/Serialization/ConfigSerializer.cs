using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using WPFCustomMessageBox;
using Serilog;

namespace HWM.Tools.Firebase.WPF.Core.Serialization
{
    public static class ConfigSerializer
    {
        private static string UserConfigFilePath => Path.Combine(GlobalData.AppDirectory.FullName, "Data", "Config.dat");

        private static readonly XmlSerializerNamespaces xns = new();
        private static readonly XmlSerializer serializer = new(typeof(ConfigFile));
        private static readonly XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };

        public static ConfigFile? Load()
        {
            try
            {
                Log.Information("Loading user configuration...");

                ConfigFile config;

                // A config file already exists
                if (!File.Exists(UserConfigFilePath))
                {
                    Log.Information("No configuration file found! Running first-time setup...");
                    config = RunFirstTimeSetup();
                }
                else
                {
                    Log.Information("Existing user configuration file found! Loading entries...");
                    config = DeserializeUserConfigFile();
                }
                config.Save();

                if (!GlobalData.LocalAppDataFolder.Exists)
                {
                    var mb = MessageBox.Show($"Game LocalAppData directory not found for the currently logged-in user '{GlobalData.CurrentUser}'\n\n" +
                                             $"Please ensure that you are logged into the profile that installed the game!", "LocalAppData Folder Missing");


                    // Delete original config and shut down
                    if (File.Exists(UserConfigFilePath)) File.Delete(UserConfigFilePath);
                    Log.Error("Game LocalAppData directory was not found. Clearing user config and shutting down...");
                    return null;
                }

                Log.Information("User configuration loaded!");
                return config;
            }
            catch (Exception ex)
            {
                Log.Fatal($"A fatal error has occurred while loading user configuration!\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                return null;
            }
        }

        public static bool Save(this ConfigFile config)
        {
            try
            {
                // Remove default namespaces
                if (xns.Count != 1)
                    xns.Add(string.Empty, string.Empty);

                // Create containing folder
                Directory.CreateDirectory(Directory.GetParent(UserConfigFilePath)!.FullName);

                // Write new data
                using var writer = XmlWriter.Create(UserConfigFilePath, xws);
                serializer.Serialize(writer, config, xns);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while saving user configuration!\n\nMessage: {ex.Message}\n\nStackTrace: {ex.StackTrace}\n\nTargetSite: {ex.TargetSite}");
                return false;
            }
        }

        private static ConfigFile RunFirstTimeSetup()
        {
            // Create default mods folder
            if (!GlobalData.DefaultUserModsFolder.Exists)
                GlobalData.DefaultUserModsFolder.Create();

            // Build and show message box
            var result = CustomMessageBox.ShowYesNo(
                messageBoxText: "Please select your game installation type\n(this can be changed later)",
                caption       : "First Time Setup",
                yesButtonText : "Steam",
                noButtonText  : "Microsoft Store"
                );

            // Save configuration
            return new ConfigFile { InstallationType = result == MessageBoxResult.Yes ? "Steam" : "Microsoft Store" };
        }

        private static ConfigFile DeserializeUserConfigFile()
        {
            try
            {
                using var reader = new StringReader(File.ReadAllText(UserConfigFilePath));
                return (ConfigFile?)serializer.Deserialize(reader) ?? throw new ArgumentNullException("Error in deserializing user configuration");
            }
            catch (Exception)
            {
                // Delete the config file and try making a new one
                if (File.Exists(UserConfigFilePath))
                    File.Delete(UserConfigFilePath);
                return RunFirstTimeSetup();
            }
        }
    }
}
