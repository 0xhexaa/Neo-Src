using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NeoLauncher.Models.UI.Appearance;
using Windows.Graphics;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;
using WinRT.Interop;

namespace NeoLauncher;

public class FixedWindow : Window
{
	private struct NativePoint
	{
		public int X;

		public int Y;
	}

	private struct NativeMinMaxInfo
	{
		public NativePoint Reserved;

		public NativePoint MaxSize;

		public NativePoint MaxPosition;

		public NativePoint MinTrackSize;

		public NativePoint MaxTrackSize;
	}

	private const uint WM_NCLBUTTONDBLCLK = 163u;

	private const uint WM_DISPLAYCHANGE = 126u;

	private const uint WM_DPICHANGED = 736u;

	private const uint WM_CLOSE = 16u;

	private const uint WM_GETMINMAXINFO = 36u;

	private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;

	private const int DWMWA_BORDER_COLOR = 34;

	private const int DWMWA_CAPTION_COLOR = 35;

	private const int DWMWA_TEXT_COLOR = 36;

	private const int DWMWCP_DONOTROUND = 1;

	private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

	private const int DWMSBT_NONE = 1;

	private const int DWMWCP_ROUND = 2;

	private const int DWMWA_COLOR_NONE = -2;

	private const int GWL_STYLE = -16;

	private const int GWL_EXSTYLE = -20;

	private const int GWLP_HWNDPARENT = -8;

	private const int WS_CAPTION = 12582912;

	private const int WS_POPUP = int.MinValue;

	private const int WS_THICKFRAME = 262144;

	private const int WS_BORDER = 8388608;

	private const int WS_DLGFRAME = 4194304;

	private const int WS_SYSMENU = 524288;

	private const int WS_MINIMIZEBOX = 131072;

	private const int WS_MAXIMIZEBOX = 65536;

	private const int WS_EX_DLGMODALFRAME = 1;

	private const int WS_EX_TOOLWINDOW = 128;

	private const int WS_EX_WINDOWEDGE = 256;

	private const int WS_EX_CLIENTEDGE = 512;

	private const int WS_EX_STATICEDGE = 131072;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_FRAMECHANGED = 32u;

	private HWND _localHWND;

	private WNDPROC _newWndProc;

	private WNDPROC _oldWndProc;

	private int _minDesignWidth;

	private int _minDesignHeight;

	private MicaController? _micaController;

	private DesktopAcrylicController? _acrylicController;

	private SystemBackdropConfiguration? _backdropConfig;

	private BackdropType _currentBackdropType;

	private ElementTheme _currentTheme;

	private readonly double _widthRatio;

	private readonly int _baseWidth;

	private readonly double _heightRatio;

	private readonly int _baseHeight;

	private readonly double _aspectRatio;

	private readonly double _aspectScreenScale;

	private readonly bool _useFixedPixels;

	private readonly bool _scalePhysicalSizeWithDpi;

	private readonly double _maxWorkAreaFraction = 1.0;

	private readonly bool _isResizable;

	private readonly bool _showNativeBorder;

	private static readonly Color GroundDark = Color.FromArgb(byte.MaxValue, 18, 18, 18);

	private static readonly Color GroundLight = Color.FromArgb(byte.MaxValue, 243, 243, 243);

	public BackdropType BackdropType
	{
		get
		{
			return _currentBackdropType;
		}
		set
		{
			if (_currentBackdropType != value || !(_backdropConfig != null))
			{
				_currentBackdropType = value;
				ApplyBackdrop();
			}
		}
	}

	public ElementTheme Theme
	{
		get
		{
			return _currentTheme;
		}
		set
		{
			_currentTheme = value;
			if (Content is FrameworkElement frameworkElement)
			{
				frameworkElement.RequestedTheme = value;
			}
			if (_backdropConfig != null)
			{
				_backdropConfig.Theme = MapTheme(value);
			}
			if (_micaController != null)
			{
				_micaController.Kind = ((value != ElementTheme.Light) ? MicaKind.BaseAlt : MicaKind.Base);
			}
			if (_micaController == null && _acrylicController == null)
			{
				ApplySolidFallback();
			}
		}
	}

