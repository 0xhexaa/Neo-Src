using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Launcher;

public class OnlineCounts
{
	[JsonPropertyName("fortnite")]
	public int Fortnite { get; set; }

	[JsonPropertyName("launcher")]
	public int Launcher { get; set; }
}
