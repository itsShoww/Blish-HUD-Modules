using System.IO;

namespace Nekres.Music_Mixer
{
    internal static class FileUtil
    {
        public static bool IsLocalPath(string p)
        {
            return new System.Uri(p).IsFile;
        }
        /// <summary>
        /// Sanitize the filename and replace illegal characters.
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="replacement">The replacement string for illegal characters.</param>
        /// <returns>The sanitized filename</returns>
        public static string Sanitize(string fileName, string replacement = "_")
        {
            return string.Join(replacement, fileName.Split(Path.GetInvalidFileNameChars()));    
        }
        /// <summary>
        /// Checks if a file is currenty locked.
        /// </summary>
        /// <remarks>
        /// Suffers from thread race condition.
        /// </remarks>
        /// <param name="file">The file info.</param>
        /// <returns><see langword="True"/> if file is locked or does not exist. Otherwise <see langword="false"/>.</returns>
        public static bool IsFileLocked(string uri)
        {
            try
            {
                using (FileStream stream = File.Open(uri, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}
