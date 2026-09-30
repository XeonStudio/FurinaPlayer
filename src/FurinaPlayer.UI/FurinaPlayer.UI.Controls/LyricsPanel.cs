using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinRT;
using Windows.Foundation;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(LyricsPanelWinRTTypeDetails))]
public sealed class LyricsPanel : UserControl, IComponentConnector
{
	private NowPlayingViewModel? _vm;

	private int _lastHighlightIndex = -1;

	private const uint FR_PRIVATE = 16u;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private TextBlock SourceText;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView LyricList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public LyricsPanel()
	{
		InitializeComponent();
	}

	public void Attach(NowPlayingViewModel vm)
	{
		_vm = vm;
		vm.PropertyChanged += OnVmPropertyChanged;
		LyricList.ItemsSource = vm.LyricLines;
		SourceText.Text = vm.LyricSource;
	}

	public void Detach()
	{
		if (_vm != null)
		{
			_vm.PropertyChanged -= OnVmPropertyChanged;
			_vm = null;
		}
	}

	[DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern int AddFontResourceEx(string lpszFilename, uint fl, nint pdv);

	public void ApplyLyricFont(string? familyName, string? fontFilePath)
	{
		try
		{
			FontFamily fontFamily = null;
			if (!string.IsNullOrWhiteSpace(fontFilePath) && File.Exists(fontFilePath))
			{
				AddFontResourceEx(fontFilePath, 16u, IntPtr.Zero);
				string text = FontFamilyNameFromFile(fontFilePath) ?? Path.GetFileNameWithoutExtension(fontFilePath);
				if (!string.IsNullOrWhiteSpace(text))
				{
					fontFamily = new FontFamily(text);
				}
			}
			else if (!string.IsNullOrWhiteSpace(familyName))
			{
				fontFamily = new FontFamily(familyName);
			}
			LyricList.FontFamily = fontFamily ?? ((FontFamily)Application.Current.Resources["ContentControlThemeFontFamily"]);
		}
		catch
		{
		}
	}

	private static string? FontFamilyNameFromFile(string path)
	{
		try
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using BinaryReader binaryReader = new BinaryReader(fileStream);
			uint num = binaryReader.ReadUInt32();
			if (num != 65536 && num != 1953658213 && num != 1330926671)
			{
				return null;
			}
			ushort num2 = binaryReader.ReadUInt16();
			binaryReader.ReadUInt16();
			binaryReader.ReadUInt16();
			binaryReader.ReadUInt16();
			long num3 = -1L;
			for (int i = 0; i < num2; i++)
			{
				uint num4 = binaryReader.ReadUInt32();
				binaryReader.ReadUInt32();
				long num5 = binaryReader.ReadUInt32();
				binaryReader.ReadUInt32();
				if (num4 == 1851878757)
				{
					num3 = num5;
					break;
				}
			}
			if (num3 < 0)
			{
				return null;
			}
			fileStream.Position = num3;
			binaryReader.ReadUInt16();
			ushort num6 = binaryReader.ReadUInt16();
			ushort num7 = binaryReader.ReadUInt16();
			for (int j = 0; j < num6; j++)
			{
				ushort num8 = binaryReader.ReadUInt16();
				ushort num9 = binaryReader.ReadUInt16();
				binaryReader.ReadUInt16();
				ushort num10 = binaryReader.ReadUInt16();
				ushort count = binaryReader.ReadUInt16();
				ushort num11 = binaryReader.ReadUInt16();
				if (num10 == 1 && num8 == 3 && (num9 == 1 || num9 == 10))
				{
					long position = num3 + num7 + num11;
					fileStream.Position = position;
					byte[] bytes = binaryReader.ReadBytes(count);
					_ = 1;
					return Encoding.BigEndianUnicode.GetString(bytes).TrimEnd('\0');
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "CurrentLyricIndex")
		{
			UpdateHighlight();
		}
		else if (e.PropertyName == "LyricLines")
		{
			LyricList.ItemsSource = _vm.LyricLines;
			_lastHighlightIndex = -1;
		}
		else if (e.PropertyName == "LyricSource")
		{
			SourceText.Text = _vm.LyricSource;
		}
	}

	private void UpdateHighlight()
	{
		if (_vm == null || _vm.LyricLines.Count == 0)
		{
			return;
		}
		int num = Math.Max(0, _vm.CurrentLyricIndex);
		if (num != _lastHighlightIndex)
		{
			ApplyStyle(_lastHighlightIndex, active: false);
			ApplyStyle(num, active: true);
			_lastHighlightIndex = num;
			if (num >= 0)
			{
				LyricList.ScrollIntoView(_vm.LyricLines[num]);
				CenterActiveLyric(num);
			}
		}
	}

	private void CenterActiveLyric(int index)
	{
		try
		{
			if (!(LyricList.ContainerFromIndex(index) is ListViewItem listViewItem))
			{
				return;
			}
			ScrollViewer? scrollViewer = FindScrollViewer(LyricList);
			if (scrollViewer == null || scrollViewer.ViewportHeight <= 0.0)
			{
				return;
			}
			Point rel = listViewItem.TransformToVisual(scrollViewer).TransformPoint(new Point(0f, 0f));
			double num = scrollViewer.VerticalOffset + rel.Y - (scrollViewer.ViewportHeight - listViewItem.ActualHeight) / 2.0;
			if (num < 0.0)
			{
				num = 0.0;
			}
			if (num > scrollViewer.ScrollableHeight)
			{
				num = scrollViewer.ScrollableHeight;
			}
			scrollViewer.ChangeView(null, num, null, disableAnimation: false);
		}
		catch
		{
		}
	}

	private static ScrollViewer? FindScrollViewer(DependencyObject root)
	{
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is ScrollViewer result)
			{
				return result;
			}
			ScrollViewer scrollViewer = FindScrollViewer(child);
			if (scrollViewer != null)
			{
				return scrollViewer;
			}
		}
		return null;
	}

	private void ApplyStyle(int i, bool active)
	{
		if (_vm != null && i >= 0 && i < _vm.LyricLines.Count && LyricList.ContainerFromIndex(i) is ListViewItem { ContentTemplateRoot: TextBlock contentTemplateRoot })
		{
			contentTemplateRoot.FontSize = (active ? 22 : 16);
			contentTemplateRoot.FontWeight = (active ? FontWeights.Bold : FontWeights.Normal);
			contentTemplateRoot.Foreground = (active ? ((Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]) : ((Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]));
			contentTemplateRoot.Opacity = (active ? 1.0 : 0.55);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/LyricsPanel.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 2:
			SourceText = target.As<TextBlock>();
			break;
		case 3:
			LyricList = target.As<ListView>();
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		return null;
	}
}
