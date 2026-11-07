using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FurinaPlayer.UI.Controls;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SkiaSharp.Views.Windows;
using SonicWave.Core.Models;
using WinRT;
using WinRT.Interop;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

namespace FurinaPlayer.UI.Pages;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.IUIElementOverrides")]
[WinRTExposedType(typeof(HomePageWinRTTypeDetails))]
public sealed class HomePage : Page, IComponentConnector
{
	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private interface IHomePage_Bindings
	{
		void Initialize();

		void Update();

		void StopTracking();

		void DisconnectUnloadedObject(int connectionId);
	}

	private interface IHomePage_BindingsScopeConnector
	{
		WeakReference Parent { get; set; }

		bool ContainsElement(int connectionId);

		void RegisterForElementConnection(int connectionId, IComponentConnector connector);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	private static class XamlBindingSetters
	{
		public static void Set_Microsoft_UI_Xaml_Controls_ColumnDefinition_Width(ColumnDefinition obj, GridLength value)
		{
			obj.Width = value;
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	[WinRTRuntimeClassName("Microsoft.UI.Xaml.IDataTemplateExtension")]
	[WinRTExposedType(typeof(HomePage_HomePage_obj6_BindingsWinRTTypeDetails))]
	private class HomePage_obj6_Bindings : IDataTemplateExtension, IDataTemplateComponent, IComponentConnector, IHomePage_Bindings
	{
		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		private class HomePage_obj6_BindingsTracking
		{
			private WeakReference<HomePage_obj6_Bindings> weakRefToBindingObj;

			public HomePage_obj6_BindingsTracking(HomePage_obj6_Bindings obj)
			{
				weakRefToBindingObj = new WeakReference<HomePage_obj6_Bindings>(obj);
			}

			public HomePage_obj6_Bindings TryGetBindingObject()
			{
				HomePage_obj6_Bindings target = null;
				if (weakRefToBindingObj != null)
				{
					weakRefToBindingObj.TryGetTarget(out target);
					if (target == null)
					{
						weakRefToBindingObj = null;
						ReleaseAllListeners();
					}
				}
				return target;
			}

			public void ReleaseAllListeners()
			{
			}
		}

		private Track dataRoot;

		private bool initialized;

		private const int NOT_PHASED = int.MinValue;

		private const int DATA_CHANGED = 1073741824;

		private bool removedDataContextHandler;

		private WeakReference obj6;

		private ColumnDefinition obj7;

		private HomePage_obj6_BindingsTracking bindingsTracking;

		public HomePage_obj6_Bindings()
		{
			bindingsTracking = new HomePage_obj6_BindingsTracking(this);
		}

		public void Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 6:
				obj6 = new WeakReference(target.As<Grid>());
				break;
			case 7:
				obj7 = target.As<ColumnDefinition>();
				break;
			}
		}

		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		public IComponentConnector GetBindingConnector(int connectionId, object target)
		{
			return null;
		}

		public void DataContextChangedHandler(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			if (SetDataRoot(args.NewValue))
			{
				Update();
			}
		}

		public bool ProcessBinding(uint phase)
		{
			throw new NotImplementedException();
		}

		public int ProcessBindings(ContainerContentChangingEventArgs args)
		{
			int nextPhase = -1;
			ProcessBindings(args.Item, args.ItemIndex, (int)args.Phase, out nextPhase);
			return nextPhase;
		}

		public void ResetTemplate()
		{
			Recycle();
		}

		public void ProcessBindings(object item, int itemIndex, int phase, out int nextPhase)
		{
			nextPhase = -1;
			if (phase == 0)
			{
				nextPhase = -1;
				SetDataRoot(item);
				if (!removedDataContextHandler)
				{
					removedDataContextHandler = true;
					Grid grid = obj6.Target as Grid;
					if (grid != null)
					{
						grid.DataContextChanged -= DataContextChangedHandler;
					}
				}
				initialized = true;
			}
			Update_(item.As<Track>(), 1 << phase);
		}

		public void Recycle()
		{
			bindingsTracking.ReleaseAllListeners();
		}

		public void Initialize()
		{
			if (!initialized)
			{
				Update();
			}
		}

		public void Update()
		{
			Update_(dataRoot, int.MinValue);
			initialized = true;
		}

		public void StopTracking()
		{
			bindingsTracking.ReleaseAllListeners();
			initialized = false;
		}

		public void DisconnectUnloadedObject(int connectionId)
		{
			throw new ArgumentException("No unloadable elements to disconnect.");
		}

		public bool SetDataRoot(object newDataRoot)
		{
			bindingsTracking.ReleaseAllListeners();
			if (newDataRoot != null)
			{
				dataRoot = newDataRoot.As<Track>();
				return true;
			}
			return false;
		}

		private void Update_(Track obj, int phase)
		{
			Update_FurinaPlayer_UI_Pages_HomePage_AlbumColumnWidth(AlbumColumnWidth, phase);
		}

		private void Update_FurinaPlayer_UI_Pages_HomePage_AlbumColumnWidth(GridLength obj, int phase)
		{
			if ((phase & -2147483647) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_ColumnDefinition_Width(obj7, obj);
			}
		}
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	[WinRTRuntimeClassName("Microsoft.UI.Xaml.Markup.IComponentConnector")]
	[WinRTExposedType(typeof(HomePage_HomePage_obj1_BindingsWinRTTypeDetails))]
	private class HomePage_obj1_Bindings : IComponentConnector, IHomePage_Bindings
	{
		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		private class HomePage_obj1_BindingsTracking
		{
			private WeakReference<HomePage_obj1_Bindings> weakRefToBindingObj;

			public HomePage_obj1_BindingsTracking(HomePage_obj1_Bindings obj)
			{
				weakRefToBindingObj = new WeakReference<HomePage_obj1_Bindings>(obj);
			}

			public HomePage_obj1_Bindings TryGetBindingObject()
			{
				HomePage_obj1_Bindings target = null;
				if (weakRefToBindingObj != null)
				{
					weakRefToBindingObj.TryGetTarget(out target);
					if (target == null)
					{
						weakRefToBindingObj = null;
						ReleaseAllListeners();
					}
				}
				return target;
			}

			public void ReleaseAllListeners()
			{
			}
		}

		private HomePage dataRoot;

		private bool initialized;

		private const int NOT_PHASED = int.MinValue;

		private const int DATA_CHANGED = 1073741824;

		private ColumnDefinition obj8;

		private HomePage_obj1_BindingsTracking bindingsTracking;

		public HomePage_obj1_Bindings()
		{
			bindingsTracking = new HomePage_obj1_BindingsTracking(this);
		}

		public void Connect(int connectionId, object target)
		{
			if (connectionId == 8)
			{
				obj8 = target.As<ColumnDefinition>();
			}
		}

		[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
		[DebuggerNonUserCode]
		public IComponentConnector GetBindingConnector(int connectionId, object target)
		{
			return null;
		}

		public void Initialize()
		{
			if (!initialized)
			{
				Update();
			}
		}

		public void Update()
		{
			Update_(dataRoot, int.MinValue);
			initialized = true;
		}

		public void StopTracking()
		{
			bindingsTracking.ReleaseAllListeners();
			initialized = false;
		}

		public void DisconnectUnloadedObject(int connectionId)
		{
			throw new ArgumentException("No unloadable elements to disconnect.");
		}

		public bool SetDataRoot(object newDataRoot)
		{
			bindingsTracking.ReleaseAllListeners();
			if (newDataRoot != null)
			{
				dataRoot = newDataRoot.As<HomePage>();
				return true;
			}
			return false;
		}

		public void Activated(object obj, WindowActivatedEventArgs data)
		{
			Initialize();
		}

		public void Loading(FrameworkElement src, object data)
		{
			Initialize();
		}

		private void Update_(HomePage obj, int phase)
		{
			Update_FurinaPlayer_UI_Pages_HomePage_AlbumColumnWidth(AlbumColumnWidth, phase);
		}

		private void Update_FurinaPlayer_UI_Pages_HomePage_AlbumColumnWidth(GridLength obj, int phase)
		{
			if ((phase & -2147483647) != 0)
			{
				XamlBindingSetters.Set_Microsoft_UI_Xaml_Controls_ColumnDefinition_Width(obj8, obj);
			}
		}
	}

	private sealed class ListItemGlassState
	{
		public bool IsHovered { get; set; }
		public bool IsPressed { get; set; }
		public Point NormalizedPointer { get; set; } = new Point(0.25, 0.2);
		public bool Hooked { get; set; }
	}

	private readonly ConditionalWeakTable<SelectorItem, ListItemGlassState> _listItemGlassStates = new();

	private SKXamlCanvas? _ambientCausticCanvas;

	private TextBox? _searchBox;

	private Button? _scanFolderBtn;

	private Button? _importFilesBtn;

	private GridView? _albumStripGrid;

	private GridView? _albumMatrixGrid;

	private bool _alignDragging;

	private double _alignStartX;

	private double _alignStartWidth;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid PageRoot;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid HeaderGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView TracksList;

	private ListView SongsList => TracksList;

	private TextBox? SearchBox => _searchBox;

	private Button? _albumViewToggleBtn;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private IHomePage_Bindings Bindings;

	public HomeViewModel ViewModel { get; set; }

	public static GridLength AlbumColumnWidth { get; set; } = new GridLength(140.0);

	public HomePage()
	{
		InitializeComponent();
		ApplyHomeLiquidGlass();
		Loaded += (s, e) =>
		{
			FluentIconFactory.UpgradeButtonsInVisualTree(this);
			// EnsureStreamingButton();
			ApplyHomeLiquidGlass();
		};
		ActualThemeChanged += (s, e) => ApplyHomeLiquidGlass();
		SizeChanged += (s, e) => _ambientCausticCanvas?.Invalidate();
	}

	public HomePage(HomeViewModel viewModel)
		: this()
	{
		ViewModel = viewModel;
		DataContext = viewModel;
	}

	protected override async void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		if (e.Parameter is HomeViewModel homeViewModel)
		{
			ViewModel = homeViewModel;
			DataContext = homeViewModel;
		}
		FluentIconFactory.UpgradeButtonsInVisualTree(this);
			// EnsureStreamingButton();
		ApplyHomeLiquidGlass();
		UpdateAlbumToggleBtnIcon();
		SyncAlbumColWidthResource();
		await ViewModel.InitializeAsync();
		UpdateAlbumToggleBtnIcon();
		ApplyHomeLiquidGlass();
	}

	private void ApplyHomeLiquidGlass()
	{
		try
		{
			bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
			Background = new SolidColorBrush(Colors.Transparent);

			if (PageRoot != null)
			{
				PageRoot.Background = new SolidColorBrush(Colors.Transparent);

				if (false && _ambientCausticCanvas == null)
				{
					_ambientCausticCanvas = LiquidGlassEngine.CreateAmbientCausticCanvas(() => LiquidGlassEngine.IsElementDarkTheme(this));
					Grid.SetRow(_ambientCausticCanvas, 0);
					Grid.SetColumn(_ambientCausticCanvas, 0);
					Grid.SetRowSpan(_ambientCausticCanvas, 12);
					Grid.SetColumnSpan(_ambientCausticCanvas, 12);
					Canvas.SetZIndex(_ambientCausticCanvas, -10);
				}

				if (_ambientCausticCanvas != null && !PageRoot.Children.Contains(_ambientCausticCanvas))
				{
					PageRoot.Children.Insert(0, _ambientCausticCanvas);
				}
				else if (_ambientCausticCanvas != null && PageRoot.Children.IndexOf(_ambientCausticCanvas) != 0)
				{
					PageRoot.Children.Remove(_ambientCausticCanvas);
					PageRoot.Children.Insert(0, _ambientCausticCanvas);
				}

				_ambientCausticCanvas?.Invalidate();
			}

			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(this, isDark);

			if (_searchBox != null)
			{
				if (_searchBox.CornerRadius == default)
				{
					_searchBox.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(_searchBox, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}

			if (_scanFolderBtn != null)
			{
				if (_scanFolderBtn.CornerRadius == default)
				{
					_scanFolderBtn.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(_scanFolderBtn, LiquidGlassVariant.AccentLensed, isDark, enablePointerTracking: true);
			}

			if (_importFilesBtn != null)
			{
				if (_importFilesBtn.CornerRadius == default)
				{
					_importFilesBtn.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(_importFilesBtn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}

			if (_albumViewToggleBtn != null)
			{
				if (_albumViewToggleBtn.CornerRadius == default)
				{
					_albumViewToggleBtn.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(_albumViewToggleBtn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}

			if (HeaderGrid?.Parent is Border headerBorder)
			{
				LiquidGlassEngine.AttachInteractiveLiquidGlass(headerBorder, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (HeaderGrid != null)
			{
				LiquidGlassEngine.AttachInteractiveLiquidGlass(HeaderGrid, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}

			if (TracksList != null)
			{
				TracksList.Background = new SolidColorBrush(Colors.Transparent);
				if (TracksList.Parent is Border songsCardBorder)
				{
					LiquidGlassEngine.AttachInteractiveLiquidGlass(songsCardBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
				}
				else if (TracksList.Parent is Grid parentGrid && parentGrid.Parent is Border outerSongsBorder)
				{
					LiquidGlassEngine.AttachInteractiveLiquidGlass(outerSongsBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
				}
			}

			if (_albumStripGrid?.Parent is Border albumStripBorder)
			{
				LiquidGlassEngine.AttachInteractiveLiquidGlass(albumStripBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
			}

			if (_albumMatrixGrid?.Parent is Border albumMatrixBorder)
			{
				LiquidGlassEngine.AttachInteractiveLiquidGlass(albumMatrixBorder, LiquidGlassVariant.Regular, isDark, enablePointerTracking: true);
			}

			UpgradeHomeVisualTreeNodes(this, isDark);
			RefreshRealizedSongListContainers();
		}
		catch
		{
		}
	}

	private void UpgradeHomeVisualTreeNodes(DependencyObject root, bool isDark)
	{
		if (root == null) return;

		int count = VisualTreeHelper.GetChildrenCount(root);
		for (int i = 0; i < count; i++)
		{
			DependencyObject child = VisualTreeHelper.GetChild(root, i);
			if (child is AutoSuggestBox suggestBox)
			{
				if (suggestBox.CornerRadius == default)
				{
					suggestBox.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(suggestBox, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is TextBox textBox && textBox != _searchBox)
			{
				if (textBox.CornerRadius == default)
				{
					textBox.CornerRadius = new CornerRadius(12);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(textBox, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is Button btn && btn != _scanFolderBtn && btn != _importFilesBtn && btn != _albumViewToggleBtn)
			{
				if (btn.CornerRadius == default)
				{
					btn.CornerRadius = new CornerRadius(10);
				}
				LiquidGlassEngine.AttachInteractiveLiquidGlass(btn, LiquidGlassVariant.Clear, isDark, enablePointerTracking: true);
			}
			else if (child is Border border && border.CornerRadius.TopLeft >= 10 && border.ActualWidth > 48 && border.ActualHeight > 28)
			{
				LiquidGlassVariant variant = border.CornerRadius.TopLeft >= 14 ? LiquidGlassVariant.Regular : LiquidGlassVariant.Clear;
				LiquidGlassEngine.AttachInteractiveLiquidGlass(border, variant, isDark, enablePointerTracking: true);
			}

			UpgradeHomeVisualTreeNodes(child, isDark);
		}
	}

	private void OnTracksListContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.InRecycleQueue) return;

		if (args.ItemContainer is ListViewItem item)
		{
			AttachSelectorItemLiquidGlass(item);
			if (item.Content is Grid content && content.ColumnDefinitions.Count > 4)
			{
				content.ColumnDefinitions[4].Width = AlbumColumnWidth;
			}
		}
	}

	private void OnAlbumGridContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
	{
		if (args.InRecycleQueue) return;

		if (args.ItemContainer is GridViewItem item)
		{
			AttachSelectorItemLiquidGlass(item);
			bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
			LiquidGlassEngine.UpgradeVisualTreeToLiquidGlass(item, isDark);
		}
	}

	private void OnTracksListSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		RefreshRealizedSongListContainers();
	}

	private void RefreshRealizedSongListContainers()
	{
		if (TracksList?.ItemsPanelRoot == null) return;
		foreach (UIElement child in TracksList.ItemsPanelRoot.Children)
		{
			if (child is ListViewItem item)
			{
				AttachSelectorItemLiquidGlass(item);
			}
		}
	}

	private void AttachSelectorItemLiquidGlass(SelectorItem item)
	{
		var state = _listItemGlassStates.GetOrCreateValue(item);
		if (item.CornerRadius == default)
		{
			item.CornerRadius = new CornerRadius(10);
		}
		if (item is ListViewItem && item.Margin == default)
		{
			item.Margin = new Thickness(4, 2, 4, 2);
		}

		UpdateSelectorItemVisuals(item, state);

		if (state.Hooked) return;
		state.Hooked = true;

		item.PointerEntered += (s, e) =>
		{
			state.IsHovered = true;
			if (s is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
			{
				var pt = e.GetCurrentPoint(fe).Position;
				state.NormalizedPointer = new Point(
					Math.Clamp(pt.X / fe.ActualWidth, 0.0, 1.0),
					Math.Clamp(pt.Y / fe.ActualHeight, 0.0, 1.0));
			}
			UpdateSelectorItemVisuals(item, state);
		};

		item.PointerMoved += (s, e) =>
		{
			if (!state.IsHovered) state.IsHovered = true;
			if (s is FrameworkElement fe && fe.ActualWidth > 0 && fe.ActualHeight > 0)
			{
				var pt = e.GetCurrentPoint(fe).Position;
				state.NormalizedPointer = new Point(
					Math.Clamp(pt.X / fe.ActualWidth, 0.0, 1.0),
					Math.Clamp(pt.Y / fe.ActualHeight, 0.0, 1.0));
				UpdateSelectorItemVisuals(item, state);
			}
		};

		item.PointerExited += (s, e) =>
		{
			state.IsHovered = false;
			state.IsPressed = false;
			state.NormalizedPointer = new Point(0.25, 0.2);
			UpdateSelectorItemVisuals(item, state);
		};

		item.PointerPressed += (s, e) =>
		{
			state.IsPressed = true;
			UpdateSelectorItemVisuals(item, state);
		};

		item.PointerReleased += (s, e) =>
		{
			state.IsPressed = false;
			UpdateSelectorItemVisuals(item, state);
		};
	}

	private void UpdateSelectorItemVisuals(SelectorItem item, ListItemGlassState state)
	{
		bool isDark = LiquidGlassEngine.IsElementDarkTheme(this);
		bool isSelected = item.IsSelected;

		if (!state.IsHovered && !state.IsPressed && !isSelected)
		{
			item.Background = new SolidColorBrush(Colors.Transparent);
			item.BorderBrush = new SolidColorBrush(Colors.Transparent);
			item.BorderThickness = new Thickness(1);
			return;
		}

		var variant = isSelected ? LiquidGlassVariant.AccentLensed : LiquidGlassVariant.Clear;
		double opacityScale = state.IsPressed ? 0.90 : (isSelected ? 0.86 : 0.72);
		item.Background = LiquidGlassEngine.CreateSurfaceBrush(variant, isDark, opacityScale, state.NormalizedPointer);
		item.BorderBrush = LiquidGlassEngine.CreateSpecularRimBrush(variant, isDark, state.IsHovered, isSelected, state.NormalizedPointer);
		item.BorderThickness = new Thickness(1);
	}

	private void UpdateAlbumToggleBtnIcon()
	{
		if (_albumViewToggleBtn != null)
		{
			string label = (ViewModel != null && !ViewModel.AlbumGridView) ? "网格视图" : "列表视图";
			FluentIconFactory.SetButtonFluentIcon(_albumViewToggleBtn, FluentIconKind.AlbumGrid, label, 16);
			_albumViewToggleBtn.Tag = "__fluent_upgraded";
		}
	}

	private void SyncAlbumColWidthResource()
	{
		try
		{
			if (ViewModel == null)
			{
				return;
			}
			GridLength width = (AlbumColumnWidth = new GridLength(Math.Clamp(ViewModel.AlbumColumnWidth, 80.0, 400.0)));
			if (HeaderGrid != null && HeaderGrid.ColumnDefinitions.Count > 4)
			{
				HeaderGrid.ColumnDefinitions[4].Width = width;
			}
			if (!(TracksList != null) || !(TracksList.ItemsPanelRoot != null))
			{
				return;
			}
			foreach (UIElement child in TracksList.ItemsPanelRoot.Children)
			{
				if (child is ListViewItem { Content: Grid content } && content.ColumnDefinitions.Count > 4)
				{
					content.ColumnDefinitions[4].Width = width;
				}
			}
		}
		catch
		{
		}
	}

	private async void OnScanFolderClick(object sender, RoutedEventArgs e)
	{
		FolderPicker folderPicker = new FolderPicker
		{
			FileTypeFilter = { "*" }
		};
		InitializeWithWindow(folderPicker);
		StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
		if (storageFolder != null)
		{
			await ViewModel.ScanFolderCommand.ExecuteAsync(storageFolder.Path);
		}
	}

	private async void OnImportFilesClick(object sender, RoutedEventArgs e)
	{
		FileOpenPicker fileOpenPicker = new FileOpenPicker
		{
			FileTypeFilter = 
			{
				".flac", ".wav", ".mp3", ".ape", ".dsf", ".dff", ".aiff", ".wv", ".m4a", ".ogg",
				".opus", ".wma"
			}
		};
		InitializeWithWindow(fileOpenPicker);
		IReadOnlyList<StorageFile> readOnlyList = await fileOpenPicker.PickMultipleFilesAsync();
		if (readOnlyList.Count > 0)
		{
			string[] array = new string[readOnlyList.Count];
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				array[i] = readOnlyList[i].Path;
			}
			await ViewModel.ImportPathsAsync(array);
		}
	}

	private async void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Enter)
		{
			await ViewModel.SearchCommand.ExecuteAsync(null);
		}
	}

	private void OnTrackClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Track track)
		{
			ViewModel.PlayTrack(track);
		}
	}

	private static void InitializeWithWindow(object picker)
	{
		nint windowHandle = WindowNative.GetWindowHandle(WindowManager.CurrentWindow);
		WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
	}

	private void OnAlbumClick(object sender, ItemClickEventArgs e)
	{
		if (e.ClickedItem is Album album)
		{
			ViewModel.SetAlbumFilter(album.Name);
		}
	}

	private void OnAlbumViewToggleClick(object sender, RoutedEventArgs e)
	{
		ViewModel.ToggleAlbumViewCommand.Execute(null);
		UpdateAlbumToggleBtnIcon();
	}

	private void OnTrackRightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		object obj = (e.OriginalSource as FrameworkElement)?.DataContext;
		Track t = obj as Track;
		if (t != null)
		{
			ShowRemoveMenu((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender), "从库中移除（不删除本地文件）", async () =>
			{
				await ViewModel.RemoveTrackCommand.ExecuteAsync(t);
			});
		}
	}

	private void OnAlbumRightTapped(object sender, RightTappedRoutedEventArgs e)
	{
		object obj = (e.OriginalSource as FrameworkElement)?.DataContext;
		Album a = obj as Album;
		if (a != null)
		{
			ShowRemoveMenu((FrameworkElement)sender, e.GetPosition((FrameworkElement)sender), "从库中移除整张专辑（不删除本地文件）", async () =>
			{
				await ViewModel.RemoveAlbumCommand.ExecuteAsync(a);
			});
		}
	}

	private void ShowRemoveMenu(FrameworkElement target, Point position, string text, Func<Task> action)
	{
		MenuFlyout menuFlyout = new MenuFlyout();
		MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
		{
			Text = text,
			Icon = new FontIcon
			{
				Glyph = "\ue74d"
			}
		};
		menuFlyoutItem.Click += async (object _, RoutedEventArgs _) =>
		{
			if (await new ContentDialog
			{
				Title = "确认移除",
				Content = "仅从音乐库中移除，不会删除本地文件。确定继续？",
				PrimaryButtonText = "移除",
				CloseButtonText = "取消",
				XamlRoot = XamlRoot,
				DefaultButton = ContentDialogButton.Close
			}.ShowAsync() == ContentDialogResult.Primary)
			{
				await action();
			}
		};
		menuFlyout.Items.Add(menuFlyoutItem);
		menuFlyout.ShowAt(target, position);
	}

	private void OnTrackDragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
	{
		ViewModel.SyncAfterDragReorder();
	}

	private void OnAlignBarPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_alignDragging = true;
		_alignStartX = e.GetCurrentPoint(PageRoot).Position.X;
		_alignStartWidth = ViewModel.AlbumColumnWidth;
		if (sender is UIElement uIElement)
		{
			uIElement.CapturePointer(e.Pointer);
		}
	}

	private void OnAlignBarPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_alignDragging)
		{
			double x = e.GetCurrentPoint(PageRoot).Position.X;
			ViewModel.AlbumColumnWidth = _alignStartWidth + (x - _alignStartX);
			SyncAlbumColWidthResource();
		}
	}

	private void OnAlignBarPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		_alignDragging = false;
		if (sender is UIElement { PointerCaptures: not null } uIElement && uIElement.PointerCaptures.Contains(e.Pointer))
		{
			uIElement.ReleasePointerCapture(e.Pointer);
		}
	}

	private void OnClearFilterClick(object sender, RoutedEventArgs e)
	{
		ViewModel.ClearAlbumFilterCommand.Execute(null);
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("ms-appx:///FurinaPlayer.UI/Pages/HomePage.xaml");
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
			PageRoot = target.As<Grid>();
			break;
		case 3:
			HeaderGrid = target.As<Grid>();
			break;
		case 4:
			TracksList = target.As<ListView>();
			TracksList.ItemClick += OnTrackClick;
			TracksList.DragItemsCompleted += OnTrackDragCompleted;
			TracksList.RightTapped += OnTrackRightTapped;
			TracksList.ContainerContentChanging += OnTracksListContainerContentChanging;
			TracksList.SelectionChanged += OnTracksListSelectionChanged;
			break;
		case 9:
		{
			Grid grid = target.As<Grid>();
			grid.PointerPressed += OnAlignBarPointerPressed;
			grid.PointerMoved += OnAlignBarPointerMoved;
			grid.PointerReleased += OnAlignBarPointerReleased;
			grid.PointerCaptureLost += OnAlignBarPointerReleased;
			break;
		}
		case 10:
			target.As<HyperlinkButton>().Click += OnClearFilterClick;
			break;
		case 11:
			_albumStripGrid = target.As<GridView>();
			_albumStripGrid.ItemClick += OnAlbumClick;
			_albumStripGrid.ContainerContentChanging += OnAlbumGridContainerContentChanging;
			break;
		case 14:
		{
			GridView gridView = target.As<GridView>();
			_albumMatrixGrid = gridView;
			gridView.ItemClick += OnAlbumClick;
			gridView.RightTapped += OnAlbumRightTapped;
			gridView.ContainerContentChanging += OnAlbumGridContainerContentChanging;
			break;
		}
		case 17:
			_albumViewToggleBtn = target.As<Button>();
			_albumViewToggleBtn.Click += OnAlbumViewToggleClick;
			break;
		case 18:
			_searchBox = target.As<TextBox>();
			_searchBox.KeyDown += OnSearchKeyDown;
			break;
		case 19:
			_scanFolderBtn = target.As<Button>();
			_scanFolderBtn.Click += OnScanFolderClick;
			break;
		case 20:
			_importFilesBtn = target.As<Button>();
			_importFilesBtn.Click += OnImportFilesClick;
			break;
		}
		_contentLoaded = true;
	}

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	[DebuggerNonUserCode]
	public IComponentConnector GetBindingConnector(int connectionId, object target)
	{
		IComponentConnector result = null;
		switch (connectionId)
		{
		case 1:
		{
			Page page = (Page)target;
			HomePage_obj1_Bindings homePage_obj1_Bindings = new HomePage_obj1_Bindings();
			result = homePage_obj1_Bindings;
			homePage_obj1_Bindings.SetDataRoot(this);
			Bindings = homePage_obj1_Bindings;
			page.Loading += homePage_obj1_Bindings.Loading;
			break;
		}
		case 6:
		{
			Grid grid = (Grid)target;
			HomePage_obj6_Bindings homePage_obj6_Bindings = new HomePage_obj6_Bindings();
			result = homePage_obj6_Bindings;
			homePage_obj6_Bindings.SetDataRoot(grid.DataContext);
			grid.DataContextChanged += homePage_obj6_Bindings.DataContextChangedHandler;
			DataTemplate.SetExtensionInstance(grid, homePage_obj6_Bindings);
			XamlBindingHelper.SetDataTemplateComponent(grid, homePage_obj6_Bindings);
			break;
		}
		}
		return result;
	}

	private Button? _streamingBtn;

	private void EnsureStreamingButton()
	{
	}

	private async void OnStreamingBtnClick(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = "FurinaPlayer 流媒体音乐中心",
			CloseButtonText = "关闭",
			XamlRoot = this.XamlRoot,
			CornerRadius = new CornerRadius(16),
			DefaultButton = ContentDialogButton.Primary
		};

		var rootSp = new StackPanel { Width = 520, Spacing = 14 };

		var tipTb = new TextBlock
		{
			Text = "支持输入网络音频直链（HTTP/HTTPS/HLS/M3U8）或直接搜索在线音乐，直通 FurinaPlayer 高保真 DSP 引擎播放。",
			FontSize = 12,
			Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)),
			TextWrapping = TextWrapping.Wrap
		};
		rootSp.Children.Add(tipTb);

		var pivot = new Pivot();

		// 1. 在线搜索
		var searchTab = new PivotItem { Header = "在线曲库搜索" };
		var searchSp = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };

		var searchBoxGrid = new Grid();
		searchBoxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });
		searchBoxGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var queryTb = new TextBox
		{
			PlaceholderText = "输入歌曲名、歌手，例如：昔涟、原神、Furina",
			CornerRadius = new CornerRadius(8)
		};
		Grid.SetColumn(queryTb, 0);
		searchBoxGrid.Children.Add(queryTb);

