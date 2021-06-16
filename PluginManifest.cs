using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using YumeChan.PluginBase.Tools;
using YumeChan.PluginBase.Tools.Data;
using System.Net.Http;
using System.Threading.Tasks;
using SocialGuard.YC.Services.Security;
using DSharpPlus;
using System.Threading;
using System;

namespace SocialGuard.YC
{
	public class PluginManifest : YumeChan.PluginBase.Plugin
	{
		public override string PluginDisplayName => "NSYS SocialGuard (YC)";
		public override bool PluginStealth => false;

		internal const string ApiConfigFileName = "api";
		
		private readonly ILogger<PluginManifest> logger;
		private readonly BroadcastsListener broadcastsListener;
		private readonly GuildTrafficHandler guildTrafficHandler;
		private readonly DiscordClient coreClient;

		internal Uri ApiPath { get; private set; }
		internal static string VersionString { get; private set; }


		public PluginManifest(DiscordClient client, ILoggerFactory loggerFactory, BroadcastsListener broadcastsListener, 
			GuildTrafficHandler guildTrafficHandler, TrustlistUserApiService trustlistUserApiService)
		{
			VersionString ??= PluginVersion;
			coreClient = client;
			logger = loggerFactory.CreateLogger<PluginManifest>();
			this.broadcastsListener = broadcastsListener;
			this.guildTrafficHandler = guildTrafficHandler;
			ApiPath = trustlistUserApiService.BaseAddress;
		}

		public override async Task LoadPlugin() 
		{
			CancellationToken cancellationToken = CancellationToken.None; // May get added into method parameters later on.

			await broadcastsListener.StartAsync(cancellationToken);
			await guildTrafficHandler.StartAsync(cancellationToken);


			await base.LoadPlugin();

			logger.LogInformation("Loaded {plugin}.", PluginDisplayName);
			logger.LogInformation("Current SocialGuard API Path: {apiPath}", ApiPath);
		}


		public override async Task UnloadPlugin()
		{
			CancellationToken cancellationToken = CancellationToken.None; // May get added into method parameters later on.	

			await broadcastsListener.StopAsync(cancellationToken);
			await guildTrafficHandler.StopAsync(cancellationToken);

			logger.LogInformation("Unloaded {plugin}.", PluginDisplayName);
			await base.UnloadPlugin();
		}

		public override IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddHostedService<GuildTrafficHandler>()
			.AddHostedService<BroadcastsListener>()
			.AddSingleton<GuildTrafficHandler>()
			.AddSingleton<BroadcastsListener>()
			.AddSingleton<TrustlistUserApiService>()
			.AddSingleton<AuthApiService>()
			.AddSingleton<EncryptionService>()
			;
	}
}
