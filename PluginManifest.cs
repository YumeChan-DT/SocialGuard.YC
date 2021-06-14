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

namespace SocialGuard.YC
{
	public class PluginManifest : YumeChan.PluginBase.Plugin
	{
		public override string PluginDisplayName => "NSYS SocialGuard (YC)";
		public override bool PluginStealth => false;

		internal const string ApiConfigFileName = "api";
		
		private readonly ILogger<PluginManifest> logger;
		private readonly DiscordClient coreClient;

		internal static string VersionString { get; private set; }

		public GuildTrafficHandler GuildTrafficHandler { get; }


		public PluginManifest(DiscordClient client, ILogger<PluginManifest> manifestLogger, ILogger<GuildTrafficHandler> trafficLogger,
			IConfigProvider<IApiConfig> apiConfig, IDatabaseProvider<PluginManifest> database, IHttpClientFactory httpClientFactory)
		{
			VersionString ??= PluginVersion;
			coreClient = client;
			logger = manifestLogger;

			TrustlistUserApiService apiService = new(httpClientFactory, apiConfig);
			GuildTrafficHandler = new(trafficLogger, apiService, database);
		}

		public override async Task LoadPlugin() 
		{
			coreClient.GuildMemberAdded += GuildTrafficHandler.OnMemberJoinedAsync;

			await base.LoadPlugin();

			logger.LogInformation("Loaded {0}.", PluginDisplayName);
		}


		public override async Task UnloadPlugin()
		{
			coreClient.GuildMemberAdded -= GuildTrafficHandler.OnMemberJoinedAsync;

			logger.LogInformation("Unloaded {0}.", PluginDisplayName);
			await base.UnloadPlugin();
		}

		public override IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddSingleton<TrustlistUserApiService>()
			.AddSingleton<AuthApiService>()
			.AddSingleton<EncryptionService>();
	}
}
