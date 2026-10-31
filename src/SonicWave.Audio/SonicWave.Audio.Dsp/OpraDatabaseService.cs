using System;
using System.Collections.Generic;
using System.Linq;

namespace SonicWave.Audio.Dsp;

public record OpraFilterDef(EchoFilterType Type, double FrequencyHz, double GainDb, double Q);

public sealed class OpraHeadphonePreset
{
    public string Id { get; init; } = "";
    public string Brand { get; init; } = "";
    public string Model { get; init; } = "";
    public string MeasurementSource { get; init; } = "Oratory1990";
    public string TargetCurve { get; init; } = "Harman Over-Ear 2018";
    public double PreampDb { get; init; } = -4.5;
    public List<OpraFilterDef> Filters { get; init; } = new();

    public string DisplayName => $"{Brand} {Model} ({MeasurementSource})";
}

/// <summary>
/// Built-in Roon OPRA / AutoEq Headphone Acoustics Calibration Database.
/// Provides Harman 2018/2019 reference frequency-response compensation for top audiophile headphones.
/// </summary>
public static class OpraDatabaseService
{
    private static readonly List<OpraHeadphonePreset> s_presets = new();

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
        return s_presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static bool ApplyPreset(EchoEqProcessor processor, string presetId)
    {
        var preset = GetPresetById(presetId);
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

        processor.SetCalibrationBands(bands, preset.PreampDb);
        processor.CalibrationEnabled = true;
        return true;
    }

    private static void InitializeDatabase()
    {
        // 1. Sennheiser HD 600 (Oratory1990 Harman Over-Ear)
        s_presets.Add(new OpraHeadphonePreset
        {
            Id = "sennheiser_hd600",
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
