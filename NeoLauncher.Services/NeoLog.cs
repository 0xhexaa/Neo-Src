using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NeoLauncher.Services;

public static class NeoLog
{
	private const int MaxSessionLogAgeDays = 7;

	private const int MaxCrashLogs = 20;

	private const long MaxLogFileBytes = 8388608L;

	private const long MaxLogDirectoryBytes = 67108864L;

	private static readonly object _lock = new object();

	private static string? _logDirectory;

	private static StreamWriter? _writer;

	private static string? _writerPath;

	private static DateTime _writerDateUtc;

	private static bool _initialized;

	private static bool _disabled;

	public static string LogDirectory => _logDirectory ?? (_logDirectory = Path.Combine(Config.DataPath, "Logs"));

	private static string AppVersion
	{
		get
		{
			try
			{
				Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
				return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? assembly.GetName().Version?.ToString() ?? "unknown";
			}
			catch
			{
				return "unknown";
			}
		}
	}

	public static void Initialize()
	{
		lock (_lock)
		{
			if (_initialized || _disabled)
			{
				return;
			}
			_initialized = true;
			try
			{
				Directory.CreateDirectory(LogDirectory);
				Sweep();
				OpenWriter();
			}
			catch
			{
				_disabled = true;
				return;
			}
		}
		Info("launcher", $"=== Neo Launcher {AppVersion} starting (PID {Environment.ProcessId}) ===");
		Info("launcher", $"OS {Environment.OSVersion.VersionString}, CLR {Environment.Version}, {(Environment.Is64BitProcess ? "x64" : "x86")}");
	}

	public static void Info(string category, string message)
	{
		Write("INFO", category, message);
	}

	public static void Warn(string category, string message)
	{
		Write("WARN", category, message);
	}

	public static void Error(string category, string message, Exception? ex = null)
	{
		Write("ERROR", category, (ex == null) ? message : (message + Environment.NewLine + Describe(ex)));
	}

	public static void Crash(string source, Exception? ex)
	{
		Initialize();
		string contents = BuildCrashReport(source, ex);
		Write("FATAL", source, (ex == null) ? "Unhandled error (no exception object)" : Describe(ex));
		if (_disabled)
		{
			return;
		}
		try
		{
			lock (_lock)
			{
				string path = $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log";
				File.WriteAllText(Path.Combine(LogDirectory, path), contents, Encoding.UTF8);
				TrimCrashLogs();
			}
		}
		catch
		{
		}
	}

	public static void CapExternalLog(string path, long maxBytes)
	{
		try
		{
			FileInfo fileInfo = new FileInfo(path);
			if (fileInfo.Exists && fileInfo.Length > maxBytes)
			{
				RollFile(path);
			}
		}
		catch
		{
		}
	}

	private static void Write(string level, string category, string message)
	{
		if (_disabled)
		{
			return;
		}
		if (!_initialized)
		{
			Initialize();
		}
		if (_disabled)
		{
			return;
		}
		try
		{
			lock (_lock)
			{
				if (_writer == null || _writerDateUtc != DateTime.UtcNow.Date)
				{
					OpenWriter();
				}
				if (_writer != null)
				{
					RollIfOversized();
					StreamWriter? writer = _writer;
					if (writer != null)
					{
						CultureInfo invariantCulture = CultureInfo.InvariantCulture;
						InlineArray4<object> buffer = default;
						buffer[0] = DateTime.Now;
						buffer[1] = level;
						buffer[2] = category;
						buffer[3] = message;
						writer.WriteLine(string.Format((IFormatProvider?)invariantCulture, "{0:yyyy-MM-dd HH:mm:ss.fff} [{1,-5}] [{2}] {3}", (ReadOnlySpan<object?>)buffer));
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void OpenWriter()
	{
		try
		{
			_writer?.Dispose();
		}
		catch
		{
		}
		_writer = null;
		Directory.CreateDirectory(LogDirectory);
		_writerDateUtc = DateTime.UtcNow.Date;
		_writerPath = Path.Combine(LogDirectory, $"launcher-{DateTime.Now:yyyyMMdd}.log");
		_writer = new StreamWriter(new FileStream(_writerPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), Encoding.UTF8)
		{
			AutoFlush = true
		};
	}

	private static void RollIfOversized()
	{
		if (_writer == null || _writerPath == null)
		{
			return;
		}
		long length;
		try
		{
			length = _writer.BaseStream.Length;
		}
		catch
		{
			return;
		}
		if (length > 8388608)
		{
			try
			{
				_writer.Dispose();
			}
			catch
			{
			}
			_writer = null;
			RollFile(_writerPath);
			OpenWriter();
			_writer?.WriteLine($"[previous log rolled to .prev.log at {8L} MB]");
		}
	}

	private static void RollFile(string path)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(path);
			if (directoryName != null)
			{
				string text = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(path) + ".prev" + Path.GetExtension(path));
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Move(path, text);
			}
		}
		catch
		{
		}
	}

	private static void Sweep()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(LogDirectory);
		if (!directoryInfo.Exists)
		{
			return;
		}
		try
		{
			DateTime dateTime = DateTime.UtcNow.AddDays(-7.0);
			foreach (FileInfo item in directoryInfo.EnumerateFiles("launcher-*.log"))
			{
				if (item.LastWriteTimeUtc < dateTime)
				{
					TryDelete(item);
				}
			}
		}
		catch
		{
		}
		try
		{
			TrimCrashLogs();
		}
		catch
		{
		}
		try
		{
			List<FileInfo> source = directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
			long num = source.Sum((FileInfo f) => SafeLength(f));
			if (num <= 67108864)
			{
				return;
			}
			foreach (FileInfo item2 in from f in source
				where !f.Name.StartsWith("crash-", StringComparison.OrdinalIgnoreCase)
				orderby f.LastWriteTimeUtc
				select f)
			{
				if (num > 67108864)
				{
					long num2 = SafeLength(item2);
					if (TryDelete(item2))
					{
						num -= num2;
					}
					continue;
				}
				break;
			}
		}
		catch
		{
		}
	}

	private static void TrimCrashLogs()
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(LogDirectory);
		if (!directoryInfo.Exists)
		{
			return;
		}
		foreach (FileInfo item in (from f in directoryInfo.EnumerateFiles("crash-*.log")
			orderby f.LastWriteTimeUtc descending
			select f).Skip(20).ToList())
		{
			TryDelete(item);
		}
	}

