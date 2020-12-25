using System;
using System.Diagnostics;

namespace Nekres.Music_Mixer
{
    internal static class ProcessExtensions
    {
        public static void SafeClose(this Process process) {
            try { process?.Close(); } catch (InvalidOperationException) {}
        }
    }
}
