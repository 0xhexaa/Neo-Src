using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using NeoLauncher.Localization;
using NeoLauncher.Models;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.NeoLauncher_XamlTypeInfo;
using NeoLauncher.Services;
using NeoLauncher.Services.Account;
using NeoLauncher.Services.Fortnite;
using NeoLauncher.Services.Friends;
using NeoLauncher.Services.Game;
using NeoLauncher.Services.Launcher;
using NeoLauncher.Services.Prism;
using NeoLauncher.Services.Updates;
using NeoLauncher.Services.WebHost;
using NeoLauncher.Views;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.Interop;
using WinRT.NeoLauncherVtableClasses;

namespace NeoLauncher;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IApplicationOverrides")]
[WinRTExposedType(typeof(NeoLauncher_AppWinRTTypeDetails))]
public class App : Application, IXamlMetadataProvider
{
	public delegate Task OnUriCallbackDelegate(Uri uri);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NOTIFYICONDATA
	{
		public uint cbSize;

		public nint hWnd;

		public uint uID;

		public uint uFlags;

		public uint uCallbackMessage;

		public nint hIcon;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
		public string szTip;

		public uint dwState;

		public uint dwStateMask;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
		public string szInfo;

		public uint uTimeoutOrVersion;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
		public string szInfoTitle;

		public uint dwInfoFlags;

		public Guid guidItem;

		public nint hBalloonIcon;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	public static MainWindow? Window;

	public static DispatcherQueue? DispatcherQueue;

	public static OnUriCallbackDelegate? OnUriCallback;

	private const uint WM_APP = 32768u;

	private const uint WM_TRAYICON = 33482u;

	private const uint WM_NULL = 0u;

	private const uint WM_CONTEXTMENU = 123u;

	private const uint WM_LBUTTONUP = 514u;

	private const uint WM_LBUTTONDBLCLK = 515u;

	private const uint WM_RBUTTONUP = 517u;

	private const uint NIM_ADD = 0u;

	private const uint NIM_DELETE = 2u;

	private const uint NIF_MESSAGE = 1u;

	private const uint NIF_ICON = 2u;

	private const uint NIF_TIP = 4u;

	private const int IDI_APPLICATION = 32512;

	private const uint IMAGE_ICON = 1u;

	private const uint LR_LOADFROMFILE = 16u;

	private const uint LR_DEFAULTSIZE = 64u;

	private const uint MF_BYPOSITION = 1024u;

	private const uint MF_STRING = 0u;

	private const uint MF_SEPARATOR = 2048u;

	private const uint TPM_RIGHTBUTTON = 2u;

	private const uint TPM_RETURNCMD = 256u;

	private const uint TrayIconId = 1u;

	private const uint TrayCloseCommand = 1001u;

	private const uint TrayFriendsCommand = 1002u;

	private const uint TrayLaunchCommand = 1003u;

	private const uint TrayLibraryCommand = 1004u;

	private const uint TrayLoginCommand = 1005u;

	private const uint TraySignupCommand = 1006u;

	private static bool _trayIconAdded;

	private static nint _trayIconHandle;

	private static bool _trayIconHandleOwned;

	private bool _handlingFatalError;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private XamlMetaDataProvider __appProvider;

	public static bool StartInFriendsMode { get; set; }

	public static IServiceProvider Services { get; private set; }

	public static LibraryService LibraryService => Services.GetRequiredService<LibraryService>();

	public static AccountService AccountService => Services.GetRequiredService<AccountService>();

	public static GameLauncher GameLauncher => Services.GetRequiredService<GameLauncher>();

	public static NeoPresenceService PresenceService => Services.GetRequiredService<NeoPresenceService>();

	public static InstallService InstallService => Services.GetRequiredService<InstallService>();

	public static LauncherService LauncherService => Services.GetRequiredService<LauncherService>();

	public static GameService GameService => Services.GetRequiredService<GameService>();

	public static SettingsService SettingsService => Services.GetRequiredService<SettingsService>();

	public static LightswitchService LightswitchService => Services.GetRequiredService<LightswitchService>();

	public static UpdateService UpdateService => Services.GetRequiredService<UpdateService>();

	public static AnalyticsService AnalyticsService => Services.GetRequiredService<AnalyticsService>();

	public static PrismService PrismService => Services.GetRequiredService<PrismService>();

	public static LoadoutService LoadoutService => Services.GetRequiredService<LoadoutService>();

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	private XamlMetaDataProvider _AppProvider
	{
		get
		{
			if (__appProvider == null)
			{
				__appProvider = new XamlMetaDataProvider();
			}
			return __appProvider;
		}
	}

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint LoadIcon(nint hInstance, nint lpIconName);

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint LoadImage(nint hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyIcon(nint hIcon);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint CreatePopupMenu();

	[DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern bool InsertMenu(nint hMenu, uint uPosition, uint uFlags, nuint uIDNewItem, string lpNewItem);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool GetCursorPos(out POINT lpPoint);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyMenu(nint hMenu);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool PostMessage(nint hWnd, uint Msg, nuint wParam, nint lParam);

	public App()
	{
		Services = new ServiceCollection().AddSingleton<LibraryService>().AddSingleton<AccountService>().AddSingleton<GameLauncher>()
			.AddSingleton<NeoPresenceService>()
			.AddSingleton<InstallService>()
			.AddSingleton<LauncherService>()
			.AddSingleton<GameService>()
			.AddSingleton<SettingsService>()
			.AddSingleton<LightswitchService>()
			.AddSingleton<UpdateService>()
			.AddSingleton<AnalyticsService>()
			.AddSingleton<PrismService>()
			.AddSingleton<LoadoutService>()
			.BuildServiceProvider();
		InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		SettingsService.ApplyLanguage();
		Window = new MainWindow();
		AppSettings settings = SettingsService.Settings;
		MainWindow window = Window;
		window.SetTheme(settings.Theme switch
		{
			AppTheme.Light => ElementTheme.Light, 
			AppTheme.Dark => ElementTheme.Dark, 
			_ => ElementTheme.Default, 
		});
		Window.SetBackdropType(settings.Material);
		DispatcherQueue = Window.DispatcherQueue;
		InitializeTrayIcon();
		Window.Activate();
		Window.Closed += (object _, WindowEventArgs _) =>
		{
			AnalyticsService.Track(EventType.LauncherClosed);
		};
		if (StartInFriendsMode)
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					Window?.HideToTray();
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
			});
		}
		UnhandledException += App_UnhandledException;
		bool flag = !SettingsService.Settings.HasLaunched;
		if (flag)
		{
			SettingsService.Settings.HasLaunched = true;
			SettingsService.Save();
		}
		AnalyticsService.Track((!flag) ? EventType.LauncherOpened : EventType.LauncherFirstRun);
		NeoWebBridge.StartLauncherUpdateCheck();
		GameLauncher.GameStateChanged += NeoWebBridge.BroadcastGameState;
	}

