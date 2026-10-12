using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NeoLauncher.Localization;
using NeoLauncher.Models.Exceptions;
using NeoLauncher.Models.Launcher;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Models.Services.Prism;
using NeoLauncher.Services.Account;
using NeoLauncher.Services.Launcher;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace NeoLauncher.Services.Game;

public class GameLauncher(AccountService accountService, LightswitchService lightswitchService)
{
	private Process? _gameProcess;

	private int _launchGuard;

	private CancellationTokenSource? _launchCts;

	private volatile bool _trackedGameRunning;

	private int _anyGameRunningCache;

	private long _anyGameCheckedAtTicks;

	private int _anyGameRefreshBusy;

	private volatile string _anyGameRunningPath = "";

	private static readonly long AnyGameCacheTtlTicks = TimeSpan.FromSeconds(2L).Ticks;

	private int _resolvedRootPid;

	private const int ProcessQueryLimitedInformation = 4096;

	private static readonly HttpClient VivoxHttpClient = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(120L)
	};

	private const string GameProcessName = "FortniteClient-Win64-Shipping";

	public bool IsGameActive
	{
		get
		{
			if (_gameProcess != null)
			{
				return _trackedGameRunning;
			}
			return false;
		}
	}

	public bool IsGameLaunching { get; private set; }

	public bool IsRunning
	{
		get
		{
			if (IsGameActive || IsGameLaunching)
			{
				return true;
			}
			RefreshAnyGameRunningIfStale();
			return Volatile.Read(in _anyGameRunningCache) == 1;
		}
	}

	public string RunningBuildPath
	{
		get
		{
			if (IsGameActive && !string.IsNullOrWhiteSpace(ActiveVersion?.Path))
			{
				return ActiveVersion.Path;
			}
			RefreshAnyGameRunningIfStale();
			return _anyGameRunningPath;
		}
	}

	public InstalledVersion? ActiveVersion { get; private set; }

	public event Action? GameStateChanged;

	private void RefreshAnyGameRunningIfStale()
	{
		if (DateTime.UtcNow.Ticks - Interlocked.Read(in _anyGameCheckedAtTicks) < AnyGameCacheTtlTicks || Interlocked.CompareExchange(ref _anyGameRefreshBusy, 1, 0) != 0)
		{
			return;
		}
		Task.Run(() =>
		{
			try
			{
				bool? flag = AnyGameProcessRunning();
				if (flag.HasValue)
				{
					Volatile.Write(ref _anyGameRunningCache, flag.Value ? 1 : 0);
					string text = (flag.Value ? ResolveRunningBuildRoot() : "");
					if (!string.Equals(text, _anyGameRunningPath, StringComparison.OrdinalIgnoreCase))
					{
						NeoLog.Info("game", flag.Value ? ("Untracked game detected, build root: " + (string.IsNullOrEmpty(text) ? "<unresolved>" : text)) : "No game process running.");
						_anyGameRunningPath = text;
					}
				}
				Interlocked.Exchange(ref _anyGameCheckedAtTicks, DateTime.UtcNow.Ticks);
			}
			finally
			{
				Interlocked.Exchange(ref _anyGameRefreshBusy, 0);
			}
		});
	}

	private static bool? AnyGameProcessRunning()
	{
		try
		{
			Process[] processesByName = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
			foreach (Process process in processesByName)
			{
				using (process)
				{
					try
					{
						if (!process.HasExited)
						{
							return true;
						}
					}
					catch
					{
						return true;
					}
				}
			}
			return false;
		}
		catch
		{
			return null;
		}
	}

	private string ResolveRunningBuildRoot()
	{
		try
		{
			using Process process = TryGetNewestGameProcess();
			if (process == null)
			{
				return "";
			}
			int id = process.Id;
			if (id == Volatile.Read(in _resolvedRootPid))
			{
				return _anyGameRunningPath;
			}
			string text = ResolveBuildRootFromCommandLine(id);
			if (string.IsNullOrEmpty(text))
			{
				string text2 = TryGetProcessImagePath(process);
				int num = text2.LastIndexOf("\\FortniteGame\\Binaries\\Win64\\", StringComparison.OrdinalIgnoreCase);
				if (num > 0)
				{
					text = text2.Substring(0, num);
				}
			}
			Volatile.Write(ref _resolvedRootPid, (!string.IsNullOrEmpty(text)) ? id : 0);
			return text;
		}
		catch
		{
			return "";
		}
	}

	private static string ResolveBuildRootFromCommandLine(int pid)
	{
		string text3;
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {pid}");
			using ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
			foreach (ManagementBaseObject item in managementObjectCollection)
			{
				using (item)
				{
					string text = item["CommandLine"] as string;
					if (string.IsNullOrWhiteSpace(text))
					{
						continue;
					}
					Match match = Regex.Match(text, "-basedir=(?:\"(?<quoted>[^\"]+)\"|(?<bare>\\S+))", RegexOptions.IgnoreCase);
					if (!match.Success)
					{
						continue;
					}
					string text2 = (match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["bare"].Value).Trim().TrimEnd(new char[2]
					{
						Path.DirectorySeparatorChar,
						Path.AltDirectorySeparatorChar
					});
					if (text2.EndsWith("\\FortniteGame\\Binaries\\Win64", StringComparison.OrdinalIgnoreCase))
					{
						text3 = text2;
						int length = "\\FortniteGame\\Binaries\\Win64".Length;
						text3 = text3.Substring(0, text3.Length - length);
					}
					else
					{
						text3 = text2;
					}
					goto IL_01c0;
				}
			}
		}
		catch (Exception ex)
		{
			NeoLog.Warn("game", $"Could not read the command line of PID {pid}: {ex.Message}");
		}
		return "";
		IL_01c0:
		return text3;
	}

	private static string TryGetProcessImagePath(Process process)
	{
		try
		{
			string text = process.MainModule?.FileName;
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		nint num = IntPtr.Zero;
		try
		{
			num = OpenProcessNative(4096, inheritHandle: false, process.Id);
			if (num == IntPtr.Zero)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(1024);
			int size = stringBuilder.Capacity;
			return QueryFullProcessImageNameNative(num, 0, stringBuilder, ref size) ? stringBuilder.ToString(0, size) : "";
		}
		catch
		{
			return "";
		}
		finally
		{
			if (num != IntPtr.Zero)
			{
				PInvoke.CloseHandle((HANDLE)num);
			}
		}
	}

	[DllImport("kernel32.dll", EntryPoint = "OpenProcess", SetLastError = true)]
	private static extern nint OpenProcessNative(int desiredAccess, bool inheritHandle, int processId);

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW", SetLastError = true)]
	private static extern bool QueryFullProcessImageNameNative(nint process, int flags, StringBuilder exeName, ref int size);

	public bool CancelLaunch()
	{
		CancellationTokenSource launchCts = _launchCts;
		if (launchCts == null)
		{
			return false;
		}
		try
		{
			if (launchCts.IsCancellationRequested)
			{
				return true;
			}
			launchCts.Cancel();
			return true;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	private static async Task SyncVivoxSdkAsync(string buildRoot, CancellationToken cancelToken)
	{
		InlineArray7<string> buffer = default;
		buffer[0] = buildRoot;
		buffer[1] = "Engine";
		buffer[2] = "Binaries";
		buffer[3] = "ThirdParty";
		buffer[4] = "Vivox";
		buffer[5] = "Win64";
		buffer[6] = "vivoxsdk_x64.dll";
		string target = Path.Combine(buffer);
		if (!File.Exists(target))
		{
			return;
		}
		string staged = target + ".new";
		try
		{
			if (await VivoxSdkMatchesETagAsync(target, cancelToken))
			{
				return;
			}
			using HttpResponseMessage response = await VivoxHttpClient.GetAsync("https://content-cdn.neofn.dev/launcher/vivoxsdk_x64.dll", HttpCompletionOption.ResponseHeadersRead, cancelToken);
			response.EnsureSuccessStatusCode();
			await using (FileStream stagedStream = File.Create(staged))
			{
				await response.Content.CopyToAsync(stagedStream, cancelToken);
			}
			if (await ComputeSha256Async(staged, cancelToken) == await ComputeSha256Async(target, cancelToken))
			{
				TryDeleteFile(staged);
				return;
			}
			string text = Path.Combine(Path.GetDirectoryName(target), "vivoxsdk_x64.old.dll");
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(target, text);
			try
			{
				File.Move(staged, target);
			}
			catch
			{
				File.Move(text, target);
				throw;
			}
			NeoLog.Info("game", "Replaced vivoxsdk_x64.dll from the CDN; previous copy kept as " + Path.GetFileName(text) + ".");
		}
		catch (OperationCanceledException)
		{
			TryDeleteFile(staged);
			throw;
		}
		catch (Exception ex2)
		{
			TryDeleteFile(staged);
			NeoLog.Warn("game", "Could not sync vivoxsdk_x64.dll, launching with the installed copy: " + ex2.Message);
		}
	}

	private static async Task<bool> VivoxSdkMatchesETagAsync(string target, CancellationToken cancelToken)
	{
		using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, "https://content-cdn.neofn.dev/launcher/vivoxsdk_x64.dll");
		using HttpResponseMessage response = await VivoxHttpClient.SendAsync(request, cancelToken);
		if (!response.IsSuccessStatusCode)
		{
			return false;
		}
		EntityTagHeaderValue eTag = response.Headers.ETag;
		if (eTag == null || eTag.IsWeak)
		{
			return false;
		}
		string tag = eTag.Tag.Trim('"');
		if (tag.Length != 32 || !tag.All(Uri.IsHexDigit))
		{
			return false;
		}
		long? contentLength = response.Content.Headers.ContentLength;
		if (contentLength.HasValue)
		{
			long valueOrDefault = contentLength.GetValueOrDefault();
			if (valueOrDefault != new FileInfo(target).Length)
			{
				return false;
			}
		}
		return string.Equals(await ComputeMd5Async(target, cancelToken), tag, StringComparison.OrdinalIgnoreCase);
	}

	private static async Task<string> ComputeMd5Async(string path, CancellationToken cancelToken)
	{
		string result;
		await using (FileStream stream = File.OpenRead(path))
		{
			result = Convert.ToHexStringLower(await MD5.HashDataAsync(stream, cancelToken));
		}
		return result;
	}

	private static void TryDeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static async Task<string> ComputeSha256Async(string path, CancellationToken cancelToken)
	{
		string result;
		await using (FileStream stream = File.OpenRead(path))
		{
			result = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancelToken));
		}
		return result;
	}

	public async Task LaunchAsync(InstalledVersion version, string? launchArgs = null)
	{
		if (IsGameActive)
		{
			throw new LaunchException(string.Format(Strings.LaunchAlreadyRunning, ActiveVersion.Version.BuildNumber), LaunchErrorCode.AlreadyRunning);
		}
		if (Interlocked.CompareExchange(ref _launchGuard, 1, 0) != 0)
		{
			throw new LaunchException("A launch is already in progress.", LaunchErrorCode.AlreadyRunning);
		}
		CancellationTokenSource cts = (_launchCts = new CancellationTokenSource());
		CancellationToken cancelToken = cts.Token;
		IsGameLaunching = true;
		ActiveVersion = version;
		GameStateChanged?.Invoke();
		PublishLauncherLifecyclePresence("launching");
		string failureReason = "Unknown";
		try
		{
			cancelToken.ThrowIfCancellationRequested();
			await lightswitchService.CheckStatusAsync();
			cancelToken.ThrowIfCancellationRequested();
			if (lightswitchService.IsBanned)
			{
				throw new LaunchException(string.IsNullOrWhiteSpace(lightswitchService.BanMessage) ? "You are banned from this service." : ("You are banned from this service. Reason: " + lightswitchService.BanMessage), LaunchErrorCode.Banned);
			}
			BanStatus banStatus = await App.PrismService.GetBanStatusAsync();
			if (banStatus != null && banStatus.Banned)
			{
				throw new LaunchException(string.IsNullOrWhiteSpace(banStatus.Reason) ? "You are banned from this service." : ("You are banned from this service. Reason: " + banStatus.Reason), LaunchErrorCode.Banned);
			}
			if (!lightswitchService.IsStatusKnown)
			{
				throw new LaunchException("Neo services could not be reached.", LaunchErrorCode.ServicesUnreachable);
			}
			if (!lightswitchService.IsServiceUp)
			{
				throw new LaunchException(string.IsNullOrWhiteSpace(lightswitchService.StatusMessage) ? "Neo services are currently down." : lightswitchService.StatusMessage, LaunchErrorCode.ServicesDown);
			}
			AccountService.ExchangeCode exchangeCode = (await accountService.GetExchangeCodeAsync()) ?? throw new LaunchException(Strings.LaunchExchangeCodeFailed, LaunchErrorCode.ExchangeCodeFailed);
			AccountService.ExchangeCode prismCode = (await accountService.GetExchangeCodeAsync()) ?? throw new LaunchException(Strings.LaunchExchangeCodeFailed, LaunchErrorCode.ExchangeCodeFailed);
			string win64Dir = Path.Combine(version.Path, "FortniteGame", "Binaries", "Win64");
			if (!File.Exists(Path.Combine(win64Dir, "FortniteClient-Win64-Shipping.exe")))
			{
				throw new LaunchException(Strings.LaunchBinaryNotFound, LaunchErrorCode.BinaryNotFound);
			}
			await SyncVivoxSdkAsync(version.Path, cancelToken);
			cancelToken.ThrowIfCancellationRequested();
			await App.PrismService.DownloadAssetsAsync();
			cancelToken.ThrowIfCancellationRequested();
			string path = Path.Combine(Config.DataPath, "FortniteClient-Win64-Shipping.exe");
			string text = Path.Combine(Config.DataPath, "NeoPrism.Bootstrapper.exe");
			if (!File.Exists(path))
			{
				throw new LaunchException("Neo couldn't download the game client.", LaunchErrorCode.PrismAssetMissing);
			}
			if (!File.Exists(text))
			{
				throw new LaunchException("Neo couldn't download the bootstrapper.", LaunchErrorCode.PrismAssetMissing);
			}
			InlineArray12<string> buffer = default;
			buffer[0] = "-basedir=\"" + win64Dir + "\"";
			buffer[1] = "-epicapp=Fortnite";
			buffer[2] = "-epicenv=Prod";
			buffer[3] = "-epicportal";
			buffer[4] = "-skippatchcheck";
			buffer[5] = "-nobe";
			buffer[6] = "-fromfl=eac";
			buffer[7] = "-AUTH_LOGIN=unused";
			buffer[8] = "-AUTH_TYPE=exchangecode";
			buffer[9] = "-AUTH_PASSWORD=" + exchangeCode.Code;
			buffer[10] = "-p=" + prismCode.Code;
			buffer[11] = "-fltoken=" + RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyz0123456789", 24);
			string text2 = string.Join(' ', (ReadOnlySpan<string?>)buffer);
			if (!string.IsNullOrWhiteSpace(launchArgs))
			{
				text2 = text2 + " " + launchArgs.Trim();
			}
			Process bootstrapperProcess = Process.Start(new ProcessStartInfo(text, text2)
			{
				WorkingDirectory = win64Dir
			}) ?? throw new LaunchException(Strings.LaunchProcessFailed, LaunchErrorCode.ProcessFailed);
			await bootstrapperProcess.WaitForExitAsync();
			try
			{
				_gameProcess = Process.GetProcessById(bootstrapperProcess.ExitCode);
			}
			catch (ArgumentException)
			{
			}
			if (_gameProcess == null)
			{
				throw new LaunchException(Strings.LaunchProcessFailed, LaunchErrorCode.ProcessFailed);
			}
			_trackedGameRunning = true;
			DateTime timeout = DateTime.UtcNow.AddSeconds(45.0);
			while (true)
			{
				if (_gameProcess.HasExited)
				{
					throw new LaunchException(Strings.LaunchProcessExited, LaunchErrorCode.ProcessExited);
				}
				_gameProcess.Refresh();
				if (_gameProcess.MainWindowHandle != IntPtr.Zero)
				{
					break;
				}
				if (DateTime.UtcNow > timeout)
				{
					throw new LaunchException(Strings.LaunchTimeout, LaunchErrorCode.Timeout);
				}
				await Task.Delay(500, cancelToken);
			}
			IsGameLaunching = false;
			GameStateChanged?.Invoke();
			PublishLauncherLifecyclePresence("game");
			App.AnalyticsService.Track(EventType.GameLaunched, new Dictionary<string, object> { ["version"] = version.Version.ToString() });
			WatchGameExit(_gameProcess, version);
		}
		catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
		{
			failureReason = LaunchErrorCode.Cancelled.ToString();
			Process gameProcess = _gameProcess;
			if (gameProcess != null)
			{
				try
				{
					if (!gameProcess.HasExited)
					{
						gameProcess.Kill(entireProcessTree: true);
					}
				}
				catch
				{
				}
			}
			_trackedGameRunning = false;
			NeoLog.Info("game", $"Launch of {version.Version} cancelled by the user.");
			throw new LaunchException("Launch cancelled.", LaunchErrorCode.Cancelled);
		}
		catch (Exception ex3)
		{
			failureReason = (ex3 as LaunchException)?.Code.ToString() ?? "Unknown";
			Process gameProcess2 = _gameProcess;
			if (gameProcess2 != null)
			{
				try
				{
					_trackedGameRunning = !gameProcess2.HasExited;
				}
				catch
				{
					_trackedGameRunning = false;
				}
			}
			throw;
		}
		finally
		{
			IsGameLaunching = false;
			_launchCts = null;
			cts.Dispose();
			Interlocked.Exchange(ref _launchGuard, 0);
			if (!IsGameActive)
			{
				_trackedGameRunning = false;
				Process gameProcess3 = _gameProcess;
				_gameProcess = null;
				try
				{
					gameProcess3?.Dispose();
				}
				catch
				{
				}
				ActiveVersion = null;
				GameStateChanged?.Invoke();
				PublishLauncherLifecyclePresence("launcher");
				App.AnalyticsService.Track(EventType.GameLaunchFailed, new Dictionary<string, object>
				{
					["version"] = version.Version.ToString(),
					["reason"] = failureReason
				});
			}
		}
	}

	private void WatchGameExit(Process process, InstalledVersion version)
	{
		Task.Run(async () =>
		{
			try
			{
				while (true)
				{
					if (_gameProcess != process)
					{
						return;
					}
					bool flag;
					try
					{
						flag = process.HasExited;
					}
					catch
					{
						flag = true;
					}
					if (flag)
					{
						break;
					}
					_trackedGameRunning = true;
					await Task.Delay(2000);
				}
				_trackedGameRunning = false;
				if (_gameProcess == process)
				{
					_gameProcess = null;
					ActiveVersion = null;
					try
					{
						process.Dispose();
					}
					catch
					{
					}
					GameStateChanged?.Invoke();
					PublishLauncherLifecyclePresence("launcher");
					App.AnalyticsService.Track(EventType.GameClosed, new Dictionary<string, object> { ["version"] = version.Version.ToString() });
					App.DispatcherQueue?.TryEnqueue(() =>
					{
						try
						{
							App.Window?.BringToFront();
						}
						catch
						{
						}
					});
				}
			}
			catch
			{
				_trackedGameRunning = false;
			}
		});
	}

	private void PublishLauncherLifecyclePresence(string launcherActivity)
	{
		Task.Run(async () =>
		{
			_ = 1;
			try
			{
				AccountService.UserRecord user = accountService.CurrentUser;
				string text = await accountService.GetAccessTokenAsync();
				if ((object)user != null && !string.IsNullOrWhiteSpace(text))
				{
					await App.PresenceService.PublishLifecyclePresenceAsync(user.Id, text, launcherActivity);
				}
			}
			catch
			{
			}
		});
	}

	private static Process? TryGetNewestGameProcess()
	{
		try
		{
			Process process = null;
			Process[] processesByName = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
			foreach (Process process2 in processesByName)
			{
				bool flag = false;
				try
				{
					if (!process2.HasExited && (process == null || process2.StartTime > process.StartTime))
					{
						process?.Dispose();
						process = process2;
						flag = true;
					}
				}
				catch
				{
				}
				if (!flag)
				{
					process2.Dispose();
				}
			}
			return process;
		}
		catch
		{
			return null;
		}
	}
}
