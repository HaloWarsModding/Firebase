using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;

namespace HWM.Tools.Firebase.WPF.Core.Versioning
{
    internal static class AutoUpdater
    {
        private const string RepoOwner = "HaloWarsModding";
        private const string RepoName  = "Firebase";
        private const string INET_AGENT = "HWM.Tools.Firebase";
        
        private static bool AlreadyRan = false;
        
        private static string REPO_URL    => $"https://github.com/{RepoOwner}/{RepoName}";
        private static string RELEASE_URL => $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

        private static string UpdateFolderPath => Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "Updates");

        public static bool GetUpdatesFromGitHub()
        {
            if (AlreadyRan)
                return false;

            Utilities.EnsureSingleInstance();

            foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(Environment.ProcessPath!)!, "*.deleteonnextlaunch", SearchOption.TopDirectoryOnly))
                File.Delete(file);
            Log.Information("Old version files detected and deleted");

            var releaseData = GetLatestReleaseJSON();
            
            if (!releaseData.UpdateExists)
            {
                Log.Information("No new update available");
                return false;
            }

            Log.Information($"A newer version of this mod manager is available ({GlobalData.AppVersion} --> {releaseData.tag_name}). Prompting user for input...");
            var result = MessageBox.Show($"A newer version of this mod manager is available. Would you like to update?\n\n" +
                                        $"{GlobalData.AppVersion} --> {releaseData.tag_name}", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            // Update was denied
            if (result != MessageBoxResult.Yes)
            {
                Log.Information("Update denied");
                return false;
            }

            // Clear out updates directory (if not already cleared)
            if (Directory.Exists(UpdateFolderPath)) Directory.Delete(UpdateFolderPath, true);
            Directory.CreateDirectory(UpdateFolderPath);

            DownloadUpdatePackage(releaseData.PatchFileUri);

            // Rename old files
            var oldFiles = Directory.EnumerateFiles(GlobalData.AppDirectory.FullName).Where(file => File.Exists(Path.Combine(UpdateFolderPath, Path.GetFileName(file))));
            foreach (var file in oldFiles) File.Move(file, file + ".deleteonnextlaunch");

            // Move new files
            var newFiles = Directory.EnumerateFiles(UpdateFolderPath);
            foreach (var file in newFiles) File.Move(file, Path.Combine(GlobalData.AppDirectory.FullName, Path.GetFileName(file)));

            Directory.Delete(UpdateFolderPath);

            // Start new process
            Utilities.PopToastNotification("Now Updating", "Please wait while Firebase updates. The client will restart automatically.");
            Process.Start(Environment.ProcessPath!, new List<string> { "-delayStart" });
            AlreadyRan = true;
            return true;
        }

        private static void DownloadUpdatePackage(string downloadUrl)
        {
            try
            {
                // Add required user-agent for GitHub API requests
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("user-agent", INET_AGENT);

                // Get the filestream, extract it to the updates directory
                var stream = client.GetStreamAsync(downloadUrl).Result;
                ZipFile.ExtractToDirectory(stream, UpdateFolderPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to download and/or extract update contents");
            }
        }

        private static Release GetLatestReleaseJSON()
        {
            try
            {
                // Add required user-agent for GitHub API requests
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("user-agent", INET_AGENT);

                // Deserialize JSON data into a parseable object
                Log.Information("Downloading latest release information...");
                var response = client.GetAsync(RELEASE_URL).Result;
                return JsonConvert.DeserializeObject<Release>(response.Content.ReadAsStringAsync().Result) ?? new Release();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to query GitHub API!");
                return new Release();
            }
        }
    }
}
