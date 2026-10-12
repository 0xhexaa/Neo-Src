using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using NeoLauncher.Models.UI.Appearance;
using NeoLauncher.Services.WebHost;
using NeoLauncher.Views;
using WinRT;
using WinRT.NeoLauncherVtableClasses;

namespace NeoLauncher;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
[WinRTExposedType(typeof(NeoLauncher_MainWindowWinRTTypeDetails))]
public sealed class MainWindow : FixedWindow, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private WebShellView WebShell;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	private bool _contentLoaded;

	public MainWindow()
		: base(1440, 810, TitleBarHeightOption.Tall, isResizable: false, showNativeBorder: true, scalePhysicalSizeWithDpi: true, 0.9)
	{
		InitializeComponent();
		App.AccountService.TryRestoreSessionAsync();
		WebShell.InitializeAsync(this);
		Activated += OnActivationChanged;
	}

	private void OnActivationChanged(object sender, WindowActivatedEventArgs args)
	{
		bool flag = args.WindowActivationState != WindowActivationState.Deactivated;
		WebShell.Bridge?.DispatchEvent("neo-window-activation", new
		{
			active = flag
		});
		if (flag)
		{
			NeoWebBridge.CheckLauncherUpdateOnActivation();
		}
	}

	public void SetBackdropType(BackdropType type)
	{
		BackdropType = type;
	}

	public void SetTheme(ElementTheme theme)
	{
		Theme = theme;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///MainWindow.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Application);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			WebShell = target.As<WebShellView>();
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
