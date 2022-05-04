using System.Security.Claims;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SocialGuard.YC.Infrastructure.Security.Authorization;

public class GuildAccessAuthorizationHandler : AuthorizationHandler<GuildPermissionsRequirement, ulong>
{
	private readonly ILogger<GuildAccessAuthorizationHandler> _logger;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly DiscordClient _discordClient;


	public GuildAccessAuthorizationHandler(ILogger<GuildAccessAuthorizationHandler> logger, IHttpContextAccessor httpContextAccessor, DiscordClient discordClient)
	{
		_logger = logger;
		_httpContextAccessor = httpContextAccessor;
		_discordClient = discordClient;
	}

	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, GuildPermissionsRequirement requirement, ulong guildId)
	{
		if (context.User.Identity is not { IsAuthenticated: true })
		{
			context.Fail(new(this, "User is not authenticated."));
		}

		if (!ulong.TryParse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out ulong userId))
		{
			context.Fail(new(this, "User id could not be parsed."));
		}

		if (!_discordClient.Guilds.TryGetValue(guildId, out DiscordGuild? discordGuild))
		{
			context.Fail(new(this, "Failed to resolve Discord guild from current request guild."));

			return;
		}

		// Check if the user is present in the guild.
		if (!discordGuild.Members.TryGetValue(userId, out DiscordMember? discordMember))
		{
			context.Fail(new(this, $"User is not present in the guild {guildId}."));

			return;
		}

		// Check if the user has the required permissions.
		if ((discordMember.Permissions & requirement.RequiredPermissions) is not 0)
		{
			context.Succeed(requirement);

			return;
		}

		// Didn't match.
		context.Fail(new(this, $"User does not have the required permissions in the guild {guildId}."));

		return;
	}
}