using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Handlers;
using MagicConchBot.Resources;
using MagicConchBot.Services;
using MagicConchBot.Services.Music;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MagicConchBot
{
    public class Program
    {
        private static CancellationTokenSource _cts;
        private static DiscordSocketClient _client;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Main(string[] args)
        {
            Logging.ConfigureLogs();

            Log.Info("Starting bolinea bot. Press 'q' at any time to quit.");

            try
            {
                _cts = new CancellationTokenSource();
                Task.Factory.StartNew(async () => await MainAsync(args, _cts.Token), _cts.Token).Wait();

                while (!_cts.Token.IsCancellationRequested)
                {
                    if (!Console.IsInputRedirected && Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        if (key == ConsoleKey.Q)
                        {
                            Stop();
                        }
                        else if (key == ConsoleKey.G)
                        {
                            Log.Info("Listing guilds: ");
                            foreach (var guild in _client.Guilds)
                            {
                                Log.Info($"{guild.Name} - '{guild?.Owner?.Username}:{guild?.Owner?.Id}'");
                            }
                        }
                        continue;
                    }

                    Thread.Sleep(100);
                }
            }
            finally
            {
                Log.Info("Bot sucessfully exited.");
                Console.WriteLine("Press enter to continue . . .");
                Console.ReadLine();
            }
        }

        public static void Stop()
        {
            _cts.Cancel();
        }

        private static async Task MainAsync(string[] args, CancellationToken cancellationToken)
        {
            using var services = ConfigureServices();
            _client = services.GetService<DiscordSocketClient>();
            var _interactionService = services.GetService<InteractionService>();

            try
            {
                _client.Log += a => { Log.WriteToLog(a); return Task.CompletedTask; };

                var commandHandler = services.GetService<CommandHandler>();

                commandHandler.SetupEvents();
                await commandHandler.InstallAsync();

                if (args != null && args.Any())
                {
                    Configuration.Token = args[0];
                }

                await _client.LoginAsync(TokenType.Bot, Configuration.Token);
                await _client.StartAsync();

                await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.WriteToLog(new LogMessage(LogSeverity.Critical, string.Empty, ex.ToString(), ex));
            }
            finally
            {
                await _client.StopAsync();
            }
        }


        public static ServiceProvider ConfigureServices()
        {
            var config = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
            };

            return new ServiceCollection()
                .AddSingleton(config)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<InteractionService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<CommandHandler>()
                .AddSingleton<CommandService>()
                .AddSingleton<YoutubeInfoService>()
                .AddSingleton<ISongInfoService, YoutubeInfoService>()
                .AddSingleton<ISongResolutionService, SongResolutionService>()
                .AddSingleton<IMusicService, MusicService>()
                .AddSingleton<ISongPlayer, FfmpegSongPlayer>()
                .BuildServiceProvider();
        }
    }
}