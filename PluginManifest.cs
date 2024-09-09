using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.YC.Services.Security;
using DSharpPlus;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using SocialGuard.YC.Infrastructure.Security.Authorization;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace SocialGuard.YC;

/// <inheritdoc />
[PublicAPI]
public sealed class PluginManifest : Plugin
{
	public override string DisplayName => "NSYS SocialGuard (YC)";
	public override string AuthorContact => "admin@nodsoft.net";

	public override Uri? IconUri { get; } = new("https://socialguard.net/assets/icons/android-chrome-512x512.png");

	public override bool StealthMode => false;
	public override bool ShouldUseNetRunner => true;

	internal const string ApiConfigFileName = "api";

	private readonly IApiConfig _apiConfig;
	private readonly ILogger<PluginManifest> _logger;
	private readonly BroadcastsListener _broadcastsListener;
	private readonly GuildTrafficHandler _guildTrafficHandler;
	private readonly ComponentInteractionsListener _componentInteractionsListener;

	internal static string VersionString { get; private set; } = string.Empty;


	public PluginManifest(ILogger<PluginManifest> logger, IApiConfig config,
		BroadcastsListener broadcastsListener, GuildTrafficHandler guildTrafficHandler, ComponentInteractionsListener componentInteractionsListener)
	{
		VersionString = Version;
		_logger = logger;
		_broadcastsListener = broadcastsListener;
		_guildTrafficHandler = guildTrafficHandler;
		_componentInteractionsListener = componentInteractionsListener;
		_apiConfig = config;
	}

	public override async Task LoadAsync()
	{
		CancellationToken ct = CancellationToken.None; // May get added into method parameters later on.
		await base.LoadAsync();
		
		// Start listeners/handlers in the background.
		_ = Task.Run(async () =>
		{
			_logger.LogInformation("Starting SocialGuard for YC background services...");
			await _broadcastsListener.StartAsync(ct);
			await _guildTrafficHandler.StartAsync(ct);
			await _componentInteractionsListener.StartAsync(ct);
		}, ct);


		_logger.LogInformation("Loaded {Plugin}", DisplayName);
		_logger.LogInformation("Current SocialGuard API Path: {ApiPath}", _apiConfig.ApiHost);
	}


	public override async Task UnloadAsync()
	{
		CancellationToken ct = CancellationToken.None; // May get added into method parameters later on.

		await _componentInteractionsListener.StopAsync(ct);
		await _broadcastsListener.StopAsync(ct);
		await _guildTrafficHandler.StopAsync(ct);

		_logger.LogInformation("Unloaded {Plugin}", DisplayName);
		await base.UnloadAsync();
	}
}

/// <summary>
/// Handles dependency injection regstrations for SocialGuard YC.
/// </summary>
public sealed class DependencyRegistrations : DependencyInjectionHandler
{
	public override IServiceCollection ConfigureServices(IServiceCollection services)
	{
		services.AddTransient(static s => s.GetRequiredService<IInterfaceConfigProvider<IApiConfig>>().InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig());
		
		// HTTP client for SocialGuard API.
		services.AddHttpClient<RestClientBase>(static (services, client) => client.BaseAddress = new(services.GetRequiredService<IApiConfig>().ApiHost));

		// Encryption service for password storage.
		services.AddSingleton<IEncryptionService>(static services =>
		{
			if (services.GetRequiredService<IApiConfig>().AzureIdentity is null)
			{
				return ActivatorUtilities.CreateInstance<LocalEncryptionService>(services);
			}

			KeyVaultService kvs = ActivatorUtilities.CreateInstance<KeyVaultService>(services);
			kvs.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
			return kvs;

		});

		// Authorization handlers for Web UI.
		services.AddAuthorizationCore(static options =>
		{
			options.AddPolicy(AuthorizationExtensions.RequireManageGuildPermission, policy => policy
				.RequireGuildRole(Permissions.ManageGuild));
			
			options.AddPolicy(AuthorizationExtensions.RequireBanMembersPermission, policy => policy
				.RequireGuildRole(Permissions.BanMembers));
		});
		
		services.AddScoped<IAuthorizationHandler, GuildAccessAuthorizationHandler>();

		services.AddSingleton<GuildTrafficHandler>();
		services.AddSingleton<BroadcastsListener>();
		services.AddSingleton<ComponentInteractionsListener>();
		services.AddSingleton<ApiAuthService>();
		services.AddSingleton<GuildConfigService>();

		services.AddSingleton(static services =>
		{
			TrustlistClient client = ActivatorUtilities.CreateInstance<TrustlistClient>(services);
			client.SetBaseUri(new(services.GetRequiredService<IApiConfig>().ApiHost));
			return client;
		});

		services.AddSingleton(static services =>
		{
			EmitterClient client = ActivatorUtilities.CreateInstance<EmitterClient>(services);
			client.SetBaseUri(new(services.GetRequiredService<IApiConfig>().ApiHost));
			return client;
		});
		
		return services;
	}
}
