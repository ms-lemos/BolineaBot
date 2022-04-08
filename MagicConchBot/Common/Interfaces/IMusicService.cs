﻿using System.Collections.Generic;
using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface IMusicService
    {
        float GetVolume();

        void SetVolume(float value);

        List<Song> GetSongs();

        Song LastSong { get; }
        
        Song CurrentSong { get; }

        PlayMode PlayMode { get; set; }

        PlayerState PlayerState { get; }

        // Refactor GuildSettings to PlaySettings data record
        void Play(IInteractionContext msg, GuildSettings settings);

        bool Stop();

        bool Pause();

        bool Skip();

        void QueueSong(Song song);

        Song RemoveSong(int songNumber);

        void ClearQueue();
    }
}