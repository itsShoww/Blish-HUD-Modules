using Musician_Module.Player.Sound;
using NAudio.Vorbis;
using System.Threading.Tasks;
using static Musician_Module.MusicianModule;

namespace Musician_Module
{
    internal static class FileUtil
    {
        public static async Task<CachedSound> GetFileStreamAsync(string relativePath) {
            return await Task.Run(() => new CachedSound(new AutoDisposeFileReader(new VorbisWaveReader(ModuleInstance.ContentsManager.GetFileStream(relativePath)))));
        }
    }
}
