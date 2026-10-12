using System;

namespace NeoLauncher.Services.WebHost;

public sealed class WebView2RuntimeMissingException : Exception
{
	public WebView2RuntimeMissingException(Exception inner)
		: base("Neo needs the Microsoft Edge WebView2 Runtime. Install it and try again.", inner)
	{
	}
}
