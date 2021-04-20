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
			public async Task LookupAsync(CommandContext context, DiscordUser user) => await LookupAsync(context, user, user.Id);

			[Command("insert"), Aliases("add"), RequireGuild, RequireUserPermissions(Permissions.BanMembers)]
			public async Task InsertUserAsync(CommandContext context, DiscordUser user, [Range(0, 3)] byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, user.Id, level, reason);
			}

			[Command("ban"), RequireGuild, RequireUserPermissions(Permissions.BanMembers), RequireBotPermissions(Permissions.BanMembers)]
			public async Task BanUserAsync(CommandContext context, DiscordUser user, [Range(0, 3)] byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, user.Id, level, reason, true);
			}

			private async Task InsertUserAsync(CommandContext context, DiscordUser user, ulong userId, byte level, string reason, bool banUser = false)
			{
				if (user is not null)
				{
					if (user?.Id == context.User.Id)
					{
						await context.RespondAsync($"{context.User.Mention} You cannot insert yourself in the Trustlist.");
						return;
					}
					else if (user.IsBot)
					{
						await context.RespondAsync($"{context.User.Mention} You cannot insert a Bot in the Trustlist.");
						return;
					}
					else if ((user as DiscordMember).Roles.Any(r => r.Permissions == (r.Permissions & Permissions.ManageGuild)))
					{
						await context.RespondAsync($"You cannot insert a server operator in the Trustlist. Demote them first.");
						return;
					}
				}

				if (reason.Length < 5)
				{
					await context.RespondAsync($"{context.User.Mention} Reason is too short");
				}
				else
				{
					try
					{
						GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);

						if (config.ApiLogin is not null)
						{
							await trustlist.InsertOrEscalateUserAsync(new()
							{
								Id = userId,
								EscalationLevel = level,
								EscalationNote = reason
							}, await auth.GetOrUpdateAuthTokenAsync(context.Guild.Id));

							await context.RespondAsync($"User '{user?.Mention ?? userId.ToString()}' successfully inserted into Trustlist.");
							await LookupAsync(context, user, userId);

							if (banUser || (config.AutoBanBlacklisted && level >= 3))
							{
								await context.Guild.BanMemberAsync(userId, 0, $"[SocialGuard] {reason}");
								await context.Guild.GetChannel(config.BanLogChannel).SendMessageAsync($"Banned user '{user}'.");
							}
						}
						else
						{
							await context.RespondAsync("No API Credentials set. Use ``sg config accesskey <key>`` to set an Access Key.");
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

			public async Task LookupAsync(CommandContext context, DiscordUser user, ulong userId, bool silenceOnClear = false)
			{
				TrustlistUser entry = await trustlist.LookupUserAsync(userId);

				if (!silenceOnClear || entry.EscalationLevel is not 0)
				{
					await context.RespondAsync(embed: Utilities.BuildUserRecordEmbed(entry, user, userId));
				}
			}
		}
	}
}
