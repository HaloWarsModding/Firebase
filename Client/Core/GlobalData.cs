﻿using HWM.Tools.Firebase.WPF.Core.Serialization;
using HWM.Tools.Firebase.WPF.Models;
using System.Diagnostics;
using System.IO;

namespace HWM.Tools.Firebase.WPF.Core
{
    public static class GlobalData
    {
        public static ConfigFile UserConfig { get; set; } = new();

        public static HWDEMod SelectedMod { get; set; } = new();
        public const string TargetStartupApplicationName = "xgameFinal";

        public static Process GameProcess { get; set; }

        #region Command line args

        public static List<string> StartupArguments => Environment.GetCommandLineArgs().ToList();
        public static bool WasLaunchedFromShortcut => StartupArguments.Contains("-mod");

        #endregion

        #region Launch Commands

        private const string Launch_HWDE_Steam = "/C start steam://rungameid/459220";
        private const string Launch_HWDE_MS = "/C start shell:AppsFolder\\Microsoft.BulldogThreshold_8wekyb3d8bbwe!xgameFinal";
        public static string LaunchCommand => UserConfig.InstallationType == "Steam" ? Launch_HWDE_Steam : Launch_HWDE_MS;

        #endregion

        #region LocalAppData Info

        private static readonly string LocalAppData_System = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string LocalAppData_Steam = Path.Combine(LocalAppData_System, "Halo Wars");
        private static readonly string LocalAppData_MS = Path.Combine(LocalAppData_System, "Packages", "Microsoft.BulldogThreshold_8wekyb3d8bbwe", "LocalState");
        public static DirectoryInfo LocalAppDataFolder => new(UserConfig.InstallationType == "Steam" ? LocalAppData_Steam : LocalAppData_MS);

        #endregion

        #region AppInfo

        public static FileInfo AppPath => new(Environment.ProcessPath!);
        public static DirectoryInfo AppDirectory => AppPath.Directory!;
        public static string AppName => Path.GetFileNameWithoutExtension(AppPath.FullName);
        public static string AppVersion => FileVersionInfo.GetVersionInfo(AppPath.FullName).FileVersion!;

        #endregion

        #region Other Directories

        public static FileInfo ModManifestFilePath => new(Path.Combine(LocalAppDataFolder.FullName, "ModManifest.txt"));
        public static DirectoryInfo DefaultUserModsFolder => new(Path.Combine(AppDirectory.FullName, "HWDE Mods"));
        public static DirectoryInfo ModDataFolderPath => new(Path.Combine(LocalAppDataFolder.FullName, "ModData"));

        #endregion

        
    }
}
