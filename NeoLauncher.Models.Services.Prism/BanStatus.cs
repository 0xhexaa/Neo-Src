using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Prism;

public class BanStatus
{
	[JsonPropertyName("banned")]
	public bool Banned { get; set; }

	[JsonPropertyName("reason")]
	public string? Reason { get; set; }

	[JsonPropertyName("bannedAt")]
	public DateTime? BannedAt { get; set; }
}
