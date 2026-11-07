using System;
using System.Collections.Generic;
using System.Diagnostics;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SonicWave.Audio.Dsp;

namespace FurinaPlayer.UI.Pages;

public sealed partial class DspPage
{
	private FrameworkElement? _listeningPlanWorkbench;
	private FrameworkElement? _outputSafetyWorkbench;
	private FrameworkElement? _audioDeviceWorkbench;

	private FrameworkElement CreateListeningPlanWorkbench()
	{
		var root = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		// 1. Top Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon { Glyph = "\uE81E", FontSize = 18, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "听音方案",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		var pill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 253, 244)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 187, 247, 208)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) },
					new TextBlock { Text = "当前方案生效中", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
				}
			}
		};
		Grid.SetColumn(pill, 1);
		headerGrid.Children.Add(pill);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// 2. Overview description
		var descTb = new TextBlock
		{
			Text = "听音方案一站式整合参数 EQ、目标频响曲线、数字余量 Headroom 与双耳空间互馈，适配不同耳机设备与听音偏好。",
			FontSize = 13,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139))
		};
		Grid.SetRow(descTb, 1);
		root.Children.Add(descTb);

		// 3. Profiles Grid
		var profiles = new (string Title, string Target, string Desc, int PresetIndex, string Tag)[]
		{
			("日常聆听 · 推荐", "平坦直通 · 自然平衡", "保留音乐原汁原味的录音混音平衡，高动态、无染色。", 0, "Flat"),
			("Moondrop / Custom", "哈曼目标曲线 Harman Target 2019", "低频适度下潜，人声清晰凝聚，高频顺滑延伸，适合现代流行音乐。", 1, "Harman"),
			("Sennheiser HD600", "弥散场声学优化 (Diffuse Field)", "优化开放式动圈耳机的高频峰值与次低频滚降，声场开阔通透。", 2, "HD600"),
			("Sony WH-1000XM5", "深潜动感重低音 (Sub-Bass Boost)", "增强 30Hz~120Hz 次低频冲击力与包围感，适合电音、摇滚与游戏配乐。", 5, "Bass"),
			("暖声人声流行", "中频饱满 · 齿音平顺", "微幅前移 1kHz~3kHz 人声泛音带，削弱高频尖锐感，聆听舒适耐听。", 7, "Vocal"),
			("开阔立体声场", "空间展开 · 双耳互馈联合优化", "结合 132% 立体声场宽度与 Chu Moy 双耳互馈，消除头中效应与疲劳感。", 8, "Stage")
		};

		var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
		var cardsGrid = new Grid { RowSpacing = 12 };
		for (int i = 0; i < profiles.Length; i++)
		{
			cardsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			var p = profiles[i];
			int presetIdx = p.PresetIndex;

			var card = new Border
			{
				CornerRadius = new CornerRadius(14),
				Padding = new Thickness(16, 14, 16, 14),
				Background = CreateAcrylicCardBrush(0.88, 235),
				BorderBrush = CreateLiquidGlassRimBrush(),
				BorderThickness = new Thickness(1)
			};
			LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark: false, enablePointerTracking: true);

			var cGrid = new Grid { ColumnSpacing = 14 };
			cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var textSp = new StackPanel { Spacing = 3 };
			textSp.Children.Add(new TextBlock { Text = p.Title, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
			textSp.Children.Add(new TextBlock { Text = p.Target, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)) });
			textSp.Children.Add(new TextBlock { Text = p.Desc, FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), TextWrapping = TextWrapping.Wrap });
			Grid.SetColumn(textSp, 0);
			cGrid.Children.Add(textSp);

			var applyBtn = new Button
			{
				Content = "应用方案",
				Padding = new Thickness(14, 6, 14, 6),
				CornerRadius = new CornerRadius(10),
				Background = LiquidGlassEngine.CreateSurfaceBrush(LiquidGlassVariant.AccentLensed, isDark: false),
				BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(LiquidGlassVariant.AccentLensed, isDark: false, isHovered: true, isSelected: true),
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
				FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
				VerticalAlignment = VerticalAlignment.Center
			};
			applyBtn.Click += (s, e) =>
			{
				ApplyEqBuiltinPreset(presetIdx);
				SelectDspCategory("param_eq");
			};
			Grid.SetColumn(applyBtn, 1);
			cGrid.Children.Add(applyBtn);

			card.Child = cGrid;
			Grid.SetRow(card, i);
			cardsGrid.Children.Add(card);
		}

		sv.Content = cardsGrid;
		Grid.SetRow(sv, 2);
		root.Children.Add(sv);

		_listeningPlanWorkbench = root;
		return root;
	}

	private FrameworkElement CreateOutputSafetyWorkbench()
	{
		var root = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		// 1. Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon { Glyph = "\uE72E", FontSize = 18, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "输出安全",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		var readyPill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 240, 253, 244)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 187, 247, 208)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			Child = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 5,
				Children =
				{
					new FontIcon { Glyph = "\uE73E", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) },
					new TextBlock { Text = "待命 · 实时防护生效中", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
				}
			}
		};
		Grid.SetColumn(readyPill, 1);
		headerGrid.Children.Add(readyPill);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// 2. Safety Guards List
		var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
		var guardsSp = new StackPanel { Spacing = 12 };

		var guards = new (string Title, string Status, string Desc, string Setting)[]
		{
			("峰值削波限制器 (True Peak Limiter)", "自动限制: 0.0 dBTP", "实时监测超采样后模拟信号真实重构峰值，杜绝 DAC 互调失真与数字破音。", "已启用 · 释放 80ms"),
			("直流偏置与次声频滤除 (DC Highpass Filter)", "截止频率: 5.0 Hz", "滤除音频源中直流偏置漂移与超低频次声扰动，保护耳机音圈与音箱低音单元不受直流烧蚀。", "硬件防护 · 持续运行"),
			("听力安全最大音量软限幅 (Safe Volume Cap)", "安全上限: -0.5 dBFS", "防止切换高电平音源或耳机校正增益过载导致爆音冲击，呵护双耳听力健康。", "全链路约束 · 自动对齐"),
			("切歌静音平滑淡入 (Soft Anti-Pop Fade)", "淡入耗时: 45 ms", "歌曲起播、暂停恢复或切歌时自动微淡入过渡，消除直流瞬态 POP 杂音与爆鸣。", "平滑引擎 · 已激活")
		};

		foreach (var g in guards)
		{
			var card = new Border
			{
				CornerRadius = new CornerRadius(14),
				Padding = new Thickness(16, 14, 16, 14),
				Background = CreateAcrylicCardBrush(0.88, 235),
				BorderBrush = CreateLiquidGlassRimBrush(),
				BorderThickness = new Thickness(1)
			};
			LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark: false, enablePointerTracking: true);

			var cGrid = new Grid { ColumnSpacing = 14 };
			cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
			cGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var textSp = new StackPanel { Spacing = 3 };
			textSp.Children.Add(new TextBlock { Text = g.Title, FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });
			textSp.Children.Add(new TextBlock { Text = g.Status, FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 22, 163, 74)) });
			textSp.Children.Add(new TextBlock { Text = g.Desc, FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)), TextWrapping = TextWrapping.Wrap });
			Grid.SetColumn(textSp, 0);
			cGrid.Children.Add(textSp);

			var settingTb = new TextBlock { Text = g.Setting, FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 71, 85, 105)), VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(settingTb, 1);
			cGrid.Children.Add(settingTb);

			card.Child = cGrid;
			guardsSp.Children.Add(card);
		}

		sv.Content = guardsSp;
		Grid.SetRow(sv, 1);
		root.Children.Add(sv);

		_outputSafetyWorkbench = root;
		return root;
	}

	private FrameworkElement CreateAudioDeviceWorkbench()
	{
		var root = new Grid
		{
			Background = new SolidColorBrush(Colors.Transparent),
			Padding = new Thickness(24, 20, 24, 20),
			RowSpacing = 16
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		// 1. Header
		var headerGrid = new Grid();
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleSp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };
		var iconBorder = new Border
		{
			Width = 36,
			Height = 36,
			CornerRadius = new CornerRadius(18),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			Child = new FontIcon { Glyph = "\uE774", FontSize = 18, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
		};
		titleSp.Children.Add(iconBorder);
		titleSp.Children.Add(new TextBlock
		{
			Text = "系统输出与音频设备",
			FontSize = 22,
			FontWeight = Microsoft.UI.Text.FontWeights.Bold,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)),
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleSp, 0);
		headerGrid.Children.Add(titleSp);

		var statusPill = new Border
		{
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 238, 242, 255)),
			BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 199, 210, 254)),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(10, 4, 10, 4),
			Child = new TextBlock { Text = "自动 · 共享模式 (低延迟)", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 70, 229)), FontWeight = Microsoft.UI.Text.FontWeights.SemiBold }
		};
		Grid.SetColumn(statusPill, 1);
		headerGrid.Children.Add(statusPill);
		Grid.SetRow(headerGrid, 0);
		root.Children.Add(headerGrid);

		// 2. Settings Card
		var sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
		var sp = new StackPanel { Spacing = 14 };

		var card = new Border
		{
			CornerRadius = new CornerRadius(14),
			Padding = new Thickness(18, 16, 18, 16),
			Background = CreateAcrylicCardBrush(0.88, 235),
			BorderBrush = CreateLiquidGlassRimBrush(),
			BorderThickness = new Thickness(1)
		};
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark: false, enablePointerTracking: true);

		var cardStack = new StackPanel { Spacing = 14 };
		cardStack.Children.Add(new TextBlock { Text = "输出驱动与模式配置", FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 15, 23, 42)) });

		var modeCombo = new ComboBox { Header = "音频驱动输出架构", MinWidth = 320, CornerRadius = new CornerRadius(8) };
		modeCombo.Items.Add(new ComboBoxItem { Content = "WASAPI 共享模式 (默认 · 支持系统并存与音效)" });
		modeCombo.Items.Add(new ComboBoxItem { Content = "WASAPI 独占模式 (Bit-Perfect 源码输出 · 超低延迟)" });
		modeCombo.Items.Add(new ComboBoxItem { Content = "ASIO 驱动直通 (专业声卡专用 · 零抖动)" });
		modeCombo.SelectedIndex = 0;
		cardStack.Children.Add(modeCombo);

		var bufferSlider = new Slider { Header = "硬件缓冲区延迟 Buffer Size (ms)", Minimum = 10, Maximum = 200, StepFrequency = 5, Value = 40, MinWidth = 320 };
		cardStack.Children.Add(bufferSlider);

		cardStack.Children.Add(new TextBlock
		{
			Text = "FurinaPlayer 音频内核自动检测声卡最佳原生采样率（最高支持 384 kHz / 32-bit Float），配合 DSP 链路实时精密运算，杜绝采样率转换抖动。",
			FontSize = 11,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		});

		card.Child = cardStack;
		sp.Children.Add(card);

		sv.Content = sp;
		Grid.SetRow(sv, 1);
		root.Children.Add(sv);

		_audioDeviceWorkbench = root;
		return root;
	}
}
