using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace HWM.Tools.Firebase.Shared.Serialization
{
    public static class ManifestSerializer
    {
        private static readonly XmlSerializerNamespaces xns = new();
        private static readonly XmlSerializer serializer = new(typeof(ModManifest));
        private static readonly XmlWriterSettings xws = new() { OmitXmlDeclaration = true, Indent = true };

        /// <summary>
        /// Deserializes a ModManifest from a filepath to a new object.
        /// </summary>
        /// <param name="filepath">A filepath to a mod manifest file</param>
        /// <returns></returns>
        public static ModManifest? DeserializeManifest(string filepath)
        {
            try
            {
                using var reader = new StringReader(File.ReadAllText(filepath));
                return serializer.Deserialize(reader) as ModManifest;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Serializes a ModManifest object to an Xml file
        /// to a specified filepath.
        /// </summary>
        /// <param name="data">Deserialized ModManfiest data</param>
        /// <param name="outFile">Location to save the current ModManfiest data</param>
        public static void SerializeManifest(ModManifest data, string outFile)
        {
            // Remove default namespaces
            if (xns.Count != 1)
                xns.Add(string.Empty, string.Empty);

            // Ensure a ModID is set
            data.ModID = string.IsNullOrEmpty(data.ModID) ? GenerateModID(data) : data.ModID;

            // Serialize to file
            using var xw = XmlWriter.Create(outFile, xws);
            serializer.Serialize(xw, data, xns);
        }

        /// <summary>
        /// Creates a SHA256 hash unique for a given mod.
        /// Takes the provided mod's title, author, and version
        /// to generate the hash.
        /// </summary>
        /// <param name="manifestData">Deserialized ModManfiest data</param>
        /// <returns>A string consiting of a SHA256 hash representing a ModID</returns>
        public static string GenerateModID(ModManifest manifestData)
        {
            var modID = new StringBuilder();

            // Hash data
            var data = $"<{manifestData.Required.Title}-{manifestData.Required.Author}-{manifestData.Required.Version}>";
            var encodedString = new UTF8Encoding(false).GetBytes(data);
            var hashedBytes   = SHA256.HashData(encodedString);

            // Build ModID string
            for (int i = 0; i < hashedBytes.Length; i++)
                modID.Append(hashedBytes[i].ToString("X2"));
            return modID.ToString().Replace("-", string.Empty);
        }
    }
}
