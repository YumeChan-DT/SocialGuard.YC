using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services.Security;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models.Api;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.CommandsNext.Attributes;
using System;

namespace SocialGuard.YC.Modules
{
	public partial class BaseSlashModule
	{
		[SlashCommandGroup("config", "Provides configuration for SocialGuard integration"), SlashRequireGuild, SlashRequireUserPermissions(Permissions.ManageGuild)]
		public class GuildConfigSlashModule : ApplicationCommandModule
		{
			internal static DiscordSelectComponentOption EmptySelectionOption { get; } = new("None", "0", null, true);

			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly AuthApiService auth;
			private readonly IEncryptionService encryption;

			public GuildConfigSlashModule(IDatabaseProvider<PluginManifest> database, AuthApiService auth, IEncryptionService encryption)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				this.auth = auth;
				this.encryption = encryption;
			}


			[SlashCommand("login", "Sets credentials for this guild's authentication to SocialGuard.")]
			public async Task SetLoginAsync(InteractionContext ctx,
				[Option("username", "Account Username")] string username,
				[Option("password", "Account Password")] string password)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);
				config.ApiLogin = new(username, encryption.Encrypt(password));

				await guildConfig.UpdateOneAsync(
					Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
					Builders<GuildConfig>.Update.Set(c => c.ApiLogin, config.ApiLogin));

				await ctx.FollowUpAsync($"API credentials has been set.", true);
			}

			[SlashCommand("register", "Registers a new API account, for authentication to SocialGuard.")]
			public async Task SetLoginAsync(InteractionContext ctx,
				[Option("username", "Account Username")] string username,
				[Option("email", "Account Email"), EmailAddress] string email,
				[Option("password", "Account Password")] string password)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				AuthRegisterCredentials credentials = new(username, email, password);
				AuthResponse<IAuthComponent> result = await auth.RegisterNewUserAsync(credentials);

				await ctx.FollowUpAsync($"{ctx.User.Mention} {result.Status} : {result.Message}\n");
			}

			[SlashCommand("joinlog", "Configures the Join-log channel."), RequireBotPermissions(Permissions.SendMessages)]
			public async Task ConfigureJoinlogAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);
				DiscordChannel current = ctx.Guild.GetChannel(config.JoinLogChannel);

				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder() { Content = $"Current Joinlog channel : {current?.Mention ?? "None"}." }
					.AddComponents(new DiscordSelectComponent("sg-joinlog-select", "Select Joinlog channel",
						GetWritableChannelOptions(ctx.Guild).Prepend(EmptySelectionOption), maxOptions: 1)));
			}

			[SlashCommand("banlog", "Configures the Banlog channel."), RequireBotPermissions(Permissions.SendMessages)]
			public async Task ConfigureBanlogAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);
				DiscordChannel current = ctx.Guild.GetChannel(config.JoinLogChannel);

				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder() { Content = $"Current Banlog channel : {current?.Mention ?? "None"}." }
					.AddComponents(new DiscordSelectComponent("sg-banlog-select", "Select Banlog channel",
						GetWritableChannelOptions(ctx.Guild).Prepend(EmptySelectionOption), maxOptions: 1)));
			}


			[SlashCommand("autoban", "Configures the auto-ban mechanism on Blacklist/N+3 entries.")]
			public async Task ConfigureAutobanAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);
				DiscordChannel current = ctx.Guild.GetChannel(config.JoinLogChannel);

				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder() { Content = $"Autoban is currently {(config.AutoBanBlacklisted ? "on" : "off")}." }
					.AddComponents(new DiscordSelectComponent("sg-autoban-select", "Turn Autoban on/off", GetToggleOptions(), maxOptions: 1)));
			}

			[SlashCommand("joinlog-suppress", "Configures the Joinlog suppression on Clean/empty records.")]
			public async Task ConfigureJoinlogSuppressAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);
				DiscordChannel current = ctx.Guild.GetChannel(config.JoinLogChannel);

				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
				{
					Content = config.SuppressJoinlogCleanRecords
						? "All clean records are currently suppressed from displaying in Joinlog."
						: "All records are displayed in Joinlog."
				}
					.AddComponents(new DiscordSelectComponent("sg-joinlog-suppress-select", "Turn Autoban on/off", GetToggleOptions(), maxOptions: 1)));
			}

			protected static IEnumerable<DiscordSelectComponentOption> GetWritableChannelOptions(DiscordGuild guild) =>
				(from c in guild.Channels.Values
				where c.Type is ChannelType.Text
				where c.PermissionsFor(guild.CurrentMember).HasPermission(Permissions.AccessChannels | Permissions.SendMessages)
				select new DiscordSelectComponentOption('#' + c.Name, c.Id.ToString(), c.Parent?.Name))
				.OrderBy(c => c.Description).ThenBy(c => c.Label);

			protected static IEnumerable<DiscordSelectComponentOption> GetToggleOptions() => new DiscordSelectComponentOption[]
			{
				new("On", "1"),
				new("Off", "0")
			};
		}
	}
}
