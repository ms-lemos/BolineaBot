﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface ISongPlayer
    {
        float GetVolume();
        void SetVolume(float value);

        PlayerState PlayerState { get; }
        Task PlaySong(IAudioClient client, Song song, string intro = "hello_bozo.pcm");
        void Stop();
        void Pause();
    }

    public interface IFileProvider
    {
        Task<string> GetStreamingFile(Song song);
    }

    public interface ISongResolver
    {
        Task<string> GetSongStreamUrl(Song song);
    }

    public enum MusicType
    {
	    YouTube = 0,
	    SoundCloud = 1,
        Spotify = 2,
	    Other
    }
}
