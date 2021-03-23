using Nodsoft.YumeChan.PluginBase.Tools;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Transcom.SocialGuard.YC.Data.Components;
using Transcom.SocialGuard.YC.Data.Models.Api;
using Transcom.SocialGuard.YC.Data.Models.Config;
using Transcom.SocialGuard.YC.Services.Security;



namespace Transcom.SocialGuard.YC.Services
{
	public class AuthApiService
	{
		private readonly HttpClient httpClient;
		private readonly EncryptionService encryption;
		private readonly IEntityRepository<GuildConfig, ulong> guildConfigRepository;

		public AuthApiService(IHttpClientFactory httpFactory, IConfigProvider<IApiConfig> configProvider, EncryptionService encryption, IDatabaseProvider<PluginManifest> database)
		{
			httpClient = httpFactory.CreateClient(nameof(PluginManifest));
			httpClient.BaseAddress = new(configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
			this.encryption = encryption;
			guildConfigRepository = database.GetEntityRepository<GuildConfig, ulong>();
		}


		public async Task<AuthToken> GetOrUpdateAuthTokenAsync(ulong guildId)
		{
			AuthCredentials login = (await guildConfigRepository.FindOrCreateConfigAsync(guildId)).ApiLogin 
				?? throw new ApplicationException("Guild must first set API login (username/password).");

			if ((await guildConfigRepository.FindOrCreateConfigAsync(guildId)).Token is AuthToken token and not null && token.IsValid())
			{
				return token;
			}
			else
			{
				using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/login")
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
					return (await response.Content.ReadFromJsonAsync<AuthResponse<AuthToken>>(Utilities.SerializerOptions)).Details;
				}
			}
		}


		public async Task<AuthResponse<IAuthComponent>> RegisterNewUserAsync(AuthRegisterCredentials registerDetails)
		{
			using HttpRequestMessage request = new(HttpMethod.Post, "/api/auth/register")
			{
				Content = JsonContent.Create(registerDetails)
			};

			using HttpResponseMessage response = await httpClient.SendAsync(request);

			return await response.Content.ReadFromJsonAsync<AuthResponse<IAuthComponent>>();
		}
	}
}
