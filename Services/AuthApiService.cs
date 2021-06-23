using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MongoDB.Driver;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models.Api;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services.Security;
using YumeChan.PluginBase.Tools;
using YumeChan.PluginBase.Tools.Data;



namespace SocialGuard.YC.Services
{
	public class AuthApiService
	{
		private readonly HttpClient httpClient;
		private readonly EncryptionService encryption;
		private readonly IMongoCollection<GuildConfig> guildConfig;

		public AuthApiService(IHttpClientFactory httpFactory, IConfigProvider<IApiConfig> configProvider, EncryptionService encryption, IDatabaseProvider<PluginManifest> database)
		{
			httpClient = httpFactory.CreateClient(nameof(PluginManifest));
			httpClient.BaseAddress = new(configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
			this.encryption = encryption;
			guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
		}


		public async Task<AuthToken> GetOrUpdateAuthTokenAsync(ulong guildId)
		{
			AuthCredentials login = (await guildConfig.FindOrCreateConfigAsync(guildId)).ApiLogin 
				?? throw new ApplicationException("Guild must first set API login (username/password).");
			GuildConfig config = await guildConfig.FindOrCreateConfigAsync(guildId);

			if (config.Token is AuthToken token && token.IsValid())
			{
				return token;
			}
			else
			{
				using HttpRequestMessage request = new(HttpMethod.Post, "/api/v3/auth/login")
				{
					Content = JsonContent.Create(new AuthCredentials(login.Username, encryption.Decrypt(login.Password)), options: Utilities.SerializerOptions)
				};

				using HttpResponseMessage response = await httpClient.SendAsync(request);

				if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
				{
					throw new UnauthorizedAccessException("Authentication Failed. Please check and update your credentials.");
				}
				else
				{
					token = (await response.Content.ReadFromJsonAsync<AuthResponse<AuthToken>>(Utilities.SerializerOptions)).Details;

					await guildConfig.UpdateOneAsync(
						Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
						Builders<GuildConfig>.Update.Set(c => c.Token, token));

					return token;
				}
			}
		}

		public async Task ClearTokenAsync(ulong guildId)
		{
			GuildConfig config = await guildConfig.FindOrCreateConfigAsync(guildId);

			if (config.Token is not null)
			{
				await guildConfig.UpdateOneAsync(
					Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
					Builders<GuildConfig>.Update.Set(c => c.Token, null)
				);
			}
		}


		public async Task<AuthResponse<IAuthComponent>> RegisterNewUserAsync(AuthRegisterCredentials registerDetails)
		{
			using HttpRequestMessage request = new(HttpMethod.Post, "/api/v3/auth/register")
			{
				Content = JsonContent.Create(registerDetails)
			};

			using HttpResponseMessage response = await httpClient.SendAsync(request);
			return await response.Content.ReadFromJsonAsync<AuthResponse<IAuthComponent>>();
		}
	}
}
