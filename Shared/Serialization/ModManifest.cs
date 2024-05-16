using System.Xml.Serialization;

namespace HWM.Tools.Firebase.Shared.Serialization
{
    [XmlRoot("HWMod", IsNullable = false)]
    public class ModManifest
    {
        [XmlAttribute]
        public string ManifestVersion { get; set; } = "1";

        [XmlAttribute]
        public string? ModID { get; set; }

        [XmlElement("RequiredData", IsNullable = false)]
        public RequiredData Required = new();

        [XmlElement("OptionalData", IsNullable = true)]
        public OptionalData Optional = new();

        #region Internal Classes
        public class RequiredData
        {
            [XmlAttribute]
            public string? Title { get; set; }

            [XmlAttribute]
            public string? Author { get; set; }

            [XmlAttribute]
            public string? Version { get; set; }
        }

        public class OptionalData
        {
            [XmlElement("BannerArt", IsNullable = true)]
            public BannerArt Banner = new();

            [XmlElement("Icon", IsNullable = true)]
            public IconArt Icon = new();

            [XmlElement("Description", IsNullable = true)]
            public Description Desc = new();

            public class BannerArt
            {
                [XmlText]
                public string? RelativePath { get; set; }
            }

            public class IconArt
            {
                [XmlText]
                public string? RelativePath { get; set; }
            }

            public class Description
            {
                [XmlText]
                public string? Text { get; set; }
            }
        }
        #endregion
    }
}
