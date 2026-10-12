using System.Net.Http.Headers;

namespace NeoLauncher.Utils;

public class BearerAuthenticationValue : AuthenticationHeaderValue
{
	public BearerAuthenticationValue(string token)
		: base("Bearer", token)
	{
	}
}
