using System;
using System.Collections.Generic;
using System.Net.Http;

namespace NeoLauncher.Models.Services.Account.OAuth;

public class OAuthTokenRequest
{
	public required OAuthGrantType GrantType { get; set; }

	public string? ExchangeCode { get; set; }

	public string? AuthorizationCode { get; set; }

	public string? RefreshToken { get; set; }

	public string? Username { get; set; }

	public string? Password { get; set; }

	public static implicit operator FormUrlEncodedContent(OAuthTokenRequest request)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string> { ["grant_type"] = GetGrantTypeString(request.GrantType) };
		if (request.Username != null)
		{
			dictionary["username"] = request.Username;
		}
		if (request.Password != null)
		{
			dictionary["password"] = request.Password;
		}
		if (request.ExchangeCode != null)
		{
			dictionary["exchange_code"] = request.ExchangeCode;
		}
		if (request.AuthorizationCode != null)
		{
			dictionary["authorization_code"] = request.AuthorizationCode;
		}
		if (request.RefreshToken != null)
		{
			dictionary["refresh_token"] = request.RefreshToken;
		}
		return new FormUrlEncodedContent(dictionary);
	}

	private static string GetGrantTypeString(OAuthGrantType type)
	{
		return type switch
		{
			OAuthGrantType.Password => "password", 
			OAuthGrantType.ExchangeCode => "exchange_code", 
			OAuthGrantType.DeviceCode => "device_code", 
			OAuthGrantType.AuthorizationCode => "authorization_code", 
			OAuthGrantType.RefreshToken => "refresh_token", 
			OAuthGrantType.ClientCredentials => "client_credentials", 
			_ => throw new ArgumentOutOfRangeException("type", "Unknown grant type"), 
		};
	}
}
