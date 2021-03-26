using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using Nodsoft.YumeChan.PluginBase.Tools;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Net.Http;
using System.Threading.Tasks;
using SocialGuard.YC.Services.Security;



namespace SocialGuard.YC
{
	public class PluginManifest : Nodsoft.YumeChan.PluginBase.Plugin
	{
		public override string PluginDisplayName => "NSYS SocialGuard (YC)";
		public override bool PluginStealth => false;

		internal const string ApiConfigFileName = "api";
		
		private readonly ILogger<PluginManifest> logger;
		private readonly DiscordSocketClient coreClient;
		
		public GuildTrafficHandler GuildTrafficHandler { get; }


		public PluginManifest(DiscordSocketClient client, ILogger<PluginManifest> logger, IConfigProvider<IApiConfig> apiConfig, IDatabaseProvider<PluginManifest> database, IHttpClientFactory httpClientFactory)
		{
			coreClient = client;
			this.logger = logger;

			TrustlistUserApiService apiService = new(httpClientFactory, apiConfig);
			GuildTrafficHandler = new(apiService, database);
		}

		public override async Task LoadPlugin() 
		{
			coreClient.UserJoined += GuildTrafficHandler.OnGuildUserJoinedAsync;

			await base.LoadPlugin();

			logger.LogInformation("Loaded Plugin.");
		}

		public override async Task UnloadPlugin()
		{
			coreClient.UserJoined -= GuildTrafficHandler.OnGuildUserJoinedAsync;

			await base.UnloadPlugin();
		}

		public override IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddSingleton<TrustlistUserApiService>()
			.AddSingleton<AuthApiService>()
			.AddSingleton<EncryptionService>();
	}
}
