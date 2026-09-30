using System;
using System.IO;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FurinaPlayer.UI.Converters;

public sealed class PathToImageConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is string text && !string.IsNullOrWhiteSpace(text) && File.Exists(text))
		{
			try
			{
				return new BitmapImage(new Uri(text));
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
