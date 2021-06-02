using System;



namespace SocialGuard.YC.Data.Models
{
	public record TrustlistEntry
	{
		public string Id { get; init; }

		public DateTime EntryAt { get; init; }
		public DateTime LastEscalated { get; set; }

		public byte EscalationLevel { get; set; }

		public string EscalationNote { get; set; }

		public Emitter Emitter { get; set; }
	}
}
