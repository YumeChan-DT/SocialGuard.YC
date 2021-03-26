using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocialGuard.YC.Services.Security;

namespace SocialGuard.YC.Modules
{
	[Group("sg")]
	public class EncryptionModule : ModuleBase<SocketCommandContext>
	{
		private readonly EncryptionService cipher;

		public EncryptionModule(EncryptionService cipher)
		{
			this.cipher = cipher;
		}


		[Command("encrypt"), RequireOwner]
		public async Task EncryptAsync([Remainder] string value)
		{
			await ReplyAsync(cipher.Encrypt(value));
		}

		[Command("decrypt"), RequireOwner]
		public async Task DecryptAsync([Remainder] string value)
		{
			await ReplyAsync(cipher.Decrypt(value));
		}
#if DEBUG
		[Command("key"), RequireOwner]
		public async Task DecryptAsync()
		{
			await ReplyAsync(cipher.GetKey());
		}
#endif
	}
}
