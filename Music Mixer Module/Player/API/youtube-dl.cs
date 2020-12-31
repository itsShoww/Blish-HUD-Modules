using Blish_HUD.Content;
using CSCore;
using CSCore.Codecs;
using Microsoft.Xna.Framework.Graphics;
using Nekres.Music_Mixer.Player.Source;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nekres.Music_Mixer.Player.API
{
    internal class youtube_dl
    {
        #region "Singleton & Constructor"

        private static youtube_dl _instance;
        public static youtube_dl Instance
        {
            get { return _instance ?? (_instance = new youtube_dl()); }
        }


        private youtube_dl()
        {
        }

        #endregion

        public string ExecutablePath
        {
            get { return Path.Combine(MusicMixerModule.ModuleInstance.ModuleDirectory, "bin/youtube-dl.exe"); }
        }

        private Regex _youtubeVideoID = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)(?<id>[a-zA-Z0-9-_]+)", RegexOptions.Compiled);

        private bool _isLoaded;

        private AudioBitrate _averageBitrate => MusicMixerModule.ModuleInstance.AverageBitrate;

        public async Task Load()
        {
            if (_isLoaded) return;

            var p = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    FileName = ExecutablePath,
                    Arguments = "-U",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                }
            };

            p.Start(); //Updating to version 2015.01.16 ...
            var info = await p.StandardOutput.ReadLineAsync();
            var regex = new Regex(@"Updating to version (?<version>(.*?)) \.\.\.");
            var match = regex.Match(info);
            if (match.Success)
            {
                await Task.Run(() => p.WaitForExit());
            }
            _isLoaded = true;
        }

        public async void GetThumbnail(string link, AsyncTexture2D texture)
        {
            var youTubeId = GetYouTubeIdFromLink(link);
            var thumbnailUrl = string.Format("https://img.youtube.com/vi/{0}/mqdefault.jpg", youTubeId);
            try
            {
                var textureDataResponse = await Blish_HUD.GameService.Gw2WebApi.AnonymousConnection.Client.Render
                                                         .DownloadToByteArrayAsync(thumbnailUrl);

                using (var textureStream = new MemoryStream(textureDataResponse))
                {
                    var loadedTexture =
                        Texture2D.FromStream(Blish_HUD.GameService.Graphics.GraphicsDevice, textureStream);

                    texture.SwapTexture(loadedTexture);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public string GetYouTubeIdFromLink(string youTubeLink)
        {
            var youtubeMatch = _youtubeVideoID.Match(youTubeLink);
            if (!youtubeMatch.Success) return string.Empty;
            return youtubeMatch.Groups["id"].Value;
        }

        private bool _tryagain;
        public async Task<Uri> GetYouTubeStreamUri(string youTubeLink)
        {
            await Load();
            using (var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = ExecutablePath,
                    Arguments = string.Format("-g {0} -f \"bestaudio[ext=m4a][abr<={1}]/bestaudio[ext=aac][abr<={1}]/bestaudio[abr<={1}]/bestaudio\"", youTubeLink, _averageBitrate.ToString().Substring(1))
                }
            })
            {
                p.Start();
                var url = await p.StandardOutput.ReadToEndAsync();
                if (string.IsNullOrEmpty(url))
                {
                    if (_tryagain)
                    {
                        _tryagain = false; throw new Exception(url);
                    }
                    _tryagain = true;
                    return await GetYouTubeStreamUri(youTubeLink);
                }
                if (!url.ToLower().StartsWith("error"))
                {
                    _tryagain = false;
                    return new Uri(url);
                }
                throw new Exception(url);
            }
        }

        public async Task Download(string link, string outputFolder, AudioFormat format, Action<double> progressChangedAction)
        {
            await Load();

            Directory.CreateDirectory(outputFolder);

            using (var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    FileName = ExecutablePath,
                    Arguments = string.Format("{0} -o \"{1}/%(title)s.%(ext)s\" --restrict-filenames --extract-audio --audio-format {2} --ffmpeg-location \"{3}\"", link, outputFolder, format.ToString().ToLower(), ffmpeg.ExecutablePath)
                }
            })
            {
                p.Start();

                if (progressChangedAction == null) return;
                var regex = new Regex(@"^\[download\].*?(?<percentage>(.*?))% of"); //[download]   2.7% of 4.62MiB at 200.00KiB/s ETA 00:23
                while (!p.HasExited)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    if (string.IsNullOrEmpty(line)) continue;
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        var doub = double.Parse(match.Groups["percentage"].Value, CultureInfo.InvariantCulture);
                        progressChangedAction.Invoke(doub);
                    }
                }
            }
        }

        public async Task<IWaveSource> GetSoundSource(string link)
        {
            var streamUri = await youtube_dl.Instance.GetYouTubeStreamUri(link);
            return await Task.Run(() => CutWaveSource(CodecFactory.Instance.GetCodec(streamUri)));
        }

        private IWaveSource CutWaveSource(IWaveSource source)
        {
            //var kHz = source.WaveFormat.SampleRate / 1000;
            return source.AppendSource(x => new CutSource(x, TimeSpan.Zero, source.GetLength()));
        }
    }
}