using SocialGuard.Common.Data.Models;


namespace SocialGuard.YC.Data.Models;


public static class TrustlistUserExtensions
{
	public static byte GetMaxEscalationLevel(this TrustlistUser user) => user.Entries?.Max(e => e.EscalationLevel) ?? 0;
	public static float GetMedianEscalationLevel(this TrustlistUser user) => (float)(user.Entries?.Average(e => e.EscalationLevel) ?? 0f);

	public static TrustlistEntry GetLatestMaxEntry(this TrustlistUser user)
	{
		if (user.Entries?.Count is null or 0)
		{
			return null;
		}

		byte maxEscalation = user.Entries.Max(e => e.EscalationLevel);

		return (from e in user.Entries
				where e.EscalationLevel == maxEscalation
				orderby e.LastEscalated descending
				select e).First();
	}
}
