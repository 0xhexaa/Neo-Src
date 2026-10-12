using System;
using System.Text.Json.Nodes;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using NeoLauncher.Models.UI.Appearance;
using NeoLauncher.Services.WebHost;
using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace NeoLauncher.Views;

internal static class NeoChildWindows
{
	private static FixedWindow? _friends;

	private static FixedWindow? _friendContextMenu;

	private static NativeFriendContextMenuView? _friendContextView;

	private static DateTime _friendContextOpenedAt = DateTime.MinValue;

	private static bool _friendContextMenuVisible;

	private const int FriendContextMenuWidth = 258;

	private static double ContextMenuScale(Window owner)
	{
		if (!(owner is FixedWindow fixedWindow))
		{
			return 1.0;
		}
		return fixedWindow.DpiScale;
	}

	private static int ScaleToPixels(double value, double scale)
	{
		return (int)Math.Round(value * scale);
	}

	public static void OpenFriends()
	{
		if ((object)_friends != null)
		{
			try
			{
				_friends.BringToFront();
				return;
			}
			catch
			{
				return;
			}
		}
		WebShellView webShellView = new WebShellView
		{
			RouteHash = "#neo-friends-window"
		};
		FixedWindow window = new FixedWindow(360, 700, TitleBarHeightOption.Standard, isResizable: true)
		{
			Title = "Friends",
			Content = webShellView
		};
		window.SetMinimumSize(360, 520);
		window.Closed += (object _, WindowEventArgs _) =>
		{
			if (_friends == window)
			{
				DestroyFriendContextMenu();
				_friends = null;
			}
		};
		_friends = window;
		window.Activate();
		webShellView.InitializeAsync(window);
	}

	private static PointInt32 GetHiddenPoint(Window owner)
	{
		try
		{
			RectInt32 workArea = DisplayArea.GetFromWindowId(owner.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
			return new PointInt32(workArea.X - 32000, workArea.Y - 32000);
		}
		catch
		{
			return new PointInt32(-32000, -32000);
		}
	}

	private static void EnsureFriendContextMenu(Window owner, int menuHeight)
	{
		if ((object)_friendContextMenu != null)
		{
			return;
		}
		NativeFriendContextMenuView nativeFriendContextMenuView = new NativeFriendContextMenuView();
		FixedWindow window = new FixedWindow(258, menuHeight, TitleBarHeightOption.Standard, isResizable: false, showNativeBorder: false)
		{
			Title = "Friend Menu",
			Content = nativeFriendContextMenuView,
			BackdropType = BackdropType.Glass,
			Theme = ElementTheme.Dark
		};
		nativeFriendContextMenuView.HeightRequested += ResizeFriendContextMenu;
		nativeFriendContextMenuView.CloseRequested += CloseFriendContextMenu;
		_friendContextOpenedAt = DateTime.UtcNow;
		_friendContextMenuVisible = false;
		window.Activated += (object _, WindowActivatedEventArgs args) =>
		{
			if (_friendContextMenuVisible && args.WindowActivationState == WindowActivationState.Deactivated && DateTime.UtcNow - _friendContextOpenedAt > TimeSpan.FromMilliseconds(180L))
			{
				CloseFriendContextMenu();
			}
		};
		window.Closed += (object _, WindowEventArgs _) =>
		{
			if (_friendContextMenu == window)
			{
				_friendContextMenu = null;
				_friendContextView = null;
				_friendContextMenuVisible = false;
			}
		};
		_friendContextMenu = window;
		_friendContextView = nativeFriendContextMenuView;
		PointInt32 hiddenPoint = GetHiddenPoint(owner);
		double scale = ContextMenuScale(owner);
		int width = ScaleToPixels(258.0, scale);
		int height = ScaleToPixels(menuHeight, scale);
		window.Activate();
		window.ApplyPopupOwner(owner);
		window.ApplyBorderlessPopupChrome();
		window.AppWindow.Resize(new SizeInt32(width, height));
		window.AppWindow.Move(hiddenPoint);
		window.ApplyRoundedPopupRegion(width, height);
	}

	public static void OpenFriendContextMenu(Window owner, NeoBridgeContext? ctx)
	{
		string text = ctx?.GetString("friendJson") ?? "";
		int num = (int)Math.Clamp(ctx?.GetNumber("menuHeight") ?? 328.0, 92.0, 430.0);
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		try
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				JsonNode.Parse(text);
				string text2 = text.ToLowerInvariant();
				flag = text2.Contains("__nativeblocked") || text2.Contains("\"relationship\":\"blocked\"") || text2.Contains("\"status\":\"blocked\"") || text2.Contains("\"blocked\":true");
				flag2 = text2.Contains("__nativeincomingrequest") || text2.Contains("__incomingrequestpanel") || text2.Contains("\"relationship\":\"incoming\"") || text2.Contains("\"status\":\"incoming\"") || text2.Contains("incoming request");
				flag3 = text2.Contains("__nativepartyinvite") || text2.Contains("__partyinvite");
			}
		}
		catch
		{
			flag = text.Contains("blocked", StringComparison.OrdinalIgnoreCase);
			flag2 = text.Contains("incoming", StringComparison.OrdinalIgnoreCase) || text.Contains("request", StringComparison.OrdinalIgnoreCase);
			flag3 = text.Contains("partyinvite", StringComparison.OrdinalIgnoreCase);
		}
		if (flag)
		{
			num = Math.Max(num, 196);
		}
		else if (flag2)
		{
			num = 214;
		}
		else if (flag3)
		{
			num = 250;
		}
		double scale = ContextMenuScale(owner);
		int num2 = ScaleToPixels(ctx?.GetNumber("relativeX") ?? 220.0, scale);
		int num3 = ScaleToPixels(ctx?.GetNumber("relativeY") ?? 204.0, scale);
		int num4 = ScaleToPixels(258.0, scale);
		int num5 = ScaleToPixels(num, scale);
		int num6 = ScaleToPixels(12.0, scale);
		int num7 = ScaleToPixels(8.0, scale);
		PointInt32 position = owner.AppWindow.Position;
		RectInt32 workArea = DisplayArea.GetFromWindowId(owner.AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
		int num8 = position.X + num2;
		int num9 = position.Y + num3;
		int num10 = num8 + num6;
		if (num10 + num4 > workArea.X + workArea.Width - num7)
		{
			num10 = num8 - num4 - num6;
		}
		int value = num9 - ScaleToPixels(10.0, scale);
		num10 = Math.Clamp(num10, workArea.X + num7, workArea.X + workArea.Width - num4 - num7);
		value = Math.Clamp(value, workArea.Y + num7, workArea.Y + workArea.Height - num5 - num7);
		EnsureFriendContextMenu(owner, num);
		if ((object)_friendContextMenu != null && (object)_friendContextView != null)
		{
			_friendContextView.SetEdgeOutlineStyle(ctx?.GetString("edgeOutlineStyle"));
			_friendContextView.SetLabels(ctx?.GetString("menuLabelsJson"));
			try
			{
				JsonNode friend = ((!string.IsNullOrWhiteSpace(text)) ? JsonNode.Parse(text) : null);
				_friendContextView.SetFriend(friend);
			}
			catch
			{
				_friendContextView.SetFriend(null);
			}
			_friendContextView.UpdateLayout();
			num5 = ScaleToPixels(Math.Clamp(_friendContextView.LastRequestedHeight, 92.0, 430.0), scale);
			value = Math.Clamp(num9 - ScaleToPixels(10.0, scale), workArea.Y + num7, workArea.Y + workArea.Height - num5 - num7);
			_friendContextOpenedAt = DateTime.UtcNow;
			_friendContextMenuVisible = true;
			_friendContextMenu.ApplyPopupOwner(owner);
			_friendContextMenu.ApplyBorderlessPopupChrome();
			_friendContextMenu.AppWindow.Resize(new SizeInt32(num4, num5));
			_friendContextMenu.AppWindow.Move(new PointInt32(num10, value));
			_friendContextMenu.ApplyRoundedPopupRegion(num4, num5);
			_friendContextMenu.BringToFront();
		}
	}

