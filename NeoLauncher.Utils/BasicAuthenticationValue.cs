using System;
using System.Net.Http.Headers;
using System.Text;

namespace NeoLauncher.Utils;

public class BasicAuthenticationValue : AuthenticationHeaderValue
{
	public BasicAuthenticationValue(string username, string password)
		: base("Basic", EncodeCredentials(username, password))
	{
	}

	private static string EncodeCredentials(string username, string password)
	{
		string s = username + ":" + password;
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
	}
}
