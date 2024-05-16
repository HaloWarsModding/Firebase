using System.IO;

namespace HWM.Tools.Firebase.WPF.Core
{
    /// <summary>
    /// Helper for ensuring a single instance is active for the mod manager.
    /// </summary>
    /// <remarks>
    /// This seemed to be more reliable than using a mutex.
    /// </remarks>
    public static class AppInstanceManager
    {
        #region Fields

        private static FileStream? LockFile_FS = null;

        #endregion

        #region Properties

        private static bool HasLock => LockFile_FS != null;
        private static string LockFile => Path.Combine(GlobalData.AppDirectory.FullName, ".lock");

        #endregion

        #region Methods

        public static bool CanAcquireLock()
        {
            // We already have a lock
            if (HasLock)
                return true;

            // Attempt to get a lock (mutex)
            try
            {
                LockFile_FS = File.Open(LockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
#pragma warning disable CA1416 // Validate platform compatibility
                LockFile_FS.Lock(0, 0);
#pragma warning restore CA1416 // Validate platform compatibility
                return true;
            }
            catch (Exception) { return false; }
        }

        public static void ReleaseLock()
        {
            if (HasLock)
            {
                LockFile_FS?.Close();
                if (File.Exists(LockFile))
                    File.Delete(LockFile);
            }
        }

        #endregion
    }
}
