using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using NeoLauncher.Localization;
using NeoLauncher.Services;
using NeoLauncher.Services.WebHost;
using Windows.Graphics;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.NeoLauncherVtableClasses;

namespace NeoLauncher.Views;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(NeoLauncher_Views_WebShellViewWinRTTypeDetails))]
public sealed class WebShellView : UserControl, IComponentConnector
{
	private record DragRect
	{
		public double x { get; set; }

		public double w { get; set; }
	}

	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	private NeoWebBridge? _bridge;

	private Window? _owner;

	private bool _nativeDragActive;

	private PointInt32 _nativeDragWindowStart;

	private NativePoint _nativeDragCursorStart;

	private static readonly Color GroundDark = Color.FromArgb(byte.MaxValue, 18, 18, 18);

	private static bool _shownFatalWebViewError;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private WebView2 WebView;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private Canvas DragLayer;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private bool _contentLoaded;

	public string RouteHash { get; set; } = string.Empty;

	public NeoWebBridge? Bridge => _bridge;

	public WebShellView()
	{
		InitializeComponent();
		WebView.DefaultBackgroundColor = GroundDark;
		WebView.Opacity = 0.0;
		WebView.NavigationCompleted += (WebView2 _, CoreWebView2NavigationCompletedEventArgs _) =>
		{
			RevealWebView();
		};
		WebView.CoreProcessFailed += (WebView2 _, CoreWebView2ProcessFailedEventArgs _) =>
		{
			RevealWebView();
		};
	}

	private void RevealWebView()
	{
		if (!(WebView.Opacity >= 1.0))
		{
			WebView.Opacity = 1.0;
		}
	}

	private void StartRevealFallback()
	{
		DispatcherQueueTimer dispatcherQueueTimer = DispatcherQueue.CreateTimer();
		dispatcherQueueTimer.Interval = TimeSpan.FromSeconds(6L);
		dispatcherQueueTimer.IsRepeating = false;
		dispatcherQueueTimer.Tick += (DispatcherQueueTimer t, object _) =>
		{
			t.Stop();
			RevealWebView();
		};
		dispatcherQueueTimer.Start();
	}

