using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Database.MongoDB;



namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("debug")]
		public class DebugModule : BaseCommandModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly ILogger<DebugModule> logger;
			private readonly ApiAuthService _apiAuthService;

			public DebugModule(ILogger<DebugModule> logger, ApiAuthService apiAuthService, IMongoDatabaseProvider<PluginManifest> database)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				this.logger = logger;
				this._apiAuthService = apiAuthService;
			}

			[Command("clear-token"), RequireUserPermissions(Permissions.Administrator)]
			public async Task ClearTokenAsync(CommandContext ctx)
			{
				await _apiAuthService.ClearTokenAsync(ctx.Guild.Id);
				await ctx.RespondAsync("Auth Token successfully cleared.");
				logger.LogDebug("Auth Token force-cleared for guild {guildId}.", ctx.Guild.Id);
			}

			[Command("force-login")]
			public async Task ForceLoginAsync(CommandContext ctx)
			{
				await _apiAuthService.ClearTokenAsync(ctx.Guild.Id);
				await _apiAuthService.GetOrUpdateAuthTokenAsync(ctx.Guild.Id);
				await ctx.RespondAsync("Auth Token successfully refreshed.");
				logger.LogDebug("Auth Token refreshed for guild {guildId}.", ctx.Guild.Id);
			}
		}
	}
}
