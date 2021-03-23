using Transcom.SocialGuard.YC.Data.Components;



namespace Transcom.SocialGuard.YC.Data.Models.Api
{
	public record AuthResponse<T>(string Status, string Message, T Details) where T : IAuthComponent;
}
