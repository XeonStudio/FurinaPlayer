using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Vst3;

public static class Vst3Manager
{
	private static readonly (string[] Keywords, string VendorName)[] KnownVendorPatterns = new (string[], string)[]
	{
		(new[] { "iZotope", "Ozone", "Nectar", "Neutron", "Insight", "RX ", "RX1", "RX8", "RX9", "RX10", "RX11", "Tonal Balance", "VocalSynth", "Neoverb", "Trash", "Vinyl", "Relay", "Audiolens" }, "iZotope, Inc."),
		(new[] { "Celemony", "Melodyne" }, "Celemony Software GmbH"),
		(new[] { "FabFilter", "Pro-Q", "Pro-C", "Pro-L", "Pro-R", "Pro-MB", "Pro-DS", "Pro-G", "Saturn", "Timeless", "Volcano", "Simplon", "Micro" }, "FabFilter"),
		(new[] { "Waves", "WaveShell" }, "Waves Audio"),
		(new[] { "Steinberg", "Halion", "Padshop", "Retrologue", "Groove Agent", "Cubase", "Nuendo", "WaveLab" }, "Steinberg Media Technologies"),
		(new[] { "Valhalla" }, "Valhalla DSP"),
		(new[] { "Native Instruments", "Kontakt", "Reaktor", "Guitar Rig", "Massive", "FM8", "Raum", "Replika" }, "Native Instruments"),
		(new[] { "Softube" }, "Softube"),
		(new[] { "Plugin Alliance", "Brainworx", "bx_", "Shadow Hills", "Maag", "Elysia", "Lindell" }, "Plugin Alliance"),
		(new[] { "UAD", "Universal Audio", "UADx" }, "Universal Audio"),
		(new[] { "Voxengo", "SPAN" }, "Voxengo"),
		(new[] { "MeldaProduction", "Melda" }, "MeldaProduction"),
		(new[] { "Soundtoys", "Decapitator", "EchoBoy", "Little AlterBoy", "Crystallizer", "FilterFreak", "MicroShift", "PanMan", "PrimalTap", "Radiator", "Sie-Q" }, "Soundtoys"),
		(new[] { "Serum", "Xfer", "LFOTool" }, "Xfer Records"),
		(new[] { "Arturia" }, "Arturia"),
		(new[] { "Eventide", "Blackhole" }, "Eventide"),
		(new[] { "Lexicon" }, "Lexicon"),
		(new[] { "Solid State Logic", "SSL " }, "Solid State Logic"),
		(new[] { "Sonnox", "Oxford" }, "Sonnox"),
		(new[] { "Overloud", "TH-U" }, "Overloud"),
		(new[] { "Neural DSP", "Archetype" }, "Neural DSP"),
		(new[] { "IK Multimedia", "T-RackS", "AmpliTube", "MODO" }, "IK Multimedia"),
		(new[] { "Kilohearts", "kHs", "Phase Plant", "Snap Heap", "Multipass" }, "Kilohearts"),
		(new[] { "Tokyo Dawn", "TDR " }, "Tokyo Dawn Labs")
	};

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

