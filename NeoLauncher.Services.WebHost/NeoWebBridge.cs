using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BuildPatchServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using NeoLauncher.Models;
using NeoLauncher.Models.Exceptions;
using NeoLauncher.Models.Launcher;
using NeoLauncher.Models.Services.Account;
using NeoLauncher.Models.Services.Account.OAuth;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Models.Services.Prism;
using NeoLauncher.Services.Account;
using NeoLauncher.Services.Fortnite;
using NeoLauncher.Services.Friends;
using NeoLauncher.Services.Game;
using NeoLauncher.Utils;
using NeoLauncher.Views;
using Velopack;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace NeoLauncher.Services.WebHost;

public sealed class NeoWebBridge
{
	private sealed record FriendPresenceState(string AccountId, string Status, string Activity, string GameStatus, string Resource, string ResourceType, int Priority, long UpdatedAt);

	private sealed record CachedAccountProfile(JsonNode? Profile, long FetchedAt);

	private sealed record CachedFriendAvatar(string AvatarUrl, long FetchedAt);

	private sealed record CachedFriendsRoster(object Payload, long FetchedAt);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct BROWSEINFO
	{
		public nint hwndOwner;

		public nint pidlRoot;

		public nint pszDisplayName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string? lpszTitle;

		public uint ulFlags;

		public nint lpfn;

		public nint lParam;

		public int iImage;
	}