	private static void InitializeTrayIcon()
	{
		if (_trayIconAdded || (object)Window == null)
		{
			return;
		}
		try
		{
			nint windowHandle = WindowNative.GetWindowHandle(Window);
			if (windowHandle != IntPtr.Zero)
			{
				NOTIFYICONDATA lpData = CreateTrayIconData(windowHandle);
				_trayIconAdded = Shell_NotifyIcon(0u, ref lpData);
				if (!_trayIconAdded)
				{
					DestroyTrayIconHandle();
				}
			}
		}
		catch
		{
			_trayIconAdded = false;
		}
	}

	private static nint LoadTrayIcon()
	{
		if (_trayIconHandle != IntPtr.Zero)
		{
			return _trayIconHandle;
		}
		string text = FindLauncherIconPath();
		if (text != null)
		{
			_trayIconHandle = LoadImage(IntPtr.Zero, text, 1u, 0, 0, 80u);
			_trayIconHandleOwned = _trayIconHandle != IntPtr.Zero;
		}
		if (_trayIconHandle == IntPtr.Zero)
		{
			_trayIconHandle = LoadIcon(IntPtr.Zero, 32512);
			_trayIconHandleOwned = false;
		}
		return _trayIconHandle;
	}

	private static string? FindLauncherIconPath()
	{
		string path = Path.Combine(AppContext.BaseDirectory, "Assets");
		string[] array = new string[2]
		{
			Path.Combine(path, "neo.ico"),
			Path.Combine(path, "neo.ico.ico")
		};
		foreach (string text in array)
		{
			if (File.Exists(text))
			{
				return text;
			}
		}
		return null;
	}

	private static void DestroyTrayIconHandle()
	{
		if (_trayIconHandleOwned && _trayIconHandle != IntPtr.Zero)
		{
			DestroyIcon(_trayIconHandle);
		}
		_trayIconHandle = IntPtr.Zero;
		_trayIconHandleOwned = false;
	}

	private static NOTIFYICONDATA CreateTrayIconData(nint hwnd)
	{
		return new NOTIFYICONDATA
		{
			cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
			hWnd = hwnd,
			uID = 1u,
			uFlags = 7u,
			uCallbackMessage = 33482u,
			hIcon = LoadTrayIcon(),
			szTip = "Neo Launcher",
			szInfo = string.Empty,
			szInfoTitle = string.Empty
		};
	}

	internal static bool HandleTrayWindowMessage(uint msg, LPARAM lParam)
	{
		if (msg != 33482)
		{
			return false;
		}
		switch ((uint)lParam.Value)
		{
		case 123u:
		case 517u:
			ShowTrayMenu();
			return true;
		case 514u:
		case 515u:
			try
			{
				Window?.BringToFront();
			}
			catch
			{
			}
			return true;
		default:
			return true;
		}
	}