	public double DpiScale
	{
		get
		{
			try
			{
				uint dpiForWindow = GetDpiForWindow(WindowNative.GetWindowHandle(this));
				return (dpiForWindow != 0) ? ((double)dpiForWindow / 96.0) : 1.0;
			}
			catch
			{
				return 1.0;
			}
		}
	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(nint hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern nint SetWindowLongPtrNative(nint hWnd, int nIndex, nint dwNewLong);

	[DllImport("gdi32.dll", SetLastError = true)]
	private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int widthEllipse, int heightEllipse);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern int SetWindowRgn(nint hWnd, nint hRgn, bool redraw);

	public FixedWindow(double widthRatio, int baseWidth, double heightRatio, int baseHeight, TitleBarHeightOption heightOption = TitleBarHeightOption.Standard, bool isResizable = false, bool showNativeBorder = true)
	{
		_widthRatio = widthRatio;
		_baseWidth = baseWidth;
		_heightRatio = heightRatio;
		_baseHeight = baseHeight;
		_isResizable = isResizable;
		_showNativeBorder = showNativeBorder;
		InitializeWindow();
	}

	public FixedWindow(int width, int height, TitleBarHeightOption heightOption = TitleBarHeightOption.Standard, bool isResizable = false, bool showNativeBorder = true, bool scalePhysicalSizeWithDpi = true, double maxWorkAreaFraction = 1.0)
	{
		_baseWidth = width;
		_baseHeight = height;
		_useFixedPixels = true;
		_isResizable = isResizable;
		_showNativeBorder = showNativeBorder;
		_scalePhysicalSizeWithDpi = scalePhysicalSizeWithDpi;
		_maxWorkAreaFraction = maxWorkAreaFraction;
		InitializeWindow();
	}

	public FixedWindow(double aspectRatio, double screenScale, TitleBarHeightOption heightOption = TitleBarHeightOption.Standard, bool isResizable = false, bool showNativeBorder = true)
	{
		_aspectRatio = aspectRatio;
		_aspectScreenScale = screenScale;
		_isResizable = isResizable;
		_showNativeBorder = showNativeBorder;
		InitializeWindow();
	}

	[MemberNotNull(new string[] { "_newWndProc", "_oldWndProc" })]
	private void InitializeWindow()
	{
		_newWndProc = WndProc;
		_localHWND = new HWND(WindowNative.GetWindowHandle(this));
		ApplyWindowIcon();
		_oldWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(PInvoke.SetWindowLongPtr(_localHWND, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc)));
		Closed += FixedWindow_Closed;
		ApplyBackdrop();
		if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			overlappedPresenter.IsResizable = _isResizable;
			overlappedPresenter.IsMaximizable = false;
			overlappedPresenter.IsMinimizable = false;
			ExtendsContentIntoTitleBar = true;
			overlappedPresenter.SetBorderAndTitleBar(_showNativeBorder, hasTitleBar: false);
			if (!_showNativeBorder)
			{
				ApplyBorderlessPopupChrome();
			}
			ApplySizeAndCenter();
		}
	}

	private void ApplyWindowIcon()
	{
		try
		{
			string text = FindLauncherIconPath();
			if (text != null)
			{
				AppWindow.SetIcon(text);
			}
		}
		catch
		{
		}
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

	public void ApplyBorderlessPopupChrome()
	{
		try
		{
			ExtendsContentIntoTitleBar = false;
			nint windowHandle = WindowNative.GetWindowHandle(this);
			int windowLong = GetWindowLong(windowHandle, -16);
			windowLong &= -13565953;
			windowLong |= int.MinValue;
			SetWindowLong(windowHandle, -16, windowLong);
			int windowLong2 = GetWindowLong(windowHandle, -20);
			windowLong2 &= -131842;
			windowLong2 |= 0x80;
			SetWindowLong(windowHandle, -20, windowLong2);
			int pvAttribute = -2;
			DwmSetWindowAttribute(windowHandle, 34, ref pvAttribute, 4);
			DwmSetWindowAttribute(windowHandle, 35, ref pvAttribute, 4);
			DwmSetWindowAttribute(windowHandle, 36, ref pvAttribute, 4);
			int pvAttribute2 = 2;
			DwmSetWindowAttribute(windowHandle, 33, ref pvAttribute2, 4);
			SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, 55u);
		}
		catch
		{
		}
	}

	public void ApplyPopupOwner(Window owner)
	{
		try
		{
			nint windowHandle = WindowNative.GetWindowHandle(this);
			nint windowHandle2 = WindowNative.GetWindowHandle(owner);
			SetWindowLongPtrNative(windowHandle, -8, windowHandle2);
			SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, 55u);
		}
		catch
		{
		}
	}

	public void ApplyRoundedPopupRegion(int width, int height, int radius = 18)
	{
		try
		{
			nint windowHandle = WindowNative.GetWindowHandle(this);
			SetWindowRgn(windowHandle, IntPtr.Zero, redraw: true);
			int pvAttribute = 5593697;
			DwmSetWindowAttribute(windowHandle, 34, ref pvAttribute, 4);
			DwmSetWindowAttribute(windowHandle, 35, ref pvAttribute, 4);
			DwmSetWindowAttribute(windowHandle, 36, ref pvAttribute, 4);
			int pvAttribute2 = 2;
			DwmSetWindowAttribute(windowHandle, 33, ref pvAttribute2, 4);
			SetWindowPos(windowHandle, IntPtr.Zero, 0, 0, 0, 0, 55u);
		}
		catch
		{
		}
	}

	private void ApplySizeAndCenter()
	{
		RectInt32 workArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
		double scale = DpiScale;
		int num;
		int num2;
		if (_useFixedPixels)
		{
			num = (_scalePhysicalSizeWithDpi ? Scaled(_baseWidth) : _baseWidth);
			num2 = (_scalePhysicalSizeWithDpi ? Scaled(_baseHeight) : _baseHeight);
			if (_maxWorkAreaFraction > 0.0 && _maxWorkAreaFraction < 1.0 && num > 0 && num2 > 0)
			{
				double val = Math.Min((double)workArea.Width * _maxWorkAreaFraction / (double)num, (double)workArea.Height * _maxWorkAreaFraction / (double)num2);
				double val2 = Math.Max((double)_baseWidth / (double)num, (double)_baseHeight / (double)num2);
				val = Math.Max(val, Math.Min(1.0, val2));
				if (val < 1.0)
				{
					num = (int)Math.Round((double)num * val);
					num2 = (int)Math.Round((double)num2 * val);
				}
			}
		}
		else if (_aspectRatio > 0.0)
		{
			int num3 = (int)((double)workArea.Width * _aspectScreenScale);
			int num4 = (int)((double)workArea.Height * _aspectScreenScale);
			num = num3;
			num2 = (int)((double)num / _aspectRatio);
			if (num2 > num4)
			{
				num2 = num4;
				num = (int)((double)num2 * _aspectRatio);
			}
		}
		else
		{
			num = Math.Max(Scaled(_baseWidth), (int)((double)workArea.Width * _widthRatio));
			num2 = Math.Max(Scaled(_baseHeight), (int)((double)workArea.Height * _heightRatio));
		}
		num = Math.Min(num, workArea.Width);
		num2 = Math.Min(num2, workArea.Height);
		AppWindow.ResizeClient(new SizeInt32(num, num2));
		SizeInt32 size = AppWindow.Size;
		int num5 = size.Width - workArea.Width;
		int num6 = size.Height - workArea.Height;
		if (num5 > 0 || num6 > 0)
		{
			AppWindow.ResizeClient(new SizeInt32(Math.Max(1, num - Math.Max(0, num5)), Math.Max(1, num2 - Math.Max(0, num6))));
			size = AppWindow.Size;
		}
		AppWindow.Move(new PointInt32(workArea.X + Math.Max(0, (workArea.Width - size.Width) / 2), workArea.Y + Math.Max(0, (workArea.Height - size.Height) / 2)));
		int Scaled(int value)
		{
			return (int)Math.Round((double)value * scale);
		}
	}

	public void SetMinimumSize(int designWidth, int designHeight)
	{
		_minDesignWidth = Math.Max(0, designWidth);
		_minDesignHeight = Math.Max(0, designHeight);
	}

	public void BringToFront()
	{
		PInvoke.ShowWindow(_localHWND, (!PInvoke.IsIconic(_localHWND)) ? SHOW_WINDOW_CMD.SW_SHOWNORMAL : SHOW_WINDOW_CMD.SW_RESTORE);
		try
		{
			Activate();
		}
		catch
		{
		}
		PInvoke.SetForegroundWindow(_localHWND);
	}

	public void HideToTray()
	{
		PInvoke.ShowWindow(_localHWND, SHOW_WINDOW_CMD.SW_HIDE);
	}

	private void FixedWindow_Closed(object sender, WindowEventArgs args)
	{
		PInvoke.SetWindowLongPtr(_localHWND, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_oldWndProc));
		if (_micaController != null)
		{
			_micaController.RemoveSystemBackdropTarget(CastExtensions.As<ICompositionSupportsSystemBackdrop>(this));
			_micaController.Dispose();
			_micaController = null;
		}
		if (_acrylicController != null)
		{
			_acrylicController.RemoveSystemBackdropTarget(CastExtensions.As<ICompositionSupportsSystemBackdrop>(this));
			_acrylicController.Dispose();
			_acrylicController = null;
		}
		_backdropConfig = null;
	}

	private void ApplyBackdrop()
	{
		_micaController?.Dispose();
		_acrylicController?.Dispose();
		_micaController = null;
		_acrylicController = null;
		_backdropConfig = new SystemBackdropConfiguration
		{
			IsInputActive = true,
			Theme = MapTheme(_currentTheme)
		};
		if (_currentBackdropType == BackdropType.Solid)
		{
			SuppressSystemBackdrop();
		}
		else if (DesktopAcrylicController.IsSupported())
		{
			_acrylicController = new DesktopAcrylicController
			{
				TintColor = Color.FromArgb(byte.MaxValue, 238, 244, 252),
				TintOpacity = 0.025f,
				LuminosityOpacity = 0.075f,
				FallbackColor = Color.FromArgb(30, 32, 34, 40)
			};
			_acrylicController.AddSystemBackdropTarget(CastExtensions.As<ICompositionSupportsSystemBackdrop>(this));
			_acrylicController.SetSystemBackdropConfiguration(_backdropConfig);
		}
		if (_micaController == null && _acrylicController == null)
		{
			ApplySolidFallback();
		}
	}

	private void SuppressSystemBackdrop()
	{
		try
		{
			int pvAttribute = 1;
			DwmSetWindowAttribute(WindowNative.GetWindowHandle(this), 38, ref pvAttribute, 4);
		}
		catch
		{
		}
	}

	private void ApplySolidFallback()
	{
		if (Content is Panel panel)
		{
			bool flag = _currentTheme == ElementTheme.Dark || (_currentTheme == ElementTheme.Default && Application.Current.RequestedTheme == ApplicationTheme.Dark);
			panel.Background = new SolidColorBrush(flag ? GroundDark : GroundLight);
		}
	}

	private static SystemBackdropTheme MapTheme(ElementTheme theme)
	{
		return theme switch
		{
			ElementTheme.Light => SystemBackdropTheme.Light, 
			ElementTheme.Dark => SystemBackdropTheme.Dark, 
			_ => SystemBackdropTheme.Default, 
		};
	}

	private LRESULT WndProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
	{
		switch (msg)
		{
		case 163u:
			return new LRESULT(0);
		case 36u:
		{
			if (_minDesignWidth <= 0 && _minDesignHeight <= 0)
			{
				break;
			}
			LRESULT result = PInvoke.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
			try
			{
				nint ptr = new IntPtr(lParam.Value);
				NativeMinMaxInfo structure = Marshal.PtrToStructure<NativeMinMaxInfo>(ptr);
				double dpiScale = DpiScale;
				if (_minDesignWidth > 0)
				{
					structure.MinTrackSize.X = Math.Max(structure.MinTrackSize.X, (int)Math.Round((double)_minDesignWidth * dpiScale));
				}
				if (_minDesignHeight > 0)
				{
					structure.MinTrackSize.Y = Math.Max(structure.MinTrackSize.Y, (int)Math.Round((double)_minDesignHeight * dpiScale));
				}
				Marshal.StructureToPtr(structure, ptr, fDeleteOld: false);
			}
			catch
			{
			}
			return result;
		}
		}
		if (App.HandleTrayWindowMessage(msg, lParam))
		{
			return new LRESULT(0);
		}
		if (msg == 16 && this is MainWindow)
		{
			App.RequestMainWindowClose();
			return new LRESULT(0);
		}
		switch (msg)
		{
		case 736u:
		{
			LRESULT result2 = PInvoke.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
			ApplySizeAndCenter();
			return result2;
		}
		case 126u:
			ApplySizeAndCenter();
			break;
		}
		return PInvoke.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
	}
}
