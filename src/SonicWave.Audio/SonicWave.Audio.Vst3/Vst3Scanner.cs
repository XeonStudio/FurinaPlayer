using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SonicWave.Audio.Vst3;

public static class Vst3Scanner
{
    private static readonly string[] DefaultScanDirs = new[]
    {
        @"C:\Program Files\Common Files\VST3",
        @"C:\Program Files\VstPlugins",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Common\VST3")
    };

    public static List<string> GetDefaultScanDirectories()
    {
        var list = new List<string>();
        foreach (var dir in DefaultScanDirs)
        {
            if (Directory.Exists(dir)) list.Add(dir);
        }
        if (list.Count == 0) list.Add(@"C:\Program Files\Common Files\VST3");
        return list;
    }

    private static readonly object s_scanLock = new object();

    public static List<Vst3PluginInfo> Scan(IEnumerable<string>? customDirs = null)
    {
        lock (s_scanLock)
        {
            var results = new List<Vst3PluginInfo>();
        var thread = new Thread(() =>
        {
            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var singleFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (customDirs != null)
            {
                foreach (var d in customDirs)
                {
                    if (string.IsNullOrWhiteSpace(d)) continue;
                    if (File.Exists(d) || (d.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase) && Directory.Exists(d)))
                    {
                        singleFiles.Add(d);
                    }
                    else if (Directory.Exists(d))
                    {
                        dirs.Add(d);
                    }
                }
            }
            foreach (var d in GetDefaultScanDirectories())
            {
                dirs.Add(d);
            }

            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Scan explicit single files first
            foreach (var file in singleFiles)
            {
                if (seenPaths.Add(file))
                {
                    try
                    {
                        var info = ProbePlugin(file);
                        if (info != null) results.Add(info);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error scanning file {file}: {ex.Message}");
                    }
                }
            }

            foreach (var dir in dirs)
            {
                try
                {
                    ScanDirectory(dir, results, seenPaths);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error scanning {dir}: {ex.Message}");
                }
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(30000); // 30s timeout

        return results;
        }
    }

    public static Vst3PluginInfo? ScanSingle(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!File.Exists(path) && !Directory.Exists(path)) return null;

        Vst3PluginInfo? info = null;
        var thread = new Thread(() =>
        {
            try
            {
                info = ProbePlugin(path);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error probing single plugin {path}: {ex.Message}");
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(15000);

        return info;
    }

    private static void ScanDirectory(string dir, List<Vst3PluginInfo> results, HashSet<string> seenPaths)
    {
        if (!Directory.Exists(dir)) return;

        // Check bundle dirs (*.vst3 as directory)
        foreach (var subDir in Directory.GetDirectories(dir, "*.vst3", SearchOption.TopDirectoryOnly))
        {
            if (seenPaths.Add(subDir))
            {
                var info = ProbePlugin(subDir);
                if (info != null) results.Add(info);
            }
        }

        // Check standalone .vst3 files in root
        foreach (var file in Directory.GetFiles(dir, "*.vst3", SearchOption.TopDirectoryOnly))
        {
            if (seenPaths.Add(file))
            {
                var info = ProbePlugin(file);
                if (info != null) results.Add(info);
            }
        }

        // Subdirectories that are not .vst3
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            if (subDir.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                // recurse down 3 levels
                ScanSubTree(subDir, results, seenPaths, 1);
            }
            catch {}
        }
    }

    private static void ScanSubTree(string dir, List<Vst3PluginInfo> results, HashSet<string> seenPaths, int depth)
    {
        if (depth > 4) return;
        foreach (var subDir in Directory.GetDirectories(dir, "*.vst3", SearchOption.TopDirectoryOnly))
        {
            if (seenPaths.Add(subDir))
            {
                var info = ProbePlugin(subDir);
                if (info != null) results.Add(info);
            }
        }
        foreach (var file in Directory.GetFiles(dir, "*.vst3", SearchOption.TopDirectoryOnly))
        {
            if (seenPaths.Add(file))
            {
                var info = ProbePlugin(file);
                if (info != null) results.Add(info);
            }
        }
        foreach (var subDir in Directory.GetDirectories(dir))
        {
            if (subDir.EndsWith(".vst3", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                ScanSubTree(subDir, results, seenPaths, depth + 1);
            }
            catch {}
        }
    }

    public static string ResolveBinaryPath(string path)
    {
        if (File.Exists(path)) return path;
        if (Directory.Exists(path))
        {
            // VST3 standard layout: Contents/x86_64-win/<name>.vst3
            string archSub = Path.Combine(path, "Contents", "x86_64-win");
            if (Directory.Exists(archSub))
            {
                var files = Directory.GetFiles(archSub, "*.vst3");
                if (files.Length > 0) return files[0];
            }
            // fallback inside dir
            var localFiles = Directory.GetFiles(path, "*.vst3");
            if (localFiles.Length > 0) return localFiles[0];
        }
        return path;
    }

    public static Vst3PluginInfo ProbePlugin(string path)
    {
        string binPath = ResolveBinaryPath(path);
        string name = Path.GetFileNameWithoutExtension(path);
        string vendor = "Unknown";
        string version = "-";
        string category = "Fx";
        string classId = "";

        if (!File.Exists(binPath))
        {
            return new Vst3PluginInfo
            {
                Name = name,
                Path = path,
                Status = "工作不正常",
                Error = "二进制文件不存在",
                Version = "-"
            };
        }

        // Try getting file version info
        try
        {
            var vi = FileVersionInfo.GetVersionInfo(binPath);
            if (!string.IsNullOrEmpty(vi.ProductName)) name = vi.ProductName;
            if (!string.IsNullOrEmpty(vi.CompanyName)) vendor = vi.CompanyName;
            if (!string.IsNullOrEmpty(vi.ProductVersion)) version = vi.ProductVersion;
            else if (!string.IsNullOrEmpty(vi.FileVersion)) version = vi.FileVersion;
        }
        catch {}

        nint hMod = NativeMethods.LoadLibraryExW(binPath, IntPtr.Zero, 8); // LOAD_WITH_ALTERED_SEARCH_PATH = 8
        if (hMod == IntPtr.Zero)
        {
            hMod = NativeMethods.LoadLibraryW(binPath);
        }

        if (hMod == IntPtr.Zero)
        {
            int err = Marshal.GetLastWin32Error();
            return new Vst3PluginInfo
            {
                Name = name,
                Vendor = vendor,
                Path = path,
                Status = "工作不正常",
                Error = $"无法加载 DLL (错误代码: {err})",
                Version = version
            };
        }

        try
        {
            nint proc = NativeMethods.GetProcAddress(hMod, "GetPluginFactory");
            if (proc == IntPtr.Zero)
            {
                return new Vst3PluginInfo
                {
                    Name = name,
                    Vendor = vendor,
                    Path = path,
                    Status = "工作不正常",
                    Error = "缺少 GetPluginFactory 导出入口",
                    Version = version
                };
            }

            var getFactory = Marshal.GetDelegateForFunctionPointer<NativeMethods.GetPluginFactoryProc>(proc);
            nint factoryRaw = getFactory();
            if (factoryRaw == IntPtr.Zero)
            {
                return new Vst3PluginInfo
                {
                    Name = name,
                    Vendor = vendor,
                    Path = path,
                    Status = "工作不正常",
                    Error = "GetPluginFactory 返回空指针",
                    Version = version
                };
            }

            var factory = Vst3Com.ToManaged<IPluginFactory>(factoryRaw);
            int count = factory.CountClasses();

            // Try IPluginFactory2 first for richer metadata
            Guid iidFactory2 = Vst3Ids.IPluginFactory2;
            IPluginFactory2? factory2 = null;
            if (Marshal.QueryInterface(factoryRaw, in iidFactory2, out nint factory2Raw) == 0 && factory2Raw != IntPtr.Zero)
            {
                try
                {
                    factory2 = Vst3Com.ToManaged<IPluginFactory2>(factory2Raw);
                }
                catch {}
            }

            for (int i = 0; i < count; i++)
            {
                if (factory2 != null)
                {
                    nint pInfo2 = Marshal.AllocHGlobal(Marshal.SizeOf<PClassInfo2>());
                    try
                    {
                        if (factory2.GetClassInfo2(i, pInfo2) == 0)
                        {
                            var info2 = Marshal.PtrToStructure<PClassInfo2>(pInfo2);
                            string cat = (info2.Category ?? "") + "|" + (info2.SubCategories ?? "");
                            if (cat.Contains("Audio", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("Fx", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("Mastering", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("Analyzer", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("EQ", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("Dynamics", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(info2.Name)) name = info2.Name;
                                if (!string.IsNullOrEmpty(info2.Vendor)) vendor = info2.Vendor;
                                if (!string.IsNullOrEmpty(info2.Version)) version = info2.Version;
                                if (!string.IsNullOrEmpty(info2.Category)) category = info2.Category;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pInfo2);
                    }
                }
                else
                {
                    nint pInfo = Marshal.AllocHGlobal(Marshal.SizeOf<PClassInfo>());
                    try
                    {
                        if (factory.GetClassInfo(i, pInfo) == 0)
                        {
                            var info = Marshal.PtrToStructure<PClassInfo>(pInfo);
                            string cat = info.Category ?? "";
                            if (cat.Contains("Audio", StringComparison.OrdinalIgnoreCase) ||
                                cat.Contains("Fx", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(info.Name)) name = info.Name;
                                if (!string.IsNullOrEmpty(info.Category)) category = info.Category;
                                break;
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pInfo);
                    }
                }
            }

            if (factory != null)
            {
                try { GC.SuppressFinalize(factory); } catch {}
            }
            if (factory2 != null)
            {
                try { GC.SuppressFinalize(factory2); } catch {}
            }

            return new Vst3PluginInfo
            {
                Name = name,
                Vendor = vendor,
                Category = category,
                Version = string.IsNullOrWhiteSpace(version) ? "1.0.0" : version,
                Path = path,
                ClassId = classId,
                Status = "工作正常",
                Enabled = true
            };
        }
        catch (Exception ex)
        {
            return new Vst3PluginInfo
            {
                Name = name,
                Vendor = vendor,
                Path = path,
                Status = "工作不正常",
                Error = ex.Message,
                Version = version
            };
        }
        finally
        {
            // Do not FreeLibrary(hMod) here: keeping scanned module handles loaded prevents
            // AccessViolationException when ComObject wrappers are finalized by GC.
        }
    }
}
