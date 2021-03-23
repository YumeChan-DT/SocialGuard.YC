using Nodsoft.YumeChan.PluginBase.Tools;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Transcom.SocialGuard.YC.Data.Components;
using Transcom.SocialGuard.YC.Data.Models;
using Transcom.SocialGuard.YC.Data.Models.Config;

namespace Transcom.SocialGuard.YC.Services
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
			using HttpRequestMessage request = new(HttpMethod.Get, "/api/emitter");
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
			using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/login")
			{
				Content = JsonContent.Create(emitter, options: Utilities.SerializerOptions)
			};

			using HttpResponseMessage response = await httpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();
		}
	}
}
