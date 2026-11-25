using System;
using System.Collections.Generic;
using System.Linq;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private Border? _soundEffectsWorkbench;
	private string _sfxSelectedCategory = "全部";
	private string _sfxActivePresetId = "off";
	private double _sfxIntensity = 1.0;
	private TextBlock? _sfxActiveBadgeTb;
	private TextBlock? _sfxIntensityValTb;
	private Grid? _sfxCardsGrid;
	private readonly Dictionary<string, Button> _sfxCategoryButtons = new();

	public FrameworkElement CreateSoundEffectsWorkbench()
	{
		var settingsSvc = ViewModel?.SettingsService ?? new SettingsService();
		var settings = settingsSvc.Load();
		var engine = ViewModel?.AudioEngine ?? ViewModel?.Engine;

		_sfxActivePresetId = engine?.DspChain?.SoundEffects?.PresetId
			?? (string.IsNullOrWhiteSpace(settings.FurinaSoundEffectPreset) ? "off" : settings.FurinaSoundEffectPreset);
		_sfxIntensity = engine?.DspChain?.SoundEffects?.Intensity ?? Math.Clamp(settings.FurinaSoundEffectIntensity, 0.0, 1.5);

		var rootBorder = new Border
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			CornerRadius = new CornerRadius(0)
		};

		var sv = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			Padding = new Thickness(28, 20, 28, 28)
		};

		var mainStack = new StackPanel
		{
			Spacing = 16,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MaxWidth = 1380
		};

		// 1. Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleStack = new StackPanel { Spacing = 4 };
		var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		titleRow.Children.Add(new TextBlock
		{
			Text = Loc.T("音效"),
			FontSize = 24,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 22, 26, 36)),
			VerticalAlignment = VerticalAlignment.Center
		});
		titleRow.Children.Add(new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(24, 37, 99, 235)),
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 3, 8, 3),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = Loc.T("FurinaPlayer 32 项实时音效引擎"),
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
			}
		});
		titleStack.Children.Add(titleRow);
		titleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("完整兼容适配 22 项经典音效与 10 项 Audition 工作室级扩展音效，支持全局实时强度调节。"),
			FontSize = 13,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 92, 98, 114)),
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(titleStack, 0);
		headerGrid.Children.Add(titleStack);

		var activeInfo = FurinaSoundEffectsProcessor.Catalog.FirstOrDefault(p => p.Id == _sfxActivePresetId)
			?? FurinaSoundEffectsProcessor.Catalog[0];
		_sfxActiveBadgeTb = new TextBlock
		{
			Text = $"{Loc.T("当前")}: {activeInfo.Name}",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235))
		};
		var activeBadge = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(20, 37, 99, 235)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(60, 37, 99, 235)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 6, 12, 6),
			VerticalAlignment = VerticalAlignment.Center,
			Child = _sfxActiveBadgeTb
		};
		Grid.SetColumn(activeBadge, 1);
		headerGrid.Children.Add(activeBadge);
		mainStack.Children.Add(headerGrid);

		// 2. Controls Bar (Category Filter + Intensity Slider + Reset)
		var barBorder = new Border
		{
			Background = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(30, 0, 0, 0)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(16, 12, 16, 12)
		};

		var barGrid = new Grid { ColumnSpacing = 16 };
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var catPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_sfxCategoryButtons.Clear();
		foreach (var cat in new[] { "全部", "人声与空间", "复古与模拟", "调制与创意", "母带与动态" })
		{
			var btn = new Button
			{
				Content = Loc.T(cat),
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(12, 6, 12, 6),
				FontSize = 12,
				FontWeight = FontWeights.SemiBold
			};
			string capturedCat = cat;
			btn.Click += (_, _) =>
			{
				_sfxSelectedCategory = capturedCat;
				UpdateSfxCategoryButtons();
				RebuildSfxCards();
			};
			_sfxCategoryButtons[cat] = btn;
			catPanel.Children.Add(btn);
		}
		Grid.SetColumn(catPanel, 0);
		barGrid.Children.Add(catPanel);

		var intensityPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		intensityPanel.Children.Add(new TextBlock
		{
			Text = Loc.T("效果强度"),
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 55, 65, 81)),
			VerticalAlignment = VerticalAlignment.Center
		});

		var intensitySlider = new Slider
		{
			Minimum = 0,
			Maximum = 150,
			Value = Math.Round(_sfxIntensity * 100.0),
			Width = 150,
			VerticalAlignment = VerticalAlignment.Center
		};
		_sfxIntensityValTb = new TextBlock
		{
			Text = $"{Math.Round(_sfxIntensity * 100.0):0}%",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Width = 42,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
			VerticalAlignment = VerticalAlignment.Center
		};
		intensitySlider.ValueChanged += (_, e) =>
		{
			_sfxIntensity = Math.Clamp(e.NewValue / 100.0, 0.0, 1.5);
			if (_sfxIntensityValTb != null)
			{
				_sfxIntensityValTb.Text = $"{Math.Round(e.NewValue):0}%";
			}
			ApplyAndSaveSoundEffect(_sfxActivePresetId, _sfxIntensity);
		};

		var bypassBtn = new Button
		{
			Content = Loc.T("一键直通"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 6, 12, 6),
			FontSize = 12
		};
		bypassBtn.Click += (_, _) =>
		{
			_sfxActivePresetId = "off";
			ApplyAndSaveSoundEffect("off", _sfxIntensity);
			RebuildSfxCards();
		};

		intensityPanel.Children.Add(intensitySlider);
		intensityPanel.Children.Add(_sfxIntensityValTb);
		intensityPanel.Children.Add(bypassBtn);
		Grid.SetColumn(intensityPanel, 1);
		barGrid.Children.Add(intensityPanel);

		barBorder.Child = barGrid;
		mainStack.Children.Add(barBorder);

		// 3. Cards Grid
		_sfxCardsGrid = new Grid
		{
			ColumnSpacing = 12,
			RowSpacing = 12,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_sfxCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_sfxCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_sfxCardsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		mainStack.Children.Add(_sfxCardsGrid);

		UpdateSfxCategoryButtons();
		RebuildSfxCards();

		sv.Content = mainStack;
		rootBorder.Child = sv;
		_soundEffectsWorkbench = rootBorder;
		return _soundEffectsWorkbench;
	}

	private void UpdateSfxCategoryButtons()
	{
		foreach (var kv in _sfxCategoryButtons)
		{
			bool selected = kv.Key == _sfxSelectedCategory;
			kv.Value.Background = new SolidColorBrush(selected
				? Color.FromArgb(255, 37, 99, 235)
				: Color.FromArgb(20, 0, 0, 0));
			kv.Value.Foreground = new SolidColorBrush(selected
				? Colors.White
				: Color.FromArgb(255, 45, 52, 64));
		}
	}

	private void RebuildSfxCards()
	{
		if (_sfxCardsGrid == null) return;
		_sfxCardsGrid.Children.Clear();
		_sfxCardsGrid.RowDefinitions.Clear();

		var items = FurinaSoundEffectsProcessor.Catalog
			.Where(p => _sfxSelectedCategory == "全部" || p.Id == "off" || p.Category == _sfxSelectedCategory)
			.ToList();

		int cols = 3;
		int rows = (items.Count + cols - 1) / cols;
		for (int r = 0; r < rows; r++)
		{
			_sfxCardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		for (int i = 0; i < items.Count; i++)
		{
			var preset = items[i];
			bool isSelected = string.Equals(preset.Id, _sfxActivePresetId, StringComparison.OrdinalIgnoreCase);

			var cardBtn = new Button
			{
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Top,
				Padding = new Thickness(14, 12, 14, 12),
				CornerRadius = new CornerRadius(12),
				Background = new SolidColorBrush(isSelected
					? Color.FromArgb(26, 37, 99, 235)
					: Color.FromArgb(215, 255, 255, 255)),
				BorderBrush = new SolidColorBrush(isSelected
					? Color.FromArgb(220, 37, 99, 235)
					: Color.FromArgb(30, 0, 0, 0)),
				BorderThickness = new Thickness(isSelected ? 1.8 : 1.0)
			};

			var content = new StackPanel { Spacing = 6 };
			var topRow = new Grid();
			topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var nameTb = new TextBlock
			{
				Text = Loc.T(preset.Name),
				FontSize = 14,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(isSelected
					? Color.FromArgb(255, 29, 78, 216)
					: Color.FromArgb(255, 22, 26, 36)),
				TextTrimming = TextTrimming.CharacterEllipsis
			};
			Grid.SetColumn(nameTb, 0);
			topRow.Children.Add(nameTb);

			string tagText = preset.OriginTag == "ECHO" ? "Furina · Classic" : preset.OriginTag;
			var tagColor = preset.OriginTag == "Audition"
				? Color.FromArgb(255, 124, 58, 237)
				: (preset.OriginTag == "系统" ? Color.FromArgb(255, 100, 116, 139) : Color.FromArgb(255, 37, 99, 235));
			var tagBorder = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(22, tagColor.R, tagColor.G, tagColor.B)),
				CornerRadius = new CornerRadius(5),
				Padding = new Thickness(6, 2, 6, 2),
				Child = new TextBlock
				{
					Text = tagText,
					FontSize = 10,
					FontWeight = FontWeights.SemiBold,
					Foreground = new SolidColorBrush(tagColor)
				}
			};
			Grid.SetColumn(tagBorder, 1);
			topRow.Children.Add(tagBorder);

			content.Children.Add(topRow);
			content.Children.Add(new TextBlock
			{
				Text = Loc.T(preset.Description),
				FontSize = 12,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 92, 98, 114)),
				TextWrapping = TextWrapping.Wrap,
				MaxLines = 2
			});

			cardBtn.Content = content;
			string capturedId = preset.Id;
			string capturedName = preset.Name;
			cardBtn.Click += (_, _) =>
			{
				_sfxActivePresetId = capturedId;
				ApplyAndSaveSoundEffect(capturedId, _sfxIntensity);
				if (_sfxActiveBadgeTb != null)
				{
					_sfxActiveBadgeTb.Text = $"{Loc.T("当前")}: {capturedName}";
				}
				RebuildSfxCards();
			};

			Grid.SetRow(cardBtn, i / cols);
			Grid.SetColumn(cardBtn, i % cols);
			_sfxCardsGrid.Children.Add(cardBtn);
		}
	}

	private void ApplyAndSaveSoundEffect(string presetId, double intensity)
	{
		var engine = ViewModel?.AudioEngine ?? ViewModel?.Engine;
		engine?.SetSoundEffect(presetId, intensity);

		var settingsSvc = ViewModel?.SettingsService ?? new SettingsService();
		var settings = settingsSvc.Load();
		settings.FurinaSoundEffectPreset = presetId;
		settings.FurinaSoundEffectIntensity = intensity;
		settingsSvc.Save(settings);
	}
}
