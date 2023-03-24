using Discord.Interactions;
using Discord.WebSocket;
using MagicConchBot.Common.Interfaces;

namespace MagicConchBot.Modules
{
    public class ConchInteractionCommandContext : SocketInteractionContext
    {
        public ConchInteractionCommandContext(
            DiscordSocketClient client,
            SocketInteraction interaction,
            IMusicService musicService) : base(client, interaction)
        {
            MusicService = musicService;
        }

        public IMusicService MusicService { get; }
    }
}