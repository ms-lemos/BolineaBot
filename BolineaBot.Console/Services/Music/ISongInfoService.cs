using MagicConchBot.Common.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicConchBot.Common.Interfaces
{
    // todo: extract these implementations to base class, keep interface as clean contract
    public interface ISongInfoService
    {
        Regex Regex { get; }

        Task<Song> GetSongInfoAsync(string url);

        /// <summary>
        /// Specifiy specific resolver to resolve Songs to a streamable Uri, defaults to youtube-dl
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        async Task<Song> ResolveStreamUri(Song song)
        {
            var youtubeDlInfo = await GetUrlFromYoutubeDlAsync(song.Identifier);

            var url = youtubeDlInfo.First();

            if (string.IsNullOrEmpty(url))
            {
                throw new Exception($"Could not resolve song uri from youtube-dl for {song}");
            }

            return song with { DefaultStreamUri = url };
        }

        private static async Task<IEnumerable<string>> GetUrlFromYoutubeDlAsync(string url)
        {
            //-reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5
            var youtubeDl = new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"-g -f bestaudio --audio-quality 0 {url}",
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var p = Process.Start(youtubeDl);
            if (p == null)
            {
                return Enumerable.Empty<string>();
            }

            var output = new List<string>();

            while (!p.StandardOutput.EndOfStream)
            {
                output.Add(await p.StandardOutput.ReadLineAsync());
            }

            return output;
        }
    }
}