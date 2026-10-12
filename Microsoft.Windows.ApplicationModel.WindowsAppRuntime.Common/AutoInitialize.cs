using System.Runtime.CompilerServices;
using Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS;

namespace Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Common;

internal class AutoInitialize
{
	[ModuleInitializer]
	internal static void InitializeWindowsAppSDK()
	{
		Microsoft.Windows.Foundation.UndockedRegFreeWinRTCS.AutoInitialize.AccessWindowsAppSDK();
	}
}
