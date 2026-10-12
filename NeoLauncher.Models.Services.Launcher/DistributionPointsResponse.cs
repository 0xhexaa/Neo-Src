using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Launcher;

public class DistributionPointsResponse
{
	[JsonPropertyName("distributions")]
	public required string[] Distributions { get; set; }
}
