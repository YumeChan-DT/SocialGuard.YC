using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Nodsoft.YumeChan.PluginBase.Tools.Data;



namespace Transcom.SocialGuard.YC.Data.Config
{
	public record GuildConfig : IDocument<ulong>
	{
		[BsonId, BsonRepresentation(BsonType.Int64)]
		public ulong Id { get; set; }

		public string WriteAccessKey { get; set; }

		public ulong JoinLogChannel { get; set; }

		public ulong BanLogChannel { get; set; }

		public bool AutoBanBlacklisted { get; set; }
	}
}
