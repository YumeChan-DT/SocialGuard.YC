using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using YumeChan.PluginBase.Tools.Data;
using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Services;
using SocialGuard.YC.Services.Security;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MongoDB.Driver;
using SocialGuard.Common.Data.Models.Authentication;
using System.Threading;
using System.Text;

namespace SocialGuard.YC.Modules
{
	public partial class BaseModule
	{
		[Group("config")]
		public class GuildConfigModule : BaseCommandModule
		{
			private readonly IMongoCollection<GuildConfig> guildConfig;
			private readonly AuthApiService auth;
			private readonly IEncryptionService _encryption;

			public GuildConfigModule(IDatabaseProvider<PluginManifest> database, AuthApiService auth, IEncryptionService encryption)
			{
				guildConfig = database.GetMongoDatabase().GetCollection<GuildConfig>(nameof(GuildConfig));
				this.auth = auth;
				_encryption = encryption;
			}


			[Command("joinlog"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task GetJoinLogAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				DiscordChannel current = context.Guild.GetChannel(config.JoinLogChannel);
				await context.RespondAsync($"Current Join Log channel : {current?.Mention ?? "None"}.");
			}
			[Command("joinlog")]
			public async Task SetJoinLogAsync(CommandContext context, DiscordChannel channel)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.JoinLogChannel = channel.Id;
				await guildConfig.SetJoinlogAsync(config);
				await context.RespondAsync($"Join Log channel set to : {context.Guild.GetChannel(config.JoinLogChannel).Mention}.");
			}

			[Command("leavelog"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task GetLeaveLogAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				DiscordChannel current = context.Guild.GetChannel(config.LeaveLogChannel);

				StringBuilder sb = new();
				sb.AppendFormat($"Leave Log is currently **{(config.LeaveLogEnabled ? "on" : "off")}**.");

				if (config.LeaveLogEnabled)
				{
					sb.AppendFormat($"\nCurrent Leave Log channel: {context.Guild.GetChannel(config.LeaveLogChannel)?.Mention ?? "None"}.");
				}

				await context.RespondAsync(sb.ToString());
			}
			[Command("leavelog")]
			public async Task SetLeaveLogAsync(CommandContext context, string key, DiscordChannel channel)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.LeaveLogEnabled = Utilities.ParseBoolParameter(key) ?? config.LeaveLogEnabled;
				config.LeaveLogChannel = channel.Id;
				await guildConfig.SetLeavelogAsync(config);
				await context.RespondAsync($"Leave Log is now **{(config.LeaveLogEnabled ? "on" : "off")}**. Channel set to : {context.Guild.GetChannel(config.LeaveLogChannel).Mention}.");
			}


			[Command("banlog"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task BanLogAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				DiscordChannel current = context.Guild.GetChannel(config.BanLogChannel);
				await context.RespondAsync($"Current Ban Log channel : {current?.Mention ?? "None"}.");
			}
			[Command("banlog")]
			public async Task BanLogAsync(CommandContext context, DiscordChannel channel)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.BanLogChannel = channel.Id;
				await guildConfig.SetBanlogAsync(config);
				await context.RespondAsync($"Ban Log channel set to : {context.Guild.GetChannel(config.BanLogChannel).Mention}.");
			}


			[Command("login"), Aliases("credentials"), Priority(10), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task ConfigureLoginAsync(CommandContext context, string username, string password)
			{
				await context.Message.DeleteAsync();

				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.ApiLogin = new() { Username = username, Password = _encryption.Encrypt(password) };

				await guildConfig.SetLoginAsync(config);

				await context.Channel.SendMessageAsync($"API credentials has been set.");
			}

			[Command("autoban"), RequireUserPermissions(Permissions.ManageGuild), RequireBotPermissions(Permissions.BanMembers)]
			public async Task AutobanAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				await context.RespondAsync($"Auto-ban Blacklist is **{(config.AutoBanBlacklisted ? "on" : "off")}**.");
			}
			[Command("autoban")]
			public async Task AutobanAsync(CommandContext context, string key)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.AutoBanBlacklisted = Utilities.ParseBoolParameter(key) ?? config.AutoBanBlacklisted;
				await guildConfig.SetAutobanAsync(config);
				await context.RespondAsync($"Auto-ban Blacklist has been turned **{(config.AutoBanBlacklisted ? "on" : "off")}**.");
			}

			[Command("joinlog-suppress"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task JoinlogSuppressAsync(CommandContext context)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);

				await context.RespondAsync(config.SuppressJoinlogCleanRecords
					? "All clean records are currently suppressed from displaying in Joinlog."
					: "All records are displayed in Joinlog.");
			}
			[Command("joinlog-suppress")]
			public async Task JoinlogSuppressAsync(CommandContext context, string key)
			{
				GuildConfig config = await guildConfig.FindOrCreateConfigAsync(context.Guild.Id);
				config.SuppressJoinlogCleanRecords = Utilities.ParseBoolParameter(key) ?? config.SuppressJoinlogCleanRecords;
				await guildConfig.SetJoinlogSuppressionAsync(config);

				await context.RespondAsync(config.SuppressJoinlogCleanRecords
					? "All clean records will now be suppressed from displaying in Joinlog."
					: "All records will now be displayed in Joinlog.");
			}


			[Command("register"), RequireUserPermissions(Permissions.ManageGuild)]
			public async Task RegisterAsync(CommandContext context, string username, [EmailAddress] string email, string password)
			{
				await context.Message.DeleteAsync();
				RegisterModel credentials = new() { Username = username, Email = email, Password = password };
				Response result = await auth.RegisterNewUserAsync(credentials, CancellationToken.None);

				await context.Channel.SendMessageAsync($"{context.User.Mention} {result.Status} : {result.Message}\n");
			}
		}
	}
}
