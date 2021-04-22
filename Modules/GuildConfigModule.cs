using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using SocialGuard.YC.Data.Components;
using SocialGuard.YC.Data.Models.Api;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.YC.Services.Security;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("config")]
		public class GuildConfigModule : BaseCommandModule
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


			[Command("joinlog"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task JoinLogAsync(CommandContext context)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				DiscordChannel current = context.Guild.GetChannel(config.JoinLogChannel);
				await context.RespondAsync($"Current Join Log channel : {current?.Mention ?? "None"}.");
			}
			[Command]
			public async Task JoinLogAsync(CommandContext context, DiscordChannel channel)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				config.JoinLogChannel = channel.Id;
				await repository.ReplaceOneAsync(config);
				await context.RespondAsync($"Join Log channel set to : {context.Guild.GetChannel(config.JoinLogChannel).Mention}.");
			}


			[Command("banlog"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task BanLogAsync(CommandContext context)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				DiscordChannel current = context.Guild.GetChannel(config.BanLogChannel);
				await context.RespondAsync($"Current Ban Log channel : {current?.Mention ?? "None"}.");
			}
			[Command]
			public async Task BanLogAsync(CommandContext context, DiscordChannel channel)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				config.BanLogChannel = channel.Id;
				await repository.ReplaceOneAsync(config);
				await context.RespondAsync($"Join Ban channel set to : {context.Guild.GetChannel(config.BanLogChannel).Mention}.");
			}


			[Command("credentials"), Aliases("login"), Priority(10), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task ConfigureAccessKeyAsync(CommandContext context, string username, string password)
			{
				await context.Message.DeleteAsync();

				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				config.ApiLogin = new(username, encryption.Encrypt(password));
				await repository.ReplaceOneAsync(config);
				await context.Channel.SendMessageAsync($"API credentials has been set.");
			}

			[Command("set-autoban"), Aliases("autoban"), RequireUserPermissions(Permissions.ManageGuild), RequireBotPermissions(Permissions.BanMembers)]
			public async Task ConfigureAutobanAsync(CommandContext context)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);
				await context.RespondAsync($"Auto-ban Blacklist is **{(config.AutoBanBlacklisted ? "on" : "off")}**.");
			}
			[Command]
			public async Task ConfigureAutobanAsync(CommandContext context, string key)
			{
				GuildConfig config = await repository.FindOrCreateConfigAsync(context.Guild.Id);

				config.AutoBanBlacklisted = key.ToLowerInvariant() switch
				{
					"true" or "yes" or "on" or "1" => true,
					"false" or "no" or "off" or "0" => false,
					_ => config.AutoBanBlacklisted
				};

				await repository.ReplaceOneAsync(config);
				await context.RespondAsync($"Auto-ban Blacklist has been turned **{(config.AutoBanBlacklisted ? "on" : "off")}**.");
			}


			[Command("register"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task RegisterAsync(CommandContext context, string username, [EmailAddress] string email, string password)
			{
				await context.Message.DeleteAsync();
				AuthRegisterCredentials credentials = new(username, email, password);
				AuthResponse<IAuthComponent> result = await auth.RegisterNewUserAsync(credentials);

				await context.Channel.SendMessageAsync($"{context.User.Mention} {result.Status} : {result.Message}\n");
			}
		}
	}
}
