using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Models;

public class PendingDownload
{
	public required long FileSizeBytes { get; init; }

	public required string ManifestPath { get; init; }

	public required InstalledVersion Build { get; init; }

	public string Name => Build.Version.Name;
}
