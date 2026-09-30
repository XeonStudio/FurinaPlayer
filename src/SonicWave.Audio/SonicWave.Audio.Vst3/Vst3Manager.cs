using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Vst3;

public static class Vst3Manager
{
	public static IReadOnlyList<string> DefaultScanFolders
	{
		get
		{
			List<string> list = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			AddIfDir(list, Path.Combine(folderPath, "Common Files", "VST3"));
			if (!string.IsNullOrEmpty(folderPath2))
			{
				AddIfDir(list, Path.Combine(folderPath2, "Common Files", "VST3"));
			}
			AddIfDir(list, Path.Combine(folderPath, "Common Files", "Steinberg", "VST3"));
			return list;
		}
	}

	private static void AddIfDir(List<string> list, string dir)
	{
		try
		{
			if (Directory.Exists(dir) && !list.Contains(dir, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(dir);
			}
		}
		catch
		{
		}
	}

	public static List<string> ScanFolder(string folder)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
		{
			return list;
		}
		try
		{
			foreach (string item in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
			{
				if (IsPluginFile(item))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		return list.OrderBy((string f) => f, StringComparer.OrdinalIgnoreCase).ToList();
	}

	public static bool IsPluginFile(string path)
	{
		string extension = Path.GetExtension(path);
		if (!string.Equals(extension, ".vst3", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase))
			{
				return path.EndsWith(".vst3.dll", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
		return true;
	}

	public static Vst3PluginState CreateState(string path)
	{
		Vst3PluginState vst3PluginState = new Vst3PluginState
		{
			Path = path
		};
		try
		{
			vst3PluginState.Name = Path.GetFileNameWithoutExtension(path);
			if (vst3PluginState.Name.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
			{
				string name = vst3PluginState.Name;
				vst3PluginState.Name = name.Substring(0, name.Length - 5);
			}
		}
		catch
		{
			vst3PluginState.Name = Path.GetFileName(path);
		}
		try
		{
			FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
			if (!string.IsNullOrWhiteSpace(versionInfo.CompanyName))
			{
				vst3PluginState.Vendor = versionInfo.CompanyName;
			}
			if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
			{
				vst3PluginState.Version = versionInfo.ProductVersion;
			}
			if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
			{
				vst3PluginState.Name = versionInfo.ProductName;
			}
		}
		catch
		{
		}
		return vst3PluginState;
	}

	public static List<Vst3PluginState> Merge(List<Vst3PluginState> saved, IEnumerable<string> discovered)
	{
		List<Vst3PluginState> list = new List<Vst3PluginState>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string path in discovered)
		{
			hashSet.Add(path);
			Vst3PluginState vst3PluginState = saved?.FirstOrDefault((Vst3PluginState s) => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));
			list.Add(vst3PluginState ?? CreateState(path));
		}
		if (saved != null)
		{
			foreach (Vst3PluginState item in saved)
			{
				if (!hashSet.Contains(item.Path))
				{
					list.Add(item);
				}
			}
		}
		return list;
	}
}
