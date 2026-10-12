using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Lightswitch;

public class ServiceStatusResponse
{
	[JsonPropertyName("serviceInstanceId")]
	public required string ServiceInstanceId { get; set; }

	[JsonPropertyName("status")]
	public required string Status { get; set; }

	[JsonPropertyName("message")]
	public required string Message { get; set; }

	[JsonPropertyName("banned")]
	public bool Banned { get; set; }

	[JsonPropertyName("banReason")]
	public string? BanReason { get; set; }

	[JsonPropertyName("allowedActions")]
	public string[] AllowedActions { get; set; } = Array.Empty<string>();

	[JsonIgnore]
	public bool IsUp => Status == "UP";
}
