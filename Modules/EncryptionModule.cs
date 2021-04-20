using System.Threading.Tasks;
using SocialGuard.YC.Services.Security;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;



namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		public class EncryptionModule : BaseCommandModule
		{
			private readonly EncryptionService cipher;

			public EncryptionModule(EncryptionService cipher)
			{
				this.cipher = cipher;
			}


			[Command("encrypt"), RequireOwner]
			public async Task EncryptAsync(CommandContext context, [RemainingText] string value)
			{
				await context.RespondAsync(cipher.Encrypt(value));
			}

			[Command("decrypt"), RequireOwner]
			public async Task DecryptAsync(CommandContext context, [RemainingText] string value)
			{
				await context.RespondAsync(cipher.Decrypt(value));
			}
#if DEBUG
			[Command("key"), RequireOwner]
			public async Task DecryptAsync(CommandContext context)
			{
				await context.RespondAsync(cipher.GetKey());
			}
#endif
		}
	}
}