		var searchDoBtn = new Button
		{
			Content = "搜索",
			Margin = new Thickness(8, 0, 0, 0),
			CornerRadius = new CornerRadius(8)
		};
		Grid.SetColumn(searchDoBtn, 1);
		searchBoxGrid.Children.Add(searchDoBtn);
		searchSp.Children.Add(searchBoxGrid);

		var resultsListView = new ListView
		{
			Height = 260,
			CornerRadius = new CornerRadius(8),
			SelectionMode = ListViewSelectionMode.Single
		};
		searchSp.Children.Add(resultsListView);

		searchDoBtn.Click += async (s2, e2) =>
		{
			string q = queryTb.Text.Trim();
			if (string.IsNullOrEmpty(q)) return;
			searchDoBtn.IsEnabled = false;
			searchDoBtn.Content = "搜索中...";
			try
			{
				var list = await ViewModel.SearchStreamingTracksAsync(q);
				resultsListView.Items.Clear();
				foreach (var item in list)
				{
					var spItem = new StackPanel { Margin = new Thickness(4, 4, 4, 4) };
					spItem.Children.Add(new TextBlock { Text = item.Title, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
					spItem.Children.Add(new TextBlock { Text = $"{item.Artist} · {item.Album} · {item.Duration:mm\\:ss}", FontSize = 11, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) });

					var btnItem = new Button
					{
						Content = spItem,
						Tag = item,
						HorizontalAlignment = HorizontalAlignment.Stretch,
						HorizontalContentAlignment = HorizontalAlignment.Left,
						Background = new SolidColorBrush(Colors.Transparent),
						BorderThickness = new Thickness(0)
					};
					btnItem.Click += async (s3, e3) =>
					{
						if (s3 is Button b && b.Tag is SonicWave.Core.Services.StreamingTrackInfo trackInfo)
						{
							await ViewModel.PlayStreamingTrackAsync(trackInfo);
							dialog.Hide();
						}
					};
					resultsListView.Items.Add(btnItem);
				}
				if (list.Count == 0)
				{
					resultsListView.Items.Add(new TextBlock { Text = "未找到匹配在线曲目", Margin = new Thickness(10), Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 116, 139)) });
				}
			}
			finally
			{
				searchDoBtn.IsEnabled = true;
				searchDoBtn.Content = "搜索";
			}
		};
		searchTab.Content = searchSp;
		pivot.Items.Add(searchTab);

