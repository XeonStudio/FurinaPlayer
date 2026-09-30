using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using SonicWave.Audio.Dsp;
using WinRT;

namespace FurinaPlayer.UI.Controls;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(EqualizerControlWinRTTypeDetails))]
public sealed class EqualizerControl : UserControl, IComponentConnector
{
	private readonly Slider[] _sliders = new Slider[10];

	private bool _updating;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid Root;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	public double[] Gains
	{
		get
		{
			double[] array = new double[10];
			for (int i = 0; i < 10; i++)
			{
				array[i] = _sliders[i].Value;
			}
			return array;
		}
		set
		{
			_updating = true;
			for (int i = 0; i < 10 && i < value.Length; i++)
			{
				_sliders[i].Value = value[i];
			}
			_updating = false;
		}
	}

	public event Action<double[]>? GainsChanged;

	public EqualizerControl()
	{
		EqualizerControl equalizerControl = this;
		InitializeComponent();
		for (int i = 0; i < 10; i++)
		{
			int bandIndex = i;
			StackPanel stackPanel = new StackPanel
			{
				Spacing = 2.0
			};
			Slider slider = new Slider
			{
				Minimum = -12.0,
				Maximum = 12.0,
				Value = 0.0,
				Orientation = Orientation.Vertical,
				Height = 80.0,
				SmallChange = 0.5
			};
			slider.ValueChanged += (object _, RangeBaseValueChangedEventArgs _) =>
			{
				equalizerControl.OnSliderChanged(bandIndex);
			};
			TextBlock item = new TextBlock
			{
				Text = FormatFreq(Equalizer.BandFrequencies[i]),
				FontSize = 9.0,
				HorizontalAlignment = HorizontalAlignment.Center,
				Opacity = 0.7
			};
			stackPanel.Children.Add(slider);
			stackPanel.Children.Add(item);
			Grid.SetColumn(stackPanel, i);
			Root.Children.Add(stackPanel);
			_sliders[i] = slider;
		}
	}

	private void OnSliderChanged(int index)
	{
		if (!_updating)
		{
			GainsChanged?.Invoke(Gains);
		}
	}

	private static string FormatFreq(double hz)
	{
		if (hz >= 1000.0)
		{
			return $"{hz / 1000.0:0.#}k";
		}
		return $"{hz:0}";
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Controls/EqualizerControl.xaml");
			Application.LoadComponent(this, resourceLocator, ComponentResourceLocation.Nested);
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void Connect(int connectionId, object target)
	{
		if (connectionId == 2)
		{
			Root = target.As<Grid>();
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
