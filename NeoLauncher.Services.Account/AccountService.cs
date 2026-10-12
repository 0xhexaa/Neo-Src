using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using NeoLauncher.Localization;
using NeoLauncher.Models.Exceptions;
using NeoLauncher.Models.Services.Account;
using NeoLauncher.Models.Services.Account.OAuth;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Utils;
using Windows.Security.Credentials;
using Windows.System;

namespace NeoLauncher.Services.Account;

public class AccountService
{
	public class LoginResult
	{
		public bool IsSuccess { get; }

		public string Error { get; init; } = Strings.AccountServiceUnreachable;

		public LoginFailureReason Reason { get; init; }

		public bool IsUnreachable => Reason == LoginFailureReason.ServiceUnreachable;

		private LoginResult(bool success)
		{
			IsSuccess = success;
		}

		public static LoginResult Success()
		{
			return new LoginResult(success: true);
		}

		public static LoginResult Fail(string? error = null, LoginFailureReason reason = LoginFailureReason.Unknown)
		{
			return new LoginResult(success: false)
			{
				Error = (error ?? Strings.AccountServiceUnreachable),
				Reason = reason
			};
		}
	}

	public enum SetupFailure
	{
		Unknown,
		DisplayNameTaken
	}

	public class SetupResult
	{
		public bool IsSuccess { get; }

		public SetupFailure Failure { get; init; }

		private SetupResult(bool success)
		{
			IsSuccess = success;
		}

		public static SetupResult Success()
		{
			return new SetupResult(success: true);
		}

		public static SetupResult Fail(SetupFailure failure)
		{
			return new SetupResult(success: false)
			{
				Failure = failure
			};
		}
	}

	private class SetupStatusResponse
	{
		[JsonPropertyName("setupCompleted")]
		public bool SetupCompleted { get; set; }
	}

	private class DisplayNameAvailabilityResponse
	{
		[JsonPropertyName("available")]
		public bool Available { get; set; }
	}

	private class SetupRequest
	{
		[JsonPropertyName("displayName")]
		public required string DisplayName { get; set; }
	}

	public class ExchangeCode
	{
		public required string Code { get; init; }

		public required DateTime ExpiresAt { get; init; }
	}

	public record UserRecord
	{
		public required string Id
		{
			[CompilerGenerated]
			get
			{
				return Id__BackingField;
			}
			[CompilerGenerated]
			init
			{
				Id__BackingField = value;
			}
		}

		public required string DisplayName
		{
			[CompilerGenerated]
			get
			{
				return DisplayName__BackingField;
			}
			[CompilerGenerated]
			init
			{
				DisplayName__BackingField = value;
			}
		}

		public required string Email
		{
			[CompilerGenerated]
			get
			{
				return Email__BackingField;
			}
			[CompilerGenerated]
			init
			{
				Email__BackingField = value;
			}
		}

		public required string AvatarUrl
		{
			[CompilerGenerated]
			get
			{
				return AvatarUrl__BackingField;
			}
			[CompilerGenerated]
			init
			{
				AvatarUrl__BackingField = value;
			}
		}

		public ImageSource AvatarImage => new BitmapImage(new Uri(AvatarUrl));

		public required OAuthToken AccessToken
		{
			[CompilerGenerated]
			get
			{
				return AccessToken__BackingField;
			}
			[CompilerGenerated]
			init
			{
				AccessToken__BackingField = value;
			}
		}

		public required OAuthToken RefreshToken
		{
			[CompilerGenerated]
			get
			{
				return RefreshToken__BackingField;
			}
			[CompilerGenerated]
			init
			{
				RefreshToken__BackingField = value;
			}
		}

		[CompilerGenerated]
		[SetsRequiredMembers]
		protected UserRecord(UserRecord original)
		{
			Id__BackingField = original.Id__BackingField;
			DisplayName__BackingField = original.DisplayName__BackingField;
			Email__BackingField = original.Email__BackingField;
			AvatarUrl__BackingField = original.AvatarUrl__BackingField;
			AccessToken__BackingField = original.AccessToken__BackingField;
			RefreshToken__BackingField = original.RefreshToken__BackingField;
		}
	}

	private const string BaseUri = "https://account-public-service-prod.neofn.dev/account";

	private const string ClientId = "8a4eeb89e05743fc9dba6fccb6766d35";

	private const string Secret = "7fe1392842624667b996d55ab5ebef03";

	private const string RedirectUri = "neolauncher://callback/auth";

	private const string VaultResource = "NeoLauncher";

	private readonly HttpClient _httpClient;

	private readonly JsonSerializerOptions _options;

	private readonly PasswordVault _vault;

	private Timer? _refreshTimer;

	private OAuthToken? _clientCredentialsToken;

	public UserRecord? CurrentUser { get; private set; }

