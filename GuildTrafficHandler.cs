using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace SocialGuard.YC
{
	public class GuildTrafficHandler
	{
		private readonly ILogger<GuildTrafficHandler> logger;
		private readonly TrustlistUserApiService apiService;
		private readonly IMongoCollection<GuildConfig> configRepository;

		public GuildTrafficHandler(ILogger<GuildTrafficHandler> logger, TrustlistUserApiService api, IDatabaseProvider<PluginManifest> database)
		{
			this.logger = logger;
			apiService = api;
			configRepository = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
		}


		public async Task OnMemberJoinedAsync(DiscordClient _, GuildMemberAddEventArgs e)
		{
			GuildConfig config = await configRepository.FindOrCreateConfigAsync(e.Member.Guild.Id);

			if (config.JoinLogChannel is not 0)
			{
				logger.LogDebug("Fetching Joinlog record for user {0} in guild {1}.", e.Member.Id, e.Guild.Id);

				TrustlistUser user = await apiService.LookupUserAsync(e.Member.Id);
				TrustlistEntry entry = user?.GetLatestMaxEntry();
				byte maxEscalation = user?.GetMaxEscalationLevel() ?? 0;
				DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(user, e.Member);

				if (maxEscalation is 0 && config.SuppressJoinlogCleanRecords)
				{
					logger.LogDebug("Suppressed clean record for user {0} in guild {1}.", e.Member.Id, e.Guild.Id);
				}
				else
				{
					DiscordChannel joinLog = e.Guild.GetChannel(config.JoinLogChannel);
					await joinLog.SendMessageAsync($"User **{e.Member.GetFullUsername()}** ({e.Member.Mention}) has joined the server.", entryEmbed);
				}

				if (maxEscalation >= 3 && config.AutoBanBlacklisted)
				{
					await e.Member.BanAsync(0, $"[SocialGuard] \n{entry.EscalationNote}");

					await e.Guild.GetChannel(config.BanLogChannel is not 0 ? config.BanLogChannel : config.JoinLogChannel)
						.SendMessageAsync($"User **{e.Member.GetFullUsername()}** ({e.Member.Mention}) banned on server join.", entryEmbed);
				}
			}
		}
	}
}
