using Microsoft.Windows.ApplicationModel.Resources;

namespace NeoLauncher.Localization;

public static class Strings
{
	private static readonly ResourceManager _manager = new ResourceManager();

	private static ResourceContext _context = _manager.CreateResourceContext();

	public static string OK => Get("OK");

	public static string Cancel => Get("Cancel");

	public static string Install => Get("Install");

	public static string Remove => Get("Remove");

	public static string UnknownError => Get("UnknownError");

	public static string CredentialsLoginError => Get("CredentialsLoginError");

	public static string OAuthRequestFailed => Get("OAuthRequestFailed");

	public static string OAuthResponseParsingFailed => Get("OAuthResponseParsingFailed");

	public static string AccountServiceUnreachable => Get("AccountServiceUnreachable");

	public static string Logout => Get("Logout");

	public static string LogoutTitle => Get("LogoutTitle");

	public static string LogoutMessage => Get("LogoutMessage");

	public static string Friends => Get("Friends");

	public static string PlayLaunch => Get("PlayLaunch");

	public static string PlayInstall => Get("PlayInstall");

	public static string News => Get("News");

	public static string LibraryLaunchFailedTitle => Get("LibraryLaunchFailedTitle");

	public static string LibraryConfigureTitle => Get("LibraryConfigureTitle");

	public static string LibraryConfigureDescription => Get("LibraryConfigureDescription");

	public static string LibraryConfigureInstallPath => Get("LibraryConfigureInstallPath");

	public static string LibraryConfigureVersion => Get("LibraryConfigureVersion");

	public static string LibraryUninstallTitle => Get("LibraryUninstallTitle");

	public static string LibraryUninstallMessage => Get("LibraryUninstallMessage");

	public static string LibraryUninstallConfirm => Get("LibraryUninstallConfirm");

	public static string LibraryUninstallFailed => Get("LibraryUninstallFailed");

	public static string LibraryRepair => Get("LibraryRepair");

	public static string LibraryRepairFailedTitle => Get("LibraryRepairFailedTitle");

	public static string LibraryRepairBuildNotFound => Get("LibraryRepairBuildNotFound");

	public static string Downloading => Get("Downloading");

	public static string AvailableVersions => Get("AvailableVersions");

	public static string Pause => Get("Pause");

	public static string Resume => Get("Resume");

	public static string Dismiss => Get("Dismiss");

	public static string ResumeDownloadsTitle => Get("ResumeDownloadsTitle");

	public static string ResumeDownloadsMessage => Get("ResumeDownloadsMessage");

	public static string Resuming => Get("Resuming");

	public static string FetchingManifest => Get("FetchingManifest");

	public static string FailedToStart => Get("FailedToStart");

	public static string ChooseInstallLocation => Get("ChooseInstallLocation");

	public static string DriveSelectPrompt => Get("DriveSelectPrompt");

	public static string NotEnoughSpace => Get("NotEnoughSpace");

	public static string FreeSpace => Get("FreeSpace");

	public static string DownloadStarted => Get("DownloadStarted");

	public static string DownloadStartedMessage => Get("DownloadStartedMessage");

	public static string DownloadComplete => Get("DownloadComplete");

	public static string DownloadCompleteMessage => Get("DownloadCompleteMessage");

	public static string DownloadFailed => Get("DownloadFailed");

	public static string DownloadFailedMessage => Get("DownloadFailedMessage");

	public static string FailedStatus => Get("FailedStatus");

	public static string Notifications => Get("Notifications");

	public static string MarkAllRead => Get("MarkAllRead");

	public static string NoNotifications => Get("NoNotifications");

	public static string TypeMessage => Get("TypeMessage");

	public static string FriendsJoin => Get("FriendsJoin");

	public static string FriendsChat => Get("FriendsChat");

	public static string FriendsBlock => Get("FriendsBlock");

	public static string FriendsUnblock => Get("FriendsUnblock");

	public static string FriendsAllowRequests => Get("FriendsAllowRequests");

	public static string FriendsShowLastSeen => Get("FriendsShowLastSeen");

	public static string FriendsChatTab => Get("FriendsChatTab");

	public static string FriendsListTab => Get("FriendsListTab");

	public static string FriendsAddTab => Get("FriendsAddTab");

	public static string FriendsSearchConversations => Get("FriendsSearchConversations");

	public static string FriendsRecent => Get("FriendsRecent");

	public static string FriendsSearchFriends => Get("FriendsSearchFriends");

