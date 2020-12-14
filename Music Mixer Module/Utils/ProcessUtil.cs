using Blish_HUD;
using System;
using System.Diagnostics;

namespace Nekres.Music_Mixer
{
    internal static class ProcessUtil
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(ProcessUtil));
        public static Process CreateProcess(string exePath, string workingDir, string arguments, bool redirectStdOut) {
            var process = new Process() {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = workingDir,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = redirectStdOut
                },
                EnableRaisingEvents = true
            };
            Logger.Info($"Created a new process for '{exePath}'\nArguments: '{arguments}'\nWorking Dir: '{workingDir}'");
            return process;
        }
    }
}
