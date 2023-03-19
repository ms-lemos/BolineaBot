using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;
using YoutubeExplode.Videos.Streams;

namespace MagicConchBot.Services.Music
{
    public class MusicService : IMusicService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ISongPlayer _songPlayer;
        private readonly IEnumerable<ISongInfoService> _songResolvers;

        private int _songIndex;

        private CancellationTokenSource _tokenSource;

        private IUserMessage _currentPlayingMessage;

        public MusicService(IEnumerable<ISongInfoService> songResolvers, ISongPlayer songPlayer)
        {
            _songResolvers = songResolvers;
            _songPlayer = songPlayer;
            _songPlayer.OnSongCompleted += async (s, e) => await PlayNextSong(s, e);
            _songPlayer.OnSongError += async (s, e) => await PlayNextSong(s, e);
            _songPlayer.OnSongError += async (s, e) => Log.Error(e.ex);
            _songList = new List<Song>();
            PlayMode = PlayMode.Queue;
            LastSong = null;
            _currentPlayingMessage = null;
        }

        private List<Song> _songList { get; }

        public PlayMode PlayMode { get; set; }

        public Song? LastSong { get; private set; }

        public Song? CurrentSong => HasNextSong ? _songList[_songIndex] : null;

        public bool HasNextSong => _songIndex >= 0 && _songIndex < _songList.Count;

        public bool IsPlaying => _songPlayer.IsPlaying();

        public float GetVolume()
        {
            return _songPlayer.GetVolume();
        }

        public void SetVolume(float value)
        {
            _songPlayer.SetVolume(value);
        }

        public List<Song> GetSongs()
        {
            return _songList;
        }

        public async Task Play(IInteractionContext context, GuildSettings settings)
        {
            if (CurrentSong == null) return;

            IVoiceChannel audioChannel = await AudioHelper.GetAudioChannel(context);
            IAudioClient audioClient = await AudioHelper.JoinChannelAsync(audioChannel);

            if (audioClient == null)
            {
                await context.Channel.SendMessageAsync("Failed to join voice channel.");
                return;
            }

            await Play(audioClient, context.Channel, CurrentSong.Value, audioChannel.Bitrate);
        }

        public async Task Stop()
        {
            await DeleteCurrentPlayingMessage();
            _songList.Clear();
            await _songPlayer.Stop();
        }

        public async Task Pause()
        {
            await DeleteCurrentPlayingMessage();
            await _songPlayer.Pause();
        }

        public async Task<bool> Skip(IInteractionContext context, GuildSettings settings)
        {
            await _songPlayer.Stop();
            SkipSong();
            await Play(context, settings);
            return HasNextSong;
        }

        public void QueueSong(Song song)
        {
            _songList.Add(song);
        }

        public async Task<Song?> RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber >= _songList.Count)
                return null;

            if (songNumber == 0)
                await Stop();

            var song = _songList[songNumber];
            _songList.Remove(song);

            return song;
        }

        public void ClearQueue()
        {
            _songList.Clear();
        }

        public void ShuffleQueue()
        {
            _songList.Shuffle();
        }

        private async Task Play(IAudioClient audioClient, IMessageChannel channel, Song song, int bitrate)
        {
            if (_tokenSource == null || _tokenSource.Token.IsCancellationRequested)
            {
                _tokenSource = new CancellationTokenSource();
            }

            try
            {
                var resolvedSong = await ResolveSong(song);
                _songList[_songIndex] = resolvedSong;

                _tokenSource.Token.ThrowIfCancellationRequested();

                Log.Info($"Playing song {resolvedSong.Name} at {channel.Name}");
                _songPlayer.PlaySong(audioClient, channel, resolvedSong, bitrate);
                await StatusUpdater(channel, resolvedSong).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
                _songList.RemoveAt(_songIndex);
            }
        }

        private async Task<Song> ResolveSong(Song song)
        {
            var properResolver = _songResolvers.FirstOrDefault(resolver => resolver.Regex.IsMatch(song.OriginalUrl));
            return await properResolver.ResolveStreamUri(song);
        }

        private async Task DeleteCurrentPlayingMessage()
        {
            await _currentPlayingMessage?.DeleteAsync();
            _currentPlayingMessage = null;
        }

        private async Task PlayNextSong(object sender, SongArgs e)
        {
            LastSong = e.Song;

            SkipSong();

            if (HasNextSong)
            {
                await Play(e.Client, e.MessageChannel, CurrentSong.Value, e.Bitrate);
            }
            else
            {
                await AudioHelper.LeaveChannelAsync(e.Client);
            }
        }

        private void SkipSong()
        {
            if (PlayMode == PlayMode.Queue)
            {
                if (HasNextSong)
                    _songList.Remove(CurrentSong.Value);
            }
            else
            {
                _songIndex = (_songIndex + 1) % _songList.Count;
            }
        }

        private async Task StatusUpdater(IMessageChannel channel, Song song)
        {
            try
            {
                if (_currentPlayingMessage != null)
                {
                    var embed = song.GetEmbed("", true, GetVolume());
                    await _currentPlayingMessage.ModifyAsync(m => m.Embed = embed);
                }

                else
                {
                    _currentPlayingMessage = await channel.SendMessageAsync(string.Empty, false, song.GetEmbed("", true, GetVolume()));
                }
            }
            catch (OperationCanceledException ex)
            {
                Log.Debug($"Player task cancelled: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
        }
    }
}