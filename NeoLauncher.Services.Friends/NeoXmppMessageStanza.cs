namespace NeoLauncher.Services.Friends;

internal sealed record NeoXmppMessageStanza(string From, string Type, string Body, long ReceivedAt, string Id = "")
{
	public string FromJid => From;

	public string FromAccountId
	{
		get
		{
			if (string.IsNullOrWhiteSpace(From))
			{
				return string.Empty;
			}
			int num = From.IndexOf('/');
			string text = ((num >= 0) ? From.Substring(0, num) : From);
			int num2 = text.IndexOf('@');
			if (num2 < 0)
			{
				return text;
			}
			return text.Substring(0, num2);
		}
	}
}
