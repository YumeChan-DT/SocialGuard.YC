using MongoDB.Driver;
using SocialGuard.YC.Data.Models.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialGuard.YC.Services
{
	public static class GuildConfigExtensions
	{
		public static async Task<GuildConfig> FindOrCreateConfigAsync(this IMongoCollection<GuildConfig> collection, ulong guildId)
		{
			GuildConfig config = (await collection.FindAsync(c => c.Id == guildId)).FirstOrDefault();

			if (config is null)
			{
				await collection.InsertOneAsync(config = new() { Id = guildId });
			}

			return config;
		}

		public static Task<UpdateResult> SetLoginAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update.Set(c => c.ApiLogin, config.ApiLogin));

		public static Task<UpdateResult> SetJoinlogAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update.Set(c => c.JoinLogChannel, config.JoinLogChannel));

		public static Task<UpdateResult> SetLeavelogAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update
				.Set(c => c.LeaveLogEnabled, config.LeaveLogEnabled)
				.Set(c => c.LeaveLogChannel, config.LeaveLogChannel));

		public static Task<UpdateResult> SetBanlogAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update.Set(c => c.BanLogChannel, config.BanLogChannel));
		public static Task<UpdateResult> SetAutobanAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update.Set(c => c.AutoBanBlacklisted, config.AutoBanBlacklisted));

		public static Task<UpdateResult> SetJoinlogSuppressionAsync(this IMongoCollection<GuildConfig> collection, GuildConfig config) => collection.UpdateOneAsync(
			Builders<GuildConfig>.Filter.Eq(c => c.Id, config.Id),
			Builders<GuildConfig>.Update.Set(c => c.AutoBanBlacklisted, config.AutoBanBlacklisted));


	}
}
