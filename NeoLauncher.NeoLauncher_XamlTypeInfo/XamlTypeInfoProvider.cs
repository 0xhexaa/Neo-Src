using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.WinUI.Controls.OpacityMaskViewRns.CommunityToolkit_WinUI_Controls_OpacityMaskView_XamlTypeInfo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.XamlTypeInfo;
using Microsoft.Web.WebView2.Core;
using NeoLauncher.Models.UI.Appearance;
using NeoLauncher.Services.WebHost;
using NeoLauncher.Views;
using Windows.UI;

namespace NeoLauncher.NeoLauncher_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2511")]
[DebuggerNonUserCode]
internal class XamlTypeInfoProvider
{
	private Dictionary<string, IXamlType> _xamlTypeCacheByName = new Dictionary<string, IXamlType>();

	private Dictionary<Type, IXamlType> _xamlTypeCacheByType = new Dictionary<Type, IXamlType>();

	private Dictionary<string, IXamlMember> _xamlMembers = new Dictionary<string, IXamlMember>();

	private string[] _typeNameTable;

	private Type[] _typeTable;

	private List<IXamlMetadataProvider> _otherProviders;

	private List<IXamlMetadataProvider> OtherProviders
	{
		get
		{
			if (_otherProviders == null)
			{
				List<IXamlMetadataProvider> list = new List<IXamlMetadataProvider>();
				IXamlMetadataProvider item = new XamlControlsXamlMetaDataProvider();
				list.Add(item);
				item = new CommunityToolkit.WinUI.Controls.OpacityMaskViewRns.CommunityToolkit_WinUI_Controls_OpacityMaskView_XamlTypeInfo.XamlMetaDataProvider();
				list.Add(item);
				_otherProviders = list;
			}
			return _otherProviders;
		}
	}

	public IXamlType GetXamlTypeByType(Type type)
	{
		IXamlType value;
		lock (_xamlTypeCacheByType)
		{
			if (_xamlTypeCacheByType.TryGetValue(type, out value))
			{
				return value;
			}
			int num = LookupTypeIndexByType(type);
			if (num != -1)
			{
				value = CreateXamlType(num);
			}
			XamlUserType xamlUserType = value as XamlUserType;
			if (value == null || (xamlUserType != null && xamlUserType.IsReturnTypeStub && !xamlUserType.IsLocalType))
			{
				IXamlType xamlType = CheckOtherMetadataProvidersForType(type);
				if (xamlType != null && (xamlType.IsConstructible || value == null))
				{
					value = xamlType;
				}
			}
			if (value != null)
			{
				_xamlTypeCacheByName.Add(value.FullName, value);
				_xamlTypeCacheByType.Add(value.UnderlyingType, value);
			}
		}
		return value;
	}

