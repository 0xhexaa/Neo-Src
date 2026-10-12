using System.IO;
using System.Reflection;

namespace NeoLauncher.Services.WebHost;

internal static class NeoBridgeScript
{
	public static string Source
	{
		get
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("NeoLauncher.Scripts.neoBridge.js");
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
	}
}