	private static async Task<bool> IsViteRunning()
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = TimeSpan.FromMilliseconds(300L)
			};
			return (await client.GetAsync("http://localhost:5173", HttpCompletionOption.ResponseHeadersRead)).IsSuccessStatusCode;
		}
		catch
		{
			return false;
		}
	}

	public async Task InitializeAsync(Window owner)
	{
		_owner = owner;
		bool isFriendContextMenu = RouteHash.Contains("neo-friend-context-menu", StringComparison.OrdinalIgnoreCase);
		if (isFriendContextMenu)
		{
			WebView.DefaultBackgroundColor = Color.FromArgb(0, 0, 0, 0);
		}
		try
		{
			CoreWebView2Environment environment = await NeoWebEnvironment.GetAsync();
			await WebView.EnsureCoreWebView2Async(environment);
		}
		catch (Exception ex)
		{
			ShowFatalWebViewError(ex);
			return;
		}
		if (isFriendContextMenu)
		{
			WebView.DefaultBackgroundColor = Color.FromArgb(0, 0, 0, 0);
		}
		CoreWebView2 core = WebView.CoreWebView2;
		core.SetVirtualHostNameToFolderMapping("neo.app", NeoWebEnvironment.WebRootPath, CoreWebView2HostResourceAccessKind.Allow);
		core.Settings.IsNonClientRegionSupportEnabled = true;
		core.Settings.AreDefaultContextMenusEnabled = false;
		core.Settings.AreBrowserAcceleratorKeysEnabled = false;
		core.Settings.AreDevToolsEnabled = false;
		await core.AddScriptToExecuteOnDocumentCreatedAsync(NeoBridgeScript.Source);
		core.NewWindowRequested += OnNewWindowRequested;
		_bridge = new NeoWebBridge(core, owner);
		owner.Closed += (object _, WindowEventArgs _) =>
		{
			_bridge?.Unregister();
		};
		_bridge.Register("neo_set_drag_rects", (NeoBridgeContext arg) =>
		{
			string json = (arg.Payload.TryGetProperty("rects", out var value) ? value.GetRawText() : "[]");
			List<DragRect> rects = JsonSerializer.Deserialize<List<DragRect>>(json) ?? new List<DragRect>();
			_bridge._dispatcher.TryEnqueue(() =>
			{
				DragLayer.Children.Clear();
				foreach (DragRect item in rects)
				{
					Border border = new Border
					{
						Width = item.w,
						Height = 50.0,
						Background = new SolidColorBrush(Colors.Transparent)
					};
					Canvas.SetLeft(border, item.x);
					Canvas.SetTop(border, 0.0);
					border.PointerPressed += NativeDragBand_PointerPressed;
					border.PointerMoved += NativeDragBand_PointerMoved;
					border.PointerReleased += NativeDragBand_PointerReleased;
					border.PointerCaptureLost += NativeDragBand_PointerCaptureLost;
					DragLayer.Children.Add(border);
				}
			});
			return Task.FromResult<object>(null);
		});
		string text = "https://neo.app" + "/index.html";
		if (!string.IsNullOrEmpty(RouteHash))
		{
			text += RouteHash;
		}
		core.DOMContentLoaded += (CoreWebView2 _, CoreWebView2DOMContentLoadedEventArgs _) =>
		{
			DispatcherQueue.TryEnqueue(RevealWebView);
		};
		core.Navigate(text);
		StartRevealFallback();
	}

	[DllImport("user32.dll")]
	private static extern bool GetCursorPos(out NativePoint point);

	private static UIElement? GetNativeDragElement(object sender)
	{
		return sender as UIElement;
	}

	private void NativeDragBand_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		PointerPoint currentPoint = e.GetCurrentPoint(DragLayer);
		UIElement nativeDragElement = GetNativeDragElement(sender);
		if (currentPoint.Properties.IsLeftButtonPressed && (object)_owner != null && (object)nativeDragElement != null && GetCursorPos(out _nativeDragCursorStart))
		{
			_nativeDragWindowStart = _owner.AppWindow.Position;
			_nativeDragActive = true;
			nativeDragElement.CapturePointer(e.Pointer);
			e.Handled = true;
		}
	}

	private void NativeDragBand_PointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_nativeDragActive && (object)_owner != null)
		{
			UIElement nativeDragElement = GetNativeDragElement(sender);
			NativePoint point;
			if (!e.GetCurrentPoint(nativeDragElement).Properties.IsLeftButtonPressed)
			{
				EndNativeDrag(sender, e);
			}
			else if (GetCursorPos(out point))
			{
				int num = point.X - _nativeDragCursorStart.X;
				int num2 = point.Y - _nativeDragCursorStart.Y;
				_owner.AppWindow.Move(new PointInt32(_nativeDragWindowStart.X + num, _nativeDragWindowStart.Y + num2));
				e.Handled = true;
			}
		}
	}

	private void NativeDragBand_PointerReleased(object sender, PointerRoutedEventArgs e)
	{
		EndNativeDrag(sender, e);
	}

	private void NativeDragBand_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
	{
		_nativeDragActive = false;
	}

	private void EndNativeDrag(object sender, PointerRoutedEventArgs e)
	{
		if (_nativeDragActive)
		{
			_nativeDragActive = false;
			try
			{
				GetNativeDragElement(sender)?.ReleasePointerCapture(e.Pointer);
			}
			catch
			{
			}
			e.Handled = true;
		}
	}

	private void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
	{
		args.Handled = true;
		string text = args.Uri ?? string.Empty;
		if (text.Contains("neo-friends-window", StringComparison.OrdinalIgnoreCase))
		{
			NeoChildWindows.OpenFriends();
		}
		else if (text.Contains("neo-friend-context-menu", StringComparison.OrdinalIgnoreCase) && (object)_owner != null)
		{
			NeoChildWindows.OpenFriendContextMenu(_owner, null);
		}
	}

	private static void ShowFatalWebViewError(Exception ex)
	{
		NeoLog.Crash("WebShellView.InitializeAsync", ex);
		if (!_shownFatalWebViewError)
		{
			_shownFatalWebViewError = true;
			string lpText = (NeoWebEnvironment.RuntimeMissing ? "Neo needs the Microsoft Edge WebView2 Runtime. Install it and try again." : Strings.FatalError);
			PInvoke.MessageBox(HWND.Null, lpText, Strings.FatalErrorTitle, MESSAGEBOX_STYLE.MB_OK);
			Environment.Exit(1);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///Views/WebShellView.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Application);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			WebView = target.As<WebView2>();
			break;
		case 3:
			DragLayer = target.As<Canvas>();
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
