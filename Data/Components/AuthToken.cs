using System;

namespace SocialGuard.YC.Data.Components
{
	public sealed record AuthToken(string Token, DateTime Expiration) : IAuthComponent
	{
		public bool IsValid() => Expiration > DateTime.UtcNow;
	};
}
