using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;

namespace MagicConchBot.Attributes
{
    public class RequireUserInVoiceChannelAttribute : PreconditionAttribute
    {
        public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo,
            IServiceProvider services)
        {
            var channel = (context.User as IGuildUser)?.VoiceChannel;
            return channel != null
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("User must be in a voice channel.");
        }
    }
}