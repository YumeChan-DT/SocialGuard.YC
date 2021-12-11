﻿using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SocialGuard.Common.Data.Models;
using SocialGuard.Common.Hubs;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YumeChan.PluginBase.Tools;
using YumeChan.PluginBase.Tools.Data;

namespace SocialGuard.YC.Services
{
	public class BroadcastsListener : IHostedService
	{
		private readonly ILogger<BroadcastsListener> logger;
		private readonly DiscordClient discordClient;
		private readonly TrustlistClient _trustlistClient;
		private readonly HubConnection hubConnection;
		private readonly IMongoCollection<GuildConfig> guildConfig;

		public BroadcastsListener(ILogger<BroadcastsListener> logger, IConfigProvider<IApiConfig> configProvider,
			DiscordClient discordClient, IDatabaseProvider<PluginManifest> database, TrustlistClient trustlistUserApiService)
		{
			IApiConfig config = configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig();
			this.logger = logger;
			this.discordClient = discordClient;
			this._trustlistClient = trustlistUserApiService;
			guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));

			hubConnection = new HubConnectionBuilder()
				.WithUrl(config.ApiHost + "/hubs/trustlist")
				.AddMessagePackProtocol()
				.WithAutomaticReconnect()
				.Build();

			void LogBroadcast(string name, ulong userid) => logger.LogDebug("Received SignalR Boradcast: {name} {userId}", name, userid);

			hubConnection.On<ulong, TrustlistEntry>(nameof(ITrustlistHubPush.NotifyNewEntry), async (userId, entry) =>
			{
				LogBroadcast(nameof(ITrustlistHubPush.NotifyNewEntry), userId);
				await BroadcastUpdateAsync(BroadcastUpdateType.NewEntry, userId, entry);
			});

			hubConnection.On<ulong, TrustlistEntry, byte>(nameof(ITrustlistHubPush.NotifyEscalatedEntry), async (userId, entry, level) =>
			{
				LogBroadcast(nameof(ITrustlistHubPush.NotifyEscalatedEntry), userId);
				await BroadcastUpdateAsync(BroadcastUpdateType.Escalation, userId, entry);
			});

			hubConnection.On<ulong, TrustlistEntry>(nameof(ITrustlistHubPush.NotifyDeletedEntry), (userId, entry) =>
			{
				LogBroadcast(nameof(ITrustlistHubPush.NotifyDeletedEntry), userId);
			});
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Loop is here to wait until the server is running
			while (hubConnection.State is not HubConnectionState.Connected)
			{
				try
				{
					await hubConnection.StartAsync(cancellationToken);
					logger.LogInformation("Now listening Trustlist Hub. (Connection ID {id}; State {state})", hubConnection.ConnectionId, hubConnection.State);
				}
				catch
				{
					await Task.Delay(1000, cancellationToken);
				}
			}
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await hubConnection.StopAsync(cancellationToken);
			logger.LogInformation("Stopped Trustlist Hub connection (Connection ID {id}; State {state})", hubConnection.ConnectionId, hubConnection.State);
			await hubConnection.DisposeAsync();
		}


		protected async Task BroadcastUpdateAsync(BroadcastUpdateType type, ulong userId, TrustlistEntry entry)
		{
			TrustlistUser trustlistUser = null;
			DiscordEmbed embed = null;

			using IAsyncCursor<GuildConfig> guilds = await guildConfig.FindAsync(c => c.BanLogChannel != 0 || c.JoinLogChannel != 0);

			await foreach (GuildConfig guildConfig in guilds.ToAsyncEnumerable())
			{
				try
				{
					DiscordGuild guild = await discordClient.GetGuildAsync(guildConfig.Id);

					if (guild.Members.GetValueOrDefault(userId) is DiscordMember member)
					{
						trustlistUser ??= await _trustlistClient.LookupUserAsync(userId);
						embed ??= Utilities.BuildUserRecordEmbed(trustlistUser, member, entry);
						DiscordChannel actionLogChannel = guild.GetChannel(guildConfig.BanLogChannel is not 0 ? guildConfig.BanLogChannel : guildConfig.JoinLogChannel);

						await actionLogChannel.SendMessageAsync(embed: embed, content: type switch
						{
							BroadcastUpdateType.NewEntry => $"New Entry added by Emitter **{entry.Emitter.DisplayName}** (`{entry.Emitter.Login}`) for user {member.Mention} :",
							BroadcastUpdateType.Escalation => $"Entry by Emitter **{entry.Emitter.DisplayName}** (`{entry.Emitter.Login}`) was escalated for user {member.Mention} :",
							_ => throw new NotImplementedException()
						});

						if (trustlistUser.GetMaxEscalationLevel() >= 3 && guildConfig.AutoBanBlacklisted)
						{
							await member.BanAsync(0, $"[SocialGuard] \n{entry.EscalationNote}");
							await actionLogChannel.SendMessageAsync($"User **{member.GetFullUsername()}** ({member.Mention}) banned from Autoban (entry sync).");
						}
					}
				}
				catch (Exception e)
				{
					logger.LogError("Broadcasting record {userId} to guild {guildId} threw following exception: \n{e}", userId, guildConfig.Id, e);
					continue;
				}
			}
		}
	}

	public enum BroadcastUpdateType
	{
		NewEntry,
		Escalation,
		Removal
	}
}
