using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System;
using DSharpPlus.Entities;
using DSharpPlus;

namespace SocialGuard.YC
{
	public static class Utilities
	{
		internal static string SignatureFooter = $"NSYS SocialGuard (YC) v{PluginManifest.VersionString}";

		internal static JsonSerializerOptions SerializerOptions => new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		public static async Task<TData> ParseResponseFullAsync<TData>(HttpResponseMessage response) => JsonSerializer.Deserialize<TData>(await response.Content.ReadAsStringAsync(), SerializerOptions);
	
		public static IApiConfig PopulateApiConfig(this IApiConfig config)
		{
			config.ApiHost ??= "https://socialguard.net";
			config.EncryptionKey ??= GenerateLocalMasterKey();

			return config;
		}

		public static DiscordEmbed BuildUserRecordEmbed(TrustlistUser trustlistUser, DiscordUser discordUser)
		{
			(DiscordColor color, string name, string desc) = trustlistUser?.EscalationLevel switch
			{
				null or 0 => (DiscordColor.Green, "Clear", "This user has no record, and is cleared safe."),
				1 => (DiscordColor.Blue, "Suspicious", "This user is marked as suspicious. Their behaviour should be monitored."),
				2 => (DiscordColor.Orange, "Untrusted", "This user is marked as untrusted. Please exerce caution when interacting with them."),
				>= 3 => (DiscordColor.Red, "Blacklisted", "This user is dangerous and has been blacklisted. Banning this user is greatly advised.")
			};

			DiscordEmbedBuilder builder = new()
			{
				Title = $"Trustlist User : {discordUser.Username}",
				Color = color,
				Description = desc,
				Footer = new() { Text = SignatureFooter }
			};

			builder.AddField("ID", $"`{discordUser.Id}`", true);
			builder.AddField("Account Created", discordUser.CreationTimestamp.UtcDateTime.ToString(), true);

			if (trustlistUser is not null)
			{
				builder
					.AddField("Emitter", $"{trustlistUser.Emitter.DisplayName} (`{trustlistUser.Emitter.Login}`)")
					.AddField("Escalation Level", $"**{trustlistUser.EscalationLevel}** - {name}", true)
					.AddField("First Entered", trustlistUser.EntryAt.ToString(), true)
					.AddField("Last Escalation", trustlistUser.LastEscalated.ToString(), true)
					.AddField("Reason", trustlistUser.EscalationNote);
			}

			return builder.Build();
		}

		public static async Task<GuildConfig> FindOrCreateConfigAsync(this IEntityRepository<GuildConfig, ulong> repository, ulong guildId)
		{
			GuildConfig config = await repository.FindByIdAsync(guildId);

			if (config is null)
			{
				await repository.InsertOneAsync(config = new() { Id = guildId });
			}

			return config;
		}


		internal static string GenerateLocalMasterKey()
		{
			using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

			byte[] bytes = new byte[96];
			randomNumberGenerator.GetBytes(bytes);
			string localMasterKeyBase64 = Convert.ToBase64String(bytes);
			return localMasterKeyBase64;
		}

		public static DiscordEmbedBuilder WithAuthor(this DiscordEmbedBuilder embed, DiscordUser user)
		{
			return embed.WithAuthor(user.GetFullUsername(), null, user.GetAvatarUrl(ImageFormat.Auto, 128));
		}

		public static string GetFullUsername(this DiscordUser user) => $"{user.Username}#{user.Discriminator}";
	}
}
