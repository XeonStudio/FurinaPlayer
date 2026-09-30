using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FurinaPlayer.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
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

	private bool _alignDragging;

	private double _alignStartX;

	private double _alignStartWidth;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid PageRoot;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private Grid HeaderGrid;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private ListView TracksList;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private bool _contentLoaded;

	[GeneratedCode("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2409")]
	private IHomePage_Bindings Bindings;

	public HomeViewModel ViewModel { get; set; }

	public static GridLength AlbumColumnWidth { get; set; } = new GridLength(140.0);

	public HomePage()
	{
		InitializeComponent();
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
		SyncAlbumColWidthResource();
		await ViewModel.InitializeAsync();
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
			target.As<GridView>().ItemClick += OnAlbumClick;
			break;
		case 14:
		{
			GridView gridView = target.As<GridView>();
			gridView.ItemClick += OnAlbumClick;
			gridView.RightTapped += OnAlbumRightTapped;
			break;
		}
		case 17:
			target.As<Button>().Click += OnAlbumViewToggleClick;
			break;
		case 18:
			target.As<TextBox>().KeyDown += OnSearchKeyDown;
			break;
		case 19:
			target.As<Button>().Click += OnScanFolderClick;
			break;
		case 20:
			target.As<Button>().Click += OnImportFilesClick;
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
}