	public static void ResizeFriendContextMenu(double height)
	{
		if ((object)_friendContextMenu != null)
		{
			double dpiScale = _friendContextMenu.DpiScale;
			int height2 = ScaleToPixels(Math.Clamp(height, 92.0, 430.0), dpiScale);
			int width = ScaleToPixels(258.0, dpiScale);
			_friendContextMenu.AppWindow.Resize(new SizeInt32(width, height2));
			_friendContextMenu.ApplyRoundedPopupRegion(width, height2);
		}
	}

	public static void CloseFriendContextMenu()
	{
		if ((object)_friendContextMenu == null)
		{
			return;
		}
		_friendContextMenuVisible = false;
		try
		{
			_friendContextView?.ResetToActions();
			if ((object)_friends != null)
			{
				_friendContextMenu.AppWindow.Move(GetHiddenPoint(_friends));
			}
			else
			{
				_friendContextMenu.AppWindow.Move(new PointInt32(-32000, -32000));
			}
		}
		catch
		{
		}
	}

	private static void DestroyFriendContextMenu()
	{
		try
		{
			_friendContextMenu?.Close();
		}
		catch
		{
		}
		_friendContextMenu = null;
		_friendContextView = null;
		_friendContextMenuVisible = false;
	}

	public static void CloseFriends()
	{
		DestroyFriendContextMenu();
		try
		{
			_friends?.Close();
		}
		catch
		{
		}
		_friends = null;
	}

	public static void MinimizeFriends()
	{
		if ((object)_friends != null)
		{
			PInvoke.ShowWindow(new HWND(WindowNative.GetWindowHandle(_friends)), SHOW_WINDOW_CMD.SW_MINIMIZE);
		}
	}
}
