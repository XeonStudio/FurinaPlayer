using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FurinaPlayer.UI.Converters;

public sealed class PathToImageConverter : IValueConverter
{
	private const int MaxCacheEntries = 96;
	private static readonly object s_cacheLock = new();
	private static readonly Dictionary<string, LinkedListNode<(string Key, WeakReference<BitmapImage> Ref)>> s_cacheMap = new(StringComparer.Ordinal);
	private static readonly LinkedList<(string Key, WeakReference<BitmapImage> Ref)> s_lruList = new();

	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is string text && !string.IsNullOrWhiteSpace(text))
		{
			try
			{
				int decodeWidth = ResolveDecodeWidth(parameter);
				if (File.Exists(text))
				{
					string fullPath = Path.GetFullPath(text);
					long ticks = File.GetLastWriteTimeUtc(fullPath).Ticks;
					string key = $"{fullPath}|{ticks}|{decodeWidth}";
					return GetOrCreateBitmap(key, new Uri(fullPath), decodeWidth);
				}
				if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "ms-appx"))
				{
					string key = $"{uri.AbsoluteUri}|{decodeWidth}";
					return GetOrCreateBitmap(key, uri, decodeWidth);
				}
			}
			catch
			{
				return null;
			}
		}
		return null;
	}

	private static int ResolveDecodeWidth(object? parameter)
	{
		if (parameter is int w) return Math.Clamp(w, 48, 1024);
		if (parameter is string s && int.TryParse(s, out int parsed)) return Math.Clamp(parsed, 48, 1024);
		return 320;
	}

	private static BitmapImage GetOrCreateBitmap(string key, Uri uri, int decodeWidth)
	{
		lock (s_cacheLock)
		{
			if (s_cacheMap.TryGetValue(key, out var existingNode))
			{
				if (existingNode.Value.Ref.TryGetTarget(out var cachedImg))
				{
					s_lruList.Remove(existingNode);
					s_lruList.AddFirst(existingNode);
					return cachedImg;
				}
				s_lruList.Remove(existingNode);
				s_cacheMap.Remove(key);
			}
		}

		var bmp = new BitmapImage
		{
			DecodePixelType = DecodePixelType.Logical,
			DecodePixelWidth = decodeWidth,
			UriSource = uri
		};

		lock (s_cacheLock)
		{
			if (s_cacheMap.TryGetValue(key, out var oldNode))
			{
				s_lruList.Remove(oldNode);
				s_cacheMap.Remove(key);
			}

			var node = new LinkedListNode<(string, WeakReference<BitmapImage>)>((key, new WeakReference<BitmapImage>(bmp)));
			s_lruList.AddFirst(node);
			s_cacheMap[key] = node;

			while (s_cacheMap.Count > MaxCacheEntries && s_lruList.Last != null)
			{
				var tail = s_lruList.Last;
				s_lruList.RemoveLast();
				s_cacheMap.Remove(tail.Value.Key);
			}
		}

		return bmp;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
