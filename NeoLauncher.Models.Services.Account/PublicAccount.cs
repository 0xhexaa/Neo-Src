using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Account;

public class PublicAccount : BaseResponse
{
	[JsonPropertyName("id")]
	public required string Id { get; set; }

	[JsonPropertyName("displayName")]
	public required string DisplayName { get; set; }
}
