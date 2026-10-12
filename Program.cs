using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Win32;
using NeoLauncher;
using NeoLauncher.Services;
using NeoLauncher.Views;
using Velopack;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using WinRT;

public static class Program
{
	private const string MutexName = "NeoLauncher_Mutex";

	private const string PipeName = "NeoLauncher_Pipe";

	private const string UriName = "NeoLauncher";

	private static Mutex? _appMutex;

	[STAThread]
	private static void Main(string[] args)
	{
		NeoLog.Initialize();
		AppDomain.CurrentDomain.UnhandledException += (object _, System.UnhandledExceptionEventArgs e) =>
		{
			NeoLog.Crash("AppDomain.UnhandledException", e.ExceptionObject as Exception);
		};
		TaskScheduler.UnobservedTaskException += (object? _, UnobservedTaskExceptionEventArgs e) =>
		{
			NeoLog.Crash("TaskScheduler.UnobservedTaskException", e.Exception);
			e.SetObserved();
		};
		VelopackApp.Build().Run();
		MainAsync(args).GetAwaiter().GetResult();
	}

	private static async Task MainAsync(string[] args)
	{
		ComWrappersSupport.InitializeComWrappers();
		App.StartInFriendsMode = args.Any((string arg) => string.Equals(arg, "--startup-friends", StringComparison.OrdinalIgnoreCase));
		bool createdNew;
		try
		{
			_appMutex = new Mutex(initiallyOwned: true, "NeoLauncher_Mutex", out createdNew);
		}
		catch (AbandonedMutexException ex)
		{
			_appMutex = ex.Mutex;
			createdNew = true;
		}
		if (!createdNew)
		{
			ForwardActivation(args);
			return;
		}
		Task.Run((Func<Task?>)WaitForActivations);
		RegisterUriProtocol();
		try
		{
			Application.Start((ApplicationInitializationCallbackParams p) =>
			{
				SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread()));
				new App();
			});
		}
		catch (Exception ex2)
		{
			NeoLog.Crash("Application.Start", ex2);
		}
		GC.KeepAlive(_appMutex);
	}

	private static void RegisterUriProtocol()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + "NeoLauncher".ToString());
			if (registryKey == null)
			{
				return;
			}
			string processPath = Environment.ProcessPath;
			if (string.IsNullOrEmpty(processPath))
			{
				return;
			}
			registryKey.SetValue("", "URL:NeoLauncher");
			registryKey.SetValue("URL Protocol", "");
			using RegistryKey registryKey2 = registryKey.CreateSubKey("DefaultIcon");
			registryKey2?.SetValue("", processPath + ",1");
			using RegistryKey registryKey3 = registryKey.CreateSubKey("shell\\open\\command");
			registryKey3?.SetValue("", "\"" + processPath + "\" \"%1\"");
		}
		catch
		{
		}
	}

	private static void ForwardActivation(string[] args)
	{
		string text = args.FirstOrDefault() ?? string.Empty;
		bool flag = text.StartsWith("NeoLauncher://", StringComparison.OrdinalIgnoreCase);
		bool flag2 = args.Any((string arg) => string.Equals(arg, "--startup-friends", StringComparison.OrdinalIgnoreCase));
		string value;
		if (flag)
		{
			value = text;
		}
		else
		{
			value = (flag2 ? "NeoLauncher://friends" : "NeoLauncher://open");
		}
		try
		{
			PInvoke.AllowSetForegroundWindow(uint.MaxValue);
			using NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "NeoLauncher_Pipe", PipeDirection.Out);
			namedPipeClientStream.Connect(3000);
			using StreamWriter streamWriter = new StreamWriter(namedPipeClientStream);
			streamWriter.WriteLine(value);
			streamWriter.Flush();
		}
		catch
		{
		}
	}

	private static async Task WaitForActivations()
	{
		_ = 1;
		while (true)
		{
			try
			{
				using NamedPipeServerStream server = new NamedPipeServerStream("NeoLauncher_Pipe", PipeDirection.In);
				await server.WaitForConnectionAsync();
				using StreamReader reader = new StreamReader(server);
				string uri = await reader.ReadLineAsync();
				if (string.IsNullOrEmpty(uri) || !uri.StartsWith("NeoLauncher://", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				App.DispatcherQueue?.TryEnqueue(async () =>
				{
					Uri uri2 = new Uri(uri);
					string text = uri2.Host.ToLowerInvariant();
					if (text == "friends")
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
							return;
						}
						catch
						{
							return;
						}
					}
					try
					{
						App.Window?.Activate();
					}
					catch
					{
					}
					try
					{
						App.Window?.BringToFront();
					}
					catch
					{
					}
					if (App.OnUriCallback != null && text != "open")
					{
						await App.OnUriCallback(uri2);
					}
				});
			}
			catch
			{
			}
		}
	}

	private static void SendCloseTabSignal()
	{
		InlineArray4<INPUT> buffer = default;
		buffer[0] = new INPUT
		{
			type = INPUT_TYPE.INPUT_KEYBOARD,
			Anonymous = new INPUT._Anonymous_e__Union
			{
				ki = new KEYBDINPUT
				{
					wVk = VIRTUAL_KEY.VK_CONTROL,
					wScan = 0,
					dwFlags = (KEYBD_EVENT_FLAGS)0u,
					time = 0u,
					dwExtraInfo = 0u
				}
			}
		};
		buffer[1] = new INPUT
		{
			type = INPUT_TYPE.INPUT_KEYBOARD,
			Anonymous = new INPUT._Anonymous_e__Union
			{
				ki = new KEYBDINPUT
				{
					wVk = VIRTUAL_KEY.VK_W,
					wScan = 0,
					dwFlags = (KEYBD_EVENT_FLAGS)0u,
					time = 0u,
					dwExtraInfo = 0u
				}
			}
		};
		buffer[2] = new INPUT
		{
			type = INPUT_TYPE.INPUT_KEYBOARD,
			Anonymous = new INPUT._Anonymous_e__Union
			{
				ki = new KEYBDINPUT
				{
					wVk = VIRTUAL_KEY.VK_W,
					wScan = 0,
					dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP,
					time = 0u,
					dwExtraInfo = 0u
				}
			}
		};
		buffer[3] = new INPUT
		{
			type = INPUT_TYPE.INPUT_KEYBOARD,
			Anonymous = new INPUT._Anonymous_e__Union
			{
				ki = new KEYBDINPUT
				{
					wVk = VIRTUAL_KEY.VK_CONTROL,
					wScan = 0,
					dwFlags = KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP,
					time = 0u,
					dwExtraInfo = 0u
				}
			}
		};
		PInvoke.SendInput((Span<INPUT>)buffer, Marshal.SizeOf<INPUT>());
	}
}
