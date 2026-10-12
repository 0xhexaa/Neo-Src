using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Launcher;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
	LauncherFirstRun,
	LauncherOpened,
	LauncherClosed,
	LauncherUpdated,
	LauncherUpdateFailed,
	LauncherLogin,
	LauncherLoginFailed,
	LauncherLogout,
	BuildDownloaded,
	BuildDownloadFailed,
	GameImported,
	GameUninstalled,
	GameRepaired,
	GameLaunched,
	GameLaunchFailed,
	GameClosed,
	StoreOpened,
	StoreClosed,
	StoreItemViewed,
	StoreItemPurchased
}
