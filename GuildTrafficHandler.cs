using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using System.Linq;

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
				TrustlistUser user = await apiService.LookupUserAsync(e.Member.Id);
				TrustlistEntry entry = user?.GetLatestMaxEntry();
				DiscordChannel joinLog = e.Guild.GetChannel(config.JoinLogChannel);
				DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(user, e.Member);


				await joinLog.SendMessageAsync($"User **{e.Member.GetFullUsername()}** ({e.Member.Mention}) has joined the server.", entryEmbed);

				if (user?.GetMaxEscalationLevel() is not null and >= 3 && config.AutoBanBlacklisted)
				{				
					await e.Member.BanAsync(0, $"[SocialGuard] \n{entry.EscalationNote}");

					await e.Guild.GetChannel(config.BanLogChannel is not 0 ? config.BanLogChannel : config.JoinLogChannel)
						.SendMessageAsync($"User **{e.Member.GetFullUsername()}** ({e.Member.Mention}) banned on server join.", entryEmbed);
				}
			}
		}
	}
}
