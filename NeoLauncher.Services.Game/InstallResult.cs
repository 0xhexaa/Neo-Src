namespace NeoLauncher.Services.Game;

public class InstallResult
{
	public bool Success { get; init; }

	public bool Cancelled { get; init; }

	public string ErrorCode { get; init; } = "";

	public string ErrorText { get; init; } = "";

	public string InstallPath { get; init; } = "";

	public string Version { get; init; } = "";

	public string BuildId { get; init; } = "";
}
