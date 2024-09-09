using System.Security.Cryptography;
using System.Text;
using SocialGuard.YC.Data.Models.Config;
using YumeChan.PluginBase.Tools;

namespace SocialGuard.YC.Services.Security;

public class LocalEncryptionService : IEncryptionService
{
	private readonly string encryptionKey;
	private static readonly byte[] salt = { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 };


	public LocalEncryptionService(IInterfaceConfigProvider<IApiConfig> configProvider)
	{
		encryptionKey = configProvider.InitConfig(PluginManifest.ApiConfigFileName).PopulateApiConfig().EncryptionKey;
	}

	public string Encrypt(string input)
	{
		byte[] clearBytes = Encoding.Unicode.GetBytes(input);

		using (Aes encryptor = Aes.Create())
		{
			Rfc2898DeriveBytes pdb = new(encryptionKey, salt);
			encryptor.Key = pdb.GetBytes(32);
			encryptor.IV = pdb.GetBytes(16);

			using MemoryStream ms = new();
			using (CryptoStream cs = new(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
			{
				cs.Write(clearBytes, 0, clearBytes.Length);
				cs.Close();
			}

			input = Convert.ToBase64String(ms.ToArray());
		}

		return input;
	}

	public Task<string> EncryptAsync(string input) => Task.FromResult(Encrypt(input));

	public string Decrypt(string cipherText)
	{
		cipherText = cipherText.Replace(" ", "+");
		byte[] cipherBytes = Convert.FromBase64String(cipherText);

		using (Aes encryptor = Aes.Create())
		{
			Rfc2898DeriveBytes pdb = new(encryptionKey, salt);
			encryptor.Key = pdb.GetBytes(32);
			encryptor.IV = pdb.GetBytes(16);

			using MemoryStream ms = new();
			using (CryptoStream cs = new(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
			{
				cs.Write(cipherBytes, 0, cipherBytes.Length);
				cs.Close();
			}

			cipherText = Encoding.Unicode.GetString(ms.ToArray());
		}

		return cipherText;
	}

	public Task<string> DecryptAsync(string input) => Task.FromResult(Decrypt(input));


	internal string GetKey() => encryptionKey;
}
