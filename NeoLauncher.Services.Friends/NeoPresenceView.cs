namespace NeoLauncher.Services.Friends;

public sealed record NeoPresenceView(string AccountId, string Status, string Activity, string GameStatus, string Resource, string ResourceType, int Priority, long UpdatedAt);
