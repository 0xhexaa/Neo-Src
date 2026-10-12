using System;
using System.Text.Json.Serialization;
using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Models.Services.Launcher;

public class BuildInfo
{
	[JsonPropertyName("version")]
	public required GameVersion Version { get; init; }

	[JsonPropertyName("fileSizeBytes")]
	public required long FileSizeBytes { get; init; }

	[JsonPropertyName("releaseDate")]
	public required DateOnly ReleaseDate { get; init; }

	[JsonPropertyName("isLive")]
	public required bool IsLive { get; init; }

	[JsonPropertyName("splashUrl")]
	public required string SplashUrl { get; init; }

	[JsonPropertyName("manifestPath")]
	public required string ManifestPath { get; init; }

	[JsonIgnore]
	public string Name => Version.Name;
}
