using Nodsoft.YumeChan.PluginBase.Tools;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;

namespace SocialGuard.YC.Services
{
	public class TrustlistUserApiService
	{
		private const string AccessKeyName = "Access-Key";
		private const string JsonMimeType = "application/json";

		private readonly HttpClient client;

		public TrustlistUserApiService(IHttpClientFactory factory, IConfigProvider<IApiConfig> config)
		{
			client = factory.CreateClient(nameof(PluginManifest));
			client.BaseAddress = new(config.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
		}

		public async Task<TrustlistUser> LookupUserAsync(ulong userId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/api/user/{userId}");
			HttpResponseMessage response = await client.SendAsync(request);

			return response.StatusCode is HttpStatusCode.NotFound 
				? null
				: await Utilities.ParseResponseFullAsync<TrustlistUser>(response);
		}

		public async Task<TrustlistUser> ListKnownUsersAsync()
		{
			HttpRequestMessage request = new(HttpMethod.Get, "/api/user/list");
			HttpResponseMessage response = await client.SendAsync(request);

			return await Utilities.ParseResponseFullAsync<TrustlistUser>(response);
		}

		public async Task InsertOrEscalateUserAsync(TrustlistUser user, AuthToken token)
		{
			using HttpRequestMessage request = new(await LookupUserAsync(user.Id) is null ? HttpMethod.Post : HttpMethod.Put, "/api/user/");
			request.Content = new StringContent(JsonSerializer.Serialize(user, Utilities.SerializerOptions), Encoding.UTF8, JsonMimeType);
			request.Headers.Authorization = new("bearer", token.Token);

			using HttpResponseMessage response = await client.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				throw new ApplicationException($"API returned {response.StatusCode}.");
			}
		}
	}
}
