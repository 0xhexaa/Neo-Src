using System.Collections.Generic;
using NeoLauncher.Models.UI.Appearance;

namespace NeoLauncher.Models;

public class AppSettings
{
	public AppTheme Theme { get; set; } = AppTheme.System;

	public BackdropType Material { get; set; }

	public string Language { get; set; } = string.Empty;

	public bool SendDiagnostics { get; set; } = true;

	public bool HasLaunched { get; set; }

	public string DownloadFolder { get; set; } = string.Empty;

	public string ProfileStatusId { get; set; } = "online";

	public int LauncherFps { get; set; } = 60;

	public bool LiteMode { get; set; }

	public bool OpenOnStartup { get; set; }

	public string AfterLaunchAction { get; set; } = "close";

	public Dictionary<string, string> FriendNicknames { get; set; } = new Dictionary<string, string>();

	public List<string> NotificationSeenIds { get; set; } = new List<string>();

	public string DevModuleOverridePath { get; set; } = string.Empty;
}
