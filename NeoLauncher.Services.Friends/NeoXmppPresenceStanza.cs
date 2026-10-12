namespace NeoLauncher.Services.Friends;

internal sealed record NeoXmppPresenceStanza(string AccountId, string Resource, string Type, string StatusJson, int Priority, long ReceivedAt);
