using DSharpPlus;
using Microsoft.AspNetCore.Authorization;

namespace SocialGuard.YC.Infrastructure.Security.Authorization;

public class GuildPermissionsRequirement : IAuthorizationRequirement
{
	public Permissions RequiredPermissions { get; init; }

	public GuildPermissionsRequirement(Permissions requirePermissions)
	{
		RequiredPermissions = requirePermissions;
	}
}