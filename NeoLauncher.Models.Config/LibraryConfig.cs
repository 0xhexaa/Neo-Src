using System.Collections.Generic;
using NeoLauncher.Models.Launcher;

namespace NeoLauncher.Models.Config;

public class LibraryConfig
{
	public List<InstalledVersion> Items { get; set; } = new List<InstalledVersion>();
}
