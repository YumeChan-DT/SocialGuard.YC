using Discord;
using Discord.Commands;
using Transcom.SocialGuard.YC.Data.Models.Config;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Threading.Tasks;
using Transcom.SocialGuard.YC.Services.Security;
using System.ComponentModel.DataAnnotations;
using Transcom.SocialGuard.YC.Data.Components;
using Transcom.SocialGuard.YC.Services;
using Transcom.SocialGuard.YC.Data.Models.Api;

namespace Transcom.SocialGuard.YC.Modules
{
	[Group("socialguard config"), Alias("sg config")]
	public class GuildConfigModule : ModuleBase<SocketCommandContext>
	{
		private readonly IEntityRepository<GuildConfig, ulong> repository;
		private readonly AuthApiService auth;
		private readonly EncryptionService encryption;

		public GuildConfigModule(IDatabaseProvider<PluginManifest> database, AuthApiService auth, EncryptionService encryption)
		{
			repository = database.GetEntityRepository<GuildConfig, ulong>();
			this.auth = auth;
			this.encryption = encryption;
		}


		[Command("joinlog"), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task GetJoinLogAsync()
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			ITextChannel current = Context.Guild.GetTextChannel(config.JoinLogChannel);
			await ReplyAsync($"Current Join Log channel : {current?.Mention ?? "None"}.");
		}

		[Command("joinlog"), Priority(10), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task SetJoinLogAsync(ITextChannel channel)
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			config.JoinLogChannel = channel.Id;
			await repository.ReplaceOneAsync(config);
			await ReplyAsync($"Join Log channel set to : {Context.Guild.GetTextChannel(config.JoinLogChannel).Mention}.");
		}


		[Command("banlog"), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task GetBanLogAsync()
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			ITextChannel current = Context.Guild.GetTextChannel(config.BanLogChannel);
			await ReplyAsync($"Current Ban Log channel : {current?.Mention ?? "None"}.");
		}

		[Command("banlog"), Priority(10), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task SetBanLogAsync(ITextChannel channel)
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			config.BanLogChannel = channel.Id;
			await repository.ReplaceOneAsync(config);
			await ReplyAsync($"Join Ban channel set to : {Context.Guild.GetTextChannel(config.BanLogChannel).Mention}.");
		}

		[Command("credentials"), Alias("login"), Priority(10), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task ConfigureAccessKeyAsync(string username, string password)
		{
			await Context.Message.DeleteAsync();

			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			config.ApiLogin = new(username, encryption.Encrypt(password));
			await repository.ReplaceOneAsync(config);
			await ReplyAsync($"API credentials has been set.");
		}

		[Command("autobanblacklisted"), Alias("autoban"), RequireUserPermission(GuildPermission.ManageGuild), RequireBotPermission(GuildPermission.BanMembers)]
		public async Task ConfigureBanLogAsync()
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);
			await ReplyAsync($"Auto-ban Blacklist is {(config.AutoBanBlacklisted ? "on" : "off")}.");
		}

		[Command("autobanblacklisted"), Alias("autoban"), Priority(10), RequireUserPermission(GuildPermission.ManageGuild), RequireBotPermission(GuildPermission.ManageGuild)]
		public async Task ConfigureBanLogAsync(string key)
		{
			GuildConfig config = await repository.FindOrCreateConfigAsync(Context.Guild.Id);

			config.AutoBanBlacklisted = key.ToLowerInvariant() switch
			{
				"true" or "yes" or "on" or "1" => true,
				"false" or "no" or "off" or "0" => false,
				_ => config.AutoBanBlacklisted
			};

			await repository.ReplaceOneAsync(config);
			await ReplyAsync($"Auto-ban Blacklist has been turned {(config.AutoBanBlacklisted ? "on" : "off")}.");
		}

		[Command("register"), RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task RegisterAsync(string username, [EmailAddress] string email, string password)
		{
			await Context.Message.DeleteAsync();
			AuthRegisterCredentials credentials = new(username, email, password);
			AuthResponse<IAuthComponent> result = await auth.RegisterNewUserAsync(credentials);

			await ReplyAsync($"{Context.User.Mention} {result.Status} : {result.Message}\n(Usage: ``sg config register <username> <email> <password>``)");
		}
	}
}
