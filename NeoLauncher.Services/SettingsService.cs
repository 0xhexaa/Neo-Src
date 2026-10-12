using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using NeoLauncher.Localization;
using NeoLauncher.Models;

namespace NeoLauncher.Services;

public class SettingsService
{
	private static readonly string SettingsPath = Path.Combine(Config.DataPath, "Settings.json");

	public static readonly List<(string Tag, string DisplayName)> SupportedLanguages;

	public AppSettings Settings { get; private set; }

	public SettingsService()
	{
		try
		{
			if (File.Exists(SettingsPath))
			{
				string json = File.ReadAllText(SettingsPath);
				Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
			}
			else
			{
				Settings = new AppSettings();
			}
		}
		catch
		{
			Settings = new AppSettings();
		}
		if (string.IsNullOrEmpty(Settings.Language))
		{
			Settings.Language = ResolveSystemLanguage();
		}
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
			string contents = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
			{
				WriteIndented = true
			});
			File.WriteAllText(SettingsPath, contents);
		}
		catch
		{
		}
	}

	public void ApplyLanguage()
	{
		Strings.SetLanguage(Settings.Language);
	}

	public int GetLanguageIndex()
	{
		for (int i = 0; i < SupportedLanguages.Count; i++)
		{
			if (string.Equals(SupportedLanguages[i].Tag, Settings.Language, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return 0;
	}

	private static string ResolveSystemLanguage()
	{
		for (CultureInfo cultureInfo = CultureInfo.CurrentUICulture; cultureInfo != CultureInfo.InvariantCulture; cultureInfo = cultureInfo.Parent)
		{
			foreach (var supportedLanguage in SupportedLanguages)
			{
				string item = supportedLanguage.Tag;
				if (string.Equals(cultureInfo.Name, item, StringComparison.OrdinalIgnoreCase))
				{
					return item;
				}
			}
		}
		return "en-US";
	}

	static SettingsService()
	{
		int num = 4;
		List<(string, string)> list = new List<(string, string)>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<(string, string)> span = CollectionsMarshal.AsSpan(list);
		span[0] = ("en-US", "English (United States)");
		span[1] = ("es", "Español");
		span[2] = ("fr", "Français");
		span[3] = ("de", "Deutsch");
		SupportedLanguages = list;
	}
}
