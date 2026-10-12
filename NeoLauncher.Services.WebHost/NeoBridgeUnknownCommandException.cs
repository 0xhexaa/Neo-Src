using System;

namespace NeoLauncher.Services.WebHost;

public sealed class NeoBridgeUnknownCommandException : Exception
{
	public NeoBridgeUnknownCommandException(string command)
		: base("unknown command: " + command)
	{
	}
}
