using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.YC.Services.Security;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace SocialGuard.YC
{
	public class PluginManifest : Plugin
	{
#if !DEBUG
		internal protected const string PasswordEncryptionKeyName = "PasswordEncryption";
#else
		internal protected const string PasswordEncryptionKeyName = "DEV-PasswordEncryption";
#endif

		public override string DisplayName => "NSYS SocialGuard (YC)";
		public override bool StealthMode => false;
		internal const string ApiConfigFileName = "api";

		private readonly IApiConfig _apiConfig;
		private readonly ILogger<PluginManifest> logger;
		private readonly BroadcastsListener broadcastsListener;
		private readonly GuildTrafficHandler guildTrafficHandler;
		private readonly ComponentInteractionsListener componentInteractionsListener;

		internal static string VersionString { get; private set; }


		public PluginManifest(ILogger<PluginManifest> logger, IConfigProvider<IApiConfig> configProvider,
			BroadcastsListener broadcastsListener, GuildTrafficHandler guildTrafficHandler, ComponentInteractionsListener componentInteractionsListener)
		{
			VersionString ??= Version;
			this.logger = logger;
			this.broadcastsListener = broadcastsListener;
			this.guildTrafficHandler = guildTrafficHandler;
			this.componentInteractionsListener = componentInteractionsListener;
			_apiConfig = configProvider.InitConfig(ApiConfigFileName).PopulateApiConfig();
		}

		public override async Task LoadAsync()
		{
			CancellationToken cancellationToken = CancellationToken.None; // May get added into method parameters later on.
			await base.LoadAsync();

			await componentInteractionsListener.StartAsync(cancellationToken);
			await broadcastsListener.StartAsync(cancellationToken);
			await guildTrafficHandler.StartAsync(cancellationToken);

			KeyClient keyClient = new(new(_apiConfig.KeyVaultUri), new ClientSecretCredential(
				_apiConfig.AzureIdentity.TenantId,
				_apiConfig.AzureIdentity.ClientId,
				_apiConfig.AzureIdentity.ClientSecret));


			bool noKey = true;

			await foreach (KeyProperties keyProp in keyClient.GetPropertiesOfKeysAsync().WithCancellation(cancellationToken))
			{
				if (keyProp is { Name: PasswordEncryptionKeyName, Enabled: true })
				{
					if ((await keyClient.GetKeyAsync(PasswordEncryptionKeyName)).Value is KeyVaultKey key
						&& key.KeyOperations.Contains(KeyOperation.Decrypt) && key.KeyOperations.Contains(KeyOperation.Encrypt))
					{
						noKey = false;
						break;
					}
				}
			}

			if (noKey)
			{
				await keyClient.CreateKeyAsync(PasswordEncryptionKeyName, KeyType.Rsa);
			}


			logger.LogInformation("Loaded {plugin}.", DisplayName);
			logger.LogInformation("Current SocialGuard API Path: {apiPath}", _apiConfig.ApiHost);
		}


		public override async Task UnloadAsync()
		{
			CancellationToken cancellationToken = CancellationToken.None; // May get added into method parameters later on.

			await componentInteractionsListener.StopAsync(cancellationToken);
			await broadcastsListener.StopAsync(cancellationToken);
			await guildTrafficHandler.StopAsync(cancellationToken);

			logger.LogInformation("Unloaded {plugin}.", DisplayName);
			await base.UnloadAsync();
		}
	}

	public class DependencyRegistrations : DependencyInjectionHandler
	{
		public override IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddSingleton<GuildTrafficHandler>()
			.AddSingleton<BroadcastsListener>()
			.AddSingleton<ComponentInteractionsListener>()
			.AddSingleton<TrustlistUserApiService>()
			.AddSingleton<AuthApiService>()
			.AddSingleton<EncryptionService>()
//			.AddSingleton((services) => services.GetRequiredService<IConfigProvider<IApiConfig>>().InitConfig(ApiConfigFileName).PopulateApiConfig())
			;
	}


}
