﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface IMusicService
    {
        float Volume { get; set; }

        List<Song> SongList { get; }

        Song LastSong { get; }
        
        Song CurrentSong { get; }

        PlayMode PlayMode { get; set; }

        AudioState AudioState { get; }

        Task PlayAsync(IUserMessage msg);

        bool Stop();

        bool Pause();

        bool Skip();

        void QueueSong(Song song);

        Song RemoveSong(int songNumber);

        void ClearQueue();
    }
}