using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Launcher;

public class InstalledVersion : IEquatable<InstalledVersion>
{
	[JsonPropertyName("version")]
	public required GameVersion Version { get; set; }

	[JsonPropertyName("path")]
	public string Path { get; set; } = string.Empty;

	[JsonPropertyName("source")]
	public string Source { get; set; } = "installed";

	[JsonPropertyName("installedAt")]
	public string InstalledAt { get; set; } = string.Empty;

	[JsonPropertyName("installSizeBytes")]
	public long InstallSizeBytes { get; set; }

	public bool Equals(InstalledVersion? other)
	{
		if (other != null)
		{
			return Version.CL == other.Version.CL;
		}
		return false;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as InstalledVersion);
	}

	public override int GetHashCode()
	{
		return Version.CL.GetHashCode();
	}
}