	public IXamlType GetXamlTypeByName(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return null;
		}
		IXamlType value;
		lock (_xamlTypeCacheByType)
		{
			if (_xamlTypeCacheByName.TryGetValue(typeName, out value))
			{
				return value;
			}
			int num = LookupTypeIndexByName(typeName);
			if (num != -1)
			{
				value = CreateXamlType(num);
			}
			XamlUserType xamlUserType = value as XamlUserType;
			if (value == null || (xamlUserType != null && xamlUserType.IsReturnTypeStub && !xamlUserType.IsLocalType))
			{
				IXamlType xamlType = CheckOtherMetadataProvidersForName(typeName);
				if (xamlType != null && (xamlType.IsConstructible || value == null))
				{
					value = xamlType;
				}
			}
			if (value != null)
			{
				_xamlTypeCacheByName.Add(value.FullName, value);
				_xamlTypeCacheByType.Add(value.UnderlyingType, value);
			}
		}
		return value;
	}

	public IXamlMember GetMemberByLongName(string longMemberName)
	{
		if (string.IsNullOrEmpty(longMemberName))
		{
			return null;
		}
		IXamlMember value;
		lock (_xamlMembers)
		{
			if (_xamlMembers.TryGetValue(longMemberName, out value))
			{
				return value;
			}
			value = CreateXamlMember(longMemberName);
			if (value != null)
			{
				_xamlMembers.Add(longMemberName, value);
			}
		}
		return value;
	}

	private void InitTypeTables()
	{
		_typeNameTable = new string[25];
		_typeNameTable[0] = "Microsoft.UI.Xaml.Controls.XamlControlsResources";
		_typeNameTable[1] = "Microsoft.UI.Xaml.ResourceDictionary";
		_typeNameTable[2] = "Object";
		_typeNameTable[3] = "Boolean";
		_typeNameTable[4] = "NeoLauncher.FixedWindow";
		_typeNameTable[5] = "Microsoft.UI.Xaml.Window";
		_typeNameTable[6] = "NeoLauncher.Models.UI.Appearance.BackdropType";
		_typeNameTable[7] = "System.Enum";
		_typeNameTable[8] = "System.ValueType";
		_typeNameTable[9] = "Microsoft.UI.Xaml.ElementTheme";
		_typeNameTable[10] = "Double";
		_typeNameTable[11] = "NeoLauncher.Views.WebShellView";
		_typeNameTable[12] = "Microsoft.UI.Xaml.Controls.UserControl";
		_typeNameTable[13] = "String";
		_typeNameTable[14] = "NeoLauncher.Services.WebHost.NeoWebBridge";
		_typeNameTable[15] = "NeoLauncher.MainWindow";
		_typeNameTable[16] = "Microsoft.UI.Xaml.Controls.WebView2";
		_typeNameTable[17] = "Microsoft.UI.Xaml.FrameworkElement";
		_typeNameTable[18] = "Microsoft.Web.WebView2.Core.CoreWebView2";
		_typeNameTable[19] = "Windows.UI.Color";
		_typeNameTable[20] = "System.Uri";
		_typeNameTable[21] = "Microsoft.UI.Xaml.Controls.TreeViewNode";
		_typeNameTable[22] = "Microsoft.UI.Xaml.DependencyObject";
		_typeNameTable[23] = "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>";
		_typeNameTable[24] = "Int32";
		_typeTable = new Type[25];
		_typeTable[0] = typeof(XamlControlsResources);
		_typeTable[1] = typeof(ResourceDictionary);
		_typeTable[2] = typeof(object);
		_typeTable[3] = typeof(bool);
		_typeTable[4] = typeof(FixedWindow);
		_typeTable[5] = typeof(Window);
		_typeTable[6] = typeof(BackdropType);
		_typeTable[7] = typeof(Enum);
		_typeTable[8] = typeof(ValueType);
		_typeTable[9] = typeof(ElementTheme);
		_typeTable[10] = typeof(double);
		_typeTable[11] = typeof(WebShellView);
		_typeTable[12] = typeof(UserControl);
		_typeTable[13] = typeof(string);
		_typeTable[14] = typeof(NeoWebBridge);
		_typeTable[15] = typeof(MainWindow);
		_typeTable[16] = typeof(WebView2);
		_typeTable[17] = typeof(FrameworkElement);
		_typeTable[18] = typeof(CoreWebView2);
		_typeTable[19] = typeof(Color);
		_typeTable[20] = typeof(Uri);
		_typeTable[21] = typeof(TreeViewNode);
		_typeTable[22] = typeof(DependencyObject);
		_typeTable[23] = typeof(IList<TreeViewNode>);
		_typeTable[24] = typeof(int);
	}

	private int LookupTypeIndexByName(string typeName)
	{
		if (_typeNameTable == null)
		{
			InitTypeTables();
		}
		for (int i = 0; i < _typeNameTable.Length; i++)
		{
			if (string.CompareOrdinal(_typeNameTable[i], typeName) == 0)
			{
				return i;
			}
		}
		return -1;
	}

	private int LookupTypeIndexByType(Type type)
	{
		if (_typeTable == null)
		{
			InitTypeTables();
		}
		for (int i = 0; i < _typeTable.Length; i++)
		{
			if (type == _typeTable[i])
			{
				return i;
			}
		}
		return -1;
	}

	private object Activate_0_XamlControlsResources()
	{
		return new XamlControlsResources();
	}

	private object Activate_11_WebShellView()
	{
		return new WebShellView();
	}

	private object Activate_15_MainWindow()
	{
		return new MainWindow();
	}

	private object Activate_16_WebView2()
	{
		return new WebView2();
	}

	private object Activate_21_TreeViewNode()
	{
		return new TreeViewNode();
	}

	private void StaticInitializer_0_XamlControlsResources()
	{
		RuntimeHelpers.RunClassConstructor(typeof(XamlControlsResources).TypeHandle);
	}

	private void StaticInitializer_4_FixedWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(FixedWindow).TypeHandle);
	}

	private void StaticInitializer_6_BackdropType()
	{
		RuntimeHelpers.RunClassConstructor(typeof(BackdropType).TypeHandle);
	}

	private void StaticInitializer_7_Enum()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Enum).TypeHandle);
	}

	private void StaticInitializer_8_ValueType()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ValueType).TypeHandle);
	}

	private void StaticInitializer_11_WebShellView()
	{
		RuntimeHelpers.RunClassConstructor(typeof(WebShellView).TypeHandle);
	}

	private void StaticInitializer_14_NeoWebBridge()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NeoWebBridge).TypeHandle);
	}

	private void StaticInitializer_15_MainWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MainWindow).TypeHandle);
	}

	private void StaticInitializer_16_WebView2()
	{
		RuntimeHelpers.RunClassConstructor(typeof(WebView2).TypeHandle);
	}

	private void StaticInitializer_18_CoreWebView2()
	{
		RuntimeHelpers.RunClassConstructor(typeof(CoreWebView2).TypeHandle);
	}

	private void StaticInitializer_19_Color()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Color).TypeHandle);
	}

	private void StaticInitializer_20_Uri()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Uri).TypeHandle);
	}

	private void StaticInitializer_21_TreeViewNode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TreeViewNode).TypeHandle);
	}

	private void StaticInitializer_23_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<TreeViewNode>).TypeHandle);
	}

	private void MapAdd_0_XamlControlsResources(object instance, object key, object item)
	{
		((IDictionary<object, object>)instance).Add(key, item);
	}

	private void VectorAdd_23_IList(object instance, object item)
	{
		ICollection<TreeViewNode> collection = (ICollection<TreeViewNode>)instance;
		TreeViewNode item2 = (TreeViewNode)item;
		collection.Add(item2);
	}

	private IXamlType CreateXamlType(int typeIndex)
	{
		XamlSystemBaseType result = null;
		string fullName = _typeNameTable[typeIndex];
		Type type = _typeTable[typeIndex];
		switch (typeIndex)
		{
		case 0:
		{
			XamlUserType xamlUserType12 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.ResourceDictionary"));
			xamlUserType12.Activator = Activate_0_XamlControlsResources;
			xamlUserType12.StaticInitializer = StaticInitializer_0_XamlControlsResources;
			xamlUserType12.DictionaryAdd = MapAdd_0_XamlControlsResources;
			xamlUserType12.AddMemberName("UseCompactResources");
			result = xamlUserType12;
			break;
		}
		case 1:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 2:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 3:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 4:
		{
			XamlUserType xamlUserType11 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Window"));
			xamlUserType11.StaticInitializer = StaticInitializer_4_FixedWindow;
			xamlUserType11.AddMemberName("BackdropType");
			xamlUserType11.AddMemberName("Theme");
			xamlUserType11.AddMemberName("DpiScale");
			xamlUserType11.SetIsLocalType();
			result = xamlUserType11;
			break;
		}
		case 5:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 6:
		{
			XamlUserType xamlUserType10 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType10.StaticInitializer = StaticInitializer_6_BackdropType;
			xamlUserType10.AddEnumValue("Solid", BackdropType.Solid);
			xamlUserType10.AddEnumValue("Glass", BackdropType.Glass);
			xamlUserType10.SetIsLocalType();
			result = xamlUserType10;
			break;
		}
		case 7:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"))
			{
				StaticInitializer = StaticInitializer_7_Enum
			};
			break;
		case 8:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				StaticInitializer = StaticInitializer_8_ValueType
			};
			break;
		case 9:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 10:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 11:
		{
			XamlUserType xamlUserType9 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType9.Activator = Activate_11_WebShellView;
			xamlUserType9.StaticInitializer = StaticInitializer_11_WebShellView;
			xamlUserType9.AddMemberName("RouteHash");
			xamlUserType9.AddMemberName("Bridge");
			xamlUserType9.SetIsLocalType();
			result = xamlUserType9;
			break;
		}
		case 12:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 13:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 14:
		{
			XamlUserType xamlUserType8 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType8.StaticInitializer = StaticInitializer_14_NeoWebBridge;
			xamlUserType8.SetIsReturnTypeStub();
			xamlUserType8.SetIsLocalType();
			result = xamlUserType8;
			break;
		}
		case 15:
		{
			XamlUserType xamlUserType7 = new XamlUserType(this, fullName, type, GetXamlTypeByName("NeoLauncher.FixedWindow"));
			xamlUserType7.Activator = Activate_15_MainWindow;
			xamlUserType7.StaticInitializer = StaticInitializer_15_MainWindow;
			xamlUserType7.SetIsLocalType();
			result = xamlUserType7;
			break;
		}
		case 16:
		{
			XamlUserType xamlUserType6 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.FrameworkElement"));
			xamlUserType6.Activator = Activate_16_WebView2;
			xamlUserType6.StaticInitializer = StaticInitializer_16_WebView2;
			xamlUserType6.AddMemberName("CanGoBack");
			xamlUserType6.AddMemberName("CanGoForward");
			xamlUserType6.AddMemberName("CoreWebView2");
			xamlUserType6.AddMemberName("DefaultBackgroundColor");
			xamlUserType6.AddMemberName("Source");
			result = xamlUserType6;
			break;
		}
		case 17:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 18:
		{
			XamlUserType xamlUserType5 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType5.StaticInitializer = StaticInitializer_18_CoreWebView2;
			xamlUserType5.SetIsReturnTypeStub();
			result = xamlUserType5;
			break;
		}
		case 19:
		{
			XamlUserType xamlUserType4 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType4.StaticInitializer = StaticInitializer_19_Color;
			xamlUserType4.SetIsReturnTypeStub();
			result = xamlUserType4;
			break;
		}
		case 20:
		{
			XamlUserType xamlUserType3 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"));
			xamlUserType3.StaticInitializer = StaticInitializer_20_Uri;
			xamlUserType3.SetIsReturnTypeStub();
			result = xamlUserType3;
			break;
		}
		case 21:
		{
			XamlUserType xamlUserType2 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType2.Activator = Activate_21_TreeViewNode;
			xamlUserType2.StaticInitializer = StaticInitializer_21_TreeViewNode;
			xamlUserType2.AddMemberName("Children");
			xamlUserType2.AddMemberName("Content");
			xamlUserType2.AddMemberName("Depth");
			xamlUserType2.AddMemberName("HasChildren");
			xamlUserType2.AddMemberName("HasUnrealizedChildren");
			xamlUserType2.AddMemberName("IsExpanded");
			xamlUserType2.AddMemberName("Parent");
			xamlUserType2.SetIsBindable();
			result = xamlUserType2;
			break;
		}
		case 22:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 23:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_23_IList;
			xamlUserType.CollectionAdd = VectorAdd_23_IList;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 24:
			result = new XamlSystemBaseType(fullName, type);
			break;
		}
		return result;
	}

	private IXamlType CheckOtherMetadataProvidersForName(string typeName)
	{
		IXamlType xamlType = null;
		IXamlType result = null;
		foreach (IXamlMetadataProvider otherProvider in OtherProviders)
		{
			xamlType = otherProvider.GetXamlType(typeName);
			if (xamlType != null)
			{
				if (xamlType.IsConstructible)
				{
					return xamlType;
				}
				result = xamlType;
			}
		}
		return result;
	}

	private IXamlType CheckOtherMetadataProvidersForType(Type type)
	{
		IXamlType xamlType = null;
		IXamlType result = null;
		foreach (IXamlMetadataProvider otherProvider in OtherProviders)
		{
			xamlType = otherProvider.GetXamlType(type);
			if (xamlType != null)
			{
				if (xamlType.IsConstructible)
				{
					return xamlType;
				}
				result = xamlType;
			}
		}
		return result;
	}

	private object get_0_XamlControlsResources_UseCompactResources(object instance)
	{
		return ((XamlControlsResources)instance).UseCompactResources;
	}

	private void set_0_XamlControlsResources_UseCompactResources(object instance, object Value)
	{
		((XamlControlsResources)instance).UseCompactResources = (bool)Value;
	}

	private object get_1_FixedWindow_BackdropType(object instance)
	{
		return ((FixedWindow)instance).BackdropType;
	}

	private void set_1_FixedWindow_BackdropType(object instance, object Value)
	{
		((FixedWindow)instance).BackdropType = (BackdropType)Value;
	}

	private object get_2_FixedWindow_Theme(object instance)
	{
		return ((FixedWindow)instance).Theme;
	}

	private void set_2_FixedWindow_Theme(object instance, object Value)
	{
		((FixedWindow)instance).Theme = (ElementTheme)Value;
	}

	private object get_3_FixedWindow_DpiScale(object instance)
	{
		return ((FixedWindow)instance).DpiScale;
	}

	private object get_4_WebShellView_RouteHash(object instance)
	{
		return ((WebShellView)instance).RouteHash;
	}

	private void set_4_WebShellView_RouteHash(object instance, object Value)
	{
		((WebShellView)instance).RouteHash = (string)Value;
	}

	private object get_5_WebShellView_Bridge(object instance)
	{
		return ((WebShellView)instance).Bridge;
	}

	private object get_6_WebView2_CanGoBack(object instance)
	{
		return ((WebView2)instance).CanGoBack;
	}

	private void set_6_WebView2_CanGoBack(object instance, object Value)
	{
		((WebView2)instance).CanGoBack = (bool)Value;
	}

	private object get_7_WebView2_CanGoForward(object instance)
	{
		return ((WebView2)instance).CanGoForward;
	}

	private void set_7_WebView2_CanGoForward(object instance, object Value)
	{
		((WebView2)instance).CanGoForward = (bool)Value;
	}

	private object get_8_WebView2_CoreWebView2(object instance)
	{
		return ((WebView2)instance).CoreWebView2;
	}

	private object get_9_WebView2_DefaultBackgroundColor(object instance)
	{
		return ((WebView2)instance).DefaultBackgroundColor;
	}

	private void set_9_WebView2_DefaultBackgroundColor(object instance, object Value)
	{
		((WebView2)instance).DefaultBackgroundColor = (Color)Value;
	}

	private object get_10_WebView2_Source(object instance)
	{
		return ((WebView2)instance).Source;
	}

	private void set_10_WebView2_Source(object instance, object Value)
	{
		((WebView2)instance).Source = (Uri)Value;
	}

	private object get_11_TreeViewNode_Children(object instance)
	{
		return ((TreeViewNode)instance).Children;
	}

	private object get_12_TreeViewNode_Content(object instance)
	{
		return ((TreeViewNode)instance).Content;
	}

	private void set_12_TreeViewNode_Content(object instance, object Value)
	{
		((TreeViewNode)instance).Content = Value;
	}

	private object get_13_TreeViewNode_Depth(object instance)
	{
		return ((TreeViewNode)instance).Depth;
	}

	private object get_14_TreeViewNode_HasChildren(object instance)
	{
		return ((TreeViewNode)instance).HasChildren;
	}

	private object get_15_TreeViewNode_HasUnrealizedChildren(object instance)
	{
		return ((TreeViewNode)instance).HasUnrealizedChildren;
	}

	private void set_15_TreeViewNode_HasUnrealizedChildren(object instance, object Value)
	{
		((TreeViewNode)instance).HasUnrealizedChildren = (bool)Value;
	}

	private object get_16_TreeViewNode_IsExpanded(object instance)
	{
		return ((TreeViewNode)instance).IsExpanded;
	}

	private void set_16_TreeViewNode_IsExpanded(object instance, object Value)
	{
		((TreeViewNode)instance).IsExpanded = (bool)Value;
	}

	private object get_17_TreeViewNode_Parent(object instance)
	{
		return ((TreeViewNode)instance).Parent;
	}

	private IXamlMember CreateXamlMember(string longMemberName)
	{
		XamlMember xamlMember = null;
		switch (longMemberName)
		{
		case "Microsoft.UI.Xaml.Controls.XamlControlsResources.UseCompactResources":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.XamlControlsResources");
			xamlMember = new XamlMember(this, "UseCompactResources", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_0_XamlControlsResources_UseCompactResources;
			xamlMember.Setter = set_0_XamlControlsResources_UseCompactResources;
			break;
		case "NeoLauncher.FixedWindow.BackdropType":
			_ = (XamlUserType)GetXamlTypeByName("NeoLauncher.FixedWindow");
			xamlMember = new XamlMember(this, "BackdropType", "NeoLauncher.Models.UI.Appearance.BackdropType");
			xamlMember.Getter = get_1_FixedWindow_BackdropType;
			xamlMember.Setter = set_1_FixedWindow_BackdropType;
			break;
		case "NeoLauncher.FixedWindow.Theme":
			_ = (XamlUserType)GetXamlTypeByName("NeoLauncher.FixedWindow");
			xamlMember = new XamlMember(this, "Theme", "Microsoft.UI.Xaml.ElementTheme");
			xamlMember.Getter = get_2_FixedWindow_Theme;
			xamlMember.Setter = set_2_FixedWindow_Theme;
			break;
		case "NeoLauncher.FixedWindow.DpiScale":
			_ = (XamlUserType)GetXamlTypeByName("NeoLauncher.FixedWindow");
			xamlMember = new XamlMember(this, "DpiScale", "Double");
			xamlMember.Getter = get_3_FixedWindow_DpiScale;
			xamlMember.SetIsReadOnly();
			break;
		case "NeoLauncher.Views.WebShellView.RouteHash":
			_ = (XamlUserType)GetXamlTypeByName("NeoLauncher.Views.WebShellView");
			xamlMember = new XamlMember(this, "RouteHash", "String");
			xamlMember.Getter = get_4_WebShellView_RouteHash;
			xamlMember.Setter = set_4_WebShellView_RouteHash;
			break;
		case "NeoLauncher.Views.WebShellView.Bridge":
			_ = (XamlUserType)GetXamlTypeByName("NeoLauncher.Views.WebShellView");
			xamlMember = new XamlMember(this, "Bridge", "NeoLauncher.Services.WebHost.NeoWebBridge");
			xamlMember.Getter = get_5_WebShellView_Bridge;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.WebView2.CanGoBack":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CanGoBack", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_6_WebView2_CanGoBack;
			xamlMember.Setter = set_6_WebView2_CanGoBack;
			break;
		case "Microsoft.UI.Xaml.Controls.WebView2.CanGoForward":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CanGoForward", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_7_WebView2_CanGoForward;
			xamlMember.Setter = set_7_WebView2_CanGoForward;
			break;
		case "Microsoft.UI.Xaml.Controls.WebView2.CoreWebView2":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "CoreWebView2", "Microsoft.Web.WebView2.Core.CoreWebView2");
			xamlMember.Getter = get_8_WebView2_CoreWebView2;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.WebView2.DefaultBackgroundColor":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "DefaultBackgroundColor", "Windows.UI.Color");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_9_WebView2_DefaultBackgroundColor;
			xamlMember.Setter = set_9_WebView2_DefaultBackgroundColor;
			break;
		case "Microsoft.UI.Xaml.Controls.WebView2.Source":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.WebView2");
			xamlMember = new XamlMember(this, "Source", "System.Uri");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_10_WebView2_Source;
			xamlMember.Setter = set_10_WebView2_Source;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Children":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Children", "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>");
			xamlMember.Getter = get_11_TreeViewNode_Children;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Content":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Content", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_12_TreeViewNode_Content;
			xamlMember.Setter = set_12_TreeViewNode_Content;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Depth":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Depth", "Int32");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_13_TreeViewNode_Depth;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_14_TreeViewNode_HasChildren;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.Getter = get_15_TreeViewNode_HasUnrealizedChildren;
			xamlMember.Setter = set_15_TreeViewNode_HasUnrealizedChildren;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.IsExpanded":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_16_TreeViewNode_IsExpanded;
			xamlMember.Setter = set_16_TreeViewNode_IsExpanded;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Parent":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Parent", "Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember.Getter = get_17_TreeViewNode_Parent;
			xamlMember.SetIsReadOnly();
			break;
		}
		return xamlMember;
	}
}
