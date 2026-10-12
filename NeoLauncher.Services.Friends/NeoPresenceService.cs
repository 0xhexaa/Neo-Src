using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NeoLauncher.Services.WebHost;

namespace NeoLauncher.Services.Friends;

public sealed class NeoPresenceService : IAsyncDisposable
{
	private sealed record LauncherPresenceState(string Presence, string Activity, string GameStatus, string LauncherActivity)
	{
		public static LauncherPresenceState InLauncher()
		{
			return new LauncherPresenceState("online", "In Launcher", "In Launcher", "launcher");
		}
	}

	private sealed class EpicPresenceStatus
	{
		public string? Status { get; set; }

		[JsonPropertyName("bIsPlaying")]
		public bool BIsPlaying { get; set; }

		[JsonPropertyName("bIsJoinable")]
		public bool BIsJoinable { get; set; }

		[JsonPropertyName("bHasVoiceSupport")]
		public bool BHasVoiceSupport { get; set; }

		public string? SessionId { get; set; }

		public Dictionary<string, object?>? Properties { get; set; }
	}

	private static readonly JsonSerializerOptions PresenceJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);

	private readonly ConcurrentDictionary<string, NeoPresenceView> _presenceByResource = new ConcurrentDictionary<string, NeoPresenceView>(StringComparer.OrdinalIgnoreCase);

	private readonly ConcurrentQueue<string> _attemptLog = new ConcurrentQueue<string>();

	private NeoXmppClient? _client;

	private string _connectedAccountId = string.Empty;

	private string _lastAccessToken = string.Empty;

	private string _lastProfileStatusId = "online";

	private LauncherPresenceState _lastPresence = LauncherPresenceState.InLauncher();

	public bool IsConnected => _client?.IsConnected ?? false;

	public IReadOnlyCollection<NeoPresenceView> GetSnapshot()
	{
		return (from g in _presenceByResource.Values.GroupBy((NeoPresenceView p) => p.AccountId, StringComparer.OrdinalIgnoreCase)
			select (from p in g
				orderby p.Priority descending, p.UpdatedAt descending
				select p).First() into p
			orderby p.Priority descending, p.UpdatedAt descending
			select p).ToArray();
	}

	public async Task<NeoPresenceView?> PublishLauncherPresenceAsync(string accountId, string accessToken, string profileStatusId, string presence, string activity, string gameStatus, string launcherActivity)
	{
		accountId = (accountId ?? string.Empty).Trim();
		accessToken = (accessToken ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accessToken))
		{
			return null;
		}
		_lastAccessToken = accessToken;
		_lastProfileStatusId = (string.IsNullOrWhiteSpace(profileStatusId) ? "online" : profileStatusId.Trim().ToLowerInvariant());
		_lastPresence = new LauncherPresenceState(NormalizePresence(presence, _lastProfileStatusId), string.IsNullOrWhiteSpace(activity) ? "In Launcher" : activity, string.IsNullOrWhiteSpace(gameStatus) ? "In Launcher" : gameStatus, string.IsNullOrWhiteSpace(launcherActivity) ? "launcher" : launcherActivity.Trim().ToLowerInvariant());
		await EnsureConnectedAsync(accountId, accessToken);
		if (_client != null && _client.IsConnected)
		{
			await SendPresenceToXmppAsync(_lastPresence, _lastProfileStatusId);
		}
		NeoPresenceView neoPresenceView = CreateLocalPresence(accountId, _lastPresence, _lastProfileStatusId);
		UpsertPresence(neoPresenceView);
		return neoPresenceView;
	}

	public Task<NeoPresenceView?> PublishLifecyclePresenceAsync(string accountId, string accessToken, string launcherActivity)
	{
		launcherActivity = (string.IsNullOrWhiteSpace(launcherActivity) ? "launcher" : launcherActivity.Trim().ToLowerInvariant());
		string text;
		if (launcherActivity == "launching")
		{
			text = "Starting Fortnite";
		}
		else
		{
			text = ((!(launcherActivity == "game")) ? "In Launcher" : "In Game");
		}
		string text2 = text;
		return PublishLauncherPresenceAsync(accountId, accessToken, _lastProfileStatusId, (_lastProfileStatusId == "invisible") ? "offline" : "online", text2, text2, launcherActivity);
	}

	public async Task<bool> EnsureConnectedAsync(string accountId, string accessToken)
	{
		accountId = (accountId ?? string.Empty).Trim();
		accessToken = (accessToken ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(accessToken))
		{
			return false;
		}
		_lastAccessToken = accessToken;
		if (_client != null && _client.IsConnected && string.Equals(_connectedAccountId, accountId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		await _connectLock.WaitAsync();
		try
		{
			if (_client != null && _client.IsConnected && string.Equals(_connectedAccountId, accountId, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			await DisposeClientAsync();
			NeoXmppClient candidate = new NeoXmppClient(new Uri("wss://xmpp-service-prod.neofn.dev"), "xmpp-service-prod.neofn.dev", accountId);
			candidate.PresenceReceived += OnPresenceReceivedAsync;
			candidate.MessageReceived += OnMessageReceivedAsync;
			candidate.Disconnected += OnDisconnected;
			try
			{
				using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(22L));
				await candidate.ConnectAsync(accessToken, timeout.Token);
				_client = candidate;
				_connectedAccountId = accountId;
				await SendPresenceToXmppAsync(_lastPresence, _lastProfileStatusId);
				return true;
			}
			catch (Exception ex)
			{
				_ = ex;
				candidate.PresenceReceived -= OnPresenceReceivedAsync;
				candidate.MessageReceived -= OnMessageReceivedAsync;
				candidate.Disconnected -= OnDisconnected;
				await candidate.DisposeAsync();
				return false;
			}
		}
		finally
		{
			_connectLock.Release();
		}
	}

	public async Task DisconnectAsync()
	{
		await DisposeClientAsync();
		_presenceByResource.Clear();
		_connectedAccountId = string.Empty;
		NeoWebBridge.Broadcast("neo-friend-presence-changed", new
		{
			presences = Array.Empty<object>()
		});
	}

	private async Task SendPresenceToXmppAsync(LauncherPresenceState state, string profileStatusId)
	{
		if (_client != null && _client.IsConnected)
		{
			if (state.Presence.Equals("offline", StringComparison.OrdinalIgnoreCase) || profileStatusId == "invisible")
			{
				await _client.SendUnavailablePresenceAsync();
				return;
			}
			string status;
			if (state.LauncherActivity.Equals("launching", StringComparison.OrdinalIgnoreCase))
			{
				status = "Starting Fortnite";
			}
			else
			{
				status = (string.IsNullOrWhiteSpace(state.Activity) ? "In Launcher" : state.Activity);
			}
			EpicPresenceStatus value = new EpicPresenceStatus
			{
				Status = status,
				BIsPlaying = state.LauncherActivity.Equals("game", StringComparison.OrdinalIgnoreCase),
				BIsJoinable = false,
				BHasVoiceSupport = false,
				SessionId = string.Empty,
				Properties = new Dictionary<string, object>
				{
					["NeoLauncherStatus_s"] = state.LauncherActivity,
					["NeoProfileStatus_s"] = profileStatusId
				}
			};
			int priority = (state.LauncherActivity.Equals("launching", StringComparison.OrdinalIgnoreCase) ? 45 : 30);
			await _client.SendAvailablePresenceAsync(JsonSerializer.Serialize(value, PresenceJsonOptions), priority);
		}
	}

	private Task OnPresenceReceivedAsync(NeoXmppPresenceStanza stanza)
	{
		if (string.IsNullOrWhiteSpace(stanza.AccountId))
		{
			return Task.CompletedTask;
		}
		NeoPresenceView presence = ConvertPresence(stanza);
		UpsertPresence(presence);
		return Task.CompletedTask;
	}

	private Task OnMessageReceivedAsync(NeoXmppMessageStanza stanza)
	{
		if (string.Equals(stanza.Type, "chat", StringComparison.OrdinalIgnoreCase))
		{
			NeoWebBridge.IngestIncomingChatMessage(stanza.FromAccountId, stanza.Body, stanza.ReceivedAt, stanza.Id);
			return Task.CompletedTask;
		}
		NeoWebBridge.BroadcastFriendsChanged(new
		{
			reason = "xmpp-message",
			from = stanza.FromAccountId,
			body = stanza.Body
		});
		return Task.CompletedTask;
	}

	public async Task<bool> SendChatMessageAsync(string toAccountId, string body, string messageId, CancellationToken ct = default(CancellationToken))
	{
		if (_client == null || !_client.IsConnected)
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(toAccountId) || string.IsNullOrWhiteSpace(body))
		{
			return false;
		}
		try
		{
			await _client.SendChatMessageAsync(toAccountId, body, messageId, ct);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void UpsertPresence(NeoPresenceView presence)
	{
		string key = presence.AccountId + "/" + presence.Resource;
		_presenceByResource[key] = presence;
		NeoWebBridge.UpsertNativeFriendPresence(presence);
		NeoWebBridge.Broadcast("neo-friend-presence-changed", new
		{
			accountId = presence.AccountId,
			presence = presence,
			presences = GetSnapshot()
		});
	}

	private static NeoPresenceView ConvertPresence(NeoXmppPresenceStanza stanza)
	{
		string resource = (string.IsNullOrWhiteSpace(stanza.Resource) ? "unknown" : stanza.Resource);
		if (stanza.Type.Equals("unavailable", StringComparison.OrdinalIgnoreCase))
		{
			return new NeoPresenceView(stanza.AccountId, "offline", "Offline", "Offline", resource, "offline", 0, stanza.ReceivedAt);
		}
		EpicPresenceStatus epicPresenceStatus = ParseStatus(stanza.StatusJson);
		string text = epicPresenceStatus?.Status;
		string resourceType = GetResourceType(resource, epicPresenceStatus);
		int num = stanza.Priority;
		if (num <= 0)
		{
			num = resourceType switch
			{
				"game" => 60, 
				"launching" => 45, 
				"launcher" => 30, 
				_ => 20, 
			};
		}
		string text2;
		if (string.IsNullOrWhiteSpace(text))
		{
			text2 = ((resourceType == "game") ? "In Game" : "In Launcher");
		}
		else
		{
			text2 = text;
		}
		return new NeoPresenceView(stanza.AccountId, "online", text2, text2, resource, resourceType, num, stanza.ReceivedAt);
	}

	private static string GetResourceType(string resource, EpicPresenceStatus? status)
	{
		string text = status?.Status ?? string.Empty;
		bool flag = status != null && status.Properties?.Keys.Any((string key) => key.Contains("Playlist", StringComparison.OrdinalIgnoreCase) || key.Contains("PlayersAlive", StringComparison.OrdinalIgnoreCase) || key.Contains("PartySize", StringComparison.OrdinalIgnoreCase) || key.Contains("SubGame", StringComparison.OrdinalIgnoreCase)) == true;
		if ((resource.Contains("fortnite", StringComparison.OrdinalIgnoreCase) || resource.StartsWith("V2:", StringComparison.OrdinalIgnoreCase) || (status?.BIsPlaying ?? false)) | flag)
		{
			return "game";
		}
		if (text.Contains("starting", StringComparison.OrdinalIgnoreCase) || text.Contains("launching", StringComparison.OrdinalIgnoreCase))
		{
			return "launching";
		}
		return "launcher";
	}

	private static EpicPresenceStatus? ParseStatus(string statusJson)
	{
		if (string.IsNullOrWhiteSpace(statusJson))
		{
			return null;
		}
		try
		{
			return JsonSerializer.Deserialize<EpicPresenceStatus>(statusJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)
			{
				PropertyNameCaseInsensitive = true
			});
		}
		catch
		{
			return new EpicPresenceStatus
			{
				Status = statusJson
			};
		}
	}

	private static NeoPresenceView CreateLocalPresence(string accountId, LauncherPresenceState state, string profileStatusId)
	{
		bool flag = !state.Presence.Equals("offline", StringComparison.OrdinalIgnoreCase) && profileStatusId != "invisible";
		string text;
		if (flag)
		{
			text = (state.LauncherActivity.Equals("launching", StringComparison.OrdinalIgnoreCase) ? "launching" : "launcher");
		}
		else
		{
			text = "offline";
		}
		int num;
		if (text == "launching")
		{
			num = 45;
		}
		else
		{
			num = ((!(text == "launcher")) ? (flag ? 20 : 0) : 30);
		}
		int priority = num;
		string text2 = accountId;
		string text3 = (flag ? "online" : "offline");
		string text4;
		if (flag)
		{
			text4 = (string.IsNullOrWhiteSpace(state.Activity) ? "In Launcher" : state.Activity);
		}
		else
		{
			text4 = "Offline";
		}
		string accountId2 = text2;
		string status = text3;
		string activity = text4;
		string gameStatus;
		if (flag)
		{
			gameStatus = (string.IsNullOrWhiteSpace(state.GameStatus) ? "In Launcher" : state.GameStatus);
		}
		else
		{
			gameStatus = "Offline";
		}
		return new NeoPresenceView(accountId2, status, activity, gameStatus, "launcher", text, priority, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
	}

	private void OnDisconnected(object? sender, EventArgs e)
	{
		Task.Run(async () =>
		{
			await DisposeClientAsync();
			if (!string.IsNullOrWhiteSpace(_connectedAccountId) && !string.IsNullOrWhiteSpace(_lastAccessToken))
			{
				int delayMs = 1000;
				for (int attempt = 0; attempt < 8; attempt++)
				{
					try
					{
						await Task.Delay(delayMs);
						if (await EnsureConnectedAsync(_connectedAccountId, _lastAccessToken))
						{
							break;
						}
					}
					catch
					{
					}
					delayMs = Math.Min(delayMs * 2, 30000);
				}
			}
		});
	}

	private async Task DisposeClientAsync()
	{
		NeoXmppClient client = _client;
		_client = null;
		if (client == null)
		{
			return;
		}
		client.PresenceReceived -= OnPresenceReceivedAsync;
		client.MessageReceived -= OnMessageReceivedAsync;
		client.Disconnected -= OnDisconnected;
		try
		{
			await client.DisposeAsync();
		}
		catch
		{
		}
	}

	private static string NormalizePresence(string presence, string profileStatusId)
	{
		if (profileStatusId.Equals("invisible", StringComparison.OrdinalIgnoreCase))
		{
			return "offline";
		}
		if (!string.IsNullOrWhiteSpace(presence))
		{
			return presence.Trim().ToLowerInvariant();
		}
		return "online";
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeClientAsync();
		_connectLock.Dispose();
	}
}
