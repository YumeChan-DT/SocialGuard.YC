using Discord;
using Discord.Commands;
using Transcom.SocialGuard.YC.Data.Config;
using Transcom.SocialGuard.YC.Data.Models;
using Transcom.SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Transcom.SocialGuard.YC.Modules
{
	[Group("socialguard"), Alias("sg")]
	public class UserLookupModule : ModuleBase<ICommandContext>
	{
		private readonly ApiService service;
		private readonly IEntityRepository<GuildConfig, ulong> repository;

		public UserLookupModule(ApiService service, IDatabaseProvider<PluginManifest> databaseProvider)
		{
			this.service = service;
			repository = databaseProvider.GetEntityRepository<GuildConfig, ulong>();
		}


		[Command("lookup"), Priority(10)]
		public async Task LookupAsync(ulong userId)
		{
			IUser user = await Context.Client.GetUserAsync(userId);
			await LookupAsync(user, userId);
		}

		[Command("lookup")]
		public async Task LookupAsync(IUser user) => await LookupAsync(user, user.Id);

		[Command("insert"), Alias("add"), Priority(10), RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.BanMembers)]
		public async Task InsertUserAsync(ulong userId, [Range(0, 3)] byte level, [Remainder] string reason)
		{
			await InsertUserAsync(await Context.Guild.GetUserAsync(userId), userId, level, reason);
		}

		[Command("insert"), Alias("add"), RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.BanMembers)]
		public async Task InsertUserAsync(IGuildUser user, [Range(0, 3)] byte level, [Remainder] string reason) => await InsertUserAsync(user, user.Id, level, reason);

		private async Task InsertUserAsync(IGuildUser user, ulong userId, byte level, string reason, bool banUser = false)
		{
			if (user is not null)
			{
				if (user?.Id == Context.User.Id)
				{
					await ReplyAsync($"{Context.User.Mention} You cannot insert yourself in the Trustlist.");
				}
				else if (user.IsBot)
				{
					await ReplyAsync($"{Context.User.Mention} You cannot insert a Bot in the Trustlist.");
				}
				else if (user.GuildPermissions.ManageGuild)
				{
					await ReplyAsync($"{Context.User.Mention} You cannot insert a server operator in the Trustlist. Demote them first.");
				}
			}
			else if (reason.Length < 5)
			{
				await ReplyAsync($"{Context.User.Mention} Reason is too short");
			}

			else
			{
				try
				{
					GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);

					if (config.WriteAccessKey is string key and not null)
					{
						await service.InsertOrEscalateUserAsync(new()
						{
							Id = userId,
							EscalationLevel = level,
							EscalationNote = reason
						}, key);

						await ReplyAsync($"{Context.User.Mention} User '{user?.Mention ?? userId.ToString()}' successfully inserted into Trustlist.");
						await LookupAsync(user, userId);

						if (banUser || (config.AutoBanBlacklisted && level >= 3))
						{
							await user.BanAsync(0, $"[SocialGuard] {reason}");
							await (await Context.Guild.GetTextChannelAsync(config.BanLogChannel)).SendMessageAsync($"Banned user '{user}'.");
						}
					}
					else
					{
						await ReplyAsync($"{Context.User.Mention} No Access Key set. Use ``sg config accesskey <key>`` to set an Access Key.");
					}
				}
				catch (ApplicationException e)
				{
					await ReplyAsync($"{Context.User.Mention} {e.Message}");
#if DEBUG
					throw;
#endif
				}
			}
		}

		[Command("ban"), RequireContext(ContextType.Guild), RequireUserPermission(GuildPermission.BanMembers), RequireBotPermission(GuildPermission.BanMembers)]
		public async Task BanUserAsync(IGuildUser user, [Range(0, 3)] byte level, [Remainder] string reason) => await InsertUserAsync(user, user.Id, level, reason, true);

		public async Task LookupAsync(IUser user, ulong userId, bool silenceOnClear = false)
		{
			TrustlistUser entry = await service.LookupUserAsync(userId);

			if (!silenceOnClear || entry.EscalationLevel is not 0)
			{
				await ReplyAsync(Context.User.Mention, embed: Utilities.BuildUserRecordEmbed(entry, user, userId));
			}
		}
	}
}
