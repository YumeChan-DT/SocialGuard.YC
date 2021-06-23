using DnsClient.Internal;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using System.Threading.Tasks;
using YumeChan.PluginBase.Tools.Data;



namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("debug")]
		public class DebugModule : BaseCommandModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly ILogger<DebugModule> logger;
			private readonly AuthApiService authService;

			public DebugModule(ILogger<DebugModule> logger, AuthApiService authService, IDatabaseProvider<PluginManifest> database)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				this.logger = logger;
				this.authService = authService;
			}

			[Command("clear-token"), RequireUserPermissions(Permissions.Administrator)]
			public async Task ClearTokenAsync(CommandContext ctx)
			{
				await authService.ClearTokenAsync(ctx.Guild.Id);
				await ctx.RespondAsync("Auth Token successfully cleared.");
				logger.LogDebug("Auth Token force-cleared for guild {guildId}.", ctx.Guild.Id);
			}

			[Command("force-login")]
			public async Task ForceLoginAsync(CommandContext ctx)
			{
				await authService.ClearTokenAsync(ctx.Guild.Id);
				await authService.GetOrUpdateAuthTokenAsync(ctx.Guild.Id);
				await ctx.RespondAsync("Auth Token successfully refreshed.");
				logger.LogDebug("Auth Token refreshed for guild {guildId}.", ctx.Guild.Id);
			}
		}
	}
}
