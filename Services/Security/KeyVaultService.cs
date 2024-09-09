using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using SocialGuard.YC.Data.Models.Config;

namespace SocialGuard.YC.Services.Security;

internal class KeyVaultService : IEncryptionService
{
	internal static EncryptionAlgorithm Algorithm => EncryptionAlgorithm.RsaOaep256;

	private readonly ClientSecretCredential _clientSecretCredential;
	private readonly KeyClient _keyClient;
	private CryptographyClient _cryptographyClient;

#if !DEBUG
	internal protected const string PasswordEncryptionKeyName = "PasswordEncryption";
#else
	internal protected const string PasswordEncryptionKeyName = "DEV-PasswordEncryption";
#endif


	public KeyVaultService(IApiConfig apiConfig)
	{
		_clientSecretCredential = new(apiConfig.AzureIdentity.TenantId, apiConfig.AzureIdentity.ClientId, apiConfig.AzureIdentity.ClientSecret);
		_keyClient = new(new(apiConfig.KeyVaultUri), _clientSecretCredential);
	}

	public async Task InitializeAsync(CancellationToken ct)
	{
		KeyVaultKey vaultKey = null;


		await foreach (KeyProperties keyProp in _keyClient.GetPropertiesOfKeysAsync(ct))
		{
			if (keyProp is { Name: PasswordEncryptionKeyName, Enabled: true }
				&& (await _keyClient.GetKeyAsync(PasswordEncryptionKeyName, cancellationToken: ct)).Value is { } key 
				&& key.KeyOperations.Contains(KeyOperation.Decrypt) && key.KeyOperations.Contains(KeyOperation.Encrypt))
			{
				vaultKey = key;
				break;
			}
		}

		vaultKey ??= await _keyClient.CreateKeyAsync(PasswordEncryptionKeyName, KeyType.Rsa, cancellationToken: ct);

		_cryptographyClient = new(vaultKey.Id, _clientSecretCredential);
	}

	public string Encrypt(string input)
	{
		EncryptResult encryptResult = _cryptographyClient.Encrypt(Algorithm, Encoding.Unicode.GetBytes(input));
		return Convert.ToBase64String(encryptResult.Ciphertext);
	}

	public async Task<string> EncryptAsync(string input)
	{
		EncryptResult encryptResult = await _cryptographyClient.EncryptAsync(Algorithm, Encoding.Unicode.GetBytes(input));
		return Convert.ToBase64String(encryptResult.Ciphertext);
	}

	public string Decrypt(string input)
	{
		DecryptResult decryptResult = _cryptographyClient.Decrypt(Algorithm, Convert.FromBase64String(input));
		return Encoding.Unicode.GetString(decryptResult.Plaintext);
	}

	public async Task<string> DecryptAsync(string input)
	{
		DecryptResult decryptResult = await _cryptographyClient.DecryptAsync(Algorithm, Convert.FromBase64String(input));
		return Encoding.Unicode.GetString(decryptResult.Plaintext);
	}
}
