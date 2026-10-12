using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BuildPatchServices;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Services.Launcher;

namespace NeoLauncher.Services.Game;

public class InstallService
{
	private static readonly string LogPath = Path.Combine(Config.DataPath, "Logs", "Download.log");

	private const long MaxDownloadLogBytes = 8388608L;

	private static StreamWriter? _logWriter;

	private static readonly object _logLock = new object();

	private LauncherService _launcherService;

	private volatile FBuildPatchInstaller? _installer;

	private Thread? _pollThread;

	private volatile bool _polling;

	private volatile bool _cancelRequested;

	private const double CancelDrainSeconds = 20.0;

	private long _totalInstallBytes;

	private float _lastReportedProgress;

	private const string CloudDirSuffix = "Builds/Fortnite/CloudDir";

	public bool IsInstalling
	{
		get
		{
			if (_polling && _installer != null)
			{
				return !_installer.IsComplete;
			}
			return false;
		}
	}

	public bool IsPaused => _installer?.IsPaused ?? false;

	public BuildInfo? ActiveBuild { get; private set; }

	public event Action<InstallProgress>? ProgressUpdated;

	public event Action<InstallResult>? InstallCompleted;

	public InstallService(LauncherService launcherService)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
		NeoLog.CapExternalLog(LogPath, 8388608L);
		_logWriter = new StreamWriter(LogPath, append: true)
		{
			AutoFlush = true
		};
		_launcherService = launcherService;
		Log.OnLog = WriteLog;
	}

	public async Task<bool> StartInstallAsync(BuildInfo build, string installDirectory)
	{
		string[] array = await _launcherService.GetDistributionPointsAsync();
		if (IsInstalling || array.Length == 0)
		{
			return false;
		}
		string pathOrUrl = array[0] + "/" + build.ManifestPath.TrimStart('/');
		string[] cloudDirs = array.Select((string d) => d + "/Builds/Fortnite/CloudDir").ToArray();
		FBuildPatchAppManifest fBuildPatchAppManifest = await LoadManifestAsync(pathOrUrl).ConfigureAwait(continueOnCapturedContext: false);
		if (fBuildPatchAppManifest == null)
		{
			return false;
		}
		string text = fBuildPatchAppManifest.GetVersionString();
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			text = text.Replace(oldChar, '_');
		}
		string text2 = Path.Combine(installDirectory, text);
		string stagingDir = Path.Combine(text2, ".egstore");
		ActiveBuild = build;
		return await StartCoreAsync(fBuildPatchAppManifest, cloudDirs, text2, stagingDir, isRepair: false);
	}

	public async Task<bool> StartRepairAsync(BuildInfo build, string installPath)
	{
		string[] array = await _launcherService.GetDistributionPointsAsync();
		if (IsInstalling || array.Length == 0)
		{
			return false;
		}
		string pathOrUrl = array[0] + "/" + build.ManifestPath.TrimStart('/');
		string[] cloudDirs = array.Select((string d) => d + "/Builds/Fortnite/CloudDir").ToArray();
		FBuildPatchAppManifest fBuildPatchAppManifest = await LoadManifestAsync(pathOrUrl).ConfigureAwait(continueOnCapturedContext: false);
		if (fBuildPatchAppManifest == null)
		{
			return false;
		}
		string stagingDir = Path.Combine(installPath, ".egstore");
		ActiveBuild = build;
		return await StartCoreAsync(fBuildPatchAppManifest, cloudDirs, installPath, stagingDir, isRepair: true);
	}

	private Task<bool> StartCoreAsync(FBuildPatchAppManifest manifest, string[] cloudDirs, string installDir, string stagingDir, bool isRepair)
	{
		List<string> list = new List<string>();
		manifest.GetFileList(list);
		_totalInstallBytes = 0L;
		foreach (string item2 in list)
		{
			_totalInstallBytes += manifest.GetFileSize(item2);
		}
		FInstallerAction item = (isRepair ? FInstallerAction.MakeRepair(manifest) : FInstallerAction.MakeInstall(manifest, new HashSet<string>()));
		FBuildInstallerConfiguration configuration = new FBuildInstallerConfiguration(new List<FInstallerAction> { item })
		{
			InstallDirectory = installDir,
			StagingDirectory = stagingDir,
			BackupDirectory = string.Empty,
			CloudDirectories = new List<string>(cloudDirs),
			ChunkDatabaseFiles = new List<string>(),
			InstallMode = EInstallMode.NonDestructiveInstall,
			VerifyMode = EVerifyMode.ShaVerifyAllFiles,
			DeltaPolicy = EDeltaPolicy.Skip
		};
		DateTime startedAt = DateTime.UtcNow;
		_lastReportedProgress = 0f;
		_installer = new FBuildPatchInstaller(configuration, (IBuildInstaller _) =>
		{
		}, (IBuildInstaller inst) =>
		{
			ActiveBuild = null;
			FBuildInstallStats buildStatistics = inst.GetBuildStatistics();
			string versionString = manifest.GetVersionString();
			if (inst.CompletedSuccessfully)
			{
				if (!isRepair)
				{
					double totalSeconds = (DateTime.UtcNow - startedAt).TotalSeconds;
					double num = ((totalSeconds > 0.0) ? Math.Round((double)_totalInstallBytes / totalSeconds / 1048576.0, 2) : 0.0);
					App.AnalyticsService.Track(EventType.BuildDownloaded, new Dictionary<string, object>
					{
						["version"] = versionString,
						["durationSeconds"] = (int)totalSeconds,
						["avgSpeedMbps"] = num
					});
				}
			}
			else
			{
				App.AnalyticsService.Track(EventType.BuildDownloadFailed, new Dictionary<string, object>
				{
					["version"] = versionString,
					["error"] = buildStatistics.ErrorCode
				});
			}
			InstallCompleted?.Invoke(new InstallResult
			{
				Success = inst.CompletedSuccessfully,
				Cancelled = inst.IsCanceled,
				ErrorCode = buildStatistics.ErrorCode,
				ErrorText = buildStatistics.FailureReasonText,
				InstallPath = installDir,
				Version = versionString,
				BuildId = manifest.GetBuildId()
			});
		});
		if (!_installer.StartInstallation())
		{
			return Task.FromResult(result: false);
		}
		FBuildPatchInstaller polled = _installer;
		_polling = true;
		_pollThread = new Thread(() =>
		{
			PollProgress(polled);
		})
		{
			IsBackground = true,
			Name = "InstallPoll"
		};
		_pollThread.Start();
		return Task.FromResult(result: true);
	}

	public bool TogglePause()
	{
		if (_installer == null)
		{
			return false;
		}
		return _installer.TogglePauseInstall();
	}

	public void Cancel()
	{
		FBuildPatchInstaller installer = _installer;
		if (installer == null)
		{
			_polling = false;
			return;
		}
		_cancelRequested = true;
		installer.CancelInstall();
	}

	private void PollProgress(FBuildPatchInstaller inst)
	{
		DateTime? dateTime = null;
		try
		{
			while (_polling && _installer == inst)
			{
				if (inst.IsComplete)
				{
					if (inst.CompletedSuccessfully)
					{
						ProgressUpdated?.Invoke(new InstallProgress
						{
							State = EBuildPatchState.Completed,
							IsPaused = false,
							Progress = 1f,
							Downloaded = inst.TotalDownloaded,
							TotalRequired = ((inst.TotalDownloadRequired > 0) ? inst.TotalDownloadRequired : _totalInstallBytes),
							SpeedMbps = 0.0,
							EtaSeconds = 0.0
						});
					}
					inst.Tick();
					break;
				}
				if (_cancelRequested)
				{
					DateTime valueOrDefault = dateTime.GetValueOrDefault();
					if (!dateTime.HasValue)
					{
						valueOrDefault = DateTime.UtcNow.AddSeconds(20.0);
						dateTime = valueOrDefault;
					}
					valueOrDefault = DateTime.UtcNow;
					DateTime? dateTime2 = dateTime;
					if (valueOrDefault > dateTime2)
					{
						WriteLog("InstallService", $"Cancelled install did not finish within {20.0:F0}s; releasing it and reporting cancelled.");
						InstallCompleted?.Invoke(new InstallResult
						{
							Success = false,
							Cancelled = true,
							ErrorCode = "UC-Abandoned",
							ErrorText = "Cancelled."
						});
						break;
					}
				}
				else
				{
					PublishProgress(inst);
				}
				if (!inst.Tick())
				{
					break;
				}
				Thread.Sleep(250);
			}
		}
		finally
		{
			if (_installer == inst)
			{
				_polling = false;
				_installer = null;
				_cancelRequested = false;
				ActiveBuild = null;
			}
		}
	}

	private void PublishProgress(FBuildPatchInstaller inst)
	{
		EBuildPatchState state = inst.State;
		float num = inst.UpdateProgress;
		if (num >= 0f)
		{
			_lastReportedProgress = num;
		}
		else
		{
			num = _lastReportedProgress;
		}
		long totalRequired = ((inst.TotalDownloadRequired > 0) ? inst.TotalDownloadRequired : _totalInstallBytes);
		ProgressUpdated?.Invoke(new InstallProgress
		{
			State = state,
			IsPaused = inst.IsPaused,
			Progress = num,
			Downloaded = inst.TotalDownloaded,
			TotalRequired = totalRequired,
			SpeedMbps = inst.DownloadSpeedMbps,
			EtaSeconds = inst.EstimatedTimeRemaining
		});
	}

	private static async Task<FBuildPatchAppManifest?> LoadManifestAsync(string pathOrUrl)
	{
		if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			try
			{
				using HttpClient http = new HttpClient();
				return FBuildPatchAppManifest.LoadFromJson(await http.GetStringAsync(pathOrUrl).ConfigureAwait(continueOnCapturedContext: false));
			}
			catch
			{
				return null;
			}
		}
		return await Task.Run(() => FBuildPatchAppManifest.LoadFromFile(pathOrUrl)).ConfigureAwait(continueOnCapturedContext: false);
	}

	private static void WriteLog(string category, string message)
	{
		lock (_logLock)
		{
			string value = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";
			_logWriter?.WriteLine(value);
		}
	}
}
