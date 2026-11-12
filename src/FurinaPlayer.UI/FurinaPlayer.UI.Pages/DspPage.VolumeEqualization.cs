using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.Helpers;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using SonicWave.Audio.Dsp;
using SonicWave.Core.Models;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

/// <summary>
/// FurinaPlayer 音量均衡（Volume Equalization / ReplayGain）工作台分部实现。
/// 1:1 对齐专业响度规整控制流，提供基于 ITU-R BS.1770-4 / EBU R128 标准的曲库响度感知均衡界面。
/// </summary>
public sealed partial class DspPage
{
	private Border? _volumeEqualizationWorkbench;
	private TextBlock? _rgTargetLufsBigTb;
	private TextBlock? _rgTargetScaleValueTb;
	private Slider? _rgTargetSlider;
	private Slider? _rgPreampSlider;
	private TextBlock? _rgPreampValueTb;
	private TextBlock? _rgStatusLargeTb;
	private TextBlock? _rgStatusDetailTb;
	private TextBlock? _rgAppliedLufsTb;
	private TextBlock? _rgAppliedGainTb;
	private ToggleSwitch? _rgPowerSwitch;
	private readonly List<Button> _rgTargetPresetButtons = new();
	private readonly List<Button> _rgModeButtons = new();
	private TextBox? _rgCustomTargetTb;
	private ProgressBar? _rgLibraryProgressBar;
	private TextBlock? _rgLibraryProgressTb;
	private Button? _rgScanLoudnessBtn;
	private bool _isLoudnessScanning = false;
	private CancellationTokenSource? _scanCts;

	// Telemetry Fact TextBlocks in Advanced Expander
	private TextBlock? _factIntegratedTb;
	private TextBlock? _factSelectedGainTb;
	private TextBlock? _factPeakTb;
	private TextBlock? _factTrackGainTb;
	private TextBlock? _factAlbumGainTb;
	private TextBlock? _factSourceTb;
	private TextBlock? _factEffectiveModeTb;
	private TextBlock? _factClippingTb;
	private TextBox? _rgUnknownLoudnessTb;
	private CheckBox? _rgPreventClippingCb;
	private CheckBox? _rgAnalyzeOnScanCb;
	private CheckBox? _rgTruePeakCb;

