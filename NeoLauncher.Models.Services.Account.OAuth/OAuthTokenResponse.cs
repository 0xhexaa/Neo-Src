using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Account.OAuth;

public sealed class OAuthTokenResponse : BaseResponse
{
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; init; }

	[JsonPropertyName("refresh_token")]
	public string? RefreshToken { get; init; }

	[JsonPropertyName("expires_in")]
	public int ExpiresIn { get; init; }

	[JsonPropertyName("refresh_expires")]
	public int? RefreshExpiresIn { get; init; }

	[JsonPropertyName("expires_at")]
	public DateTime ExpiresAt { get; init; }

	[JsonPropertyName("refresh_expires_at")]
	public DateTime? RefreshExpiresAt { get; init; }

	[JsonPropertyName("token_type")]
	public required string TokenType { get; init; }

	[JsonPropertyName("client_id")]
	public required string ClientId { get; init; }

	[JsonPropertyName("client_service")]
	public required string ClientService { get; init; }

	[JsonPropertyName("internal_client")]
	public bool InternalClient { get; init; }

	[JsonPropertyName("account_id")]
	public string? AccountId { get; init; }

	[JsonPropertyName("in_app_id")]
	public string? InAppId { get; init; }

	[JsonPropertyName("scope")]
	public IReadOnlyList<string>? Scope { get; init; }

	[JsonPropertyName("displayName")]
	public string? DisplayName { get; init; }

	[JsonPropertyName("app")]
	public string? App { get; init; }
}
