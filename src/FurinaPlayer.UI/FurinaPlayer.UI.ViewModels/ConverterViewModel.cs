using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FurinaPlayer.Audio.Services;
using FurinaPlayer.Core.Services;

namespace FurinaPlayer.UI.ViewModels;

public enum ConverterItemState
{
	Pending,
	Running,
	Succeeded,
	Failed,
	Cancelled
}

public sealed class ConverterTaskItem : ObservableObject
{
	public required string InputPath { get; init; }
	public string FileName => Path.GetFileName(InputPath);
	public string SourceExt => Path.GetExtension(InputPath).TrimStart('.').ToUpperInvariant();
	public string FileSizeText { get; init; } = string.Empty;

	private ConverterItemState _state = ConverterItemState.Pending;
	public ConverterItemState State
	{
		get => _state;
		set => SetProperty(ref _state, value);
	}

	private int _progress;
	public int Progress
	{
		get => _progress;
		set => SetProperty(ref _progress, value);
	}

	private string _statusText = Loc.T("等待转换");
	public string StatusText
	{
		get => _statusText;
		set => SetProperty(ref _statusText, value);
	}

	private string? _outputPath;
	public string? OutputPath
	{
		get => _outputPath;
		set => SetProperty(ref _outputPath, value);
	}

	private string _targetContainer = string.Empty;
	public string TargetContainer
	{
		get => _targetContainer;
		set => SetProperty(ref _targetContainer, value);
	}

	private string _resolvedTitle = string.Empty;
	public string ResolvedTitle
	{
		get => _resolvedTitle;
		set => SetProperty(ref _resolvedTitle, value);
	}

	private string _resolvedArtist = string.Empty;
	public string ResolvedArtist
	{
		get => _resolvedArtist;
		set => SetProperty(ref _resolvedArtist, value);
	}

	private bool _hasMetadata;
	public bool HasMetadata
	{
		get => _hasMetadata;
		set => SetProperty(ref _hasMetadata, value);
	}

	private bool _metadataCompleted;
	public bool MetadataCompleted
	{
		get => _metadataCompleted;
		set => SetProperty(ref _metadataCompleted, value);
	}

	private bool _hasLyrics;
	public bool HasLyrics
	{
		get => _hasLyrics;
		set => SetProperty(ref _hasLyrics, value);
	}

	private bool _lyricsCompleted;
	public bool LyricsCompleted
	{
		get => _lyricsCompleted;
		set => SetProperty(ref _lyricsCompleted, value);
	}

	private bool _hasCover;
	public bool HasCover
	{
		get => _hasCover;
		set => SetProperty(ref _hasCover, value);
	}

	private bool _coverCompleted;
	public bool CoverCompleted
	{
		get => _coverCompleted;
		set => SetProperty(ref _coverCompleted, value);
	}
}

public sealed class ConverterViewModel : ObservableObject
{
	private readonly FormatConverterService _converter;
	private CancellationTokenSource? _cts;

	public ObservableCollection<ConverterTaskItem> Items { get; } = new();

	private ConvertTargetFormat _targetFormat = ConvertTargetFormat.AutoMp3OrFlac;
	public ConvertTargetFormat TargetFormat
	{
		get => _targetFormat;
		set => SetProperty(ref _targetFormat, value);
	}

	private string _outputDirectory = string.Empty;
	public string OutputDirectory
	{
		get => _outputDirectory;
		set => SetProperty(ref _outputDirectory, value);
	}

	private bool _autoEnrichMetadata = true;
	public bool AutoEnrichMetadata
	{
		get => _autoEnrichMetadata;
		set => SetProperty(ref _autoEnrichMetadata, value);
	}

	private bool _isConverting;
	public bool IsConverting
	{
		get => _isConverting;
		set => SetProperty(ref _isConverting, value);
	}

	private int _completedCount;
	public int CompletedCount
	{
		get => _completedCount;
		set => SetProperty(ref _completedCount, value);
	}

	private int _totalCount;
	public int TotalCount
	{
		get => _totalCount;
		set => SetProperty(ref _totalCount, value);
	}

	private double _overallProgress;
	public double OverallProgress
	{
		get => _overallProgress;
		set => SetProperty(ref _overallProgress, value);
	}

	private string _overallStatusText = string.Empty;
	public string OverallStatusText
	{
		get => _overallStatusText;
		set => SetProperty(ref _overallStatusText, value);
	}

	public ConverterViewModel(FormatConverterService? converter = null)
	{
		_converter = converter ?? new FormatConverterService();
	}

	public int AddFiles(IEnumerable<string> filePaths)
	{
		int added = 0;
		var existing = new HashSet<string>(Items.Select(i => i.InputPath), StringComparer.OrdinalIgnoreCase);
		foreach (string path in filePaths)
		{
			if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
			if (!AudioDecoders.IsSupportedExtension(path)) continue;
			if (!existing.Add(path)) continue;

			long bytes = 0;
			try { bytes = new FileInfo(path).Length; } catch { }
			Items.Add(new ConverterTaskItem
			{
				InputPath = path,
				FileSizeText = FormatSize(bytes),
				StatusText = Loc.T("等待转换")
			});
			added++;
		}
		UpdateCounters();
		return added;
	}

