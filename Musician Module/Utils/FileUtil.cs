using Nekres.Musician_Module.Player.Sound;
using NAudio.Vorbis;
using System.Threading.Tasks;
using static Nekres.Musician_Module.MusicianModule;

namespace Nekres.Musician_Module
{
    internal static class FileUtil
    {
        public static async Task<CachedSound> GetFileStreamAsync(string relativePath) {
            return await Task.Run(() => new CachedSound(new AutoDisposeFileReader(new VorbisWaveReader(ModuleInstance.ContentsManager.GetFileStream(relativePath)))));
        }
    }
}
