using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NeoLauncher.Utils;

namespace NeoLauncher.Services.Fortnite;

public class LoadoutService
{
	public const string DefaultCharacterId = "cid_001_athena_commando_f_default";

	private const int MaxBulkAccounts = 100;

	private static readonly string BaseUri = "https://fortnite-public-service-prod11.neofn.dev/fortnite";

	private readonly HttpClient _httpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(6L)
	};

	public static string AvatarUrlForCharacter(string? characterId)
	{
		string stringToEscape = (string.IsNullOrWhiteSpace(characterId) ? "cid_001_athena_commando_f_default" : characterId.Trim());
		return "https://fortnite-api.com/images/cosmetics/br/" + Uri.EscapeDataString(stringToEscape) + "/smallicon.png";
	}

	public async Task<string?> GetEquippedCharacterIdAsync(string accountId, string accessToken, CancellationToken ct = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accessToken))
		{
			return null;
		}
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUri + "/api/game/v2/loadout/" + Uri.EscapeDataString(accountId));
			request.Headers.Authorization = new BearerAuthenticationValue(accessToken);
			using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			using Stream stream = await response.Content.ReadAsStreamAsync(ct);
			using JsonDocument jsonDocument = await JsonDocument.ParseAsync(stream, default, ct);
			if (!jsonDocument.RootElement.TryGetProperty("character", out var value) || value.ValueKind != JsonValueKind.Object)
			{
				return null;
			}
			if (!value.TryGetProperty("templateId", out var value2) || value2.ValueKind != JsonValueKind.String)
			{
				return null;
			}
			return ExtractCharacterId(value2.GetString());
		}
		catch
		{
			return null;
		}
	}

	public async Task<string> GetAvatarUrlAsync(string accountId, string accessToken, CancellationToken ct = default(CancellationToken))
	{
		return AvatarUrlForCharacter(await GetEquippedCharacterIdAsync(accountId, accessToken, ct));
	}

	public async Task<Dictionary<string, string>> GetAvatarUrlsAsync(IEnumerable<string> accountIds, string accessToken, CancellationToken ct = default(CancellationToken))
	{
		Dictionary<string, string> avatars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (string.IsNullOrWhiteSpace(accessToken))
		{
			return avatars;
		}
		string[] ids = accountIds?.Where((string id) => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>();
		for (int offset = 0; offset < ids.Length; offset += 100)
		{
			string[] array = ids.Skip(offset).Take(100).ToArray();
			if (array.Length == 0)
			{
				break;
			}
			try
			{
				string text = string.Join("&", array.Select((string id) => "accountId=" + Uri.EscapeDataString(id)));
				using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, BaseUri + "/api/game/v2/loadout?" + text);
				request.Headers.Authorization = new BearerAuthenticationValue(accessToken);
				using (HttpResponseMessage response = await _httpClient.SendAsync(request, ct))
				{
					if (!response.IsSuccessStatusCode)
					{
						continue;
					}
					using Stream stream = await response.Content.ReadAsStreamAsync(ct);
					using JsonDocument jsonDocument = await JsonDocument.ParseAsync(stream, default, ct);
					if (jsonDocument.RootElement.ValueKind != JsonValueKind.Array)
					{
						goto end_IL_02ca;
					}
					foreach (JsonElement item in jsonDocument.RootElement.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("accountId", out var value) && value.ValueKind == JsonValueKind.String && item.TryGetProperty("character", out var value2) && value2.ValueKind == JsonValueKind.Object && value2.TryGetProperty("templateId", out var value3) && value3.ValueKind == JsonValueKind.String)
						{
							string text2 = ExtractCharacterId(value3.GetString());
							if (text2 != null)
							{
								avatars[value.GetString()] = AvatarUrlForCharacter(text2);
							}
						}
					}
					goto end_IL_0122;
					end_IL_02ca:;
				}
				end_IL_0122:;
			}
			catch
			{
			}
		}
		return avatars;
	}

	internal static string? ExtractCharacterId(string? templateId)
	{
		if (string.IsNullOrWhiteSpace(templateId))
		{
			return null;
		}
		int num = templateId.IndexOf(':');
		string text;
		if (num < 0)
		{
			text = templateId;
		}
		else
		{
			int num2 = num + 1;
			text = templateId.Substring(num2, templateId.Length - num2);
		}
		string text2 = text;
		text2 = text2.Trim();
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return null;
	}
}
