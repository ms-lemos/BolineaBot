﻿using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using MagicConchBot.Modules;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using NLog;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private DiscordSocketClient _client;

        private CmdSrv _commands;
        private IDependencyMap _map;

        public void ConfigureServices(IDependencyMap depMap)
        {
            _map = depMap;
            _map.Add(new GoogleApiService());
            _map.Add(new YouTubeInfoService(_map));
            _map.Add(new SoundCloudInfoService());
            _map.Add(new ChanService());
            _map.Add(new StardewValleyService());
        }

        public async Task InstallAsync()
        {
            // Create Command Service, inject it into Dependency Map
            _client = _map.Get<DiscordSocketClient>();
            _commands = new CmdSrv();

            //_map.Add(_commands);

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _client.MessageReceived += HandleCommandAsync;
            _client.GuildAvailable += HandleGuildAvailableAsync;
            _client.JoinedGuild += HandleJoinedGuildAsync;
            _client.MessageReceived += HandleMessageReceivedAsync;
        }

        public async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage as SocketUserMessage;
            if (message == null)
                return;

            // Mark where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message has a valid prefix, adjust argPos 
            if (!(message.HasMentionPrefix(_client.CurrentUser, ref argPos) || message.HasCharPrefix('!', ref argPos)))
                return;

            // Handle case of !! or !!! (some prefixes for other bots)
            if (message.Content.Split('!').Length > 2)
                return;

            // Create a Command Context
            var context = new MusicCommandContext(_client, message);

            // Execute the Command, store the result
            var result = await _commands.ExecuteAsync(context, argPos, _map, MultiMatchHandling.Best);

            // If the command failed, notify the user
            if (!result.IsSuccess)
                if (result.ErrorReason == Configuration.Load().WrongChannelError)
                    await message.Channel.SendMessageAsync($"{result.ErrorReason}", true);
                else
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
        }

        private static async Task HandleMessageReceivedAsync(SocketMessage arg)
        {
            foreach (var attachment in arg.Attachments)
                if (attachment.Filename.EndsWith(".webm"))
                {
                    Log.Info($"Url: {attachment.Url}");
                    Log.Info($"Proxy: {attachment.ProxyUrl}");
                    await Task.Delay(1);
                }
        }

        private static async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            await arg.DefaultChannel.SendMessageAsync($"All hail the Magic Conch. In order to use the Music functions of this bot, please create a role named '{Configuration.Load().RequiredRole}' and add that role to the users whom you want to be able to control the Music functions of this bot. Type !help for help.");
            await HandleGuildAvailableAsync(arg);
        }

        private static Task HandleGuildAvailableAsync(SocketGuild guild)
        {
            var fileProvider = new HttpStreamingFileProvider();
            var songPlayer = new FfmpegSongPlayer(fileProvider);
            var urlResolver = new UrlSteamResolver();

            var musicService = new MusicService(urlResolver, songPlayer);

            MusicServiceProvider.AddServices(guild.Id, musicService, new Mp3ConverterService());
            return Task.CompletedTask;
        }
    }
}