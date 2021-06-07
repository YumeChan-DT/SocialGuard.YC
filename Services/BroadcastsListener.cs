using System;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.PluginBase.Tools;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;

namespace SocialGuard.YC.Services
{
	public class BroadcastsListener : IHostedService
	{
		private readonly ILogger<BroadcastsListener> logger;
		private readonly DiscordClient discordClient;
		private readonly HubConnection hubConnection;

		public BroadcastsListener(ILogger<BroadcastsListener> logger, IConfigProvider<IApiConfig> configProvider, DiscordClient discordClient)
		{
			IApiConfig config = configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig();
			this.logger = logger;
			this.discordClient = discordClient;
			hubConnection = new HubConnectionBuilder()
				.WithUrl(config.ApiHost + "/hubs/trustlist")
				.AddMessagePackProtocol()
				.WithAutomaticReconnect()
				.Build();

			hubConnection.On<TrustlistEntry>("NotifyNewEntry", async (entry) =>
				{
					await TestPing();
				});
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			// Loop is here to wait until the server is running
			while (hubConnection.State is not HubConnectionState.Connected)
			{
				try
				{
					await hubConnection.StartAsync(cancellationToken);
				}
				catch
				{
					await Task.Delay(1000, cancellationToken);
				}
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => hubConnection.DisposeAsync().AsTask();

		public async Task TestPing() => await (await discordClient.GetChannelAsync(815152787990118430)).SendMessageAsync("SignalR broadcast received.");
	}
}
