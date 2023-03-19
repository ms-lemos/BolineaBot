using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Services.Music;

namespace MagicConchBot.Common.Interfaces
{
    public interface IMusicService
    {
        float GetVolume();

        void SetVolume(float value);

        List<Song> GetSongs();

        Song? CurrentSong { get; }

        Song? LastSong { get; }
        
        PlayMode PlayMode { get; set; }

        bool IsPlaying { get; }

        // Refactor GuildSettings to PlaySettings data record
        Task Play(IInteractionContext msg, GuildSettings settings);

        Task Stop();

        Task Pause();

        Task<bool> Skip(IInteractionContext msg, GuildSettings settings);

        void QueueSong(Song song);

        Task<Song?> RemoveSong(int songNumber);

        void ClearQueue();

        void ShuffleQueue();
    }
}