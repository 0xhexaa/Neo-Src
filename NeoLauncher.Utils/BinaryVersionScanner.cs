using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using NeoLauncher.Extensions;
using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Utils;

public class BinaryVersionScanner
{
	public static Task<InstalledVersion?> ScanAsync(string binaryPath)
	{
		return Task.Run(() => Scan(binaryPath));
	}

	private static InstalledVersion? Scan(string binaryPath)
	{
		if (!File.Exists(binaryPath))
		{
			return null;
		}
		using FileStream peStream = File.OpenRead(binaryPath);
		using PEReader pEReader = new PEReader(peStream);
		byte[] array = pEReader.GetSectionData(".rdata").GetContent().ToArray();
		byte[] bytes = Encoding.Unicode.GetBytes("++Fortnite+Release-");
		int num;
		for (num = 0; num < array.Length; num += bytes.Length)
		{
			num = array.Scan(bytes, num);
			if (num == -1)
			{
				break;
			}
			int count = Math.Min(100, array.Length - num);
			if (GameVersion.TryParse(Encoding.Unicode.GetString(array, num, count), out GameVersion result) && result != null)
			{
				string text = new FileInfo(binaryPath).Directory?.Parent?.Parent?.Parent?.FullName;
				if (text == null)
				{
					return null;
				}
				return new InstalledVersion
				{
					Path = text,
					Version = result
				};
			}
		}
		return null;
	}
}
