using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.ComponentModel.DataAnnotations;



namespace Transcom.SocialGuard.YC.Data.Models
{
	public record TrustlistUser : IDocument<ulong>
	{
		[Key]
		public ulong Id { get; set; }

		public DateTime EntryAt { get; set; }

		public DateTime LastEscalated { get; set; }

		[Required, Range(0, 3)]
		public byte EscalationLevel { get; set; }

		[MinLength(5), MaxLength(2000)]
		public string EscalationNote { get; set; }
	}
}
