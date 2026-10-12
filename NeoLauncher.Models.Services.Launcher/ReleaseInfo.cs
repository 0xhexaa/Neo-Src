namespace NeoLauncher.Models.Services.Launcher;

public class ReleaseInfo
{
	public required string PackageId { get; init; }

	public required string Version { get; init; }

	public required string DownloadUrl { get; init; }

	public required string Sha256 { get; init; }

	public required long SizeBytes { get; init; }

	public required bool IsDelta { get; init; }
}
