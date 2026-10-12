using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using NeoLauncher.Models;
using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Services.Game;

public class LibraryService
{
	private readonly string ConfigPath = Path.Combine(Config.DataPath, "Library.json");

	private readonly string PendingPath = Path.Combine(Config.DataPath, "Downloads.json");

	public event Action? LibraryChanged;

	public void Save(HashSet<InstalledVersion> items)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
		File.WriteAllText(ConfigPath, JsonSerializer.Serialize(items, new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}));
		LibraryChanged?.Invoke();
	}

	public HashSet<InstalledVersion> Load()
	{
		if (!File.Exists(ConfigPath))
		{
			return new HashSet<InstalledVersion>();
		}
		try
		{
			return JsonSerializer.Deserialize<HashSet<InstalledVersion>>(File.ReadAllText(ConfigPath)) ?? new HashSet<InstalledVersion>();
		}
		catch
		{
			return new HashSet<InstalledVersion>();
		}
	}

	public void SavePending(PendingDownload download)
	{
		List<PendingDownload> list = LoadPending();
		list.RemoveAll((PendingDownload pending) => pending.Build.Version.BuildNumber == download.Build.Version.BuildNumber);
		list.Add(download);
		WritePending(list);
	}

	public void RemovePending(string buildNumber)
	{
		List<PendingDownload> list = LoadPending();
		list.RemoveAll((PendingDownload pending) => pending.Build.Version.BuildNumber == buildNumber);
		WritePending(list);
	}

	public List<PendingDownload> LoadPending()
	{
		if (!File.Exists(PendingPath))
		{
			return new List<PendingDownload>();
		}
		try
		{
			return JsonSerializer.Deserialize<List<PendingDownload>>(File.ReadAllText(PendingPath)) ?? new List<PendingDownload>();
		}
		catch
		{
			return new List<PendingDownload>();
		}
	}

	private void WritePending(List<PendingDownload> downloads)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(PendingPath));
		File.WriteAllText(PendingPath, JsonSerializer.Serialize(downloads, new JsonSerializerOptions
		{
			WriteIndented = true,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
		}));
	}
}
