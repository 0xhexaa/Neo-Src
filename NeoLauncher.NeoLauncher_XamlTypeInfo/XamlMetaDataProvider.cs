using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation.Metadata;
using WinRT;
using WinRT.NeoLauncherVtableClasses;

namespace NeoLauncher.NeoLauncher_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
[DebuggerNonUserCode]
[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IXamlMetadataProvider")]
[WinRTExposedType(typeof(NeoLauncher_NeoLauncher_XamlTypeInfo_XamlMetaDataProviderWinRTTypeDetails))]
public sealed class XamlMetaDataProvider : IXamlMetadataProvider
{
	private XamlTypeInfoProvider _provider;

	private XamlTypeInfoProvider Provider
	{
		get
		{
			if (_provider == null)
			{
				_provider = new XamlTypeInfoProvider();
			}
			return _provider;
		}
	}

	[DefaultOverload]
	public IXamlType GetXamlType(Type type)
	{
		return Provider.GetXamlTypeByType(type);
	}

	public IXamlType GetXamlType(string fullName)
	{
		return Provider.GetXamlTypeByName(fullName);
	}

	public XmlnsDefinition[] GetXmlnsDefinitions()
	{
		return new XmlnsDefinition[0];
	}
}
