using System;
using System.Collections.Generic;
using System.Linq;
using FurinaPlayer.Audio;
using FurinaPlayer.Audio.Services;
using FurinaPlayer.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class ConverterPage
{
	private Button? _navHearingBtn;
	private FrameworkElement? _hearingView;
	private HearingTestService? _hearingService;

	private HearingTestEar _htEar = HearingTestEar.Left;
	private int _htFreqIndex = 2; // 1000 Hz default
	private double _htCurrentDbHl = 20.0;
	private readonly double[] _htLeftThresholds = { 10, 10, 10, 10, 15, 15, 20, 25 };
	private readonly double[] _htRightThresholds = { 10, 10, 10, 10, 15, 15, 20, 25 };

	private Button? _htEarLeftBtn;
	private Button? _htEarRightBtn;
	private Button? _htEarBothBtn;
	private readonly List<Button> _htFreqButtons = new();
	private Button? _htPlayStopBtn;
	private Slider? _htDbSlider;
	private TextBlock? _htDbLabel;
	private TextBlock? _htScoreTb;
	private TextBlock? _htGradeTb;
	private TextBlock? _htSummaryTb;
	private TextBlock? _htEqPreviewTb;
	private TextBlock? _htStatusTb;
	private SKXamlCanvas? _audiogramCanvas;
	private bool _suppressHtSlider;

	private FrameworkElement CreateHearingTestView()
	{
		_hearingService ??= new HearingTestService();

		var scroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalScrollMode = ScrollMode.Disabled,
			IsHorizontalRailEnabled = false,
			IsHorizontalScrollChainingEnabled = false,
			BringIntoViewOnFocusChange = false,
			HorizontalContentAlignment = HorizontalAlignment.Center,
			ZoomMode = ZoomMode.Disabled
		};

		var rootStack = new StackPanel
		{
			Spacing = 16,
			MaxWidth = 1280,
			Margin = new Thickness(24, 18, 24, 28),
			HorizontalAlignment = HorizontalAlignment.Stretch
		};

		scroll.SizeChanged += (_, e) =>
		{
			double targetWidth = Math.Min(1280.0, Math.Max(320.0, e.NewSize.Width - 48.0));
			if (Math.Abs(rootStack.Width - targetWidth) > 0.5)
			{
				rootStack.Width = targetWidth;
			}
			if (scroll.HorizontalOffset != 0)
			{
				scroll.ChangeView(0, null, null, disableAnimation: true);
			}
		};
		scroll.BringIntoViewRequested += (_, args) => { args.Handled = true; };
		scroll.ViewChanging += (_, args) =>
		{
			if (args.NextView.HorizontalOffset != 0)
			{
				scroll.ChangeView(0, args.NextView.VerticalOffset, null, disableAnimation: true);
			}
		};
		scroll.ViewChanged += (_, _) =>
		{
			if (scroll.HorizontalOffset != 0)
			{
				scroll.ChangeView(0, null, null, disableAnimation: true);
			}
		};

		// 1. Header Card
		var headerCard = CreateCardBorder(new Thickness(20, 16, 20, 16));
		var hGrid = new Grid { ColumnSpacing = 12 };
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var hLeft = new StackPanel { Spacing = 4 };
		var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
		titleRow.Children.Add(new TextBlock
		{
			Text = Loc.T("听力检测与黄金听感"),
			FontSize = 22,
			FontWeight = FontWeights.Bold
		});
		titleRow.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(6),
			Padding = new Thickness(8, 3, 8, 3),
			Background = new SolidColorBrush(Color.FromArgb(24, 245, 158, 11)),
			VerticalAlignment = VerticalAlignment.Center,
			Child = new TextBlock
			{
				Text = Loc.T("纯音测听 Audiogram · EQ 联动"),
				FontSize = 11,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 119, 6))
			}
		});
		hLeft.Children.Add(titleRow);
		hLeft.Children.Add(new TextBlock
		{
			Text = Loc.T("参照临床纯音测听流程，分耳播放 250–16000 Hz 校准正弦波，绘制听力图并生成安全非失真 EQ 补偿方案。"),
			FontSize = 12.5,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(hLeft, 0);
		hGrid.Children.Add(hLeft);

		var applyEqBtn = new Button
		{
			Content = Loc.T("一键应用黄金听感 EQ 补偿"),
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(16, 10, 16, 10),
			Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		};
		applyEqBtn.Click += (_, _) => ApplyHearingCompensationToEq();
		Grid.SetColumn(applyEqBtn, 1);
		hGrid.Children.Add(applyEqBtn);

		headerCard.Child = hGrid;
		rootStack.Children.Add(headerCard);

		// Safety Notice
		rootStack.Children.Add(new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(14, 10, 14, 10),
			Background = new SolidColorBrush(Color.FromArgb(20, 245, 158, 11)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(60, 245, 158, 11)),
			BorderThickness = new Thickness(1),
			Child = new TextBlock
			{
				Text = Loc.T("安全提示：请佩戴耳机并在安静环境中进行测试，将系统音量调至舒适的 40%–50%。本模块内置 -10 dBFS 输出保护上限，测试结果仅供听音偏好补偿参考，不替代临床医疗诊断。"),
				FontSize = 12,
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 83, 9))
			}
		});

		// 2. Main Split: Left Console + Right Audiogram & Score
		var mainGrid = new Grid { ColumnSpacing = 16 };
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });

		// Left: Pure-tone test console
		var consoleCard = CreateCardBorder(new Thickness(18, 16, 18, 18));
		var cStack = new StackPanel { Spacing = 14 };

		cStack.Children.Add(new TextBlock { Text = Loc.T("1. 选择测试声道"), FontSize = 14, FontWeight = FontWeights.SemiBold });
		var earRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		_htEarLeftBtn = CreateHtOptionButton(Loc.T("左耳 (L)"), () => SetHtEar(HearingTestEar.Left));
		_htEarRightBtn = CreateHtOptionButton(Loc.T("右耳 (R)"), () => SetHtEar(HearingTestEar.Right));
		_htEarBothBtn = CreateHtOptionButton(Loc.T("双耳 (L+R)"), () => SetHtEar(HearingTestEar.Both));
		earRow.Children.Add(_htEarLeftBtn);
		earRow.Children.Add(_htEarRightBtn);
		earRow.Children.Add(_htEarBothBtn);
		cStack.Children.Add(earRow);

		cStack.Children.Add(new TextBlock { Text = Loc.T("2. 选择目标测试频点 (Hz)"), FontSize = 14, FontWeight = FontWeights.SemiBold });
		var freqGrid = new Grid { ColumnSpacing = 6, RowSpacing = 6 };
		for (int c = 0; c < 4; c++) freqGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		freqGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		freqGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_htFreqButtons.Clear();
		for (int i = 0; i < HearingTestService.TestFrequenciesHz.Length; i++)
		{
			double f = HearingTestService.TestFrequenciesHz[i];
			string label = f >= 1000 ? $"{f / 1000:0.#} kHz" : $"{f:0} Hz";
			int capturedIdx = i;
			var fb = CreateHtOptionButton(label, () => SelectHtFrequency(capturedIdx));
			fb.HorizontalAlignment = HorizontalAlignment.Stretch;
			_htFreqButtons.Add(fb);
			Grid.SetRow(fb, i / 4);
			Grid.SetColumn(fb, i % 4);
			freqGrid.Children.Add(fb);
		}
		cStack.Children.Add(freqGrid);

		cStack.Children.Add(new TextBlock { Text = Loc.T("3. 调节测试音听阈 (dB HL)"), FontSize = 14, FontWeight = FontWeights.SemiBold });
		var dbHeader = new Grid();
		dbHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		dbHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		dbHeader.Children.Add(new TextBlock { Text = Loc.T("当前测试强度 (越小越灵敏)"), FontSize = 12, Opacity = 0.72, VerticalAlignment = VerticalAlignment.Center });
		_htDbLabel = new TextBlock { Text = "20 dB HL", FontSize = 14, FontWeight = FontWeights.Bold, MinWidth = 76, TextAlignment = TextAlignment.Right, Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)) };
		Grid.SetColumn(_htDbLabel, 1);
		dbHeader.Children.Add(_htDbLabel);
		cStack.Children.Add(dbHeader);

		_htDbSlider = new Slider
		{
			Minimum = 0,
			Maximum = 85,
			StepFrequency = 5,
			Value = _htCurrentDbHl,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			IsThumbToolTipEnabled = false
		};
		_htDbSlider.BringIntoViewRequested += (_, args) => { args.Handled = true; };
		_htDbSlider.ValueChanged += (_, e) =>
		{
			if (_suppressHtSlider) return;
			_htCurrentDbHl = Math.Round(e.NewValue / 5.0) * 5.0;
			if (_htDbLabel != null) _htDbLabel.Text = $"{_htCurrentDbHl:0} dB HL";
			if (_hearingService?.IsPlaying == true)
			{
				_hearingService.UpdateTone(HearingTestService.TestFrequenciesHz[_htFreqIndex], _htCurrentDbHl, _htEar);
			}
		};
		cStack.Children.Add(_htDbSlider);

		// Play/Stop + Audiometry Staircase Buttons
		var actionGrid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
		actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		actionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		actionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		actionGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_htPlayStopBtn = new Button
		{
			Content = Loc.T("播放测试纯音"),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 9, 12, 9),
			Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold
		};
		_htPlayStopBtn.Click += (_, _) => ToggleHtTone();
		Grid.SetRow(_htPlayStopBtn, 0);
		Grid.SetColumn(_htPlayStopBtn, 0);
		Grid.SetColumnSpan(_htPlayStopBtn, 2);
		actionGrid.Children.Add(_htPlayStopBtn);

		var heardBtn = new Button { Content = Loc.T("能听到 (-5 dB)"), HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8) };
		heardBtn.Click += (_, _) => StepHtLevel(-5.0);
		Grid.SetRow(heardBtn, 1);
		Grid.SetColumn(heardBtn, 0);
		actionGrid.Children.Add(heardBtn);

		var notHeardBtn = new Button { Content = Loc.T("听不见 (+5 dB)"), HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8) };
		notHeardBtn.Click += (_, _) => StepHtLevel(5.0);
		Grid.SetRow(notHeardBtn, 1);
		Grid.SetColumn(notHeardBtn, 1);
		actionGrid.Children.Add(notHeardBtn);

		cStack.Children.Add(actionGrid);

		var recordRow = new Grid { ColumnSpacing = 8 };
		recordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
		recordRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		var confirmBtn = new Button
		{
			Content = Loc.T("记录当前频点听阈并下一频点"),
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 9, 12, 9),
			Background = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
			Foreground = new SolidColorBrush(Colors.White),
			FontWeight = FontWeights.SemiBold
		};
		confirmBtn.Click += (_, _) => RecordCurrentThresholdAndAdvance();
		Grid.SetColumn(confirmBtn, 0);
		recordRow.Children.Add(confirmBtn);

		var resetBtn = new Button { Content = Loc.T("重置基线"), HorizontalAlignment = HorizontalAlignment.Stretch, CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 9, 10, 9) };
		resetBtn.Click += (_, _) => ResetHearingBaseline();
		Grid.SetColumn(resetBtn, 1);
		recordRow.Children.Add(resetBtn);
		cStack.Children.Add(recordRow);

		_htStatusTb = new TextBlock
		{
			Text = Loc.T("选择左右耳与频点，点击“播放测试纯音”，以刚能听清的最小音量记录阈值。"),
			FontSize = 12,
			Opacity = 0.78,
			TextWrapping = TextWrapping.Wrap
		};
		cStack.Children.Add(_htStatusTb);

		consoleCard.Child = cStack;
		Grid.SetColumn(consoleCard, 0);
		mainGrid.Children.Add(consoleCard);

		// Right: Audiogram Chart + Golden Ear Diagnosis
		var rightStack = new StackPanel { Spacing = 14 };

		var chartCard = CreateCardBorder(new Thickness(16, 14, 16, 14));
		var chartStack = new StackPanel { Spacing = 10 };
		var chartHeader = new Grid();
		chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		chartHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		chartHeader.Children.Add(new TextBlock { Text = Loc.T("纯音听力图 (Audiogram)"), FontSize = 15, FontWeight = FontWeights.SemiBold });
		var legendRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
		legendRow.Children.Add(new TextBlock { Text = "× 左耳 (L)", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)) });
		legendRow.Children.Add(new TextBlock { Text = "○ 右耳 (R)", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)) });
		Grid.SetColumn(legendRow, 1);
		chartHeader.Children.Add(legendRow);
		chartStack.Children.Add(chartHeader);

		_audiogramCanvas = new SKXamlCanvas { Height = 270, HorizontalAlignment = HorizontalAlignment.Stretch };
		_audiogramCanvas.PaintSurface += OnPaintAudiogram;
		chartStack.Children.Add(_audiogramCanvas);
		chartCard.Child = chartStack;
		rightStack.Children.Add(chartCard);

		var resultCard = CreateCardBorder(new Thickness(16, 14, 16, 16));
		var resStack = new StackPanel { Spacing = 8 };
		var resTop = new Grid();
		resTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		resTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

		_htScoreTb = new TextBlock
		{
			Text = "92",
			FontSize = 36,
			FontWeight = FontWeights.Bold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 217, 119, 6)),
			Margin = new Thickness(0, 0, 14, 0),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_htScoreTb, 0);
		resTop.Children.Add(_htScoreTb);

		var gradeStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
		_htGradeTb = new TextBlock { Text = Loc.T("黄金听感"), FontSize = 16, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap };
		_htSummaryTb = new TextBlock { Text = string.Empty, FontSize = 12, Opacity = 0.78, TextWrapping = TextWrapping.Wrap };
		gradeStack.Children.Add(_htGradeTb);
		gradeStack.Children.Add(_htSummaryTb);
		Grid.SetColumn(gradeStack, 1);
		resTop.Children.Add(gradeStack);
		resStack.Children.Add(resTop);

		_htEqPreviewTb = new TextBlock
		{
			Text = string.Empty,
			FontSize = 11.5,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 37, 99, 235)),
			TextWrapping = TextWrapping.Wrap
		};
		resStack.Children.Add(_htEqPreviewTb);
		resultCard.Child = resStack;
		rightStack.Children.Add(resultCard);

		Grid.SetColumn(rightStack, 1);
		mainGrid.Children.Add(rightStack);

		rootStack.Children.Add(mainGrid);
		scroll.Content = rootStack;

		UpdateHtOptionStyles();
		RefreshAudiogramAnalysis();
		return scroll;
	}

	private static Button CreateHtOptionButton(string text, Action onClick)
	{
		var btn = new Button
		{
			Content = text,
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(12, 7, 12, 7),
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold
		};
		btn.Click += (_, _) => onClick();
		return btn;
	}

	private void SetHtEar(HearingTestEar ear)
	{
		_htEar = ear;
		UpdateHtOptionStyles();
		if (_hearingService?.IsPlaying == true)
		{
			_hearingService.UpdateTone(HearingTestService.TestFrequenciesHz[_htFreqIndex], _htCurrentDbHl, _htEar);
		}
	}

	private void SelectHtFrequency(int index)
	{
		_htFreqIndex = Math.Clamp(index, 0, HearingTestService.TestFrequenciesHz.Length - 1);
		double existing = _htEar == HearingTestEar.Right
			? _htRightThresholds[_htFreqIndex]
			: _htLeftThresholds[_htFreqIndex];
		_htCurrentDbHl = existing;
		_suppressHtSlider = true;
		if (_htDbSlider != null) _htDbSlider.Value = _htCurrentDbHl;
		_suppressHtSlider = false;
		if (_htDbLabel != null) _htDbLabel.Text = $"{_htCurrentDbHl:0} dB HL";
		UpdateHtOptionStyles();
		if (_hearingService?.IsPlaying == true)
		{
			_hearingService.UpdateTone(HearingTestService.TestFrequenciesHz[_htFreqIndex], _htCurrentDbHl, _htEar);
		}
	}

	private void StepHtLevel(double deltaDb)
	{
		_htCurrentDbHl = Math.Clamp(_htCurrentDbHl + deltaDb, 0.0, 85.0);
		_suppressHtSlider = true;
		if (_htDbSlider != null) _htDbSlider.Value = _htCurrentDbHl;
		_suppressHtSlider = false;
		if (_htDbLabel != null) _htDbLabel.Text = $"{_htCurrentDbHl:0} dB HL";
		if (_hearingService?.IsPlaying == true)
		{
			_hearingService.UpdateTone(HearingTestService.TestFrequenciesHz[_htFreqIndex], _htCurrentDbHl, _htEar);
		}
	}

	private void ToggleHtTone()
	{
		if (_hearingService == null) return;
		if (_hearingService.IsPlaying)
		{
			_hearingService.StopTone();
			if (_htPlayStopBtn != null)
			{
				_htPlayStopBtn.Content = Loc.T("播放测试纯音");
				_htPlayStopBtn.Background = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129));
			}
		}
		else
		{
			_hearingService.StartTone(HearingTestService.TestFrequenciesHz[_htFreqIndex], _htCurrentDbHl, _htEar);
			if (_htPlayStopBtn != null)
			{
				_htPlayStopBtn.Content = Loc.T("停止测试音");
				_htPlayStopBtn.Background = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68));
			}
		}
	}

	private void RecordCurrentThresholdAndAdvance()
	{
		if (_htEar == HearingTestEar.Left || _htEar == HearingTestEar.Both)
		{
			_htLeftThresholds[_htFreqIndex] = _htCurrentDbHl;
		}
		if (_htEar == HearingTestEar.Right || _htEar == HearingTestEar.Both)
		{
			_htRightThresholds[_htFreqIndex] = _htCurrentDbHl;
		}
		RefreshAudiogramAnalysis();

		double freq = HearingTestService.TestFrequenciesHz[_htFreqIndex];
		if (_htStatusTb != null)
		{
			_htStatusTb.Text = $"已记录 {freq:0} Hz 听阈：{_htCurrentDbHl:0} dB HL。";
		}

		if (_htFreqIndex + 1 < HearingTestService.TestFrequenciesHz.Length)
		{
			SelectHtFrequency(_htFreqIndex + 1);
		}
	}

	private void ResetHearingBaseline()
	{
		double[] baseline = { 10, 10, 10, 10, 15, 15, 20, 25 };
		Array.Copy(baseline, _htLeftThresholds, baseline.Length);
		Array.Copy(baseline, _htRightThresholds, baseline.Length);
		SelectHtFrequency(2);
		RefreshAudiogramAnalysis();
		if (_htStatusTb != null) _htStatusTb.Text = Loc.T("已恢复默认健康听阈基线。");
	}

	private void UpdateHtOptionStyles()
	{
		void StyleOpt(Button? btn, bool active)
		{
			if (btn == null) return;
			btn.Background = new SolidColorBrush(active ? Color.FromArgb(255, 37, 99, 235) : Color.FromArgb(20, 0, 0, 0));
			btn.Foreground = new SolidColorBrush(active ? Colors.White : Color.FromArgb(255, 45, 52, 64));
		}
		StyleOpt(_htEarLeftBtn, _htEar == HearingTestEar.Left);
		StyleOpt(_htEarRightBtn, _htEar == HearingTestEar.Right);
		StyleOpt(_htEarBothBtn, _htEar == HearingTestEar.Both);
		for (int i = 0; i < _htFreqButtons.Count; i++)
		{
			StyleOpt(_htFreqButtons[i], i == _htFreqIndex);
		}
	}

	private void RefreshAudiogramAnalysis()
	{
		var res = HearingTestService.AnalyzeAudiogram(_htLeftThresholds, _htRightThresholds);
		if (_htScoreTb != null) _htScoreTb.Text = res.GoldenEarScore.ToString();
		if (_htGradeTb != null) _htGradeTb.Text = $"{res.GradeLabel}  (PTA L:{res.LeftPtaDb:0.#} / R:{res.RightPtaDb:0.#} dB HL)";
		if (_htSummaryTb != null) _htSummaryTb.Text = res.SummaryText;
		if (_htEqPreviewTb != null)
		{
			var pairs = HearingTestService.EqBandFrequenciesHz
				.Select((f, idx) => $"{(f >= 1000 ? $"{f / 1000:0.#}k" : $"{f:0}")}:{(res.RecommendedEqGains10[idx] >= 0 ? "+" : "")}{res.RecommendedEqGains10[idx]:0.#}dB");
			_htEqPreviewTb.Text = $"建议 EQ 补偿 (Preamp {res.RecommendedPreampDb:0.#} dB): " + string.Join("  ", pairs);
		}
		_audiogramCanvas?.Invalidate();
	}

	private void ApplyHearingCompensationToEq()
	{
		var res = HearingTestService.AnalyzeAudiogram(_htLeftThresholds, _htRightThresholds);
		HearingTestService.ApplyCompensationToEngine(AudioEngine.Instance, new SettingsService(), res);
		if (_htStatusTb != null)
		{
			_htStatusTb.Text = Loc.T("已将黄金听感 EQ 补偿曲线应用到 FurinaPlayer 均衡器并保存配置，可前往“效果器与 DSP 设置 → 参数 EQ”查看实时曲线。");
		}
	}

	private void OnPaintAudiogram(object? sender, SKPaintSurfaceEventArgs e)
	{
		var canvas = e.Surface.Canvas;
		canvas.Clear(SKColors.Transparent);
		int w = e.Info.Width;
		int h = e.Info.Height;
		if (w < 60 || h < 60) return;

		float padL = 44f, padR = 20f, padT = 18f, padB = 30f;
		float plotW = w - padL - padR;
		float plotH = h - padT - padB;

		// Normal hearing zone (0..25 dB HL)
		float y25 = padT + (25f / 90f) * plotH;
		using (var normPaint = new SKPaint { Color = new SKColor(16, 185, 129, 28), Style = SKPaintStyle.Fill })
		{
			canvas.DrawRect(padL, padT, plotW, y25 - padT, normPaint);
		}

		// Grid lines & labels
		using var gridPaint = new SKPaint { Color = new SKColor(148, 163, 184, 75), StrokeWidth = 1f, IsAntialias = true };
		using var labelPaint = new SKPaint { Color = new SKColor(100, 116, 139, 230), TextSize = 11f, IsAntialias = true };

		for (int db = 0; db <= 90; db += 15)
		{
			float y = padT + (db / 90f) * plotH;
			canvas.DrawLine(padL, y, padL + plotW, y, gridPaint);
			canvas.DrawText($"{db}", 6f, y + 4f, labelPaint);
		}

		double minLog = Math.Log10(250.0);
		double maxLog = Math.Log10(16000.0);
		float MapX(double freq) => padL + (float)((Math.Log10(freq) - minLog) / (maxLog - minLog)) * plotW;
		float MapY(double db) => padT + (float)(Math.Clamp(db, 0.0, 90.0) / 90.0) * plotH;

		foreach (double f in HearingTestService.TestFrequenciesHz)
		{
			float x = MapX(f);
			canvas.DrawLine(x, padT, x, padT + plotH, gridPaint);
			string txt = f >= 1000 ? $"{f / 1000:0.#}k" : $"{f:0}";
			canvas.DrawText(txt, x - 11f, padT + plotH + 18f, labelPaint);
		}

		// Left ear curve (Blue, X markers)
		using var leftLine = new SKPaint { Color = new SKColor(37, 99, 235), StrokeWidth = 2.4f, Style = SKPaintStyle.Stroke, IsAntialias = true };
		using var leftPath = new SKPath();
		for (int i = 0; i < HearingTestService.TestFrequenciesHz.Length; i++)
		{
			float x = MapX(HearingTestService.TestFrequenciesHz[i]);
			float y = MapY(_htLeftThresholds[i]);
			if (i == 0) leftPath.MoveTo(x, y);
			else leftPath.LineTo(x, y);
			canvas.DrawLine(x - 5f, y - 5f, x + 5f, y + 5f, leftLine);
			canvas.DrawLine(x - 5f, y + 5f, x + 5f, y - 5f, leftLine);
		}
		canvas.DrawPath(leftPath, leftLine);

		// Right ear curve (Red, O markers)
		using var rightLine = new SKPaint { Color = new SKColor(239, 68, 68), StrokeWidth = 2.4f, Style = SKPaintStyle.Stroke, IsAntialias = true };
		using var rightPath = new SKPath();
		for (int i = 0; i < HearingTestService.TestFrequenciesHz.Length; i++)
		{
			float x = MapX(HearingTestService.TestFrequenciesHz[i]);
			float y = MapY(_htRightThresholds[i]);
			if (i == 0) rightPath.MoveTo(x, y);
			else rightPath.LineTo(x, y);
			canvas.DrawCircle(x, y, 5f, rightLine);
		}
		canvas.DrawPath(rightPath, rightLine);
	}
}
