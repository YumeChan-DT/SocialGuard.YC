using DSharpPlus;
using Microsoft.AspNetCore.Authorization;

namespace SocialGuard.YC.Infrastructure.Security.Authorization;

public static class AuthorizationExtensions
{
	public const string RequireManageGuildPermission = "SG.RequireManageGuildPermission";
	public const string RequireBanMembersPermission = "SG.RequireBanMembersPermission";
	
	public static AuthorizationPolicyBuilder RequireGuildRole(this AuthorizationPolicyBuilder builder, Permissions permissions)
	{
		builder.AddRequirements(new GuildPermissionsRequirement(permissions));
		return builder;
	}
}