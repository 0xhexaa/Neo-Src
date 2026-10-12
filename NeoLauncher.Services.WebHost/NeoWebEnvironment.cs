using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace NeoLauncher.Services.WebHost;

public static class NeoWebEnvironment
{
	public const string VirtualHost = "neo.app";

	private static Task<CoreWebView2Environment>? _environmentTask;

	public static string WebRootPath => Path.Combine(AppContext.BaseDirectory, "web");

	public static bool RuntimeMissing { get; private set; }

	public static Task<CoreWebView2Environment> GetAsync()
	{
		Task<CoreWebView2Environment> environmentTask = _environmentTask;
		if (environmentTask != null && !environmentTask.IsFaulted && !environmentTask.IsCanceled)
		{
			return environmentTask;
		}
		return _environmentTask = CreateAsync();
	}

	private static async Task<CoreWebView2Environment> CreateAsync()
	{
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeoLauncher", "WebView2");
		Directory.CreateDirectory(text);
		CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions
		{
			AdditionalBrowserArguments = "--autoplay-policy=no-user-gesture-required"
		};
		try
		{
			CoreWebView2Environment result = await CoreWebView2Environment.CreateWithOptionsAsync(null, text, options);
			RuntimeMissing = false;
			return result;
		}
		catch (Exception ex) when ((ex is FileNotFoundException || ex is DllNotFoundException) ? true : false)
		{
			RuntimeMissing = true;
			NeoLog.Error("webview", "Microsoft Edge WebView2 Runtime is not installed.", ex);
			throw new WebView2RuntimeMissingException(ex);
		}
	}
}
