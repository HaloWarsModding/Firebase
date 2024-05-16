using System.Xml.Serialization;

namespace HWM.Tools.Firebase.WPF.Core.Serialization
{
    [XmlRoot("Config", IsNullable = false)]
    public class ConfigFile
    {
        [XmlAttribute]
        public string? ReleaseVer { get; set; } = GlobalData.AppVersion;

        [XmlElement("Distro", IsNullable = false)]
        public string InstallationType { get; set; } = "Steam";

        [XmlElement("ModsDir", IsNullable = false)]
        public string UserModsFolder { get; set; } = GlobalData.DefaultUserModsFolder.FullName;

        [XmlElement("TimeoutDelay", IsNullable = false)]
        public string TimeoutDelay { get; set; } = "30";
    }
}
