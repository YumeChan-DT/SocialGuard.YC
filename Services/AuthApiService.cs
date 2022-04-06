using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MongoDB.Driver;
using SocialGuard.Common.Data.Models.Authentication;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services.Security;
using YumeChan.PluginBase.Tools;
using YumeChan.PluginBase.Tools.Data;



namespace SocialGuard.YC.Services;

public class AuthApiService : AuthenticationClient
{
	private readonly IEncryptionService _encryption;
	private readonly IMongoCollection<GuildConfig> _guildConfig;

	public AuthApiService(HttpClient httpClient, IInterfaceConfigProvider<IApiConfig> configProvider, IEncryptionService encryption, IDatabaseProvider<PluginManifest> database) 
		: base(httpClient)
	{
		httpClient.BaseAddress = new(configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
		_encryption = encryption;
		_guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
	}


	public async Task<TokenResult> GetOrUpdateAuthTokenAsync(ulong guildId)
	{
		GuildConfig config = await _guildConfig.FindOrCreateConfigAsync(guildId);
		LoginModel login = config.ApiLogin ?? throw new ApplicationException("Guild must first set API login (username/password).");

		if (config.Token is TokenResult token && token.IsValid())
		{
			return token;
		}
		else
		{
			try
			{
				token = await LoginAsync(login.Username, _encryption.Decrypt(login.Password));

				await _guildConfig.UpdateOneAsync(
					Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
					Builders<GuildConfig>.Update.Set(c => c.Token, token));

				return token;
			}
			catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
			{
				throw new UnauthorizedAccessException(e.Message, e);
			}
		}
	}

	public async Task ClearTokenAsync(ulong guildId)
	{
		GuildConfig config = await _guildConfig.FindOrCreateConfigAsync(guildId);

		if (config.Token is not null)
		{
			await _guildConfig.UpdateOneAsync(
				Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
				Builders<GuildConfig>.Update.Set(c => c.Token, null)
			);
		}
	}
}