	private static void ShowTrayMenu()
	{
		if ((object)Window == null)
		{
			return;
		}
		nint windowHandle = WindowNative.GetWindowHandle(Window);
		if (windowHandle == IntPtr.Zero)
		{
			return;
		}
		nint num = CreatePopupMenu();
		if (num == IntPtr.Zero)
		{
			return;
		}
		try
		{
			if ((object)AccountService.CurrentUser == null)
			{
				InsertMenu(num, 0u, 1024u, new UIntPtr(1005u), "Log In");
				InsertMenu(num, 1u, 1024u, new UIntPtr(1006u), "Sign Up");
				InsertMenu(num, 2u, 1024u, new UIntPtr(1001u), "Close");
			}
			else
			{
				InsertMenu(num, 0u, 1024u, new UIntPtr(1002u), "Friends");
				InsertMenu(num, 1u, 3072u, UIntPtr.Zero, string.Empty);
				InsertMenu(num, 2u, 1024u, new UIntPtr(1003u), "Launch");
				InsertMenu(num, 3u, 1024u, new UIntPtr(1004u), "Library");
				InsertMenu(num, 4u, 3072u, UIntPtr.Zero, string.Empty);
				InsertMenu(num, 5u, 1024u, new UIntPtr(1001u), "Close");
			}
			if (GetCursorPos(out var lpPoint))
			{
				SetForegroundWindow(windowHandle);
				uint num2 = TrackPopupMenu(num, 258u, lpPoint.X, lpPoint.Y, 0, windowHandle, IntPtr.Zero);
				PostMessage(windowHandle, 0u, UIntPtr.Zero, IntPtr.Zero);
				if (num2 != 0)
				{
					RunTrayCommand(num2);
				}
			}
		}
		finally
		{
			DestroyMenu(num);
		}
	}

	private static void RunTrayCommand(uint command)
	{
		switch (command)
		{
		case 1001u:
			CloseAllLauncherProcesses();
			break;
		case 1002u:
			try
			{
				NeoChildWindows.OpenFriends();
			}
			catch
			{
			}
			NeoWebBridge.Broadcast("neo-tray-action", new
			{
				action = "friends"
			});
			break;
		case 1003u:
			NeoWebBridge.Broadcast("neo-tray-action", new
			{
				action = "launch"
			});
			break;
		case 1004u:
			try
			{
				Window?.Activate();
			}
			catch
			{
			}
			try
			{
				Window?.BringToFront();
			}
			catch
			{
			}
			NeoWebBridge.Broadcast("neo-tray-action", new
			{
				action = "library"
			});
			break;
		case 1005u:
			try
			{
				Window?.Activate();
			}
			catch
			{
			}
			try
			{
				Window?.BringToFront();
			}
			catch
			{
			}
			NeoWebBridge.Broadcast("neo-tray-action", new
			{
				action = "login"
			});
			break;
		case 1006u:
			try
			{
				Window?.Activate();
			}
			catch
			{
			}
			try
			{
				Window?.BringToFront();
			}
			catch
			{
			}
			NeoWebBridge.Broadcast("neo-tray-action", new
			{
				action = "signup"
			});
			break;
		}
	}

	public static void DisposeTrayIcon()
	{
		try
		{
			if (_trayIconAdded && (object)Window != null)
			{
				nint windowHandle = WindowNative.GetWindowHandle(Window);
				NOTIFYICONDATA lpData = new NOTIFYICONDATA
				{
					cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
					hWnd = windowHandle,
					uID = 1u
				};
				Shell_NotifyIcon(2u, ref lpData);
				_trayIconAdded = false;
				DestroyTrayIconHandle();
			}
		}
		catch
		{
		}
	}

	public static void RequestMainWindowClose()
	{
		bool flag;
		try
		{
			flag = GameLauncher.IsRunning;
		}
		catch
		{
			flag = false;
		}
		if (flag)
		{
			try
			{
				Window?.HideToTray();
				return;
			}
			catch
			{
				return;
			}
		}
		CloseAllLauncherProcesses();
	}

	private static void CloseAllLauncherProcesses()
	{
		try
		{
			NeoChildWindows.CloseFriends();
		}
		catch
		{
		}
		DisposeTrayIcon();
		Environment.Exit(0);
	}

	private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		if (!_handlingFatalError)
		{
			_handlingFatalError = true;
			e.Handled = true;
			NeoLog.Crash("App.UnhandledException", e.Exception);
			PInvoke.MessageBox(HWND.Null, Strings.FatalError, Strings.FatalErrorTitle, MESSAGEBOX_STYLE.MB_OK);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///App.xaml");
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(Type type)
	{
		return _AppProvider.GetXamlType(type);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public IXamlType GetXamlType(string fullName)
	{
		return _AppProvider.GetXamlType(fullName);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return _AppProvider.GetXmlnsDefinitions();
	}
}