	public int AddFolder(string folderPath)
	{
		if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return 0;
		try
		{
			var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
				.Where(AudioDecoders.IsSupportedExtension);
			return AddFiles(files);
		}
		catch
		{
			return 0;
		}
	}

	public void RemoveItem(ConverterTaskItem item)
	{
		if (IsConverting && item.State == ConverterItemState.Running) return;
		Items.Remove(item);
		UpdateCounters();
	}

	public void ClearList()
	{
		if (IsConverting) return;
		Items.Clear();
		UpdateCounters();
	}

	public void CancelConversion()
	{
		_cts?.Cancel();
	}

	public async Task StartConversionAsync(Action<Action> dispatchToUi)
	{
		if (IsConverting || Items.Count == 0) return;

		_cts?.Dispose();
		_cts = new CancellationTokenSource();
		var ct = _cts.Token;

		IsConverting = true;
		var pending = Items.Where(i => i.State != ConverterItemState.Succeeded).ToList();
		if (pending.Count == 0)
		{
			foreach (var item in Items)
			{
				item.State = ConverterItemState.Pending;
				item.Progress = 0;
				item.StatusText = Loc.T("等待转换");
			}
			pending = Items.ToList();
		}

		TotalCount = pending.Count;
		CompletedCount = 0;
		OverallProgress = 0;
		OverallStatusText = Loc.F("已完成 {0} / {1} 首", 0, pending.Count);

		try
		{
			for (int idx = 0; idx < pending.Count; idx++)
			{
				if (ct.IsCancellationRequested) break;
				var item = pending[idx];

				dispatchToUi(() =>
				{
					item.State = ConverterItemState.Running;
					item.Progress = 5;
					item.StatusText = Loc.T("正在解密私有音频容器...");
				});

				int currentIndex = idx;
				var progress = new Progress<ConvertProgressInfo>(info =>
				{
					dispatchToUi(() =>
					{
						item.Progress = info.Percent;
						item.StatusText = info.Stage;
						double basePct = (currentIndex + info.Percent / 100.0) / Math.Max(1, pending.Count) * 100.0;
						OverallProgress = Math.Clamp(basePct, 0, 100);
					});
				});

				var result = await _converter.ConvertFileAsync(new ConvertRequest
				{
					InputPath = item.InputPath,
					OutputDirectory = string.IsNullOrWhiteSpace(OutputDirectory) ? null : OutputDirectory,
					TargetFormat = TargetFormat,
					AutoEnrichMetadata = AutoEnrichMetadata,
					EnableOnlineEnrich = AutoEnrichMetadata
				}, progress, ct);

				dispatchToUi(() =>
				{
					if (result.Success)
					{
						item.State = ConverterItemState.Succeeded;
						item.Progress = 100;
						item.StatusText = Loc.T("转换完成");
						item.OutputPath = result.OutputPath;
						item.TargetContainer = result.TargetContainer.ToUpperInvariant();
						item.ResolvedTitle = result.Title;
						item.ResolvedArtist = result.Artist;
						item.HasMetadata = result.HasMetadata;
						item.MetadataCompleted = result.MetadataCompleted;
						item.HasLyrics = result.HasLyrics;
						item.LyricsCompleted = result.LyricsCompleted;
						item.HasCover = result.HasCover;
						item.CoverCompleted = result.CoverCompleted;
					}
					else if (ct.IsCancellationRequested)
					{
						item.State = ConverterItemState.Cancelled;
						item.StatusText = Loc.T("任务已取消");
					}
					else
					{
						item.State = ConverterItemState.Failed;
						item.Progress = 0;
						item.StatusText = result.ErrorMessage ?? "Error";
					}

					CompletedCount = idx + 1;
					OverallProgress = (double)CompletedCount / Math.Max(1, pending.Count) * 100.0;
					OverallStatusText = Loc.F("已完成 {0} / {1} 首", CompletedCount, pending.Count);
				});
			}
		}
		finally
		{
			dispatchToUi(() =>
			{
				IsConverting = false;
			});
		}
	}

	private void UpdateCounters()
	{
		TotalCount = Items.Count;
		CompletedCount = Items.Count(i => i.State == ConverterItemState.Succeeded);
		OverallProgress = TotalCount > 0 ? (double)CompletedCount / TotalCount * 100.0 : 0;
		OverallStatusText = TotalCount > 0 ? Loc.F("已完成 {0} / {1} 首", CompletedCount, TotalCount) : string.Empty;
	}

	private static string FormatSize(long bytes)
	{
		if (bytes <= 0) return "0 KB";
		double mb = bytes / (1024.0 * 1024.0);
		return mb >= 1.0 ? $"{mb:0.00} MB" : $"{Math.Max(1, bytes / 1024)} KB";
	}
}
