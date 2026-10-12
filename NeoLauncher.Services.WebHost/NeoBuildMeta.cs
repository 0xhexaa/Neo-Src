using System;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Services.WebHost;

internal static class NeoBuildMeta
{
	private static readonly Regex CardIdRegex = new Regex("[^a-z0-9]+", RegexOptions.IgnoreCase);

	public static GameVersion? Parse(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		if (!GameVersion.TryParse(raw.Contains("++Fortnite+Release-", StringComparison.OrdinalIgnoreCase) ? raw : ("++Fortnite+Release-" + raw), out GameVersion result))
		{
			return null;
		}
		return result;
	}

	public static void Enrich(JsonArray builds)
	{
		foreach (JsonNode build in builds)
		{
			if (build is JsonObject jsonObject)
			{
				string text = jsonObject["version"]?.GetValue<string>() ?? "";
				bool canLaunch = jsonObject["isLive"]?.GetValue<bool>() ?? false;
				GameVersion gameVersion = Parse(text);
				if ((object)gameVersion != null)
				{
					Stamp(jsonObject, gameVersion, canLaunch);
				}
				else
				{
					StampUnknown(jsonObject, text);
				}
			}
		}
	}

	public static JsonObject Describe(string versionString, bool canLaunch = false)
	{
		JsonObject jsonObject = new JsonObject();
		GameVersion gameVersion = Parse(versionString);
		if ((object)gameVersion != null)
		{
			Stamp(jsonObject, gameVersion, canLaunch);
		}
		else
		{
			StampUnknown(jsonObject, versionString);
		}
		return jsonObject;
	}

	public static string Identity(GameVersion version)
	{
		return $"{DisplayBuildNumber(version)}-CL-{version.CL}";
	}

	private static string DisplayBuildNumber(GameVersion version)
	{
		string[] array = version.BuildNumber.Split('.');
		for (int i = 0; i < array.Length; i++)
		{
			if (int.TryParse(array[i], out var result))
			{
				array[i] = result.ToString();
			}
		}
		return string.Join(".", array);
	}

	private static void Stamp(JsonObject build, GameVersion version, bool canLaunch)
	{
		string text = Identity(version);
		int num = SeasonMajor(version);
		build["versionIdentity"] = text;
		build["displayVersion"] = text;
		build["name"] = version.Name;
		build["seasonTitle"] = version.Name;
		build["image"] = SeasonImage(num);
		build["openedImage"] = OpenedImage(num);
		build["seasonMajor"] = num;
		build["chapterId"] = $"chapter-{version.Chapter}";
		build["chapterTitle"] = $"Chapter {version.Chapter}";
		build["sortValue"] = version.CL;
		build["chapterSortValue"] = version.Chapter;
		build["cardId"] = CardId(text);
		build["build"] = "Release " + text;
		build["canLaunch"] = canLaunch;
		build["downloadable"] = true;
	}

	private static void StampUnknown(JsonObject build, string versionString)
	{
		if (TryExtractBuildNumber(versionString, out string buildNumber))
		{
			StampBuildNumberOnly(build, buildNumber);
			return;
		}
		string text = (string.IsNullOrWhiteSpace(versionString) ? "unknown" : versionString);
		build["versionIdentity"] = text;
		build["displayVersion"] = versionString;
		build["build"] = (string.IsNullOrWhiteSpace(versionString) ? "Release Unknown" : ("Release " + versionString));
		build["name"] = "Unknown Build";
		build["seasonTitle"] = "Unknown Build";
		build["image"] = "";
		build["openedImage"] = "";
		build["seasonMajor"] = null;
		build["chapterId"] = "unknown";
		build["chapterTitle"] = "Unknown";
		build["sortValue"] = int.MaxValue;
		build["chapterSortValue"] = 999;
		build["cardId"] = CardId(text);
		build["canLaunch"] = false;
		build["downloadable"] = true;
	}

	private static bool TryExtractBuildNumber(string raw, out string buildNumber)
	{
		buildNumber = "";
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		Match match = Regex.Match(raw.Trim(), "(?<!\\d)(?<major>\\d{1,2})(?:[._-](?<minor>\\d{1,2}))?(?!\\d)");
		if (!match.Success)
		{
			return false;
		}
		string value = match.Groups["major"].Value;
		string s = (match.Groups["minor"].Success ? match.Groups["minor"].Value : "00");
		if (!int.TryParse(value, out var result) || result <= 0)
		{
			return false;
		}
		if (!int.TryParse(s, out var result2))
		{
			return false;
		}
		buildNumber = $"{result}.{result2}";
		return true;
	}

	private static void StampBuildNumberOnly(JsonObject build, string buildNumber)
	{
		int result = 0;
		int.TryParse(buildNumber.Split('.')[0], out result);
		int num = ChapterFromMajor(result);
		int value = SeasonFromMajor(result, num, buildNumber);
		string text = ((num > 0) ? $"Chapter {num}" : "Chapter 1");
		string text2 = $"{text} Season {value}";
		build["versionIdentity"] = buildNumber;
		build["displayVersion"] = buildNumber;
		build["build"] = "Release " + buildNumber;
		build["name"] = text2;
		build["seasonTitle"] = text2;
		build["image"] = SeasonImage(result);
		build["openedImage"] = OpenedImage(result);
		build["seasonMajor"] = result;
		build["chapterId"] = $"chapter-{num}";
		build["chapterTitle"] = text;
		build["sortValue"] = result * 100000 + BuildMinorSort(buildNumber);
		build["chapterSortValue"] = num;
		build["cardId"] = CardId(buildNumber);
		build["canLaunch"] = false;
		build["downloadable"] = true;
	}

	private static int ChapterFromMajor(int major)
	{
		if (major <= 22)
		{
			if (major > 10)
			{
				if (major <= 18)
				{
					return 2;
				}
				return 3;
			}
			return 1;
		}
		if (major <= 32)
		{
			if (major <= 27)
			{
				return 4;
			}
			return 5;
		}
		if (major <= 38)
		{
			return 6;
		}
		return 7;
	}

	private static int SeasonFromMajor(int major, int chapter, string buildNumber)
	{
		return chapter switch
		{
			1 => (buildNumber == "1.11") ? 2 : major, 
			2 => major - 10, 
			3 => major - 18, 
			4 => major - 22, 
			5 => major - 27, 
			6 => major - 32, 
			_ => major - 38, 
		};
	}

	private static int BuildMinorSort(string buildNumber)
	{
		string[] array = buildNumber.Split('.');
		if (array.Length < 2)
		{
			return 0;
		}
		if (!int.TryParse(array[1], out var result))
		{
			return 0;
		}
		return result;
	}

	private static int SeasonMajor(GameVersion version)
	{
		if (!int.TryParse(version.BuildNumber.Split('.')[0], out var result))
		{
			return 0;
		}
		return result;
	}

	private static string SeasonImage(int major)
	{
		if (major != 10)
		{
			return $"/assets/seasons/season-{major}.png";
		}
		return "/assets/seasons/season-x.png";
	}

	private static string OpenedImage(int major)
	{
		return $"/assets/seasons/opened/season{major}-card.png";
	}

	private static string CardId(string identity)
	{
		return "neo-build-" + CardIdRegex.Replace(identity, "-").ToLowerInvariant();
	}
}