	private static long SafeLength(FileInfo file)
	{
		try
		{
			return file.Length;
		}
		catch
		{
			return 0L;
		}
	}

	private static bool TryDelete(FileInfo file)
	{
		try
		{
			file.Delete();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string BuildCrashReport(string source, Exception? ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Neo Launcher crash report");
		stringBuilder.AppendLine("=========================");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Time      : ");
		handler.AppendFormatted(DateTime.Now, "yyyy-MM-dd HH:mm:ss.fff zzz");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Source    : ");
		handler.AppendFormatted(source);
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Version   : ");
		handler.AppendFormatted(AppVersion);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("OS        : ");
		handler.AppendFormatted(Environment.OSVersion.VersionString);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
		handler.AppendLiteral("Runtime   : ");
		handler.AppendFormatted(Environment.Version);
		handler.AppendLiteral(" (");
		handler.AppendFormatted(Environment.Is64BitProcess ? "x64" : "x86");
		handler.AppendLiteral(")");
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Process   : ");
		handler.AppendFormatted(Environment.ProcessId);
		stringBuilder8.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(12, 1, stringBuilder2);
		handler.AppendLiteral("Uptime    : ");
		handler.AppendFormatted(TimeSpan.FromMilliseconds(Environment.TickCount64));
		stringBuilder9.AppendLine(ref handler);
		stringBuilder.AppendLine();
		if (ex == null)
		{
			stringBuilder.AppendLine("No exception object was supplied by the runtime.");
			return stringBuilder.ToString();
		}
		stringBuilder.AppendLine(Describe(ex));
		return stringBuilder.ToString();
	}

	private static string Describe(Exception ex)
	{
		StringBuilder stringBuilder = new StringBuilder();
		Describe(ex, stringBuilder, 0);
		return stringBuilder.ToString().TrimEnd();
	}

	private static void Describe(Exception ex, StringBuilder builder, int depth)
	{
		if (depth > 8)
		{
			return;
		}
		string value = new string(' ', depth * 2);
		StringBuilder stringBuilder = builder;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 3, stringBuilder);
		handler.AppendFormatted(value);
		handler.AppendFormatted(ex.GetType().FullName);
		handler.AppendLiteral(": ");
		handler.AppendFormatted(ex.Message);
		stringBuilder2.AppendLine(ref handler);
		if (!string.IsNullOrWhiteSpace(ex.StackTrace))
		{
			string[] array = ex.StackTrace.Split('\n');
			foreach (string text in array)
			{
				stringBuilder = builder;
				StringBuilder stringBuilder3 = stringBuilder;
				handler = new StringBuilder.AppendInterpolatedStringHandler(0, 2, stringBuilder);
				handler.AppendFormatted(value);
				handler.AppendFormatted(text.TrimEnd());
				stringBuilder3.AppendLine(ref handler);
			}
		}
		IEnumerable<Exception> enumerable2;
		if (!(ex is AggregateException ex2))
		{
			IEnumerable<Exception> enumerable = ((ex.InnerException == null) ? Array.Empty<Exception>() : new Exception[1] { ex.InnerException });
			enumerable2 = enumerable;
		}
		else
		{
			IEnumerable<Exception> enumerable = ex2.InnerExceptions;
			enumerable2 = enumerable;
		}
		foreach (Exception item in enumerable2)
		{
			stringBuilder = builder;
			StringBuilder stringBuilder4 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(23, 1, stringBuilder);
			handler.AppendFormatted(value);
			handler.AppendLiteral("--- inner exception ---");
			stringBuilder4.AppendLine(ref handler);
			Describe(item, builder, depth + 1);
		}
	}
}
