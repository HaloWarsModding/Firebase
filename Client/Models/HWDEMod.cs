using HWM.Tools.Firebase.Shared.Serialization;
using System.IO;

namespace HWM.Tools.Firebase.WPF.Models
{
    public class HWDEMod
    {
        #region Required

        public string? ModID   => ManifestData?.ModID;
        public string  Title   => ManifestData?.Required.Title   ?? (!IsVanilla ? "[Missing Element: Title]"   : "Vanilla");
        public string  Author  => ManifestData?.Required.Author  ?? (!IsVanilla ? "[Missing Element: Author]"  : "Ensemble Studios");
        public string  Version => ManifestData?.Required.Version ?? (!IsVanilla ? "[Missing Element: Version]" : "1.12185.2.0");
        
        #endregion

        #region Optional

        public  string Description  => $"Author: {Author}\n\n{Desc}";
        public  string BannerArt    => GetResourceUri(ManifestData?.Optional.Banner.RelativePath, "DefaultBannerArt.png");
        public  string ShortcutIcon => GetResourceUri(ManifestData?.Optional.Icon.RelativePath, "Icon_Blagoicons.ico");
        private string Desc         => ManifestData?.Optional.Desc.Text ?? (!IsVanilla ? "[Missing Element: Description]" :
                                       "The classic Halo Wars: Definitive Edition experience.\n\nFinish the fight!");
        #endregion

        #region Integrity
        public string? ManifestFilePath { get; private set; }
        public string? ManifestDirectory => Path.GetDirectoryName(ManifestFilePath);
        public string? ModDataFolder     => Path.Combine(ManifestDirectory!, "ModData");
        public bool    IsVanilla => ManifestDirectory == null;
        public bool    IsValid   => IsVanilla || (ModID != null && ManifestData != null && ModID == ManifestSerializer.GenerateModID(ManifestData));
        #endregion

        public ModManifest? ManifestData { get; private set; }
        public HWDEMod(string? filepath = null)
        {
            if (filepath != null)
            {
                ManifestData = ManifestSerializer.DeserializeManifest(filepath);
                ManifestFilePath = filepath;
            }
        }

        private string GetResourceUri(string? relativePath, string embeddedResource)
        {
            if (!IsVanilla && relativePath != null)
            {
                string bannerPath = Path.Combine(ManifestDirectory ?? string.Empty, relativePath);
                if (File.Exists(bannerPath)) return bannerPath;
            }

            // If all else fails, return something useful
            return embeddedResource.EndsWith(".ico") ? Environment.ProcessPath! : $"pack://application:,,,/Resources/{embeddedResource}";
        }
    }
}
