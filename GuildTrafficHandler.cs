using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;

namespace SocialGuard.YC
{
	public class GuildTrafficHandler
	{
		private readonly TrustlistUserApiService apiService;
		private readonly IEntityRepository<GuildConfig, ulong> configRepository;

		public GuildTrafficHandler(TrustlistUserApiService api, IDatabaseProvider<PluginManifest> database)
		{
			apiService = api;
			configRepository = database.GetEntityRepository<GuildConfig, ulong>();
		}


		public async Task OnMemberJoinedAsync(DiscordClient _, GuildMemberAddEventArgs e)
		{
			GuildConfig config = await configRepository.FindOrCreateConfigAsync(e.Member.Guild.Id);

			if (config.JoinLogChannel is not 0)
			{
				TrustlistUser entry = await apiService.LookupUserAsync(e.Member.Id);
				DiscordChannel joinLog = e.Guild.GetChannel(config.JoinLogChannel);
				DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(entry, e.Member);


				await joinLog.SendMessageAsync($"User **{e.Member}** ({e.Member.Mention}) has joined the server.", entryEmbed);

				if (entry?.EscalationLevel >= 3 && config.AutoBanBlacklisted)
				{				
					await e.Member.BanAsync(0, $"[SocialGuard] \n{entry.EscalationNote}");

					await e.Guild.GetChannel(config.BanLogChannel is not 0 ? config.BanLogChannel : config.JoinLogChannel)
						.SendMessageAsync($"User **{e.Member}** ({e.Member.Mention}) banned on server join.", entryEmbed);
				}
			}
		}
	}
}