			foreach (string dir in Directory.EnumerateDirectories(folder, "*.vst3", SearchOption.AllDirectories))
			{
				string bin64 = Path.Combine(dir, "Contents", "x86_64-win");
				if (Directory.Exists(bin64))
				{
					var files = Directory.GetFiles(bin64, "*.vst3");
					foreach (var f in files)
					{
						if (!list.Contains(f, StringComparer.OrdinalIgnoreCase))
						{
							list.Add(f);
						}
					}
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
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
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

	public static bool IsValidVst3Path(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		try
		{
			if (File.Exists(path) && IsPluginFile(path))
			{
				return true;
			}
			if (Directory.Exists(path) && path.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
			{
				string bin64 = Path.Combine(path, "Contents", "x86_64-win");
				if (Directory.Exists(bin64) && Directory.EnumerateFiles(bin64, "*.vst3").Any())
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public static Vst3PluginState CreateState(string path)
	{
		string resolvedPath = ResolveBinaryPath(path);
		Vst3PluginState vst3PluginState = new Vst3PluginState
		{
			Path = resolvedPath,
			Vendor = "VST3 Native Plugin"
		};
		try
		{
			vst3PluginState.Name = Path.GetFileNameWithoutExtension(resolvedPath);
			if (vst3PluginState.Name.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
			{
				string name = vst3PluginState.Name;
				vst3PluginState.Name = name.Substring(0, name.Length - 5);
			}
		}
		catch
		{
			vst3PluginState.Name = Path.GetFileName(resolvedPath);
		}

		string? detectedVendor = null;
		try
		{
			if (File.Exists(resolvedPath))
			{
				FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(resolvedPath);
				if (!string.IsNullOrWhiteSpace(versionInfo.CompanyName))
				{
					detectedVendor = versionInfo.CompanyName.Trim();
				}
				if (!string.IsNullOrWhiteSpace(versionInfo.ProductVersion))
				{
					vst3PluginState.Version = versionInfo.ProductVersion.Trim();
				}
				if (!string.IsNullOrWhiteSpace(versionInfo.ProductName))
				{
					vst3PluginState.Name = versionInfo.ProductName.Trim();
				}
			}
		}
		catch
		{
		}

		if (string.IsNullOrWhiteSpace(detectedVendor))
		{
			TryReadModuleInfoJson(resolvedPath, ref detectedVendor, vst3PluginState);
		}

		if (string.IsNullOrWhiteSpace(detectedVendor))
		{
			detectedVendor = InferVendorFromPathOrName(resolvedPath, vst3PluginState.Name);
		}

		vst3PluginState.Vendor = !string.IsNullOrWhiteSpace(detectedVendor) ? detectedVendor! : "VST3 Native Plugin";
		return vst3PluginState;
	}

	private static string ResolveBinaryPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}
		try
		{
			if (Directory.Exists(path) && path.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
			{
				string bin64 = Path.Combine(path, "Contents", "x86_64-win");
				if (Directory.Exists(bin64))
				{
					string? firstBin = Directory.EnumerateFiles(bin64, "*.vst3").FirstOrDefault();
					if (!string.IsNullOrEmpty(firstBin))
					{
						return firstBin;
					}
				}
			}
		}
		catch
		{
		}
		return path;
	}

	private static void TryReadModuleInfoJson(string binaryPath, ref string? vendor, Vst3PluginState state)
	{
		try
		{
			string? bundleRoot = FindBundleRoot(binaryPath);
			if (string.IsNullOrEmpty(bundleRoot))
			{
				return;
			}
			string moduleInfoPath = Path.Combine(bundleRoot, "Contents", "Resources", "moduleinfo.json");
			if (!File.Exists(moduleInfoPath))
			{
				moduleInfoPath = Path.Combine(bundleRoot, "Contents", "moduleinfo.json");
			}
			if (!File.Exists(moduleInfoPath))
			{
				return;
			}

			string json = File.ReadAllText(moduleInfoPath);
			using JsonDocument doc = JsonDocument.Parse(json, new JsonDocumentOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			});
			JsonElement root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return;
			}

			if (root.TryGetProperty("Factory Info", out JsonElement factoryInfo) && factoryInfo.ValueKind == JsonValueKind.Object)
			{
				if (factoryInfo.TryGetProperty("Vendor", out JsonElement vProp) && vProp.ValueKind == JsonValueKind.String)
				{
					string? v = vProp.GetString();
					if (!string.IsNullOrWhiteSpace(v))
					{
						vendor = v.Trim();
					}
				}
			}
			if (string.IsNullOrWhiteSpace(vendor) && root.TryGetProperty("Vendor", out JsonElement rootVendor) && rootVendor.ValueKind == JsonValueKind.String)
			{
				string? v = rootVendor.GetString();
				if (!string.IsNullOrWhiteSpace(v))
				{
					vendor = v.Trim();
				}
			}
			if (root.TryGetProperty("Version", out JsonElement verProp) && verProp.ValueKind == JsonValueKind.String)
			{
				string? ver = verProp.GetString();
				if (!string.IsNullOrWhiteSpace(ver) && (string.IsNullOrWhiteSpace(state.Version) || state.Version == "1.0.0"))
				{
					state.Version = ver.Trim();
				}
			}
		}
		catch
		{
		}
	}

	private static string? FindBundleRoot(string path)
	{
		try
		{
			string? current = Path.GetDirectoryName(path);
			while (!string.IsNullOrEmpty(current))
			{
				if (current.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase))
				{
					return current;
				}
				current = Path.GetDirectoryName(current);
			}
		}
		catch
		{
		}
		return null;
	}

	public static string InferVendorFromPathOrName(string path, string? pluginName)
	{
		string combined = (path ?? string.Empty) + " " + (pluginName ?? string.Empty);
		foreach (var (keywords, vendorName) in KnownVendorPatterns)
		{
			foreach (string kw in keywords)
			{
				if (combined.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return vendorName;
				}
			}
		}

		try
		{
			string? bundleRoot = FindBundleRoot(path ?? string.Empty);
			string? startDir = !string.IsNullOrEmpty(bundleRoot) ? Path.GetDirectoryName(bundleRoot) : Path.GetDirectoryName(path ?? string.Empty);
			if (!string.IsNullOrEmpty(startDir))
			{
				string folderName = Path.GetFileName(startDir);
				if (!string.IsNullOrWhiteSpace(folderName) &&
					!string.Equals(folderName, "VST3", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "Common Files", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "Program Files", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "Program Files (x86)", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "Steinberg", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "Contents", StringComparison.OrdinalIgnoreCase) &&
					!string.Equals(folderName, "x86_64-win", StringComparison.OrdinalIgnoreCase))
				{
					return folderName;
				}
			}
		}
		catch
		{
		}

		return "VST3 Native Plugin";
	}

	public static List<Vst3PluginState> Merge(List<Vst3PluginState> saved, IEnumerable<string> discovered)
	{
		List<Vst3PluginState> list = new List<Vst3PluginState>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string path in discovered)
		{
			hashSet.Add(path);
			Vst3PluginState? vst3PluginState = saved?.FirstOrDefault((Vst3PluginState s) => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));
			if (vst3PluginState != null)
			{
				UpgradeLegacyVendor(vst3PluginState);
				list.Add(vst3PluginState);
			}
			else
			{
				list.Add(CreateState(path));
			}
		}
		if (saved != null)
		{
			foreach (Vst3PluginState item in saved)
			{
				if (!hashSet.Contains(item.Path))
				{
					UpgradeLegacyVendor(item);
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static void UpgradeLegacyVendor(Vst3PluginState item)
	{
		if (item == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(item.Vendor) ||
			string.Equals(item.Vendor, "未知（模拟宿主）", StringComparison.OrdinalIgnoreCase) ||
			item.Vendor.Contains("未知", StringComparison.OrdinalIgnoreCase) ||
			item.Vendor.Contains("模拟宿主", StringComparison.OrdinalIgnoreCase))
		{
			Vst3PluginState fresh = CreateState(item.Path);
			item.Vendor = fresh.Vendor;
			if (!string.IsNullOrWhiteSpace(fresh.Version) && (string.IsNullOrWhiteSpace(item.Version) || item.Version == "1.0.0"))
			{
				item.Version = fresh.Version;
			}
		}
	}
}
