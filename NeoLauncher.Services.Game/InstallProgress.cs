using System;
using BuildPatchServices;

namespace NeoLauncher.Services.Game;

public class InstallProgress
{
	public EBuildPatchState State { get; init; }

	public bool IsPaused { get; init; }

	public float Progress { get; init; }

	public long Downloaded { get; init; }

	public long TotalRequired { get; init; }

	public double SpeedMbps { get; init; }

	public double EtaSeconds { get; init; }

	public string DownloadedText => FormatBytes(Downloaded);

	public string TotalRequiredText => FormatBytes(TotalRequired);

	public string SpeedText
	{
		get
		{
			if (IsPaused || !(SpeedMbps > 0.1))
			{
				return "";
			}
			return $"{SpeedMbps:F1} Mbps";
		}
	}

	public string PercentText => $"{(double)Progress * 100.0:F1}%";

	public string EtaText
	{
		get
		{
			if (IsPaused)
			{
				return "N/A";
			}
			if (EtaSeconds <= 0.0 || EtaSeconds >= 86400.0)
			{
				return "N/A";
			}
			TimeSpan timeSpan = TimeSpan.FromSeconds(EtaSeconds);
			if (timeSpan.Hours <= 0)
			{
				if (timeSpan.Minutes > 0)
				{
					return $"{timeSpan.Minutes}m {timeSpan.Seconds:D2}s";
				}
				return $"{timeSpan.Seconds}s";
			}
			return $"{timeSpan.Hours}h {timeSpan.Minutes:D2}m";
		}
	}

	public string StatusText
	{
		get
		{
			if (IsPaused)
			{
				return "Paused";
			}
			return State switch
			{
				EBuildPatchState.Queued => "Queued", 
				EBuildPatchState.Initializing => "Initializing...", 
				EBuildPatchState.Resuming => "Resuming...", 
				EBuildPatchState.Downloading => "Downloading...", 
				EBuildPatchState.Installing => "Installing...", 
				EBuildPatchState.MovingToInstall => "Moving files...", 
				EBuildPatchState.SettingAttributes => "Setting attributes...", 
				EBuildPatchState.BuildVerification => "Verifying...", 
				EBuildPatchState.CleanUp => "Cleaning up...", 
				EBuildPatchState.PrerequisitesInstall => "Prerequisites...", 
				EBuildPatchState.Completed => "Complete", 
				_ => "Working...", 
			};
		}
	}

	private static string FormatBytes(long bytes)
	{
		if (bytes <= 0)
		{
			return "0 B";
		}
		string[] array = new string[5] { "B", "KB", "MB", "GB", "TB" };
		int num = 0;
		double num2 = bytes;
		while (num2 >= 1024.0 && num < array.Length - 1)
		{
			num++;
			num2 /= 1024.0;
		}
		return $"{num2:F2} {array[num]}";
	}
}