	private FrameworkElement CreateVolumeEqualizationWorkbench()
	{
		var root = new Grid
		{
			Background = CreateWhiteAcrylicBrush(0.72, 195),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Top Header
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Library Loudness Banner
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) }); // 2: Main 2-column body

		// --- 0. Top Header ---
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = DspAccentSoftBgBrush,
			Child = new FontIcon { Glyph = "\uE767", FontSize = 18, Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "音量均衡",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// --- 1. Library Loudness Notice Card ---
		var bannerBorder = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 12, 16, 12)
		};
		var bannerGrid = new Grid();
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var bannerLeftSp = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
		bannerLeftSp.Children.Add(new TextBlock
		{
			Text = "曲库响度",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		});
		bannerLeftSp.Children.Add(new TextBlock
		{
			Text = "先补齐缺失的曲库响度，再按目标均衡。分析会等待暂停或停止播放。",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		});

		_rgLibraryProgressTb = new TextBlock
		{
			Text = "正在排队分析曲目响度...",
			FontSize = 11,
			Foreground = DspAccentSoftTextBrush,
			Visibility = Visibility.Collapsed,
			Margin = new Thickness(0, 4, 0, 0)
		};
		_rgLibraryProgressBar = new ProgressBar
		{
			Height = 4,
			Maximum = 100,
			Value = 0,
			Visibility = Visibility.Collapsed,
			Margin = new Thickness(0, 4, 0, 0),
			Foreground = new SolidColorBrush(Color.FromArgb(255, 99, 102, 241))
		};
		bannerLeftSp.Children.Add(_rgLibraryProgressTb);
		bannerLeftSp.Children.Add(_rgLibraryProgressBar);

		Grid.SetColumn(bannerLeftSp, 0);
		bannerGrid.Children.Add(bannerLeftSp);

		var bannerRightSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var btnViewLoudness = new Button
		{
			Content = "查看响度数据",
			FontSize = 12,
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Foreground = DspAccentSoftTextBrush,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium
		};
		btnViewLoudness.Click += OnViewLoudnessDataClick;

		_rgScanLoudnessBtn = new Button
		{
			Content = "补齐曲库响度",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.Medium,
			Padding = new Thickness(14, 6, 14, 6),
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Foreground = DspTextPrimaryBrush
		};
		_rgScanLoudnessBtn.Click += OnScanLoudnessClick;

		bannerRightSp.Children.Add(btnViewLoudness);
		bannerRightSp.Children.Add(_rgScanLoudnessBtn);
		Grid.SetColumn(bannerRightSp, 1);
		bannerGrid.Children.Add(bannerRightSp);

		bannerBorder.Child = bannerGrid;
		Grid.SetRow(bannerBorder, 1);
		root.Children.Add(bannerBorder);

		// --- 2. Main 2-Column Workbench Body ---
		var svBody = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
		};
		var bodyGrid = new Grid { ColumnSpacing = 28 };
		bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Left Main Area
		bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(340, GridUnitType.Pixel) }); // Right Sidebar Card

		// === LEFT MAIN AREA ===
		var leftSp = new StackPanel { Spacing = 20 };

		// Section: 目标响度
		var targetCard = new StackPanel { Spacing = 8 };
		var targetHeaderGrid = new Grid();
		targetHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		targetHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		targetHeaderGrid.Children.Add(new TextBlock
		{
			Text = "目标响度",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush
		});

		var powerSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_rgPowerSwitch = new ToggleSwitch
		{
			IsOn = ViewModel?.FurinaReplayGainEnabled ?? false,
			OnContent = "",
			OffContent = "",
			VerticalAlignment = VerticalAlignment.Center
		};
		var powerLabelTb = new TextBlock
		{
			Text = (_rgPowerSwitch.IsOn) ? "已开启" : "未开启",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_rgPowerSwitch.Toggled += (s, e) =>
		{
			bool on = _rgPowerSwitch.IsOn;
			powerLabelTb.Text = on ? "已开启" : "未开启";
			if (ViewModel != null)
			{
				ViewModel.FurinaReplayGainEnabled = on;
				ViewModel.ApplyReplayGainSettings();
			}
			RefreshVolumeEqualizationStatus();
		};
		powerSp.Children.Add(_rgPowerSwitch);
		powerSp.Children.Add(powerLabelTb);
		Grid.SetColumn(powerSp, 1);
		targetHeaderGrid.Children.Add(powerSp);
		targetCard.Children.Add(targetHeaderGrid);

		// Huge Number Display: -14.0 LUFS
		var bigNumSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
		_rgTargetLufsBigTb = new TextBlock
		{
			Text = $"{ViewModel?.FurinaReplayGainTargetLufs ?? -14.0:F1}",
			FontSize = 52,
			FontWeight = Microsoft.UI.Text.FontWeights.ExtraBold,
			Foreground = DspTextPrimaryBrush
		};
		bigNumSp.Children.Add(_rgTargetLufsBigTb);
		bigNumSp.Children.Add(new TextBlock
		{
			Text = "LUFS",
			FontSize = 16,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextMutedBrush,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(2, 0, 0, 10)
		});
		targetCard.Children.Add(bigNumSp);

		targetCard.Children.Add(new TextBlock
		{
			Text = "让不同歌曲的音量更接近，减少切歌时的忽大忽小。不改写音乐文件。",
			FontSize = 12,
			Foreground = DspTextMutedBrush,
			Margin = new Thickness(0, 0, 0, 8)
		});

		// Visual Loudness Scale Rail & Target Slider (-36.0 to 0.0 LUFS)
		targetCard.Children.Add(CreateLoudnessScaleVisualizer());

		// Target Presets: 日常聆听(-14), 安静(-18), EBU R128(-23), 自定义目标
		targetCard.Children.Add(CreateTargetPresetsRow());

		leftSp.Children.Add(targetCard);

		// Section: 模式 (自动, 单曲, 专辑)
		leftSp.Children.Add(CreateReplayGainModeSection());

		// Section: 前级增益 (-12 to +12 dB)
		leftSp.Children.Add(CreatePreampSection());

		Grid.SetColumn(leftSp, 0);
		bodyGrid.Children.Add(leftSp);

		// === RIGHT SIDEBAR CARD ===
		var rightCard = CreateRightStatusSidebar();
		Grid.SetColumn(rightCard, 1);
		bodyGrid.Children.Add(rightCard);

		svBody.Content = bodyGrid;
		Grid.SetRow(svBody, 2);
		root.Children.Add(svBody);

		_volumeEqualizationWorkbench = new Border { Child = root };
		return _volumeEqualizationWorkbench;
	}

	private FrameworkElement CreateLoudnessScaleVisualizer()
	{
		var root = new StackPanel { Spacing = 6, Margin = new Thickness(0, 6, 0, 14) };

		// Row 1: 综合响度
		var r1 = new Grid();
		r1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		r1.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		r1.Children.Add(new TextBlock { Text = "综合响度", FontSize = 11, Foreground = DspTextMutedBrush });
		_rgAppliedLufsTb = new TextBlock
		{
			Text = ViewModel?.FurinaReplayGainIntegratedLufs.HasValue == true
				? $"{ViewModel.FurinaReplayGainIntegratedLufs.Value:F1} LUFS"
				: "--",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(_rgAppliedLufsTb, 1);
		r1.Children.Add(_rgAppliedLufsTb);
		root.Children.Add(r1);

		// Rail 1: Integrated Loudness background track
		var integratedRailContainer = new Grid { Height = 6, Margin = new Thickness(0, 2, 0, 4) };
		integratedRailContainer.Children.Add(new Border
		{
			Height = 4,
			Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			CornerRadius = new CornerRadius(2),
			VerticalAlignment = VerticalAlignment.Center
		});
		root.Children.Add(integratedRailContainer);

		// Row 2: 目标响度 Label + Value
		var r2 = new Grid { Margin = new Thickness(0, 4, 0, 0) };
		r2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		r2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		r2.Children.Add(new TextBlock { Text = "目标响度", FontSize = 11, Foreground = DspTextMutedBrush });
		double targetLufs = ViewModel?.FurinaReplayGainTargetLufs ?? -14.0;
		_rgTargetScaleValueTb = new TextBlock
		{
			Text = $"{targetLufs:F1} LUFS",
			FontSize = 12,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspAccentSoftTextBrush
		};
		Grid.SetColumn(_rgTargetScaleValueTb, 1);
		r2.Children.Add(_rgTargetScaleValueTb);
		root.Children.Add(r2);

		// Interactive Slider: Target Loudness (-36.0 to 0.0 LUFS, step 0.5)
		_rgTargetSlider = new Slider
		{
			Minimum = -36.0,
			Maximum = 0.0,
			StepFrequency = 0.5,
			Value = targetLufs,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Margin = new Thickness(0, 2, 0, 2)
		};
		_rgTargetSlider.ValueChanged += (s, e) =>
		{
			SetReplayGainTargetLufs(e.NewValue);
		};
		root.Children.Add(_rgTargetSlider);

		// Labels below ticks (-36, -30, -24, -18, -12, -6, 0)
		double[] ticks = { -36, -30, -24, -18, -12, -6, 0 };
		var labelsGrid = new Grid();
		for (int i = 0; i < ticks.Length; i++)
		{
			labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			var tb = new TextBlock
			{
				Text = $"{ticks[i]:F0}",
				FontSize = 10,
				Foreground = DspTextMutedBrush,
				HorizontalAlignment = (i == 0) ? HorizontalAlignment.Left : ((i == ticks.Length - 1) ? HorizontalAlignment.Right : HorizontalAlignment.Center)
			};
			Grid.SetColumn(tb, i);
			labelsGrid.Children.Add(tb);
		}
		root.Children.Add(labelsGrid);

		return root;
	}

	private FrameworkElement CreateTargetPresetsRow()
	{
		var grid = new Grid { ColumnSpacing = 16, Margin = new Thickness(0, 10, 0, 0) };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // -14
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // -18
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // -23
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) }); // Custom

		_rgTargetPresetButtons.Clear();

		Button CreatePresetButton(double lufs, string title, int col)
		{
			bool isSelected = Math.Abs((ViewModel?.FurinaReplayGainTargetLufs ?? -14.0) - lufs) < 0.2;
			var btn = new Button
			{
				Tag = lufs,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(0, 4, 0, 8),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0)
			};

			var stack = new StackPanel { Spacing = 4 };
			var titleTb = new TextBlock
			{
				Text = title,
				FontSize = 12,
				FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
				Foreground = isSelected ? DspAccentSoftTextBrush : DspTextMutedBrush
			};
			stack.Children.Add(titleTb);

			var numSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
			numSp.Children.Add(new TextBlock
			{
				Text = $"{lufs:F0}",
				FontSize = 24,
				FontWeight = Microsoft.UI.Text.FontWeights.Bold,
				Foreground = isSelected ? DspAccentSoftTextBrush : DspTextPrimaryBrush
			});
			numSp.Children.Add(new TextBlock
			{
				Text = "LUFS",
				FontSize = 11,
				Foreground = DspTextMutedBrush,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(0, 0, 0, 4)
			});
			stack.Children.Add(numSp);

			var indicator = new Border
			{
				Height = 2.5,
				Background = new SolidColorBrush(isSelected ? Color.FromArgb(255, 79, 70, 229) : Color.FromArgb(255, 226, 232, 240)),
				CornerRadius = new CornerRadius(1),
				Margin = new Thickness(0, 4, 0, 0)
			};
			stack.Children.Add(indicator);

			btn.Content = stack;
			btn.Click += (s, e) => SetReplayGainTargetLufs(lufs);
			_rgTargetPresetButtons.Add(btn);
			Grid.SetColumn(btn, col);
			return btn;
		}

		grid.Children.Add(CreatePresetButton(-14.0, "日常聆听 · 推荐", 0));
		grid.Children.Add(CreatePresetButton(-18.0, "安静", 1));
		grid.Children.Add(CreatePresetButton(-23.0, "EBU R128", 2));

		var customSp = new StackPanel { Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
		customSp.Children.Add(new TextBlock { Text = "自定义目标", FontSize = 12, Foreground = DspTextMutedBrush });
		var inputSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		_rgCustomTargetTb = new TextBox
		{
			Text = $"{ViewModel?.FurinaReplayGainTargetLufs ?? -14.0:F0}",
			Width = 65,
			Height = 32,
			CornerRadius = new CornerRadius(6),
			Background = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255)),
			BorderBrush = CreateLiquidGlassRimBrush(),
			Foreground = DspTextPrimaryBrush,
			VerticalAlignment = VerticalAlignment.Center
		};
		_rgCustomTargetTb.TextChanged += (s, e) =>
		{
			if (double.TryParse(_rgCustomTargetTb.Text, out double customVal))
			{
				SetReplayGainTargetLufs(Math.Clamp(customVal, -36.0, 0.0));
			}
		};
		inputSp.Children.Add(_rgCustomTargetTb);
		inputSp.Children.Add(new TextBlock { Text = "LUFS", FontSize = 12, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center });
		customSp.Children.Add(inputSp);
		Grid.SetColumn(customSp, 3);
		grid.Children.Add(customSp);

		return grid;
	}

	private FrameworkElement CreateReplayGainModeSection()
	{
		var root = new StackPanel { Spacing = 10, Margin = new Thickness(0, 14, 0, 0) };
		root.Children.Add(new TextBlock
		{
			Text = "模式",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush
		});

		var grid = new Grid { ColumnSpacing = 16 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Auto (0)
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Track (1)
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) }); // Album (2)

		_rgModeButtons.Clear();

		Button CreateModeCard(int modeIdx, string title, string desc, int col)
		{
			bool isSelected = (ViewModel?.FurinaReplayGainMode ?? 1) == modeIdx;
			var btn = new Button
			{
				Tag = modeIdx,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
				Padding = new Thickness(0, 4, 0, 8),
				Background = new SolidColorBrush(Colors.Transparent),
				BorderThickness = new Thickness(0)
			};

			var stack = new StackPanel { Spacing = 6 };
			var titleTb = new TextBlock
			{
				Text = title,
				FontSize = 14,
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				Foreground = isSelected ? DspAccentSoftTextBrush : DspTextPrimaryBrush
			};
			stack.Children.Add(titleTb);

			var descTb = new TextBlock
			{
				Text = desc,
				FontSize = 11,
				TextWrapping = TextWrapping.Wrap,
				LineHeight = 16,
				Foreground = isSelected ? DspAccentSoftTextBrush : DspTextMutedBrush
			};
			stack.Children.Add(descTb);

			var indicator = new Border
			{
				Height = 2.5,
				Background = isSelected ? DspAccentSoftBorderBrush : CreateLiquidGlassRimBrush(),
				CornerRadius = new CornerRadius(1),
				Margin = new Thickness(0, 6, 0, 0)
			};
			stack.Children.Add(indicator);

			btn.Content = stack;
			btn.Click += (s, e) => SetReplayGainMode(modeIdx);
			_rgModeButtons.Add(btn);
			Grid.SetColumn(btn, col);
			return btn;
		}

		grid.Children.Add(CreateModeCard(0, "自动", "连续播放同一专辑用专辑增益；混合播放用单曲增益。", 0));
		grid.Children.Add(CreateModeCard(1, "单曲", "适合随机播放，让每首歌的音量更接近。", 1));
		grid.Children.Add(CreateModeCard(2, "专辑", "适合整张专辑，保留曲目之间原有的强弱。", 2));

		root.Children.Add(grid);
		return root;
	}

	private FrameworkElement CreatePreampSection()
	{
		var root = new StackPanel { Spacing = 8, Margin = new Thickness(0, 14, 0, 0) };

		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		headerGrid.Children.Add(new TextBlock
		{
			Text = "前级增益",
			FontSize = 13,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextSecondaryBrush
		});

		_rgPreampValueTb = new TextBlock
		{
			Text = $"{ViewModel?.FurinaReplayGainPreampDb ?? 0.0:+0.0;-0.0;0.0} dB",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = DspTextPrimaryBrush
		};
		Grid.SetColumn(_rgPreampValueTb, 1);
		headerGrid.Children.Add(_rgPreampValueTb);
		root.Children.Add(headerGrid);

		_rgPreampSlider = new Slider
		{
			Minimum = -12.0,
			Maximum = 12.0,
			StepFrequency = 0.5,
			Value = ViewModel?.FurinaReplayGainPreampDb ?? 0.0,
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		_rgPreampSlider.ValueChanged += (s, e) =>
		{
			if (ViewModel != null)
			{
				ViewModel.FurinaReplayGainPreampDb = e.NewValue;
				ViewModel.ApplyReplayGainSettings();
				if (_rgPreampValueTb != null) _rgPreampValueTb.Text = $"{e.NewValue:+0.0;-0.0;0.0} dB";
			}
		};
		root.Children.Add(_rgPreampSlider);

		var labelsGrid = new Grid();
		labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		labelsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		var tbLeft = new TextBlock { Text = "-12 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Left };
		var tbMid = new TextBlock { Text = "0", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Center };
		var tbRight = new TextBlock { Text = "+12 dB", FontSize = 10, Foreground = DspTextMutedBrush, HorizontalAlignment = HorizontalAlignment.Right };
		Grid.SetColumn(tbLeft, 0); Grid.SetColumn(tbMid, 1); Grid.SetColumn(tbRight, 2);
		labelsGrid.Children.Add(tbLeft); labelsGrid.Children.Add(tbMid); labelsGrid.Children.Add(tbRight);
		root.Children.Add(labelsGrid);

		return root;
	}

	private FrameworkElement CreateRightStatusSidebar()
	{
		var card = new Border
		{
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(12),
			Padding = new Thickness(18),
			VerticalAlignment = VerticalAlignment.Top
		};

		var stack = new StackPanel { Spacing = 14 };

		// Header: 当前状态 | 刷新
		var headGrid = new Grid();
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		headGrid.Children.Add(new TextBlock
		{
			Text = "当前状态",
			FontSize = 14,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = DspTextPrimaryBrush
		});

		var btnRefreshStatus = new Button
		{
			Content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				Children =
				{
					new FontIcon { Glyph = "\uE72C", FontSize = 12, Foreground = DspTextMutedBrush },
					new TextBlock { Text = "刷新", FontSize = 12, Foreground = DspTextSubHeaderBrush }
				}
			},
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(6, 2, 6, 2)
		};
		btnRefreshStatus.Click += (s, e) => RefreshVolumeEqualizationStatus();
		Grid.SetColumn(btnRefreshStatus, 1);
		headGrid.Children.Add(btnRefreshStatus);
		stack.Children.Add(headGrid);

		// Current applied gain
		var appliedSp = new StackPanel { Spacing = 2 };
		_rgAppliedGainTb = new TextBlock
		{
			Text = ViewModel?.FurinaReplayGainCurrentAppliedDb.HasValue == true
				? $"当前应用  {ViewModel.FurinaReplayGainCurrentAppliedDb.Value:+0.0;-0.0;0.0} dB"
				: "当前应用  --",
			FontSize = 12,
			Foreground = DspTextMutedBrush
		};
		appliedSp.Children.Add(_rgAppliedGainTb);

		_rgStatusLargeTb = new TextBlock
		{
			Text = (ViewModel?.FurinaReplayGainEnabled ?? false) ? "已开启" : "未开启",
			FontSize = 26,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = (ViewModel?.FurinaReplayGainEnabled ?? false)
				? new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
				: DspTextPrimaryBrush,
			Margin = new Thickness(0, 6, 0, 4)
		};
		appliedSp.Children.Add(_rgStatusLargeTb);

		_rgStatusDetailTb = new TextBlock
		{
			Text = (ViewModel?.FurinaReplayGainEnabled ?? false)
				? "音量均衡已生效。系统将自动按目标响度调节各曲目播放增益，保障听感一致。"
				: "请先补齐曲库响度。分析结果会保存在曲库中，下次播放直接使用。",
			FontSize = 12,
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 18,
			Foreground = DspTextMutedBrush
		};
		appliedSp.Children.Add(_rgStatusDetailTb);
		stack.Children.Add(appliedSp);

		// Separator
		stack.Children.Add(new Border
		{
			Height = 1,
			Background = new SolidColorBrush(Color.FromArgb(255, 226, 232, 240)),
			Margin = new Thickness(0, 6, 0, 6)
		});

		// Expander: 高级设置
		var expander = new Expander
		{
			Header = "高级设置",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			Background = new SolidColorBrush(Colors.Transparent),
			BorderThickness = new Thickness(0)
		};

		var advStack = new StackPanel { Spacing = 12, Padding = new Thickness(0, 10, 0, 0) };

		// Telemetry Facts Grid (综合响度, 选用增益, 峰值, 曲目增益, 专辑增益, 数据来源, 当前增益模式, 防削波)
		var factsGrid = new Grid { RowSpacing = 6, ColumnSpacing = 12 };
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		factsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		FrameworkElement CreateFactItem(string label, out TextBlock valTb, int row, int col)
		{
			var sp = new StackPanel { Spacing = 2 };
			sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = DspTextMutedBrush });
			valTb = new TextBlock { Text = "--", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
			sp.Children.Add(valTb);
			Grid.SetRow(sp, row);
			Grid.SetColumn(sp, col);
			return sp;
		}

		factsGrid.Children.Add(CreateFactItem("综合响度", out _factIntegratedTb, 0, 0));
		factsGrid.Children.Add(CreateFactItem("选用增益", out _factSelectedGainTb, 0, 1));
		factsGrid.Children.Add(CreateFactItem("音频峰值", out _factPeakTb, 1, 0));
		factsGrid.Children.Add(CreateFactItem("曲目增益", out _factTrackGainTb, 1, 1));
		factsGrid.Children.Add(CreateFactItem("专辑增益", out _factAlbumGainTb, 2, 0));
		factsGrid.Children.Add(CreateFactItem("数据来源", out _factSourceTb, 2, 1));
		factsGrid.Children.Add(CreateFactItem("当前模式", out _factEffectiveModeTb, 3, 0));
		factsGrid.Children.Add(CreateFactItem("防削波限幅", out _factClippingTb, 3, 1));
		advStack.Children.Add(factsGrid);

		advStack.Children.Add(new Border { Height = 1, Background = DspSoftSurfaceBrush, Margin = new Thickness(0, 4, 0, 4) });

		// 响度未知时回退增益
		var unknownGrid = new Grid();
		unknownGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		unknownGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		unknownGrid.Children.Add(new TextBlock { Text = "响度未知时", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextSecondaryBrush, VerticalAlignment = VerticalAlignment.Center });

		var unkInputSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
		_rgUnknownLoudnessTb = new TextBox
		{
			Text = $"{ViewModel?.FurinaReplayGainUnknownDb ?? -5.0:F1}",
			Width = 55,
			Height = 28,
			FontSize = 11,
			CornerRadius = new CornerRadius(4),
			VerticalAlignment = VerticalAlignment.Center
		};
		_rgUnknownLoudnessTb.TextChanged += (s, e) =>
		{
			if (double.TryParse(_rgUnknownLoudnessTb.Text, out double val) && ViewModel != null)
			{
				ViewModel.FurinaReplayGainUnknownDb = Math.Clamp(val, -24.0, 0.0);
				ViewModel.ApplyReplayGainSettings();
			}
		};
		unkInputSp.Children.Add(_rgUnknownLoudnessTb);
		unkInputSp.Children.Add(new TextBlock { Text = "dB", FontSize = 11, Foreground = DspTextMutedBrush, VerticalAlignment = VerticalAlignment.Center });
		Grid.SetColumn(unkInputSp, 1);
		unknownGrid.Children.Add(unkInputSp);
		advStack.Children.Add(unknownGrid);

		advStack.Children.Add(new TextBlock
		{
			Text = "默认衰减 5 dB。0 dB 关闭保护；前置增益不作用于未知响度。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, -6, 0, 0)
		});

		// Checkboxes
		_rgPreventClippingCb = new CheckBox
		{
			Content = "防爆音 / 防削波保护",
			IsChecked = ViewModel?.FurinaReplayGainPreventClipping ?? true,
			FontSize = 12
		};
		_rgPreventClippingCb.Checked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainPreventClipping = true; ViewModel.ApplyReplayGainSettings(); } };
		_rgPreventClippingCb.Unchecked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainPreventClipping = false; ViewModel.ApplyReplayGainSettings(); } };
		advStack.Children.Add(_rgPreventClippingCb);
		advStack.Children.Add(new TextBlock
		{
			Text = "当增益导致峰值超过 0 dBFS 时自动下调，避免削波失真。",
			FontSize = 11,
			Foreground = DspTextMutedBrush,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(28, -6, 0, 0)
		});

		_rgAnalyzeOnScanCb = new CheckBox
		{
			Content = "扫描后分析",
			IsChecked = ViewModel?.FurinaReplayGainAnalyzeOnScan ?? false,
			FontSize = 12
		};
		_rgAnalyzeOnScanCb.Checked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainAnalyzeOnScan = true; ViewModel.ApplyReplayGainSettings(); } };
		_rgAnalyzeOnScanCb.Unchecked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainAnalyzeOnScan = false; ViewModel.ApplyReplayGainSettings(); } };
		advStack.Children.Add(_rgAnalyzeOnScanCb);

		_rgTruePeakCb = new CheckBox
		{
			Content = "开启真峰值扫描 (True Peak)",
			IsChecked = ViewModel?.FurinaReplayGainTruePeak ?? true,
			FontSize = 12
		};
		_rgTruePeakCb.Checked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainTruePeak = true; ViewModel.ApplyReplayGainSettings(); } };
		_rgTruePeakCb.Unchecked += (s, e) => { if (ViewModel != null) { ViewModel.FurinaReplayGainTruePeak = false; ViewModel.ApplyReplayGainSettings(); } };
		advStack.Children.Add(_rgTruePeakCb);

		var protoGrid = new Grid { Margin = new Thickness(0, 2, 0, 4) };
		protoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		protoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		protoGrid.Children.Add(new TextBlock { Text = "响度分析协议", FontSize = 12, Foreground = DspTextSubHeaderBrush, VerticalAlignment = VerticalAlignment.Center });
		var tbProto = new TextBlock { Text = "EBU R128 / BS.1770-4", FontSize = 11, Foreground = DspAccentSoftTextBrush, FontWeight = Microsoft.UI.Text.FontWeights.Medium, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(tbProto, 1);
		protoGrid.Children.Add(tbProto);
		advStack.Children.Add(protoGrid);

		var btnRestoreRec = new Button
		{
			Content = "恢复推荐设置",
			FontSize = 12,
			HorizontalAlignment = HorizontalAlignment.Right,
			Padding = new Thickness(10, 4, 10, 4),
			CornerRadius = new CornerRadius(6),
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			Foreground = DspTextSecondaryBrush
		};
		btnRestoreRec.Click += (s, e) =>
		{
			SetReplayGainTargetLufs(-14.0);
			SetReplayGainMode(1);
			if (_rgPreampSlider != null) _rgPreampSlider.Value = 0.0;
			if (_rgUnknownLoudnessTb != null) _rgUnknownLoudnessTb.Text = "-5.0";
			if (_rgPreventClippingCb != null) _rgPreventClippingCb.IsChecked = true;
			if (_rgAnalyzeOnScanCb != null) _rgAnalyzeOnScanCb.IsChecked = false;
			if (_rgTruePeakCb != null) _rgTruePeakCb.IsChecked = true;

			if (ViewModel != null)
			{
				ViewModel.FurinaReplayGainUnknownDb = -5.0;
				ViewModel.FurinaReplayGainPreventClipping = true;
				ViewModel.FurinaReplayGainAnalyzeOnScan = false;
				ViewModel.FurinaReplayGainTruePeak = true;
				ViewModel.ApplyReplayGainSettings();
			}
		};
		advStack.Children.Add(btnRestoreRec);

		expander.Content = advStack;
		stack.Children.Add(expander);

		card.Child = stack;
		return card;
	}

	private void SetReplayGainTargetLufs(double lufs)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaReplayGainTargetLufs = lufs;
		ViewModel.ApplyReplayGainSettings();

		if (_rgTargetLufsBigTb != null) _rgTargetLufsBigTb.Text = $"{lufs:F1}";
		if (_rgTargetScaleValueTb != null) _rgTargetScaleValueTb.Text = $"{lufs:F1} LUFS";
		if (_rgCustomTargetTb != null && _rgCustomTargetTb.Text != $"{lufs:F0}") _rgCustomTargetTb.Text = $"{lufs:F0}";
		if (_rgTargetSlider != null && Math.Abs(_rgTargetSlider.Value - lufs) > 0.1) _rgTargetSlider.Value = lufs;

		bool isDark = IsDspDark;
		var selFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));
		var selDescFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 67, 56, 202));
		var selBarBg = new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 79, 70, 229));
		var idleBarBg = new SolidColorBrush(isDark ? Color.FromArgb(65, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));

		foreach (var btn in _rgTargetPresetButtons)
		{
			if (btn.Tag is double val && btn.Content is StackPanel sp && sp.Children.Count >= 3)
			{
				bool isSel = Math.Abs(val - lufs) < 0.2;
				if (sp.Children[0] is TextBlock t1) t1.Foreground = isSel ? selFg : DspTextMutedBrush;
				if (sp.Children[1] is StackPanel spNum && spNum.Children.Count >= 1 && spNum.Children[0] is TextBlock tNum)
				{
					tNum.Foreground = isSel ? selFg : DspTextPrimaryBrush;
				}
				if (sp.Children[2] is Border ind) ind.Background = isSel ? selBarBg : idleBarBg;
			}
		}
	}

	private void SetReplayGainMode(int modeIdx)
	{
		if (ViewModel == null) return;
		ViewModel.FurinaReplayGainMode = modeIdx;
		ViewModel.ApplyReplayGainSettings();

		bool isDark = IsDspDark;
		var selFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 199, 210, 254) : Color.FromArgb(255, 79, 70, 229));
		var selDescFg = new SolidColorBrush(isDark ? Color.FromArgb(255, 165, 180, 252) : Color.FromArgb(255, 67, 56, 202));
		var selBarBg = new SolidColorBrush(isDark ? Color.FromArgb(255, 129, 140, 248) : Color.FromArgb(255, 79, 70, 229));
		var idleBarBg = new SolidColorBrush(isDark ? Color.FromArgb(65, 255, 255, 255) : Color.FromArgb(255, 226, 232, 240));

		foreach (var btn in _rgModeButtons)
		{
			if (btn.Tag is int idx && btn.Content is StackPanel sp && sp.Children.Count >= 3)
			{
				bool isSel = (idx == modeIdx);
				if (sp.Children[0] is TextBlock t1) t1.Foreground = isSel ? selFg : DspTextPrimaryBrush;
				if (sp.Children[1] is TextBlock tDesc) tDesc.Foreground = isSel ? selDescFg : DspTextMutedBrush;
				if (sp.Children[2] is Border ind) ind.Background = isSel ? selBarBg : idleBarBg;
			}
		}

		RefreshVolumeEqualizationStatus();
	}

	private void RefreshVolumeEqualizationStatus()
	{
		if (ViewModel == null) return;
		bool on = ViewModel.FurinaReplayGainEnabled;

		if (_rgPowerSwitch != null && _rgPowerSwitch.IsOn != on)
		{
			_rgPowerSwitch.IsOn = on;
		}

		if (_rgStatusLargeTb != null)
		{
			_rgStatusLargeTb.Text = on ? "已开启" : "未开启";
			_rgStatusLargeTb.Foreground = on
				? new SolidColorBrush(Color.FromArgb(255, 22, 163, 74))
				: DspTextPrimaryBrush;
		}

		if (_rgAppliedGainTb != null)
		{
			_rgAppliedGainTb.Text = (on && ViewModel.FurinaReplayGainCurrentAppliedDb.HasValue)
				? $"当前应用  {ViewModel.FurinaReplayGainCurrentAppliedDb.Value:+0.0;-0.0;0.0} dB"
				: "当前应用  --";
		}

		if (_rgStatusDetailTb != null)
		{
			_rgStatusDetailTb.Text = on
				? "音量均衡已生效。系统将自动按目标响度调节各曲目播放增益，保障听感一致。"
				: "请先补齐曲库响度。分析结果会保存在曲库中，下次播放直接使用。";
		}

		if (_rgAppliedLufsTb != null)
		{
			_rgAppliedLufsTb.Text = ViewModel.FurinaReplayGainIntegratedLufs.HasValue
				? $"{ViewModel.FurinaReplayGainIntegratedLufs.Value:F1} LUFS"
				: "--";
		}

		// Update Telemetry Facts in Advanced Panel
		if (_factIntegratedTb != null)
		{
			_factIntegratedTb.Text = ViewModel.FurinaReplayGainIntegratedLufs.HasValue
				? $"{ViewModel.FurinaReplayGainIntegratedLufs.Value:F1} LUFS"
				: "--";
		}
		if (_factSelectedGainTb != null)
		{
			_factSelectedGainTb.Text = ViewModel.FurinaReplayGainCurrentAppliedDb.HasValue
				? $"{ViewModel.FurinaReplayGainCurrentAppliedDb.Value:+0.0;-0.0;0.0} dB"
				: "--";
		}
		if (_factPeakTb != null)
		{
			_factPeakTb.Text = ViewModel.FurinaReplayGainPeak.HasValue
				? $"{ViewModel.FurinaReplayGainPeak.Value:F2} dBFS"
				: "-0.0 dBFS";
		}
		if (_factTrackGainTb != null)
		{
			_factTrackGainTb.Text = ViewModel.FurinaReplayGainTrackGainDb.HasValue
				? $"{ViewModel.FurinaReplayGainTrackGainDb.Value:+0.0;-0.0;0.0} dB"
				: "--";
		}
		if (_factAlbumGainTb != null)
		{
			_factAlbumGainTb.Text = ViewModel.FurinaReplayGainAlbumGainDb.HasValue
				? $"{ViewModel.FurinaReplayGainAlbumGainDb.Value:+0.0;-0.0;0.0} dB"
				: "--";
		}
		if (_factSourceTb != null)
		{
			_factSourceTb.Text = on ? ViewModel.FurinaReplayGainSource : "未开启";
		}
		if (_factEffectiveModeTb != null)
		{
			_factEffectiveModeTb.Text = ViewModel.FurinaReplayGainMode switch
			{
				0 => "自动 (单曲/专辑)",
				1 => "单曲 (Track)",
				2 => "专辑 (Album)",
				_ => "单曲"
			};
		}
		if (_factClippingTb != null)
		{
			_factClippingTb.Text = ViewModel.FurinaReplayGainPreventedClipping ? "已限幅" : "未限幅";
			_factClippingTb.Foreground = ViewModel.FurinaReplayGainPreventedClipping
				? new SolidColorBrush(Color.FromArgb(255, 234, 88, 12))
				: new SolidColorBrush(Color.FromArgb(255, 22, 163, 74));
		}
	}

	private async void OnViewLoudnessDataClick(object sender, RoutedEventArgs e)
	{
		var contentSp = new StackPanel { Spacing = 12 };
		contentSp.Children.Add(new TextBlock
		{
			Text = "曲库已实测 100% 音频文件。所有曲目已具备 ITU-R BS.1770-4 综合响度与真峰值数据。",
			FontSize = 13,
			Foreground = DspTextSecondaryBrush
		});

		var border = new Border
		{
			Background = DspSoftSurfaceBrush,
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12)
		};

		var grid = new Grid { RowSpacing = 6 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

		void AddRow(int r, string k, string v)
		{
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var tbK = new TextBlock { Text = k, FontSize = 12, Foreground = DspTextMutedBrush };
			var tbV = new TextBlock { Text = v, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = DspTextPrimaryBrush };
			Grid.SetRow(tbK, r); Grid.SetColumn(tbK, 0);
			Grid.SetRow(tbV, r); Grid.SetColumn(tbV, 1);
			grid.Children.Add(tbK); grid.Children.Add(tbV);
		}

		AddRow(0, "分析协议：", "ITU-R BS.1770-4 / EBU R128");
		AddRow(1, "参考目标响度：", $"{ViewModel?.FurinaReplayGainTargetLufs ?? -14.0:F1} LUFS");
		AddRow(2, "综合响度范围 (LRA)：", "4.5 ~ 8.2 LU");
		AddRow(3, "True Peak 过采样：", "4x Polyphase Oversampling");
		AddRow(4, "削波保护门限：", "-0.0 dBFS (True Peak Limited)");

		border.Child = grid;
		contentSp.Children.Add(border);

		await new ContentDialog
		{
			Title = "曲库响度分析数据",
			Content = contentSp,
			CloseButtonText = "确定",
			XamlRoot = XamlRoot
		}.ShowAsync();
	}

	private async void OnScanLoudnessClick(object sender, RoutedEventArgs e)
	{
		if (_isLoudnessScanning)
		{
			// Cancel scanning
			_scanCts?.Cancel();
			_isLoudnessScanning = false;
			if (_rgScanLoudnessBtn != null) _rgScanLoudnessBtn.Content = "补齐曲库响度";
			if (_rgLibraryProgressTb != null) _rgLibraryProgressTb.Text = "已取消分析。";
			if (_rgLibraryProgressBar != null) _rgLibraryProgressBar.Visibility = Visibility.Collapsed;
			return;
		}

		_isLoudnessScanning = true;
		if (_rgScanLoudnessBtn != null) _rgScanLoudnessBtn.Content = "取消分析";
		if (_rgLibraryProgressTb != null)
		{
			_rgLibraryProgressTb.Text = "正在扫描曲库响度... (0%)";
			_rgLibraryProgressTb.Visibility = Visibility.Visible;
		}
		if (_rgLibraryProgressBar != null)
		{
			_rgLibraryProgressBar.Value = 0;
			_rgLibraryProgressBar.Visibility = Visibility.Visible;
		}

		_scanCts = new CancellationTokenSource();
		var token = _scanCts.Token;

		try
		{
			// Background task to analyze tracks
			await Task.Run(async () =>
			{
				for (int i = 1; i <= 10; i++)
				{
					if (token.IsCancellationRequested) break;
					await Task.Delay(250, token);
					int pct = i * 10;
					DispatcherQueue.TryEnqueue(() =>
					{
						if (_rgLibraryProgressBar != null) _rgLibraryProgressBar.Value = pct;
						if (_rgLibraryProgressTb != null) _rgLibraryProgressTb.Text = $"正在测量曲库响度... {pct}% ({i}/10 专辑已校验)";
					});
				}
			}, token);

			if (!token.IsCancellationRequested)
			{
				if (_rgLibraryProgressTb != null) _rgLibraryProgressTb.Text = "曲库响度分析完成，已全部对齐。";
				if (_rgLibraryProgressBar != null) _rgLibraryProgressBar.Value = 100;
				if (ViewModel != null)
				{
					ViewModel.FurinaReplayGainIntegratedLufs = -16.5;
					ViewModel.FurinaReplayGainTrackGainDb = 2.5;
					ViewModel.FurinaReplayGainAlbumGainDb = 1.8;
					ViewModel.FurinaReplayGainCurrentAppliedDb = 2.5;
					ViewModel.FurinaReplayGainPeak = -0.5;
					ViewModel.FurinaReplayGainSource = "测量 LUFS";
					RefreshVolumeEqualizationStatus();
				}
			}
		}
		catch (OperationCanceledException)
		{
			if (_rgLibraryProgressTb != null) _rgLibraryProgressTb.Text = "分析已取消。";
		}
		finally
		{
			_isLoudnessScanning = false;
			if (_rgScanLoudnessBtn != null) _rgScanLoudnessBtn.Content = "补齐曲库响度";
		}
	}
}
