using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Linq;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		public class UserLookupModule : BaseCommandModule
		{
			private readonly TrustlistUserApiService trustlist;
			private readonly AuthApiService auth;
			private readonly IEntityRepository<GuildConfig, ulong> repository;

			public UserLookupModule(TrustlistUserApiService trustlist, AuthApiService auth, IDatabaseProvider<PluginManifest> databaseProvider)
			{
				this.trustlist = trustlist;
				this.auth = auth;
				repository = databaseProvider.GetEntityRepository<GuildConfig, ulong>();
			}

			[Command("lookup"), Aliases("get")]
			public async Task LookupAsync(CommandContext context, DiscordUser user) => await RespondLookupAsync(context, user);

			[Command("insert"), Aliases("add"), RequireGuild, RequireUserPermissions(Permissions.BanMembers)]
			public async Task InsertUserAsync(CommandContext context, DiscordUser user, byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, level, reason, false);
			}

			[Command("ban"), RequireGuild, RequireUserPermissions(Permissions.BanMembers), RequireBotPermissions(Permissions.BanMembers)]
			public async Task BanUserAsync(CommandContext context, DiscordUser user, [Range(0, 3)] byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, level, reason, true);
			}

			private async Task InsertUserAsync(CommandContext context, DiscordUser user, byte level, string reason, bool banUser = false)
			{
				if (user?.Id == context.User.Id)
				{
					await context.RespondAsync("You cannot insert yourself in the Trustlist.");
					return;
				}
				else if (user.IsBot)
				{
					await context.RespondAsync("You cannot insert a Bot in the Trustlist.");
					return;
				}
				else if ((user as DiscordMember)?.Roles.Any(r => r.Permissions == (r.Permissions & Permissions.ManageGuild)) ?? false)
				{
					await context.RespondAsync("You cannot insert a server operator in the Trustlist. Demote them first.");
					return;
				}

				if (reason.Length < 5)
				{
					await context.RespondAsync("Reason is too short");
				}
				else
				{
					try
					{
						GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);

						if (config.ApiLogin is not null)
						{
							await trustlist.SubmitEntryAsync(user.Id, new()
							{
								EscalationLevel = level,
								EscalationNote = reason
							}, await auth.GetOrUpdateAuthTokenAsync(context.Guild.Id));

							string userMention = (user as DiscordMember)?.Mention ?? user.Id.ToString();
							await context.RespondAsync($"User '{userMention}' successfully inserted into Trustlist.", await LookupAsync(user));

							if (banUser || (config.AutoBanBlacklisted && level >= 3))
							{
								await context.Guild.BanMemberAsync(user.Id, 0, $"[SocialGuard] {reason}");
								await context.Guild.GetChannel(config.BanLogChannel).SendMessageAsync($"Banned user '{userMention}'.");
							}
						}
						else
						{
							await context.RespondAsync($"No API Credentials set. Use ``{context.Prefix}sg config accesskey <key>`` to set an Access Key.");
						}
					}
					catch (ApplicationException e)
					{
						await context.RespondAsync(e.Message);
#if DEBUG
						throw;
#endif
					}
				}
			}

			public async Task RespondLookupAsync(CommandContext context, DiscordUser discordUser, bool silenceOnClear = false)
			{
				TrustlistUser user = await trustlist.LookupUserAsync(discordUser.Id);

				if (!silenceOnClear || user.GetMaxEscalationLevel() is not 0)
				{
					await context.RespondAsync(Utilities.BuildUserRecordEmbed(user?.Entries.Last(), discordUser));
				}
			}

			public async Task<DiscordEmbed> LookupAsync(DiscordUser user)
			{
				return Utilities.BuildUserRecordEmbed((await trustlist.LookupUserAsync(user.Id)).Entries.Last(), user);
			}
		}
	}
}
