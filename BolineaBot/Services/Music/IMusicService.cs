using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        Task Play(IInteractionContext msg);

        Task Stop();

        Task Pause();

        Task<bool> Skip(IInteractionContext msg);

        void QueueSong(Song song);

        Task<Song?> RemoveSong(int songNumber);

        void ClearQueue();

        void ShuffleQueue();
    }
}