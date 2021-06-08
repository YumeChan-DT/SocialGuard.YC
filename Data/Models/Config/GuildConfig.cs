using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using SocialGuard.YC.Data.Components;



namespace SocialGuard.YC.Data.Models.Config
{
	public record GuildConfig : IDocument<ulong>
	{
		[BsonId, BsonRepresentation(BsonType.Int64)]
		public ulong Id { get; set; }

		public ulong JoinLogChannel { get; set; }

		public ulong BanLogChannel { get; set; }

		public bool AutoBanBlacklisted { get; set; }
		public bool SuppressJoinlogCleanRecords { get; set; }

		public AuthCredentials ApiLogin { get; set; }

		public AuthToken Token { get; set; }
	}
}
