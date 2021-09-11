using DSharpPlus.SlashCommands;



namespace SocialGuard.YC.Data.Components
{
	public enum TrustlistEscalationLevel
	{
		[ChoiceName("Clean")] Clean = 0,
		[ChoiceName("Suspicious")] Suspicious = 1,
		[ChoiceName("Untrusted")] Untrusted = 2,
		[ChoiceName("Dangerous")] Dangerous = 3
	}
}
