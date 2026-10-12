using System;
using System.IO;

namespace NeoLauncher;

public static class Config
{
	public const string BackendScheme = "https";

	public const string BackendHost = "neofn.dev";

	public const string BackendUri = "https://neofn.dev";

	public const string ContentCdnUri = "https://content-cdn.neofn.dev";

	public const string VivoxSdkUri = "https://content-cdn.neofn.dev/launcher/vivoxsdk_x64.dll";

	public const string XmppUri = "wss://xmpp-service-prod.neofn.dev";

	public const string XmppDomain = "xmpp-service-prod.neofn.dev";

	public static readonly string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeoLauncherData");
}
