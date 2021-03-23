namespace Transcom.SocialGuard.YC.Data.Models
{
	public record Emitter
	{
		public string Login { get; init; }

		public EmitterType EmitterType { get; init; }

		public ulong Snowflake { get; init; }

		public string DisplayName { get; init; }
	}

	public enum EmitterType
	{
		Unknown = 0,
		User = 1,
		Server = 2
	}
}
