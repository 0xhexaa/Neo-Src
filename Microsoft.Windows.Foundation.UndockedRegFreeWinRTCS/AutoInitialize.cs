using System;

namespace Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS;

internal class AutoInitialize
{
	internal static void AccessWindowsAppSDK()
	{
		Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
		NativeMethods.WindowsAppRuntime_EnsureIsLoaded();
	}
}
