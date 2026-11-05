using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SonicWave.Audio.Dsp;

public record OpraFilterDef(EchoFilterType Type, double FrequencyHz, double GainDb, double Q);

public sealed class OpraHeadphonePreset
{
    public string Id { get; init; } = "";
    public string ProductId { get; init; } = "";
    public string Brand { get; init; } = "";
    public string Model { get; init; } = "";
    public string Author { get; init; } = "AutoEQ";
    public string Details { get; init; } = "";
    public string MeasurementSource { get; init; } = "Oratory1990";
    public string TargetCurve { get; init; } = "Harman Over-Ear 2018";
    public double PreampDb { get; init; } = -4.5;
    public List<OpraFilterDef> Filters { get; init; } = new();

    public string DisplayName => $"{Brand} {Model} ({MeasurementSource})";
}

public sealed class OpraVendor
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? LogoUrl { get; init; }
    public string? Blurb { get; init; }
    public int ProductCount { get; set; }
    public int EqCount { get; set; }
}

public sealed class OpraProduct
{
    public string Id { get; init; } = "";
    public string VendorId { get; init; } = "";
    public string VendorName { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Subtype { get; init; }
    public string? AssetUrl { get; init; }
    public int PresetCount => Eqs.Count;
    public List<OpraHeadphonePreset> Eqs { get; init; } = new();

    public string DisplayName => $"{VendorName} {Name}";
}

/// <summary>
/// Built-in Roon OPRA / AutoEq Headphone Acoustics Calibration Database.
/// Provides Harman 2018/2019 reference frequency-response compensation for top audiophile headphones.
/// </summary>
public static class OpraDatabaseService
{
    public const string OpraAssetBaseUrl = "https://opra.roonlabs.net/";

    private static readonly List<OpraHeadphonePreset> s_presets = new();
    private static readonly object s_dbLock = new();
    private static bool s_isFullDatabaseLoaded = false;

    private static readonly Dictionary<string, OpraVendor> s_vendors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, OpraProduct> s_products = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, List<OpraProduct>> s_productsByVendor = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, OpraHeadphonePreset> s_presetsById = new(StringComparer.OrdinalIgnoreCase);

    static OpraDatabaseService()
    {
        InitializeDatabase();
    }

    public static IReadOnlyList<OpraHeadphonePreset> GetAllPresets() => s_presets;

    public static IEnumerable<OpraHeadphonePreset> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return s_presets;
        string q = query.Trim().ToLowerInvariant();
        return s_presets.Where(p =>
            p.Brand.ToLowerInvariant().Contains(q) ||
            p.Model.ToLowerInvariant().Contains(q) ||
            p.DisplayName.ToLowerInvariant().Contains(q));
    }

    public static OpraHeadphonePreset? GetPresetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var builtin = s_presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        if (builtin != null) return builtin;

