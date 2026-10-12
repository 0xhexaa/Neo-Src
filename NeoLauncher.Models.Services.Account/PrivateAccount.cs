using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Account;

public class PrivateAccount : PublicAccount
{
	[JsonPropertyName("email")]
	public required string Email { get; set; }
}
