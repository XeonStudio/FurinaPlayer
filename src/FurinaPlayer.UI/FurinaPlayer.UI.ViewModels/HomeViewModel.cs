using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using SonicWave.Audio;
using SonicWave.Core.Models;
using SonicWave.Core.Services;
using WinRT;

namespace FurinaPlayer.UI.ViewModels;

[WinRTRuntimeClassName("Microsoft.UI.Xaml.Data.INotifyPropertyChanged")]
[WinRTExposedType(typeof(HomeViewModelWinRTTypeDetails))]
public class HomeViewModel : ObservableObject
{
	private readonly LibraryService _library;

	private readonly AudioEngine _engine;

	private readonly SettingsService? _settingsService;

	[ObservableProperty]
	private double albumColumnWidth = 140.0;

	[ObservableProperty]
	private ObservableCollection<Track> tracks = new ObservableCollection<Track>();

	[ObservableProperty]
	private ObservableCollection<Track> visibleTracks = new ObservableCollection<Track>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasAlbumFilter")]
	[NotifyPropertyChangedFor("AlbumFilterText")]
	[NotifyPropertyChangedFor("AlbumFilterVisibility")]
	private string? albumFilter;

	[ObservableProperty]
	private ObservableCollection<Album> albums = new ObservableCollection<Album>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("AlbumGridViewVisibility")]
	[NotifyPropertyChangedFor("AlbumListViewVisibility")]
	private bool albumGridView = true;

	[ObservableProperty]
	private ObservableCollection<Track> recentTracks = new ObservableCollection<Track>();

	[ObservableProperty]
	private string searchText = string.Empty;

	[ObservableProperty]
	private bool isScanning;

	[ObservableProperty]
	private string progressText = string.Empty;

	[ObservableProperty]
	private string statusText = string.Empty;

	[ObservableProperty]
	private Track? selectedTrack;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? toggleAlbumViewCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? refreshCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand? searchCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand<string>? scanFolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand? clearAlbumFilterCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private RelayCommand<Track?>? playTrackCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand<Track?>? removeTrackCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand<Album?>? removeAlbumCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	private AsyncRelayCommand<Track?>? toggleFavoriteCommand;

	public bool HasAlbumFilter => !string.IsNullOrEmpty(AlbumFilter);

	public string AlbumFilterText
	{
		get
		{
			if (!HasAlbumFilter)
			{
				return string.Empty;
			}
			return "专辑：" + AlbumFilter;
		}
	}

	public Visibility AlbumFilterVisibility
	{
		get
		{
			if (!HasAlbumFilter)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility AlbumGridViewVisibility
	{
		get
		{
			if (!AlbumGridView)
			{
				return Visibility.Collapsed;
			}
			return Visibility.Visible;
		}
	}

	public Visibility AlbumListViewVisibility
	{
		get
		{
			if (!AlbumGridView)
			{
				return Visibility.Visible;
			}
			return Visibility.Collapsed;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public double AlbumColumnWidth
	{
		get
		{
			return albumColumnWidth;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(albumColumnWidth, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AlbumColumnWidth);
				albumColumnWidth = value;
				OnAlbumColumnWidthChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumColumnWidth);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Track> Tracks
	{
		get
		{
			return tracks;
		}
		[MemberNotNull("tracks")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Track>>.Default.Equals(tracks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Tracks);
				tracks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Tracks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Track> VisibleTracks
	{
		get
		{
			return visibleTracks;
		}
		[MemberNotNull("visibleTracks")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Track>>.Default.Equals(visibleTracks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VisibleTracks);
				visibleTracks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VisibleTracks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string? AlbumFilter
	{
		get
		{
			return albumFilter;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(albumFilter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AlbumFilter);
				albumFilter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumFilter);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasAlbumFilter);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumFilterText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumFilterVisibility);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Album> Albums
	{
		get
		{
			return albums;
		}
		[MemberNotNull("albums")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Album>>.Default.Equals(albums, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Albums);
				albums = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Albums);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AlbumGridView
	{
		get
		{
			return albumGridView;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(albumGridView, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AlbumGridView);
				albumGridView = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumGridView);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumGridViewVisibility);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AlbumListViewVisibility);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<Track> RecentTracks
	{
		get
		{
			return recentTracks;
		}
		[MemberNotNull("recentTracks")]
		set
		{
			if (!EqualityComparer<ObservableCollection<Track>>.Default.Equals(recentTracks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RecentTracks);
				recentTracks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RecentTracks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string SearchText
	{
		get
		{
			return searchText;
		}
		[MemberNotNull("searchText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(searchText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SearchText);
				searchText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsScanning
	{
		get
		{
			return isScanning;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(isScanning, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsScanning);
				isScanning = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsScanning);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string ProgressText
	{
		get
		{
			return progressText;
		}
		[MemberNotNull("progressText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(progressText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ProgressText);
				progressText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ProgressText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public string StatusText
	{
		get
		{
			return statusText;
		}
		[MemberNotNull("statusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(statusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusText);
				statusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public Track? SelectedTrack
	{
		get
		{
			return selectedTrack;
		}
		set
		{
			if (!EqualityComparer<Track>.Default.Equals(selectedTrack, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedTrack);
				selectedTrack = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedTrack);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleAlbumViewCommand => toggleAlbumViewCommand ?? (toggleAlbumViewCommand = new RelayCommand(ToggleAlbumView));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RefreshCommand => refreshCommand ?? (refreshCommand = new AsyncRelayCommand(RefreshAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SearchCommand => searchCommand ?? (searchCommand = new AsyncRelayCommand(SearchAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<string> ScanFolderCommand => scanFolderCommand ?? (scanFolderCommand = new AsyncRelayCommand<string>(ScanFolderAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearAlbumFilterCommand => clearAlbumFilterCommand ?? (clearAlbumFilterCommand = new RelayCommand(ClearAlbumFilter));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<Track?> PlayTrackCommand => playTrackCommand ?? (playTrackCommand = new RelayCommand<Track>(PlayTrack));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<Track?> RemoveTrackCommand => removeTrackCommand ?? (removeTrackCommand = new AsyncRelayCommand<Track>(RemoveTrackAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<Album?> RemoveAlbumCommand => removeAlbumCommand ?? (removeAlbumCommand = new AsyncRelayCommand<Album>(RemoveAlbumAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.2.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<Track?> ToggleFavoriteCommand => toggleFavoriteCommand ?? (toggleFavoriteCommand = new AsyncRelayCommand<Track>(ToggleFavoriteAsync));

	public HomeViewModel(LibraryService library, AudioEngine engine, SettingsService? settingsService = null)
	{
		_library = library;
		_engine = engine;
		_settingsService = settingsService;
		AppSettings appSettings = _settingsService?.Load();
		if (appSettings != null && appSettings.AlbumColumnWidth >= 80.0 && appSettings.AlbumColumnWidth <= 400.0)
		{
			albumColumnWidth = appSettings.AlbumColumnWidth;
		}
	}

	[RelayCommand]
	private void ToggleAlbumView()
	{
		AlbumGridView = !AlbumGridView;
	}

	public async Task InitializeAsync()
	{
		await _library.InitializeAsync();
		await RefreshAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		Tracks = new ObservableCollection<Track>(await _library.GetTracksAsync(SearchText));
		Albums = new ObservableCollection<Album>(await _library.GetAlbumsAsync());
		RecentTracks = new ObservableCollection<Track>(await _library.GetRecentAsync(20));
		ApplyAlbumFilter();
		StatusText = $"共 {Tracks.Count} 首歌曲 · {Albums.Count} 张专辑";
	}

	[RelayCommand]
	private async Task SearchAsync()
	{
		await RefreshAsync();
	}

	[RelayCommand]
	private async Task ScanFolderAsync(string folder)
	{
		if (IsScanning)
		{
			return;
		}
		IsScanning = true;
		try
		{
			ProgressText = "扫描中…";
			Progress<(string, int)> progress = new Progress<(string, int)>(((string file, int total) p) =>
			{
				ProgressText = $"扫描中… {p.total} 个文件";
			});
			(int, int, int) tuple = await _library.ScanFolderAsync(folder, progress);
			ProgressText = $"新增 {tuple.Item1} 首，更新 {tuple.Item2} 首，失败 {tuple.Item3} 首";
			await RefreshAsync();
		}
		finally
		{
			IsScanning = false;
		}
	}

	public async Task ImportPathsAsync(string[] paths)
	{
		int added = 0;
		foreach (string text in paths)
		{
			if (Directory.Exists(text))
			{
				added += (await _library.ScanFolderAsync(text)).Item1;
			}
			else if (File.Exists(text) && (await _library.ImportFileAsync(text)).Item1)
			{
				added++;
			}
		}
		ProgressText = ((added > 0) ? $"扫描完成，新增 {added} 首" : "未发现新的音频文件");
		await RefreshAsync();
	}

	public void SetAlbumFilter(string album)
	{
		AlbumFilter = album;
		ApplyAlbumFilter();
	}

	[RelayCommand]
	private void ClearAlbumFilter()
	{
		AlbumFilter = null;
		ApplyAlbumFilter();
	}

	private void ApplyAlbumFilter()
	{
		VisibleTracks = (string.IsNullOrEmpty(AlbumFilter) ? new ObservableCollection<Track>(Tracks) : new ObservableCollection<Track>(Tracks.Where((Track t) => string.Equals(t.Album, AlbumFilter, StringComparison.OrdinalIgnoreCase))));
	}

	[RelayCommand]
	public void PlayTrack(Track? track)
	{
		if (track == null)
		{
			return;
		}
		if (_engine.CurrentTrack != null && _engine.CurrentTrack.Id == track.Id)
		{
			if (_engine.State == PlaybackState.Playing)
			{
				_engine.Seek(TimeSpan.Zero);
			}
			else
			{
				_engine.Play();
			}
			return;
		}
		IList<Track> activeList = (HasAlbumFilter || !string.IsNullOrWhiteSpace(SearchText)) ? VisibleTracks : Tracks;
		int num = -1;
		for (int i = 0; i < activeList.Count; i++)
		{
			if (activeList[i].Id == track.Id)
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			_engine.SetQueue(activeList, num);
			_engine.Play();
		}
		else
		{
			_engine.PlayTrack(track);
		}
	}

	[RelayCommand]
	public async Task RemoveTrackAsync(Track? track)
	{
		if (track != null)
		{
			await _library.RemoveTrackAsync(track.Id);
			_engine.RemoveTrackFromQueue(track.Id);
			await RefreshAsync();
		}
	}

	[RelayCommand]
	public async Task RemoveAlbumAsync(Album? album)
	{
		if (album == null)
		{
			return;
		}
		foreach (Track t in album.Tracks.ToList())
		{
			await _library.RemoveTrackAsync(t.Id);
			_engine.RemoveTrackFromQueue(t.Id);
		}
		await RefreshAsync();
	}

	public void SyncAfterDragReorder()
	{
		SyncMainOrderFromVisible();
		SyncQueueOrder();
	}

	private void SyncMainOrderFromVisible()
	{
		ObservableCollection<Track> observableCollection = new ObservableCollection<Track>();
		foreach (Track visibleTrack in VisibleTracks)
		{
			observableCollection.Add(visibleTrack);
		}
		foreach (Track track in Tracks)
		{
			if (IndexOfId(VisibleTracks, track.Id) < 0)
			{
				observableCollection.Add(track);
			}
		}
		Tracks = observableCollection;
	}

	private static int IndexOfId(ObservableCollection<Track> list, long id)
	{
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}

	private void SyncQueueOrder()
	{
		if (_engine.Queue.Count != Tracks.Count)
		{
			return;
		}
		foreach (Track item in _engine.Queue)
		{
			if (IndexOfId(Tracks, item.Id) < 0)
			{
				return;
			}
		}
		Track currentTrack = _engine.CurrentTrack;
		int startIndex = ((currentTrack != null) ? Math.Max(0, IndexOfId(Tracks, currentTrack.Id)) : Math.Clamp(_engine.QueueIndex, 0, Math.Max(0, Tracks.Count - 1)));
		_engine.SetQueue(Tracks, startIndex);
	}

	[RelayCommand]
	private async Task ToggleFavoriteAsync(Track? track)
	{
		if (track != null)
		{
			await _library.ToggleFavoriteAsync(track.Id);
			track.IsFavorite = !track.IsFavorite;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.2.0.0")]
	private void OnAlbumColumnWidthChanged(double value)
	{
		double v = Math.Clamp(value, 80.0, 400.0);
		if (v != value)
		{
			AlbumColumnWidth = v;
		}
		try
		{
			_settingsService?.Update((AppSettings s) =>
			{
				s.AlbumColumnWidth = v;
			});
		}
		catch
		{
		}
	}
}
