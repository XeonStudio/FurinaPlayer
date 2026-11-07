using System;
using System.IO;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FurinaPlayer.UI.Converters;

public sealed class PathToImageConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is string text && !string.IsNullOrWhiteSpace(text))
		{
			try
			{
				if (File.Exists(text))
				{
					string fullPath = Path.GetFullPath(text);
					return new BitmapImage(new Uri(fullPath));
				}
				if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https" || uri.Scheme == "ms-appx"))
				{
					return new BitmapImage(uri);
				}
			}
			catch
			{
				return null;
			}
		}
		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
