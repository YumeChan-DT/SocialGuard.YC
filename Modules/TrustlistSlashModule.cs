using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using MongoDB.Driver;
using YumeChan.PluginBase.Tools.Data;
using DSharpPlus.SlashCommands.Attributes;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Models;

namespace SocialGuard.YC.Modules
{
	public partial class BaseSlashModule
	{
		[SlashCommandGroup("trustlist", "Provides commands and integrations to the SocialGuard Trustlist.")]
		public class TrustlistSlashModule : ApplicationCommandModule
		{
			private readonly TrustlistClient _trustlist;
			private readonly ApiAuthService _apiAuth;
			private readonly IMongoCollection<GuildConfig> guildConfig;

			public TrustlistSlashModule(TrustlistClient trustlist, ApiAuthService apiAuth, IDatabaseProvider<PluginManifest> databaseProvider)
			{
				_trustlist = trustlist;
				this._apiAuth = apiAuth;
				guildConfig = databaseProvider.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
			}

			[SlashCommand("lookup", "Looks up a user record on the Trustlist.")]
			public Task LookupSlashAsync(InteractionContext ctx, [Option("user", "User to lookup")] DiscordUser user) => LookupAsync(ctx, user);

			[ContextMenu(ApplicationCommandType.UserContextMenu, "SocialGuard Lookup")]
			public Task LookupContextAsync(ContextMenuContext ctx) => LookupAsync(ctx, ctx.TargetUser);

			[SlashCommand("add", "Inserts/Updates an entry in the Trustlist."), SlashRequireGuild, SlashRequireUserPermissions(Permissions.BanMembers)]
			public async Task AddAsync(InteractionContext ctx,
				[Option("user", "User to insert into Trustlist")] DiscordUser user,
				[Option("severity", "Escalation level (severity) of Trustlist entry")] TrustlistEscalationLevel level,
				[Option("reason", "Description of Trustlist entry")] string reason,
				[Option("ban", "Ban the user locally?")] bool localBan = false,
				[Option("message-removal-span", "Remove message history (if ban) from last..."), Choice("None", 0), Choice("1 Day", 1), Choice("1 Week", 7)] long deleteDays = 0)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });
				await InsertUserAsync(ctx, user, (byte)level, reason, localBan, (int)deleteDays);
			}

			protected async Task LookupAsync(BaseContext ctx, DiscordUser user)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(await _trustlist.GetLookupEmbedAsync(user)));
			}

			protected async Task InsertUserAsync(InteractionContext context, DiscordUser user, byte level, string reason, bool banUser = false, int deleteDays = 0)
			{
				if (user?.Id == context.User.Id)
				{
					await context.FollowUpAsync("You cannot insert yourself in the Trustlist.", true);
				}
				else if (user.IsBot)
				{
					await context.FollowUpAsync("You cannot insert a Bot in the Trustlist.", true);
				}
				else if ((user as DiscordMember)?.Roles.Any(r => r.Permissions.HasPermission(Permissions.ManageGuild)) ?? false)
				{
					await context.FollowUpAsync("You cannot insert a server operator in the Trustlist. Demote them first.", true);
				}
				else if (reason.Length < 5)
				{
					await context.FollowUpAsync("Reason is too short", true);
				}
				else
				{
					try
					{
						GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);

						if (config.ApiLogin is null)
						{
							await context.FollowUpAsync("No API Credentials set. Use ``/socialguard config accesskey <key>`` to set an Access Key.");
						}
						else
						{
							await _trustlist.SubmitEntryAsync(user.Id, new()
							{
								EscalationLevel = level,
								EscalationNote = reason
							}, (await _apiAuth.GetOrUpdateAuthTokenAsync(context.Guild.Id)).Token);

							string userMention = (user as DiscordMember)?.Mention ?? user.Id.ToString();
							DiscordEmbed embed = await _trustlist.GetLookupEmbedAsync(user);

							await context.FollowUpAsync(new DiscordFollowupMessageBuilder() { Content = $"User '{userMention}' successfully inserted into Trustlist." }
								.AddEmbed(embed));

							if (banUser || (config.AutoBanBlacklisted && level >= 3))
							{
								await context.Guild.BanMemberAsync(user.Id, deleteDays, $"[SocialGuard] {reason}");
								await context.Guild.GetChannel(config.BanLogChannel).SendMessageAsync($"Banned user '{userMention}'.", embed);
							}
						}
					}
					catch (ApplicationException e)
					{
						await context.FollowUpAsync(e.Message, true);
#if DEBUG
						throw;
#endif
					}
				}
			}
		}
	}
}
