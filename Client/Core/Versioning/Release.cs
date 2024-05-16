namespace HWM.Tools.Firebase.WPF.Core.Versioning
{
    internal class Release
    {
        public string tag_name { get; set; } = string.Empty;
        public List<ReleaseAsset> assets { get; set; } = [];


        public bool UpdateExists   => new Version(tag_name == string.Empty ? "0.0.0.0" : tag_name) > new Version(GlobalData.AppVersion) && PatchFileUri != string.Empty;
        public string PatchFileUri => MatchedAsset != null ? MatchedAsset.browser_download_url : string.Empty;

        private ReleaseAsset? MatchedAsset => assets.FirstOrDefault(asset => asset.name == "AutoUpdatePackage.zip");
    }

    internal class ReleaseAsset
    {
        public string name { get; set; } = string.Empty;
        public string browser_download_url { get; set; } = string.Empty;
    }
}
