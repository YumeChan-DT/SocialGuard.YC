using Transcom.SocialGuard.YC.Data.Config;
using Transcom.SocialGuard.YC.Data.Models;
using Nodsoft.YumeChan.PluginBase.Tools;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;



namespace Transcom.SocialGuard.YC.Services
{
	public class ApiService
	{
		private const string AccessKeyName = "Access-Key";
		private const string JsonMimeType = "application/json";

		private readonly HttpClient client;
		private static readonly JsonSerializerOptions serializerOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		public ApiService(IHttpClientFactory factory, IConfigProvider<IApiConfig> config)
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

		public async Task InsertOrEscalateUserAsync(TrustlistUser user, string accessKey)
		{
			using HttpRequestMessage request = new(await LookupUserAsync(user.Id) is null ? HttpMethod.Post : HttpMethod.Put, "/api/user/");
			request.Content = new StringContent(JsonSerializer.Serialize(user, serializerOptions), Encoding.UTF8, JsonMimeType);
			request.Headers.Add(AccessKeyName, accessKey);

			using HttpResponseMessage response = await client.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				throw new ApplicationException($"API returned {response.StatusCode}.");
			}
		}
	}
}
