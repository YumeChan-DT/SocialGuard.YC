namespace SocialGuard.YC.Services.Security;

public interface IEncryptionService
{
	public string Encrypt(string input);
	public Task<string> EncryptAsync(string input);

	public string Decrypt(string input);
	public Task<string> DecryptAsync(string input);
}
