using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace MagicConchBot.Attributes
{
    public class RequireUserInVoiceChannelAttribute : PreconditionAttribute
    {
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command,
            IServiceProvider map)
        {
            var channel = (context.User as IGuildUser)?.VoiceChannel;
            return channel != null
                ? Task.FromResult(PreconditionResult.FromSuccess())
                : Task.FromResult(PreconditionResult.FromError("User must be in a voice channel."));
        }
    }
}