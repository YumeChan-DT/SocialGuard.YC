using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using YumeChan.PluginBase.Tools.Data;
using MongoDB.Driver;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.Common.Data.Models;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("emitter")]
		public class EmitterModule : BaseCommandModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly EmitterClient _emitterClient;
			private readonly ApiAuthService _apiAuthService;

			public EmitterModule(EmitterClient emitterClient, ApiAuthService apiAuthService, IDatabaseProvider<PluginManifest> database)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				_emitterClient = emitterClient;
				this._apiAuthService = apiAuthService;
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

				Emitter emitter = await _emitterClient.GetEmitterAsync(await _apiAuthService.GetOrUpdateAuthTokenAsync(context.Guild.Id));

				if (emitter is null)
				{
					await context.RespondAsync("No Emitter set for provided credentials.");
				}
				else
				{
					await context.RespondAsync(embed: Utilities.BuildEmitterEmbed(emitter));
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

				await _emitterClient.SetEmitterAsync(new()
				{
					DisplayName = context.Guild.Name,
					EmitterType = EmitterType.Server,
					Snowflake = context.Guild.Id
				}, await _apiAuthService.GetOrUpdateAuthTokenAsync(context.Guild.Id));

				await context.RespondAsync("Emitter successfully set :",
					Utilities.BuildEmitterEmbed(await _emitterClient.GetEmitterAsync(await _apiAuthService.GetOrUpdateAuthTokenAsync(context.Guild.Id))));
			}
		}
	}
}
