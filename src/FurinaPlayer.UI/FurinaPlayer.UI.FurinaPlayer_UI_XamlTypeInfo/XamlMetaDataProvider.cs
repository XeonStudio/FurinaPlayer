using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI.Xaml.Markup;
using WinRT;
using Windows.Foundation.Metadata;

namespace FurinaPlayer.UI.FurinaPlayer_UI_XamlTypeInfo;

[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
[DebuggerNonUserCode]
[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IXamlMetadataProvider")]
[WinRTExposedType(typeof(XamlMetaDataProviderWinRTTypeDetails))]
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
