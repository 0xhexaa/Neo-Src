using System.Collections.Generic;
using NeoLauncher.Models.Services.Launcher;

namespace NeoLauncher.Models.Services.Analytics;

public class PostEventRequest
{
	public required EventType EventType { get; init; }

	public required string SessionId { get; init; }

	public string? AccountId { get; init; }

	public required string AppVersion { get; init; }

	public string? OsVersion { get; init; }

	public string? Locale { get; init; }

	public Dictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}
