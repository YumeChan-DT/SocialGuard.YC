using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using YumeChan.PluginBase.Tools.Data;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("emitter")]
		public class EmitterModule : BaseCommandModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly EmitterApiService emitterService;
			private readonly AuthApiService authService;

			public EmitterModule(EmitterApiService emitterService, AuthApiService authService, IDatabaseProvider<PluginManifest> database)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				this.emitterService = emitterService;
				this.authService = authService;
			}

			[Command("info"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task GetEmitterAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);

				if (config.ApiLogin is null)
				{
					await context.RespondAsync("Please set API Credentials first.");
					return;
				}

				Emitter emitter = await emitterService.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(context.Guild.Id));

				if (emitter is null)
				{
					await context.RespondAsync("No Emitter set for provided credentials.");
				}
				else
				{
					await context.RespondAsync(embed: BuildEmitterEmbed(emitter));
				}
			}

			[Command("set-server"), Aliases("set"), RequireGuild, RequireUserPermissions(Permissions.ManageGuild)]
			public async Task SetServerEmitterAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);

				if (config.ApiLogin is null)
				{
					await context.RespondAsync("Please set API Credentials first.");
					return;
				}

				await emitterService.SetEmitterAsync(new()
				{
					DisplayName = context.Guild.Name,
					EmitterType = EmitterType.Server,
					Snowflake = context.Guild.Id
				}, await authService.GetOrUpdateAuthTokenAsync(context.Guild.Id));

				await context.RespondAsync(
					"Emitter successfully set :",
					embed: BuildEmitterEmbed(await emitterService.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(context.Guild.Id))));
			}

			public static DiscordEmbed BuildEmitterEmbed(Emitter emitter) => new DiscordEmbedBuilder()
						.WithTitle("Emitter Info")
						.WithDescription(emitter.DisplayName)
						.AddField("Username", emitter.Login)
						.AddField("Type", EmitterTypeToString(emitter.EmitterType), true)
						.AddField("Discord ID", emitter.Snowflake is 0 ? "N/A" : emitter.Snowflake.ToString(), true)
						.WithFooter(Utilities.SignatureFooter)
						.Build();

			public static string EmitterTypeToString(EmitterType type) => type switch
			{
				EmitterType.User => "User",
				EmitterType.Server => "Server",
				_ => "Unknown"
			};
		}
	}
}
