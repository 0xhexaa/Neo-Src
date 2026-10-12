using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;

namespace NeoLauncher.Services;

public static class SystemCapability
{
	public sealed record Snapshot(int LogicalCores, double TotalMemoryGb, string GpuName, double GpuMemoryGb, bool GpuLooksIntegrated, int Score, bool IsLowEnd, string[] Reasons);

	private struct MEMORYSTATUSEX
	{
		public uint dwLength;

		public uint dwMemoryLoad;

		public ulong ullTotalPhys;

		public ulong ullAvailPhys;

		public ulong ullTotalPageFile;

		public ulong ullAvailPageFile;

		public ulong ullTotalVirtual;

		public ulong ullAvailVirtual;

		public ulong ullAvailExtendedVirtual;
	}

	private static Snapshot? _cached;

	private static readonly object Gate = new object();

	public static Snapshot Get()
	{
		if ((object)_cached != null)
		{
			return _cached;
		}
		lock (Gate)
		{
			if ((object)_cached == null)
			{
				_cached = Probe();
			}
			return _cached;
		}
	}

	private static Snapshot Probe()
	{
		int num = SafeProcessorCount();
		double num2 = TotalPhysicalMemoryGb();
		(string Name, double MemoryGb) tuple = QueryPrimaryGpu();
		string item = tuple.Name;
		double item2 = tuple.MemoryGb;
		bool flag = LooksIntegrated(item);
		int num3 = 0;
		List<string> list = new List<string>();
		if (num2 > 0.0)
		{
			if (num2 < 6.0)
			{
				num3 += 3;
				list.Add($"{num2:0.#}GB RAM");
			}
			else if (num2 < 8.0)
			{
				num3 += 2;
				list.Add($"{num2:0.#}GB RAM");
			}
		}
		switch (num)
		{
		case 1:
		case 2:
			num3 += 3;
			list.Add($"{num} CPU threads");
			break;
		case 3:
		case 4:
			num3++;
			list.Add($"{num} CPU threads");
			break;
		}
		if (flag)
		{
			num3++;
			list.Add("integrated graphics");
		}
		else if (item2 > 0.0 && item2 < 1.0)
		{
			num3 += 2;
			list.Add($"{item2:0.#}GB VRAM");
		}
		bool isLowEnd = num3 >= 3;
		return new Snapshot(num, Math.Round(num2, 1), item, Math.Round(item2, 2), flag, num3, isLowEnd, list.ToArray());
	}

	private static int SafeProcessorCount()
	{
		try
		{
			return Environment.ProcessorCount;
		}
		catch
		{
			return 0;
		}
	}

	private static double TotalPhysicalMemoryGb()
	{
		try
		{
			MEMORYSTATUSEX lpBuffer = new MEMORYSTATUSEX
			{
				dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
			};
			if (!GlobalMemoryStatusEx(ref lpBuffer))
			{
				return 0.0;
			}
			return (double)lpBuffer.ullTotalPhys / 1024.0 / 1024.0 / 1024.0;
		}
		catch
		{
			return 0.0;
		}
	}

	private static (string Name, double MemoryGb) QueryPrimaryGpu()
	{
		try
		{
			using ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
			using ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();
			string item = "";
			double num = -1.0;
			foreach (ManagementBaseObject item2 in managementObjectCollection)
			{
				using (item2)
				{
					string text = (item2["Name"] as string) ?? "";
					double num2 = 0.0;
					try
					{
						if (item2["AdapterRAM"] != null)
						{
							num2 = Convert.ToDouble(item2["AdapterRAM"]) / 1024.0 / 1024.0 / 1024.0;
						}
					}
					catch
					{
					}
					if (!string.IsNullOrWhiteSpace(text) && num2 > num)
					{
						num = num2;
						item = text;
					}
				}
			}
			return (Name: item, MemoryGb: (num < 0.0) ? 0.0 : num);
		}
		catch
		{
			return (Name: "", MemoryGb: 0.0);
		}
	}

	private static bool LooksIntegrated(string gpuName)
	{
		if (string.IsNullOrWhiteSpace(gpuName))
		{
			return false;
		}
		string text = gpuName.ToLowerInvariant();
		if (text.Contains("basic display") || text.Contains("basic render"))
		{
			return true;
		}
		if (text.Contains("standard vga"))
		{
			return true;
		}
		if (text.Contains("intel") && !text.Contains("arc"))
		{
			return true;
		}
		if (text.Contains("vega") && text.Contains("graphics"))
		{
			return true;
		}
		if (text.Contains("radeon") && text.Contains("graphics") && !text.Contains(" rx ") && !text.Contains("pro"))
		{
			return true;
		}
		return false;
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
