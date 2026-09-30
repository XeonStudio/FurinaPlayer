using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using LibVLCSharp.LibVLCSharp_XamlTypeInfo;
using LiveChartsCore.SkiaSharpView.WinUI.LiveChartsCore_SkiaSharpView_WinUI_XamlTypeInfo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.XamlTypeInfo;
using SkiaSharp;
using SkiaSharp.Views.Windows;

namespace FurinaPlayer.UI.FurinaPlayer_UI_XamlTypeInfo;

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
		_typeNameTable = new string[44];
		_typeNameTable[0] = "FurinaPlayer.UI.Controls.EqualizerControl";
		_typeNameTable[1] = "Microsoft.UI.Xaml.Controls.UserControl";
		_typeNameTable[2] = "Double[]";
		_typeNameTable[3] = "System.Array";
		_typeNameTable[4] = "Object";
		_typeNameTable[5] = "Double";
		_typeNameTable[6] = "FurinaPlayer.UI.Controls.LiquidGlassBorder";
		_typeNameTable[7] = "FurinaPlayer.UI.Controls.LyricsPanel";
		_typeNameTable[8] = "FurinaPlayer.UI.Controls.MiniPlayer";
		_typeNameTable[9] = "SkiaSharp.Views.Windows.SKXamlCanvas";
		_typeNameTable[10] = "Microsoft.UI.Xaml.Controls.Canvas";
		_typeNameTable[11] = "Microsoft.UI.Xaml.Controls.Panel";
		_typeNameTable[12] = "SkiaSharp.SKSize";
		_typeNameTable[13] = "System.ValueType";
		_typeNameTable[14] = "Boolean";
		_typeNameTable[15] = "FurinaPlayer.UI.Controls.PhaseAnalyzerControl";
		_typeNameTable[16] = "FurinaPlayer.UI.Controls.SpectrumAnalyzer";
		_typeNameTable[17] = "FurinaPlayer.UI.Controls.WaveformControl";
		_typeNameTable[18] = "Microsoft.UI.Xaml.Controls.ProgressBar";
		_typeNameTable[19] = "Microsoft.UI.Xaml.Controls.Primitives.RangeBase";
		_typeNameTable[20] = "Microsoft.UI.Xaml.Controls.ProgressBarTemplateSettings";
		_typeNameTable[21] = "Microsoft.UI.Xaml.DependencyObject";
		_typeNameTable[22] = "FurinaPlayer.UI.Pages.DspPage";
		_typeNameTable[23] = "Microsoft.UI.Xaml.Controls.Page";
		_typeNameTable[24] = "FurinaPlayer.UI.ViewModels.DspViewModel";
		_typeNameTable[25] = "CommunityToolkit.Mvvm.ComponentModel.ObservableObject";
		_typeNameTable[26] = "FurinaPlayer.UI.Pages.EditorPage";
		_typeNameTable[27] = "FurinaPlayer.UI.ViewModels.EditorViewModel";
		_typeNameTable[28] = "Microsoft.UI.Xaml.Controls.ProgressRing";
		_typeNameTable[29] = "Microsoft.UI.Xaml.Controls.Control";
		_typeNameTable[30] = "Microsoft.UI.Xaml.Controls.ProgressRingTemplateSettings";
		_typeNameTable[31] = "FurinaPlayer.UI.Pages.HomePage";
		_typeNameTable[32] = "FurinaPlayer.UI.ViewModels.HomeViewModel";
		_typeNameTable[33] = "FurinaPlayer.UI.Pages.NowPlayingPage";
		_typeNameTable[34] = "FurinaPlayer.UI.ViewModels.NowPlayingViewModel";
		_typeNameTable[35] = "FurinaPlayer.UI.Pages.SettingsPage";
		_typeNameTable[36] = "FurinaPlayer.UI.ViewModels.SettingsViewModel";
		_typeNameTable[37] = "Microsoft.UI.Xaml.Controls.GridViewItem";
		_typeNameTable[38] = "Microsoft.UI.Xaml.Controls.ListViewItem";
		_typeNameTable[39] = "Microsoft.UI.Xaml.Controls.Primitives.ToggleButton";
		_typeNameTable[40] = "Microsoft.UI.Xaml.Controls.Button";
		_typeNameTable[41] = "Microsoft.UI.Xaml.Controls.TreeViewNode";
		_typeNameTable[42] = "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>";
		_typeNameTable[43] = "Int32";
		_typeTable = new Type[44];
		_typeTable[0] = typeof(EqualizerControl);
		_typeTable[1] = typeof(UserControl);
		_typeTable[2] = typeof(double[]);
		_typeTable[3] = typeof(Array);
		_typeTable[4] = typeof(object);
		_typeTable[5] = typeof(double);
		_typeTable[6] = typeof(LiquidGlassBorder);
		_typeTable[7] = typeof(LyricsPanel);
		_typeTable[8] = typeof(MiniPlayer);
		_typeTable[9] = typeof(SKXamlCanvas);
		_typeTable[10] = typeof(Canvas);
		_typeTable[11] = typeof(Panel);
		_typeTable[12] = typeof(SKSize);
		_typeTable[13] = typeof(ValueType);
		_typeTable[14] = typeof(bool);
		_typeTable[15] = typeof(PhaseAnalyzerControl);
		_typeTable[16] = typeof(SpectrumAnalyzer);
		_typeTable[17] = typeof(WaveformControl);
		_typeTable[18] = typeof(ProgressBar);
		_typeTable[19] = typeof(RangeBase);
		_typeTable[20] = typeof(ProgressBarTemplateSettings);
		_typeTable[21] = typeof(DependencyObject);
		_typeTable[22] = typeof(DspPage);
		_typeTable[23] = typeof(Page);
		_typeTable[24] = typeof(DspViewModel);
		_typeTable[25] = typeof(ObservableObject);
		_typeTable[26] = typeof(EditorPage);
		_typeTable[27] = typeof(EditorViewModel);
		_typeTable[28] = typeof(ProgressRing);
		_typeTable[29] = typeof(Control);
		_typeTable[30] = typeof(ProgressRingTemplateSettings);
		_typeTable[31] = typeof(HomePage);
		_typeTable[32] = typeof(HomeViewModel);
		_typeTable[33] = typeof(NowPlayingPage);
		_typeTable[34] = typeof(NowPlayingViewModel);
		_typeTable[35] = typeof(SettingsPage);
		_typeTable[36] = typeof(SettingsViewModel);
		_typeTable[37] = typeof(GridViewItem);
		_typeTable[38] = typeof(ListViewItem);
		_typeTable[39] = typeof(ToggleButton);
		_typeTable[40] = typeof(Button);
		_typeTable[41] = typeof(TreeViewNode);
		_typeTable[42] = typeof(IList<TreeViewNode>);
		_typeTable[43] = typeof(int);
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

	private object Activate_0_EqualizerControl()
	{
		return new EqualizerControl();
	}

	private object Activate_6_LiquidGlassBorder()
	{
		return new LiquidGlassBorder();
	}

	private object Activate_7_LyricsPanel()
	{
		return new LyricsPanel();
	}

	private object Activate_8_MiniPlayer()
	{
		return new MiniPlayer();
	}

	private object Activate_9_SKXamlCanvas()
	{
		return new SKXamlCanvas();
	}

	private object Activate_15_PhaseAnalyzerControl()
	{
		return new PhaseAnalyzerControl();
	}

	private object Activate_16_SpectrumAnalyzer()
	{
		return new SpectrumAnalyzer();
	}

	private object Activate_17_WaveformControl()
	{
		return new WaveformControl();
	}

	private object Activate_18_ProgressBar()
	{
		return new ProgressBar();
	}

	private object Activate_22_DspPage()
	{
		return new DspPage();
	}

	private object Activate_26_EditorPage()
	{
		return new EditorPage();
	}

	private object Activate_28_ProgressRing()
	{
		return new ProgressRing();
	}

	private object Activate_31_HomePage()
	{
		return new HomePage();
	}

	private object Activate_33_NowPlayingPage()
	{
		return new NowPlayingPage();
	}

	private object Activate_35_SettingsPage()
	{
		return new SettingsPage();
	}

	private object Activate_41_TreeViewNode()
	{
		return new TreeViewNode();
	}

	private void StaticInitializer_0_EqualizerControl()
	{
		RuntimeHelpers.RunClassConstructor(typeof(EqualizerControl).TypeHandle);
	}

	private void StaticInitializer_3_Array()
	{
		RuntimeHelpers.RunClassConstructor(typeof(Array).TypeHandle);
	}

	private void StaticInitializer_6_LiquidGlassBorder()
	{
		RuntimeHelpers.RunClassConstructor(typeof(LiquidGlassBorder).TypeHandle);
	}

	private void StaticInitializer_7_LyricsPanel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(LyricsPanel).TypeHandle);
	}

	private void StaticInitializer_8_MiniPlayer()
	{
		RuntimeHelpers.RunClassConstructor(typeof(MiniPlayer).TypeHandle);
	}

	private void StaticInitializer_9_SKXamlCanvas()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SKXamlCanvas).TypeHandle);
	}

	private void StaticInitializer_12_SKSize()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SKSize).TypeHandle);
	}

	private void StaticInitializer_13_ValueType()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ValueType).TypeHandle);
	}

	private void StaticInitializer_15_PhaseAnalyzerControl()
	{
		RuntimeHelpers.RunClassConstructor(typeof(PhaseAnalyzerControl).TypeHandle);
	}

	private void StaticInitializer_16_SpectrumAnalyzer()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SpectrumAnalyzer).TypeHandle);
	}

	private void StaticInitializer_17_WaveformControl()
	{
		RuntimeHelpers.RunClassConstructor(typeof(WaveformControl).TypeHandle);
	}

	private void StaticInitializer_18_ProgressBar()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressBar).TypeHandle);
	}

	private void StaticInitializer_20_ProgressBarTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressBarTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_22_DspPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(DspPage).TypeHandle);
	}

	private void StaticInitializer_24_DspViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(DspViewModel).TypeHandle);
	}

	private void StaticInitializer_25_ObservableObject()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ObservableObject).TypeHandle);
	}

	private void StaticInitializer_26_EditorPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(EditorPage).TypeHandle);
	}

	private void StaticInitializer_27_EditorViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(EditorViewModel).TypeHandle);
	}

	private void StaticInitializer_28_ProgressRing()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressRing).TypeHandle);
	}

	private void StaticInitializer_30_ProgressRingTemplateSettings()
	{
		RuntimeHelpers.RunClassConstructor(typeof(ProgressRingTemplateSettings).TypeHandle);
	}

	private void StaticInitializer_31_HomePage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(HomePage).TypeHandle);
	}

	private void StaticInitializer_32_HomeViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(HomeViewModel).TypeHandle);
	}

	private void StaticInitializer_33_NowPlayingPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NowPlayingPage).TypeHandle);
	}

	private void StaticInitializer_34_NowPlayingViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(NowPlayingViewModel).TypeHandle);
	}

	private void StaticInitializer_35_SettingsPage()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SettingsPage).TypeHandle);
	}

	private void StaticInitializer_36_SettingsViewModel()
	{
		RuntimeHelpers.RunClassConstructor(typeof(SettingsViewModel).TypeHandle);
	}

	private void StaticInitializer_41_TreeViewNode()
	{
		RuntimeHelpers.RunClassConstructor(typeof(TreeViewNode).TypeHandle);
	}

	private void StaticInitializer_42_IList()
	{
		RuntimeHelpers.RunClassConstructor(typeof(IList<TreeViewNode>).TypeHandle);
	}

	private void VectorAdd_42_IList(object instance, object item)
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
			XamlUserType xamlUserType26 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType26.Activator = Activate_0_EqualizerControl;
			xamlUserType26.StaticInitializer = StaticInitializer_0_EqualizerControl;
			xamlUserType26.AddMemberName("Gains");
			xamlUserType26.SetIsLocalType();
			result = xamlUserType26;
			break;
		}
		case 1:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 2:
		{
			XamlUserType xamlUserType25 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.Array"));
			xamlUserType25.SetIsReturnTypeStub();
			result = xamlUserType25;
			break;
		}
		case 3:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				StaticInitializer = StaticInitializer_3_Array
			};
			break;
		case 4:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 5:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 6:
		{
			XamlUserType xamlUserType24 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType24.Activator = Activate_6_LiquidGlassBorder;
			xamlUserType24.StaticInitializer = StaticInitializer_6_LiquidGlassBorder;
			xamlUserType24.SetContentPropertyName("FurinaPlayer.UI.Controls.LiquidGlassBorder.Content");
			xamlUserType24.AddMemberName("Content");
			xamlUserType24.SetIsLocalType();
			result = xamlUserType24;
			break;
		}
		case 7:
		{
			XamlUserType xamlUserType23 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType23.Activator = Activate_7_LyricsPanel;
			xamlUserType23.StaticInitializer = StaticInitializer_7_LyricsPanel;
			xamlUserType23.SetIsLocalType();
			result = xamlUserType23;
			break;
		}
		case 8:
		{
			XamlUserType xamlUserType22 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType22.Activator = Activate_8_MiniPlayer;
			xamlUserType22.StaticInitializer = StaticInitializer_8_MiniPlayer;
			xamlUserType22.SetIsLocalType();
			result = xamlUserType22;
			break;
		}
		case 9:
		{
			XamlUserType xamlUserType21 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Canvas"));
			xamlUserType21.Activator = Activate_9_SKXamlCanvas;
			xamlUserType21.StaticInitializer = StaticInitializer_9_SKXamlCanvas;
			xamlUserType21.AddMemberName("CanvasSize");
			xamlUserType21.AddMemberName("IgnorePixelScaling");
			xamlUserType21.AddMemberName("Dpi");
			result = xamlUserType21;
			break;
		}
		case 10:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 11:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 12:
		{
			XamlUserType xamlUserType20 = new XamlUserType(this, fullName, type, GetXamlTypeByName("System.ValueType"));
			xamlUserType20.StaticInitializer = StaticInitializer_12_SKSize;
			xamlUserType20.SetIsReturnTypeStub();
			result = xamlUserType20;
			break;
		}
		case 13:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				StaticInitializer = StaticInitializer_13_ValueType
			};
			break;
		case 14:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 15:
		{
			XamlUserType xamlUserType19 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType19.Activator = Activate_15_PhaseAnalyzerControl;
			xamlUserType19.StaticInitializer = StaticInitializer_15_PhaseAnalyzerControl;
			xamlUserType19.SetIsLocalType();
			result = xamlUserType19;
			break;
		}
		case 16:
		{
			XamlUserType xamlUserType18 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType18.Activator = Activate_16_SpectrumAnalyzer;
			xamlUserType18.StaticInitializer = StaticInitializer_16_SpectrumAnalyzer;
			xamlUserType18.SetIsLocalType();
			result = xamlUserType18;
			break;
		}
		case 17:
		{
			XamlUserType xamlUserType17 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.UserControl"));
			xamlUserType17.Activator = Activate_17_WaveformControl;
			xamlUserType17.StaticInitializer = StaticInitializer_17_WaveformControl;
			xamlUserType17.SetIsLocalType();
			result = xamlUserType17;
			break;
		}
		case 18:
		{
			XamlUserType xamlUserType16 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Primitives.RangeBase"));
			xamlUserType16.Activator = Activate_18_ProgressBar;
			xamlUserType16.StaticInitializer = StaticInitializer_18_ProgressBar;
			xamlUserType16.AddMemberName("IsIndeterminate");
			xamlUserType16.AddMemberName("ShowError");
			xamlUserType16.AddMemberName("ShowPaused");
			xamlUserType16.AddMemberName("TemplateSettings");
			result = xamlUserType16;
			break;
		}
		case 19:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 20:
		{
			XamlUserType xamlUserType15 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType15.StaticInitializer = StaticInitializer_20_ProgressBarTemplateSettings;
			xamlUserType15.SetIsReturnTypeStub();
			result = xamlUserType15;
			break;
		}
		case 21:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 22:
		{
			XamlUserType xamlUserType14 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType14.Activator = Activate_22_DspPage;
			xamlUserType14.StaticInitializer = StaticInitializer_22_DspPage;
			xamlUserType14.AddMemberName("ViewModel");
			xamlUserType14.SetIsLocalType();
			result = xamlUserType14;
			break;
		}
		case 23:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 24:
		{
			XamlUserType xamlUserType13 = new XamlUserType(this, fullName, type, GetXamlTypeByName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject"));
			xamlUserType13.StaticInitializer = StaticInitializer_24_DspViewModel;
			xamlUserType13.SetIsReturnTypeStub();
			xamlUserType13.SetIsLocalType();
			result = xamlUserType13;
			break;
		}
		case 25:
			result = new XamlUserType(this, fullName, type, GetXamlTypeByName("Object"))
			{
				StaticInitializer = StaticInitializer_25_ObservableObject
			};
			break;
		case 26:
		{
			XamlUserType xamlUserType12 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType12.Activator = Activate_26_EditorPage;
			xamlUserType12.StaticInitializer = StaticInitializer_26_EditorPage;
			xamlUserType12.AddMemberName("ViewModel");
			xamlUserType12.SetIsLocalType();
			result = xamlUserType12;
			break;
		}
		case 27:
		{
			XamlUserType xamlUserType11 = new XamlUserType(this, fullName, type, GetXamlTypeByName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject"));
			xamlUserType11.StaticInitializer = StaticInitializer_27_EditorViewModel;
			xamlUserType11.SetIsReturnTypeStub();
			xamlUserType11.SetIsLocalType();
			result = xamlUserType11;
			break;
		}
		case 28:
		{
			XamlUserType xamlUserType10 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Control"));
			xamlUserType10.Activator = Activate_28_ProgressRing;
			xamlUserType10.StaticInitializer = StaticInitializer_28_ProgressRing;
			xamlUserType10.AddMemberName("IsActive");
			xamlUserType10.AddMemberName("IsIndeterminate");
			xamlUserType10.AddMemberName("Maximum");
			xamlUserType10.AddMemberName("Minimum");
			xamlUserType10.AddMemberName("TemplateSettings");
			xamlUserType10.AddMemberName("Value");
			result = xamlUserType10;
			break;
		}
		case 29:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 30:
		{
			XamlUserType xamlUserType9 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType9.StaticInitializer = StaticInitializer_30_ProgressRingTemplateSettings;
			xamlUserType9.SetIsReturnTypeStub();
			result = xamlUserType9;
			break;
		}
		case 31:
		{
			XamlUserType xamlUserType8 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType8.Activator = Activate_31_HomePage;
			xamlUserType8.StaticInitializer = StaticInitializer_31_HomePage;
			xamlUserType8.AddMemberName("ViewModel");
			xamlUserType8.SetIsLocalType();
			result = xamlUserType8;
			break;
		}
		case 32:
		{
			XamlUserType xamlUserType7 = new XamlUserType(this, fullName, type, GetXamlTypeByName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject"));
			xamlUserType7.StaticInitializer = StaticInitializer_32_HomeViewModel;
			xamlUserType7.SetIsReturnTypeStub();
			xamlUserType7.SetIsLocalType();
			result = xamlUserType7;
			break;
		}
		case 33:
		{
			XamlUserType xamlUserType6 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType6.Activator = Activate_33_NowPlayingPage;
			xamlUserType6.StaticInitializer = StaticInitializer_33_NowPlayingPage;
			xamlUserType6.AddMemberName("ViewModel");
			xamlUserType6.SetIsLocalType();
			result = xamlUserType6;
			break;
		}
		case 34:
		{
			XamlUserType xamlUserType5 = new XamlUserType(this, fullName, type, GetXamlTypeByName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject"));
			xamlUserType5.StaticInitializer = StaticInitializer_34_NowPlayingViewModel;
			xamlUserType5.SetIsReturnTypeStub();
			xamlUserType5.SetIsLocalType();
			result = xamlUserType5;
			break;
		}
		case 35:
		{
			XamlUserType xamlUserType4 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.Controls.Page"));
			xamlUserType4.Activator = Activate_35_SettingsPage;
			xamlUserType4.StaticInitializer = StaticInitializer_35_SettingsPage;
			xamlUserType4.AddMemberName("ViewModel");
			xamlUserType4.SetIsLocalType();
			result = xamlUserType4;
			break;
		}
		case 36:
		{
			XamlUserType xamlUserType3 = new XamlUserType(this, fullName, type, GetXamlTypeByName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject"));
			xamlUserType3.StaticInitializer = StaticInitializer_36_SettingsViewModel;
			xamlUserType3.SetIsReturnTypeStub();
			xamlUserType3.SetIsLocalType();
			result = xamlUserType3;
			break;
		}
		case 37:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 38:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 39:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 40:
			result = new XamlSystemBaseType(fullName, type);
			break;
		case 41:
		{
			XamlUserType xamlUserType2 = new XamlUserType(this, fullName, type, GetXamlTypeByName("Microsoft.UI.Xaml.DependencyObject"));
			xamlUserType2.Activator = Activate_41_TreeViewNode;
			xamlUserType2.StaticInitializer = StaticInitializer_41_TreeViewNode;
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
		case 42:
		{
			XamlUserType xamlUserType = new XamlUserType(this, fullName, type, null);
			xamlUserType.StaticInitializer = StaticInitializer_42_IList;
			xamlUserType.CollectionAdd = VectorAdd_42_IList;
			xamlUserType.SetIsReturnTypeStub();
			result = xamlUserType;
			break;
		}
		case 43:
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

	private object get_0_EqualizerControl_Gains(object instance)
	{
		return ((EqualizerControl)instance).Gains;
	}

	private void set_0_EqualizerControl_Gains(object instance, object Value)
	{
		((EqualizerControl)instance).Gains = (double[])Value;
	}

	private object get_1_LiquidGlassBorder_Content(object instance)
	{
		return ((LiquidGlassBorder)instance).Content;
	}

	private void set_1_LiquidGlassBorder_Content(object instance, object Value)
	{
		((LiquidGlassBorder)instance).Content = Value;
	}

	private object get_2_SKXamlCanvas_CanvasSize(object instance)
	{
		return ((SKXamlCanvas)instance).CanvasSize;
	}

	private object get_3_SKXamlCanvas_IgnorePixelScaling(object instance)
	{
		return ((SKXamlCanvas)instance).IgnorePixelScaling;
	}

	private void set_3_SKXamlCanvas_IgnorePixelScaling(object instance, object Value)
	{
		((SKXamlCanvas)instance).IgnorePixelScaling = (bool)Value;
	}

	private object get_4_SKXamlCanvas_Dpi(object instance)
	{
		return ((SKXamlCanvas)instance).Dpi;
	}

	private object get_5_ProgressBar_IsIndeterminate(object instance)
	{
		return ((ProgressBar)instance).IsIndeterminate;
	}

	private void set_5_ProgressBar_IsIndeterminate(object instance, object Value)
	{
		((ProgressBar)instance).IsIndeterminate = (bool)Value;
	}

	private object get_6_ProgressBar_ShowError(object instance)
	{
		return ((ProgressBar)instance).ShowError;
	}

	private void set_6_ProgressBar_ShowError(object instance, object Value)
	{
		((ProgressBar)instance).ShowError = (bool)Value;
	}

	private object get_7_ProgressBar_ShowPaused(object instance)
	{
		return ((ProgressBar)instance).ShowPaused;
	}

	private void set_7_ProgressBar_ShowPaused(object instance, object Value)
	{
		((ProgressBar)instance).ShowPaused = (bool)Value;
	}

	private object get_8_ProgressBar_TemplateSettings(object instance)
	{
		return ((ProgressBar)instance).TemplateSettings;
	}

	private object get_9_DspPage_ViewModel(object instance)
	{
		return ((DspPage)instance).ViewModel;
	}

	private void set_9_DspPage_ViewModel(object instance, object Value)
	{
		((DspPage)instance).ViewModel = (DspViewModel)Value;
	}

	private object get_10_EditorPage_ViewModel(object instance)
	{
		return ((EditorPage)instance).ViewModel;
	}

	private void set_10_EditorPage_ViewModel(object instance, object Value)
	{
		((EditorPage)instance).ViewModel = (EditorViewModel)Value;
	}

	private object get_11_ProgressRing_IsActive(object instance)
	{
		return ((ProgressRing)instance).IsActive;
	}

	private void set_11_ProgressRing_IsActive(object instance, object Value)
	{
		((ProgressRing)instance).IsActive = (bool)Value;
	}

	private object get_12_ProgressRing_IsIndeterminate(object instance)
	{
		return ((ProgressRing)instance).IsIndeterminate;
	}

	private void set_12_ProgressRing_IsIndeterminate(object instance, object Value)
	{
		((ProgressRing)instance).IsIndeterminate = (bool)Value;
	}

	private object get_13_ProgressRing_Maximum(object instance)
	{
		return ((ProgressRing)instance).Maximum;
	}

	private void set_13_ProgressRing_Maximum(object instance, object Value)
	{
		((ProgressRing)instance).Maximum = (double)Value;
	}

	private object get_14_ProgressRing_Minimum(object instance)
	{
		return ((ProgressRing)instance).Minimum;
	}

	private void set_14_ProgressRing_Minimum(object instance, object Value)
	{
		((ProgressRing)instance).Minimum = (double)Value;
	}

	private object get_15_ProgressRing_TemplateSettings(object instance)
	{
		return ((ProgressRing)instance).TemplateSettings;
	}

	private object get_16_ProgressRing_Value(object instance)
	{
		return ((ProgressRing)instance).Value;
	}

	private void set_16_ProgressRing_Value(object instance, object Value)
	{
		((ProgressRing)instance).Value = (double)Value;
	}

	private object get_17_HomePage_ViewModel(object instance)
	{
		return ((HomePage)instance).ViewModel;
	}

	private void set_17_HomePage_ViewModel(object instance, object Value)
	{
		((HomePage)instance).ViewModel = (HomeViewModel)Value;
	}

	private object get_18_NowPlayingPage_ViewModel(object instance)
	{
		return ((NowPlayingPage)instance).ViewModel;
	}

	private void set_18_NowPlayingPage_ViewModel(object instance, object Value)
	{
		((NowPlayingPage)instance).ViewModel = (NowPlayingViewModel)Value;
	}

	private object get_19_SettingsPage_ViewModel(object instance)
	{
		return ((SettingsPage)instance).ViewModel;
	}

	private void set_19_SettingsPage_ViewModel(object instance, object Value)
	{
		((SettingsPage)instance).ViewModel = (SettingsViewModel)Value;
	}

	private object get_20_TreeViewNode_Children(object instance)
	{
		return ((TreeViewNode)instance).Children;
	}

	private object get_21_TreeViewNode_Content(object instance)
	{
		return ((TreeViewNode)instance).Content;
	}

	private void set_21_TreeViewNode_Content(object instance, object Value)
	{
		((TreeViewNode)instance).Content = Value;
	}

	private object get_22_TreeViewNode_Depth(object instance)
	{
		return ((TreeViewNode)instance).Depth;
	}

	private object get_23_TreeViewNode_HasChildren(object instance)
	{
		return ((TreeViewNode)instance).HasChildren;
	}

	private object get_24_TreeViewNode_HasUnrealizedChildren(object instance)
	{
		return ((TreeViewNode)instance).HasUnrealizedChildren;
	}

	private void set_24_TreeViewNode_HasUnrealizedChildren(object instance, object Value)
	{
		((TreeViewNode)instance).HasUnrealizedChildren = (bool)Value;
	}

	private object get_25_TreeViewNode_IsExpanded(object instance)
	{
		return ((TreeViewNode)instance).IsExpanded;
	}

	private void set_25_TreeViewNode_IsExpanded(object instance, object Value)
	{
		((TreeViewNode)instance).IsExpanded = (bool)Value;
	}

	private object get_26_TreeViewNode_Parent(object instance)
	{
		return ((TreeViewNode)instance).Parent;
	}

	private IXamlMember CreateXamlMember(string longMemberName)
	{
		XamlMember xamlMember = null;
		switch (longMemberName)
		{
		case "FurinaPlayer.UI.Controls.EqualizerControl.Gains":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Controls.EqualizerControl");
			xamlMember = new XamlMember(this, "Gains", "Double[]");
			xamlMember.Getter = get_0_EqualizerControl_Gains;
			xamlMember.Setter = set_0_EqualizerControl_Gains;
			break;
		case "FurinaPlayer.UI.Controls.LiquidGlassBorder.Content":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Controls.LiquidGlassBorder");
			xamlMember = new XamlMember(this, "Content", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_1_LiquidGlassBorder_Content;
			xamlMember.Setter = set_1_LiquidGlassBorder_Content;
			break;
		case "SkiaSharp.Views.Windows.SKXamlCanvas.CanvasSize":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "CanvasSize", "SkiaSharp.SKSize");
			xamlMember.Getter = get_2_SKXamlCanvas_CanvasSize;
			xamlMember.SetIsReadOnly();
			break;
		case "SkiaSharp.Views.Windows.SKXamlCanvas.IgnorePixelScaling":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "IgnorePixelScaling", "Boolean");
			xamlMember.Getter = get_3_SKXamlCanvas_IgnorePixelScaling;
			xamlMember.Setter = set_3_SKXamlCanvas_IgnorePixelScaling;
			break;
		case "SkiaSharp.Views.Windows.SKXamlCanvas.Dpi":
			_ = (XamlUserType)GetXamlTypeByName("SkiaSharp.Views.Windows.SKXamlCanvas");
			xamlMember = new XamlMember(this, "Dpi", "Double");
			xamlMember.Getter = get_4_SKXamlCanvas_Dpi;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressBar.IsIndeterminate":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressBar");
			xamlMember = new XamlMember(this, "IsIndeterminate", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_5_ProgressBar_IsIndeterminate;
			xamlMember.Setter = set_5_ProgressBar_IsIndeterminate;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressBar.ShowError":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressBar");
			xamlMember = new XamlMember(this, "ShowError", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_6_ProgressBar_ShowError;
			xamlMember.Setter = set_6_ProgressBar_ShowError;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressBar.ShowPaused":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressBar");
			xamlMember = new XamlMember(this, "ShowPaused", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_7_ProgressBar_ShowPaused;
			xamlMember.Setter = set_7_ProgressBar_ShowPaused;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressBar.TemplateSettings":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressBar");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.ProgressBarTemplateSettings");
			xamlMember.Getter = get_8_ProgressBar_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		case "FurinaPlayer.UI.Pages.DspPage.ViewModel":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Pages.DspPage");
			xamlMember = new XamlMember(this, "ViewModel", "FurinaPlayer.UI.ViewModels.DspViewModel");
			xamlMember.Getter = get_9_DspPage_ViewModel;
			xamlMember.Setter = set_9_DspPage_ViewModel;
			break;
		case "FurinaPlayer.UI.Pages.EditorPage.ViewModel":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Pages.EditorPage");
			xamlMember = new XamlMember(this, "ViewModel", "FurinaPlayer.UI.ViewModels.EditorViewModel");
			xamlMember.Getter = get_10_EditorPage_ViewModel;
			xamlMember.Setter = set_10_EditorPage_ViewModel;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.IsActive":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "IsActive", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_11_ProgressRing_IsActive;
			xamlMember.Setter = set_11_ProgressRing_IsActive;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.IsIndeterminate":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "IsIndeterminate", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_12_ProgressRing_IsIndeterminate;
			xamlMember.Setter = set_12_ProgressRing_IsIndeterminate;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Maximum":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Maximum", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_13_ProgressRing_Maximum;
			xamlMember.Setter = set_13_ProgressRing_Maximum;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Minimum":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Minimum", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_14_ProgressRing_Minimum;
			xamlMember.Setter = set_14_ProgressRing_Minimum;
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.TemplateSettings":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "TemplateSettings", "Microsoft.UI.Xaml.Controls.ProgressRingTemplateSettings");
			xamlMember.Getter = get_15_ProgressRing_TemplateSettings;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.ProgressRing.Value":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.ProgressRing");
			xamlMember = new XamlMember(this, "Value", "Double");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_16_ProgressRing_Value;
			xamlMember.Setter = set_16_ProgressRing_Value;
			break;
		case "FurinaPlayer.UI.Pages.HomePage.ViewModel":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Pages.HomePage");
			xamlMember = new XamlMember(this, "ViewModel", "FurinaPlayer.UI.ViewModels.HomeViewModel");
			xamlMember.Getter = get_17_HomePage_ViewModel;
			xamlMember.Setter = set_17_HomePage_ViewModel;
			break;
		case "FurinaPlayer.UI.Pages.NowPlayingPage.ViewModel":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Pages.NowPlayingPage");
			xamlMember = new XamlMember(this, "ViewModel", "FurinaPlayer.UI.ViewModels.NowPlayingViewModel");
			xamlMember.Getter = get_18_NowPlayingPage_ViewModel;
			xamlMember.Setter = set_18_NowPlayingPage_ViewModel;
			break;
		case "FurinaPlayer.UI.Pages.SettingsPage.ViewModel":
			_ = (XamlUserType)GetXamlTypeByName("FurinaPlayer.UI.Pages.SettingsPage");
			xamlMember = new XamlMember(this, "ViewModel", "FurinaPlayer.UI.ViewModels.SettingsViewModel");
			xamlMember.Getter = get_19_SettingsPage_ViewModel;
			xamlMember.Setter = set_19_SettingsPage_ViewModel;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Children":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Children", "System.Collections.Generic.IList`1<Microsoft.UI.Xaml.Controls.TreeViewNode>");
			xamlMember.Getter = get_20_TreeViewNode_Children;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Content":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Content", "Object");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_21_TreeViewNode_Content;
			xamlMember.Setter = set_21_TreeViewNode_Content;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Depth":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Depth", "Int32");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_22_TreeViewNode_Depth;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasChildren", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_23_TreeViewNode_HasChildren;
			xamlMember.SetIsReadOnly();
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.HasUnrealizedChildren":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "HasUnrealizedChildren", "Boolean");
			xamlMember.Getter = get_24_TreeViewNode_HasUnrealizedChildren;
			xamlMember.Setter = set_24_TreeViewNode_HasUnrealizedChildren;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.IsExpanded":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "IsExpanded", "Boolean");
			xamlMember.SetIsDependencyProperty();
			xamlMember.Getter = get_25_TreeViewNode_IsExpanded;
			xamlMember.Setter = set_25_TreeViewNode_IsExpanded;
			break;
		case "Microsoft.UI.Xaml.Controls.TreeViewNode.Parent":
			_ = (XamlUserType)GetXamlTypeByName("Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember = new XamlMember(this, "Parent", "Microsoft.UI.Xaml.Controls.TreeViewNode");
			xamlMember.Getter = get_26_TreeViewNode_Parent;
			xamlMember.SetIsReadOnly();
			break;
		}
		return xamlMember;
	}
}
