using System.Runtime.InteropServices;
using ABI.Microsoft.UI.Composition;
using ABI.Microsoft.UI.Xaml;
using ABI.Microsoft.UI.Xaml.Controls;
using ABI.Microsoft.UI.Xaml.Markup;

namespace WinRT.NeoLauncherVtableClasses;

internal sealed class NeoLauncher_Views_WebShellViewWinRTTypeDetails : IWinRTExposedTypeDetails
{
	public ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
	{
		return new ComWrappers.ComInterfaceEntry[7]
		{
			new ComWrappers.ComInterfaceEntry
			{
				IID = IUIElementOverridesMethods.IID,
				Vtable = IUIElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IAnimationObjectMethods.IID,
				Vtable = IAnimationObjectMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IVisualElementMethods.IID,
				Vtable = IVisualElementMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IVisualElement2Methods.IID,
				Vtable = IVisualElement2Methods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IFrameworkElementOverridesMethods.IID,
				Vtable = IFrameworkElementOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IControlOverridesMethods.IID,
				Vtable = IControlOverridesMethods.AbiToProjectionVftablePtr
			},
			new ComWrappers.ComInterfaceEntry
			{
				IID = IComponentConnectorMethods.IID,
				Vtable = IComponentConnectorMethods.AbiToProjectionVftablePtr
			}
		};
	}
}
