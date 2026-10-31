using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using SonicWave.Audio;
using SonicWave.Core.Models;
using SonicWave.Core.Services;

namespace FurinaPlayer.Avalonia.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private readonly LibraryService _library;
    private readonly AudioEngine _engine;
    private readonly SettingsService? _settings;

    private readonly List<Track> _allTracks = new();
    private ObservableCollection<Track> _visibleTracks = new();
    private ObservableCollection<Album> _albums = new();
    private bool _isAlbumGridView;
    private string _searchText = string.Empty;
    private bool _isScanning;
    private string _scanStatusText = string.Empty;
    private Track? _selectedTrack;
    private string _summaryText = "曲库加载中...";

    public ObservableCollection<Track> VisibleTracks
    {
        get => _visibleTracks;
        private set => SetProperty(ref _visibleTracks, value);
    }

    public ObservableCollection<Album> Albums
    {
        get => _albums;
        private set => SetProperty(ref _albums, value);
    }

    public bool IsAlbumGridView
    {
        get => _isAlbumGridView;
        set
        {
            if (SetProperty(ref _isAlbumGridView, value))
            {
                OnPropertyChanged(nameof(ViewToggleText));
            }
        }
    }

    public string ViewToggleText => IsAlbumGridView ? "列表视图" : "专辑视图";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set => SetProperty(ref _isScanning, value);
    }

    public string ScanStatusText
    {
        get => _scanStatusText;
        private set => SetProperty(ref _scanStatusText, value);
    }

    public Track? SelectedTrack
    {
        get => _selectedTrack;
        set => SetProperty(ref _selectedTrack, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public HomeViewModel(LibraryService library, AudioEngine engine, SettingsService? settings)
    {
        _library = library;
        _engine = engine;
        _settings = settings;

        _ = RefreshLibraryAsync();
    }

    public async Task RefreshLibraryAsync()
    {
        try
        {
            var tracks = await _library.GetAllTracksAsync();
            var albums = await _library.GetAlbumsAsync();

            _allTracks.Clear();
            _allTracks.AddRange(tracks);

            ApplyFilter();

            Albums = new ObservableCollection<Album>(albums);
            UpdateSummary();
        }
        catch (Exception ex)
        {
            SummaryText = $"曲库加载失败: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        string query = SearchText.Trim();
        IEnumerable<Track> filtered;

        if (string.IsNullOrWhiteSpace(query))
        {
            filtered = _allTracks;
        }
        else
        {
            filtered = _allTracks.Where(t =>
                (!string.IsNullOrEmpty(t.Title) && t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.Artist) && t.Artist.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.Album) && t.Album.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(t.FilePath) && Path.GetFileName(t.FilePath).Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        VisibleTracks = new ObservableCollection<Track>(filtered);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        long totalMs = _allTracks.Sum(t => t.DurationMilliseconds);
        var totalTime = TimeSpan.FromMilliseconds(totalMs);
        string timeStr = totalTime.TotalHours >= 1.0 ? $"{(int)totalTime.TotalHours}小时{totalTime.Minutes}分" : $"{totalTime.Minutes}分钟";

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SummaryText = $"共 {_allTracks.Count} 首曲目 · {_albums.Count} 张专辑 · 总时长 {timeStr}";
        }
        else
        {
            SummaryText = $"搜索匹配: {VisibleTracks.Count} 首曲目 (共 {_allTracks.Count} 首)";
        }
    }

    public void PlayTrack(Track track)
    {
        if (track == null) return;

        var currentList = VisibleTracks.ToList();
        int index = currentList.IndexOf(track);
        if (index < 0)
        {
            currentList.Add(track);
            index = currentList.Count - 1;
        }

        _engine.SetQueue(currentList, index);
        _engine.Play();
    }

    public async Task RemoveTrackAsync(Track track)
    {
        if (track == null) return;

        await _library.RemoveTrackAsync(track.Id);
        _allTracks.Remove(track);
        VisibleTracks.Remove(track);
        UpdateSummary();
    }

    public async Task ScanFolderAsync(string folderPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        IsScanning = true;
        ScanStatusText = "正在扫描文件夹...";

        var progress = new Progress<(string file, int total)>(p =>
        {
            ScanStatusText = $"扫描中 ({p.total} 文件): {Path.GetFileName(p.file)}";
        });

        try
        {
            var res = await _library.ScanFolderAsync(folderPath, progress, ct);
            ScanStatusText = $"扫描完成: 新增 {res.Added} 首，更新 {res.Updated} 首";
            await RefreshLibraryAsync();
        }
        catch (Exception ex)
        {
            ScanStatusText = $"扫描出错: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public async Task ImportFilesAsync(IEnumerable<string> filePaths)
    {
        if (filePaths == null) return;

        IsScanning = true;
        int added = 0;

        try
        {
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                {
                    var res = await _library.ImportFileAsync(path);
                    if (res.Added) added++;
                }
            }
            ScanStatusText = $"成功导入 {added} 个音频文件";
            await RefreshLibraryAsync();
        }
        catch (Exception ex)
        {
            ScanStatusText = $"导入出错: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public void ToggleView()
    {
        IsAlbumGridView = !IsAlbumGridView;
    }
}
