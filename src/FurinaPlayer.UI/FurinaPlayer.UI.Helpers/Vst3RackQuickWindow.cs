using System;
using FurinaPlayer.Core.Services;
using FurinaPlayer.UI.Pages;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace FurinaPlayer.UI.Helpers;

/// <summary>
/// Compact secondary popup window for quick access to the VST3 Rack and Plugin Manager
/// from PowerPoint Presentation Mode without leaving the active playback stage.
/// </summary>
public sealed class Vst3RackQuickWindow
{
	private static Vst3RackQuickWindow? s_instance;

	private readonly Window _window;
	private readonly DspPage _dspPage;

	private Vst3RackQuickWindow(DspViewModel viewModel)
	{
		_window = new Window
		{
			Title = Loc.T("FurinaPlayer — VST3 效果机架与插件管理"),
			SystemBackdrop = new MicaBackdrop()
		};

		_dspPage = new DspPage(viewModel);
		ElementTheme activeTheme = ElementTheme.Default;
		if (WindowManager.CurrentWindow?.Content is FrameworkElement mainRoot)
		{
			activeTheme = mainRoot.ActualTheme;
		}
		_dspPage.RequestedTheme = activeTheme;

		var root = new Grid
		{
			RequestedTheme = activeTheme
		};
		root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

		var topBar = new Border
		{
			Padding = new Thickness(18, 10, 14, 10),
			BorderThickness = new Thickness(0, 0, 0, 1),
			BorderBrush = new SolidColorBrush(activeTheme == ElementTheme.Dark
				? Windows.UI.Color.FromArgb(40, 255, 255, 255)
				: Windows.UI.Color.FromArgb(24, 15, 23, 42))
		};
		var topGrid = new Grid();
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var titleStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
		titleStack.Children.Add(new FontIcon { Glyph = "\uE9E9", FontSize = 15 });
		titleStack.Children.Add(new TextBlock
		{
			Text = Loc.T("VST3 效果机架 / 插件管理快捷窗口"),
			FontSize = 13,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center
		});
		Grid.SetColumn(titleStack, 0);
		topGrid.Children.Add(titleStack);

		var closeBtn = new Button
		{
			Content = Loc.T("关闭窗口"),
			FontSize = 12,
			Padding = new Thickness(12, 4, 12, 4),
			CornerRadius = new CornerRadius(6)
		};
		closeBtn.Click += (_, _) => _window.Close();
		Grid.SetColumn(closeBtn, 1);
		topGrid.Children.Add(closeBtn);
		topBar.Child = topGrid;

		Grid.SetRow(topBar, 0);
		root.Children.Add(topBar);

		FrameworkElement workbench = _dspPage.CreateVst3RackWorkbench();
		Grid.SetRow(workbench, 1);
		root.Children.Add(workbench);

		_window.Content = root;
		_window.ExtendsContentIntoTitleBar = false;

		try
		{
			var appWindow = _window.AppWindow;
			appWindow.Resize(new SizeInt32(940, 680));
			if (WindowManager.CurrentWindow != null)
			{
				var mainAppWin = WindowManager.CurrentWindow.AppWindow;
				var pos = mainAppWin.Position;
				var size = mainAppWin.Size;
				int x = pos.X + Math.Max(0, (size.Width - 940) / 2);
				int y = pos.Y + Math.Max(0, (size.Height - 680) / 2);
				appWindow.Move(new PointInt32(x, y));
			}
			else
			{
				RectInt32 workArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
				appWindow.Move(new PointInt32(
					workArea.X + Math.Max(0, (workArea.Width - 940) / 2),
					workArea.Y + Math.Max(0, (workArea.Height - 680) / 2)));
			}
		}
		catch
		{
		}

		_window.Closed += (_, _) =>
		{
			if (ReferenceEquals(s_instance, this))
			{
				s_instance = null;
			}
		};
	}

	public static void ShowWindow(string focusSection = "rack")
	{
		var dispatcher = WindowManager.CurrentWindow?.DispatcherQueue;
		if (dispatcher != null && !dispatcher.HasThreadAccess)
		{
			dispatcher.TryEnqueue(() => ShowWindow(focusSection));
			return;
		}

		if (s_instance == null)
		{
			DspViewModel vm = WindowManager.SharedDspViewModel
				?? new DspViewModel(new FurinaPlayer.Audio.AudioEngine(), new FurinaPlayer.Core.Services.SettingsService());
			s_instance = new Vst3RackQuickWindow(vm);
		}
		else
		{
			try
			{
				if (WindowManager.CurrentWindow?.Content is FrameworkElement mainRoot &&
				    s_instance._window.Content is FrameworkElement quickRoot)
				{
					s_instance._dspPage.RequestedTheme = mainRoot.ActualTheme;
					quickRoot.RequestedTheme = mainRoot.ActualTheme;
				}
			}
			catch
			{
			}
		}

		s_instance._window.Activate();
		s_instance._dspPage.FocusVst3Section(focusSection);
	}
}