		// 2. 网络直链
		var urlTab = new PivotItem { Header = "网络音频直链" };
		var urlSp = new StackPanel { Spacing = 10, Margin = new Thickness(0, 10, 0, 0) };

		var urlInput = new TextBox
		{
			Header = "音频 URL 直链 (HTTP / HTTPS / M3U8 / RTSP)",
			PlaceholderText = "https://example.com/audio.mp3 或广播流 URL",
			CornerRadius = new CornerRadius(8)
		};
		urlSp.Children.Add(urlInput);

		var titleInput = new TextBox
		{
			Header = "曲目名称 (可选)",
			PlaceholderText = "例如：网络实时广播",
			CornerRadius = new CornerRadius(8)
		};
		urlSp.Children.Add(titleInput);

		var playUrlBtn = new Button
		{
			Content = "立即播放网络直链",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			CornerRadius = new CornerRadius(8),
			Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 99, 102, 241)),
			Foreground = new SolidColorBrush(Colors.White),
			Margin = new Thickness(0, 8, 0, 0)
		};
		playUrlBtn.Click += async (s2, e2) =>
		{
			string url = urlInput.Text.Trim();
			if (!string.IsNullOrEmpty(url))
			{
				await ViewModel.PlayStreamUrlAsync(url, titleInput.Text.Trim());
				dialog.Hide();
			}
		};
		urlSp.Children.Add(playUrlBtn);

		urlTab.Content = urlSp;
		pivot.Items.Add(urlTab);

		rootSp.Children.Add(pivot);
		dialog.Content = rootSp;

		try
		{
			await dialog.ShowAsync();
		}
		catch
		{
		}
	}
}
