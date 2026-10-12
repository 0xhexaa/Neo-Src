using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Lightswitch;
using NeoLauncher.Services.Account;
using NeoLauncher.Utils;

namespace NeoLauncher.Services.Launcher;

public class LightswitchService
{
	private const string BaseUri = "https://lightswitch-public-service-prod.neofn.dev/lightswitch";

	private const string FortniteServiceId = "fortnite";

	private readonly AccountService _accountService;

	private readonly HttpClient _httpClient = new HttpClient
	{
		BaseAddress = new Uri("https://lightswitch-public-service-prod.neofn.dev/lightswitch".TrimEnd('/') + "/"),
		Timeout = TimeSpan.FromSeconds(15L)
	};

	public bool IsBanned { get; private set; }

	public string BanMessage { get; private set; } = string.Empty;

	public string[] AllowedActions { get; private set; } = Array.Empty<string>();

	public bool IsStatusKnown { get; private set; }

	public bool IsServiceUp { get; private set; }

	public string StatusMessage { get; private set; } = string.Empty;

	public event Action? StatusChanged;

	public LightswitchService(AccountService accountService)
	{
		_accountService = accountService;
	}

	public async Task CheckStatusAsync()
	{
		ServiceStatusResponse serviceStatusResponse = await GetServiceStatusAsync("fortnite");
		IsBanned = serviceStatusResponse?.Banned ?? false;
		BanMessage = ((!IsBanned) ? string.Empty : (serviceStatusResponse?.BanReason ?? string.Empty));
		AllowedActions = serviceStatusResponse?.AllowedActions ?? Array.Empty<string>();
		IsStatusKnown = serviceStatusResponse != null;
		IsServiceUp = serviceStatusResponse?.IsUp ?? false;
		StatusMessage = serviceStatusResponse?.Message ?? string.Empty;
		StatusChanged?.Invoke();
	}

	public async Task<string> GetFortniteStatusAsync()
	{
		ServiceStatusResponse serviceStatusResponse = await GetServiceStatusAsync("fortnite");
		if (serviceStatusResponse == null)
		{
			return "Unknown";
		}
		return serviceStatusResponse.IsUp ? "Active" : "Down";
	}

	private async Task<ServiceStatusResponse?> GetServiceStatusAsync(string serviceId)
	{
		_ = 2;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/service/" + serviceId + "/status");
			HttpRequestHeaders headers = request.Headers;
			headers.Authorization = new BearerAuthenticationValue(await _accountService.GetClientCredentialsAccessTokenAsync());
			HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
			httpResponseMessage.EnsureSuccessStatusCode();
			return await httpResponseMessage.Content.ReadFromJsonAsync<ServiceStatusResponse>();
		}
		catch
		{
			return null;
		}
	}
}
