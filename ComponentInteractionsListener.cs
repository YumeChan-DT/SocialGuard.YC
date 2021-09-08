using DSharpPlus.EventArgs;
using DSharpPlus;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using YumeChan.PluginBase.Tools.Data;
using System.Linq;
using SocialGuard.YC.Services;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace SocialGuard.YC
{
	public class ComponentInteractionsListener : IHostedService
	{
		private readonly IMongoCollection<GuildConfig> configCollection;
		private readonly ILogger<GuildTrafficHandler> logger;
		private readonly DiscordClient discordClient;

		public ComponentInteractionsListener(ILogger<GuildTrafficHandler> logger, DiscordClient discordClient, IDatabaseProvider<PluginManifest> database)
		{
			configCollection = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
			this.logger = logger;
			this.discordClient = discordClient;
		}


		public Task StartAsync(CancellationToken cancellationToken)
		{
			discordClient.ComponentInteractionCreated += OnComponentInteractionCreatedAsync;

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			discordClient.ComponentInteractionCreated -= OnComponentInteractionCreatedAsync;

			return Task.CompletedTask;
		}

		public Task OnComponentInteractionCreatedAsync(DiscordClient sender, ComponentInteractionCreateEventArgs e) => e.Id switch
		{
			"sg-joinlog-select" => HandleJoinlogConfigurationAsync(e),

			_ => Task.CompletedTask
		};


		public async Task HandleJoinlogConfigurationAsync(ComponentInteractionCreateEventArgs e)
		{
			e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

			if ((await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
			{
				DiscordChannel channel = null;
				GuildConfig config = await configCollection.FindOrCreateConfigAsync(e.Guild.Id);

				if (e.Values.Any())
				{
					channel = e.Guild.GetChannel(ulong.Parse(e.Values.FirstOrDefault()));

					if (channel is not null)
					{
						config.JoinLogChannel = channel.Id;
					}
				}
				else
				{
					config.JoinLogChannel = default;
				}

				await configCollection.SetJoinlogAsync(config);

				await e.Interaction.CreateFollowupMessageAsync(new()
				{
					Content = $"({e.User.Mention}) Edited Joinlog channel to {channel?.Mention ?? "None"}.",
					IsEphemeral = false
				});

				e.Handled = true;
			}
		}
	}
}
