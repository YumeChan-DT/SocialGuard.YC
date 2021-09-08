using SocialGuard.YC.Data.Models.Config;
using SocialGuard.YC.Data.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System;
using DSharpPlus.Entities;
using DSharpPlus;
using MongoDB.Driver;
using System.Collections.Generic;
using SocialGuard.YC.Services;
using DSharpPlus.SlashCommands;

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

		public static async Task<DiscordEmbed> GetLookupEmbedAsync(this TrustlistUserApiService trustlist, DiscordUser user)
			=> Utilities.BuildUserRecordEmbed(await trustlist.LookupUserAsync(user.Id), user);

		public static DiscordEmbed BuildUserRecordEmbed(TrustlistUser trustlistUser, DiscordUser discordUser, TrustlistEntry entry = null)
		{
			entry ??= trustlistUser?.GetLatestMaxEntry();
			(DiscordColor color, string name, string desc) = GetEscalationDescriptions(entry?.EscalationLevel ?? 0);

			DiscordEmbedBuilder builder = new()
			{
				Title = $"Trustlist User : {discordUser.Username}",
				Color = color,
				Description = desc,
				Footer = new() { Text = SignatureFooter }
			};

			builder.AddField("ID", $"`{discordUser.Id}`", true);
			builder.AddField("Account Created", discordUser.CreationTimestamp.UtcDateTime.ToString(), true);

			if (entry is not null)
			{
				builder
					.AddField("Last Emitter", $"{entry.Emitter.DisplayName} (`{entry.Emitter.Login}`)")
					.AddField("Highest Escalation Level", $"**{entry.EscalationLevel}** - {name}", true)
					.AddField("Average Escalation Level", $"**{trustlistUser.GetMedianEscalationLevel():F2}**", true)
					.AddField("Total Entries", $"**{trustlistUser.Entries.Length}**", true)
					.AddField("First Entered", entry.EntryAt.ToString(), true)
					.AddField("Last Escalation", entry.LastEscalated.ToString(), true)
					.AddField("Last Reason", entry.EscalationNote);
			}

			return builder.Build();
		}

		private static (DiscordColor color, string name, string desc) GetEscalationDescriptions(byte escalationLevel)
		{
			return escalationLevel switch
			{
				0 => (DiscordColor.Green, "Clear", "This user has no record, and is cleared safe."),
				1 => (DiscordColor.Blue, "Suspicious", "This user is marked as suspicious. Their behaviour should be monitored."),
				2 => (DiscordColor.Orange, "Untrusted", "This user is marked as untrusted. Please exerce caution when interacting with them."),
				>= 3 => (DiscordColor.Red, "Blacklisted", "This user is dangerous and has been blacklisted. Banning this user is greatly advised.")
			};
		}

		public static async Task<GuildConfig> FindOrCreateConfigAsync(this IMongoCollection<GuildConfig> collection, ulong guildId)
		{
			GuildConfig config = (await collection.FindAsync(c => c.Id == guildId)).FirstOrDefault();

			if (config is null)
			{
				await collection.InsertOneAsync(config = new() { Id = guildId });
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

		public static bool? ParseBoolParameter(string parameter) =>	parameter.ToLowerInvariant() switch
		{
			"true" or "yes" or "on" or "1" => true,
			"false" or "no" or "off" or "0" => false,
			_ => null
		};

		public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IAsyncCursor<T> asyncCursor)
		{
			while (await asyncCursor.MoveNextAsync())
			{
				foreach (T current in asyncCursor.Current)
				{
					yield return current;
				}
			}
		}

		public static Task<DiscordMessage> FollowUpAsync(this BaseContext ctx, string content, bool isEphemeral = false)
			=> ctx.FollowUpAsync(new() { Content = content, IsEphemeral = isEphemeral });
	}
}
