using Discord.Commands;
using Discord.Interactions;
using MagicConchBot.Attributes;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Services;
using NLog;
using System;
using System.Threading.Tasks;
using RunMode = Discord.Interactions.RunMode;

namespace MagicConchBot.Modules
{
    [RequireUserInVoiceChannel]
    [Name("Music Commands")]
    public class MusicModule : InteractionModuleBase<ConchInteractionCommandContext>
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ISongResolutionService _songResolutionService;
        private readonly YoutubeInfoService _googleApiInfoService;

        public MusicModule(ISongResolutionService songResolutionService, YoutubeInfoService googleApiInfoService)
        {
            _songResolutionService = songResolutionService;
            _googleApiInfoService = googleApiInfoService;
        }

        [SlashCommand(
            "resume",
            "Plays a song from YouTube or SoundCloud or search for a song on YouTube",
            runMode: RunMode.Async), Alias("p")]
        public async Task Resume()
        {
            if (Context.MusicService.IsPlaying)
            {
                await RespondAsync("Song already playing.");
            }
            else if (Context.MusicService.GetSongs().Count > 0)
            {
                await Context.MusicService.Play(Context);
                await RespondAsync("Resuming queue.");
            }
            else
            {
                await RespondAsync("No songs currently in the queue.");
            }
        }

        [SlashCommand(
            "play",
            "Plays a song from YouTube or SoundCloud or search for a song on YouTube",
            runMode: RunMode.Async), Alias("p")]
        public async Task Play(
            string queryOrUrl,
            TimeSpan? startTime = null)
        {
            await Enqueue(queryOrUrl, startTime);

            // if not playing, start playing and then the player service
            if (!Context.MusicService.IsPlaying)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.Play(Context);
            }
        }

        [SlashCommand("stop", "Stops the bot if it is playing music and disconnects it from the voice channel.")]
        public async Task StopAsync()
        {
            await Context.MusicService.Stop();
            await RespondAsync("Music stopped playing.");
        }

        [SlashCommand("pause", "Pauses the current song.")]
        public async Task PauseAsync()
        {
            await Context.MusicService.Pause();
            await RespondAsync("Music paused successfully.");
        }

        [SlashCommand("skip", "Skips the current song if one is playing.")]
        public async Task SkipAsync()
        {
            var skipped = await Context.MusicService.Skip(Context);
            await RespondAsync(skipped ? "Skipped current song." : "No song available to skip");
        }

        [SlashCommand("shuffle", "shuffles")]
        public async Task ShuffleAsync()
        {
            Context.MusicService.ShuffleQueue();
            await RespondAsync("Shuffled");
        }

        [SlashCommand("dotinha", "dotinha da madrugada")]
        public async Task DotinhaDaMadrugadaAsync()
        {
            await RespondAsync($"Vamo da-le");

            await Enqueue("https://www.youtube.com/playlist?list=PL89yfO4AmqBTdOJ0tTurRq6Qqk9rqd4nY", silent: true);
            await Enqueue("https://www.youtube.com/playlist?list=PL89yfO4AmqBT2-xAjlhyyjUT3691tt3pQ", silent: true);
            await Enqueue("https://www.youtube.com/playlist?list=PL89yfO4AmqBSX-4jDUP7ck1KZIETj0NQ2", silent: true);

            Context.MusicService.ShuffleQueue();

            Context.MusicService.PlayMode = PlayMode.Playlist;

            if (!Context.MusicService.IsPlaying)
            {
                Log.Info("No song currently playing, playing.");
                await Context.MusicService.Play(Context);
            }
        }

        [SlashCommand("volume", "Gets or changes the volume of the current playing song and future songs.")]
        public async Task ChangeVolumeAsync([MinValue(0)][MaxValue(100)] int? volume = null)
        {
            if (volume == null)
            {
                await RespondAsync($"Current volume: {Context.MusicService.GetVolume() * 100}%.");
                return;
            }
            Context.MusicService.SetVolume(volume == 0 ? 0 : volume.Value / 100f);
            await RespondAsync($"Current volume set to {Context.MusicService.GetVolume() * 100}%.");
        }

        [SlashCommand("current", "Displays the current song")]
        public async Task CurrentSongAsync()
        {
            var currentSong = Context.MusicService.CurrentSong;

            if (currentSong == null)
            {
                await RespondAsync(embed: currentSong.Value.GetEmbed());
            }
            else
            {
                await RespondAsync("No song is currently playing.");
            }
        }

        [SlashCommand("loop", "Loops")]
        public async Task Loop()
        {
            Context.MusicService.PlayMode = PlayMode.Playlist;
            await RespondAsync(
                "Successfully changed mode to playlist mode. Songs will not be removed from queue after they are done playing.");
        }

        private async Task Enqueue(string queryOrUrl, TimeSpan? startTime = null, bool silent = false)
        {
            if (queryOrUrl == null)
            {
                await Resume();
                return;
            }

            string url;

            if (!SongHelper.UrlRegex.IsMatch(queryOrUrl))
            {

                url = await _googleApiInfoService.GetFirstVideoByKeywordsAsync(queryOrUrl);
            }
            else
            {
                url = queryOrUrl;
            }

            // url invalid
            if (string.IsNullOrEmpty(url))
            {
                await RespondAsync($"Could not find any videos for: {queryOrUrl}");
                return;
            }

            var youtubeMatch = _googleApiInfoService.Regex.Match(url);
            var playlistId = youtubeMatch.Groups["PlaylistId"].Value;
            if (playlistId != "")
            {
                if (!silent) await ReplyAsync("Queueing songs from playlist. This may take a while, please wait.");
                var songs = await _googleApiInfoService.GetSongsByPlaylistAsync(playlistId);

                songs.ForEach(Context.MusicService.QueueSong);

                if (!silent) await ReplyAsync($"Queued {songs.Count} songs from playlist.");
            }
            else
            {
                Log.Info("Resolving song");
                var song = await _songResolutionService.ResolveSong(url, startTime ?? TimeSpan.Zero);

                // add to queue
                Log.Debug("Queueing song");
                Context.MusicService.QueueSong(song);

                try
                {
                    await RespondAsync(embed: song.GetEmbed());
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
            }
        }
    }
}