using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FurinaPlayer.Audio;
using FurinaPlayer.Audio.Output;
using FurinaPlayer.Core.Models;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Controls;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace FurinaPlayer.UI.Pages;

public sealed partial class SettingsPage
{
	private Border? _audioCheckRepairCard;
	private DispatcherQueueTimer? _checkRepairLiveTimer;
	private CancellationTokenSource? _diagWorkCts;
	private bool _checkRepairIsDark;
	private bool _isDiagOperationRunning;
	private int _isStatusInspecting;
	private string? _lastChainSignature;
	private string? _lastAnomaliesSignature;
	private string? _lastStagesSignature;

	// Overall & live telemetry controls
	private Border? _overallHealthPill;
	private TextBlock? _overallHealthPillText;
	private ToggleSwitch? _liveMonitorToggle;
	private ProgressBar? _diagGlobalProgressBar;
	private TextBlock? _diagGlobalPercentText;
	private TextBlock? _diagGlobalReasonText;

	// Assistant 1: Playback Status Checker controls
	private TextBlock? _statusPlaybackValText;
	private TextBlock? _statusOutputValText;
	private TextBlock? _statusDeviceValText;
	private TextBlock? _statusRateBufferValText;
	private StackPanel? _statusChainBadgesPanel;
	private Border? _statusAnomaliesBox;
	private StackPanel? _statusAnomaliesList;
	private Button? _togglePipelineBtn;
	private StackPanel? _pipelineStagesStack;
	private bool _pipelineDetailsExpanded;
	private Button? _copyDiagBtn;
	private TextBlock? _copyDiagBtnLabel;
	private Button? _retryPlaybackStatusBtn;
	private PlaybackStatusReport? _lastStatusReport;

	// Assistant 2: Compatibility Checker controls
	private Button? _runCompatBtn;
	private StackPanel? _compatItemsStack;

	// Assistant 3: Interactive Troubleshooting Wizard controls
	private int _wizardStepIndex;
	private TextBlock? _wizardStepTitleText;
	private TextBlock? _wizardStepCounterText;
	private TextBlock? _wizardStepDescText;
	private StackPanel? _wizardStepActionPanel;
	private ProgressBar? _wizardStepProgressBar;
	private Border? _wizardFeedbackBanner;
	private TextBlock? _wizardFeedbackText;
	private Button? _wizardPrevBtn;
	private Button? _wizardNextBtn;
	private readonly List<Button> _wizardStepPills = new();

	// Assistant 4: Reset Audio Settings controls
	private StackPanel? _repairStepsLogStack;
	private Border? _repairSummaryBanner;
	private TextBlock? _repairSummaryText;
	private Button? _btnSoftResetEngine;
	private Button? _btnRebindDefaultDev;
	private Button? _btnResetAllAudio;

	private Border CreateAudioCheckAndRepairCard(bool isDark)
	{
		_checkRepairIsDark = isDark;
		var card = new Border
		{
			Name = "AudioCheckAndRepairCard",
			CornerRadius = new CornerRadius(16),
			Margin = new Thickness(0, 0, 0, 16)
		};

		var rootStack = new StackPanel { Spacing = 16, Padding = new Thickness(16) };

		// ── Top Section Header (first TextBlock MUST be "检查与修复" for sidebar & card icon matching) ──
		var headerGrid = new Grid { ColumnSpacing = 12 };
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleLeft = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		titleLeft.Children.Add(FluentIconFactory.Create(FluentIconKind.DiagnosticLog, 20));
		titleLeft.Children.Add(new TextBlock
		{
			Text = Loc.T("检查与修复"),
			FontSize = 16,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleLeft, 0);
		headerGrid.Children.Add(titleLeft);

		var headerRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
		_overallHealthPill = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 4, 10, 4),
			VerticalAlignment = VerticalAlignment.Center
		};
		_overallHealthPillText = new TextBlock
		{
			Text = Loc.T("待检测"),
			FontSize = 12,
			FontWeight = FontWeights.SemiBold
		};
		_overallHealthPill.Child = _overallHealthPillText;
		headerRight.Children.Add(_overallHealthPill);

		_liveMonitorToggle = new ToggleSwitch
		{
			Header = null,
			OnContent = Loc.T("实时监控开"),
			OffContent = Loc.T("实时监控关"),
			IsOn = true,
			MinWidth = 0,
			VerticalAlignment = VerticalAlignment.Center
		};
		_liveMonitorToggle.Toggled += (_, _) =>
		{
			if (_liveMonitorToggle.IsOn) StartCheckRepairLiveTimer();
			else StopCheckRepairLiveTimer();
		};
		headerRight.Children.Add(_liveMonitorToggle);

		Grid.SetColumn(headerRight, 1);
		headerGrid.Children.Add(headerRight);
		rootStack.Children.Add(headerGrid);

		rootStack.Children.Add(new TextBlock
		{
			Text = Loc.T("实时检查音频播放链路、排查硬件兼容隐患、逐步引导无声修复并安全重置音频引擎，全程展示当前检查进度与原因。"),
			FontSize = 12,
			Opacity = 0.75,
			TextWrapping = TextWrapping.Wrap
		});

		// ── Live Progress & Diagnostic Reason Telemetry Bar ──
		var progressBox = CreateSectionSubCard(isDark, new Thickness(14, 10, 14, 10));
		var progressStack = new StackPanel { Spacing = 6 };
		var progressTop = new Grid { ColumnSpacing = 10 };
		progressTop.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		progressTop.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_diagGlobalReasonText = new TextBlock
		{
			Text = Loc.T("就绪：点击任意检查或修复按钮可实时查看各步骤检测结果与修复原因。"),
			FontSize = 12,
			Opacity = 0.85,
			TextWrapping = TextWrapping.Wrap,
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_diagGlobalReasonText, 0);
		progressTop.Children.Add(_diagGlobalReasonText);

		_diagGlobalPercentText = new TextBlock
		{
			Text = "100%",
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_diagGlobalPercentText, 1);
		progressTop.Children.Add(_diagGlobalPercentText);
		progressStack.Children.Add(progressTop);

		_diagGlobalProgressBar = new ProgressBar
		{
			Minimum = 0,
			Maximum = 100,
			Value = 100,
			Height = 5,
			CornerRadius = new CornerRadius(3)
		};
		progressStack.Children.Add(_diagGlobalProgressBar);
		progressBox.Child = progressStack;
		rootStack.Children.Add(progressBox);