        EnsureFullDatabaseLoaded();
        lock (s_dbLock)
        {
            if (s_presetsById.TryGetValue(id, out var preset)) return preset;
        }
        return null;
    }

    public static bool ApplyPreset(EchoEqProcessor processor, string presetId, double? overridePreamp = null)
    {
        var preset = GetPresetById(presetId);
        if (preset == null || processor == null) return false;
        return ApplyPreset(processor, preset, overridePreamp);
    }

    public static bool ApplyPreset(EchoEqProcessor processor, OpraHeadphonePreset preset, double? overridePreamp = null)
    {
        if (preset == null || processor == null) return false;

        var bands = new List<EchoBiquadFilter>();
        foreach (var f in preset.Filters)
        {
            bands.Add(new EchoBiquadFilter
            {
                FilterType = f.Type,
                FrequencyHz = f.FrequencyHz,
                GainDb = f.GainDb,
                Q = f.Q,
                Enabled = true
            });
        }

        double preamp = overridePreamp ?? preset.PreampDb;
        processor.SetCalibrationBands(bands, preamp);
        processor.CalibrationEnabled = true;
        return true;
    }

    public static IReadOnlyList<OpraVendor> GetVendors(string source = "opra")
    {
        EnsureFullDatabaseLoaded();
        lock (s_dbLock)
        {
            return s_vendors.Values
                .OrderByDescending(v => v.ProductCount)
                .ThenBy(v => v.Name)
                .ToList();
        }
    }

    public static IReadOnlyList<OpraProduct> GetProductsByVendor(string vendorId, string source = "opra")
    {
        if (string.IsNullOrWhiteSpace(vendorId)) return Array.Empty<OpraProduct>();
        EnsureFullDatabaseLoaded();
        lock (s_dbLock)
        {
            if (s_productsByVendor.TryGetValue(vendorId, out var list))
            {
                return list.OrderBy(p => p.Name).ToList();
            }
            return Array.Empty<OpraProduct>();
        }
    }

    public static IReadOnlyList<OpraProduct> SearchProducts(string? query, string source = "opra", int limit = 90)
    {
        EnsureFullDatabaseLoaded();
        if (string.IsNullOrWhiteSpace(query))
        {
            lock (s_dbLock)
            {
                return s_products.Values.Take(limit).ToList();
            }
        }

        string q = query.Trim().ToLowerInvariant();
        lock (s_dbLock)
        {
            return s_products.Values
                .Where(p => p.VendorName.ToLowerInvariant().Contains(q) ||
                            p.Name.ToLowerInvariant().Contains(q) ||
                            p.Id.ToLowerInvariant().Contains(q))
                .Take(limit)
                .ToList();
        }
    }

    public static OpraProduct? GetProductById(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId)) return null;
        EnsureFullDatabaseLoaded();
        lock (s_dbLock)
        {
            s_products.TryGetValue(productId, out var prod);
            return prod;
        }
    }

    public static (int vendorCount, int productCount, int eqCount) GetDatabaseStats()
    {
        EnsureFullDatabaseLoaded();
        lock (s_dbLock)
        {
            int eqTotal = s_products.Values.Sum(p => p.Eqs.Count);
            return (s_vendors.Count, s_products.Count, eqTotal);
        }
    }

    public static void EnsureFullDatabaseLoaded()
    {
        if (s_isFullDatabaseLoaded) return;

        lock (s_dbLock)
        {
            if (s_isFullDatabaseLoaded) return;

            string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FurinaPlayer", "opra", "database_v1.jsonl");
            string roamingEchoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ECHO Steam", "opra", "database_v1.jsonl");

            string? targetPath = File.Exists(localPath) ? localPath : (File.Exists(roamingEchoPath) ? roamingEchoPath : null);

            if (targetPath != null && File.Exists(targetPath))
            {
                try
                {
                    LoadJsonlDatabase(targetPath);
                    s_isFullDatabaseLoaded = true;
                    return;
                }
                catch
                {
                    // Fallback to built-in if file read fails
                }
            }

            LoadBuiltinFallbackCatalog();
            s_isFullDatabaseLoaded = true;
        }
    }

    private static void LoadJsonlDatabase(string filePath)
    {
        var rawEqs = new List<(string eqId, string productId, string author, string details, double gainDb, List<OpraFilterDef> filters)>();

        foreach (var line in File.ReadLines(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) continue;
                string type = typeProp.GetString() ?? "";
                string id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                if (string.IsNullOrEmpty(id) || !root.TryGetProperty("data", out var dataProp)) continue;

                if (type == "vendor")
                {
                    string name = dataProp.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
                    string? logo = dataProp.TryGetProperty("logo", out var l) ? l.GetString() : null;
                    string? blurb = dataProp.TryGetProperty("blurb", out var b) ? b.GetString() : null;
                    string? logoUrl = !string.IsNullOrEmpty(logo) ? OpraAssetBaseUrl + logo : null;

                    s_vendors[id] = new OpraVendor
                    {
                        Id = id,
                        Name = name,
                        LogoUrl = logoUrl,
                        Blurb = blurb
                    };
                }
                else if (type == "product")
                {
                    string name = dataProp.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    string vendorId = dataProp.TryGetProperty("vendor_id", out var v) ? v.GetString() ?? "" : "";
                    string? subtype = dataProp.TryGetProperty("subtype", out var s) ? s.GetString() : null;
                    string? lineArtPng = dataProp.TryGetProperty("line_art_96x64_png", out var lap) ? lap.GetString() : null;
                    string? lineArtSvg = dataProp.TryGetProperty("line_art_svg", out var las) ? las.GetString() : null;
                    string? assetUrl = !string.IsNullOrEmpty(lineArtPng) ? OpraAssetBaseUrl + lineArtPng :
                                      (!string.IsNullOrEmpty(lineArtSvg) ? OpraAssetBaseUrl + lineArtSvg : null);

                    string vendorName = s_vendors.TryGetValue(vendorId, out var vend) ? vend.Name : vendorId;

                    var prod = new OpraProduct
                    {
                        Id = id,
                        VendorId = vendorId,
                        VendorName = vendorName,
                        Name = name,
                        Subtype = subtype,
                        AssetUrl = assetUrl
                    };
                    s_products[id] = prod;

                    if (!s_productsByVendor.TryGetValue(vendorId, out var vList))
                    {
                        vList = new List<OpraProduct>();
                        s_productsByVendor[vendorId] = vList;
                    }
                    vList.Add(prod);
                }
                else if (type == "eq")
                {
                    string productId = dataProp.TryGetProperty("product_id", out var p) ? p.GetString() ?? "" : "";
                    string author = dataProp.TryGetProperty("author", out var a) ? a.GetString() ?? "AutoEQ" : "AutoEQ";
                    string details = dataProp.TryGetProperty("details", out var d) ? d.GetString() ?? "" : "";
                    double gainDb = 0.0;
                    var filters = new List<OpraFilterDef>();

                    if (dataProp.TryGetProperty("parameters", out var paramsProp))
                    {
                        if (paramsProp.TryGetProperty("gain_db", out var g)) gainDb = g.GetDouble();
                        if (paramsProp.TryGetProperty("bands", out var bandsProp) && bandsProp.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var bandEl in bandsProp.EnumerateArray())
                            {
                                string bTypeStr = bandEl.TryGetProperty("type", out var bt) ? bt.GetString() ?? "" : "";
                                double freq = bandEl.TryGetProperty("frequency", out var bf) ? bf.GetDouble() : 1000.0;
                                double gain = bandEl.TryGetProperty("gain_db", out var bg) ? bg.GetDouble() : 0.0;
                                double q = bandEl.TryGetProperty("q", out var bq) ? bq.GetDouble() : 1.0;

                                EchoFilterType fType = bTypeStr switch
                                {
                                    "low_shelf" => EchoFilterType.LowShelf,
                                    "high_shelf" => EchoFilterType.HighShelf,
                                    "low_pass" => EchoFilterType.LowPass,
                                    "high_pass" => EchoFilterType.HighPass,
                                    "notch" => EchoFilterType.Notch,
                                    "band_pass" => EchoFilterType.BandPass,
                                    _ => EchoFilterType.Peaking
                                };
                                filters.Add(new OpraFilterDef(fType, freq, gain, q));
                            }
                        }
                    }

                    rawEqs.Add((id, productId, author, details, gainDb, filters));
                }
            }
            catch
            {
                // Continue reading
            }
        }

        // Attach EQs to products
        foreach (var (eqId, productId, author, details, gainDb, filters) in rawEqs)
        {
            if (s_products.TryGetValue(productId, out var prod))
            {
                string measSource = !string.IsNullOrWhiteSpace(details) && details.StartsWith("Measured by ", StringComparison.OrdinalIgnoreCase)
                    ? details.Substring("Measured by ".Length)
                    : author;

                var preset = new OpraHeadphonePreset
                {
                    Id = eqId,
                    ProductId = productId,
                    Brand = prod.VendorName,
                    Model = prod.Name,
                    Author = author,
                    Details = details,
                    MeasurementSource = measSource,
                    TargetCurve = "Harman Reference",
                    PreampDb = gainDb,
                    Filters = filters
                };

                prod.Eqs.Add(preset);
                s_presetsById[eqId] = preset;
            }
        }

        // Calculate vendor product and eq counts
        foreach (var vendor in s_vendors.Values)
        {
            if (s_productsByVendor.TryGetValue(vendor.Id, out var prods))
            {
                vendor.ProductCount = prods.Count;
                vendor.EqCount = prods.Sum(p => p.Eqs.Count);
            }
        }
    }

    private static void LoadBuiltinFallbackCatalog()
    {
        var topBrands = new (string id, string name, int prods, int eqs, string? logo)[]
        {
            ("sennheiser", "Sennheiser", 278, 763, "assets/8c/c5/8cc5dcc3248e4e9578656cca184e7cbbbad84a97a83132bfc22ed59e8f4d86b6.png"),
            ("sony", "Sony", 177, 421, "assets/26/80/2680e9612bcc2f68238ec0f8fee11019749853b3b9d4da7f003781bcdbe20102.png"),
            ("moondrop", "Moondrop", 99, 359, "assets/ab/4c/ab4c4cac69b87ca577b40b05693ec39272a673d9cf431ecf6241d0daed7cbe17.png"),
            ("beyerdynamic", "Beyerdynamic", 137, 348, "assets/76/82/7682022587531cf24594daf889cc92fb2afa2d074f42ea9988d589779dd1f569.png"),
            ("kz", "KZ", 109, 275, null),
            ("akg", "AKG", 84, 263, null),
            ("hifiman", "HIFIMAN", 93, 263, null),
            ("fiio", "FiiO", 85, 213, null),
            ("audeze", "Audeze", 62, 209, null),
            ("simgot", "Simgot Audio", 42, 209, null),
            ("64audio", "64 Audio", 68, 187, null),
            ("audio_technica", "Audio-Technica", 94, 182, null),
            ("zmf", "ZMF", 75, 171, null),
            ("dunu", "DUNU", 54, 164, null),
            ("truthear", "TRUTHEAR", 17, 146, null),
            ("final", "Final Audio", 50, 121, null),
            ("kiwi_ears", "Kiwi Ears", 28, 120, null),
            ("stax", "STAX", 61, 117, null),
            ("thieaudio", "ThieAudio", 47, 116, null),
            ("jbl", "JBL", 83, 115, null),
            ("shure", "Shure", 36, 114, null),
            ("tin_hifi", "Tin HiFi", 40, 111, null),
            ("shuoer", "Shuoer", 29, 106, null),
            ("meze", "Meze", 33, 101, null),
            ("lz_audio", "LZ Audio", 41, 98, null),
            ("etymotic", "Etymotic", 29, 96, null),
            ("tanchjim", "Tanchjim", 30, 96, null),
            ("focal", "Focal", 30, 95, null),
            ("grado", "Grado", 48, 95, null),
            ("koss", "Koss", 42, 95, null),
            ("bose", "Bose", 52, 94, null),
            ("philips", "Philips", 31, 90, null),
            ("cca", "CCA", 43, 86, null),
            ("hidizs", "Hidizs", 34, 86, null),
            ("dan_clark", "Dan Clark Audio", 34, 84, null),
            ("bgvp", "BGVP", 48, 83, null),
            ("campfire", "Campfire Audio", 42, 81, null),
            ("imr", "IMR Acoustics", 47, 81, null),
            ("anker", "Anker", 39, 79, null),
            ("trn", "TRN", 51, 79, null),
            ("samsung", "Samsung", 34, 77, null),
            ("apple", "Apple", 35, 76, null),
            ("fostex", "Fostex", 34, 74, null),
            ("jh_audio", "JH Audio", 46, 74, null),
            ("7hz", "7Hz", 16, 70, null),
            ("qdc", "QDC", 38, 70, null),
            ("jvc", "JVC", 29, 68, null),
            ("tansio_mirai", "Tansio Mirai", 37, 68, null),
            ("beats", "Beats", 40, 65, null),
            ("creative", "Creative", 37, 65, null),
            ("nicehck", "NiceHCK Audio", 37, 63, null),
            ("kinera", "Kinera", 35, 60, null),
            ("denon", "Denon", 30, 54, null),
            ("skullcandy", "Skullcandy", 44, 54, null),
            ("blon", "BLON", 22, 52, null),
            ("superlux", "Superlux", 26, 48, null),
            ("seeaudio", "SeeAudio", 18, 46, null),
            ("jabra", "Jabra", 28, 44, null),
            ("1more", "1MORE", 24, 42, null),
            ("razer", "Razer", 30, 40, null)
        };

        foreach (var b in topBrands)
        {
            var v = new OpraVendor
            {
                Id = b.id,
                Name = b.name,
                LogoUrl = b.logo != null ? OpraAssetBaseUrl + b.logo : null,
                ProductCount = b.prods,
                EqCount = b.eqs
            };
            s_vendors[b.id] = v;
            s_productsByVendor[b.id] = new List<OpraProduct>();
        }

        // Add built-in products matching reference presets
        foreach (var p in s_presets)
        {
            string vendorId = p.Brand.ToLowerInvariant().Replace(" ", "_");
            if (!s_vendors.TryGetValue(vendorId, out var vend))
            {
                vend = new OpraVendor { Id = vendorId, Name = p.Brand, ProductCount = 1, EqCount = 1 };
                s_vendors[vendorId] = vend;
                s_productsByVendor[vendorId] = new List<OpraProduct>();
            }

            string prodId = $"{vendorId}::{p.Model.ToLowerInvariant().Replace(" ", "_")}";
            if (!s_products.TryGetValue(prodId, out var prod))
            {
                prod = new OpraProduct
                {
                    Id = prodId,
                    VendorId = vendorId,
                    VendorName = vend.Name,
                    Name = p.Model,
                    Subtype = "headphones"
                };
                s_products[prodId] = prod;
                s_productsByVendor[vendorId].Add(prod);
            }
            prod.Eqs.Add(p);
            s_presetsById[p.Id] = p;
        }
    }

    private static void InitializeDatabase()
    {
        // 1. Sennheiser HD 600 (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "sennheiser_hd600",
            ProductId = "sennheiser::hd_600",
            Brand = "Sennheiser",
            Model = "HD 600",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -6.4,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.LowShelf, 45.0, 5.5, 0.71),
                new(EchoFilterType.Peaking, 180.0, -1.8, 1.4),
                new(EchoFilterType.Peaking, 1250.0, -1.4, 2.0),
                new(EchoFilterType.Peaking, 3400.0, -2.5, 3.2),
                new(EchoFilterType.Peaking, 5600.0, -1.8, 4.0),
                new(EchoFilterType.HighShelf, 10000.0, 2.5, 0.71)
            }
        });

        // 2. Sennheiser HD 650 (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "sennheiser_hd650",
            ProductId = "sennheiser::hd_650",
            Brand = "Sennheiser",
            Model = "HD 650",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -7.2,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.LowShelf, 40.0, 6.2, 0.71),
                new(EchoFilterType.Peaking, 160.0, -2.2, 1.2),
                new(EchoFilterType.Peaking, 1300.0, -1.5, 2.2),
                new(EchoFilterType.Peaking, 3200.0, -2.0, 3.0),
                new(EchoFilterType.HighShelf, 9000.0, 3.0, 0.71)
            }
        });

        // 3. Sennheiser HD 800 S (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "sennheiser_hd800s",
            ProductId = "sennheiser::hd_800_s",
            Brand = "Sennheiser",
            Model = "HD 800 S",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -6.0,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.LowShelf, 35.0, 5.5, 0.71),
                new(EchoFilterType.Peaking, 1800.0, 2.8, 1.6),
                new(EchoFilterType.Peaking, 5800.0, -4.5, 3.8),
                new(EchoFilterType.Peaking, 9200.0, -3.2, 4.5),
                new(EchoFilterType.HighShelf, 11000.0, 1.5, 0.71)
            }
        });

        // 4. Sony WH-1000XM4 (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "sony_wh1000xm4",
            ProductId = "sony::wh1000xm4",
            Brand = "Sony",
            Model = "WH-1000XM4",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -6.8,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.Peaking, 140.0, -4.5, 0.9),
                new(EchoFilterType.Peaking, 720.0, 3.2, 1.4),
                new(EchoFilterType.Peaking, 2400.0, 4.0, 1.8),
                new(EchoFilterType.Peaking, 4600.0, -3.8, 3.2),
                new(EchoFilterType.HighShelf, 8500.0, 4.2, 0.71)
            }
        });

        // 5. Beyerdynamic DT 770 Pro (80 Ohm)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "beyer_dt770_80",
            ProductId = "beyerdynamic::dt_770_pro_80",
            Brand = "Beyerdynamic",
            Model = "DT 770 Pro 80Ω",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -5.8,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.Peaking, 105.0, -3.2, 1.2),
                new(EchoFilterType.Peaking, 3400.0, 3.5, 1.6),
                new(EchoFilterType.Peaking, 5900.0, -6.5, 4.0),
                new(EchoFilterType.Peaking, 8600.0, -4.8, 3.5),
                new(EchoFilterType.HighShelf, 10000.0, 2.0, 0.71)
            }
        });

        // 6. HiFiMAN Sundara (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "hifiman_sundara",
            ProductId = "hifiman::sundara",
            Brand = "HiFiMAN",
            Model = "Sundara",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -5.5,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.LowShelf, 40.0, 5.0, 0.71),
                new(EchoFilterType.Peaking, 2100.0, -2.5, 2.0),
                new(EchoFilterType.Peaking, 5900.0, -3.2, 3.0),
                new(EchoFilterType.HighShelf, 9000.0, 1.8, 0.71)
            }
        });

        // 7. Moondrop Blessing 2 (Crinacle Harman In-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "moondrop_blessing2",
            ProductId = "moondrop::blessing2",
            Brand = "Moondrop",
            Model = "Blessing 2",
            MeasurementSource = "Crinacle",
            TargetCurve = "Harman In-Ear 2019",
            PreampDb = -4.0,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.LowShelf, 60.0, 3.5, 0.71),
                new(EchoFilterType.Peaking, 3000.0, -1.8, 2.5),
                new(EchoFilterType.Peaking, 6500.0, -2.2, 3.5),
                new(EchoFilterType.HighShelf, 10000.0, 2.0, 0.71)
            }
        });

        // 8. Apple AirPods Max (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "apple_airpods_max",
            ProductId = "apple::airpods_max",
            Brand = "Apple",
            Model = "AirPods Max",
            MeasurementSource = "Oratory1990",
            TargetCurve = "Harman Over-Ear 2018",
            PreampDb = -4.2,
            Filters = new List<OpraFilterDef>
            {
                new(EchoFilterType.Peaking, 180.0, -2.0, 1.2),
                new(EchoFilterType.Peaking, 1400.0, 2.5, 1.8),
                new(EchoFilterType.Peaking, 3800.0, -3.0, 2.8),
                new(EchoFilterType.HighShelf, 8000.0, 2.5, 0.71)
            }
        });
    }
}
