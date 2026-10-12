using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NeoLauncher.Models.Launcher;

[JsonConverter(typeof(GameVersionConverter))]
public class GameVersion : IEquatable<GameVersion>
{
	private static readonly Regex ParseRegex = new Regex("\\+\\+Fortnite\\+Release-(?<BuildNumber>[^-]+)-CL-(?<CL>\\d+)");

	private static readonly Dictionary<int, string> CLMap = new Dictionary<int, string>
	{
		{ 3541083, "1.2" },
		{ 3700114, "1.7" },
		{ 3724489, "1.8" },
		{ 3757339, "1.9" },
		{ 3775276, "1.9.1" },
		{ 3790078, "1.10" },
		{ 3807424, "1.11" },
		{ 3825894, "2.1" },
		{ 3841827, "2.2" },
		{ 3847564, "2.3" },
		{ 3856999, "2.3.2" },
		{ 3858292, "2.4" },
		{ 3870737, "2.4.2" },
		{ 3889387, "2.5" },
		{ 3901517, "3.0" },
		{ 3915963, "3.1" },
		{ 3917250, "3.1" },
		{ 3929794, "3.2" },
		{ 3935073, "3.2" },
		{ 3942182, "3.3" },
		{ 3948073, "3.3" },
		{ 3968866, "3.4" },
		{ 3973340, "3.4" }
	};

	public int CL { get; set; }

	public string BuildNumber { get; set; } = string.Empty;

	public int Chapter => ChapterFromCL(CL);

	public int Season => SeasonFromVersion(BuildNumber, Chapter);

	public string Name => $"Chapter {Chapter} Season {Season}";

	public GameVersion()
	{
	}

	public GameVersion(string versionString)
	{
		Match match = ParseRegex.Match(versionString);
		if (!match.Success)
		{
			throw new FormatException("Invalid version string: " + versionString);
		}
		string value = match.Groups["BuildNumber"].Value;
		CL = int.Parse(match.Groups["CL"].Value);
		BuildNumber = ((!double.TryParse(value, out var _) && CLMap.TryGetValue(CL, out string value2)) ? value2 : value);
	}

	public override string ToString()
	{
		return $"++Fortnite+Release-{BuildNumber}-CL-{CL}";
	}

	public static bool TryParse(string versionString, out GameVersion? result)
	{
		Match match = ParseRegex.Match(versionString);
		if (!match.Success)
		{
			result = null;
			return false;
		}
		int num = int.Parse(match.Groups["CL"].Value);
		string value = match.Groups["BuildNumber"].Value;
		result = new GameVersion
		{
			CL = num,
			BuildNumber = ((!double.TryParse(value, out var _) && CLMap.TryGetValue(num, out string value2)) ? value2 : value)
		};
		return true;
	}

	private static int ChapterFromCL(int cl)
	{
		if (cl < 23344627)
		{
			if (cl >= 9562734)
			{
				if (cl < 18335626)
				{
					return 2;
				}
				return 3;
			}
			return 1;
		}
		if (cl < 38324112)
		{
			if (cl < 29915848)
			{
				return 4;
			}
			return 5;
		}
		if (cl < 48444883)
		{
			return 6;
		}
		return 7;
	}

	private static int SeasonFromVersion(string version, int chapter)
	{
		int num = (int.TryParse(version.Split('.')[0], out var result) ? result : 0);
		return chapter switch
		{
			0 => num, 
			1 => (version == "1.11") ? 2 : num, 
			2 => num - 10, 
			3 => num - 18, 
			4 => num - 22, 
			5 => num - 27, 
			6 => num - 32, 
			_ => num - 38, 
		};
	}

	public bool Equals(GameVersion? other)
	{
		if ((object)other == null)
		{
			return false;
		}
		if ((object)this == other)
		{
			return true;
		}
		return CL == other.CL;
	}

	public override bool Equals(object? obj)
	{
		return Equals(obj as GameVersion);
	}

	public override int GetHashCode()
	{
		return CL.GetHashCode();
	}

	public static bool operator ==(GameVersion? left, GameVersion? right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(GameVersion? left, GameVersion? right)
	{
		return !(left == right);
	}
}
