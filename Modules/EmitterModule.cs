using Discord;
using Discord.Commands;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Transcom.SocialGuard.YC.Data.Models;
using Transcom.SocialGuard.YC.Data.Models.Config;
using Transcom.SocialGuard.YC.Services;

namespace Transcom.SocialGuard.YC.Modules
{
	[Group("sg emitter")]
	public class EmitterModule : ModuleBase<SocketCommandContext>
	{
		private readonly IEntityRepository<GuildConfig, ulong> guildConfig;
		private readonly EmitterApiService emitterService;
		private readonly AuthApiService authService;

		public EmitterModule(EmitterApiService emitterService, AuthApiService authService, IDatabaseProvider<PluginManifest> database)
		{
			guildConfig = database.GetEntityRepository<GuildConfig, ulong>();
			this.emitterService = emitterService;
			this.authService = authService;
		}

		[Command("info"), Alias("")]
		public async Task GetEmitterAsync()
		{
			GuildConfig config = await guildConfig.FindOrCreateConfigAsync(Context.Guild.Id);

			if (config.ApiLogin is null)
			{
				await ReplyAsync("Please set API Credentials first.");
				return;
			}

			Emitter emitter = await emitterService.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(Context.Guild.Id));

			if (emitter is null)
			{
				await ReplyAsync("No Emitter set for provided credentials.");
			}
			else
			{
				await ReplyAsync(embed: BuildEmitterEmbed(emitter));
			}
		}

		[Command("set server"), Alias("set")]
		public async Task SetServerEmitterAsync()
		{
			GuildConfig config = await guildConfig.FindOrCreateConfigAsync(Context.Guild.Id);

			if (config.ApiLogin is null)
			{
				await ReplyAsync("Please set API Credentials first.");
				return;
			}

			await emitterService.SetEmitterAsync(new()
			{
				DisplayName = Context.Guild.Name,
				EmitterType = EmitterType.Server,
				Snowflake = Context.Guild.Id
			}, await authService.GetOrUpdateAuthTokenAsync(Context.Guild.Id));

			await ReplyAsync(
				"Emitter successfully set :", 
				embed: BuildEmitterEmbed(await emitterService.GetEmitterAsync(await authService.GetOrUpdateAuthTokenAsync(Context.Guild.Id))));
		}

		public static Embed BuildEmitterEmbed(Emitter emitter) => new EmbedBuilder()
					.WithTitle("Emitter Info")
					.WithDescription(emitter.DisplayName)
					.AddField("Username", emitter.Login)
					.AddField("Type", EmitterTypeToString(emitter.EmitterType), true)
					.AddField("Discord ID", emitter.Snowflake is 0 ? "N/A" : emitter.Snowflake, true)
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
