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
			"sg-banlog-select" => HandleBanlogConfigurationAsync(e),
			"sg-autoban-select" => HandleAutobanConfigurationAsync(e),
			"sg-joinlog-suppress-select" => HandleJoinlogSuppressConfigurationAsync(e),

			_ => Task.CompletedTask
		};


		public async Task HandleJoinlogConfigurationAsync(ComponentInteractionCreateEventArgs e)
		{
			await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

			if ((await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
			{
				DiscordChannel channel = null;
				GuildConfig config = await configCollection.FindOrCreateConfigAsync(e.Guild.Id);

				if (e.Values?.First() is not "0")
				{
					channel = e.Guild.GetChannel(ulong.Parse(e.Values.First()));

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
					IsEphemeral = true
				});

				e.Handled = true;
			}
		}

		public async Task HandleBanlogConfigurationAsync(ComponentInteractionCreateEventArgs e)
		{
			await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

			if ((await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
			{
				DiscordChannel channel = null;
				GuildConfig config = await configCollection.FindOrCreateConfigAsync(e.Guild.Id);

				if (e.Values?.First() is not "0")
				{
					channel = e.Guild.GetChannel(ulong.Parse(e.Values.First()));

					if (channel is not null)
					{
						config.BanLogChannel = channel.Id;
					}
				}
				else
				{
					config.JoinLogChannel = default;
				}

				await configCollection.SetJoinlogAsync(config);

				await e.Interaction.CreateFollowupMessageAsync(new()
				{
					Content = $"({e.User.Mention}) Edited Banlog channel to {channel?.Mention ?? "None"}.",
					IsEphemeral = true
				});

				e.Handled = true;
			}
		}

		public async Task HandleAutobanConfigurationAsync(ComponentInteractionCreateEventArgs e)
		{
			await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

			if ((await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
			{
				string value = e.Values.First();
				GuildConfig config = await configCollection.FindOrCreateConfigAsync(e.Guild.Id);

				config.AutoBanBlacklisted = value switch
				{
					"0" => false,
					"1" => true,
					_ => config.AutoBanBlacklisted
				};

				await configCollection.SetJoinlogAsync(config);

				await e.Interaction.CreateFollowupMessageAsync(new()
				{
					Content = $"({e.User.Mention}) Autoban setting is now {(config.AutoBanBlacklisted ? "on" : "off")}.",
					IsEphemeral = true
				});

				e.Handled = true;
			}
		}

		public async Task HandleJoinlogSuppressConfigurationAsync(ComponentInteractionCreateEventArgs e)
		{
			await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

			if ((await e.Guild.GetMemberAsync(e.User.Id)).Permissions.HasPermission(Permissions.ManageGuild))
			{
				string value = e.Values.First();
				GuildConfig config = await configCollection.FindOrCreateConfigAsync(e.Guild.Id);

				config.SuppressJoinlogCleanRecords = value switch
				{
					"0" => false,
					"1" => true,
					_ => config.SuppressJoinlogCleanRecords
				};

				await configCollection.SetJoinlogAsync(config);

				await e.Interaction.CreateFollowupMessageAsync(new()
				{
					Content = config.SuppressJoinlogCleanRecords
						? "All clean records will now be suppressed from displaying in Joinlog."
						: "All records will now be displayed in Joinlog.",

					IsEphemeral = true
				});

				e.Handled = true;
			}
		}
	}
}
