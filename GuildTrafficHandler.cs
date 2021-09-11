﻿using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Microsoft.Extensions.Hosting;
using System.Threading;

namespace SocialGuard.YC
{
	public class GuildTrafficHandler : IHostedService
	{
		private readonly ILogger<GuildTrafficHandler> logger;
		private readonly DiscordClient discordClient;
		private readonly TrustlistUserApiService apiService;
		private readonly IMongoCollection<GuildConfig> configRepository;

		public GuildTrafficHandler(ILogger<GuildTrafficHandler> logger, DiscordClient discordClient, TrustlistUserApiService api, IDatabaseProvider<PluginManifest> database)
		{
			this.logger = logger;
			this.discordClient = discordClient;
			apiService = api;
			configRepository = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
		}

		public Task StartAsync(CancellationToken _)
		{
			discordClient.GuildMemberAdded += OnMemberJoinedAsync;
			logger.LogDebug("Hooked SocialGuard Joinlogs.");

			return Task.CompletedTask;
		}
		public Task StopAsync(CancellationToken _)
		{
			discordClient.GuildMemberAdded -= OnMemberJoinedAsync;
			logger.LogDebug("Unhooked SocialGuard Joinlogs.");

			return Task.CompletedTask;
		}


		public async Task OnMemberJoinedAsync(DiscordClient _, GuildMemberAddEventArgs e)
		{
			GuildConfig config = await configRepository.FindOrCreateConfigAsync(e.Member.Guild.Id);

			if (config.JoinLogChannel is not 0)
			{
				logger.LogDebug("Fetching Joinlog record for user {userId} in guild {guildId}.", e.Member.Id, e.Guild.Id);

				TrustlistUser user = await apiService.LookupUserAsync(e.Member.Id);
				TrustlistEntry entry = user?.GetLatestMaxEntry();
				byte maxEscalation = user?.GetMaxEscalationLevel() ?? 0;
				DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(user, e.Member);

				if (maxEscalation is 0 && config.SuppressJoinlogCleanRecords)
				{
					logger.LogDebug("Suppressed clean record for user {userId} in guild {guildId}.", e.Member.Id, e.Guild.Id);
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
