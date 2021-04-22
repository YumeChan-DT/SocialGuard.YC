namespace SocialGuard.YC.Data.Components
{
	public record AuthCredentials(string Username, string Password);
	public record AuthRegisterCredentials(string Username, string Email, string Password) : AuthCredentials(Username, Password);
}
