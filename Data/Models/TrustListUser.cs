using System.Linq;



namespace SocialGuard.YC.Data.Models
{
	public record TrustlistUser
	{
		public ulong Id { get; init; }

		public TrustlistEntry[] Entries { get; init; }


		public byte GetMaxEscalationLevel() => Entries?.Max(e => e.EscalationLevel) ?? 0;
		public float GetMedianEscalationLevel() => (float)(Entries?.Average(e => e.EscalationLevel) ?? 0f);


	}
}