	[ComImport]
	[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
	private sealed class FileOpenDialog
	{
	}

	[ComImport]
	[Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IFileOpenDialog
	{
		[PreserveSig]
		int Show(nint parent);

		void SetFileTypes(uint cFileTypes, nint rgFilterSpec);

		void SetFileTypeIndex(uint iFileType);

		void GetFileTypeIndex(out uint piFileType);

		void Advise(nint pfde, out uint pdwCookie);

		void Unadvise(uint dwCookie);

		void SetOptions(uint fos);

		void GetOptions(out uint pfos);

		void SetDefaultFolder(nint psi);

		void SetFolder(nint psi);

		void GetFolder(out nint ppsi);

		void GetCurrentSelection(out nint ppsi);

		void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

		void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

		void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

		void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

		void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

		void GetResult(out IShellItem ppsi);

		void AddPlace(nint psi, int fdap);

		void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

		void Close(int hr);

		void SetClientGuid(ref Guid guid);

		void ClearClientData();

		void SetFilter(nint pFilter);

		void GetResults(out nint ppenum);

		void GetSelectedItems(out nint ppsai);
	}

	[ComImport]
	[Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellItem
	{
		void BindToHandler(nint pbc, ref Guid bhid, ref Guid riid, out nint ppv);

		void GetParent(out IShellItem ppsi);

		void GetDisplayName(uint sigdnName, out nint ppszName);

		void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

		void Compare(IShellItem psi, uint hint, out int piOrder);
	}

	private sealed record ChatMessage(string Id, string AccountId, string Direction, string Body, long SentAt);

	private sealed record ConversationView(string accountId, object[] messages, int unread, long lastAt);

	private sealed class AccountEntitlementsDto
	{
		public string[]? OwnedOfferIds { get; set; }

		public EntitlementOrderDto[]? Orders { get; set; }

		public EntitlementSubscriptionDto[]? Subscriptions { get; set; }
	}

	private sealed class EntitlementOrderDto
	{
		public string? OfferId { get; set; }

		public string? SubscriptionId { get; set; }

		public bool Paid { get; set; }

		public bool Refunded { get; set; }
	}

	private sealed class EntitlementSubscriptionDto
	{
		public string? OfferId { get; set; }

		public bool Active { get; set; }
	}

	internal static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

	private static readonly object BridgesLock = new object();

	private static readonly List<NeoWebBridge> LiveBridges = new List<NeoWebBridge>();

	private readonly CoreWebView2 _core;

	private readonly Window _window;

	public readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcher;

	private readonly Dictionary<string, Func<NeoBridgeContext, Task<object?>>> _handlers = new Dictionary<string, Func<NeoBridgeContext, Task<object>>>(StringComparer.Ordinal);

	private readonly List<Action> _cleanup = new List<Action>();

	private bool _disposed;

	private static readonly HttpClient ContentHttp = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10L)
	};

	private static readonly string ContentPagesUrl = "https://fortnitecontent-website-prod07.neofn.dev/content/api/pages/fortnite-game";

	private static readonly string LauncherNewsUrl = "https://fortnitecontent-website-prod07.neofn.dev/content/api/launcher/news";

	private const int LauncherNewsSchemaVersion = 1;

	private static readonly object NewsCacheLock = new object();

	private static JsonArray? CachedNews;

	private static DateTime CachedNewsUtc = DateTime.MinValue;

	private static readonly object LauncherNewsCacheLock = new object();

	private static JsonArray? CachedLauncherNews;

	private static DateTime CachedLauncherNewsUtc = DateTime.MinValue;

	private static readonly object BuildsCacheLock = new object();

	private static JsonArray? CachedBuilds;

	private static DateTime CachedBuildsUtc = DateTime.MinValue;

	private static Task? BuildsRefreshTask;

	private static readonly HttpClient FriendsHttp = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15L)
	};

	private static readonly ConcurrentDictionary<string, FriendPresenceState> FriendPresenceCache = new ConcurrentDictionary<string, FriendPresenceState>(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, CachedAccountProfile> AccountProfileCache = new ConcurrentDictionary<string, CachedAccountProfile>(StringComparer.OrdinalIgnoreCase);

	private static readonly long AccountProfileCacheTtlMs = (long)TimeSpan.FromSeconds(60L).TotalMilliseconds;

	private const int MaxBulkAccountLookup = 100;

	private static readonly ConcurrentDictionary<string, CachedFriendAvatar> FriendAvatarCache = new ConcurrentDictionary<string, CachedFriendAvatar>(StringComparer.OrdinalIgnoreCase);

	private static readonly long FriendAvatarCacheTtlMs = (long)TimeSpan.FromMinutes(5L).TotalMilliseconds;

	private static readonly ConcurrentDictionary<string, CachedFriendsRoster> FriendsRosterCache = new ConcurrentDictionary<string, CachedFriendsRoster>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Task<object?>> FriendsRosterInFlight = new Dictionary<string, Task<object>>(StringComparer.OrdinalIgnoreCase);

	private static readonly object FriendsRosterGate = new object();

	private static readonly long FriendsRosterCacheTtlMs = (long)TimeSpan.FromSeconds(30L).TotalMilliseconds;

	private static int _friendsRosterGeneration;

	private static readonly string[] FortniteProcessNames = new string[3] { "FortniteClient-Win64-Shipping", "FortniteClient-Win64-Shipping_BE", "FortniteLauncher" };

	private string _installVersionTag = "";

	private GameVersion? _installGameVersion;

	private static readonly ConcurrentDictionary<string, string> BuildSplashDataUriCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private const uint SplashMaxDimension = 1024u;

	private const float SplashJpegQuality = 0.85f;

	private static readonly HashSet<string> DeveloperAccountIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "82d3672cac2d4cd3816f69496ee5d2c8", "c583b7287c1740ffa4c710d341b759e5", "273a82046dbd4fdaa5fd59e751485a61" };

	private static readonly string[] PublicBuildVersions = new string[1] { "10.40" };

	private int _folderPickerOpen;

	private const int HRESULT_CANCELLED = -2147023673;

	private const uint FOS_NOCHANGEDIR = 8u;

	private const uint FOS_PICKFOLDERS = 32u;

	private const uint FOS_FORCEFILESYSTEM = 64u;

	private const uint FOS_PATHMUSTEXIST = 2048u;

	private const uint SIGDN_FILESYSPATH = 2147844096u;

	private const uint BIF_RETURNONLYFSDIRS = 1u;

	private const uint BIF_EDITBOX = 16u;

	private const uint BIF_VALIDATE = 32u;

	private const uint BIF_NEWDIALOGSTYLE = 64u;

	private static readonly ConcurrentDictionary<string, List<ChatMessage>> Conversations = new ConcurrentDictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase);

	private static readonly ConcurrentDictionary<string, int> ConversationUnread = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	private static readonly object ConversationsLock = new object();

	private const int MaxMessagesPerConversation = 200;

	private static string _pendingMessageThreadAccountId = "";

	private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string RunValueName = "NeoLauncher";

	private const string CrewOfferId = "crew";

	private const string CrewPlusOfferId = "crew-plus";

	private const string FoundersOfferId = "founders";

	private static readonly HttpClient StoreHttp = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10L)
	};

	private static readonly JsonSerializerOptions EntitlementsJsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly string StoragePath = Path.Combine(Config.DataPath, "LauncherStorage.json");

	private static readonly object StorageLock = new object();

	private static JsonObject? _storage;

	private static readonly object UpdateStateLock = new object();

	private static UpdateInfo? _pendingLauncherUpdate;

	private static string _launcherUpdateStatus = "idle";

	private static int _launcherUpdateProgress;

	private static string? _launcherUpdateError;

	private static bool _launcherUpdateBusy;

	private static Timer? _launcherUpdateTimer;

	private static readonly TimeSpan LauncherUpdateInterval = TimeSpan.FromMinutes(30L);

	private static long _lastLauncherUpdateCheckTicks;

	public Window Window => _window;

	public CoreWebView2 Core => _core;

	private static string FriendsBaseUrl => "https://friends-public-service-prod.neofn.dev/friends";

	private static string AccountBaseUrl => "https://account-public-service-prod.neofn.dev/account";

	public NeoWebBridge(CoreWebView2 core, Window window)
	{
		_core = core;
		_window = window;
		_dispatcher = window.DispatcherQueue;
		_core.WebMessageReceived += OnWebMessageReceived;
		RegisterHandlers();
		lock (BridgesLock)
		{
			LiveBridges.Add(this);
		}
	}

	public static void Broadcast(string eventName, object? detail)
	{
		NeoWebBridge[] array;
		lock (BridgesLock)
		{
			array = LiveBridges.ToArray();
		}
		NeoWebBridge[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].DispatchEvent(eventName, detail);
		}
	}

	public void AddCleanup(Action cleanup)
	{
		_cleanup.Add(cleanup);
	}

	public void Unregister()
	{
		if (_disposed)
		{
			return;
		}
		_disposed = true;
		lock (BridgesLock)
		{
			LiveBridges.Remove(this);
		}
		try
		{
			_core.WebMessageReceived -= OnWebMessageReceived;
		}
		catch
		{
		}
		foreach (Action item in _cleanup)
		{
			try
			{
				item();
			}
			catch
			{
			}
		}
		_cleanup.Clear();
	}

	public void Register(string command, Func<NeoBridgeContext, Task<object?>> handler)
	{
		_handlers[command] = handler;
	}

	public void Register(string command, Func<NeoBridgeContext, Task> handler)
	{
		_handlers[command] = async (NeoBridgeContext ctx) =>
		{
			await handler(ctx);
			return (object?)null;
		};
	}

	private void RegisterHandlers()
	{
		Register("ping", (NeoBridgeContext _) => Task.FromResult((object)"pong"));
		RegisterWindowHandlers();
		RegisterAuthHandlers();
		RegisterBuildHandlers();
		RegisterGameHandlers();
		RegisterShopFriendHandlers();
		RegisterMessageHandlers();
		RegisterDataHandlers();
		RegisterSettingsHandlers();
		RegisterStorageHandlers();
		RegisterLibraryHandlers();
		RegisterUpdateHandlers();
	}

	private void RegisterWindowHandlers()
	{
		Register("neo_window_minimize", (NeoBridgeContext _) =>
		{
			MinimizeWindow();
			return Task.CompletedTask;
		});
		Register("neo_window_close", (NeoBridgeContext _) =>
		{
			try
			{
				App.RequestMainWindowClose();
			}
			catch
			{
				_window.Close();
			}
			return Task.CompletedTask;
		});
		Register("neo_window_drag_start", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			ok = true,
			native = true
		}));
		Register("neo_window_drag_move", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			ok = true,
			native = true
		}));
		Register("neo_window_drag_end", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			ok = true,
			native = true
		}));
		Register("open_external_url", OpenExternalUrlAsync);
		Register("openExternalUrl", OpenExternalUrlAsync);
	}

	private void RegisterAuthHandlers()
	{
		Register("loginDiscord", LoginDiscordAsync);
		Register("exchange_discord_oauth_code", ExchangeDiscordOAuthCodeAsync);
		Register("complete_neo_account_setup", CompleteAccountSetupAsync);
		Register("completeAccountSetup", CompleteAccountSetupAsync);
		Register("getSession", CurrentSessionAsync);
		Register("logout", LogoutAsync);
	}

	private void RegisterBuildHandlers()
	{
		App.InstallService.ProgressUpdated += OnInstallProgress;
		App.InstallService.InstallCompleted += OnInstallCompleted;
		AddCleanup(() =>
		{
			App.InstallService.ProgressUpdated -= OnInstallProgress;
			App.InstallService.InstallCompleted -= OnInstallCompleted;
		});
		Register("startInstall", StartInstallAsync);
		Register("cancelInstall", (NeoBridgeContext _) =>
		{
			App.InstallService.Cancel();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("cancelDownload", (NeoBridgeContext _) =>
		{
			App.InstallService.Cancel();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("toggleInstallPause", (NeoBridgeContext _) =>
		{
			bool paused = App.InstallService.TogglePause();
			return Task.FromResult((object)new
			{
				isSuccess = true,
				paused = paused
			});
		});
		Register("checkDownloadServices", CheckDownloadServicesAsync);
		Register("get_neo_server_status", async (NeoBridgeContext _) =>
		{
			string status = "Unknown";
			try
			{
				status = await App.LightswitchService.GetFortniteStatusAsync();
			}
			catch
			{
			}
			return new
			{
				online = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase),
				status = status
			};
		});
		Register("get_build_splash", GetBuildSplashAsync);
		Register("verify_neo_build", VerifyBuildAsync);
		Register("describe_build", DescribeBuildAsync);
		Register("list_install_drives", (NeoBridgeContext _) => Task.FromResult(ListInstallDrives()));
		Register("list_neo_folder_roots", (NeoBridgeContext _) => Task.FromResult(ListFolderRoots()));
		Register("list_neo_folder_children", ListFolderChildrenAsync);
		Register("describe_neo_build_folder", DescribeBuildFolderPathAsync);
		Register("select_neo_download_folder", (NeoBridgeContext _) => PickDownloadFolderAsync());
		Register("select_neo_install_folder", (NeoBridgeContext _) => PickInstallFolderAsync());
		Register("select_neo_build_folder", (NeoBridgeContext _) => PickBuildFolderAsync());
	}

	private void RegisterGameHandlers()
	{
		Register("launch_neo_build", LaunchBuildAsync);
		Register("launchBuild", LaunchBuildAsync);
		Register("terminate_neo_build", async (NeoBridgeContext _) =>
		{
			await Task.Run((Action)TerminateGame);
			return new
			{
				isSuccess = true
			};
		});
		Register("cancel_neo_launch", async (NeoBridgeContext _) =>
		{
			bool wasLaunching = App.GameLauncher.IsGameLaunching;
			App.GameLauncher.CancelLaunch();
			await Task.Run((Action)TerminateGame);
			return new
			{
				isSuccess = true,
				wasLaunching = wasLaunching
			};
		});
		Register("write_neo_log", (NeoBridgeContext ctx) =>
		{
			string text = ctx.GetString("message") ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				string category = ctx.GetString("category") ?? "webui";
				if (string.Equals(ctx.GetString("level"), "warn", StringComparison.OrdinalIgnoreCase))
				{
					NeoLog.Warn(category, text);
				}
				else
				{
					NeoLog.Info(category, text);
				}
			}
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("is_neo_game_running", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			isRunning = IsGameRunning(),
			running = IsGameRunning()
		}));
		Register("get_neo_game_state", (NeoBridgeContext _) => Task.FromResult(GameStateViewModel()));
		Register("set_neo_presence", SetNeoPresenceAsync);
	}

	private void RegisterShopFriendHandlers()
	{
		Register("fetch_neo_friends", FetchNeoFriendsAsync);
		Register("fetch_neo_friend_presence", FetchNeoFriendPresenceAsync);
		Register("connect_neo_presence", ConnectNeoPresenceAsync);
		Register("disconnect_neo_presence", DisconnectNeoPresenceAsync);
		Register("search_neo_accounts", SearchNeoAccountsAsync);
		Register("neo_friend_action", NeoFriendActionAsync);
		Register("setFriendNickname", SetFriendNickname);
		Register("open_neo_friends_window", (NeoBridgeContext _) =>
		{
			NeoChildWindows.OpenFriends();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("close_neo_friends_window", (NeoBridgeContext _) =>
		{
			NeoChildWindows.CloseFriends();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("minimize_neo_friends_window", (NeoBridgeContext _) =>
		{
			NeoChildWindows.MinimizeFriends();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("open_neo_friend_context_menu", (NeoBridgeContext ctx) =>
		{
			NeoChildWindows.OpenFriendContextMenu(_window, ctx);
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("resize_neo_friend_context_menu", (NeoBridgeContext ctx) =>
		{
			NeoChildWindows.ResizeFriendContextMenu(ctx.GetNumber("menuHeight") ?? 328.0);
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
		Register("close_neo_friend_context_menu", (NeoBridgeContext _) =>
		{
			NeoChildWindows.CloseFriendContextMenu();
			return Task.FromResult((object)new
			{
				isSuccess = true
			});
		});
	}

	private void RegisterMessageHandlers()
	{
		Register("fetch_neo_messages", FetchNeoMessagesAsync);
		Register("send_neo_message", SendNeoMessageAsync);
		Register("mark_neo_messages_read", MarkNeoMessagesReadAsync);
		Register("open_neo_message_thread", (NeoBridgeContext ctx) =>
		{
			OpenMessageThread(ctx.GetString("accountId") ?? "");
			return Task.FromResult((object)new
			{
				ok = true
			});
		});
		Register("consume_neo_message_thread", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			accountId = Interlocked.Exchange(ref _pendingMessageThreadAccountId, "")
		}));
	}

	private void RegisterDataHandlers()
	{
		Register("getNews", GetNewsAsync);
		Register("getTrailers", GetLauncherNewsAsync);
		Register("getBuilds", GetBuildsAsync);
		Register("getServicesState", GetServicesStateAsync);
		Register("checkUsernameAvailable", CheckUsernameAvailableAsync);
	}

	private void RegisterSettingsHandlers()
	{
		Register("getSettings", (NeoBridgeContext _) => Task.FromResult(SettingsViewModel()));
		Register("setLauncherPreferences", SetLauncherPreferencesAsync);
		Register("setLauncherFps", SetLauncherFpsAsync);
		Register("setOpenOnStartup", SetOpenOnStartupAsync);
		Register("setAfterLaunchAction", SetAfterLaunchActionAsync);
		Register("applyAfterLaunchAction", ApplyAfterLaunchActionAsync);
		Register("getAccountStatus", GetAccountStatusAsync);
		Register("getAccountTier", GetAccountTierAsync);
		Register("setDownloadFolder", SetDownloadFolderAsync);
		Register("getProfileStatus", (NeoBridgeContext _) => Task.FromResult((object)new
		{
			statusId = App.SettingsService.Settings.ProfileStatusId
		}));
		Register("setProfileStatus", SetProfileStatusAsync);
		Register("getNotificationSeenIds", (NeoBridgeContext _) => Task.FromResult((object)App.SettingsService.Settings.NotificationSeenIds.ToArray()));
		Register("setNotificationSeenIds", SetNotificationSeenIdsAsync);
		Register("get_neo_system_capability", async (NeoBridgeContext _) =>
		{
			SystemCapability.Snapshot snapshot = await Task.Run((Func<SystemCapability.Snapshot>)SystemCapability.Get);
			return new
			{
				logicalCores = snapshot.LogicalCores,
				totalMemoryGb = snapshot.TotalMemoryGb,
				gpuName = snapshot.GpuName,
				gpuMemoryGb = snapshot.GpuMemoryGb,
				gpuLooksIntegrated = snapshot.GpuLooksIntegrated,
				score = snapshot.Score,
				isLowEnd = snapshot.IsLowEnd,
				reasons = snapshot.Reasons
			};
		});
	}

	private void RegisterStorageHandlers()
	{
		Register("storageGetAll", (NeoBridgeContext _) => Task.FromResult((object)SanitizedStorageSnapshot()));
		Register("storageSet", (NeoBridgeContext ctx) =>
		{
			string text = ctx.GetString("key");
			if (!string.IsNullOrEmpty(text))
			{
				JsonElement? element = ctx.GetElement("value");
				JsonNode node = (element.HasValue ? JsonNode.Parse(element.Value.GetRawText()) : null);
				node = SanitizeStorageValue(text, node);
				lock (StorageLock)
				{
					Storage()[text] = node;
				}
				SaveStorage();
				Broadcast("neo-storage-changed", new
				{
					key = text,
					value = node?.DeepClone()
				});
			}
			return Task.FromResult((object)new
			{
				ok = true
			});
		});
		Register("storageRemove", (NeoBridgeContext ctx) =>
		{
			string text = ctx.GetString("key");
			if (!string.IsNullOrEmpty(text))
			{
				lock (StorageLock)
				{
					Storage().Remove(text);
				}
				SaveStorage();
				Broadcast("neo-storage-changed", new
				{
					key = text,
					value = (object)null
				});
			}
			return Task.FromResult((object)new
			{
				ok = true
			});
		});
	}

	private void RegisterLibraryHandlers()
	{
		Register("getLibrary", GetLibraryAsync);
		Register("import_neo_build", ImportBuildAsync);
		Register("forget_neo_build", ForgetBuildAsync);
		Register("migrate_neo_library", MigrateLibraryAsync);
	}

	private void RegisterUpdateHandlers()
	{
		Register("getLauncherUpdate", (NeoBridgeContext _) => Task.FromResult(LauncherUpdateViewModel()));
		Register("checkLauncherUpdate", async (NeoBridgeContext _) =>
		{
			await CheckLauncherUpdateAsync();
			return LauncherUpdateViewModel();
		});
		Register("applyLauncherUpdate", async (NeoBridgeContext _) =>
		{
			await ApplyLauncherUpdateAsync();
			return LauncherUpdateViewModel();
		});
	}

	private async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		string text;
		try
		{
			text = args.TryGetWebMessageAsString();
		}
		catch
		{
			return;
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		string id = "";
		try
		{
			using JsonDocument doc = JsonDocument.Parse(text);
			JsonElement rootElement = doc.RootElement;
			if (rootElement.ValueKind != JsonValueKind.Object || !rootElement.TryGetProperty("kind", out var value) || value.GetString() != "invoke")
			{
				return;
			}
			id = (rootElement.TryGetProperty("id", out var value2) ? (value2.GetString() ?? "") : "");
			string command = (rootElement.TryGetProperty("command", out var value3) ? (value3.GetString() ?? "") : "");
			JsonElement payload = (rootElement.TryGetProperty("payload", out var value4) ? value4.Clone() : default(JsonElement));
			Reply(id, ok: true, await DispatchAsync(command, payload), null);
		}
		catch (Exception ex)
		{
			Reply(id, ok: false, null, ex.Message);
		}
	}

	private async Task<object?> DispatchAsync(string command, JsonElement payload)
	{
		if (!_handlers.TryGetValue(command, out Func<NeoBridgeContext, Task<object>> value))
		{
			throw new NeoBridgeUnknownCommandException(command);
		}
		return await value(new NeoBridgeContext(this, payload));
	}

	private void Reply(string id, bool ok, object? result, string? error)
	{
		if (!string.IsNullOrEmpty(id))
		{
			EnqueuePost(JsonSerializer.Serialize(new
			{
				kind = "result",
				id = id,
				ok = ok,
				result = result,
				error = error
			}, JsonOptions));
		}
	}

	public void DispatchEvent(string eventName, object? detail)
	{
		EnqueuePost(JsonSerializer.Serialize(new
		{
			kind = "event",
			@event = eventName,
			detail = detail
		}, JsonOptions));
	}

	private void EnqueuePost(string json)
	{
		if (_dispatcher.HasThreadAccess)
		{
			TryPost(json);
			return;
		}
		_dispatcher.TryEnqueue(() =>
		{
			TryPost(json);
		});
	}

	private void TryPost(string json)
	{
		try
		{
			_core.PostWebMessageAsString(json);
		}
		catch
		{
		}
	}

	private static async Task<object?> LogoutAsync(NeoBridgeContext ctx)
	{
		await App.PresenceService.DisconnectAsync();
		App.AccountService.Logout();
		Broadcast("neo-session-changed", new
		{
			isLoggedIn = false
		});
		return new
		{
			ok = true
		};
	}

	private static async Task<object?> CurrentSessionAsync(NeoBridgeContext ctx)
	{
		bool flag = false;
		if ((object)App.AccountService.CurrentUser == null && App.AccountService.HasStoredSession)
		{
			try
			{
				AccountService.LoginResult loginResult = await App.AccountService.TryRestoreSessionAsync();
				flag = !loginResult.IsSuccess && loginResult.IsUnreachable;
			}
			catch
			{
				flag = App.AccountService.HasStoredSession;
			}
		}
		if ((object)App.AccountService.CurrentUser == null)
		{
			return new
			{
				isLoggedIn = false,
				offline = (flag && App.AccountService.HasStoredSession)
			};
		}
		return await BuildSessionAsync(App.AccountService);
	}

	private static async Task<object?> LoginDiscordAsync(NeoBridgeContext ctx)
	{
		AccountService account = App.AccountService;
		TaskCompletionSource<string?> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		await account.LaunchSsoAsync("Discord", (string err) =>
		{
			tcs.TrySetResult(string.IsNullOrEmpty(err) ? "Login failed." : err);
		}, () =>
		{
			tcs.TrySetResult(null);
		});
		string text = await tcs.Task;
		if (text != null)
		{
			return new
			{
				isSuccess = false,
				error = text
			};
		}
		object obj = await BuildSessionAsync(account);
		Broadcast("neo-session-changed", obj);
		return obj;
	}

	private static async Task<object?> ExchangeDiscordOAuthCodeAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("code");
		if (string.IsNullOrEmpty(text))
		{
			return new
			{
				isSuccess = false,
				error = "Missing authorization code."
			};
		}
		AccountService account = App.AccountService;
		AccountService.LoginResult loginResult = await account.LoginAsync(new OAuthTokenRequest
		{
			GrantType = OAuthGrantType.AuthorizationCode,
			AuthorizationCode = text
		});
		if (!loginResult.IsSuccess)
		{
			return new
			{
				isSuccess = false,
				error = loginResult.Error
			};
		}
		return await BuildSessionAsync(account);
	}

	private static async Task<object> BuildSessionAsync(AccountService account)
	{
		bool setupCompleted = true;
		try
		{
			setupCompleted = await account.GetSetupCompletedAsync();
			await App.LightswitchService.CheckStatusAsync();
		}
		catch
		{
		}
		AccountService.UserRecord user = account.CurrentUser;
		await account.GetAccessTokenAsync();
		await App.LightswitchService.CheckStatusAsync();
		BanStatus accountBan = null;
		try
		{
			accountBan = await App.PrismService.GetBanStatusAsync();
		}
		catch
		{
		}
		return new
		{
			isSuccess = true,
			isLoggedIn = true,
			providerName = "Discord",
			banned = App.LightswitchService.IsBanned,
			bannedAt = accountBan?.BannedAt?.ToString("o"),
			banReason = accountBan?.Reason,
			accountId = user.Id,
			displayName = user.DisplayName,
			username = user.DisplayName,
			email = user.Email,
			avatarUrl = user.AvatarUrl,
			setupCompleted = setupCompleted,
			account = new
			{
				id = user.Id,
				displayName = user.DisplayName,
				email = user.Email,
				avatarUrl = user.AvatarUrl
			}
		};
	}

	private static async Task<object?> CompleteAccountSetupAsync(NeoBridgeContext ctx)
	{
		string displayName = ctx.GetString("displayName");
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return new
			{
				isSuccess = false,
				message = "Missing display name."
			};
		}
		AccountService.SetupResult setupResult = await App.AccountService.CompleteSetupAsync(displayName);
		if (setupResult.IsSuccess)
		{
			AccountService.UserRecord user = App.AccountService.CurrentUser;
			if ((object)user != null)
			{
				Broadcast("neo-session-changed", await BuildSessionAsync(App.AccountService));
			}
			return new
			{
				isSuccess = true,
				account = new
				{
					displayName = (user?.DisplayName ?? displayName)
				}
			};
		}
		if (setupResult.Failure == AccountService.SetupFailure.DisplayNameTaken)
		{
			return new
			{
				isSuccess = false,
				failure = "DisplayNameTaken"
			};
		}
		return new
		{
			isSuccess = false,
			message = "Neo could not save this username."
		};
	}

	private static JsonArray CloneLauncherNewsOrEmpty()
	{
		lock (LauncherNewsCacheLock)
		{
			if (CachedLauncherNews == null)
			{
				return new JsonArray();
			}
			try
			{
				return (JsonNode.Parse(CachedLauncherNews.ToJsonString()) as JsonArray) ?? new JsonArray();
			}
			catch
			{
				return new JsonArray();
			}
		}
	}

	private static async Task<object?> GetLauncherNewsAsync(NeoBridgeContext ctx)
	{
		lock (LauncherNewsCacheLock)
		{
			if (CachedLauncherNews != null && DateTime.UtcNow - CachedLauncherNewsUtc < TimeSpan.FromMinutes(5L))
			{
				return CloneLauncherNewsOrEmpty();
			}
		}
		try
		{
			using HttpResponseMessage response = await ContentHttp.GetAsync(LauncherNewsUrl).ConfigureAwait(continueOnCapturedContext: false);
			if (response.IsSuccessStatusCode && JsonNode.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false)) is JsonObject jsonObject)
			{
				int num = ((jsonObject["version"] is JsonValue jsonValue && jsonValue.TryGetValue<int>(out var value)) ? value : 0);
				if (num == 1 && jsonObject["trailers"] is JsonArray jsonArray)
				{
					JsonArray cachedLauncherNews = (JsonNode.Parse(jsonArray.ToJsonString()) as JsonArray) ?? new JsonArray();
					lock (LauncherNewsCacheLock)
					{
						CachedLauncherNews = cachedLauncherNews;
						CachedLauncherNewsUtc = DateTime.UtcNow;
					}
				}
				else
				{
					NeoLog.Warn("WebBridge", $"Launcher news feed reported schema version {num}; expected {1}. Ignoring.");
				}
			}
		}
		catch
		{
		}
		return CloneLauncherNewsOrEmpty();
	}

	private static JsonArray CloneNewsOrEmpty()
	{
		lock (NewsCacheLock)
		{
			if (CachedNews == null)
			{
				return new JsonArray();
			}
			try
			{
				return (JsonNode.Parse(CachedNews.ToJsonString()) as JsonArray) ?? new JsonArray();
			}
			catch
			{
				return new JsonArray();
			}
		}
	}

	private static async Task<object?> GetNewsAsync(NeoBridgeContext ctx)
	{
		lock (NewsCacheLock)
		{
			if (CachedNews != null && DateTime.UtcNow - CachedNewsUtc < TimeSpan.FromMinutes(5L))
			{
				return CloneNewsOrEmpty();
			}
		}
		try
		{
			using HttpResponseMessage response = await ContentHttp.GetAsync(ContentPagesUrl).ConfigureAwait(continueOnCapturedContext: false);
			if (response.IsSuccessStatusCode && JsonNode.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false)) is JsonObject root)
			{
				JsonArray cachedNews = MapContentPagesNews(root);
				lock (NewsCacheLock)
				{
					CachedNews = cachedNews;
					CachedNewsUtc = DateTime.UtcNow;
				}
			}
		}
		catch
		{
		}
		return CloneNewsOrEmpty();
	}

	private static JsonArray MapContentPagesNews(JsonObject root)
	{
		JsonArray jsonArray = new JsonArray();
		JsonArray jsonArray2 = GetNewsMessages(root, "battleroyalenews") ?? GetNewsMessages(root, "battleroyalenewsv2") ?? GetNewsMessages(root, "creativenews");
		if (jsonArray2 == null)
		{
			return jsonArray;
		}
		int num = 0;
		bool value = default;
		foreach (JsonNode item in jsonArray2)
		{
			if (!(item is JsonObject jsonObject))
			{
				num++;
				continue;
			}
			if ((jsonObject["hidden"] is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out value)) & value)
			{
				num++;
				continue;
			}
			string text = SafeLocalizedString(jsonObject["title"]);
			if (string.IsNullOrWhiteSpace(text))
			{
				num++;
				continue;
			}
			string text2 = SafeLocalizedString(jsonObject["body"]);
			string text3 = SafeLocalizedString(jsonObject["image"]);
			string value2 = SafeLocalizedString(jsonObject["adspace"]);
			JsonArray jsonArray3 = new JsonArray();
			if (!string.IsNullOrWhiteSpace(value2))
			{
				jsonArray3.Add(value2);
			}
			jsonArray3.Add("News");
			jsonArray.Add(new JsonObject
			{
				["id"] = $"neo-news-{num}",
				["contentType"] = "news",
				["title"] = text,
				["modalTitle"] = text,
				["image"] = text3,
				["modalImage"] = text3,
				["modalBody"] = text2,
				["categories"] = jsonArray3,
				["badge"] = "NEW"
			});
			num++;
		}
		return jsonArray;
	}

	private static JsonArray? GetNewsMessages(JsonObject root, string key)
	{
		if (root[key] is JsonObject jsonObject && jsonObject["news"] is JsonObject jsonObject2 && jsonObject2["messages"] is JsonArray { Count: >0 } jsonArray)
		{
			return jsonArray;
		}
		return null;
	}

	private static string SafeLocalizedString(JsonNode? node)
	{
		if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
		{
			return value;
		}
		if (node is JsonObject jsonObject)
		{
			if (jsonObject["en"] is JsonValue jsonValue2 && jsonValue2.TryGetValue<string>(out string value2))
			{
				return value2;
			}
			foreach (KeyValuePair<string, JsonNode> item in jsonObject)
			{
				if (item.Value is JsonValue jsonValue3 && jsonValue3.TryGetValue<string>(out string value3))
				{
					return value3;
				}
			}
		}
		return "";
	}

	private static async Task<object?> GetServicesStateAsync(NeoBridgeContext ctx)
	{
		string season = "";
		string status = "Unknown";
		try
		{
			await App.LauncherService.GetBuildsAsync();
			BuildInfo liveBuild = App.LauncherService.LiveBuild;
			if (liveBuild != null)
			{
				season = liveBuild.Version.Name;
			}
		}
		catch
		{
		}
		try
		{
			status = await App.LightswitchService.GetFortniteStatusAsync();
		}
		catch
		{
		}
		OnlineCounts onlineCounts = await App.LauncherService.GetOnlineCountsAsync();
		return new
		{
			playersOnline = (onlineCounts?.Fortnite ?? 0),
			launcherOnline = (onlineCounts?.Launcher ?? 0),
			season = season,
			status = status
		};
	}

	private static async Task<object?> CheckUsernameAvailableAsync(NeoBridgeContext ctx)
	{
		string text = (ctx.GetString("name") ?? ctx.GetString("username") ?? "").Trim();
		if (text.Length == 0)
		{
			return new
			{
				available = false,
				@checked = false
			};
		}
		try
		{
			return new
			{
				available = await App.AccountService.IsDisplayNameAvailableAsync(text),
				@checked = true
			};
		}
		catch
		{
			return new
			{
				available = true,
				@checked = false
			};
		}
	}

	private static async Task<object?> GetBuildsAsync(NeoBridgeContext ctx)
	{
		JsonArray jsonArray = CloneCachedBuilds();
		if (jsonArray != null)
		{
			EnsureBuildsRefreshStarted();
			return jsonArray;
		}
		try
		{
			List<BuildInfo> builds = App.LauncherService.Builds;
			if (builds.Count > 0)
			{
				JsonArray jsonArray2 = (JsonSerializer.SerializeToNode(builds, JsonOptions) as JsonArray) ?? new JsonArray();
				NeoBuildMeta.Enrich(jsonArray2);
				lock (BuildsCacheLock)
				{
					CachedBuilds = jsonArray2;
					CachedBuildsUtc = DateTime.UtcNow;
				}
				EnsureBuildsRefreshStarted();
				return CloneCachedBuilds() ?? new JsonArray();
			}
		}
		catch
		{
		}
		EnsureBuildsRefreshStarted();
		return new JsonArray();
	}

	private static JsonArray? CloneCachedBuilds()
	{
		lock (BuildsCacheLock)
		{
			if (CachedBuilds == null)
			{
				return null;
			}
			try
			{
				return JsonNode.Parse(CachedBuilds.ToJsonString(JsonOptions)) as JsonArray;
			}
			catch
			{
				return new JsonArray();
			}
		}
	}

	private static Task EnsureBuildsRefreshStarted()
	{
		Task task;
		lock (BuildsCacheLock)
		{
			task = BuildsRefreshTask;
			if (task != null && !task.IsCompleted)
			{
				task = BuildsRefreshTask;
			}
			else
			{
				TimeSpan timeSpan = DateTime.UtcNow - CachedBuildsUtc;
				if (CachedBuilds != null && timeSpan < TimeSpan.FromMinutes(2L))
				{
					task = Task.CompletedTask;
				}
				else
				{
					BuildsRefreshTask = Task.Run((Func<Task?>)RefreshBuildsCacheAsync);
					task = BuildsRefreshTask;
				}
			}
		}
		return task;
	}

	private static async Task RefreshBuildsCacheAsync()
	{
		JsonArray array = new JsonArray();
		try
		{
			List<BuildInfo> list = await App.LauncherService.GetBuildsAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (list.Count > 0)
			{
				array = (JsonSerializer.SerializeToNode(list, JsonOptions) as JsonArray) ?? array;
			}
		}
		catch
		{
			return;
		}
		NeoBuildMeta.Enrich(array);
		lock (BuildsCacheLock)
		{
			CachedBuilds = array;
			CachedBuildsUtc = DateTime.UtcNow;
		}
		Broadcast("neo-service-builds-updated", CloneCachedBuilds() ?? new JsonArray());
		BroadcastLibraryChanged();
	}

	private static async Task<object?> DisconnectNeoPresenceAsync(NeoBridgeContext ctx)
	{
		await App.PresenceService.DisconnectAsync();
		return new
		{
			ok = true
		};
	}

	private static async Task<object?> ConnectNeoPresenceAsync(NeoBridgeContext ctx)
	{
		string accountId = ctx.GetString("accountId") ?? App.AccountService.CurrentUser?.Id ?? "";
		string text = await GetFriendsAccessTokenAsync(ctx);
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(text))
		{
			return new
			{
				ok = false,
				error = "missing-auth"
			};
		}
		bool flag = await App.PresenceService.EnsureConnectedAsync(accountId, text);
		return new
		{
			ok = flag,
			connected = flag
		};
	}

	private static async Task<object?> FetchNeoFriendsAsync(NeoBridgeContext ctx)
	{
		string accountId = ctx.GetString("accountId") ?? App.AccountService.CurrentUser?.Id ?? "";
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return EmptyFriends("missing-account");
		}
		string text = await GetFriendsAccessTokenAsync(ctx);
		if (string.IsNullOrWhiteSpace(text))
		{
			return EmptyFriends("missing-auth");
		}
		App.PresenceService.EnsureConnectedAsync(accountId, text);
		if (!ctx.GetBool("force") && FriendsRosterCache.TryGetValue(accountId, out CachedFriendsRoster value) && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - value.FetchedAt < FriendsRosterCacheTtlMs)
		{
			return value.Payload;
		}
		return await GetOrStartRosterFetch(accountId, text);
	}

	private static Task<object?> GetOrStartRosterFetch(string accountId, string accessToken)
	{
		lock (FriendsRosterGate)
		{
			if (FriendsRosterInFlight.TryGetValue(accountId, out Task<object> value))
			{
				return value;
			}
			Task<object?> task = BuildFriendsRosterAsync(accountId, accessToken);
			FriendsRosterInFlight[accountId] = task;
			task.ContinueWith((Task<object?> _) =>
			{
				lock (FriendsRosterGate)
				{
					if (FriendsRosterInFlight.TryGetValue(accountId, out Task<object> value2) && value2 == task)
					{
						FriendsRosterInFlight.Remove(accountId);
					}
				}
			}, TaskScheduler.Default);
			return task;
		}
	}

	public static void InvalidateFriendsRosterCache()
	{
		Interlocked.Increment(ref _friendsRosterGeneration);
		FriendsRosterCache.Clear();
	}

	public static void BroadcastFriendsChanged(object? detail = null)
	{
		InvalidateFriendsRosterCache();
		Broadcast("neo-friends-changed", detail);
	}

	private static async Task<object?> BuildFriendsRosterAsync(string accountId, string accessToken)
	{
		int generation = Volatile.Read(in _friendsRosterGeneration);
		JsonNode friendsPayload = await GetJsonAsync(HttpMethod.Get, FriendsBaseUrl + "/api/public/friends/" + Uri.EscapeDataString(accountId) + "?includePending=true", accessToken);
		JsonNode node = (await TryGetJsonAsync(HttpMethod.Get, FriendsBaseUrl + "/api/public/blocklist/" + Uri.EscapeDataString(accountId), accessToken)) ?? new JsonArray();
		JsonArray friendEntries = NormalizeArray(friendsPayload, "friends", "data", "items");
		JsonArray blockEntries = NormalizeArray(node, "blockedUsers", "blocked", "data", "items");
		HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (JsonNode item in friendEntries)
		{
			string friendAccountId = GetFriendAccountId(item, accountId);
			if (!string.IsNullOrWhiteSpace(friendAccountId))
			{
				ids.Add(friendAccountId);
			}
		}
		foreach (JsonNode item2 in blockEntries)
		{
			string text = GetString(item2, "accountId", "blockedId", "blocked_id", "id");
			if (string.IsNullOrWhiteSpace(text) && item2 is JsonValue jsonValue)
			{
				text = jsonValue.ToJsonString().Trim('"');
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				ids.Add(text);
			}
		}
		Dictionary<string, JsonNode?> profiles = await FetchPublicAccountProfilesAsync(ids, accessToken);
		Dictionary<string, string> dictionary = await FetchFriendAvatarsAsync(ids, accessToken);
		Dictionary<string, string> friendNicknames = App.SettingsService.Settings.FriendNicknames;
		List<object> list = new List<object>();
		List<object> list2 = new List<object>();
		List<object> list3 = new List<object>();
		foreach (JsonNode item3 in friendEntries)
		{
			string friendAccountId2 = GetFriendAccountId(item3, accountId);
			if (!string.IsNullOrWhiteSpace(friendAccountId2))
			{
				object obj = CreateFriendViewModel(item3, profiles.TryGetValue(friendAccountId2, out JsonNode value) ? value : null, friendNicknames, accountId, dictionary.TryGetValue(friendAccountId2, out var value2) ? value2 : null);
				string stringFromAnonymous = GetStringFromAnonymous(obj, "relationship");
				if (stringFromAnonymous == "incoming")
				{
					list2.Add(obj);
				}
				else if (stringFromAnonymous == "outgoing")
				{
					list3.Add(obj);
				}
				else
				{
					list.Add(obj);
				}
			}
		}
		List<object> list4 = new List<object>();
		foreach (JsonNode item4 in blockEntries)
		{
			string text2 = GetString(item4, "accountId", "blockedId", "blocked_id", "id");
			if (string.IsNullOrWhiteSpace(text2) && item4 is JsonValue jsonValue2)
			{
				text2 = jsonValue2.ToJsonString().Trim('"');
			}
			if (!string.IsNullOrWhiteSpace(text2))
			{
				profiles.TryGetValue(text2, out JsonNode value3);
				string displayName = GetDisplayName(value3, text2);
				friendNicknames.TryGetValue(text2, out var value4);
				list4.Add(new
				{
					id = text2,
					accountId = text2,
					username = displayName,
					nickname = (value4 ?? ""),
					status = "blocked",
					gameStatus = "Blocked",
					avatarUrl = ResolveAvatarUrl(dictionary.TryGetValue(text2, out var value5) ? value5 : null, value3),
					relationship = "blocked",
					raw = item4
				});
			}
		}
		var anon = new
		{
			myAccountId = accountId,
			source = "neo",
			friends = list,
			incoming = list2,
			outgoing = list3,
			blocked = list4
		};
		if (Volatile.Read(in _friendsRosterGeneration) == generation)
		{
			FriendsRosterCache[accountId] = new CachedFriendsRoster(anon, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		}
		return anon;
	}

	internal static async Task<object?> SetNeoPresenceAsync(NeoBridgeContext ctx)
	{
		string accountId = ctx.GetString("accountId") ?? App.AccountService.CurrentUser?.Id ?? "";
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return new
			{
				isSuccess = false,
				error = "missing-account"
			};
		}
		string profileStatusId = (ctx.GetString("profileStatusId") ?? "online").Trim().ToLowerInvariant();
		string presence = (ctx.GetString("presence") ?? ((profileStatusId == "invisible") ? "offline" : "online")).Trim().ToLowerInvariant();
		string activity = ctx.GetString("activity") ?? ctx.GetString("gameStatus") ?? ((presence == "online") ? "In Launcher" : "Offline");
		string gameStatus = ctx.GetString("gameStatus") ?? activity;
		string text = ctx.GetString("resource") ?? "launcher";
		string launcherActivity = (ctx.GetString("launcherActivity") ?? "launcher").Trim().ToLowerInvariant();
		bool flag = presence != "offline" && profileStatusId != "invisible";
		string text2;
		if (flag)
		{
			if (launcherActivity == "launching")
			{
				text2 = "launching";
			}
			else
			{
				text2 = (text.Contains("fortnite", StringComparison.OrdinalIgnoreCase) ? "game" : "launcher");
			}
		}
		else
		{
			text2 = "offline";
		}
		int priority = text2 switch
		{
			"game" => 60, 
			"launching" => 45, 
			"launcher" => 30, 
			_ => flag ? 20 : 0, 
		};
		FriendPresenceState state = new FriendPresenceState(accountId, flag ? "online" : "offline", flag ? activity : "Offline", flag ? gameStatus : "Offline", text, text2, priority, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		FriendPresenceCache[accountId + "/" + text] = state;
		Broadcast("neo-friend-presence-changed", new
		{
			accountId = accountId,
			presence = state
		});
		string text3 = await GetFriendsAccessTokenAsync(ctx);
		NeoPresenceView neoPresenceView = ((!string.IsNullOrWhiteSpace(text3)) ? (await App.PresenceService.PublishLauncherPresenceAsync(accountId, text3, profileStatusId, presence, activity, gameStatus, launcherActivity)) : null);
		NeoPresenceView neoPresenceView2 = neoPresenceView;
		FriendPresenceState friendPresenceState = state;
		if ((object)neoPresenceView2 != null)
		{
			friendPresenceState = ToFriendPresenceState(neoPresenceView2);
			FriendPresenceCache[friendPresenceState.AccountId + "/" + friendPresenceState.Resource] = friendPresenceState;
		}
		return new
		{
			isSuccess = true,
			presence = friendPresenceState,
			xmppConnected = App.PresenceService.IsConnected
		};
	}

	public static void UpsertNativeFriendPresence(NeoPresenceView presence)
	{
		if (!string.IsNullOrWhiteSpace(presence.AccountId))
		{
			FriendPresenceState friendPresenceState = ToFriendPresenceState(presence);
			FriendPresenceCache[friendPresenceState.AccountId + "/" + friendPresenceState.Resource] = friendPresenceState;
		}
	}

	private static FriendPresenceState ToFriendPresenceState(NeoPresenceView presence)
	{
		return new FriendPresenceState(presence.AccountId, presence.Status, presence.Activity, presence.GameStatus, presence.Resource, presence.ResourceType, presence.Priority, presence.UpdatedAt);
	}

	private static Task<object?> FetchNeoFriendPresenceAsync(NeoBridgeContext ctx)
	{
		FriendPresenceState[] presences = (from p in FriendPresenceCache.Values
			orderby p.Priority descending, p.UpdatedAt descending
			select p).ToArray();
		return Task.FromResult((object)new
		{
			source = "bridge-cache",
			presences = presences
		});
	}

	private static async Task<object?> SearchNeoAccountsAsync(NeoBridgeContext ctx)
	{
		string text = (ctx.GetString("query") ?? "").Trim();
		if (text.Length < 2)
		{
			return new
			{
				results = Array.Empty<object>(),
				debug = "short query"
			};
		}
		try
		{
			PublicAccount publicAccount = await App.AccountService.GetAccountByDisplayNameAsync(text);
			if (publicAccount == null)
			{
				return new
				{
					results = Array.Empty<object>(),
					debug = "no match"
				};
			}
			return new
			{
				results = new[]
				{
					new
					{
						id = publicAccount.Id,
						accountId = publicAccount.Id,
						username = publicAccount.DisplayName,
						displayName = publicAccount.DisplayName,
						avatarUrl = ""
					}
				},
				debug = "displayName lookup"
			};
		}
		catch (Exception ex)
		{
			return new
			{
				results = Array.Empty<object>(),
				debug = ex.Message
			};
		}
	}

	private static async Task<object?> NeoFriendActionAsync(NeoBridgeContext ctx)
	{
		string action = (ctx.GetString("action") ?? "").Trim().ToLowerInvariant();
		string accountId = ctx.GetString("accountId") ?? App.AccountService.CurrentUser?.Id ?? "";
		string friendAccountId = ctx.GetString("friendAccountId") ?? "";
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(friendAccountId))
		{
			return new
			{
				ok = false,
				error = "missing_auth_or_friend"
			};
		}
		string text = await GetFriendsAccessTokenAsync(ctx);
		if (string.IsNullOrWhiteSpace(text))
		{
			return new
			{
				ok = false,
				error = "missing_access_token"
			};
		}
		string text2 = action;
		string url;
		HttpMethod method;
		if ((text2 == "accept" || text2 == "add") ? true : false)
		{
			url = $"{FriendsBaseUrl}/api/public/friends/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
			method = HttpMethod.Post;
		}
		else
		{
			bool flag;
			switch (action)
			{
			case "remove":
			case "decline":
			case "cancel":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				url = $"{FriendsBaseUrl}/api/public/friends/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Delete;
			}
			else if (action == "block")
			{
				url = $"{FriendsBaseUrl}/api/public/blocklist/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Post;
			}
			else
			{
				if (!(action == "unblock"))
				{
					return new
					{
						ok = false,
						error = "unknown_action"
					};
				}
				url = $"{FriendsBaseUrl}/api/public/blocklist/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Delete;
			}
		}
		await SendMutationAsync(method, url, text);
		BroadcastFriendsChanged();
		return new
		{
			ok = true
		};
	}

	public static async Task<object?> ExecuteNativeFriendActionAsync(string action, string friendAccountId)
	{
		action = (action ?? "").Trim().ToLowerInvariant();
		string accountId = App.AccountService.CurrentUser?.Id ?? "";
		if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(friendAccountId))
		{
			return new
			{
				ok = false,
				error = "missing_auth_or_friend"
			};
		}
		string text;
		try
		{
			text = await App.AccountService.GetAccessTokenAsync();
		}
		catch
		{
			text = "";
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return new
			{
				ok = false,
				error = "missing_access_token"
			};
		}
		string text2 = action;
		string url;
		HttpMethod method;
		if ((text2 == "accept" || text2 == "add") ? true : false)
		{
			url = $"{FriendsBaseUrl}/api/public/friends/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
			method = HttpMethod.Post;
		}
		else
		{
			bool flag;
			switch (action)
			{
			case "remove":
			case "decline":
			case "cancel":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				url = $"{FriendsBaseUrl}/api/public/friends/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Delete;
			}
			else if (action == "block")
			{
				url = $"{FriendsBaseUrl}/api/public/blocklist/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Post;
			}
			else
			{
				if (!(action == "unblock"))
				{
					return new
					{
						ok = false,
						error = "unknown_action"
					};
				}
				url = $"{FriendsBaseUrl}/api/public/blocklist/{Uri.EscapeDataString(accountId)}/{Uri.EscapeDataString(friendAccountId)}";
				method = HttpMethod.Delete;
			}
		}
		await SendMutationAsync(method, url, text);
		BroadcastFriendsChanged();
		return new
		{
			ok = true
		};
	}

	public static void RespondToNativePartyInvite(string friendAccountId, string answer, string buildId, string buildName, string partyId)
	{
		friendAccountId = (friendAccountId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(friendAccountId))
		{
			Broadcast("neo-party-invite-response", new
			{
				accountId = friendAccountId,
				answer = (answer ?? "").Trim().ToLowerInvariant(),
				buildId = (buildId ?? ""),
				buildName = (buildName ?? ""),
				partyId = (partyId ?? "")
			});
		}
	}

	public static void SetNativeFriendNickname(string accountId, string nickname)
	{
		if (!string.IsNullOrWhiteSpace(accountId))
		{
			Dictionary<string, string> friendNicknames = App.SettingsService.Settings.FriendNicknames;
			nickname = (nickname ?? "").Trim();
			if (string.IsNullOrWhiteSpace(nickname))
			{
				friendNicknames.Remove(accountId);
			}
			else
			{
				friendNicknames[accountId] = nickname;
			}
			App.SettingsService.Save();
			BroadcastFriendsChanged();
		}
	}

	private static object EmptyFriends(string source = "empty")
	{
		return new
		{
			myAccountId = (App.AccountService.CurrentUser?.Id ?? ""),
			source = source,
			friends = Array.Empty<object>(),
			incoming = Array.Empty<object>(),
			outgoing = Array.Empty<object>(),
			blocked = Array.Empty<object>()
		};
	}

	private Task<object?> SetFriendNickname(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("accountId");
		if (!string.IsNullOrWhiteSpace(text))
		{
			string value = ctx.GetString("nickname") ?? "";
			Dictionary<string, string> friendNicknames = App.SettingsService.Settings.FriendNicknames;
			if (string.IsNullOrEmpty(value))
			{
				friendNicknames.Remove(text);
			}
			else
			{
				friendNicknames[text] = value;
			}
			App.SettingsService.Save();
			BroadcastFriendsChanged();
		}
		return Task.FromResult((object)new
		{
			ok = true
		});
	}

	private static async Task<string> GetFriendsAccessTokenAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("accessToken");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		try
		{
			return await App.AccountService.GetAccessTokenAsync();
		}
		catch
		{
			return "";
		}
	}

	private static async Task<JsonNode> GetJsonAsync(HttpMethod method, string url, string accessToken)
	{
		using HttpRequestMessage request = new HttpRequestMessage(method, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Accept.ParseAdd("application/json");
		HttpResponseMessage response = await FriendsHttp.SendAsync(request);
		string text = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"{(int)response.StatusCode} {response.ReasonPhrase}. {text}");
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return new JsonObject();
		}
		return JsonNode.Parse(text) ?? new JsonObject();
	}

	private static async Task<JsonNode?> TryGetJsonAsync(HttpMethod method, string url, string accessToken)
	{
		try
		{
			return await GetJsonAsync(method, url, accessToken);
		}
		catch
		{
			return null;
		}
	}

	private static async Task SendMutationAsync(HttpMethod method, string url, string accessToken)
	{
		using HttpRequestMessage request = new HttpRequestMessage(method, url);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Accept.ParseAdd("application/json");
		if (method == HttpMethod.Post)
		{
			request.Content = new StringContent("", Encoding.UTF8, "application/json");
		}
		HttpResponseMessage response = await FriendsHttp.SendAsync(request);
		string value = await response.Content.ReadAsStringAsync();
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"{(int)response.StatusCode} {response.ReasonPhrase}. {value}");
		}
	}

	private static JsonArray NormalizeArray(JsonNode? node, params string[] keys)
	{
		if (node is JsonArray result)
		{
			return result;
		}
		if (node is JsonObject jsonObject)
		{
			foreach (string propertyName in keys)
			{
				if (jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode) && jsonNode is JsonArray result2)
				{
					return result2;
				}
			}
		}
		return new JsonArray();
	}

	private static string? GetString(JsonNode? node, params string[] keys)
	{
		if (node == null)
		{
			return null;
		}
		foreach (string propertyName in keys)
		{
			if (node is JsonObject jsonObject && jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode))
			{
				string text = ValueToString(jsonNode);
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		return null;
	}

	private static string? ValueToString(JsonNode? node)
	{
		if (node == null)
		{
			return null;
		}
		if (node is JsonValue jsonValue)
		{
			if (jsonValue.TryGetValue<string>(out string value))
			{
				return value;
			}
			if (jsonValue.TryGetValue<int>(out var value2))
			{
				return value2.ToString();
			}
			if (jsonValue.TryGetValue<long>(out var value3))
			{
				return value3.ToString();
			}
			if (jsonValue.TryGetValue<Guid>(out var value4))
			{
				return value4.ToString();
			}
		}
		return null;
	}

	private static JsonNode? GetObject(JsonNode? node, params string[] keys)
	{
		if (!(node is JsonObject jsonObject))
		{
			return null;
		}
		foreach (string propertyName in keys)
		{
			if (jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode) && jsonNode is JsonObject)
			{
				return jsonNode;
			}
		}
		return null;
	}

	private static string GetFriendAccountId(JsonNode? entry, string currentAccountId)
	{
		string text = GetString(entry, "accountId", "friendId", "friend_id", "friendAccountId", "friend_account_id", "id");
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, currentAccountId, StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		string[] array = new string[3] { "friend", "account", "user" };
		foreach (string text2 in array)
		{
			string text3 = GetString(GetObject(entry, text2), "accountId", "account_id", "id");
			if (!string.IsNullOrWhiteSpace(text3) && !string.Equals(text3, currentAccountId, StringComparison.OrdinalIgnoreCase))
			{
				return text3;
			}
		}
		return "";
	}

	private static string GetRelationshipBucket(JsonNode? entry)
	{
		string text = (GetString(entry, "status", "Status", "friendshipStatus", "friendStatus") ?? "").ToLowerInvariant();
		string text2 = (GetString(entry, "direction", "Direction", "requestDirection") ?? "").ToLowerInvariant();
		if (text.Contains("pending") && (text2.Contains("inbound") || text2.Contains("incoming")))
		{
			return "incoming";
		}
		if (text.Contains("pending") && (text2.Contains("outbound") || text2.Contains("outgoing")))
		{
			return "outgoing";
		}
		if (text.Contains("pending"))
		{
			return "outgoing";
		}
		return "accepted";
	}

	private static string GetDisplayName(JsonNode? profile, string fallback)
	{
		return GetString(profile, "displayName", "DisplayName", "display_name", "username", "name") ?? fallback ?? "Neo Player";
	}

	private static async Task<Dictionary<string, JsonNode?>?> FetchPublicAccountProfileBatchAsync(IReadOnlyList<string> accountIds, string accessToken)
	{
		string text = string.Join("&", accountIds.Select((string id) => "accountId=" + Uri.EscapeDataString(id)));
		JsonNode node;
		try
		{
			node = await GetJsonAsync(HttpMethod.Get, AccountBaseUrl + "/api/public/account?" + text, accessToken);
		}
		catch
		{
			return null;
		}
		Dictionary<string, JsonNode> dictionary = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase);
		foreach (JsonNode item in NormalizeArray(node, "accounts", "data", "items"))
		{
			string text2 = GetString(item, "id", "accountId", "account_id");
			if (!string.IsNullOrWhiteSpace(text2))
			{
				dictionary[text2] = item;
			}
		}
		return dictionary;
	}

	private static async Task<Dictionary<string, JsonNode?>> FetchPublicAccountProfilesAsync(IEnumerable<string> accountIds, string accessToken)
	{
		Dictionary<string, JsonNode?> profiles = new Dictionary<string, JsonNode>(StringComparer.OrdinalIgnoreCase);
		long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		List<string> idsToFetch = new List<string>();
		foreach (string accountId in accountIds)
		{
			if (AccountProfileCache.TryGetValue(accountId, out CachedAccountProfile value) && now - value.FetchedAt < AccountProfileCacheTtlMs)
			{
				profiles[accountId] = value.Profile;
			}
			else
			{
				idsToFetch.Add(accountId);
			}
		}
		for (int offset = 0; offset < idsToFetch.Count; offset += 100)
		{
			List<string> batch = idsToFetch.GetRange(offset, Math.Min(100, idsToFetch.Count - offset));
			Dictionary<string, JsonNode> dictionary = await FetchPublicAccountProfileBatchAsync(batch, accessToken);
			if (dictionary == null)
			{
				continue;
			}
			foreach (string item in batch)
			{
				JsonNode profile = (profiles[item] = (dictionary.TryGetValue(item, out var value2) ? value2 : null));
				AccountProfileCache[item] = new CachedAccountProfile(profile, now);
			}
		}
		return profiles;
	}

	private static string ResolveAvatarUrl(string? loadoutAvatarUrl, JsonNode? profile)
	{
		if (!string.IsNullOrWhiteSpace(loadoutAvatarUrl))
		{
			return loadoutAvatarUrl;
		}
		string text = GetString(profile, "avatarUrl", "avatar_url", "picture", "avatar");
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return LoadoutService.AvatarUrlForCharacter(null);
	}

	private static async Task<Dictionary<string, string>> FetchFriendAvatarsAsync(IEnumerable<string> accountIds, string accessToken)
	{
		Dictionary<string, string> avatars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		List<string> idsToFetch = new List<string>();
		foreach (string accountId in accountIds)
		{
			if (FriendAvatarCache.TryGetValue(accountId, out CachedFriendAvatar value) && now - value.FetchedAt < FriendAvatarCacheTtlMs)
			{
				if (!string.IsNullOrEmpty(value.AvatarUrl))
				{
					avatars[accountId] = value.AvatarUrl;
				}
			}
			else
			{
				idsToFetch.Add(accountId);
			}
		}
		if (idsToFetch.Count == 0)
		{
			return avatars;
		}
		Dictionary<string, string> dictionary = await App.LoadoutService.GetAvatarUrlsAsync(idsToFetch, accessToken);
		foreach (string item in idsToFetch)
		{
			string text = (dictionary.TryGetValue(item, out var value2) ? value2 : string.Empty);
			FriendAvatarCache[item] = new CachedFriendAvatar(text, now);
			if (!string.IsNullOrEmpty(text))
			{
				avatars[item] = text;
			}
		}
		return avatars;
	}

	private static object CreateFriendViewModel(JsonNode? entry, JsonNode? profile, IDictionary<string, string> nicknames, string currentAccountId, string? loadoutAvatarUrl = null)
	{
		string friendAccountId = GetFriendAccountId(entry, currentAccountId);
		string fallback = GetString(entry, "displayName", "display_name", "username", "name") ?? friendAccountId;
		string displayName = GetDisplayName(profile, fallback);
		nicknames.TryGetValue(friendAccountId, out string value);
		string relationshipBucket = GetRelationshipBucket(entry);
		string text = (GetString(entry, "presence", "statusText") ?? "").ToLowerInvariant();
		bool flag = GetBool(entry, "online") || text == "online";
		string text2 = GetString(entry, "gameStatus", "statusMessage", "activityText", "activity") ?? GetString(GetObject(entry, "status"), "gameStatus", "activity", "statusMessage", "launcherActivity");
		return new
		{
			id = (string.IsNullOrWhiteSpace(friendAccountId) ? (displayName + "-" + relationshipBucket) : friendAccountId),
			accountId = friendAccountId,
			username = displayName,
			nickname = (value ?? GetString(entry, "alias") ?? ""),
			status = (flag ? "online" : "offline"),
			gameStatus = (flag ? (text2 ?? "In Launcher") : "Offline"),
			level = (GetString(entry, "level") ?? ""),
			avatarUrl = ResolveAvatarUrl(loadoutAvatarUrl, profile),
			relationship = relationshipBucket,
			raw = entry
		};
	}

	private static bool GetBool(JsonNode? node, params string[] keys)
	{
		if (!(node is JsonObject jsonObject))
		{
			return false;
		}
		foreach (string propertyName in keys)
		{
			if (jsonObject.TryGetPropertyValue(propertyName, out JsonNode jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var value))
			{
				return value;
			}
		}
		return false;
	}

	private static string? GetStringFromAnonymous(object model, string propertyName)
	{
		return model.GetType().GetProperty(propertyName)?.GetValue(model)?.ToString();
	}

	private static async Task<object?> LaunchBuildAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("version") ?? "";
		string text2 = ctx.GetString("installPath") ?? "";
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new
			{
				isSuccess = false,
				error = "Build path is missing."
			};
		}
		if (!GameVersion.TryParse(text.StartsWith("++Fortnite", StringComparison.OrdinalIgnoreCase) ? text : ("++Fortnite+Release-" + text), out GameVersion result) || (object)result == null)
		{
			return new
			{
				isSuccess = false,
				error = "Unrecognised build version: " + text
			};
		}
		InstalledVersion installed = new InstalledVersion
		{
			Version = result,
			Path = text2
		};
		string modifiers = CombineLaunchArguments(EncodeGameModifiers(ctx), ctx.GetString("launchOptions"));
		try
		{
			await Task.Run(() => App.GameLauncher.LaunchAsync(installed, modifiers));
			return new
			{
				isSuccess = true
			};
		}
		catch (LaunchException ex)
		{
			var (title, error, hint) = DescribeLaunchFailure(ex.Code, ex.Message);
			return new
			{
				isSuccess = false,
				error = error,
				title = title,
				hint = hint,
				code = ex.Code.ToString()
			};
		}
		catch (Exception ex2)
		{
			return new
			{
				isSuccess = false,
				error = ex2.Message,
				title = "Launch Failed",
				hint = (string)null,
				code = "Unknown"
			};
		}
	}

	private static string? CombineLaunchArguments(string? modifiers, string? launchOptions)
	{
		string text = launchOptions?.Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return modifiers;
		}
		if (!string.IsNullOrWhiteSpace(modifiers))
		{
			return modifiers + " " + text;
		}
		return text;
	}

	private static string? EncodeGameModifiers(NeoBridgeContext ctx)
	{
		JsonElement? element = ctx.GetElement("modifiers");
		if (!element.HasValue || element.GetValueOrDefault().ValueKind != JsonValueKind.Object)
		{
			return null;
		}
		try
		{
			string rawText = element.Value.GetRawText();
			if (string.IsNullOrWhiteSpace(rawText) || rawText == "{}")
			{
				return null;
			}
			return "-NeoModifiers=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(rawText));
		}
		catch
		{
			return null;
		}
	}

	private static (string Title, string Message, string? Hint) DescribeLaunchFailure(LaunchErrorCode code, string rawMessage)
	{
		return code switch
		{
			LaunchErrorCode.Injection => (Title: "Couldn't finish launching", Message: "Neo started the game but couldn't attach the sign-in module.", Hint: "This is almost always antivirus (or Windows Defender) blocking Neo. Add an exclusion for the Neo launcher and your game folder, then relaunch. Running Neo as administrator can also help."), 
			LaunchErrorCode.ModuleInUse => (Title: "Sign-in module is in use", Message: "Neo's sign-in module is locked by another process.", Hint: "Close any running game or launcher instances (or restart your PC) and try again. This is almost always antivirus (or Windows Defender) blocking Neo. Add an exclusion for the Neo launcher and your game folder, then relaunch. Running Neo as administrator can also help."), 
			LaunchErrorCode.ModuleNotFound => (Title: "Sign-in module missing", Message: "Neo's sign-in module is missing from the install.", Hint: "Reinstall the Neo launcher. If it keeps happening, antivirus may be deleting the file — add an exclusion."), 
			LaunchErrorCode.ProcessExited => (Title: "Game closed during launch", Message: "The game closed right after starting.", Hint: "This is usually anti-cheat or antivirus. Verify your game files are intact, then add a Neo exclusion and try again."), 
			LaunchErrorCode.PrismAssetMissing => (Title: "Game client missing", Message: "Neo couldn't download the game client.", Hint: "Check your internet connection and try again. If it keeps failing, antivirus may be deleting the file — add an exclusion for the Neo launcher."), 
			LaunchErrorCode.BinaryNotFound => (Title: "Game files not found", Message: "Neo couldn't find the game in this build folder.", Hint: "Re-import or reinstall the build and pick the folder that contains the FortniteGame and Engine folders."), 
			LaunchErrorCode.Timeout => (Title: "Launch timed out", Message: "The game took too long to open.", Hint: "Close other heavy apps and try again. Slow drives or an active antivirus scan can cause this."), 
			LaunchErrorCode.ExchangeCodeFailed => (Title: "Sign-in failed", Message: "Neo couldn't get a launch token for your account.", Hint: "Log out and back in, then try launching again."), 
			LaunchErrorCode.ProcessFailed => IsLauncherElevated() ? (Title: "Close Neo and reopen it normally", Message: "Neo is running as administrator, which stops it from starting the game.", Hint: "The launch helper only configures the anti-cheat service when elevated, so it never starts the game. Quit Neo and reopen it without \"Run as administrator\".") : (Title: "Couldn't start the game", Message: "Windows refused to start the game process.", Hint: "Add an antivirus exclusion for the Neo launcher and your game folder, then try again."), 
			LaunchErrorCode.ServicesDown => (Title: "Neo services are down", Message: rawMessage, Hint: "The game can't be played while the servers are offline. Check the status in the launcher and try again once it reads Active."), 
			LaunchErrorCode.ServicesUnreachable => (Title: "Can't reach Neo services", Message: "Neo couldn't check whether the servers are online.", Hint: "Check your internet connection (or your VPN and firewall) and try again."), 
			LaunchErrorCode.AlreadyRunning => (Title: "Already running", Message: rawMessage, Hint: null), 
			LaunchErrorCode.Cancelled => (Title: "Launch cancelled", Message: "You stopped the launch.", Hint: null), 
			_ => (Title: "Launch Failed", Message: rawMessage, Hint: null), 
		};
	}

	private static bool IsLauncherElevated()
	{
		try
		{
			using WindowsIdentity ntIdentity = WindowsIdentity.GetCurrent();
			return new WindowsPrincipal(ntIdentity).IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch
		{
			return false;
		}
	}

	private static void TerminateGame()
	{
		string[] fortniteProcessNames = FortniteProcessNames;
		for (int i = 0; i < fortniteProcessNames.Length; i++)
		{
			Process[] processesByName = Process.GetProcessesByName(fortniteProcessNames[i]);
			foreach (Process process in processesByName)
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
			}
		}
	}

	private static bool IsGameRunning()
	{
		return App.GameLauncher.IsRunning;
	}

	public static object GameStateViewModel()
	{
		GameLauncher gameLauncher = App.GameLauncher;
		string text;
		if (gameLauncher.IsGameLaunching)
		{
			text = "launching";
		}
		else
		{
			text = ((!gameLauncher.IsRunning) ? "launch" : "running");
		}
		return new
		{
			state = text,
			identity = (gameLauncher.ActiveVersion?.Version.ToString() ?? ""),
			path = ((text == "running") ? gameLauncher.RunningBuildPath : ""),
			tracked = gameLauncher.IsGameActive
		};
	}

	public static void BroadcastGameState()
	{
		Broadcast("neo-game-state-changed", GameStateViewModel());
	}

	private static bool IsDeveloperAccount()
	{
		string text = App.AccountService.CurrentUser?.Id;
		if (!string.IsNullOrWhiteSpace(text))
		{
			return DeveloperAccountIds.Contains(text);
		}
		return false;
	}

	private static bool IsPublicBuild(string? version)
	{
		string raw = (version ?? "").Replace("++Fortnite+Release-", "").Trim();
		if (raw.Length == 0)
		{
			return false;
		}
		return PublicBuildVersions.Any((string v) => raw.Equals(v, StringComparison.OrdinalIgnoreCase) || raw.StartsWith(v + "-", StringComparison.OrdinalIgnoreCase));
	}

	private static string GetBuildSplashCacheKey(string splashPath)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(splashPath);
			return $"{splashPath}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
		}
		catch
		{
			return splashPath;
		}
	}

	private static object ListInstallDrives()
	{
		return new
		{
			drives = (from d in DriveInfo.GetDrives().Where((DriveInfo d) =>
				{
					try
					{
						return d.IsReady && d.DriveType == DriveType.Fixed;
					}
					catch
					{
						return false;
					}
				}).OrderByDescending((DriveInfo d) =>
				{
					try
					{
						return d.AvailableFreeSpace;
					}
					catch
					{
						return 0L;
					}
				})
				select new
				{
					name = d.Name,
					letter = ((d.Name.Length > 0) ? d.Name[0].ToString() : ""),
					label = (string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel),
					freeBytes = d.AvailableFreeSpace,
					totalBytes = d.TotalSize
				}).ToArray(),
			installFolder = "FortniteBuilds",
			bufferBytes = 16106127360L
		};
	}

	private async Task<object?> StartInstallAsync(NeoBridgeContext ctx)
	{
		JsonElement? element = ctx.GetElement("build");
		if (element.HasValue)
		{
			JsonElement valueOrDefault = element.GetValueOrDefault();
			if (valueOrDefault.ValueKind == JsonValueKind.Object)
			{
				BuildInfo buildInfo;
				try
				{
					buildInfo = valueOrDefault.Deserialize<BuildInfo>(JsonOptions);
				}
				catch (Exception ex)
				{
					return new
					{
						isSuccess = false,
						error = "Invalid build payload: " + ex.Message
					};
				}
				if (buildInfo == null)
				{
					return new
					{
						isSuccess = false,
						error = "Invalid build payload."
					};
				}
				string text = ctx.GetString("version") ?? buildInfo.Version.ToString();
				if (!IsDeveloperAccount() && !IsPublicBuild(text) && !IsPublicBuild(buildInfo.Version.ToString()))
				{
					return new
					{
						isSuccess = false,
						error = "This build is currently unavailable on this account."
					};
				}
				_installVersionTag = text;
				_installGameVersion = buildInfo.Version;
				string text2 = ctx.GetString("installRoot");
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = App.SettingsService.Settings.DownloadFolder;
				}
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = Path.Combine(Config.DataPath, "Builds");
				}
				Directory.CreateDirectory(text2);
				if (buildInfo.FileSizeBytes > 0)
				{
					try
					{
						DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(text2)));
						if (driveInfo.AvailableFreeSpace < buildInfo.FileSizeBytes)
						{
							double value = (double)buildInfo.FileSizeBytes / 1000000000.0;
							double value2 = (double)driveInfo.AvailableFreeSpace / 1000000000.0;
							return new
							{
								isSuccess = false,
								error = $"Not enough space on {driveInfo.Name} — needs {value:F1} GB, only {value2:F1} GB free. Choose another drive."
							};
						}
					}
					catch
					{
					}
				}
				try
				{
					bool flag = await App.InstallService.StartInstallAsync(buildInfo, text2);
					return new
					{
						isSuccess = flag,
						started = flag
					};
				}
				catch (Exception ex2)
				{
					return new
					{
						isSuccess = false,
						error = ex2.Message
					};
				}
			}
		}
		return new
		{
			isSuccess = false,
			error = "Missing build payload."
		};
	}

	private async Task<object?> VerifyBuildAsync(NeoBridgeContext ctx)
	{
		string identity = ctx.GetString("version") ?? ctx.GetString("identity") ?? "";
		string installPath = ctx.GetString("path") ?? "";
		if (string.IsNullOrWhiteSpace(installPath) && !string.IsNullOrWhiteSpace(identity))
		{
			try
			{
				installPath = App.LibraryService.Load().FirstOrDefault((InstalledVersion v) => NeoBuildMeta.Identity(v.Version) == identity)?.Path ?? "";
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath))
		{
			return new
			{
				isSuccess = false,
				error = "This build isn't installed, so there is nothing to verify."
			};
		}
		BuildInfo build = null;
		JsonElement? element = ctx.GetElement("build");
		if (element.HasValue)
		{
			JsonElement valueOrDefault = element.GetValueOrDefault();
			if (valueOrDefault.ValueKind == JsonValueKind.Object)
			{
				try
				{
					build = valueOrDefault.Deserialize<BuildInfo>(JsonOptions);
				}
				catch
				{
					build = null;
				}
			}
		}
		if (build == null)
		{
			try
			{
				build = (await App.LauncherService.GetBuildsAsync()).FirstOrDefault((BuildInfo b) => NeoBuildMeta.Identity(b.Version) == identity);
			}
			catch
			{
			}
		}
		if (build == null)
		{
			return new
			{
				isSuccess = false,
				error = "Couldn't find this build on the Neo service to verify against."
			};
		}
		_installVersionTag = (string.IsNullOrWhiteSpace(identity) ? build.Version.ToString() : identity);
		_installGameVersion = build.Version;
		try
		{
			bool flag = await App.InstallService.StartRepairAsync(build, installPath);
			if (!flag)
			{
				return new
				{
					isSuccess = false,
					error = "Another download or verify is already running."
				};
			}
			return new
			{
				isSuccess = true,
				started = flag,
				path = installPath
			};
		}
		catch (Exception ex)
		{
			return new
			{
				isSuccess = false,
				error = ex.Message
			};
		}
	}

	private static async Task<object?> GetBuildSplashAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("path") ?? "";
		string identity = ctx.GetString("version") ?? ctx.GetString("identity") ?? "";
		if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(identity))
		{
			try
			{
				text = App.LibraryService.Load().FirstOrDefault((InstalledVersion v) => NeoBuildMeta.Identity(v.Version) == identity)?.Path ?? "";
			}
			catch
			{
			}
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return new
			{
				dataUri = ""
			};
		}
		return new
		{
			dataUri = await TryReadBuildSplashDataUriAsync(text)
		};
	}

	private static async Task<object?> CheckDownloadServicesAsync(NeoBridgeContext ctx)
	{
		try
		{
			bool flag = (await App.LauncherService.GetDistributionPointsAsync()).Length != 0;
			return new
			{
				isSuccess = true,
				available = flag,
				status = (flag ? "ok" : "unavailable")
			};
		}
		catch (Exception ex)
		{
			return new
			{
				isSuccess = false,
				available = false,
				error = ex.Message
			};
		}
	}

	private async Task<object?> PickDownloadFolderAsync()
	{
		string text = await ShowFolderPickerAsync();
		if (text == null)
		{
			return new
			{
				path = (string)null
			};
		}
		App.SettingsService.Settings.DownloadFolder = text;
		App.SettingsService.Save();
		Broadcast("neo-settings-changed", SettingsViewModel());
		return new
		{
			path = text
		};
	}

	private async Task<object?> PickInstallFolderAsync()
	{
		string text = await ShowFolderPickerAsync();
		if (text == null)
		{
			return new
			{
				isSuccess = false,
				cancelled = true
			};
		}
		long freeBytes = 0L;
		long totalBytes = 0L;
		string label = "";
		try
		{
			text = Path.GetFullPath(text);
			DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(text));
			freeBytes = driveInfo.AvailableFreeSpace;
			totalBytes = driveInfo.TotalSize;
			label = (string.IsNullOrWhiteSpace(driveInfo.VolumeLabel) ? "Local Disk" : driveInfo.VolumeLabel);
		}
		catch
		{
		}
		return new
		{
			isSuccess = true,
			path = text,
			freeBytes = freeBytes,
			totalBytes = totalBytes,
			label = label
		};
	}

	private async Task<object?> PickBuildFolderAsync()
	{
		return await DescribePickedBuildFolderAsync((await ShowFolderPickerAsync()) ?? throw new OperationCanceledException("Build folder selection cancelled."));
	}

	private async Task<object?> DescribeBuildFolderPathAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("path") ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return new
			{
				isSuccess = false,
				error = "No folder selected."
			};
		}
		try
		{
			text = Path.GetFullPath(text);
		}
		catch (Exception ex)
		{
			return new
			{
				isSuccess = false,
				error = "Invalid folder path: " + ex.Message
			};
		}
		if (!Directory.Exists(text))
		{
			return new
			{
				isSuccess = false,
				error = "Selected folder does not exist."
			};
		}
		return await DescribePickedBuildFolderAsync(text);
	}

	private static async Task<JsonObject> DescribePickedBuildFolderAsync(string folder)
	{
		string root = folder;
		string version = "";
		string text = FindShippingBinary(folder);
		if (text != null)
		{
			InstalledVersion installedVersion = await BinaryVersionScanner.ScanAsync(text);
			if (installedVersion != null)
			{
				root = installedVersion.Path;
				version = installedVersion.Version.ToString();
			}
		}
		if (string.IsNullOrWhiteSpace(version))
		{
			version = InferBuildVersionFromPath(root);
		}
		JsonObject result = NeoBuildMeta.Describe(version, await IsLiveServiceBuildAsync(version));
		long installSizeBytes = await Task.Run(() => CalculateDirectorySizeBytes(root));
		string text2 = await TryReadBuildSplashDataUriAsync(root);
		result["isSuccess"] = true;
		result["path"] = root;
		result["version"] = version;
		result["installSizeBytes"] = installSizeBytes;
		result["installSize"] = FormatInstallSize(installSizeBytes);
		if (!string.IsNullOrWhiteSpace(text2))
		{
			result["splashDataUri"] = text2;
		}
		return result;
	}

	private static async Task<object?> DescribeBuildAsync(NeoBridgeContext ctx)
	{
		string version = ctx.GetString("version") ?? ctx.GetString("identity") ?? "";
		JsonObject jsonObject = NeoBuildMeta.Describe(version, await IsLiveServiceBuildAsync(version));
		jsonObject["isSuccess"] = true;
		return jsonObject;
	}

	private static async Task<bool> IsLiveServiceBuildAsync(string version)
	{
		GameVersion scanned = NeoBuildMeta.Parse(version);
		if ((object)scanned == null)
		{
			return false;
		}
		try
		{
			foreach (BuildInfo item in await App.LauncherService.GetBuildsAsync())
			{
				if (item.IsLive && item.Version == scanned)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static object ListFolderRoots()
	{
		var entries = DriveInfo.GetDrives().Where((DriveInfo d) =>
		{
			try
			{
				return d.IsReady;
			}
			catch
			{
				return false;
			}
		}).OrderBy((DriveInfo d) => d.Name, StringComparer.OrdinalIgnoreCase)
			.Select((DriveInfo d) =>
			{
				string label;
				try
				{
					label = (string.IsNullOrWhiteSpace(d.VolumeLabel) ? "Local Disk" : d.VolumeLabel);
				}
				catch
				{
					label = "Local Disk";
				}
				long freeBytes = 0L;
				long totalBytes = 0L;
				try
				{
					freeBytes = d.AvailableFreeSpace;
					totalBytes = d.TotalSize;
				}
				catch
				{
				}
				return new
				{
					name = d.Name,
					label = label,
					path = d.RootDirectory.FullName,
					kind = "drive",
					freeBytes = freeBytes,
					totalBytes = totalBytes
				};
			})
			.ToArray();
		return new
		{
			isSuccess = true,
			path = "",
			parent = (string)null,
			entries = entries
		};
	}

	private static Task<object?> ListFolderChildrenAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("path") ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return Task.FromResult(ListFolderRoots());
		}
		try
		{
			text = Path.GetFullPath(text);
		}
		catch (Exception ex)
		{
			return Task.FromResult((object)new
			{
				isSuccess = false,
				error = "Invalid path: " + ex.Message,
				path = text
			});
		}
		if (!Directory.Exists(text))
		{
			return Task.FromResult((object)new
			{
				isSuccess = false,
				error = "Folder does not exist.",
				path = text
			});
		}
		object[] array = Array.Empty<object>();
		try
		{
			array = (from entry in Directory.EnumerateDirectories(text).Select((string folder) =>
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(folder);
					bool hidden = (directoryInfo.Attributes & FileAttributes.Hidden) != 0;
					bool system = (directoryInfo.Attributes & FileAttributes.System) != 0;
					return new
					{
						name = directoryInfo.Name,
						label = directoryInfo.Name,
						path = directoryInfo.FullName,
						kind = "folder",
						hidden = hidden,
						system = system
					};
				})
				where !entry.system
				select entry).OrderBy(entry => entry.name, StringComparer.OrdinalIgnoreCase).Take(500).Cast<object>()
				.ToArray();
		}
		catch (Exception ex2)
		{
			return Task.FromResult((object)new
			{
				isSuccess = false,
				error = "Cannot read this folder: " + ex2.Message,
				path = text
			});
		}
		string parent = null;
		try
		{
			parent = Directory.GetParent(text)?.FullName;
		}
		catch
		{
		}
		return Task.FromResult((object)new
		{
			isSuccess = true,
			path = text,
			parent = parent,
			entries = array
		});
	}

	private Task<string?> ShowFolderPickerAsync()
	{
		if (Interlocked.CompareExchange(ref _folderPickerOpen, 1, 0) != 0)
		{
			return Task.FromResult<string>(null);
		}
		TaskCompletionSource<string?> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		if (!_dispatcher.TryEnqueue(() =>
		{
			try
			{
				tcs.TrySetResult(ShowExplorerFolderPickerDialog());
			}
			catch (Exception exception)
			{
				tcs.TrySetException(exception);
			}
			finally
			{
				Volatile.Write(ref _folderPickerOpen, 0);
			}
		}))
		{
			Volatile.Write(ref _folderPickerOpen, 0);
			tcs.TrySetException(new InvalidOperationException("Unable to open folder picker."));
		}
		return tcs.Task;
	}

	private string? ShowExplorerFolderPickerDialog()
	{
		nint windowHandle = WindowNative.GetWindowHandle(_window);
		try
		{
			IFileOpenDialog fileOpenDialog = (IFileOpenDialog)(object)new FileOpenDialog();
			fileOpenDialog.GetOptions(out var pfos);
			fileOpenDialog.SetOptions(pfos | 0x20 | 0x40 | 0x800 | 8);
			fileOpenDialog.SetTitle("Select your build folder");
			fileOpenDialog.SetOkButtonLabel("Select Folder");
			int num = fileOpenDialog.Show(windowHandle);
			if (num == -2147023673)
			{
				return null;
			}
			Marshal.ThrowExceptionForHR(num);
			fileOpenDialog.GetResult(out IShellItem ppsi);
			ppsi.GetDisplayName(2147844096u, out var ppszName);
			try
			{
				return Marshal.PtrToStringUni(ppszName);
			}
			finally
			{
				if (ppszName != IntPtr.Zero)
				{
					CoTaskMemFree(ppszName);
				}
			}
		}
		catch
		{
			return ShowClassicFolderPickerDialog(windowHandle);
		}
	}

	private static string? ShowClassicFolderPickerDialog(nint hwnd)
	{
		BROWSEINFO lpbi = new BROWSEINFO
		{
			hwndOwner = hwnd,
			pidlRoot = IntPtr.Zero,
			pszDisplayName = IntPtr.Zero,
			lpszTitle = "Select your build folder",
			ulFlags = 113u,
			lpfn = IntPtr.Zero,
			lParam = IntPtr.Zero,
			iImage = 0
		};
		nint num = IntPtr.Zero;
		try
		{
			num = SHBrowseForFolder(ref lpbi);
			if (num == IntPtr.Zero)
			{
				return null;
			}
			StringBuilder stringBuilder = new StringBuilder(32768);
			return SHGetPathFromIDList(num, stringBuilder) ? stringBuilder.ToString() : null;
		}
		finally
		{
			if (num != IntPtr.Zero)
			{
				CoTaskMemFree(num);
			}
		}
	}

	private static string InferBuildVersionFromPath(string folder)
	{
		string text = folder;
		for (int i = 0; i < 3; i++)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				break;
			}
			Match match = Regex.Match(Path.GetFileName(text.TrimEnd(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			})) ?? "", "(?<!\\d)(?<major>\\d{1,2})(?:[._-](?<minor>\\d{1,2}))?(?!\\d)");
			if (match.Success && int.TryParse(match.Groups["major"].Value, out var result) && result > 0 && int.TryParse(match.Groups["minor"].Success ? match.Groups["minor"].Value : "00", out var result2))
			{
				return $"{result}.{result2}";
			}
			try
			{
				text = Directory.GetParent(text)?.FullName ?? "";
			}
			catch
			{
				break;
			}
		}
		return "";
	}

	private static async Task<string> TryReadBuildSplashDataUriAsync(string root)
	{
		if (string.IsNullOrWhiteSpace(root))
		{
			return "";
		}
		try
		{
			InlineArray5<string> buffer = default;
			buffer[0] = root;
			buffer[1] = "FortniteGame";
			buffer[2] = "Content";
			buffer[3] = "Splash";
			buffer[4] = "Splash.bmp";
			string text = Path.Combine(buffer);
			if (!File.Exists(text))
			{
				return "";
			}
			string cacheKey = GetBuildSplashCacheKey(text);
			if (BuildSplashDataUriCache.TryGetValue(cacheKey, out string value))
			{
				return value;
			}
			byte[] bytes = await File.ReadAllBytesAsync(text);
			byte[] array = await TryTranscodeSplashToJpegAsync(bytes);
			string text2 = ((array != null) ? ("data:image/jpeg;base64," + Convert.ToBase64String(array)) : ("data:image/bmp;base64," + Convert.ToBase64String(bytes)));
			BuildSplashDataUriCache[cacheKey] = text2;
			return text2;
		}
		catch
		{
			return "";
		}
	}

	private static async Task<byte[]?> TryTranscodeSplashToJpegAsync(byte[] source)
	{
		_ = 6;
		try
		{
			using InMemoryRandomAccessStream input = new InMemoryRandomAccessStream();
			using (DataWriter writer = new DataWriter(input))
			{
				writer.WriteBytes(source);
				await writer.StoreAsync();
				await writer.FlushAsync();
				writer.DetachStream();
			}
			input.Seek(0uL);
			BitmapDecoder bitmapDecoder = await BitmapDecoder.CreateAsync(input);
			uint num = bitmapDecoder.PixelWidth;
			uint num2 = bitmapDecoder.PixelHeight;
			if (num == 0 || num2 == 0)
			{
				return null;
			}
			uint num3 = Math.Max(num, num2);
			if (num3 > 1024)
			{
				double num4 = 1024.0 / (double)num3;
				num = Math.Max(1u, (uint)Math.Round((double)num * num4));
				num2 = Math.Max(1u, (uint)Math.Round((double)num2 * num4));
			}
			using SoftwareBitmap bitmap = await bitmapDecoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, new BitmapTransform
			{
				ScaledWidth = num,
				ScaledHeight = num2,
				InterpolationMode = BitmapInterpolationMode.Fant
			}, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
			using InMemoryRandomAccessStream output = new InMemoryRandomAccessStream();
			BitmapPropertySet encodingOptions = new BitmapPropertySet { ["ImageQuality"] = new BitmapTypedValue(0.85f, PropertyType.Single) };
			BitmapEncoder bitmapEncoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output, encodingOptions);
			bitmapEncoder.SetSoftwareBitmap(bitmap);
			await bitmapEncoder.FlushAsync();
			byte[] encoded = new byte[output.Size];
			output.Seek(0uL);
			using (DataReader reader = new DataReader(output))
			{
				await reader.LoadAsync((uint)output.Size);
				reader.ReadBytes(encoded);
			}
			return encoded;
		}
		catch
		{
			return null;
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	private static extern nint SHBrowseForFolder(ref BROWSEINFO lpbi);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SHGetPathFromIDList(nint pidl, StringBuilder pszPath);

	[DllImport("ole32.dll")]
	private static extern void CoTaskMemFree(nint pv);

	private static string? FindShippingBinary(string folder)
	{
		string path = Path.Combine("FortniteGame", "Binaries", "Win64", "FortniteClient-Win64-Shipping.exe");
		string text = Path.Combine(folder, path);
		if (File.Exists(text))
		{
			return text;
		}
		try
		{
			foreach (string item in Directory.EnumerateDirectories(folder))
			{
				string text2 = Path.Combine(item, path);
				if (File.Exists(text2))
				{
					return text2;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static long CalculateDirectorySizeBytes(string folder)
	{
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
		{
			return 0L;
		}
		try
		{
			long num = 0L;
			Stack<string> stack = new Stack<string>();
			stack.Push(folder);
			while (stack.Count > 0)
			{
				string path = stack.Pop();
				try
				{
					foreach (string item in Directory.EnumerateFiles(path))
					{
						try
						{
							num += new FileInfo(item).Length;
						}
						catch
						{
						}
					}
					foreach (string item2 in Directory.EnumerateDirectories(path))
					{
						stack.Push(item2);
					}
				}
				catch
				{
				}
			}
			return num;
		}
		catch
		{
			return 0L;
		}
	}

	private static string FormatInstallSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "";
		}
		double num = (double)bytes / 1000.0 / 1000.0 / 1000.0;
		if (num >= 1.0)
		{
			return $"{num:F2} GB";
		}
		return $"{(double)bytes / 1000.0 / 1000.0:F1} MB";
	}

	private void OnInstallProgress(InstallProgress progress)
	{
		DispatchEvent("neo-build-download-progress", new
		{
			version = _installVersionTag,
			versionIdentity = _installVersionTag,
			status = MapInstallStatus(progress),
			phase = progress.StatusText,
			progress = Math.Round((double)progress.Progress * 100.0, 1),
			downloaded = progress.Downloaded,
			total = progress.TotalRequired,
			speed = progress.SpeedText,
			eta = progress.EtaText,
			paused = progress.IsPaused
		});
	}

	private void OnInstallCompleted(InstallResult result)
	{
		if (result.Success)
		{
			try
			{
				if ((object)_installGameVersion != null)
				{
					RecordLibraryEntry(_installGameVersion, result.InstallPath, "downloaded", 0L);
					BroadcastLibraryChanged();
				}
			}
			catch
			{
			}
			DispatchEvent("neo-build-installed", new
			{
				version = _installVersionTag,
				versionIdentity = _installVersionTag,
				fullVersion = result.Version,
				path = result.InstallPath
			});
		}
		else
		{
			NeoWebBridge neoWebBridge = this;
			string installVersionTag = _installVersionTag;
			string installVersionTag2 = _installVersionTag;
			string status = (result.Cancelled ? "cancelled" : "failed");
			string error;
			if (result.Cancelled)
			{
				error = "";
			}
			else
			{
				error = (string.IsNullOrEmpty(result.ErrorText) ? result.ErrorCode : result.ErrorText);
			}
			neoWebBridge.DispatchEvent("neo-build-download-progress", new
			{
				version = installVersionTag,
				versionIdentity = installVersionTag2,
				status = status,
				error = error
			});
		}
	}

	private static string MapInstallStatus(InstallProgress progress)
	{
		if (progress.IsPaused)
		{
			return "paused";
		}
		switch (progress.State)
		{
		case EBuildPatchState.Installing:
		case EBuildPatchState.MovingToInstall:
		case EBuildPatchState.SettingAttributes:
			return "installing";
		case EBuildPatchState.BuildVerification:
			return "verifying";
		case EBuildPatchState.CleanUp:
		case EBuildPatchState.PrerequisitesInstall:
			return "finalizing";
		case EBuildPatchState.Completed:
			return "completed";
		default:
			return "downloading";
		}
	}

	private static async Task<object?> GetLibraryAsync(NeoBridgeContext ctx)
	{
		HashSet<InstalledVersion> entries = LoadLibrarySafe();
		JsonArray builds = new JsonArray();
		HashSet<int> liveCLs = new HashSet<int>();
		try
		{
			List<BuildInfo> list = App.LauncherService.Builds;
			if (list.Count == 0)
			{
				list = await App.LauncherService.GetBuildsAsync();
			}
			foreach (BuildInfo item in list)
			{
				if (item.IsLive)
				{
					liveCLs.Add(item.Version.CL);
				}
			}
		}
		catch
		{
		}
		NeoLog.Info("library", $"getLibrary: {entries.Count} entries, {liveCLs.Count} live service build(s) [{string.Join(",", liveCLs)}]; entry CLs [{string.Join(",", entries.Select((InstalledVersion e) => e.Version.CL))}]");
		foreach (InstalledVersion item2 in entries.OrderBy((InstalledVersion e) => e.Version.CL))
		{
			string text = NeoBuildMeta.Identity(item2.Version);
			bool flag = !string.IsNullOrWhiteSpace(item2.Path) && Directory.Exists(item2.Path);
			bool flag2 = liveCLs.Contains(item2.Version.CL);
			JsonObject jsonObject = NeoBuildMeta.Describe(item2.Version.ToString(), flag);
			jsonObject["path"] = item2.Path;
			jsonObject["source"] = (string.IsNullOrWhiteSpace(item2.Source) ? "installed" : item2.Source);
			jsonObject["installedAt"] = item2.InstalledAt;
			jsonObject["installSizeBytes"] = item2.InstallSizeBytes;
			jsonObject["installSize"] = FormatInstallSize(item2.InstallSizeBytes);
			jsonObject["installed"] = flag;
			jsonObject["isLiveBuild"] = flag2;
			jsonObject["isLive"] = flag2;
			jsonObject["versionIdentity"] = text;
			jsonObject["version"] = text;
			builds.Add(jsonObject);
		}
		return new { builds };
	}

	private static async Task<object?> ImportBuildAsync(NeoBridgeContext ctx)
	{
		string path = ctx.GetString("path") ?? "";
		if (string.IsNullOrWhiteSpace(path))
		{
			return new
			{
				isSuccess = false,
				error = "No folder selected."
			};
		}
		try
		{
			path = Path.GetFullPath(path);
		}
		catch (Exception ex)
		{
			return new
			{
				isSuccess = false,
				error = "Invalid folder path: " + ex.Message
			};
		}
		if (!Directory.Exists(path))
		{
			return new
			{
				isSuccess = false,
				error = "Selected folder does not exist."
			};
		}
		JsonObject jsonObject = await DescribePickedBuildFolderAsync(path);
		string raw = jsonObject["version"]?.GetValue<string>() ?? "";
		string text = jsonObject["path"]?.GetValue<string>() ?? path;
		long sizeBytes = jsonObject["installSizeBytes"]?.GetValue<long>() ?? 0;
		string text2 = ctx.GetString("source") ?? "imported";
		GameVersion gameVersion = NeoBuildMeta.Parse(raw);
		if ((object)gameVersion == null)
		{
			return new
			{
				isSuccess = false,
				error = "Could not determine a build version for " + text + "."
			};
		}
		RecordLibraryEntry(gameVersion, text, text2, sizeBytes);
		BroadcastLibraryChanged();
		jsonObject["isSuccess"] = true;
		jsonObject["source"] = text2;
		return jsonObject;
	}

	private static Task<object?> ForgetBuildAsync(NeoBridgeContext ctx)
	{
		string identity = ctx.GetString("identity") ?? ctx.GetString("versionIdentity") ?? "";
		if (string.IsNullOrWhiteSpace(identity))
		{
			return Task.FromResult((object)new
			{
				isSuccess = false,
				error = "No build identity supplied."
			});
		}
		try
		{
			HashSet<InstalledVersion> hashSet = LoadLibrarySafe();
			int num = hashSet.RemoveWhere((InstalledVersion v) => NeoBuildMeta.Identity(v.Version) == identity);
			if (num > 0)
			{
				App.LibraryService.Save(hashSet);
				BroadcastLibraryChanged();
			}
			return Task.FromResult((object)new
			{
				isSuccess = true,
				removed = num
			});
		}
		catch (Exception ex)
		{
			return Task.FromResult((object)new
			{
				isSuccess = false,
				error = ex.Message
			});
		}
	}

	private static Task<object?> MigrateLibraryAsync(NeoBridgeContext ctx)
	{
		JsonElement? element = ctx.GetElement("builds");
		if (element.HasValue)
		{
			JsonElement valueOrDefault = element.GetValueOrDefault();
			if (valueOrDefault.ValueKind == JsonValueKind.Array)
			{
				HashSet<InstalledVersion> hashSet = LoadLibrarySafe();
				HashSet<string> hashSet2 = hashSet.Select((InstalledVersion v) => NeoBuildMeta.Identity(v.Version)).ToHashSet(StringComparer.OrdinalIgnoreCase);
				int num = 0;
				foreach (JsonElement item in valueOrDefault.EnumerateArray())
				{
					if (item.ValueKind != JsonValueKind.Object)
					{
						continue;
					}
					string text = ReadString(item, "versionIdentity", "version", "build", "id");
					string text2 = ReadString(item, "path", "installPath", "launchPath");
					if (!string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(text2))
					{
						GameVersion gameVersion = NeoBuildMeta.Parse(text);
						if ((object)gameVersion != null && hashSet2.Add(NeoBuildMeta.Identity(gameVersion)))
						{
							long installSizeBytes = ((item.TryGetProperty("installSizeBytes", out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var value2)) ? value2 : 0);
							InstalledVersion installedVersion = new InstalledVersion
							{
								Version = gameVersion,
								Path = text2
							};
							string text3 = ReadString(item, "source");
							installedVersion.Source = ((text3 != null && text3.Length > 0) ? text3 : "imported");
							installedVersion.InstalledAt = ReadString(item, "installedAt");
							installedVersion.InstallSizeBytes = installSizeBytes;
							hashSet.Add(installedVersion);
							num++;
						}
					}
				}
				if (num > 0)
				{
					App.LibraryService.Save(hashSet);
					BroadcastLibraryChanged();
				}
				return Task.FromResult((object)new
				{
					isSuccess = true,
					imported = num
				});
			}
		}
		return Task.FromResult((object)new
		{
			isSuccess = true,
			imported = 0
		});
	}

	internal static void RecordLibraryEntry(GameVersion version, string path, string source, long sizeBytes)
	{
		try
		{
			HashSet<InstalledVersion> hashSet = LoadLibrarySafe();
			InstalledVersion installedVersion = hashSet.FirstOrDefault((InstalledVersion v) => v.Version.CL == version.CL);
			hashSet.RemoveWhere((InstalledVersion v) => v.Version.CL == version.CL);
			hashSet.Add(new InstalledVersion
			{
				Version = version,
				Path = path,
				Source = source,
				InstalledAt = (string.IsNullOrWhiteSpace(installedVersion?.InstalledAt) ? DateTime.UtcNow.ToString("o") : installedVersion.InstalledAt),
				InstallSizeBytes = ((sizeBytes > 0) ? sizeBytes : (installedVersion?.InstallSizeBytes ?? 0))
			});
			App.LibraryService.Save(hashSet);
		}
		catch (Exception ex)
		{
			NeoLog.Warn("library", "Failed to record library entry for " + path + ": " + ex.Message);
		}
	}

	internal static void BroadcastLibraryChanged()
	{
		Broadcast("neo-library-changed", null);
	}

	private static HashSet<InstalledVersion> LoadLibrarySafe()
	{
		try
		{
			return App.LibraryService.Load();
		}
		catch
		{
			return new HashSet<InstalledVersion>();
		}
	}

	private static string ReadString(JsonElement element, params string[] names)
	{
		foreach (string propertyName in names)
		{
			if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
			{
				string text = value.GetString() ?? "";
				if (!string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
		}
		return "";
	}

	public static void OpenMessageThread(string accountId)
	{
		accountId = (accountId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(accountId))
		{
			Interlocked.Exchange(ref _pendingMessageThreadAccountId, accountId);
			try
			{
				NeoChildWindows.OpenFriends();
			}
			catch
			{
			}
			Broadcast("neo-open-message-thread", new { accountId });
		}
	}

	private static Task<object?> FetchNeoMessagesAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("accountId");
		lock (ConversationsLock)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				return Task.FromResult((object)new
				{
					conversations = new ConversationView[1] { ConversationViewModel(text) }
				});
			}
			return Task.FromResult((object)new
			{
				conversations = (from c in Conversations.Keys.Select(ConversationViewModel)
					orderby c.lastAt descending
					select c).ToArray()
			});
		}
	}

	private static async Task<object?> SendNeoMessageAsync(NeoBridgeContext ctx)
	{
		string toAccountId = (ctx.GetString("accountId") ?? "").Trim();
		string body = (ctx.GetString("body") ?? "").Trim();
		if (string.IsNullOrWhiteSpace(toAccountId) || string.IsNullOrWhiteSpace(body))
		{
			return new
			{
				ok = false,
				error = "missing-target-or-body"
			};
		}
		string myAccountId = App.AccountService.CurrentUser?.Id ?? "";
		if (string.IsNullOrWhiteSpace(myAccountId))
		{
			return new
			{
				ok = false,
				error = "missing-account"
			};
		}
		if (!App.PresenceService.IsConnected)
		{
			try
			{
				string accessToken = await App.AccountService.GetAccessTokenAsync();
				await App.PresenceService.EnsureConnectedAsync(myAccountId, accessToken);
			}
			catch
			{
			}
		}
		string messageId = $"neo_{Guid.NewGuid():N}";
		if (!(await App.PresenceService.SendChatMessageAsync(toAccountId, body, messageId)))
		{
			return new
			{
				ok = false,
				error = "not-connected"
			};
		}
		ChatMessage message = new ChatMessage(messageId, toAccountId, "outgoing", body, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
		AppendMessage(toAccountId, message, incrementUnread: false);
		BroadcastConversation(toAccountId, message);
		return new
		{
			ok = true,
			message = MessageViewModel(message)
		};
	}

	private static Task<object?> MarkNeoMessagesReadAsync(NeoBridgeContext ctx)
	{
		string text = (ctx.GetString("accountId") ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			ConversationUnread[text] = 0;
			Broadcast("neo-friend-message-read", new
			{
				accountId = text
			});
		}
		return Task.FromResult((object)new
		{
			ok = true
		});
	}

	public static void IngestIncomingChatMessage(string fromAccountId, string body, long receivedAt, string stanzaId)
	{
		if (!string.IsNullOrWhiteSpace(fromAccountId) && !string.IsNullOrWhiteSpace(body))
		{
			ChatMessage message = new ChatMessage(string.IsNullOrWhiteSpace(stanzaId) ? $"in_{Guid.NewGuid():N}" : stanzaId, fromAccountId, "incoming", body, (receivedAt > 0) ? receivedAt : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
			if (AppendMessage(fromAccountId, message, incrementUnread: true))
			{
				BroadcastConversation(fromAccountId, message);
			}
		}
	}

	private static bool AppendMessage(string accountId, ChatMessage message, bool incrementUnread)
	{
		lock (ConversationsLock)
		{
			List<ChatMessage> orAdd = Conversations.GetOrAdd(accountId, (string _) => new List<ChatMessage>());
			if (orAdd.Any((ChatMessage m) => string.Equals(m.Id, message.Id, StringComparison.Ordinal)))
			{
				return false;
			}
			orAdd.Add(message);
			if (orAdd.Count > 200)
			{
				orAdd.RemoveRange(0, orAdd.Count - 200);
			}
			if (incrementUnread)
			{
				ConversationUnread[accountId] = ((!ConversationUnread.TryGetValue(accountId, out var value)) ? 1 : (value + 1));
			}
		}
		return true;
	}

	private static void BroadcastConversation(string accountId, ChatMessage message)
	{
		Broadcast("neo-friend-message", new
		{
			accountId = accountId,
			message = MessageViewModel(message),
			unread = (ConversationUnread.TryGetValue(accountId, out var value) ? value : 0)
		});
	}

	private static object MessageViewModel(ChatMessage message)
	{
		return new
		{
			id = message.Id,
			accountId = message.AccountId,
			direction = message.Direction,
			body = message.Body,
			sentAt = message.SentAt
		};
	}

	private static ConversationView ConversationViewModel(string accountId)
	{
		object[] messages = (Conversations.TryGetValue(accountId, out List<ChatMessage> value) ? value.Select(MessageViewModel).ToArray() : Array.Empty<object>());
		long num;
		if (!Conversations.TryGetValue(accountId, out List<ChatMessage> value2) || value2.Count <= 0)
		{
			num = 0L;
		}
		else
		{
			List<ChatMessage> list = value2;
			num = list[list.Count - 1].SentAt;
		}
		long lastAt = num;
		int value3;
		return new ConversationView(accountId, messages, ConversationUnread.TryGetValue(accountId, out value3) ? value3 : 0, lastAt);
	}

	private Task<object?> SetNotificationSeenIdsAsync(NeoBridgeContext ctx)
	{
		JsonElement? element = ctx.GetElement("ids");
		List<string> list = (element.HasValue ? (element.Value.Deserialize<List<string>>(JsonOptions) ?? new List<string>()) : new List<string>());
		App.SettingsService.Settings.NotificationSeenIds = list;
		App.SettingsService.Save();
		Broadcast("neo-notifications-changed", new
		{
			seenIds = list
		});
		return Task.FromResult((object)new
		{
			ok = true
		});
	}

	private static int NormalizeLauncherFps(double? value)
	{
		if ((int)Math.Round(value ?? ((double)App.SettingsService.Settings.LauncherFps)) > 30)
		{
			return 60;
		}
		return 30;
	}

	private static string NormalizeAfterLaunchAction(string? value)
	{
		string text = (value ?? App.SettingsService.Settings.AfterLaunchAction).Trim();
		if (!(text == "keepFriends"))
		{
			if (text == "nothing")
			{
				return "nothing";
			}
			return "close";
		}
		return "keepFriends";
	}

	private static void SaveAndBroadcastSettings()
	{
		App.SettingsService.Save();
		Broadcast("neo-settings-changed", SettingsViewModel());
		Broadcast("neo-launcher-settings-changed", new
		{
			launcherFps = App.SettingsService.Settings.LauncherFps,
			liteMode = App.SettingsService.Settings.LiteMode,
			openOnStartup = App.SettingsService.Settings.OpenOnStartup,
			afterLaunchAction = App.SettingsService.Settings.AfterLaunchAction
		});
		Broadcast("neo-launcher-fps-changed", new
		{
			launcherFps = App.SettingsService.Settings.LauncherFps
		});
	}

	private static object SettingsViewModel()
	{
		AppSettings settings = App.SettingsService.Settings;
		return new
		{
			downloadFolder = settings.DownloadFolder,
			profileStatusId = settings.ProfileStatusId,
			launcherFps = ((settings.LauncherFps <= 30) ? 30 : 60),
			liteMode = settings.LiteMode,
			openOnStartup = settings.OpenOnStartup,
			afterLaunchAction = NormalizeAfterLaunchAction(settings.AfterLaunchAction),
			theme = settings.Theme.ToString(),
			material = settings.Material.ToString(),
			language = settings.Language,
			sendDiagnostics = settings.SendDiagnostics
		};
	}

	private Task<object?> SetLauncherPreferencesAsync(NeoBridgeContext ctx)
	{
		AppSettings settings = App.SettingsService.Settings;
		settings.LauncherFps = NormalizeLauncherFps(ctx.GetNumber("launcherFps") ?? ctx.GetNumber("fps"));
		settings.LiteMode = ctx.GetBool("liteMode", settings.LiteMode);
		settings.OpenOnStartup = ctx.GetBool("openOnStartup", settings.OpenOnStartup);
		settings.AfterLaunchAction = NormalizeAfterLaunchAction(ctx.GetString("afterLaunchAction"));
		ApplyStartupRegistry(settings.OpenOnStartup);
		SaveAndBroadcastSettings();
		return Task.FromResult((object)new
		{
			ok = true,
			settings = SettingsViewModel()
		});
	}

	private Task<object?> SetLauncherFpsAsync(NeoBridgeContext ctx)
	{
		App.SettingsService.Settings.LauncherFps = NormalizeLauncherFps(ctx.GetNumber("launcherFps") ?? ctx.GetNumber("fps"));
		SaveAndBroadcastSettings();
		return Task.FromResult((object)new
		{
			ok = true,
			settings = SettingsViewModel()
		});
	}

	private Task<object?> SetOpenOnStartupAsync(NeoBridgeContext ctx)
	{
		bool flag = ctx.GetBool("openOnStartup", App.SettingsService.Settings.OpenOnStartup);
		App.SettingsService.Settings.OpenOnStartup = flag;
		ApplyStartupRegistry(flag);
		SaveAndBroadcastSettings();
		return Task.FromResult((object)new
		{
			ok = true,
			settings = SettingsViewModel()
		});
	}

	private Task<object?> SetAfterLaunchActionAsync(NeoBridgeContext ctx)
	{
		App.SettingsService.Settings.AfterLaunchAction = NormalizeAfterLaunchAction(ctx.GetString("afterLaunchAction"));
		SaveAndBroadcastSettings();
		return Task.FromResult((object)new
		{
			ok = true,
			settings = SettingsViewModel()
		});
	}

	private Task<object?> ApplyAfterLaunchActionAsync(NeoBridgeContext ctx)
	{
		string action = NormalizeAfterLaunchAction(App.SettingsService.Settings.AfterLaunchAction);
		App.DispatcherQueue?.TryEnqueue(() =>
		{
			if (action == "close")
			{
				try
				{
					NeoChildWindows.CloseFriends();
				}
				catch
				{
				}
				try
				{
					App.Window?.HideToTray();
					return;
				}
				catch
				{
					return;
				}
			}
			if (action == "keepFriends")
			{
				try
				{
					App.Window?.HideToTray();
				}
				catch
				{
				}
				try
				{
					NeoChildWindows.OpenFriends();
				}
				catch
				{
				}
			}
		});
		return Task.FromResult((object)new
		{
			ok = true,
			action = action
		});
	}

	private static void ApplyStartupRegistry(bool enabled)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true) ?? Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey == null)
			{
				return;
			}
			if (!enabled)
			{
				registryKey.DeleteValue("NeoLauncher", throwOnMissingValue: false);
				return;
			}
			string processPath = Environment.ProcessPath;
			if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
			{
				registryKey.SetValue("NeoLauncher", "\"" + processPath + "\" --startup-friends");
			}
		}
		catch
		{
		}
	}

	private static object AccountTierFallback(string reason)
	{
		return new
		{
			isMember = false,
			tiers = Array.Empty<string>(),
			ownedOfferIds = Array.Empty<string>(),
			highestTier = "Member",
			effectiveTier = "Member",
			isFounder = false,
			isBooster = false,
			hasCrew = false,
			hasCrewPlus = false,
			source = "unavailable:" + reason
		};
	}

	private static string EntitlementsUrl(string accountId)
	{
		return $"{"https"}://store.{"neofn.dev"}/api/v1/entitlements/{Uri.EscapeDataString(accountId)}";
	}

	private static async Task<object?> GetAccountTierAsync(NeoBridgeContext ctx)
	{
		string accountId = App.AccountService.CurrentUser?.Id;
		if (string.IsNullOrWhiteSpace(accountId))
		{
			return AccountTierFallback("signed-out");
		}
		string token;
		try
		{
			token = await App.AccountService.GetAccessTokenAsync();
		}
		catch
		{
			return AccountTierFallback("no-token");
		}
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, EntitlementsUrl(accountId));
			request.Headers.Authorization = new BearerAuthenticationValue(token);
			using HttpResponseMessage response = await StoreHttp.SendAsync(request);
			if (!response.IsSuccessStatusCode)
			{
				return AccountTierFallback(((int)response.StatusCode).ToString());
			}
			AccountEntitlementsDto accountEntitlementsDto = await response.Content.ReadFromJsonAsync<AccountEntitlementsDto>(EntitlementsJsonOptions);
			return (accountEntitlementsDto == null) ? AccountTierFallback("empty-body") : BuildAccountTier(ResolveOwnedOfferIds(accountEntitlementsDto));
		}
		catch
		{
			return AccountTierFallback("request-failed");
		}
	}

	private static string[] ResolveOwnedOfferIds(AccountEntitlementsDto entitlements)
	{
		if (entitlements.Orders == null && entitlements.Subscriptions == null)
		{
			return entitlements.OwnedOfferIds ?? Array.Empty<string>();
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		EntitlementOrderDto[] array = entitlements.Orders ?? Array.Empty<EntitlementOrderDto>();
		foreach (EntitlementOrderDto entitlementOrderDto in array)
		{
			if (!string.IsNullOrWhiteSpace(entitlementOrderDto.OfferId) && string.IsNullOrWhiteSpace(entitlementOrderDto.SubscriptionId) && entitlementOrderDto.Paid && !entitlementOrderDto.Refunded)
			{
				hashSet.Add(entitlementOrderDto.OfferId);
			}
		}
		EntitlementSubscriptionDto[] array2 = entitlements.Subscriptions ?? Array.Empty<EntitlementSubscriptionDto>();
		foreach (EntitlementSubscriptionDto entitlementSubscriptionDto in array2)
		{
			if (!string.IsNullOrWhiteSpace(entitlementSubscriptionDto.OfferId) && entitlementSubscriptionDto.Active)
			{
				hashSet.Add(entitlementSubscriptionDto.OfferId);
			}
		}
		return hashSet.ToArray();
	}

	private static object BuildAccountTier(string[] ownedOfferIds)
	{
		bool flag = Owns("crew-plus");
		bool flag2 = Owns("founders");
		bool flag3 = Owns("crew");
		List<string> list = new List<string>();
		if (flag)
		{
			list.Add("Crew+");
		}
		if (flag2)
		{
			list.Add("Founders");
		}
		if (flag3)
		{
			list.Add("Crew");
		}
		string text;
		if (flag)
		{
			text = "Crew+";
		}
		else if (flag2)
		{
			text = "Founders";
		}
		else
		{
			text = (flag3 ? "Crew" : "Member");
		}
		return new
		{
			isMember = (list.Count > 0),
			tiers = list.ToArray(),
			ownedOfferIds = ownedOfferIds,
			highestTier = text,
			effectiveTier = text,
			isFounder = flag2,
			isBooster = false,
			hasCrew = flag3,
			hasCrewPlus = flag,
			source = "store"
		};
		bool Owns(string offerId)
		{
			return ownedOfferIds.Any((string id) => string.Equals(id, offerId, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static async Task<object?> GetAccountStatusAsync(NeoBridgeContext ctx)
	{
		try
		{
			await App.LightswitchService.CheckStatusAsync();
		}
		catch
		{
		}
		bool isBanned = App.LightswitchService.IsBanned;
		string banMessage = App.LightswitchService.BanMessage;
		bool hasRestrictions = isBanned;
		bool banned = isBanned;
		string banMessage2 = banMessage;
		string[] allowedActions = App.LightswitchService.AllowedActions;
		bool restricted = isBanned;
		string message;
		if (isBanned)
		{
			message = (string.IsNullOrWhiteSpace(banMessage) ? "Active restrictions found on your account." : banMessage);
		}
		else
		{
			message = "Everything looking good!";
		}
		return new
		{
			hasRestrictions = hasRestrictions,
			banned = banned,
			banMessage = banMessage2,
			allowedActions = allowedActions,
			services = new
			{
				neoServices = new { restricted, message },
				matchmaker = new
				{
					restricted = false,
					message = "Everything looking good!"
				}
			}
		};
	}

	private Task<object?> SetDownloadFolderAsync(NeoBridgeContext ctx)
	{
		App.SettingsService.Settings.DownloadFolder = ctx.GetString("path") ?? "";
		App.SettingsService.Save();
		Broadcast("neo-settings-changed", SettingsViewModel());
		return Task.FromResult((object)new
		{
			ok = true,
			downloadFolder = App.SettingsService.Settings.DownloadFolder
		});
	}

	private Task<object?> SetProfileStatusAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("statusId");
		if (!string.IsNullOrWhiteSpace(text))
		{
			App.SettingsService.Settings.ProfileStatusId = text;
			App.SettingsService.Save();
			Broadcast("neo-profile-status-changed", new
			{
				statusId = text
			});
		}
		return Task.FromResult((object)new
		{
			ok = true,
			statusId = App.SettingsService.Settings.ProfileStatusId
		});
	}

	private static JsonObject Storage()
	{
		if (_storage != null)
		{
			return _storage;
		}
		lock (StorageLock)
		{
			if (_storage != null)
			{
				return _storage;
			}
			try
			{
				if (File.Exists(StoragePath))
				{
					_storage = JsonNode.Parse(File.ReadAllText(StoragePath)) as JsonObject;
				}
			}
			catch
			{
			}
			if (_storage == null)
			{
				_storage = new JsonObject();
			}
			bool flag = false;
			foreach (KeyValuePair<string, JsonNode> item in _storage)
			{
				if (IsBuildsKey(item.Key))
				{
					flag |= StripHeavyImageData(item.Value);
				}
			}
			if (flag)
			{
				SaveStorage();
			}
		}
		return _storage;
	}

	private static bool IsBuildsKey(string key)
	{
		if (!key.StartsWith("neo_launcher_imported_builds", StringComparison.Ordinal))
		{
			return key.StartsWith("neo_launcher_installed_builds", StringComparison.Ordinal);
		}
		return true;
	}

	private static void SaveStorage()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(StoragePath));
			lock (StorageLock)
			{
				File.WriteAllText(StoragePath, Storage().ToJsonString());
			}
		}
		catch
		{
		}
	}

	private static JsonNode? SanitizeStorageValue(string key, JsonNode? node)
	{
		if (node == null)
		{
			return null;
		}
		if (IsBuildsKey(key))
		{
			StripHeavyImageData(node);
		}
		return node;
	}

	private static bool StripHeavyImageData(JsonNode? node)
	{
		if (node is JsonArray jsonArray)
		{
			bool flag = false;
			{
				foreach (JsonNode item in jsonArray)
				{
					flag |= StripHeavyImageData(item);
				}
				return flag;
			}
		}
		if (!(node is JsonObject jsonObject))
		{
			return false;
		}
		bool result = false;
		string[] array = new string[4] { "splashDataUri", "image", "coverImage", "openedImage" };
		foreach (string propertyName in array)
		{
			if (jsonObject[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value) && value.StartsWith("data:image/"))
			{
				jsonObject.Remove(propertyName);
				result = true;
			}
		}
		return result;
	}

	private static JsonObject SanitizedStorageSnapshot()
	{
		JsonObject jsonObject = (Storage().DeepClone() as JsonObject) ?? new JsonObject();
		string[] array = jsonObject.Select((KeyValuePair<string, JsonNode> kv) => kv.Key).ToArray();
		foreach (string text in array)
		{
			jsonObject[text] = SanitizeStorageValue(text, jsonObject[text]);
		}
		return jsonObject;
	}

	public static void StartLauncherUpdateCheck()
	{
		Task.Run((Func<Task?>)RunLauncherUpdateCheckAsync);
		if (_launcherUpdateTimer == null)
		{
			_launcherUpdateTimer = new Timer((object? _) =>
			{
				_ = RunLauncherUpdateCheckAsync();
			}, null, LauncherUpdateInterval, LauncherUpdateInterval);
		}
	}

	public static void CheckLauncherUpdateOnActivation()
	{
		long num = Interlocked.Read(in _lastLauncherUpdateCheckTicks);
		if (DateTime.UtcNow.Ticks - num >= TimeSpan.FromMinutes(5L).Ticks)
		{
			Task.Run((Func<Task?>)RunLauncherUpdateCheckAsync);
		}
	}

	private static async Task RunLauncherUpdateCheckAsync()
	{
		Interlocked.Exchange(ref _lastLauncherUpdateCheckTicks, DateTime.UtcNow.Ticks);
		try
		{
			await CheckLauncherUpdateAsync();
		}
		catch
		{
		}
		bool flag;
		lock (UpdateStateLock)
		{
			flag = _launcherUpdateStatus == "available" && _pendingLauncherUpdate != null;
		}
		if (!flag || !CanAutoApplyLauncherUpdate())
		{
			return;
		}
		try
		{
			await ApplyLauncherUpdateAsync();
		}
		catch
		{
		}
	}

	private static bool CanAutoApplyLauncherUpdate()
	{
		try
		{
			if (App.GameLauncher.IsRunning)
			{
				return false;
			}
			if (App.InstallService.IsInstalling)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return true;
	}

	private static object LauncherUpdateViewModel()
	{
		lock (UpdateStateLock)
		{
			return new
			{
				currentVersion = App.UpdateService.CurrentVersion,
				installed = App.UpdateService.IsInstalled,
				status = _launcherUpdateStatus,
				availableVersion = _pendingLauncherUpdate?.TargetFullRelease.Version.ToString(),
				progress = _launcherUpdateProgress,
				error = _launcherUpdateError
			};
		}
	}

	private static void BroadcastLauncherUpdate()
	{
		Broadcast("neo-launcher-update-changed", LauncherUpdateViewModel());
	}

	private static async Task CheckLauncherUpdateAsync()
	{
		lock (UpdateStateLock)
		{
			if (_launcherUpdateBusy)
			{
				return;
			}
			bool flag;
			switch (_launcherUpdateStatus)
			{
			case "available":
			case "downloading":
			case "ready":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				return;
			}
			_launcherUpdateBusy = true;
			_launcherUpdateStatus = "checking";
			_launcherUpdateProgress = 0;
			_launcherUpdateError = null;
		}
		BroadcastLauncherUpdate();
		try
		{
			UpdateInfo updateInfo = ((!App.UpdateService.IsInstalled) ? null : (await App.UpdateService.CheckAsync()));
			UpdateInfo updateInfo2 = updateInfo;
			lock (UpdateStateLock)
			{
				_pendingLauncherUpdate = updateInfo2;
				string launcherUpdateStatus;
				if (updateInfo2 != null)
				{
					launcherUpdateStatus = "available";
				}
				else
				{
					launcherUpdateStatus = (App.UpdateService.IsInstalled ? "upToDate" : "idle");
				}
				_launcherUpdateStatus = launcherUpdateStatus;
			}
		}
		catch (Exception ex)
		{
			lock (UpdateStateLock)
			{
				_launcherUpdateStatus = "error";
				_launcherUpdateError = ex.Message;
			}
		}
		finally
		{
			lock (UpdateStateLock)
			{
				_launcherUpdateBusy = false;
			}
			BroadcastLauncherUpdate();
		}
	}

	private static async Task ApplyLauncherUpdateAsync()
	{
		UpdateInfo update;
		lock (UpdateStateLock)
		{
			if (_launcherUpdateBusy || _pendingLauncherUpdate == null)
			{
				return;
			}
			update = _pendingLauncherUpdate;
			_launcherUpdateBusy = true;
			_launcherUpdateStatus = "downloading";
			_launcherUpdateProgress = 0;
			_launcherUpdateError = null;
		}
		BroadcastLauncherUpdate();
		try
		{
			await App.UpdateService.DownloadAsync(update, (int progress) =>
			{
				bool flag;
				lock (UpdateStateLock)
				{
					flag = progress != _launcherUpdateProgress;
					_launcherUpdateProgress = progress;
				}
				if (flag)
				{
					BroadcastLauncherUpdate();
				}
			});
			lock (UpdateStateLock)
			{
				_launcherUpdateStatus = "ready";
			}
			BroadcastLauncherUpdate();
			App.DispatcherQueue?.TryEnqueue(() =>
			{
				try
				{
					App.UpdateService.ApplyAndRestart(update);
				}
				catch (Exception ex2)
				{
					lock (UpdateStateLock)
					{
						_launcherUpdateStatus = "error";
						_launcherUpdateError = ex2.Message;
					}
					BroadcastLauncherUpdate();
				}
			});
		}
		catch (Exception ex)
		{
			lock (UpdateStateLock)
			{
				_launcherUpdateStatus = "error";
				_launcherUpdateError = ex.Message;
			}
			BroadcastLauncherUpdate();
		}
		finally
		{
			lock (UpdateStateLock)
			{
				_launcherUpdateBusy = false;
			}
		}
	}

	private void MinimizeWindow()
	{
		PInvoke.ShowWindow(new HWND(WindowNative.GetWindowHandle(_window)), SHOW_WINDOW_CMD.SW_MINIMIZE);
	}

	private static async Task<object?> OpenExternalUrlAsync(NeoBridgeContext ctx)
	{
		string text = ctx.GetString("url");
		if (!string.IsNullOrWhiteSpace(text) && Uri.TryCreate(text, UriKind.Absolute, out Uri result))
		{
			await Windows.System.Launcher.LaunchUriAsync(result);
		}
		return null;
	}
}
