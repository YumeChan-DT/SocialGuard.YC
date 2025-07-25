﻿using System.Net;
using MongoDB.Driver;
using SocialGuard.Common.Data.Models.Authentication;
using SocialGuard.Common.Services;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services.Security;
using YumeChan.PluginBase.Database.MongoDB;
using YumeChan.PluginBase.Tools;

namespace SocialGuard.YC.Services;

public class ApiAuthService : AuthenticationClient
{
	private readonly IEncryptionService _encryption;
	private readonly IMongoCollection<GuildConfig> _guildConfig;

	public ApiAuthService(
		HttpClient httpClient, 
		IInterfaceConfigProvider<IApiConfig> configProvider, 
		IEncryptionService encryption, 
		IMongoDatabaseProvider<PluginManifest> database
	) : base(httpClient)
	{
		httpClient.BaseAddress = new(configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().ApiHost);
		_encryption = encryption;
		_guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
	}


	public async Task<TokenResult?> GetOrUpdateAuthTokenAsync(ulong guildId)
	{
		GuildConfig config = await _guildConfig.FindOrCreateConfigAsync(guildId);

		if (config.ApiLogin is not { } login)
		{
			return null;
		}

		if (config.Token is { } token && token.IsValid())
		{
			return token;
		}

		try
		{
			token = await LoginAsync(login.Username, await _encryption.DecryptAsync(login.Password));

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
