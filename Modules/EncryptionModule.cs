using System.Threading.Tasks;
using SocialGuard.YC.Services.Security;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		public class EncryptionModule : BaseCommandModule
		{
			private readonly IEncryptionService _cipher;

			public EncryptionModule(IEncryptionService cipher)
			{
				this._cipher = cipher;
			}


			[Command("encrypt"), RequireOwner]
			public async Task EncryptAsync(CommandContext context, [RemainingText] string value)
			{
				await context.RespondAsync(_cipher.Encrypt(value));
			}

			[Command("decrypt"), RequireOwner]
			public async Task DecryptAsync(CommandContext context, [RemainingText] string value)
			{
				await context.RespondAsync(_cipher.Decrypt(value));
			}
#if DEBUG
			[Command("key"), RequireOwner]
			public Task DecryptAsync(CommandContext context)
			{
				throw new NotSupportedException();
				//await context.RespondAsync(cipher.GetKey());
			}
#endif
		}
	}
}
