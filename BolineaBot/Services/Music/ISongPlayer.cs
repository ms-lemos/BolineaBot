using Discord;
using Discord.Audio;
using MagicConchBot.Common.Types;
using System;
using System.Threading.Tasks;

namespace MagicConchBot.Common.Interfaces
{
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
    public record SongArgs(IAudioClient Client, IMessageChannel MessageChannel, Song Song, int Bitrate);
    public record SongErrorArgs(Exception Ex, IAudioClient Client, IMessageChannel MessageChannel, Song Song, int Bitrate) : SongArgs(Client, MessageChannel, Song, Bitrate);

    public interface ISongPlayer
    {
        event AsyncEventHandler<SongArgs> OnSongCompleted;
        event AsyncEventHandler<SongErrorArgs> OnSongError;
        float GetVolume();
        void SetVolume(float value);
        void PlaySong(IAudioClient client, IMessageChannel messageChannel, Song song, int bitrate);
        bool IsPlaying();
        Task Stop();
        Task Pause();
    }

    public interface IFileProvider
    {
        Task<string> GetStreamingFile(Song song);
    }

    public enum MusicType
    {
        YouTube = 0,
        SoundCloud = 1,
        Spotify = 2,
        Other
    }
}
