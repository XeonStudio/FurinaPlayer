using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Converters;
using FurinaPlayer.UI.FurinaPlayer_UI_XamlTypeInfo;
using LibVLCSharp.LibVLCSharp_XamlTypeInfo;
using LiveChartsCore.SkiaSharpView.WinUI.LiveChartsCore_SkiaSharpView_WinUI_XamlTypeInfo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.XamlTypeInfo;
using SkiaSharp;
using SkiaSharp.Views.Windows;

namespace SonicWave.App.SonicWave_App_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
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
				item = new FurinaPlayer.UI.FurinaPlayer_UI_XamlTypeInfo.XamlMetaDataProvider();
				list.Add(item);
				item = new LibVLCSharp.LibVLCSharp_XamlTypeInfo.XamlMetaDataProvider();
				list.Add(item);
				item = new LiveChartsCore.SkiaSharpView.WinUI.LiveChartsCore_SkiaSharpView_WinUI_XamlTypeInfo.XamlMetaDataProvider();
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
		_typeNameTable = new string[42];
		_typeNameTable[0] = "Microsoft.UI.Xaml.Controls.XamlControlsResources";
		_typeNameTable[1] = "Microsoft.UI.Xaml.ResourceDictionary";
		_typeNameTable[2] = "Object";
		_typeNameTable[3] = "Boolean";
		_typeNameTable[4] = "FurinaPlayer.UI.Converters.PathToImageConverter";
		_typeNameTable[5] = "SkiaSharp.Views.Windows.SKXamlCanvas";
		_typeNameTable[6] = "Microsoft.UI.Xaml.Controls.Canvas";
		_typeNameTable[7] = "Microsoft.UI.Xaml.Controls.Panel";
		_typeNameTable[8] = "SkiaSharp.SKSize";
		_typeNameTable[9] = "System.ValueType";
		_typeNameTable[10] = "Double";
		_typeNameTable[11] = "Microsoft.UI.Xaml.Controls.NavigationView";
		_typeNameTable[12] = "Microsoft.UI.Xaml.Controls.ContentControl";
		_typeNameTable[13] = "Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible";
		_typeNameTable[14] = "System.Enum";
		_typeNameTable[15] = "Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode";
		_typeNameTable[16] = "System.Collections.Generic.IList`1<Object>";
		_typeNameTable[17] = "Microsoft.UI.Xaml.Controls.AutoSuggestBox";
		_typeNameTable[18] = "Microsoft.UI.Xaml.UIElement";
		_typeNameTable[19] = "Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode";
		_typeNameTable[20] = "Microsoft.UI.Xaml.DataTemplate";
		_typeNameTable[21] = "Microsoft.UI.Xaml.Style";
		_typeNameTable[22] = "Microsoft.UI.Xaml.Controls.StyleSelector";
		_typeNameTable[23] = "Microsoft.UI.Xaml.Controls.DataTemplateSelector";
		_typeNameTable[24] = "Microsoft.UI.Xaml.Controls.NavigationViewOverflowLabelMode";
		_typeNameTable[25] = "String";
		_typeNameTable[26] = "Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus";
		_typeNameTable[27] = "Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled";
		_typeNameTable[28] = "Microsoft.UI.Xaml.Controls.NavigationViewTemplateSettings";
		_typeNameTable[29] = "Microsoft.UI.Xaml.DependencyObject";
		_typeNameTable[30] = "FurinaPlayer.UI.Controls.MiniPlayer";
		_typeNameTable[31] = "Microsoft.UI.Xaml.Controls.UserControl";
		_typeNameTable[32] = "Microsoft.UI.Xaml.Controls.NavigationViewItem";
		_typeNameTable[33] = "Microsoft.UI.Xaml.Controls.NavigationViewItemBase";
		_typeNameTable[34] = "Microsoft.UI.Xaml.Controls.IconElement";
		_typeNameTable[35] = "Microsoft.UI.Xaml.Controls.InfoBadge";
		_typeNameTable[36] = "Microsoft.UI.Xaml.Controls.Control";
		_typeNameTable[37] = "SonicWave.App.MainWindow";
		_typeNameTable[38] = "Microsoft.UI.Xaml.Window";
		_typeNameTable[39] = "Microsoft.UI.Xaml.Controls.TreeViewNode";
		_typeNameTable[40] = "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>";
		_typeNameTable[41] = "Int32";
		_typeTable = new Type[42];
		_typeTable[0] = typeof(XamlControlsResources);
		_typeTable[1] = typeof(ResourceDictionary);
		_typeTable[2] = typeof(object);
		_typeTable[3] = typeof(bool);
		_typeTable[4] = typeof(PathToImageConverter);
		_typeTable[5] = typeof(SKXamlCanvas);
		_typeTable[6] = typeof(Canvas);
		_typeTable[7] = typeof(Panel);
		_typeTable[8] = typeof(SKSize);
		_typeTable[9] = typeof(ValueType);
		_typeTable[10] = typeof(double);
		_typeTable[11] = typeof(NavigationView);
		_typeTable[12] = typeof(ContentControl);
		_typeTable[13] = typeof(NavigationViewBackButtonVisible);
		_typeTable[14] = typeof(Enum);
		_typeTable[15] = typeof(NavigationViewPaneDisplayMode);
		_typeTable[16] = typeof(IList<object>);
		_typeTable[17] = typeof(AutoSuggestBox);
		_typeTable[18] = typeof(UIElement);
		_typeTable[19] = typeof(NavigationViewDisplayMode);
		_typeTable[20] = typeof(DataTemplate);
		_typeTable[21] = typeof(Style);
		_typeTable[22] = typeof(StyleSelector);
		_typeTable[23] = typeof(DataTemplateSelector);
		_typeTable[24] = typeof(NavigationViewOverflowLabelMode);
		_typeTable[25] = typeof(string);
		_typeTable[26] = typeof(NavigationViewSelectionFollowsFocus);
		_typeTable[27] = typeof(NavigationViewShoulderNavigationEnabled);
		_typeTable[28] = typeof(NavigationViewTemplateSettings);
		_typeTable[29] = typeof(DependencyObject);
		_typeTable[30] = typeof(MiniPlayer);
		_typeTable[31] = typeof(UserControl);
		_typeTable[32] = typeof(NavigationViewItem);
		_typeTable[33] = typeof(NavigationViewItemBase);
		_typeTable[34] = typeof(IconElement);
		_typeTable[35] = typeof(InfoBadge);
		_typeTable[36] = typeof(Control);
		_typeTable[37] = typeof(MainWindow);
		_typeTable[38] = typeof(Window);
		_typeTable[39] = typeof(TreeViewNode);
		_typeTable[40] = typeof(IList<TreeViewNode>);
		_typeTable[41] = typeof(int);
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

	private object Activate_4_PathToImageConverter()
	{
		return new PathToImageConverter();
	}

	private object Activate_5_SKXamlCanvas()
	{
		return new SKXamlCanvas();
	}

	private object Activate_11_NavigationView()
	{
		return new NavigationView();
	}

	private object Activate_28_NavigationViewTemplateSettings()
	{
		return new NavigationViewTemplateSettings();
	}

	private object Activate_30_MiniPlayer()
	{
		return new MiniPlayer();
	}

	private object Activate_32_NavigationViewItem()
	{
		return new NavigationViewItem();
	}

	private object Activate_35_InfoBadge()
	{
		return new InfoBadge();
	}

	private object Activate_39_TreeViewNode()
	{
		return new TreeViewNode();
	}

	private void StaticInitializer_0_XamlControlsResources()
	{
		RuntimeHelpers.RunClassConstructor(typeof(XamlControlsResources).TypeHandle);
	}

	private void StaticInitializer_4_PathToImageConverter()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PathToImageConverter).TypeHandle);
	}

	private void StaticInitializer_5_SKXamlCanvas()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SKXamlCanvas).TypeHandle);
	}

	private void StaticInitializer_8_SKSize()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SKSize).TypeHandle);
	}

	private void StaticInitializer_9_ValueType()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ValueType).TypeHandle);
	}

	private void StaticInitializer_11_NavigationView()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationView).TypeHandle);
	}

	private void StaticInitializer_13_NavigationViewBackButtonVisible()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewBackButtonVisible).TypeHandle);
	}

	private void StaticInitializer_14_Enum()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Enum).TypeHandle);
	}

	private void StaticInitializer_15_NavigationViewPaneDisplayMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewPaneDisplayMode).TypeHandle);
	}

	private void StaticInitializer_16_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<object>).TypeHandle);
	}

	private void StaticInitializer_19_NavigationViewDisplayMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewDisplayMode).TypeHandle);
	}

	private void StaticInitializer_24_NavigationViewOverflowLabelMode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewOverflowLabelMode).TypeHandle);
	}

	private void StaticInitializer_26_NavigationViewSelectionFollowsFocus()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewSelectionFollowsFocus).TypeHandle);
	}

	private void StaticInitializer_27_NavigationViewShoulderNavigationEnabled()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewShoulderNavigationEnabled).TypeHandle);
	}

	private void StaticInitializer_28_NavigationViewTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_30_MiniPlayer()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MiniPlayer).TypeHandle);
	}

	private void StaticInitializer_32_NavigationViewItem()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItem).TypeHandle);
	}

	private void StaticInitializer_33_NavigationViewItemBase()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NavigationViewItemBase).TypeHandle);
	}

	private void StaticInitializer_35_InfoBadge()
	{
		RuntimeHelpers.RunClassConstructor(typeof(InfoBadge).TypeHandle);
	}

	private void StaticInitializer_37_MainWindow()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MainWindow).TypeHandle);
	}

	private void StaticInitializer_39_TreeViewNode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TreeViewNode).TypeHandle);
	}

	private void StaticInitializer_40_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<TreeViewNode>).TypeHandle);
	}

	private void MapAdd_0_XamlControlsResources(object instance, object key, object item)
	{
		((IDictionary<object, object>)instance).Add(key, item);
	}

	private void VectorAdd_16_IList(object instance, object item)
	{
		((ICollection<object>)instance).Add(item);
	}

	private void VectorAdd_40_IList(object instance, object item)
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
			XamlUserType xamlUserType18 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.ResourceDictionary"));
			xamlUserType18.Activator = Activate_0_XamlControlsResources;
			xamlUserType18.StaticInitializer = StaticInitializer_0_XamlControlsResources;
			xamlUserType18.DictionaryAdd = MapAdd_0_XamlControlsResources;
			xamlUserType18.AddMemberName("UseCompactResources");
			result = xamlUserType18;
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
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				Activator = Activate_4_PathToImageConverter,
				StaticInitializer = StaticInitializer_4_PathToImageConverter
			};
			break;
		case 5:
		{
			XamlUserType xamlUserType17 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Canvas"));
			xamlUserType17.Activator = Activate_5_SKXamlCanvas;
			xamlUserType17.StaticInitializer = StaticInitializer_5_SKXamlCanvas;
			xamlUserType17.AddMemberName("CanvasSize");
			xamlUserType17.AddMemberName("IgnorePixelScaling");
			xamlUserType17.AddMemberName("Dpi");
			result = xamlUserType17;
			break;
		}
		case 6:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 7:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 8:
		{
			XamlUserType xamlUserType16 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType16.StaticInitializer = StaticInitializer_8_SKSize;
			xamlUserType16.SetIsReturnTypeStub();
			result = xamlUserType16;
			break;
		}
		case 9:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				StaticInitializer = StaticInitializer_9_ValueType
			};
			break;
		case 10:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 11:
		{
			XamlUserType xamlUserType15 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType15.Activator = Activate_11_NavigationView;
			xamlUserType15.StaticInitializer = StaticInitializer_11_NavigationView;
			xamlUserType15.AddMemberName("IsBackButtonVisible");
			xamlUserType15.AddMemberName("IsSettingsVisible");
			xamlUserType15.AddMemberName("PaneDisplayMode");
			xamlUserType15.AddMemberName("OpenPaneLength");
			xamlUserType15.AddMemberName("MenuItems");
			xamlUserType15.AddMemberName("FooterMenuItems");
			xamlUserType15.AddMemberName("AlwaysShowHeader");
			xamlUserType15.AddMemberName("AutoSuggestBox");
			xamlUserType15.AddMemberName("CompactModeThresholdWidth");
			xamlUserType15.AddMemberName("CompactPaneLength");
			xamlUserType15.AddMemberName("ContentOverlay");
			xamlUserType15.AddMemberName("DisplayMode");
			xamlUserType15.AddMemberName("ExpandedModeThresholdWidth");
			xamlUserType15.AddMemberName("FooterMenuItemsSource");
			xamlUserType15.AddMemberName("Header");
			xamlUserType15.AddMemberName("HeaderTemplate");
			xamlUserType15.AddMemberName("IsBackEnabled");
			xamlUserType15.AddMemberName("IsPaneOpen");
			xamlUserType15.AddMemberName("IsPaneToggleButtonVisible");
			xamlUserType15.AddMemberName("IsPaneVisible");
			xamlUserType15.AddMemberName("IsTitleBarAutoPaddingEnabled");
			xamlUserType15.AddMemberName("MenuItemContainerStyle");
			xamlUserType15.AddMemberName("MenuItemContainerStyleSelector");
			xamlUserType15.AddMemberName("MenuItemTemplate");
			xamlUserType15.AddMemberName("MenuItemTemplateSelector");
			xamlUserType15.AddMemberName("MenuItemsSource");
			xamlUserType15.AddMemberName("OverflowLabelMode");
			xamlUserType15.AddMemberName("PaneCustomContent");
			xamlUserType15.AddMemberName("PaneFooter");
			xamlUserType15.AddMemberName("PaneHeader");
			xamlUserType15.AddMemberName("PaneTitle");
			xamlUserType15.AddMemberName("PaneToggleButtonStyle");
			xamlUserType15.AddMemberName("SelectedItem");
			xamlUserType15.AddMemberName("SelectionFollowsFocus");
			xamlUserType15.AddMemberName("SettingsItem");
			xamlUserType15.AddMemberName("ShoulderNavigationEnabled");
			xamlUserType15.AddMemberName("TemplateSettings");
			result = xamlUserType15;
			break;
		}
		case 12:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 13:
		{
			XamlUserType xamlUserType14 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType14.StaticInitializer = StaticInitializer_13_NavigationViewBackButtonVisible;
			xamlUserType14.AddEnumValue("Collapsed", NavigationViewBackButtonVisible.Collapsed);
			xamlUserType14.AddEnumValue("Visible", NavigationViewBackButtonVisible.Visible);
			xamlUserType14.AddEnumValue("Auto", NavigationViewBackButtonVisible.Auto);
			result = xamlUserType14;
			break;
		}
		case 14:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"))
			{
				StaticInitializer = StaticInitializer_14_Enum
			};
			break;
		case 15:
		{
			XamlUserType xamlUserType13 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType13.StaticInitializer = StaticInitializer_15_NavigationViewPaneDisplayMode;
			xamlUserType13.AddEnumValue("Auto", NavigationViewPaneDisplayMode.Auto);
			xamlUserType13.AddEnumValue("Left", NavigationViewPaneDisplayMode.Left);
			xamlUserType13.AddEnumValue("Top", NavigationViewPaneDisplayMode.Top);
			xamlUserType13.AddEnumValue("LeftCompact", NavigationViewPaneDisplayMode.LeftCompact);
			xamlUserType13.AddEnumValue("LeftMinimal", NavigationViewPaneDisplayMode.LeftMinimal);
			result = xamlUserType13;
			break;
		}
		case 16:
		{
			XamlUserType xamlUserType12 = new XamlUserType(this, fullName, type, null);
			xamlUserType12.StaticInitializer = StaticInitializer_16_IList;
			xamlUserType12.CollectionAdd = VectorAdd_16_IList;
			xamlUserType12.SetIsReturnTypeStub();
			result = xamlUserType12;
			break;
		}
		case 17:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 18:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 19:
		{
			XamlUserType xamlUserType11 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType11.StaticInitializer = StaticInitializer_19_NavigationViewDisplayMode;
			xamlUserType11.AddEnumValue("Minimal", NavigationViewDisplayMode.Minimal);
			xamlUserType11.AddEnumValue("Compact", NavigationViewDisplayMode.Compact);
			xamlUserType11.AddEnumValue("Expanded", NavigationViewDisplayMode.Expanded);
			result = xamlUserType11;
			break;
		}
		case 20:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 21:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 22:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 23:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 24:
		{
			XamlUserType xamlUserType10 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType10.StaticInitializer = StaticInitializer_24_NavigationViewOverflowLabelMode;
			xamlUserType10.AddEnumValue("MoreLabel", NavigationViewOverflowLabelMode.MoreLabel);
			xamlUserType10.AddEnumValue("NoLabel", NavigationViewOverflowLabelMode.NoLabel);
			result = xamlUserType10;
			break;
		}
		case 25:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 26:
		{
			XamlUserType xamlUserType9 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType9.StaticInitializer = StaticInitializer_26_NavigationViewSelectionFollowsFocus;
			xamlUserType9.AddEnumValue("Disabled", NavigationViewSelectionFollowsFocus.Disabled);
			xamlUserType9.AddEnumValue("Enabled", NavigationViewSelectionFollowsFocus.Enabled);
			result = xamlUserType9;
			break;
		}
		case 27:
		{
			XamlUserType xamlUserType8 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Enum"));
			xamlUserType8.StaticInitializer = StaticInitializer_27_NavigationViewShoulderNavigationEnabled;
			xamlUserType8.AddEnumValue("WhenSelectionFollowsFocus", NavigationViewShoulderNavigationEnabled.WhenSelectionFollowsFocus);
			xamlUserType8.AddEnumValue("Always", NavigationViewShoulderNavigationEnabled.Always);
			xamlUserType8.AddEnumValue("Never", NavigationViewShoulderNavigationEnabled.Never);
			result = xamlUserType8;
			break;
		}
		case 28:
		{
			XamlUserType xamlUserType7 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType7.StaticInitializer = StaticInitializer_28_NavigationViewTemplateSettings;
			xamlUserType7.SetIsReturnTypeStub();
			result = xamlUserType7;
			break;
		}
		case 29:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 30:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"))
			{
				Activator = Activate_30_MiniPlayer,
				StaticInitializer = StaticInitializer_30_MiniPlayer
			};
			break;
		case 31:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 32:
		{
			XamlUserType xamlUserType6 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItemBase"));
			xamlUserType6.Activator = Activate_32_NavigationViewItem;
			xamlUserType6.StaticInitializer = StaticInitializer_32_NavigationViewItem;
			xamlUserType6.AddMemberName("Icon");
			xamlUserType6.AddMemberName("CompactPaneLength");
			xamlUserType6.AddMemberName("HasUnrealizedChildren");
			xamlUserType6.AddMemberName("InfoBadge");
			xamlUserType6.AddMemberName("IsChildSelected");
			xamlUserType6.AddMemberName("IsExpanded");
			xamlUserType6.AddMemberName("MenuItems");
			xamlUserType6.AddMemberName("MenuItemsSource");
			xamlUserType6.AddMemberName("SelectsOnInvoked");
			result = xamlUserType6;
			break;
		}
		case 33:
		{
			XamlUserType xamlUserType5 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ContentControl"));
			xamlUserType5.StaticInitializer = StaticInitializer_33_NavigationViewItemBase;
			xamlUserType5.AddMemberName("IsSelected");
			result = xamlUserType5;
			break;
		}
		case 34:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 35:
		{
			XamlUserType xamlUserType4 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Control"));
			xamlUserType4.StaticInitializer = StaticInitializer_35_InfoBadge;
			xamlUserType4.SetIsReturnTypeStub();
			result = xamlUserType4;
			break;
		}
		case 36:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 37:
		{
			XamlUserType xamlUserType3 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Window"));
			xamlUserType3.StaticInitializer = StaticInitializer_37_MainWindow;
			xamlUserType3.SetIsLocalType();
			result = xamlUserType3;
			break;
		}
		case 38:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 39:
		{
			XamlUserType xamlUserType2 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType2.Activator = Activate_39_TreeViewNode;
			xamlUserType2.StaticInitializer = StaticInitializer_39_TreeViewNode;
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
		case 40:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_40_IList;
			xamlUserType.CollectionAdd = VectorAdd_40_IList;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 41:
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

	private object get_1_SKXamlCanvas_CanvasSize(object instance)
	{
		return ((SKXamlCanvas)instance).CanvasSize;
	}

	private object get_2_SKXamlCanvas_IgnorePixelScaling(object instance)
	{
		return ((SKXamlCanvas)instance).IgnorePixelScaling;
	}

	private void set_2_SKXamlCanvas_IgnorePixelScaling(object instance, object Value)
	{
		((SKXamlCanvas)instance).IgnorePixelScaling = (bool)Value;
	}

	private object get_3_SKXamlCanvas_Dpi(object instance)
	{
		return ((SKXamlCanvas)instance).Dpi;
	}

	private object get_4_NavigationView_IsBackButtonVisible(object instance)
	{
		return ((NavigationView)instance).IsBackButtonVisible;
	}

	private void set_4_NavigationView_IsBackButtonVisible(object instance, object Value)
	{
		((NavigationView)instance).IsBackButtonVisible = (NavigationViewBackButtonVisible)Value;
	}

	private object get_5_NavigationView_IsSettingsVisible(object instance)
	{
		return ((NavigationView)instance).IsSettingsVisible;
	}

	private void set_5_NavigationView_IsSettingsVisible(object instance, object Value)
	{
		((NavigationView)instance).IsSettingsVisible = (bool)Value;
	}

	private object get_6_NavigationView_PaneDisplayMode(object instance)
	{
		return ((NavigationView)instance).PaneDisplayMode;
	}

	private void set_6_NavigationView_PaneDisplayMode(object instance, object Value)
	{
		((NavigationView)instance).PaneDisplayMode = (NavigationViewPaneDisplayMode)Value;
	}

	private object get_7_NavigationView_OpenPaneLength(object instance)
	{
		return ((NavigationView)instance).OpenPaneLength;
	}

	private void set_7_NavigationView_OpenPaneLength(object instance, object Value)
	{
		((NavigationView)instance).OpenPaneLength = (double)Value;
	}

	private object get_8_NavigationView_MenuItems(object instance)
	{
		return ((NavigationView)instance).MenuItems;
	}

	private object get_9_NavigationView_FooterMenuItems(object instance)
	{
		return ((NavigationView)instance).FooterMenuItems;
	}

	private object get_10_NavigationView_AlwaysShowHeader(object instance)
	{
		return ((NavigationView)instance).AlwaysShowHeader;
	}

	private void set_10_NavigationView_AlwaysShowHeader(object instance, object Value)
	{
		((NavigationView)instance).AlwaysShowHeader = (bool)Value;
	}

	private object get_11_NavigationView_AutoSuggestBox(object instance)
	{
		return ((NavigationView)instance).AutoSuggestBox;
	}

	private void set_11_NavigationView_AutoSuggestBox(object instance, object Value)
	{
		((NavigationView)instance).AutoSuggestBox = (AutoSuggestBox)Value;
	}

	private object get_12_NavigationView_CompactModeThresholdWidth(object instance)
	{
		return ((NavigationView)instance).CompactModeThresholdWidth;
	}

	private void set_12_NavigationView_CompactModeThresholdWidth(object instance, object Value)
	{
		((NavigationView)instance).CompactModeThresholdWidth = (double)Value;
	}

	private object get_13_NavigationView_CompactPaneLength(object instance)
	{
		return ((NavigationView)instance).CompactPaneLength;
	}

	private void set_13_NavigationView_CompactPaneLength(object instance, object Value)
	{
		((NavigationView)instance).CompactPaneLength = (double)Value;
	}

	private object get_14_NavigationView_ContentOverlay(object instance)
	{
		return ((NavigationView)instance).ContentOverlay;
	}

	private void set_14_NavigationView_ContentOverlay(object instance, object Value)
	{
		((NavigationView)instance).ContentOverlay = (UIElement)Value;
	}

	private object get_15_NavigationView_DisplayMode(object instance)
	{
		return ((NavigationView)instance).DisplayMode;
	}

	private object get_16_NavigationView_ExpandedModeThresholdWidth(object instance)
	{
		return ((NavigationView)instance).ExpandedModeThresholdWidth;
	}

	private void set_16_NavigationView_ExpandedModeThresholdWidth(object instance, object Value)
	{
		((NavigationView)instance).ExpandedModeThresholdWidth = (double)Value;
	}

	private object get_17_NavigationView_FooterMenuItemsSource(object instance)
	{
		return ((NavigationView)instance).FooterMenuItemsSource;
	}

	private void set_17_NavigationView_FooterMenuItemsSource(object instance, object Value)
	{
		((NavigationView)instance).FooterMenuItemsSource = Value;
	}

	private object get_18_NavigationView_Header(object instance)
	{
		return ((NavigationView)instance).Header;
	}

	private void set_18_NavigationView_Header(object instance, object Value)
	{
		((NavigationView)instance).Header = Value;
	}

	private object get_19_NavigationView_HeaderTemplate(object instance)
	{
		return ((NavigationView)instance).HeaderTemplate;
	}

	private void set_19_NavigationView_HeaderTemplate(object instance, object Value)
	{
		((NavigationView)instance).HeaderTemplate = (DataTemplate)Value;
	}

	private object get_20_NavigationView_IsBackEnabled(object instance)
	{
		return ((NavigationView)instance).IsBackEnabled;
	}

	private void set_20_NavigationView_IsBackEnabled(object instance, object Value)
	{
		((NavigationView)instance).IsBackEnabled = (bool)Value;
	}

	private object get_21_NavigationView_IsPaneOpen(object instance)
	{
		return ((NavigationView)instance).IsPaneOpen;
	}

	private void set_21_NavigationView_IsPaneOpen(object instance, object Value)
	{
		((NavigationView)instance).IsPaneOpen = (bool)Value;
	}

	private object get_22_NavigationView_IsPaneToggleButtonVisible(object instance)
	{
		return ((NavigationView)instance).IsPaneToggleButtonVisible;
	}

	private void set_22_NavigationView_IsPaneToggleButtonVisible(object instance, object Value)
	{
		((NavigationView)instance).IsPaneToggleButtonVisible = (bool)Value;
	}

	private object get_23_NavigationView_IsPaneVisible(object instance)
	{
		return ((NavigationView)instance).IsPaneVisible;
	}

	private void set_23_NavigationView_IsPaneVisible(object instance, object Value)
	{
		((NavigationView)instance).IsPaneVisible = (bool)Value;
	}

	private object get_24_NavigationView_IsTitleBarAutoPaddingEnabled(object instance)
	{
		return ((NavigationView)instance).IsTitleBarAutoPaddingEnabled;
	}

	private void set_24_NavigationView_IsTitleBarAutoPaddingEnabled(object instance, object Value)
	{
		((NavigationView)instance).IsTitleBarAutoPaddingEnabled = (bool)Value;
	}

	private object get_25_NavigationView_MenuItemContainerStyle(object instance)
	{
		return ((NavigationView)instance).MenuItemContainerStyle;
	}

	private void set_25_NavigationView_MenuItemContainerStyle(object instance, object Value)
	{
		((NavigationView)instance).MenuItemContainerStyle = (Style)Value;
	}

	private object get_26_NavigationView_MenuItemContainerStyleSelector(object instance)
	{
		return ((NavigationView)instance).MenuItemContainerStyleSelector;
	}

	private void set_26_NavigationView_MenuItemContainerStyleSelector(object instance, object Value)
	{
		((NavigationView)instance).MenuItemContainerStyleSelector = (StyleSelector)Value;
	}

	private object get_27_NavigationView_MenuItemTemplate(object instance)
	{
		return ((NavigationView)instance).MenuItemTemplate;
	}

	private void set_27_NavigationView_MenuItemTemplate(object instance, object Value)
	{
		((NavigationView)instance).MenuItemTemplate = (DataTemplate)Value;
	}

	private object get_28_NavigationView_MenuItemTemplateSelector(object instance)
	{
		return ((NavigationView)instance).MenuItemTemplateSelector;
	}

	private void set_28_NavigationView_MenuItemTemplateSelector(object instance, object Value)
	{
		((NavigationView)instance).MenuItemTemplateSelector = (DataTemplateSelector)Value;
	}

	private object get_29_NavigationView_MenuItemsSource(object instance)
	{
		return ((NavigationView)instance).MenuItemsSource;
	}

	private void set_29_NavigationView_MenuItemsSource(object instance, object Value)
	{
		((NavigationView)instance).MenuItemsSource = Value;
	}

	private object get_30_NavigationView_OverflowLabelMode(object instance)
	{
		return ((NavigationView)instance).OverflowLabelMode;
	}

	private void set_30_NavigationView_OverflowLabelMode(object instance, object Value)
	{
		((NavigationView)instance).OverflowLabelMode = (NavigationViewOverflowLabelMode)Value;
	}

	private object get_31_NavigationView_PaneCustomContent(object instance)
	{
		return ((NavigationView)instance).PaneCustomContent;
	}

	private void set_31_NavigationView_PaneCustomContent(object instance, object Value)
	{
		((NavigationView)instance).PaneCustomContent = (UIElement)Value;
	}

	private object get_32_NavigationView_PaneFooter(object instance)
	{
		return ((NavigationView)instance).PaneFooter;
	}

	private void set_32_NavigationView_PaneFooter(object instance, object Value)
	{
		((NavigationView)instance).PaneFooter = (UIElement)Value;
	}

	private object get_33_NavigationView_PaneHeader(object instance)
	{
		return ((NavigationView)instance).PaneHeader;
	}

	private void set_33_NavigationView_PaneHeader(object instance, object Value)
	{
		((NavigationView)instance).PaneHeader = (UIElement)Value;
	}

	private object get_34_NavigationView_PaneTitle(object instance)
	{
		return ((NavigationView)instance).PaneTitle;
	}

	private void set_34_NavigationView_PaneTitle(object instance, object Value)
	{
		((NavigationView)instance).PaneTitle = (string)Value;
	}

	private object get_35_NavigationView_PaneToggleButtonStyle(object instance)
	{
		return ((NavigationView)instance).PaneToggleButtonStyle;
	}

	private void set_35_NavigationView_PaneToggleButtonStyle(object instance, object Value)
	{
		((NavigationView)instance).PaneToggleButtonStyle = (Style)Value;
	}

	private object get_36_NavigationView_SelectedItem(object instance)
	{
		return ((NavigationView)instance).SelectedItem;
	}

	private void set_36_NavigationView_SelectedItem(object instance, object Value)
	{
		((NavigationView)instance).SelectedItem = Value;
	}

	private object get_37_NavigationView_SelectionFollowsFocus(object instance)
	{
		return ((NavigationView)instance).SelectionFollowsFocus;
	}

	private void set_37_NavigationView_SelectionFollowsFocus(object instance, object Value)
	{
		((NavigationView)instance).SelectionFollowsFocus = (NavigationViewSelectionFollowsFocus)Value;
	}

	private object get_38_NavigationView_SettingsItem(object instance)
	{
		return ((NavigationView)instance).SettingsItem;
	}

	private object get_39_NavigationView_ShoulderNavigationEnabled(object instance)
	{
		return ((NavigationView)instance).ShoulderNavigationEnabled;
	}

	private void set_39_NavigationView_ShoulderNavigationEnabled(object instance, object Value)
	{
		((NavigationView)instance).ShoulderNavigationEnabled = (NavigationViewShoulderNavigationEnabled)Value;
	}

	private object get_40_NavigationView_TemplateSettings(object instance)
	{
		return ((NavigationView)instance).TemplateSettings;
	}

	private object get_41_NavigationViewItem_Icon(object instance)
	{
		return ((NavigationViewItem)instance).Icon;
	}

	private void set_41_NavigationViewItem_Icon(object instance, object Value)
	{
		((NavigationViewItem)instance).Icon = (IconElement)Value;
	}

	private object get_42_NavigationViewItem_CompactPaneLength(object instance)
	{
		return ((NavigationViewItem)instance).CompactPaneLength;
	}

	private object get_43_NavigationViewItem_HasUnrealizedChildren(object instance)
	{
		return ((NavigationViewItem)instance).HasUnrealizedChildren;
	}

	private void set_43_NavigationViewItem_HasUnrealizedChildren(object instance, object Value)
	{
		((NavigationViewItem)instance).HasUnrealizedChildren = (bool)Value;
	}

	private object get_44_NavigationViewItem_InfoBadge(object instance)
	{
		return ((NavigationViewItem)instance).InfoBadge;
	}

	private void set_44_NavigationViewItem_InfoBadge(object instance, object Value)
	{
		((NavigationViewItem)instance).InfoBadge = (InfoBadge)Value;
	}

	private object get_45_NavigationViewItem_IsChildSelected(object instance)
	{
		return ((NavigationViewItem)instance).IsChildSelected;
	}

	private void set_45_NavigationViewItem_IsChildSelected(object instance, object Value)
	{
		((NavigationViewItem)instance).IsChildSelected = (bool)Value;
	}

	private object get_46_NavigationViewItem_IsExpanded(object instance)
	{
		return ((NavigationViewItem)instance).IsExpanded;
	}

	private void set_46_NavigationViewItem_IsExpanded(object instance, object Value)
	{
		((NavigationViewItem)instance).IsExpanded = (bool)Value;
	}

	private object get_47_NavigationViewItem_MenuItems(object instance)
	{
		return ((NavigationViewItem)instance).MenuItems;
	}

	private object get_48_NavigationViewItem_MenuItemsSource(object instance)
	{
		return ((NavigationViewItem)instance).MenuItemsSource;
	}

	private void set_48_NavigationViewItem_MenuItemsSource(object instance, object Value)
	{
		((NavigationViewItem)instance).MenuItemsSource = Value;
	}

	private object get_49_NavigationViewItem_SelectsOnInvoked(object instance)
	{
		return ((NavigationViewItem)instance).SelectsOnInvoked;
	}

	private void set_49_NavigationViewItem_SelectsOnInvoked(object instance, object Value)
	{
		((NavigationViewItem)instance).SelectsOnInvoked = (bool)Value;
	}

	private object get_50_NavigationViewItemBase_IsSelected(object instance)
	{
		return ((NavigationViewItemBase)instance).IsSelected;
	}

	private void set_50_NavigationViewItemBase_IsSelected(object instance, object Value)
	{
		((NavigationViewItemBase)instance).IsSelected = (bool)Value;
	}

	private object get_51_TreeViewNode_Children(object instance)
	{
		return ((TreeViewNode)instance).Children;
	}

	private object get_52_TreeViewNode_Content(object instance)
	{
		return ((TreeViewNode)instance).Content;
	}

	private void set_52_TreeViewNode_Content(object instance, object Value)
	{
		((TreeViewNode)instance).Content = Value;
	}

	private object get_53_TreeViewNode_Depth(object instance)
	{
		return ((TreeViewNode)instance).Depth;
	}

	private object get_54_TreeViewNode_HasChildren(object instance)
	{
		return ((TreeViewNode)instance).HasChildren;
	}

	private object get_55_TreeViewNode_HasUnrealizedChildren(object instance)
	{
		return ((TreeViewNode)instance).HasUnrealizedChildren;
	}

	private void set_55_TreeViewNode_HasUnrealizedChildren(object instance, object Value)
	{
		((TreeViewNode)instance).HasUnrealizedChildren = (bool)Value;
	}

	private object get_56_TreeViewNode_IsExpanded(object instance)
	{
		return ((TreeViewNode)instance).IsExpanded;
	}

	private void set_56_TreeViewNode_IsExpanded(object instance, object Value)
	{
		((TreeViewNode)instance).IsExpanded = (bool)Value;
	}

	private object get_57_TreeViewNode_Parent(object instance)
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
		case "SkiaSharp.Views.Windows.SKXamlCanvas.CanvasSize":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "CanvasSize", "SkiaSharp.SKSize");
			xamlMember.Getter = get_1_SKXamlCanvas_CanvasSize;
			xamlMember.SetIsReadOnly();
			break;
		case "SkiaSharp.Views.Windows.SKXamlCanvas.IgnorePixelScaling":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "IgnorePixelScaling", "Boolean");
			xamlMember.Getter = get_2_SKXamlCanvas_IgnorePixelScaling;
			xamlMember.Setter = set_2_SKXamlCanvas_IgnorePixelScaling;
			break;
		case "SkiaSharp.Views.Windows.SKXamlCanvas.Dpi":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "Dpi", "Double");
			xamlMember.Getter = get_3_SKXamlCanvas_Dpi;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsBackButtonVisible":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsBackButtonVisible", "Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_4_NavigationView_IsBackButtonVisible;
			xamlMember.Setter = set_4_NavigationView_IsBackButtonVisible;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsSettingsVisible":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsSettingsVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_5_NavigationView_IsSettingsVisible;
			xamlMember.Setter = set_5_NavigationView_IsSettingsVisible;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneDisplayMode":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneDisplayMode", "Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_6_NavigationView_PaneDisplayMode;
			xamlMember.Setter = set_6_NavigationView_PaneDisplayMode;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.OpenPaneLength":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "OpenPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_7_NavigationView_OpenPaneLength;
			xamlMember.Setter = set_7_NavigationView_OpenPaneLength;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItems":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_8_NavigationView_MenuItems;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.FooterMenuItems":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "FooterMenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_9_NavigationView_FooterMenuItems;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.AlwaysShowHeader":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "AlwaysShowHeader", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_10_NavigationView_AlwaysShowHeader;
			xamlMember.Setter = set_10_NavigationView_AlwaysShowHeader;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.AutoSuggestBox":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "AutoSuggestBox", "Microsoft.UI.Xaml.Controls.AutoSuggestBox");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_11_NavigationView_AutoSuggestBox;
			xamlMember.Setter = set_11_NavigationView_AutoSuggestBox;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.CompactModeThresholdWidth":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "CompactModeThresholdWidth", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_12_NavigationView_CompactModeThresholdWidth;
			xamlMember.Setter = set_12_NavigationView_CompactModeThresholdWidth;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.CompactPaneLength":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "CompactPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_13_NavigationView_CompactPaneLength;
			xamlMember.Setter = set_13_NavigationView_CompactPaneLength;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.ContentOverlay":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ContentOverlay", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_14_NavigationView_ContentOverlay;
			xamlMember.Setter = set_14_NavigationView_ContentOverlay;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.DisplayMode":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "DisplayMode", "Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_15_NavigationView_DisplayMode;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.ExpandedModeThresholdWidth":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ExpandedModeThresholdWidth", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_16_NavigationView_ExpandedModeThresholdWidth;
			xamlMember.Setter = set_16_NavigationView_ExpandedModeThresholdWidth;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.FooterMenuItemsSource":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "FooterMenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_17_NavigationView_FooterMenuItemsSource;
			xamlMember.Setter = set_17_NavigationView_FooterMenuItemsSource;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.Header":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "Header", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_18_NavigationView_Header;
			xamlMember.Setter = set_18_NavigationView_Header;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.HeaderTemplate":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "HeaderTemplate", "Microsoft.UI.Xaml.DataTemplate");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_19_NavigationView_HeaderTemplate;
			xamlMember.Setter = set_19_NavigationView_HeaderTemplate;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsBackEnabled":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsBackEnabled", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_20_NavigationView_IsBackEnabled;
			xamlMember.Setter = set_20_NavigationView_IsBackEnabled;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneOpen":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneOpen", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_21_NavigationView_IsPaneOpen;
			xamlMember.Setter = set_21_NavigationView_IsPaneOpen;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneToggleButtonVisible":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneToggleButtonVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_22_NavigationView_IsPaneToggleButtonVisible;
			xamlMember.Setter = set_22_NavigationView_IsPaneToggleButtonVisible;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsPaneVisible":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsPaneVisible", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_23_NavigationView_IsPaneVisible;
			xamlMember.Setter = set_23_NavigationView_IsPaneVisible;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.IsTitleBarAutoPaddingEnabled":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "IsTitleBarAutoPaddingEnabled", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_24_NavigationView_IsTitleBarAutoPaddingEnabled;
			xamlMember.Setter = set_24_NavigationView_IsTitleBarAutoPaddingEnabled;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemContainerStyle":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemContainerStyle", "Microsoft.UI.Xaml.Style");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_25_NavigationView_MenuItemContainerStyle;
			xamlMember.Setter = set_25_NavigationView_MenuItemContainerStyle;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemContainerStyleSelector":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemContainerStyleSelector", "Microsoft.UI.Xaml.Controls.StyleSelector");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_26_NavigationView_MenuItemContainerStyleSelector;
			xamlMember.Setter = set_26_NavigationView_MenuItemContainerStyleSelector;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemTemplate":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemTemplate", "Microsoft.UI.Xaml.DataTemplate");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_27_NavigationView_MenuItemTemplate;
			xamlMember.Setter = set_27_NavigationView_MenuItemTemplate;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemTemplateSelector":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemTemplateSelector", "Microsoft.UI.Xaml.Controls.DataTemplateSelector");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_28_NavigationView_MenuItemTemplateSelector;
			xamlMember.Setter = set_28_NavigationView_MenuItemTemplateSelector;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.MenuItemsSource":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "MenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_29_NavigationView_MenuItemsSource;
			xamlMember.Setter = set_29_NavigationView_MenuItemsSource;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.OverflowLabelMode":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "OverflowLabelMode", "Microsoft.UI.Xaml.Controls.NavigationViewOverflowLabelMode");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_30_NavigationView_OverflowLabelMode;
			xamlMember.Setter = set_30_NavigationView_OverflowLabelMode;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneCustomContent":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneCustomContent", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_31_NavigationView_PaneCustomContent;
			xamlMember.Setter = set_31_NavigationView_PaneCustomContent;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneFooter":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneFooter", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_32_NavigationView_PaneFooter;
			xamlMember.Setter = set_32_NavigationView_PaneFooter;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneHeader":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneHeader", "Microsoft.UI.Xaml.UIElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_33_NavigationView_PaneHeader;
			xamlMember.Setter = set_33_NavigationView_PaneHeader;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneTitle":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneTitle", "String");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_34_NavigationView_PaneTitle;
			xamlMember.Setter = set_34_NavigationView_PaneTitle;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.PaneToggleButtonStyle":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "PaneToggleButtonStyle", "Microsoft.UI.Xaml.Style");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_35_NavigationView_PaneToggleButtonStyle;
			xamlMember.Setter = set_35_NavigationView_PaneToggleButtonStyle;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.SelectedItem":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SelectedItem", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_36_NavigationView_SelectedItem;
			xamlMember.Setter = set_36_NavigationView_SelectedItem;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.SelectionFollowsFocus":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SelectionFollowsFocus", "Microsoft.UI.Xaml.Controls.NavigationViewSelectionFollowsFocus");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_37_NavigationView_SelectionFollowsFocus;
			xamlMember.Setter = set_37_NavigationView_SelectionFollowsFocus;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.SettingsItem":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "SettingsItem", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_38_NavigationView_SettingsItem;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.ShoulderNavigationEnabled":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "ShoulderNavigationEnabled", "Microsoft.UI.Xaml.Controls.NavigationViewShoulderNavigationEnabled");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_39_NavigationView_ShoulderNavigationEnabled;
			xamlMember.Setter = set_39_NavigationView_ShoulderNavigationEnabled;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationView.TemplateSettings":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationView");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.NavigationViewTemplateSettings");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_40_NavigationView_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.Icon":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "Icon", "Microsoft.UI.Xaml.Controls.IconElement");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_41_NavigationViewItem_Icon;
			xamlMember.Setter = set_41_NavigationViewItem_Icon;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.CompactPaneLength":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "CompactPaneLength", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_42_NavigationViewItem_CompactPaneLength;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.HasUnrealizedChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_43_NavigationViewItem_HasUnrealizedChildren;
			xamlMember.Setter = set_43_NavigationViewItem_HasUnrealizedChildren;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.InfoBadge":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "InfoBadge", "Microsoft.UI.Xaml.Controls.InfoBadge");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_44_NavigationViewItem_InfoBadge;
			xamlMember.Setter = set_44_NavigationViewItem_InfoBadge;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.IsChildSelected":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "IsChildSelected", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_45_NavigationViewItem_IsChildSelected;
			xamlMember.Setter = set_45_NavigationViewItem_IsChildSelected;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.IsExpanded":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_46_NavigationViewItem_IsExpanded;
			xamlMember.Setter = set_46_NavigationViewItem_IsExpanded;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.MenuItems":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "MenuItems", "System.Collections.Generic.IList`1<Object>");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_47_NavigationViewItem_MenuItems;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.MenuItemsSource":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "MenuItemsSource", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_48_NavigationViewItem_MenuItemsSource;
			xamlMember.Setter = set_48_NavigationViewItem_MenuItemsSource;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItem.SelectsOnInvoked":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItem");
			xamlMember = new XamlMember(this, "SelectsOnInvoked", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_49_NavigationViewItem_SelectsOnInvoked;
			xamlMember.Setter = set_49_NavigationViewItem_SelectsOnInvoked;
			break;
		case "Microsoft.UI.Xaml.Controls.NavigationViewItemBase.IsSelected":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.NavigationViewItemBase");
			xamlMember = new XamlMember(this, "IsSelected", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_50_NavigationViewItemBase_IsSelected;
			xamlMember.Setter = set_50_NavigationViewItemBase_IsSelected;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Children":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Children", "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>");
			xamlMember.Getter = get_51_TreeViewNode_Children;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Content":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Content", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_52_TreeViewNode_Content;
			xamlMember.Setter = set_52_TreeViewNode_Content;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Depth":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Depth", "Int32");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_53_TreeViewNode_Depth;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_54_TreeViewNode_HasChildren;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.Getter = get_55_TreeViewNode_HasUnrealizedChildren;
			xamlMember.Setter = set_55_TreeViewNode_HasUnrealizedChildren;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.IsExpanded":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_56_TreeViewNode_IsExpanded;
			xamlMember.Setter = set_56_TreeViewNode_IsExpanded;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Parent":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Parent", "Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember.Getter = get_57_TreeViewNode_Parent;
			xamlMember.SetIsReadOnly();
			break;
		}
		return xamlMember;
	}
}
