using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord.Net;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Modules;
using MagicConchBot.Resources;
using NLog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MagicConchBot.Handlers
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;

        private readonly CommandService _commands;
        private readonly InteractionService _interactionService;
        private readonly IServiceProvider _services;
        private readonly IMusicService _musicService;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool _reconnecting;
        private int _reconnectAttempts;
        private bool _commandsRegistered;
        private bool _resumePending;

        public CommandHandler(InteractionService interactionService, DiscordSocketClient client, CommandService commands, IServiceProvider services, IMusicService musicService)
        {
            _interactionService = interactionService;
            _client = client;
            _commands = commands;
            _services = services;
            _musicService = musicService;
        }

        public void SetupEvents()
        {
            _commands.Log += LogAsync;
            _interactionService.Log += LogAsync;

            _client.InteractionCreated += HandleInteraction;
            _client.MessageReceived += HandleCommandAsync;
            _client.JoinedGuild += HandleJoinedGuildAsync;
            _client.Ready += ClientReady;
            _client.Disconnected += HandleDisconnectedAsync;
        }

        private async Task ClientReady()
        {
            if (_commandsRegistered)
            {
                return;
            }

            try
            {
                await _interactionService.RegisterCommandsGloballyAsync();
                _commandsRegistered = true;
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to register global commands on Ready.");
            }

            if (_resumePending)
            {
                _resumePending = false;
                await _musicService.ResumeAfterReconnectAsync();
            }
        }

        private async Task HandleDisconnectedAsync(Exception exception)
        {
            if (_reconnecting)
            {
                return;
            }

            if (exception is WebSocketClosedException socketClosed && (socketClosed.CloseCode == 4006 || socketClosed.CloseCode == 4009))
            {
                Log.Warn($"Gateway session invalid (code {socketClosed.CloseCode}). Restarting Discord client.");
            }
            else
            {
                Log.Warn(exception, "Gateway disconnected; attempting restart.");
            }

            _reconnecting = true;

            try
            {
                var delay = Math.Min(30000, (int)Math.Pow(2, _reconnectAttempts) * 2000);
                _reconnectAttempts = Math.Min(_reconnectAttempts + 1, 4);
                await Task.Delay(delay);

                await _client.StopAsync();

                if (_client.LoginState != LoginState.LoggedIn)
                {
                    await _client.LoginAsync(TokenType.Bot, Configuration.Token);
                }

                await _client.StartAsync();
                _reconnectAttempts = 0;
                _resumePending = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to restart Discord client after disconnect.");
            }
            finally
            {
                _reconnecting = false;
            }
        }

        public async Task InstallAsync()
        {
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new ConchInteractionCommandContext(_client, arg, _musicService);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }

        private async Task HandleCommandAsync(SocketMessage parameterMessage)
        {
            // Don't handle the command if it is a system message
            if (parameterMessage is not SocketUserMessage message)
                return;
            if (message.Source != MessageSource.User)
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
            var context = new CommandContext(_client, message);

            await Task.Factory.StartNew(async () =>
            {
                // Execute the Command, store the result
                var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);

                // If the command failed, notify the user
                if (!result.IsSuccess)
                    await message.Channel.SendMessageAsync($"**Error:** {result.ErrorReason}");
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }

        public static async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException cmdException)
            {
                // We can tell the user that something unexpected has happened
                await cmdException.Context.Channel.SendMessageAsync("Something went catastrophically wrong!");

                // We can also log this incident
                Console.WriteLine($"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException.ToString());
            }
        }

        private async Task HandleJoinedGuildAsync(SocketGuild arg)
        {
            await _interactionService.RegisterCommandsToGuildAsync(arg.Id);
            await arg.DefaultChannel.SendMessageAsync($"All hail the bolinea bot.");
        }
    }
}