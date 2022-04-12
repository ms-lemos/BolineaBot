﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Resources;
using SpotifyAPI.Web;
using YoutubeExplode;

namespace MagicConchBot.Services
{
    public class SpotifyResolveService : ISongInfoService
    {
        public SpotifyResolveService()
        {
            var authenticator = new ClientCredentialsAuthenticator(
                Configuration.SpotifyClientId,
                Configuration.SpotifyClientSecret
            );

            Client = new SpotifyClient(
                SpotifyClientConfig
                    .CreateDefault()
                    .WithAuthenticator(authenticator)
            );
        }

        private YoutubeClient youtubeClient = new();

        public SpotifyClient Client { get; set; }

        //https://open.spotify.com/track/712uvW1Vezq8WpQi38v2L9?si=b963989009c74cd8
        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?open\.spotify\.com\/track\/(?<trackId>\w+)?(\?.+)?",
            RegexOptions.IgnoreCase);

        public async Task<Maybe<Song>> GetSongInfoAsync(string url)
        {
            var trackId = Regex.Match(url).Groups["trackId"];

            var songUrl = $"https://open.spotify.com/track/{trackId.Value}";

            var track = await Client.Tracks.Get(trackId.Value);

            return new Song(
                track.Name + " - " + string.Join(",", track.Artists.Select(a => a.Name)),
                new SongTime(Length: new TimeSpan(0, 0, 0, 0, track.DurationMs)),
                track.Album.Images.FirstOrDefault()?.Url,
                url,
                songUrl,
                MusicType.Spotify,
                track.Name + " " + string.Join(" ", track.Artists.Select(a => a.Name))
            );
        }

        public async Task<Song> ResolveStreamUri(Song song)
        {
            var results = youtubeClient.Search.GetResultsAsync(song.Identifier);
            var res = await results.FirstAsync();
            return song with { Identifier = res.Url, StreamUri = res.Url };
        }
    }
}