	public static string FriendsOnline => Get("FriendsOnline");

	public static string FriendsOffline => Get("FriendsOffline");

	public static string FriendsBlocked => Get("FriendsBlocked");

	public static string FriendsAddFriend => Get("FriendsAddFriend");

	public static string FriendsAddDescription => Get("FriendsAddDescription");

	public static string FriendsUsername => Get("FriendsUsername");

	public static string FriendsSendRequest => Get("FriendsSendRequest");

	public static string FriendsIncoming => Get("FriendsIncoming");

	public static string FriendsIncomingRequest => Get("FriendsIncomingRequest");

	public static string FriendsAccept => Get("FriendsAccept");

	public static string FriendsDecline => Get("FriendsDecline");

	public static string FriendsOutgoing => Get("FriendsOutgoing");

	public static string FriendsPending => Get("FriendsPending");

	public static string FriendsCancelRequest => Get("FriendsCancelRequest");

	public static string FriendsSettings => Get("FriendsSettings");

	public static string FriendsOnlineStatus => Get("FriendsOnlineStatus");

	public static string FriendsOfflineStatus => Get("FriendsOfflineStatus");

	public static string FriendsBlockedStatus => Get("FriendsBlockedStatus");

	public static string FriendsInLauncher => Get("FriendsInLauncher");

	public static string SettingsAppearance => Get("SettingsAppearance");

	public static string SettingsAppTheme => Get("SettingsAppTheme");

	public static string SettingsAppThemeDescription => Get("SettingsAppThemeDescription");

	public static string SettingsThemeLight => Get("SettingsThemeLight");

	public static string SettingsThemeDark => Get("SettingsThemeDark");

	public static string SettingsThemeSystem => Get("SettingsThemeSystem");

	public static string SettingsMaterialType => Get("SettingsMaterialType");

	public static string SettingsMaterialDescription => Get("SettingsMaterialDescription");

	public static string SettingsMaterialSolid => Get("SettingsMaterialSolid");

	public static string SettingsMaterialGlass => Get("SettingsMaterialGlass");

	public static string SettingsGeneral => Get("SettingsGeneral");

	public static string SettingsNotifications => Get("SettingsNotifications");

	public static string SettingsNotificationsDescription => Get("SettingsNotificationsDescription");

	public static string SettingsLanguage => Get("SettingsLanguage");

	public static string SettingsLanguageDescription => Get("SettingsLanguageDescription");

	public static string SettingsPrivacy => Get("SettingsPrivacy");

	public static string SettingsDiagnostics => Get("SettingsDiagnostics");

	public static string SettingsDiagnosticsDescription => Get("SettingsDiagnosticsDescription");

	public static string SettingsAbout => Get("SettingsAbout");

	public static string SettingsPrivacyPolicy => Get("SettingsPrivacyPolicy");

	public static string SettingsTermsOfService => Get("SettingsTermsOfService");

	public static string GameLaunched => Get("GameLaunched");

	public static string GameLaunchedMessage => Get("GameLaunchedMessage");

	public static string GameClosed => Get("GameClosed");

	public static string GameClosedMessage => Get("GameClosedMessage");

	public static string LaunchAlreadyRunning => Get("LaunchAlreadyRunning");

	public static string LaunchExchangeCodeFailed => Get("LaunchExchangeCodeFailed");

	public static string LaunchBinaryNotFound => Get("LaunchBinaryNotFound");

	public static string LaunchProcessFailed => Get("LaunchProcessFailed");

	public static string LaunchProcessExited => Get("LaunchProcessExited");

	public static string LaunchTimeout => Get("LaunchTimeout");

	public static string LaunchOpenProcessFailed => Get("LaunchOpenProcessFailed");

	public static string LaunchLoadLibraryFailed => Get("LaunchLoadLibraryFailed");

	public static string LaunchAllocFailed => Get("LaunchAllocFailed");

	public static string LaunchThreadFailed => Get("LaunchThreadFailed");

	public static string LaunchModuleInUse => Get("LaunchModuleInUse");

	public static string LaunchModuleNotFound => Get("LaunchModuleNotFound");

	public static string FatalError => Get("FatalError");

	public static string FatalErrorTitle => Get("FatalErrorTitle");

	public static void SetLanguage(string language)
	{
		_context = _manager.CreateResourceContext();
		_context.QualifierValues["Language"] = language;
	}

	private static string Get(string key)
	{
		return _manager.MainResourceMap.GetValue("Resources/" + key, _context).ValueAsString;
	}
}
