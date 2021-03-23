using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Transcom.SocialGuard.YC.Data.Components
{
	public record AuthToken(string Token, DateTime Expiration) : IAuthComponent
	{
		public bool IsValid() => Expiration > DateTime.UtcNow;
	};
}
