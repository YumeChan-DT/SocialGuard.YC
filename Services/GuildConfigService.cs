using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services.Security;
using YumeChan.PluginBase.Database.MongoDB;

namespace SocialGuard.YC.Services;

/// <summary>
/// Defines a service to configure a guild's trustlist settings.
/// </summary>
public class GuildConfigService
{
	private readonly IMongoCollection<GuildConfig> _guildConfig;
	private readonly ApiAuthService _apiAuth;
	private readonly IEncryptionService _encryption;

	public GuildConfigService(IMongoDatabaseProvider<PluginManifest> database, ApiAuthService apiAuth, IEncryptionService encryption)
	{
		_guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
		_apiAuth = apiAuth;
		_encryption = encryption;
	}
	
	public async Task<GuildConfig> GetGuildConfigAsync(ulong guildId) => await _guildConfig.FindOrCreateConfigAsync(guildId);
	
	public async Task<bool> SetGuildConfigAsync(GuildConfig config)
	{
		ReplaceOneResult? result = await _guildConfig.ReplaceOneAsync(x => x.Id == config.Id, config, new ReplaceOptions { IsUpsert = true });
		return result.IsAcknowledged;
	}
}