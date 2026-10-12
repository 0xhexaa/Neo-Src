using System;
using System.Text.Json.Serialization;

namespace NeoLauncher.Models.Services.Account.OAuth;

public class OAuthToken
{
	public string Value { get; }

	public DateTime ExpiresAt { get; }

	[JsonIgnore]
	public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

	public OAuthToken(string value, DateTime expiresAt)
	{
		Value = value ?? throw new ArgumentNullException("value");
		ExpiresAt = expiresAt;
	}
}
