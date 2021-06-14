using YumeChan.PluginBase.Tools;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;

namespace SocialGuard.YC.Services
{
	public class EmitterApiService
	{
		private readonly HttpClient httpClient;

		public EmitterApiService(IHttpClientFactory httpFactory, IConfigProvider<IApiConfig> configProvider)
		{
			httpClient = httpFactory.CreateClient(nameof(PluginManifest));
			httpClient.BaseAddress = new(configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
		}

		public async Task<Emitter> GetEmitterAsync(AuthToken token)
		{
			using HttpRequestMessage request = new(HttpMethod.Get, "/api/v2/emitter");
			request.Headers.Authorization = new("bearer", token.Token);

			using HttpResponseMessage response = await httpClient.SendAsync(request);

			if (response.StatusCode is HttpStatusCode.OK)
			{
				return await response.Content.ReadFromJsonAsync<Emitter>();
			}
			else
			{
				return null;
			}
		}

		public async Task SetEmitterAsync(Emitter emitter, AuthToken token)
		{
			using HttpRequestMessage request = new(HttpMethod.Post, "/api/v2/emitter");
			request.Content = JsonContent.Create(emitter);
			request.Headers.Authorization = new("bearer", token.Token);

			using HttpResponseMessage response = await httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();
		}
	}
}