		// ── Assistant 1: 音频播放状态检查助手 ──
		rootStack.Children.Add(BuildPlaybackStatusAssistantCard(isDark));

		// ── Assistant 2: 音频播放兼容性助手 ──
		rootStack.Children.Add(BuildCompatibilityAssistantCard(isDark));

		// ── Assistant 3: 音频播放排障助手 ──
		rootStack.Children.Add(BuildTroubleshootingWizardCard(isDark));

		// ── Assistant 4: 重置音频设置助手 ──
		rootStack.Children.Add(BuildResetAudioAssistantCard(isDark));

		card.Child = rootStack;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(card, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);

		// Initial state inspection + live 1s monitor
		RefreshPlaybackStatusSnapshot();
		UpdateWizardStepUi();
		StartCheckRepairLiveTimer();

		return card;
	}

	private Border BuildPlaybackStatusAssistantCard(bool isDark)
	{
		var box = CreateSectionSubCard(isDark);
		var sp = new StackPanel { Spacing = 10 };

		// Header row
		var hdr = new Grid { ColumnSpacing = 10 };
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var hdrLeft = new StackPanel { Spacing = 2 };
		hdrLeft.Children.Add(new TextBlock
		{
			Text = Loc.T("1. 音频播放状态检查助手"),
			FontSize = 15,
			FontWeight = FontWeights.SemiBold
		});
		hdrLeft.Children.Add(new TextBlock
		{
			Text = Loc.T("实时监控播放状态、输出模式、设备采样率握手与 6 段 DSP 信号链路，卡顿/无声时快速定位异常。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(hdrLeft, 0);
		hdr.Children.Add(hdrLeft);

		var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

		var refreshBtn = CreateActionButton(Loc.T("刷新检查"), FluentIconKind.RefreshReset, isAccent: false);
		refreshBtn.Click += (_, _) =>
		{
			SetGlobalProgress(35, Loc.T("正在采集音频引擎会话与 Windows 端点状态..."));
			RefreshPlaybackStatusSnapshot();
			SetGlobalProgress(100, Loc.T("实时播放链路快照已更新。"));
		};
		btnRow.Children.Add(refreshBtn);

		_copyDiagBtn = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 6, 10, 6)
		};
		var copySp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		copySp.Children.Add(FluentIconFactory.Create(FluentIconKind.Save, 14));
		_copyDiagBtnLabel = new TextBlock { Text = Loc.T("复制诊断信息"), FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
		copySp.Children.Add(_copyDiagBtnLabel);
		_copyDiagBtn.Content = copySp;
		_copyDiagBtn.Click += OnCopyDiagnosticsClick;
		btnRow.Children.Add(_copyDiagBtn);

		_retryPlaybackStatusBtn = CreateActionButton(Loc.T("尝试播放"), FluentIconKind.Play, isAccent: true);
		_retryPlaybackStatusBtn.Click += OnRetryPlayCurrentTrackClick;
		btnRow.Children.Add(_retryPlaybackStatusBtn);

		Grid.SetColumn(btnRow, 1);
		hdr.Children.Add(btnRow);
		sp.Children.Add(hdr);

		// 2x2 Key Metrics Grid
		var metricsGrid = new Grid { ColumnSpacing = 10, RowSpacing = 10 };
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		metricsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		metricsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		metricsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

		_statusPlaybackValText = AddMetricTile(metricsGrid, 0, 0, Loc.T("当前状态"), "--", isDark);
		_statusOutputValText = AddMetricTile(metricsGrid, 0, 1, Loc.T("输出方式与后端"), "--", isDark);
		_statusDeviceValText = AddMetricTile(metricsGrid, 1, 0, Loc.T("输出设备与硬件"), "--", isDark);
		_statusRateBufferValText = AddMetricTile(metricsGrid, 1, 1, Loc.T("采样率 · 缓冲 · 电平"), "--", isDark);
		sp.Children.Add(metricsGrid);

		// Signal Chain Badges
		_statusChainBadgesPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		sp.Children.Add(_statusChainBadgesPanel);

		// Anomaly Banner
		_statusAnomaliesBox = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 10, 12, 10),
			Visibility = Visibility.Collapsed
		};
		_statusAnomaliesList = new StackPanel { Spacing = 4 };
		_statusAnomaliesBox.Child = _statusAnomaliesList;
		sp.Children.Add(_statusAnomaliesBox);

		// Collapsible 6-Stage Pipeline
		_togglePipelineBtn = new Button
		{
			Content = Loc.T("查看 6 段详细信号链路状态 ▾"),
			CornerRadius = new CornerRadius(8),
			Padding = new Thickness(10, 5, 10, 5),
			FontSize = 12,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		_togglePipelineBtn.Click += (_, _) =>
		{
			_pipelineDetailsExpanded = !_pipelineDetailsExpanded;
			if (_pipelineStagesStack != null)
			{
				_pipelineStagesStack.Visibility = _pipelineDetailsExpanded ? Visibility.Visible : Visibility.Collapsed;
				if (_pipelineDetailsExpanded && _lastStatusReport != null)
				{
					RenderPipelineStagesIfExpanded(_lastStatusReport);
				}
			}
			_togglePipelineBtn.Content = _pipelineDetailsExpanded
				? Loc.T("收起 6 段详细信号链路状态 ▴")
				: Loc.T("查看 6 段详细信号链路状态 ▾");
		};
		sp.Children.Add(_togglePipelineBtn);

		_pipelineStagesStack = new StackPanel
		{
			Spacing = 6,
			Visibility = Visibility.Collapsed
		};
		sp.Children.Add(_pipelineStagesStack);

		box.Child = sp;
		return box;
	}

	private Border BuildCompatibilityAssistantCard(bool isDark)
	{
		var box = CreateSectionSubCard(isDark);
		var sp = new StackPanel { Spacing = 10 };

		var hdr = new Grid { ColumnSpacing = 10 };
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var left = new StackPanel { Spacing = 2 };
		left.Children.Add(new TextBlock
		{
			Text = Loc.T("2. 音频播放兼容性助手"),
			FontSize = 15,
			FontWeight = FontWeights.SemiBold
		});
		left.Children.Add(new TextBlock
		{
			Text = Loc.T("深度扫描 WASAPI 独占握手、设备离线绑定、高倍频升频缓冲、DSP 负前级与本地解码器状态，并提供一键修复。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(left, 0);
		hdr.Children.Add(left);

		_runCompatBtn = CreateActionButton(Loc.T("开始全面兼容扫描"), FluentIconKind.HiRes, isAccent: true);
		_runCompatBtn.Click += OnRunCompatibilityScanClick;
		Grid.SetColumn(_runCompatBtn, 1);
		hdr.Children.Add(_runCompatBtn);
		sp.Children.Add(hdr);

		_compatItemsStack = new StackPanel { Spacing = 8 };
		_compatItemsStack.Children.Add(new TextBlock
		{
			Text = Loc.T("尚未扫描：点击右侧“开始全面兼容扫描”，实时查看每一步检测原因与优化建议。"),
			FontSize = 12,
			Opacity = 0.7
		});
		sp.Children.Add(_compatItemsStack);

		box.Child = sp;
		return box;
	}

	private Border BuildTroubleshootingWizardCard(bool isDark)
	{
		var box = CreateSectionSubCard(isDark);
		var sp = new StackPanel { Spacing = 12 };

		var hdr = new Grid { ColumnSpacing = 10 };
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		hdr.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var left = new StackPanel { Spacing = 2 };
		left.Children.Add(new TextBlock
		{
			Text = Loc.T("3. 音频播放排障助手（无声与点播失败向导）"),
			FontSize = 15,
			FontWeight = FontWeights.SemiBold
		});
		left.Children.Add(new TextBlock
		{
			Text = Loc.T("按步骤检查音量静音、输出模式冲突、设备绑定、SRC 升频、VST3 链路并用 523Hz 测试提示音现场验证发声。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});
		Grid.SetColumn(left, 0);
		hdr.Children.Add(left);

		_wizardStepCounterText = new TextBlock
		{
			Text = Loc.F("步骤 {0} / 6", 1),
			FontSize = 12,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 189, 248)),
			VerticalAlignment = VerticalAlignment.Center
		};
		Grid.SetColumn(_wizardStepCounterText, 1);
		hdr.Children.Add(_wizardStepCounterText);
		sp.Children.Add(hdr);

		// Step indicator pills row
		var pillsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		_wizardStepPills.Clear();
		string[] pillLabels =
		{
			Loc.T("1.音量"),
			Loc.T("2.模式"),
			Loc.T("3.设备"),
			Loc.T("4.升频"),
			Loc.T("5.插件"),
			Loc.T("6.实测")
		};
		for (int i = 0; i < pillLabels.Length; i++)
		{
			int stepIdx = i;
			var pBtn = new Button
			{
				Content = pillLabels[i],
				FontSize = 12,
				CornerRadius = new CornerRadius(8),
				Padding = new Thickness(10, 4, 10, 4)
			};
			pBtn.Click += (_, _) =>
			{
				_wizardStepIndex = stepIdx;
				UpdateWizardStepUi();
			};
			_wizardStepPills.Add(pBtn);
			pillsRow.Children.Add(pBtn);
		}
		sp.Children.Add(pillsRow);

		_wizardStepProgressBar = new ProgressBar
		{
			Minimum = 0,
			Maximum = 6,
			Value = 1,
			Height = 4,
			CornerRadius = new CornerRadius(2)
		};
		sp.Children.Add(_wizardStepProgressBar);

		// Step active card body
		var bodyBorder = CreateSectionSubCard(isDark, new Thickness(14, 12, 14, 12));
		var bodyStack = new StackPanel { Spacing = 8 };
		_wizardStepTitleText = new TextBlock { FontSize = 14, FontWeight = FontWeights.SemiBold };
		_wizardStepDescText = new TextBlock { FontSize = 12.5, Opacity = 0.85, TextWrapping = TextWrapping.Wrap };
		_wizardStepActionPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		bodyStack.Children.Add(_wizardStepTitleText);
		bodyStack.Children.Add(_wizardStepDescText);
		bodyStack.Children.Add(_wizardStepActionPanel);
		bodyBorder.Child = bodyStack;
		sp.Children.Add(bodyBorder);

		// Inline step feedback banner
		_wizardFeedbackBanner = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 8, 12, 8),
			Visibility = Visibility.Collapsed
		};
		_wizardFeedbackText = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
		_wizardFeedbackBanner.Child = _wizardFeedbackText;
		sp.Children.Add(_wizardFeedbackBanner);

		// Prev / Next footer
		var navGrid = new Grid();
		navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		navGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		_wizardPrevBtn = new Button
		{
			Content = Loc.T("← 上一步"),
			CornerRadius = new CornerRadius(9),
			Padding = new Thickness(12, 6, 12, 6)
		};
		_wizardPrevBtn.Click += (_, _) =>
		{
			if (_wizardStepIndex > 0)
			{
				_wizardStepIndex--;
				UpdateWizardStepUi();
			}
		};
		Grid.SetColumn(_wizardPrevBtn, 0);
		navGrid.Children.Add(_wizardPrevBtn);

		_wizardNextBtn = new Button
		{
			Content = Loc.T("下一步 →"),
			CornerRadius = new CornerRadius(9),
			Padding = new Thickness(12, 6, 12, 6)
		};
		_wizardNextBtn.Click += (_, _) =>
		{
			if (_wizardStepIndex < 5)
			{
				_wizardStepIndex++;
				UpdateWizardStepUi();
			}
		};
		Grid.SetColumn(_wizardNextBtn, 2);
		navGrid.Children.Add(_wizardNextBtn);

		sp.Children.Add(navGrid);
		box.Child = sp;
		return box;
	}

	private Border BuildResetAudioAssistantCard(bool isDark)
	{
		var box = CreateSectionSubCard(isDark);
		var sp = new StackPanel { Spacing = 10 };

		sp.Children.Add(new TextBlock
		{
			Text = Loc.T("4. 重置音频设置助手"),
			FontSize = 15,
			FontWeight = FontWeights.SemiBold
		});
		sp.Children.Add(new TextBlock
		{
			Text = Loc.T("无需退出软件，快速释放并重建卡死音频引擎，或安全还原所有音频与 DSP 设置（音乐库目录与外观主题完好保留）。"),
			FontSize = 12,
			Opacity = 0.72,
			TextWrapping = TextWrapping.Wrap
		});

		var actionRow1 = CreateResetRow(
			Loc.T("热重置音频引擎"),
			Loc.T("停止当前播放并释放 NAudio / LibVLC 后端实例，重建输出握手。设备或驱动卡住时优先尝试此项。"),
			Loc.T("重置音频引擎"),
			FluentIconKind.RefreshReset,
			isAccent: false,
			out _btnSoftResetEngine);
		if (_btnSoftResetEngine != null)
		{
			_btnSoftResetEngine.Click += async (_, _) => await RunRepairWithLiveProgressAsync("reset_engine");
		}
		sp.Children.Add(actionRow1);

		var actionRow2 = CreateResetRow(
			Loc.T("刷新并重绑默认输出设备"),
			Loc.T("重新枚举 USB DAC、蓝牙耳机与声卡端点，并将输出绑定切换为当前 Windows 默认设备。"),
			Loc.T("重绑默认设备"),
			FluentIconKind.DeviceAudio,
			isAccent: false,
			out _btnRebindDefaultDev);
		if (_btnRebindDefaultDev != null)
		{
			_btnRebindDefaultDev.Click += async (_, _) => await RunRepairWithLiveProgressAsync("rebind_default");
		}
		sp.Children.Add(actionRow2);

		var actionRow3 = CreateResetRow(
			Loc.T("重置所有音频设置"),
			Loc.T("还原为 WASAPI 共享、系统默认设备、200 ms 缓冲、80% 音量并清除 EQ/升频/VST3（保留音乐库与界面配置）。"),
			Loc.T("重置所有音频设置"),
			FluentIconKind.Delete,
			isAccent: true,
			out _btnResetAllAudio);
		if (_btnResetAllAudio != null)
		{
			_btnResetAllAudio.Click += OnResetAllAudioConfirmedClick;
		}
		sp.Children.Add(actionRow3);

		_repairSummaryBanner = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 8, 12, 8),
			Visibility = Visibility.Collapsed
		};
		_repairSummaryText = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
		_repairSummaryBanner.Child = _repairSummaryText;
		sp.Children.Add(_repairSummaryBanner);

		_repairStepsLogStack = new StackPanel { Spacing = 4 };
		sp.Children.Add(_repairStepsLogStack);

		box.Child = sp;
		return box;
	}

	private Grid CreateResetRow(string title, string desc, string btnLabel, FluentIconKind iconKind, bool isAccent, out Button button)
	{
		var g = new Grid { ColumnSpacing = 12, Margin = new Thickness(0, 2, 0, 2) };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var info = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		info.Children.Add(new TextBlock { Text = title, FontSize = 13.5, FontWeight = FontWeights.Medium });
		info.Children.Add(new TextBlock { Text = desc, FontSize = 12, Opacity = 0.72, TextWrapping = TextWrapping.Wrap });
		Grid.SetColumn(info, 0);
		g.Children.Add(info);

		button = CreateActionButton(btnLabel, iconKind, isAccent);
		Grid.SetColumn(button, 1);
		g.Children.Add(button);
		return g;
	}

	private static Border CreateSectionSubCard(bool isDark, Thickness? padding = null)
	{
		return new Border
		{
			CornerRadius = new CornerRadius(12),
			Padding = padding ?? new Thickness(14, 12, 14, 12),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(145, 255, 255, 255)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(36, 255, 255, 255) : Color.FromArgb(65, 148, 163, 184))
		};
	}

	private TextBlock AddMetricTile(Grid parent, int row, int col, string label, string value, bool isDark)
	{
		var tile = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(10, 8, 10, 8),
			Background = new SolidColorBrush(isDark ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(120, 241, 245, 249)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(isDark ? Color.FromArgb(28, 255, 255, 255) : Color.FromArgb(50, 148, 163, 184))
		};
		var stack = new StackPanel { Spacing = 2 };
		stack.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Opacity = 0.68 });
		var valTb = new TextBlock
		{
			Text = value,
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			TextWrapping = TextWrapping.Wrap
		};
		stack.Children.Add(valTb);
		tile.Child = stack;
		Grid.SetRow(tile, row);
		Grid.SetColumn(tile, col);
		parent.Children.Add(tile);
		return valTb;
	}

	private Button CreateActionButton(string text, FluentIconKind icon, bool isAccent)
	{
		var btn = new Button
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 7, 12, 7),
			VerticalAlignment = VerticalAlignment.Center
		};
		var sp = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
		sp.Children.Add(FluentIconFactory.Create(icon, 15));
		sp.Children.Add(new TextBlock
		{
			Text = text,
			FontSize = 12.5,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		btn.Content = sp;
		LiquidGlassEngine.AttachInteractiveLiquidGlass(
			btn,
			isAccent ? LiquidGlassVariant.AccentLensed : LiquidGlassVariant.Clear,
			_checkRepairIsDark);
		return btn;
	}

	private void RefreshPlaybackStatusSnapshot()
	{
		_ = RefreshPlaybackStatusSnapshotAsync();
	}

	private async Task RefreshPlaybackStatusSnapshotAsync()
	{
		if (Interlocked.CompareExchange(ref _isStatusInspecting, 1, 0) != 0)
		{
			return;
		}
		try
		{
			var settings = ViewModel?.CurrentAppSettings ?? ViewModel?.Settings ?? new AppSettings();
			var report = await Task.Run(() => AudioDiagnosticsService.InspectPlaybackStatus(settings, AudioSession.Instance));
			if (DispatcherQueue != null)
			{
				DispatcherQueue.TryEnqueue(() => ApplyStatusReportToUi(report, settings));
			}
			else
			{
				ApplyStatusReportToUi(report, settings);
			}
		}
		catch
		{
		}
		finally
		{
			Interlocked.Exchange(ref _isStatusInspecting, 0);
		}
	}

	private void ApplyStatusReportToUi(PlaybackStatusReport report, AppSettings settings)
	{
		try
		{
			_lastStatusReport = report;

			if (_overallHealthPill != null && _overallHealthPillText != null)
			{
				(Color bg, Color fg) = GetSeverityColors(report.OverallHealth);
				_overallHealthPill.Background = new SolidColorBrush(bg);
				_overallHealthPillText.Foreground = new SolidColorBrush(fg);
				_overallHealthPillText.Text = report.HealthBadgeText;
			}

			if (_statusPlaybackValText != null)
			{
				string trackLine = string.IsNullOrWhiteSpace(report.Runtime.CurrentTrackTitle)
					? report.PlaybackStateText
					: $"{report.PlaybackStateText} · {report.Runtime.CurrentTrackTitle}";
				_statusPlaybackValText.Text = trackLine;
			}
			if (_statusOutputValText != null) _statusOutputValText.Text = report.OutputSummaryText;
			if (_statusDeviceValText != null)
			{
				_statusDeviceValText.Text = $"{report.ActiveDeviceName} ({report.EndpointSampleRate} Hz / {report.EndpointBitsPerSample}-bit)";
			}
			if (_statusRateBufferValText != null)
			{
				string volNote = report.SystemEndpointMuted ? Loc.T("系统静音!") : $"{Math.Round(report.SystemMasterVolume01 * 100)}%";
				_statusRateBufferValText.Text = Loc.F(
					"{0}→{1} Hz · 缓冲 {2} ms · 系统 {3} / 应用 {4}%",
					report.Runtime.SourceSampleRate > 0 ? report.Runtime.SourceSampleRate : report.EndpointSampleRate,
					report.Runtime.OutputSampleRate > 0 ? report.Runtime.OutputSampleRate : report.EndpointSampleRate,
					settings.BufferMilliseconds,
					volNote,
					settings.Volume);
			}

			if (_statusChainBadgesPanel != null)
			{
				string chainSig = _checkRepairIsDark + "|" + string.Join("|", report.SignalChainBadges);
				if (!string.Equals(chainSig, _lastChainSignature, StringComparison.Ordinal))
				{
					_lastChainSignature = chainSig;
					_statusChainBadgesPanel.Children.Clear();
					foreach (string b in report.SignalChainBadges)
					{
						_statusChainBadgesPanel.Children.Add(new Border
						{
							CornerRadius = new CornerRadius(7),
							Padding = new Thickness(8, 3, 8, 3),
							Background = new SolidColorBrush(_checkRepairIsDark ? Color.FromArgb(38, 56, 189, 248) : Color.FromArgb(32, 14, 165, 233)),
							Child = new TextBlock { Text = b, FontSize = 11.5, FontWeight = FontWeights.Medium }
						});
					}
				}
			}

			if (_statusAnomaliesBox != null && _statusAnomaliesList != null)
			{
				string anomSig = $"{_checkRepairIsDark}|{(int)report.OverallHealth}|" + string.Join("|", report.AnomalyReasons);
				if (!string.Equals(anomSig, _lastAnomaliesSignature, StringComparison.Ordinal))
				{
					_lastAnomaliesSignature = anomSig;
					_statusAnomaliesList.Children.Clear();
					if (report.AnomalyReasons.Count > 0)
					{
						(Color bg, Color fg) = GetSeverityColors(report.OverallHealth == DiagnosticSeverity.Ok ? DiagnosticSeverity.Warning : report.OverallHealth);
						_statusAnomaliesBox.Background = new SolidColorBrush(bg);
						_statusAnomaliesBox.Visibility = Visibility.Visible;
						_statusAnomaliesList.Children.Add(new TextBlock
						{
							Text = Loc.T("检测到的异常原因与处理提示："),
							FontSize = 12,
							FontWeight = FontWeights.SemiBold,
							Foreground = new SolidColorBrush(fg)
						});
						foreach (string reason in report.AnomalyReasons)
						{
							_statusAnomaliesList.Children.Add(new TextBlock
							{
								Text = "• " + reason,
								FontSize = 12,
								TextWrapping = TextWrapping.Wrap,
								Foreground = new SolidColorBrush(fg)
							});
						}
					}
					else
					{
						_statusAnomaliesBox.Visibility = Visibility.Collapsed;
					}
				}
			}

			if (_pipelineDetailsExpanded)
			{
				RenderPipelineStagesIfExpanded(report);
			}
		}
		catch
		{
		}
	}

	private void RenderPipelineStagesIfExpanded(PlaybackStatusReport report)
	{
		if (_pipelineStagesStack == null) return;
		string stageSig = _checkRepairIsDark + "|" + string.Join("|", report.Stages.Select(s => $"{(int)s.Severity}:{s.Title}:{s.Summary}:{s.Detail}"));
		if (string.Equals(stageSig, _lastStagesSignature, StringComparison.Ordinal))
		{
			return;
		}
		_lastStagesSignature = stageSig;
		_pipelineStagesStack.Children.Clear();
		foreach (var stage in report.Stages)
		{
			(Color bg, Color fg) = GetSeverityColors(stage.Severity);
			var row = new Grid
			{
				ColumnSpacing = 10,
				Padding = new Thickness(10, 7, 10, 7),
				CornerRadius = new CornerRadius(9),
				Background = new SolidColorBrush(_checkRepairIsDark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(110, 248, 250, 252))
			};
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			var badge = new Border
			{
				CornerRadius = new CornerRadius(6),
				Padding = new Thickness(7, 2, 7, 2),
				Background = new SolidColorBrush(bg),
				VerticalAlignment = VerticalAlignment.Center,
				Child = new TextBlock { Text = stage.Severity == DiagnosticSeverity.Ok ? Loc.T("正常") : (stage.Severity == DiagnosticSeverity.Error ? Loc.T("异常") : Loc.T("注意")), FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(fg) }
			};
			Grid.SetColumn(badge, 0);
			row.Children.Add(badge);

			var titleTb = new TextBlock { Text = stage.Title, FontSize = 12.5, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(titleTb, 1);
			row.Children.Add(titleTb);

			var descPanel = new StackPanel { Spacing = 1 };
			descPanel.Children.Add(new TextBlock { Text = stage.Summary, FontSize = 12.5, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap });
			descPanel.Children.Add(new TextBlock { Text = stage.Detail, FontSize = 11.5, Opacity = 0.72, TextWrapping = TextWrapping.Wrap });
			Grid.SetColumn(descPanel, 2);
			row.Children.Add(descPanel);

			_pipelineStagesStack.Children.Add(row);
		}
	}

	private async void OnRunCompatibilityScanClick(object sender, RoutedEventArgs e)
	{
		if (_isDiagOperationRunning) return;
		_isDiagOperationRunning = true;
		_diagWorkCts?.Cancel();
		_diagWorkCts = new CancellationTokenSource();
		var ct = _diagWorkCts.Token;

		if (_runCompatBtn != null) _runCompatBtn.IsEnabled = false;
		if (_compatItemsStack != null) _compatItemsStack.Children.Clear();
		SetGlobalProgress(5, Loc.T("正在启动 6 项音频兼容性体检..."));

		try
		{
			var settings = ViewModel?.CurrentAppSettings ?? ViewModel?.Settings ?? new AppSettings();
			var items = await AudioDiagnosticsService.RunCompatibilityAuditAsync(
				settings,
				AudioSession.Instance,
				update =>
				{
					DispatcherQueue?.TryEnqueue(() =>
					{
						SetGlobalProgress(update.Progress01 * 100.0, $"{update.CurrentStepTitle} — {update.StatusReason}");
						if (update.CompletedItem != null)
						{
							AppendCompatibilityItemRow(update.CompletedItem);
						}
					});
				},
				ct);

			int issueCount = items.Count(x => x.Severity >= DiagnosticSeverity.Warning);
			SetGlobalProgress(100, issueCount == 0
				? Loc.T("全部 6 项兼容性体检均通过！当前音频配置与系统设备匹配良好。")
				: Loc.F("体检完成：发现 {0} 项需要注意或可一键优化的配置，请查看下方修复建议。", issueCount));
			RefreshPlaybackStatusSnapshot();
		}
		catch (OperationCanceledException)
		{
			SetGlobalProgress(0, Loc.T("兼容性扫描已取消。"));
		}
		finally
		{
			if (_runCompatBtn != null) _runCompatBtn.IsEnabled = true;
			_isDiagOperationRunning = false;
		}
	}

	private void AppendCompatibilityItemRow(CompatibilityCheckItem item)
	{
		if (_compatItemsStack == null) return;
		(Color bg, Color fg) = GetSeverityColors(item.Severity);

		var row = new Border
		{
			CornerRadius = new CornerRadius(10),
			Padding = new Thickness(12, 9, 12, 9),
			Background = new SolidColorBrush(_checkRepairIsDark ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(125, 255, 255, 255)),
			BorderThickness = new Thickness(1),
			BorderBrush = new SolidColorBrush(_checkRepairIsDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(55, 148, 163, 184))
		};

		var g = new Grid { ColumnSpacing = 10 };
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var badge = new Border
		{
			CornerRadius = new CornerRadius(7),
			Padding = new Thickness(8, 3, 8, 3),
			Background = new SolidColorBrush(bg),
			VerticalAlignment = VerticalAlignment.Top,
			Child = new TextBlock
			{
				Text = item.Severity == DiagnosticSeverity.Ok ? Loc.T("通过") : (item.Severity == DiagnosticSeverity.Error ? Loc.T("异常") : Loc.T("建议优化")),
				FontSize = 11.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(fg)
			}
		};
		Grid.SetColumn(badge, 0);
		g.Children.Add(badge);

		var textSp = new StackPanel { Spacing = 2 };
		textSp.Children.Add(new TextBlock { Text = $"{item.Title} · {item.ResultSummary}", FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
		textSp.Children.Add(new TextBlock { Text = item.ReasonDetail, FontSize = 12, Opacity = 0.76, TextWrapping = TextWrapping.Wrap });
		Grid.SetColumn(textSp, 1);
		g.Children.Add(textSp);

		if (!string.IsNullOrWhiteSpace(item.RecommendedFixId) && !string.IsNullOrWhiteSpace(item.RecommendedFixLabel))
		{
			string repairId = item.RecommendedFixId;
			var fixBtn = CreateActionButton(item.RecommendedFixLabel, FluentIconKind.RefreshReset, isAccent: true);
			fixBtn.Click += async (_, _) => await RunRepairWithLiveProgressAsync(repairId);
			Grid.SetColumn(fixBtn, 2);
			g.Children.Add(fixBtn);
		}

		row.Child = g;
		_compatItemsStack.Children.Add(row);
	}

	private void UpdateWizardStepUi()
	{
		if (_wizardStepCounterText != null) _wizardStepCounterText.Text = Loc.F("步骤 {0} / 6", _wizardStepIndex + 1);
		if (_wizardStepProgressBar != null) _wizardStepProgressBar.Value = _wizardStepIndex + 1;
		if (_wizardPrevBtn != null) _wizardPrevBtn.IsEnabled = _wizardStepIndex > 0;
		if (_wizardNextBtn != null) _wizardNextBtn.IsEnabled = _wizardStepIndex < 5;

		for (int i = 0; i < _wizardStepPills.Count; i++)
		{
			LiquidGlassEngine.AttachInteractiveLiquidGlass(
				_wizardStepPills[i],
				i == _wizardStepIndex ? LiquidGlassVariant.AccentLensed : LiquidGlassVariant.Clear,
				_checkRepairIsDark);
		}

		if (_wizardStepTitleText == null || _wizardStepDescText == null || _wizardStepActionPanel == null) return;
		_wizardStepActionPanel.Children.Clear();

		var settings = ViewModel?.CurrentAppSettings ?? ViewModel?.Settings ?? new AppSettings();
		var status = _lastStatusReport ?? AudioDiagnosticsService.InspectPlaybackStatus(settings, AudioSession.Instance);

		switch (_wizardStepIndex)
		{
			case 0:
				_wizardStepTitleText.Text = Loc.T("步骤 1：检查系统输出音量、静音与软件增益");
				_wizardStepDescText.Text = Loc.F(
					"当前系统音量：{0}%（静音={1}），FurinaPlayer 应用音量：{2}%。如果被误静音或前级负增益过大，即使播放进度走动也听不见声音。",
					Math.Round(status.SystemMasterVolume01 * 100),
					status.SystemEndpointMuted ? Loc.T("是") : Loc.T("否"),
					settings.Volume);
				var b1 = CreateActionButton(Loc.T("解除静音并恢复 80% 安全音量"), FluentIconKind.Volume, isAccent: true);
				b1.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("unmute_volume");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				_wizardStepActionPanel.Children.Add(b1);
				break;

			case 1:
				_wizardStepTitleText.Text = Loc.T("步骤 2：核查音频输出模式是否与系统软件冲突");
				_wizardStepDescText.Text = Loc.F(
					"当前模式：{0}。WASAPI 独占模式与部分 ASIO 驱动容易被浏览器、语音通话或其他播放器抢占导致“点击播放后瞬间停止或无声”。建议先切回兼容性最佳的 WASAPI 共享模式。",
					status.OutputSummaryText);
				var b2 = CreateActionButton(Loc.T("切回 WASAPI 共享（推荐）"), FluentIconKind.DeviceAudio, isAccent: true);
				b2.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("switch_shared");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				_wizardStepActionPanel.Children.Add(b2);
				break;

			case 2:
				_wizardStepTitleText.Text = Loc.T("步骤 3：校验输出设备热切换与 Windows 默认端点");
				_wizardStepDescText.Text = Loc.F(
					"当前目标设备：{0}{1}。拔插耳机、蓝牙休眠重连或外置 DAC 断电后，旧设备句柄会失效。点此可重新扫描并重绑当前默认声卡。",
					status.ActiveDeviceName,
					status.ConfiguredDeviceMissing ? Loc.T("（已离线）") : string.Empty);
				var b3 = CreateActionButton(Loc.T("刷新并绑定系统默认设备"), FluentIconKind.RefreshReset, isAccent: true);
				b3.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("rebind_default");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				_wizardStepActionPanel.Children.Add(b3);
				break;

			case 3:
				_wizardStepTitleText.Text = Loc.T("步骤 4：验证升频 (FurinaSrc) 与缓冲兼容");
				_wizardStepDescText.Text = Loc.F(
					"当前升频设置：Mode={0}，输出缓冲={1} ms。部分 USB DAC 或蓝牙耳塞不支持过高的目标采样率，或低缓冲下易出现爆音/无声，可临时恢复原生直通对比确认。",
					settings.FurinaSrcMode ?? "off",
					settings.BufferMilliseconds);
				var b4 = CreateActionButton(Loc.T("关闭升频恢复原生直通"), FluentIconKind.HiRes, isAccent: true);
				b4.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("bypass_dsp_src");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				var b4Buf = CreateActionButton(Loc.T("调优缓冲为 200 ms"), FluentIconKind.Dynamics, isAccent: false);
				b4Buf.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("tune_buffer");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				_wizardStepActionPanel.Children.Add(b4);
				_wizardStepActionPanel.Children.Add(b4Buf);
				break;

			case 4:
				_wizardStepTitleText.Text = Loc.T("步骤 5：检查 EQ / Headroom 与第三方 VST3 插件链路");
				_wizardStepDescText.Text = Loc.F(
					"当前 DSP 状态：{0}（VST3={1}）。个别异常插件或极低前级衰减会阻断信号或输出静音。",
					status.Runtime.DspActive ? Loc.T("已启用") : Loc.T("直通"),
					status.Runtime.Vst3Active ? Loc.T("已加载") : Loc.T("未加载"));
				var b5 = CreateActionButton(Loc.T("旁路插件并重建引擎"), FluentIconKind.VstPlugin, isAccent: true);
				b5.Click += async (_, _) =>
				{
					await RunRepairWithLiveProgressAsync("bypass_dsp_src");
					var res = await RunRepairWithLiveProgressAsync("reset_engine");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				_wizardStepActionPanel.Children.Add(b5);
				break;

			case 5:
			default:
				_wizardStepTitleText.Text = Loc.T("步骤 6：播放硬件测试提示音 & 重试当前曲目");
				_wizardStepDescText.Text = Loc.T("通过当前配置的音频后端直接向声卡发送一段清脆的 523Hz/784Hz 双音提示音，以验证物理链路是否通畅，随后重新触发播放。");
				var b6Tone = CreateActionButton(Loc.T("播放测试提示音"), FluentIconKind.Volume, isAccent: true);
				b6Tone.Click += OnPlayTestToneClick;
				var b6Reset = CreateActionButton(Loc.T("热重置音频引擎"), FluentIconKind.RefreshReset, isAccent: false);
				b6Reset.Click += async (_, _) =>
				{
					var res = await RunRepairWithLiveProgressAsync("reset_engine");
					ShowWizardFeedback(res.Detail, res.Success);
				};
				var b6Play = CreateActionButton(Loc.T("重试播放当前曲目"), FluentIconKind.Play, isAccent: false);
				b6Play.Click += OnRetryPlayCurrentTrackClick;
				_wizardStepActionPanel.Children.Add(b6Tone);
				_wizardStepActionPanel.Children.Add(b6Reset);
				_wizardStepActionPanel.Children.Add(b6Play);
				break;
		}
	}

	private async void OnPlayTestToneClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn) btn.IsEnabled = false;
		SetGlobalProgress(40, Loc.T("正在通过当前音频输出端点发送 523Hz/784Hz 测试提示音..."));
		try
		{
			var s = ViewModel?.CurrentAppSettings ?? ViewModel?.Settings ?? new AppSettings();
			string? devId = s.OutputMode == OutputMode.Asio ? null : s.OutputDeviceId;
			string? asio = s.OutputMode == OutputMode.Asio ? s.OutputDeviceId : null;
			var result = await AudioDiagnosticsService.PlayDiagnosticTestToneAsync(s.OutputMode, devId, asio, s.BufferMilliseconds);
			ShowWizardFeedback(result.Message, result.Success);
			SetGlobalProgress(100, result.Message);
		}
		finally
		{
			if (sender is Button b) b.IsEnabled = true;
		}
	}

	private async void OnRetryPlayCurrentTrackClick(object sender, RoutedEventArgs e)
	{
		SetGlobalProgress(45, Loc.T("正在检查当前曲目句柄并尝试重新唤醒播放..."));
		var engine = AudioEngine.Instance;
		if (engine == null)
		{
			ShowWizardFeedback(Loc.T("音频引擎尚未初始化。"), false);
			SetGlobalProgress(100, Loc.T("音频引擎尚未初始化。"));
			return;
		}

		await engine.SoftResetAudioEngineAsync(preserveCurrentTrackAndSeek: true);
		engine.Play();
		await Task.Delay(180);
		RefreshPlaybackStatusSnapshot();

		bool playing = engine.State == PlaybackState.Playing;
		string msg = playing
			? Loc.T("已重新唤醒当前输出端点并恢复正常播放！")
			: Loc.T("当前队列无待播曲目或设备仍被占用，请尝试切换为 WASAPI 共享模式或在曲库点选一首歌曲播放。");
		ShowWizardFeedback(msg, playing);
		SetGlobalProgress(100, msg);
	}

	private async void OnResetAllAudioConfirmedClick(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = Loc.T("重置所有音频设置"),
			Content = Loc.T("将恢复输出模式为 WASAPI 共享、使用系统默认设备、缓冲 200 ms、音量 80%，并清空升频与 EQ/VST3 覆盖开关。\n\n此操作不会影响您的音乐库目录、播放列表与外观主题配置，是否继续？"),
			PrimaryButtonText = Loc.T("确认重置"),
			CloseButtonText = Loc.T("取消"),
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot
		};
		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			await RunRepairWithLiveProgressAsync("reset_all_audio");
		}
	}

	private async Task<RepairActionResult> RunRepairWithLiveProgressAsync(string repairId)
	{
		var settingsService = ViewModel?.SettingsServiceInstance;
		var settings = ViewModel?.CurrentAppSettings ?? ViewModel?.Settings ?? new AppSettings();
		if (settingsService == null)
		{
			return new RepairActionResult(false, Loc.T("修复"), Loc.T("设置服务尚未就绪。"), Array.Empty<string>());
		}

		if (_repairStepsLogStack != null) _repairStepsLogStack.Children.Clear();
		SetGlobalProgress(10, Loc.T("正在初始化修复操作..."));

		var result = await AudioDiagnosticsService.ExecuteRepairActionAsync(
			repairId,
			settings,
			settingsService,
			AudioEngine.Instance,
			(progress01, msg) =>
			{
				DispatcherQueue?.TryEnqueue(() =>
				{
					SetGlobalProgress(progress01 * 100.0, msg);
					if (_repairStepsLogStack != null)
					{
						_repairStepsLogStack.Children.Add(new TextBlock
						{
							Text = $"[{DateTime.Now:HH:mm:ss}] {msg}",
							FontSize = 11.5,
							Opacity = 0.82,
							TextWrapping = TextWrapping.Wrap
						});
					}
				});
			});

		SyncOutputControlsAfterRepair();
		RefreshPlaybackStatusSnapshot();
		UpdateWizardStepUi();

		if (_repairSummaryBanner != null && _repairSummaryText != null)
		{
			(Color bg, Color fg) = GetSeverityColors(result.Success ? DiagnosticSeverity.Ok : DiagnosticSeverity.Error);
			_repairSummaryBanner.Background = new SolidColorBrush(bg);
			_repairSummaryText.Foreground = new SolidColorBrush(fg);
			_repairSummaryText.Text = $"{result.ActionTitle} · {result.Detail}";
			_repairSummaryBanner.Visibility = Visibility.Visible;
		}
		SetGlobalProgress(100, $"{result.ActionTitle}：{result.Detail}");
		return result;
	}

	private void SyncOutputControlsAfterRepair()
	{
		try
		{
			ViewModel?.SyncFromAudioRepair();
			_suppressOutputUiEvents = true;
			try
			{
				if (OutputModeCombo != null && ViewModel != null)
				{
					OutputModeCombo.SelectedIndex = (int)ViewModel.Settings.OutputMode;
				}
			}
			finally
			{
				_suppressOutputUiEvents = false;
			}
			RefreshDevices();
			SettingsSaved?.Invoke();
		}
		catch
		{
		}
	}

	private async void OnCopyDiagnosticsClick(object sender, RoutedEventArgs e)
	{
		RefreshPlaybackStatusSnapshot();
		if (_lastStatusReport == null) return;
		try
		{
			var pkg = new DataPackage();
			pkg.SetText(_lastStatusReport.CopyableReportText);
			Clipboard.SetContent(pkg);
			if (_copyDiagBtnLabel != null)
			{
				_copyDiagBtnLabel.Text = Loc.T("已复制诊断信息");
				await Task.Delay(1800);
				if (_copyDiagBtnLabel != null) _copyDiagBtnLabel.Text = Loc.T("复制诊断信息");
			}
		}
		catch
		{
		}
	}

	private void ShowWizardFeedback(string text, bool isSuccess)
	{
		if (_wizardFeedbackBanner == null || _wizardFeedbackText == null) return;
		(Color bg, Color fg) = GetSeverityColors(isSuccess ? DiagnosticSeverity.Ok : DiagnosticSeverity.Warning);
		_wizardFeedbackBanner.Background = new SolidColorBrush(bg);
		_wizardFeedbackText.Foreground = new SolidColorBrush(fg);
		_wizardFeedbackText.Text = text;
		_wizardFeedbackBanner.Visibility = Visibility.Visible;
	}

	private void SetGlobalProgress(double percent, string reasonText)
	{
		double clamped = Math.Clamp(percent, 0.0, 100.0);
		if (_diagGlobalProgressBar != null) _diagGlobalProgressBar.Value = clamped;
		if (_diagGlobalPercentText != null) _diagGlobalPercentText.Text = $"{Math.Round(clamped)}%";
		if (_diagGlobalReasonText != null) _diagGlobalReasonText.Text = reasonText;
	}

	private void StartCheckRepairLiveTimer()
	{
		try
		{
			if (_checkRepairLiveTimer == null)
			{
				var dq = DispatcherQueue ?? Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
				if (dq == null) return;
				_checkRepairLiveTimer = dq.CreateTimer();
				_checkRepairLiveTimer.Interval = TimeSpan.FromMilliseconds(2500);
				_checkRepairLiveTimer.Tick += (_, _) =>
				{
					if (_liveMonitorToggle?.IsOn == true && !_isDiagOperationRunning)
					{
						RefreshPlaybackStatusSnapshot();
					}
				};
			}
			if (!_checkRepairLiveTimer.IsRunning) _checkRepairLiveTimer.Start();
		}
		catch
		{
		}
	}

	private void StopCheckRepairLiveTimer()
	{
		try
		{
			_checkRepairLiveTimer?.Stop();
			_diagWorkCts?.Cancel();
		}
		catch
		{
		}
	}

	private void RefreshCheckRepairTheme(bool isDark)
	{
		_checkRepairIsDark = isDark;
		_lastChainSignature = null;
		_lastAnomaliesSignature = null;
		_lastStagesSignature = null;
		RefreshPlaybackStatusSnapshot();
	}

	private void ResumeCheckRepairOnNavigatedTo()
	{
		if (_audioCheckRepairCard == null) return;
		if (_liveMonitorToggle?.IsOn == true)
		{
			StartCheckRepairLiveTimer();
			RefreshPlaybackStatusSnapshot();
		}
	}

	private (Color bg, Color fg) GetSeverityColors(DiagnosticSeverity severity)
	{
		return severity switch
		{
			DiagnosticSeverity.Ok => (
				_checkRepairIsDark ? Color.FromArgb(48, 34, 197, 94) : Color.FromArgb(38, 22, 163, 74),
				_checkRepairIsDark ? Color.FromArgb(255, 134, 239, 172) : Color.FromArgb(255, 21, 128, 61)),
			DiagnosticSeverity.Error => (
				_checkRepairIsDark ? Color.FromArgb(52, 239, 68, 68) : Color.FromArgb(36, 220, 38, 38),
				_checkRepairIsDark ? Color.FromArgb(255, 252, 165, 165) : Color.FromArgb(255, 185, 28, 28)),
			_ => (
				_checkRepairIsDark ? Color.FromArgb(52, 245, 158, 11) : Color.FromArgb(38, 217, 119, 6),
				_checkRepairIsDark ? Color.FromArgb(255, 253, 224, 71) : Color.FromArgb(255, 180, 83, 9))
		};
	}
}
