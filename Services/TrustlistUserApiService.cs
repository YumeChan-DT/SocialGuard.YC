using YumeChan.PluginBase.Tools;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models;
using SocialGuard.YC.Data.Models.Config;
using System.Linq;

namespace SocialGuard.YC.Services
{
	public class TrustlistUserApiService
	{
		private const string AccessKeyName = "Access-Key";
		private const string JsonMimeType = "application/json";

		internal Uri BaseAddress => client.BaseAddress;

		private readonly HttpClient client;

		public TrustlistUserApiService(IHttpClientFactory factory, IConfigProvider<IApiConfig> config)
		{
			client = factory.CreateClient(nameof(PluginManifest));
			client.BaseAddress = new(config.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
		}

		public async Task<TrustlistUser> LookupUserAsync(ulong userId) => (await LookupUsersAsync(new ulong[] { userId }))?[0];

		public async Task<TrustlistUser[]> LookupUsersAsync(ulong[] usersId)
		{
			HttpRequestMessage request = new(HttpMethod.Get, $"/api/v3/user/{string.Join(',', usersId)}");
			HttpResponseMessage response = await client.SendAsync(request);

			return await Utilities.ParseResponseFullAsync<TrustlistUser[]>(response) is TrustlistUser[] parsed && parsed.Length is not 0
				? parsed
				: null;
		}

		public async Task<ulong[]> ListKnownUsersAsync()
		{
			HttpRequestMessage request = new(HttpMethod.Get, "/api/v3/user/list");
			HttpResponseMessage response = await client.SendAsync(request);

			return await Utilities.ParseResponseFullAsync<ulong[]>(response);
		}

		public async Task SubmitEntryAsync(ulong userId, TrustlistEntry entry, AuthToken token)
		{
			using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v3/user/{userId}");
			request.Content = new StringContent(JsonSerializer.Serialize(entry, Utilities.SerializerOptions), Encoding.UTF8, JsonMimeType);
			request.Headers.Authorization = new("bearer", token.Token);

			using HttpResponseMessage response = await client.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				throw new ApplicationException($"API returned {response.StatusCode}.");
			}
		}
	}
}
