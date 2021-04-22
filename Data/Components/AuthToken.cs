using System;

namespace SocialGuard.YC.Data.Components
{
	public record AuthToken(string Token, DateTime Expiration) : IAuthComponent
	{
		public bool IsValid() => Expiration > DateTime.UtcNow;
	};
}
