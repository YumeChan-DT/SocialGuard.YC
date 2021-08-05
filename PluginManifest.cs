using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.YC.Services.Security;
using System;
using System.Threading;
using System.Threading.Tasks;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace SocialGuard.YC
{
	public class PluginManifest : Plugin
	{
		public override string PluginDisplayName => "NSYS SocialGuard (YC)";
		public override bool PluginStealth => false;
		internal const string ApiConfigFileName = "api";

		private readonly ILogger<PluginManifest> logger;
		private readonly BroadcastsListener broadcastsListener;
		private readonly GuildTrafficHandler guildTrafficHandler;

		internal Uri ApiPath { get; private set; }
		internal static string VersionString { get; private set; }


		public PluginManifest(ILogger<PluginManifest> logger, IConfigProvider<IApiConfig> configProvider, BroadcastsListener broadcastsListener, GuildTrafficHandler guildTrafficHandler)
		{
			VersionString ??= PluginVersion;
			this.logger = logger;
			this.broadcastsListener = broadcastsListener;
			this.guildTrafficHandler = guildTrafficHandler;
			IApiConfig apiConfig = configProvider.InitConfig(ApiConfigFileName).PopulateApiConfig();
			ApiPath = new(apiConfig.ApiHost);
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
	}

	public class DependencyRegistrations : InjectionRegistry
	{
		public override IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddSingleton<GuildTrafficHandler>()
			.AddSingleton<BroadcastsListener>()
			.AddSingleton<TrustlistUserApiService>()
			.AddSingleton<AuthApiService>()
			.AddSingleton<EncryptionService>()
//			.AddSingleton((services) => services.GetRequiredService<IConfigProvider<IApiConfig>>().InitConfig(ApiConfigFileName).PopulateApiConfig())
			;
	}
}
