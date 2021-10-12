using DSharpPlus.CommandsNext;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands.Attributes;
using MongoDB.Driver;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools.Data;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext.Attributes;
using SocialGuard.Common.Services;
using SocialGuard.Common.Data.Models;

namespace SocialGuard.YC.Modules
{
	public partial class BaseSlashModule
	{
		[SlashCommandGroup("emitter", "Provides Emitter lookup and configuration commands.")]
		public class EmitterSlashModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly EmitterClient _emitterClient;
			private readonly AuthApiService authService;

			public EmitterSlashModule(EmitterClient emitterClient, AuthApiService authService, IDatabaseProvider<PluginManifest> database)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				_emitterClient = emitterClient;
				this.authService = authService;
			}

			[SlashCommand("info", "Displays information on current Emitter info for guild."), SlashRequireGuild, SlashRequireUserPermissions(Permissions.ManageGuild)]
			public async Task GetEmitterAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);

				if (config.ApiLogin is null)
				{
					await ctx.FollowUpAsync("Please set API Credentials first.");
					return;
				}

				Emitter emitter = await _emitterClient.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(ctx.Guild.Id));

				if (emitter is null)
				{
					await ctx.FollowUpAsync("No Emitter set for provided credentials.");
				}
				else
				{
					await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(Utilities.BuildEmitterEmbed(emitter)));
				}
			}

			[SlashCommand("set-server", "Configures Emitter profile based on guild information."), SlashRequireGuild, RequireUserPermissions(Permissions.ManageGuild)]
			public async Task SetServerEmitterAsync(InteractionContext ctx)
			{
				await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new() { IsEphemeral = true });

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(ctx.Guild.Id);

				if (config.ApiLogin is null)
				{
					await ctx.FollowUpAsync("Please set API Credentials first.");
					return;
				}

				await _emitterClient.SetEmitterAsync(new()
				{
					DisplayName = ctx.Guild.Name,
					EmitterType = EmitterType.Server,
					Snowflake = ctx.Guild.Id
				}, await authService.GetOrUpdateAuthTokenAsync(ctx.Guild.Id));

				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder { Content = "Emitter successfully set :" }
					.AddEmbed(Utilities.BuildEmitterEmbed(await _emitterClient.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(ctx.Guild.Id)))));
			}
		}
	}
}
