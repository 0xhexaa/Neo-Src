using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Account.OAuth;

public class ExchangeCodeResponse : BaseResponse
{
	[JsonPropertyName("code")]
	public required string Code { get; set; }

	[JsonPropertyName("creatingClientId")]
	public required string CreatingClientId { get; set; }

	[JsonPropertyName("expiresInSeconds")]
	public required int ExpiresInSeconds { get; set; }
}
