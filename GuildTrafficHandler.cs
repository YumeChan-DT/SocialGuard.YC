using SocialGuard.YC.Data.Models.Config;
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
using SocialGuard.Common.Services;
using SocialGuard.Common.Data.Models;

namespace SocialGuard.YC;

public class GuildTrafficHandler : IHostedService
{
	private readonly ILogger<GuildTrafficHandler> _logger;
	private readonly DiscordClient _discordClient;
	private readonly TrustlistClient _client;
	private readonly IMongoCollection<GuildConfig> _configRepository;

	public GuildTrafficHandler(ILogger<GuildTrafficHandler> logger, DiscordClient discordClient, TrustlistClient client, IDatabaseProvider<PluginManifest> database)
	{
		_logger = logger;
		_discordClient = discordClient;
		_client = client;
		_configRepository = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
	}

	public Task StartAsync(CancellationToken _)
	{
		_discordClient.GuildMemberAdded += OnMemberJoinedAsync;
		_discordClient.GuildMemberRemoved += OnMemberLeaveAsync;
		_logger.LogDebug("Hooked SocialGuard Join/Leave Logs.");

		return Task.CompletedTask;
	}
	public Task StopAsync(CancellationToken _)
	{
		_discordClient.GuildMemberAdded -= OnMemberJoinedAsync;
		_discordClient.GuildMemberRemoved -= OnMemberLeaveAsync;
		_logger.LogDebug("Unhooked SocialGuard Join/Leave Logs.");

		return Task.CompletedTask;
	}


	public async Task OnMemberJoinedAsync(DiscordClient _, GuildMemberAddEventArgs e)
	{
		GuildConfig config = await _configRepository.FindOrCreateConfigAsync(e.Member.Guild.Id);

		if (config.JoinLogChannel is not 0)
		{
			_logger.LogDebug("Fetching Joinlog record for user {userId} in guild {guildId}.", e.Member.Id, e.Guild.Id);

			TrustlistUser user = await _client.LookupUserAsync(e.Member.Id);
			TrustlistEntry entry = user?.GetLatestMaxEntry();
			byte maxEscalation = user?.GetMaxEscalationLevel() ?? 0;
			DiscordEmbed entryEmbed = Utilities.BuildUserRecordEmbed(user, e.Member);

			if (maxEscalation is 0 && config.SuppressJoinlogCleanRecords)
			{
				_logger.LogDebug("Suppressed clean record for user {userId} in guild {guildId}.", e.Member.Id, e.Guild.Id);
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

	public async Task OnMemberLeaveAsync(DiscordClient _, GuildMemberRemoveEventArgs e)
	{
		GuildConfig config = await _configRepository.FindOrCreateConfigAsync(e.Member.Guild.Id);

		if (config.LeaveLogEnabled)
		{
			DiscordChannel channel = e.Guild.GetChannel(config.LeaveLogChannel);
			await channel.SendMessageAsync($"User **{e.Member.GetFullUsername()}** ({e.Member.Mention}) has left the server.", Utilities.BuildLeaveEmbed(e.Member));
		}
	}
}
