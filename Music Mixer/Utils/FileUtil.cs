using System.IO;

namespace Nekres.Music_Mixer
{
    internal static class FileUtil
    {
        public static bool IsLocalPath(string p)
        {
            return new System.Uri(p).IsFile;
        }
        public static string Sanitize(string fileName, string replacement = "_")
        {
            return string.Join(replacement, fileName.Split(Path.GetInvalidFileNameChars()));    
        }
    }
}
