using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NeoLauncher.Models.Services.Prism;
using NeoLauncher.Services.Account;
using NeoLauncher.Utils;

namespace NeoLauncher.Services.Prism;

public class PrismService
{
	private const string BaseUri = "https://prism-public-service-prod.neofn.dev/prism";

	private readonly AccountService _accountService;

	private readonly HttpClient _httpClient = new HttpClient
	{
		BaseAddress = new Uri("https://prism-public-service-prod.neofn.dev/prism".TrimEnd('/') + "/"),
		Timeout = TimeSpan.FromSeconds(300L)
	};

	public PrismService(AccountService accountService)
	{
		_accountService = accountService;
	}

	public async Task<AssetInfo[]> GetAssetsAsync()
	{
		_ = 2;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/assets");
			HttpRequestHeaders headers = request.Headers;
			headers.Authorization = new BearerAuthenticationValue(await _accountService.GetAccessTokenAsync());
			HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
			httpResponseMessage.EnsureSuccessStatusCode();
			return (await httpResponseMessage.Content.ReadFromJsonAsync<AssetInfo[]>()) ?? Array.Empty<AssetInfo>();
		}
		catch
		{
			return Array.Empty<AssetInfo>();
		}
	}

	public async Task<BanStatus?> GetBanStatusAsync()
	{
		_ = 2;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/v1/ban-status");
			HttpRequestHeaders headers = request.Headers;
			headers.Authorization = new BearerAuthenticationValue(await _accountService.GetAccessTokenAsync());
			HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);
			httpResponseMessage.EnsureSuccessStatusCode();
			return await httpResponseMessage.Content.ReadFromJsonAsync<BanStatus>();
		}
		catch
		{
			return null;
		}
	}

	public async Task DownloadAssetsAsync()
	{
		AssetInfo[] array = await GetAssetsAsync();
		if (array.Length == 0)
		{
			return;
		}
		Directory.CreateDirectory(Config.DataPath);
		AssetInfo[] array2 = array;
		foreach (AssetInfo asset in array2)
		{
			try
			{
				string path = Path.Combine(Config.DataPath, asset.Filename);
				bool flag = File.Exists(path);
				if (flag)
				{
					flag = await ComputeSha256Async(path) == asset.Hash256;
				}
				if (flag)
				{
					continue;
				}
				using HttpResponseMessage response = await _httpClient.GetAsync(asset.Url);
				response.EnsureSuccessStatusCode();
				await using FileStream fileStream = File.Create(path);
				await response.Content.CopyToAsync(fileStream);
			}
			catch
			{
			}
		}
	}

	private static async Task<string> ComputeSha256Async(string path)
	{
		string result;
		await using (FileStream stream = File.OpenRead(path))
		{
			result = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream));
		}
		return result;
	}
}
