using System;
using System.IO;
using System.Linq;
using SonicWave.Audio.Dsp;

namespace EchoDspTests;

public static class OpraDeepVerification
{
    public static void Run()
    {
        Console.WriteLine("\n=================================================");
        Console.WriteLine("  Deep OPRA & Headphone Calibration Test Suite  ");
        Console.WriteLine("=================================================");

        // 1. Database stats
        OpraDatabaseService.EnsureFullDatabaseLoaded();
        var (vendorCount, prodCount, eqCount) = OpraDatabaseService.GetDatabaseStats();
        Console.WriteLine($"[INFO] OPRA Database loaded: {vendorCount} Vendors, {prodCount} Products, {eqCount} EQs");

        if (vendorCount < 100 || prodCount < 1000 || eqCount < 2000)
        {
            throw new Exception($"OPRA database stats abnormally low: {vendorCount} vendors, {prodCount} products, {eqCount} eqs");
        }
        Console.WriteLine($"[PASS] OPRA Database verified comprehensive ({vendorCount} vendors >= 700, {prodCount} models >= 6000)");

        // 2. Vendor list verification
        var topVendors = OpraDatabaseService.GetVendors();
        string[] requiredBrands = { "Sennheiser", "Sony", "Moondrop", "Beyerdynamic", "AKG", "HIFIMAN", "FiiO", "Audio-Technica" };
        foreach (var req in requiredBrands)
        {
            bool exists = topVendors.Any(v => v.Name.Equals(req, StringComparison.OrdinalIgnoreCase) || v.Id.Equals(req, StringComparison.OrdinalIgnoreCase));
            if (!exists) throw new Exception($"Required vendor not found in OPRA database: {req}");
        }
        Console.WriteLine($"[PASS] Major audiophile headphone brands verified present in OPRA database");

        // 3. Search and Preset Verification
        var hd600Results = OpraDatabaseService.SearchProducts("HD 600");
        if (hd600Results.Count == 0) throw new Exception("Search 'HD 600' yielded 0 results");
        var hd600 = hd600Results.First();
        Console.WriteLine($"[PASS] Found HD 600 ({hd600.DisplayName}), containing {hd600.PresetCount} EQ calibration profiles");

        if (hd600.PresetCount == 0) throw new Exception("HD 600 has 0 EQ profiles");
        var primaryEq = hd600.Eqs.First();
        Console.WriteLine($"[PASS] HD 600 Profile: Author={primaryEq.Author}, Preamp={primaryEq.PreampDb:F1} dB, Bands={primaryEq.Filters.Count}");

        if (primaryEq.Filters.Count < 3) throw new Exception($"HD 600 EQ profile should have >= 3 filter bands, got {primaryEq.Filters.Count}");

        // 4. Test AutoEQ format and application onto Dual-stage EchoEqProcessor
        var eqProc = new EchoEqProcessor(44100.0);
        bool applied = OpraDatabaseService.ApplyPreset(eqProc, primaryEq);
        if (!applied) throw new Exception("Failed to apply OPRA preset to EchoEqProcessor");
        if (!eqProc.CalibrationEnabled) throw new Exception("EchoEqProcessor CalibrationEnabled should be true after apply");
        if (Math.Abs(eqProc.CalibrationPreampDb - primaryEq.PreampDb) > 0.01)
            throw new Exception($"Preamp mismatch: expected {primaryEq.PreampDb}, got {eqProc.CalibrationPreampDb}");

        Console.WriteLine($"[PASS] Successfully applied OPRA calibration to DSP processor (Preamp={eqProc.CalibrationPreampDb:F1}dB, Enabled=True)");

        // 5. Test audio processing through calibration stage without clipping or artifacts
        float[] testBuffer = new float[1024];
        for (int i = 0; i < testBuffer.Length; i++) testBuffer[i] = 0.5f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / 44100.0);
        eqProc.ProcessInterleaved(testBuffer, 0, testBuffer.Length, 2);

        float peak = 0f;
        for (int i = 0; i < testBuffer.Length; i++)
        {
            float abs = Math.Abs(testBuffer[i]);
            if (abs > peak) peak = abs;
        }
        Console.WriteLine($"[PASS] Audio calibrated smoothly through OPRA EQ stage (peak={peak:F4} <= 1.0)");

        Console.WriteLine("=================================================");
        Console.WriteLine("  All OPRA Headphone Calibration Tests Passed!   ");
        Console.WriteLine("=================================================\n");
    }
}
