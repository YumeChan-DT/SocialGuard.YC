using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus;
using System.Linq;
using MongoDB.Driver;
using SocialGuard.Common.Services;
using SocialGuard.Common.Data.Models;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		public class TrustlistModule : BaseCommandModule
		{
			private readonly TrustlistClient _trustlist;
			private readonly ApiAuthService _apiAuth;
			private readonly IMongoCollection<GuildConfig> guildConfig;

			public TrustlistModule(TrustlistClient trustlist, ApiAuthService apiAuth, IDatabaseProvider<PluginManifest> databaseProvider)
			{
				this._trustlist = trustlist;
				this._apiAuth = apiAuth;
				guildConfig = databaseProvider.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
			}

			[Command("lookup"), Aliases("get")]
			public async Task LookupAsync(CommandContext context, DiscordUser user) => await RespondLookupAsync(context, user);

			[Command("insert"), Aliases("add"), RequireGuild, RequireUserPermissions(Permissions.BanMembers)]
			public async Task InsertUserAsync(CommandContext context, DiscordUser user, byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, level, reason, false);
			}

			[Command("ban"), RequireGuild, RequirePermissions(Permissions.BanMembers)]
			public async Task BanUserAsync(CommandContext context, DiscordUser user, [Range(0, 3)] byte level, [RemainingText] string reason)
			{
				await InsertUserAsync(context, user, level, reason, true);
			}

			private async Task InsertUserAsync(CommandContext context, DiscordUser user, byte level, string reason, bool banUser)
			{
				if (user?.Id == context.User.Id)
				{
					await context.RespondAsync("You cannot insert yourself in the Trustlist.");
				}
				else if (user.IsBot)
				{
					await context.RespondAsync("You cannot insert a Bot in the Trustlist.");
				}
				else if ((user as DiscordMember)?.Roles.Any(r => r.Permissions == (r.Permissions & Permissions.ManageGuild)) ?? false)
				{
					await context.RespondAsync("You cannot insert a server operator in the Trustlist. Demote them first.");
				}
				else if (reason.Length < 5)
				{
					await context.RespondAsync("Reason is too short");
				}
				else
				{
					try
					{
						GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);

						if (config.ApiLogin is not null)
						{
							await _trustlist.SubmitEntryAsync(user.Id, new()
							{
								EscalationLevel = level,
								EscalationNote = reason
							}, (await _apiAuth.GetOrUpdateAuthTokenAsync(context.Guild.Id)).Token);

							string userMention = (user as DiscordMember)?.Mention ?? user.Id.ToString();

							DiscordEmbed embed = await _trustlist.GetLookupEmbedAsync(user);
							await context.RespondAsync($"User '{userMention}' successfully inserted into Trustlist.", embed);

							if (banUser || (config.AutoBanBlacklisted && level >= 3))
							{
								await context.Guild.BanMemberAsync(user.Id, 0, $"[SocialGuard] {reason}");
								await context.Guild.GetChannel(config.BanLogChannel).SendMessageAsync($"Banned user '{userMention}'.", embed);
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
				TrustlistUser user = await _trustlist.LookupUserAsync(discordUser.Id);

				if (!silenceOnClear || user.GetMaxEscalationLevel() is not 0)
				{
					await context.RespondAsync(Utilities.BuildUserRecordEmbed(user, discordUser));
				}
			}


		}
	}
}