	public bool HasStoredSession
	{
		get
		{
			try
			{
				return _vault.FindAllByResource("NeoLauncher").Count > 0;
			}
			catch (COMException)
			{
				return false;
			}
		}
	}

	public AccountService()
	{
		_httpClient = new HttpClient
		{
			BaseAddress = new Uri("https://account-public-service-prod.neofn.dev/account".TrimEnd('/') + "/"),
			Timeout = TimeSpan.FromSeconds(15L)
		};
		_options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
		};
		_vault = new PasswordVault();
	}

	private void SaveRefreshToken(string accountId, string refreshToken)
	{
		ClearRefreshToken(accountId);
		_vault.Add(new PasswordCredential("NeoLauncher", accountId, refreshToken));
	}

	private void ClearRefreshToken(string? accountId = null)
	{
		try
		{
			IReadOnlyList<PasswordCredential> readOnlyList;
			if (accountId == null)
			{
				readOnlyList = _vault.FindAllByResource("NeoLauncher");
			}
			else
			{
				IReadOnlyList<PasswordCredential> readOnlyList2 = new _003C_003Ez__ReadOnlySingleElementList<PasswordCredential>(_vault.Retrieve("NeoLauncher", accountId));
				readOnlyList = readOnlyList2;
			}
			foreach (PasswordCredential item in readOnlyList)
			{
				_vault.Remove(item);
			}
		}
		catch (COMException)
		{
		}
	}

	private async Task<OAuthTokenResponse> GetOAuthTokenAsync(OAuthTokenRequest content, CancellationToken ct = default(CancellationToken))
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/oauth/token");
		request.Headers.Authorization = new BasicAuthenticationValue("8a4eeb89e05743fc9dba6fccb6766d35", "7fe1392842624667b996d55ab5ebef03");
		request.Content = (FormUrlEncodedContent)content;
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			int statusCode = (int)httpResponseMessage.StatusCode;
			LoginFailureReason reason = ((statusCode >= 400 && statusCode < 500) ? LoginFailureReason.InvalidCredentials : LoginFailureReason.ServiceUnreachable);
			throw (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest) ? new LoginException(Strings.CredentialsLoginError, reason) : new LoginException(string.Format(Strings.OAuthRequestFailed, statusCode), reason);
		}
		return (await httpResponseMessage.Content.ReadFromJsonAsync<OAuthTokenResponse>(_options, ct)) ?? throw new LoginException(Strings.OAuthResponseParsingFailed, LoginFailureReason.ServiceUnreachable);
	}

	private async Task KillOtherClientSessionsAsync(string accessToken, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "api/oauth/sessions/kill?killType=OTHERS_ACCOUNT_CLIENT");
			request.Headers.Authorization = new BearerAuthenticationValue(accessToken);
			using (await _httpClient.SendAsync(request, ct))
			{
			}
		}
		catch
		{
		}
	}

	private async Task<PrivateAccount> GetAccountAsync(string accountId, string oauthToken, CancellationToken ct = default(CancellationToken))
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/account/" + accountId);
		request.Headers.Authorization = new BearerAuthenticationValue(oauthToken);
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new AccountServiceException(string.Format(Strings.OAuthRequestFailed, (int)httpResponseMessage.StatusCode));
		}
		return (await httpResponseMessage.Content.ReadFromJsonAsync<PrivateAccount>(_options, ct)) ?? throw new AccountServiceException(Strings.OAuthResponseParsingFailed);
	}

	public async Task<bool> GetSetupCompletedAsync(CancellationToken ct = default(CancellationToken))
	{
		string token = await GetAccessTokenAsync(ct);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/account/setup/status");
		request.Headers.Authorization = new BearerAuthenticationValue(token);
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new AccountServiceException(string.Format(Strings.OAuthRequestFailed, (int)httpResponseMessage.StatusCode));
		}
		return ((await httpResponseMessage.Content.ReadFromJsonAsync<SetupStatusResponse>(_options, ct)) ?? throw new AccountServiceException(Strings.OAuthResponseParsingFailed)).SetupCompleted;
	}

	public async Task<bool> IsDisplayNameAvailableAsync(string displayName, CancellationToken ct = default(CancellationToken))
	{
		string token = await GetAccessTokenAsync(ct);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/account/displayName/" + Uri.EscapeDataString(displayName) + "/available");
		request.Headers.Authorization = new BearerAuthenticationValue(token);
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new AccountServiceException(string.Format(Strings.OAuthRequestFailed, (int)httpResponseMessage.StatusCode));
		}
		return ((await httpResponseMessage.Content.ReadFromJsonAsync<DisplayNameAvailabilityResponse>(_options, ct)) ?? throw new AccountServiceException(Strings.OAuthResponseParsingFailed)).Available;
	}

	public async Task<PublicAccount?> GetAccountByDisplayNameAsync(string displayName, CancellationToken ct = default(CancellationToken))
	{
		string token = await GetAccessTokenAsync(ct);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/public/account/displayName/" + Uri.EscapeDataString(displayName));
		request.Headers.Authorization = new BearerAuthenticationValue(token);
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
		{
			return null;
		}
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new AccountServiceException(string.Format(Strings.OAuthRequestFailed, (int)httpResponseMessage.StatusCode));
		}
		return await httpResponseMessage.Content.ReadFromJsonAsync<PublicAccount>(_options, ct);
	}

	public async Task<SetupResult> CompleteSetupAsync(string displayName, CancellationToken ct = default(CancellationToken))
	{
		string token = await GetAccessTokenAsync(ct);
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "api/public/account/setup");
		request.Headers.Authorization = new BearerAuthenticationValue(token);
		request.Content = JsonContent.Create(new SetupRequest
		{
			DisplayName = displayName
		});
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (httpResponseMessage.IsSuccessStatusCode)
		{
			string confirmedName = null;
			try
			{
				confirmedName = (await httpResponseMessage.Content.ReadFromJsonAsync<PrivateAccount>(_options, ct))?.DisplayName;
			}
			catch (JsonException)
			{
			}
			catch (NotSupportedException)
			{
			}
			if ((object)CurrentUser != null)
			{
				CurrentUser = CurrentUser with
				{
					DisplayName = (string.IsNullOrWhiteSpace(confirmedName) ? displayName : confirmedName)
				};
			}
			return SetupResult.Success();
		}
		if (httpResponseMessage.StatusCode == HttpStatusCode.Conflict)
		{
			return SetupResult.Fail(SetupFailure.DisplayNameTaken);
		}
		return SetupResult.Fail(SetupFailure.Unknown);
	}

	private async Task<ExchangeCodeResponse> GetExchangeCodeAsync(string oauthToken, CancellationToken ct = default(CancellationToken))
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "api/oauth/exchange");
		request.Headers.Authorization = new BearerAuthenticationValue(oauthToken);
		HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			throw new AccountServiceException(string.Format(Strings.OAuthRequestFailed, (int)httpResponseMessage.StatusCode));
		}
		return (await httpResponseMessage.Content.ReadFromJsonAsync<ExchangeCodeResponse>(_options, ct)) ?? throw new AccountServiceException(Strings.OAuthResponseParsingFailed);
	}

	public async Task<ExchangeCode?> GetExchangeCodeAsync(CancellationToken ct = default(CancellationToken))
	{
		string text = await GetAccessTokenAsync(ct);
		if (text == null)
		{
			return null;
		}
		try
		{
			ExchangeCodeResponse exchangeCodeResponse = await GetExchangeCodeAsync(text, ct);
			return new ExchangeCode
			{
				Code = exchangeCodeResponse.Code,
				ExpiresAt = DateTime.UtcNow.AddSeconds(exchangeCodeResponse.ExpiresInSeconds)
			};
		}
		catch (AccountServiceException)
		{
			return null;
		}
	}

	public async Task<LoginResult> LoginAsync(OAuthTokenRequest content, CancellationToken ct = default(CancellationToken), bool killOtherSessions = true)
	{
		if (content.GrantType == OAuthGrantType.ClientCredentials)
		{
			return LoginResult.Fail();
		}
		try
		{
			OAuthTokenResponse response = await GetOAuthTokenAsync(content, ct);
			PrivateAccount account = await GetAccountAsync(response.AccountId, response.AccessToken, ct);
			if (killOtherSessions)
			{
				await KillOtherClientSessionsAsync(response.AccessToken, ct);
			}
			string avatarUrl = await App.LoadoutService.GetAvatarUrlAsync(response.AccountId, response.AccessToken, ct);
			CurrentUser = new UserRecord
			{
				Id = response.AccountId,
				DisplayName = response.DisplayName,
				Email = account.Email,
				AvatarUrl = avatarUrl,
				AccessToken = new OAuthToken(response.AccessToken, response.ExpiresAt),
				RefreshToken = new OAuthToken(response.RefreshToken, response.RefreshExpiresAt.Value)
			};
			SaveRefreshToken(CurrentUser.Id, response.RefreshToken);
			ScheduleRefreshTimer();
			App.AnalyticsService.Track(EventType.LauncherLogin);
			return LoginResult.Success();
		}
		catch (LoginException ex)
		{
			App.AnalyticsService.Track(EventType.LauncherLoginFailed);
			return LoginResult.Fail(ex.Message, ex.Reason);
		}
		catch (AccountServiceException ex2)
		{
			App.AnalyticsService.Track(EventType.LauncherLoginFailed);
			return LoginResult.Fail(ex2.Message, LoginFailureReason.ServiceUnreachable);
		}
		catch (HttpRequestException)
		{
			App.AnalyticsService.Track(EventType.LauncherLoginFailed);
			return LoginResult.Fail(Strings.AccountServiceUnreachable, LoginFailureReason.ServiceUnreachable);
		}
		catch (TaskCanceledException)
		{
			App.AnalyticsService.Track(EventType.LauncherLoginFailed);
			return LoginResult.Fail(Strings.AccountServiceUnreachable, LoginFailureReason.ServiceUnreachable);
		}
	}

	public async Task<LoginResult> TryRestoreSessionAsync(CancellationToken ct = default(CancellationToken))
	{
		PasswordCredential cred = null;
		try
		{
			IReadOnlyList<PasswordCredential> readOnlyList = _vault.FindAllByResource("NeoLauncher");
			cred = ((readOnlyList.Count > 0) ? readOnlyList[0] : null);
		}
		catch (COMException)
		{
		}
		if ((object)cred == null)
		{
			return LoginResult.Fail();
		}
		cred.RetrievePassword();
		LoginResult loginResult = await LoginAsync(new OAuthTokenRequest
		{
			GrantType = OAuthGrantType.RefreshToken,
			RefreshToken = cred.Password
		}, ct);
		if (!loginResult.IsSuccess && !loginResult.IsUnreachable)
		{
			ClearRefreshToken(cred.UserName);
		}
		return loginResult;
	}

	public async Task<string> GetAccessTokenAsync(CancellationToken ct = default(CancellationToken))
	{
		if (CurrentUser == null)
		{
			throw new AccountServiceException("Not logged in.");
		}
		if (CurrentUser.AccessToken.ExpiresAt - DateTime.UtcNow < TimeSpan.FromMinutes(5L))
		{
			await RefreshSessionAsync(ct);
		}
		return CurrentUser.AccessToken.Value;
	}

	public async Task<string> GetClientCredentialsAccessTokenAsync(CancellationToken ct = default(CancellationToken))
	{
		if (_clientCredentialsToken == null || _clientCredentialsToken.IsExpired)
		{
			OAuthTokenResponse oAuthTokenResponse = await GetOAuthTokenAsync(new OAuthTokenRequest
			{
				GrantType = OAuthGrantType.ClientCredentials
			}, ct);
			_clientCredentialsToken = new OAuthToken(oAuthTokenResponse.AccessToken, oAuthTokenResponse.ExpiresAt);
		}
		return _clientCredentialsToken.Value;
	}

	private async Task RefreshSessionAsync(CancellationToken ct = default(CancellationToken))
	{
		if (CurrentUser == null || (await LoginAsync(new OAuthTokenRequest
		{
			GrantType = OAuthGrantType.RefreshToken,
			RefreshToken = CurrentUser.RefreshToken.Value
		}, ct, killOtherSessions: false)).IsSuccess)
		{
			return;
		}
		throw new AccountServiceException("Session refresh failed.");
	}

	private void ScheduleRefreshTimer()
	{
		_refreshTimer?.Dispose();
		if (CurrentUser == null)
		{
			return;
		}
		TimeSpan timeSpan = CurrentUser.AccessToken.ExpiresAt - DateTime.UtcNow - TimeSpan.FromMinutes(5L);
		if (timeSpan < TimeSpan.Zero)
		{
			timeSpan = TimeSpan.Zero;
		}
		_refreshTimer = new Timer(async (object? _) =>
		{
			try
			{
				await RefreshSessionAsync();
			}
			catch
			{
			}
		}, null, timeSpan, Timeout.InfiniteTimeSpan);
	}

	public void Logout()
	{
		_refreshTimer?.Dispose();
		_refreshTimer = null;
		ClearRefreshToken(CurrentUser?.Id);
		CurrentUser = null;
		App.AnalyticsService.Track(EventType.LauncherLogout);
	}

	public async Task LaunchSsoAsync(string scheme, Action<string> onFailure, Action onSuccess)
	{
		await Windows.System.Launcher.LaunchUriAsync(new Uri($"{"https://account-public-service-prod.neofn.dev/account"}/api/oauth/challenge/{scheme}?clientId={"8a4eeb89e05743fc9dba6fccb6766d35"}&redirectUri={"neolauncher://callback/auth"}"));
		App.OnUriCallback = async (Uri uri) =>
		{
			App.OnUriCallback = null;
			string text = HttpUtility.ParseQueryString(uri.Query).Get("code");
			if (text == null)
			{
				onFailure(Strings.UnknownError);
			}
			else
			{
				LoginResult loginResult = await LoginAsync(new OAuthTokenRequest
				{
					GrantType = OAuthGrantType.AuthorizationCode,
					AuthorizationCode = text
				});
				if (!loginResult.IsSuccess)
				{
					onFailure(loginResult.Error);
				}
				else
				{
					onSuccess();
				}
			}
		};
	}
}
