using Discord;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            _songPlayer.OnSongCompleted += async (s, e) => await PlayNextSong(e);
            _songPlayer.OnSongError += async (s, e) => await PlayNextSong(e);
            _songPlayer.OnSongError += (s, e) => { Log.Error(e.Ex); return Task.CompletedTask; };
            SongList = new List<Song>();
            PlayMode = PlayMode.Queue;
            LastSong = null;
            _currentPlayingMessage = null;
        }

        private List<Song> SongList { get; }

        public PlayMode PlayMode { get; set; }

        public Song? LastSong { get; private set; }

        public Song? CurrentSong => HasNextSong ? SongList[_songIndex] : null;

        public bool HasNextSong => _songIndex >= 0 && _songIndex < SongList.Count;

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
            return SongList;
        }

        public async Task Play(IInteractionContext context)
        {
            if (CurrentSong == null) return;

            IVoiceChannel audioChannel = AudioHelper.GetAudioChannel(context);
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
            SongList.Clear();
            await _songPlayer.Stop();
        }

        public async Task Pause()
        {
            await DeleteCurrentPlayingMessage();
            await _songPlayer.Pause();
        }

        public async Task<bool> Skip(IInteractionContext context)
        {
            await _songPlayer.Stop();
            SkipSong();
            await Play(context);
            return HasNextSong;
        }

        public void QueueSong(Song song)
        {
            SongList.Add(song);
        }

        public async Task<Song?> RemoveSong(int songNumber)
        {
            if (songNumber < 0 || songNumber >= SongList.Count)
                return null;

            if (songNumber == 0)
                await Stop();

            var song = SongList[songNumber];
            SongList.Remove(song);

            return song;
        }

        public void ClearQueue()
        {
            SongList.Clear();
        }

        public void ShuffleQueue()
        {
            SongList.Shuffle();
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
                SongList[_songIndex] = resolvedSong;

                _tokenSource.Token.ThrowIfCancellationRequested();

                Log.Info($"Playing song {resolvedSong.Name} at {channel.Name}");
                _songPlayer.PlaySong(audioClient, channel, resolvedSong, bitrate);
                await StatusUpdater(channel, resolvedSong).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
                SongList.RemoveAt(_songIndex);
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

        private async Task PlayNextSong(SongArgs e)
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
                    SongList.Remove(CurrentSong.Value);
            }
            else
            {
                _songIndex = (_songIndex + 1) % SongList.Count;
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