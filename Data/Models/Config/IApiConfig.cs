namespace SocialGuard.YC.Data.Models.Config
{
	public interface IApiConfig
	{
		public string ApiHost { get; set; }
		public string EncryptionKey { get; set; }

		public IAzureClientIdentity? AzureIdentity { get; set; }
		public string KeyVaultUri { get; set; }
	}

	public interface IAzureClientIdentity
	{
		public string TenantId { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
	}
}
