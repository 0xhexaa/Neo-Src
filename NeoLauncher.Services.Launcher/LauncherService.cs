using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Services.Account;
using NeoLauncher.Utils;

namespace NeoLauncher.Services.Launcher;

public class LauncherService(AccountService accountService)
{
	private const string BaseUri = "https://launcher-public-service-prod06.neofn.dev/launcher";

	private readonly HttpClient _httpClient = new HttpClient
	{
		BaseAddress = new Uri("https://launcher-public-service-prod06.neofn.dev/launcher".TrimEnd('/') + "/"),
		Timeout = TimeSpan.FromSeconds(15L)
	};

	public string[] DistributionPoints { get; private set; } = Array.Empty<string>();

	public List<BuildInfo> Builds { get; private set; } = new List<BuildInfo>();

	public BuildInfo? LiveBuild => Builds.Find((BuildInfo b) => b.IsLive);

	public async Task<List<ReleaseInfo>> GetReleasesAsync()
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/releases");
		HttpRequestHeaders headers = request.Headers;
		headers.Authorization = new BearerAuthenticationValue(await accountService.GetClientCredentialsAccessTokenAsync());
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
		httpResponseMessage.EnsureSuccessStatusCode();
		return (await httpResponseMessage.Content.ReadFromJsonAsync<List<ReleaseInfo>>()) ?? new List<ReleaseInfo>();
	}

	public async Task<OnlineCounts?> GetOnlineCountsAsync()
	{
		_ = 1;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/onlinecount");
			HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
			httpResponseMessage.EnsureSuccessStatusCode();
			return await httpResponseMessage.Content.ReadFromJsonAsync<OnlineCounts>();
		}
		catch
		{
			return null;
		}
	}

	public async Task<List<BuildInfo>> GetBuildsAsync()
	{
		if (Builds.Count > 0)
		{
			return Builds;
		}
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/builds");
		HttpRequestHeaders headers = request.Headers;
		headers.Authorization = new BearerAuthenticationValue(await accountService.GetClientCredentialsAccessTokenAsync());
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
		httpResponseMessage.EnsureSuccessStatusCode();
		Builds = (await httpResponseMessage.Content.ReadFromJsonAsync<List<BuildInfo>>()) ?? new List<BuildInfo>();
		return Builds;
	}

	public async Task<string[]> GetDistributionPointsAsync()
	{
		if (DistributionPoints.Length != 0)
		{
			return DistributionPoints;
		}
		try
		{
			DistributionPointsResponse distributionPointsResponse = await _httpClient.GetFromJsonAsync<DistributionPointsResponse>("api/public/distributionpoints");
			if (distributionPointsResponse?.Distributions == null || distributionPointsResponse.Distributions.Length == 0)
			{
				return Array.Empty<string>();
			}
			DistributionPoints = distributionPointsResponse.Distributions.Select((string d) => d.TrimEnd('/')).ToArray();
		}
		catch
		{
			return Array.Empty<string>();
		}
		return DistributionPoints;
	}
}
