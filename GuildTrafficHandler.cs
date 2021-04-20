using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;



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


		public async Task OnMemberJoinedAsync(DiscordMember user)
		{
			GuildConfig config = await configRepository.FindOrCreateConfigAsync(user.Guild.Id);

			if (config.JoinLogChannel is not 0)
			{
				TrustlistUser entry = await apiService.LookupUserAsync(user.Id);
				DiscordChannel joinLog = user.Guild.GetChannel(config.JoinLogChannel);
				DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(entry, user, user.Id);


				await joinLog.SendMessageAsync($"User **{user}** ({user.Mention}) has joined the server.", embed: entryEmbed);

				if (entry?.EscalationLevel >= 3 && config.AutoBanBlacklisted)
				{				
					await user.BanAsync(0, $"[SocialGuard] \n{entry.EscalationNote}");

					await user.Guild.GetChannel(config.BanLogChannel is not 0 ? config.BanLogChannel : config.JoinLogChannel)
						.SendMessageAsync($"User **{user}** ({user.Mention}) banned on server join.", embed: entryEmbed);
				}
			}
		}
	}
}
