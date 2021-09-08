using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SocialGuard.YC.Services;
using System.Threading.Tasks;

namespace SocialGuard.YC.Modules
{
	public partial class BaseSlashModule
	{
		[SlashCommandGroup("trustlist", "Provides commands and integrations to the SocialGuard Trustlist.")]
		public class TrustlistSlashModule : ApplicationCommandModule
		{
			private readonly TrustlistUserApiService trustlist;

			public TrustlistSlashModule(TrustlistUserApiService trustlist)
			{
				this.trustlist = trustlist;
			}

			[SlashCommand("lookup", "Looks up a user record on the Trustlist.")]
			public Task LookupSlashAsync(InteractionContext ctx, [Option("user", "User to lookup")] DiscordUser user) => LookupAsync(ctx, user);

			[ContextMenu(ApplicationCommandType.UserContextMenu, "SocialGuard Lookup")]
			public Task LookupContextAsync(ContextMenuContext ctx) => LookupAsync(ctx, ctx.User);


			protected async Task LookupAsync(BaseContext ctx, DiscordUser user)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(await trustlist.GetLookupEmbedAsync(user)));
			}
		}
	}
}
