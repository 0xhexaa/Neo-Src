using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Analytics;
using NeoLauncher.Models.Services.Launcher;

namespace NeoLauncher.Services.Launcher;

public class AnalyticsService
{
	private const string BaseUri = "https://analytics-public-service-prod.neofn.dev/analytics";

	private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
	{
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	private readonly HttpClient _httpClient;

	private readonly string _sessionId;

	private readonly string _appVersion;

	private readonly string _osVersion;

	public AnalyticsService()
	{
		_httpClient = new HttpClient
		{
			BaseAddress = new Uri("https://analytics-public-service-prod.neofn.dev/analytics".TrimEnd('/') + "/"),
			Timeout = TimeSpan.FromSeconds(10L)
		};
		_sessionId = Guid.NewGuid().ToString("N");
		_appVersion = App.UpdateService.CurrentVersion ?? Assembly.GetEntryAssembly().GetName().Version.ToString();
		_osVersion = GetOsVersion();
	}

	public void Track(EventType eventType, Dictionary<string, object>? properties = null)
	{
		if (App.SettingsService.Settings.SendDiagnostics)
		{
			PostEventAsync(eventType, properties);
		}
	}

	private async Task PostEventAsync(EventType eventType, Dictionary<string, object>? properties)
	{
		try
		{
			PostEventRequest value = new PostEventRequest
			{
				EventType = eventType,
				SessionId = _sessionId,
				AccountId = App.AccountService.CurrentUser?.Id,
				AppVersion = _appVersion,
				OsVersion = _osVersion,
				Locale = App.SettingsService.Settings.Language,
				Properties = (properties ?? new Dictionary<string, object>())
			};
			await _httpClient.PostAsJsonAsync("api/v1/public/event", value, _jsonOptions);
		}
		catch
		{
		}
	}

	private static string GetOsVersion()
	{
		Version version = Environment.OSVersion.Version;
		if (version.Build >= 22000)
		{
			return $"Windows 11 Build {version.Build}";
		}
		return $"Windows 10 Build {version.Build}";
	}
}
