using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeoLauncher.Models.Exceptions;
using NeoLauncher.Models.Services.Launcher;
using NeoLauncher.Services.Launcher;
using Velopack;

namespace NeoLauncher.Services.Updates;

public class UpdateService
{
	private readonly UpdateManager _manager;

	public bool IsInstalled => _manager.IsInstalled;

	public string? CurrentVersion => _manager.CurrentVersion?.ToString();

	public UpdateService(LauncherService launcherService)
	{
		_manager = new UpdateManager(new NeoUpdateSource(launcherService));
	}

	public async Task<UpdateInfo?> CheckAsync()
	{
		if (!_manager.IsInstalled)
		{
			return null;
		}
		try
		{
			return await _manager.CheckForUpdatesAsync();
		}
		catch (Exception ex)
		{
			throw new UpdateException("Failed to check for updates: " + ex.Message, ex);
		}
	}

	public async Task DownloadAsync(UpdateInfo info, Action<int>? progress = null)
	{
		try
		{
			await _manager.DownloadUpdatesAsync(info, (progress == null) ? null : ((Action<int>)((int p) =>
			{
				progress(p);
			})));
		}
		catch (Exception ex)
		{
			App.AnalyticsService.Track(EventType.LauncherUpdateFailed, new Dictionary<string, object> { ["version"] = info.TargetFullRelease.Version.ToString() });
			throw new UpdateException($"Failed to download update {info.TargetFullRelease.Version}: {ex.Message}", ex);
		}
	}

	public void ApplyOnExit(UpdateInfo info)
	{
		try
		{
			_manager.WaitExitThenApplyUpdates(info);
			App.AnalyticsService.Track(EventType.LauncherUpdated, new Dictionary<string, object> { ["version"] = info.TargetFullRelease.Version.ToString() });
		}
		catch (Exception ex)
		{
			App.AnalyticsService.Track(EventType.LauncherUpdateFailed, new Dictionary<string, object> { ["version"] = info.TargetFullRelease.Version.ToString() });
			throw new UpdateException($"Failed to stage update {info.TargetFullRelease.Version}: {ex.Message}", ex);
		}
	}

	public void ApplyAndRestart(UpdateInfo info)
	{
		try
		{
			App.AnalyticsService.Track(EventType.LauncherUpdated, new Dictionary<string, object> { ["version"] = info.TargetFullRelease.Version.ToString() });
			_manager.ApplyUpdatesAndRestart(info);
		}
		catch (Exception ex)
		{
			App.AnalyticsService.Track(EventType.LauncherUpdateFailed, new Dictionary<string, object> { ["version"] = info.TargetFullRelease.Version.ToString() });
			throw new UpdateException($"Failed to apply update {info.TargetFullRelease.Version}: {ex.Message}", ex);
		}
	}
}
