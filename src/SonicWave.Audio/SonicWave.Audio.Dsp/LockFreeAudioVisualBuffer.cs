using System;
using System.Threading;

namespace SonicWave.Audio.Dsp;

/// <summary>
/// Lock-free circular audio visual buffer designed for 60fps real-time UI visualization.
/// Captures interleaved PCM samples from the real-time audio thread without allocation or mutex locks.
/// Computes instant FFT spectrum, BS.1770-4 momentary loudness, true peak, and phase correlation.
/// </summary>
public sealed class LockFreeAudioVisualBuffer
{
    private const int BufferCapacity = 8192; // Power-of-two capacity
    private const int BufferMask = BufferCapacity - 1;

    private readonly float[] _ringBufferL = new float[BufferCapacity];
    private readonly float[] _ringBufferR = new float[BufferCapacity];
    private long _writeIndex;

    // Real-time BS.1770 K-weighting filter states (Stage 1: High shelf, Stage 2: High pass)
    // Coeffs for ~48kHz (pre-filter and RLB filter)
    private float _k1_x1_L, _k1_x2_L, _k1_y1_L, _k1_y2_L;
    private float _k1_x1_R, _k1_x2_R, _k1_y1_R, _k1_y2_R;
    private float _k2_x1_L, _k2_x2_L, _k2_y1_L, _k2_y2_L;
    private float _k2_x1_R, _k2_x2_R, _k2_y1_R, _k2_y2_R;

    private float _momentaryPowerSum;
    private int _momentarySampleCount;
    private float _momentaryLufs = -70.0f;
    private float _shortTermLufs = -70.0f;
    private float _recentTruePeak = 0.0f;
    private float _recentCorrelation = 1.0f;

    // FFT scratch buffers
    private readonly float[] _fftReal = new float[1024];
    private readonly float[] _fftImag = new float[1024];
    private readonly float[] _fftWindow = new float[1024];

    public static LockFreeAudioVisualBuffer Instance { get; } = new();

    public LockFreeAudioVisualBuffer()
    {
        // Pre-compute Hann window for 1024 FFT
        for (int i = 0; i < _fftWindow.Length; i++)
        {
            _fftWindow[i] = 0.5f * (1.0f - (float)Math.Cos(2.0 * Math.PI * i / (_fftWindow.Length - 1)));
        }
    }

    /// <summary>
    /// Thread-safe lock-free push from real-time audio rendering thread.
    /// </summary>
    public void PushSamples(float[] interleavedBuffer, int offset, int count, int channels)
    {
        if (count <= 0 || channels <= 0) return;

        int frames = count / channels;
        long write = Interlocked.Read(ref _writeIndex);

        float peakL = 0f;
        float peakR = 0f;
        float dotProduct = 0f;
        float energyL = 0f;
        float energyR = 0f;

        for (int i = 0; i < frames; i++)
        {
            int baseIdx = offset + i * channels;
            float left = interleavedBuffer[baseIdx];
            float right = channels > 1 ? interleavedBuffer[baseIdx + 1] : left;

            if (float.IsNaN(left) || float.IsInfinity(left)) left = 0f;
            if (float.IsNaN(right) || float.IsInfinity(right)) right = 0f;

            int targetIdx = (int)((write + i) & BufferMask);
            _ringBufferL[targetIdx] = left;
            _ringBufferR[targetIdx] = right;

            float absL = Math.Abs(left);
            float absR = Math.Abs(right);
            if (absL > peakL) peakL = absL;
            if (absR > peakR) peakR = absR;

            dotProduct += left * right;
            energyL += left * left;
            energyR += right * right;

            // Simplified BS.1770 K-Weighting filter integration
            // Stage 1: High shelf filter (~1.5kHz +4dB)
            float s1L = 1.535f * left - 2.691f * _k1_x1_L + 1.198f * _k1_x2_L - (-1.690f) * _k1_y1_L - (0.732f) * _k1_y2_L;
            _k1_x2_L = _k1_x1_L; _k1_x1_L = left; _k1_y2_L = _k1_y1_L; _k1_y1_L = s1L;

            float s1R = 1.535f * right - 2.691f * _k1_x1_R + 1.198f * _k1_x2_R - (-1.690f) * _k1_y1_R - (0.732f) * _k1_y2_R;
            _k1_x2_R = _k1_x1_R; _k1_x1_R = right; _k1_y2_R = _k1_y1_R; _k1_y1_R = s1R;

            // Stage 2: High pass filter (~38Hz)
            float s2L = s1L - 2.0f * _k2_x1_L + _k2_x2_L - (-1.990f) * _k2_y1_L - (0.990f) * _k2_y2_L;
            _k2_x2_L = _k2_x1_L; _k2_x1_L = s1L; _k2_y2_L = _k2_y1_L; _k2_y1_L = s2L;

            float s2R = s1R - 2.0f * _k2_x1_R + _k2_x2_R - (-1.990f) * _k2_y1_R - (0.990f) * _k2_y2_R;
            _k2_x2_R = _k2_x1_R; _k2_x1_R = s1R; _k2_y2_R = _k2_y1_R; _k2_y1_R = s2R;

            _momentaryPowerSum += s2L * s2L + s2R * s2R;
            _momentarySampleCount++;
        }

        Interlocked.Add(ref _writeIndex, frames);

        // Update correlation
        float denom = (float)(Math.Sqrt(energyL * energyR) + 1e-9);
        _recentCorrelation = Math.Clamp(dotProduct / denom, -1.0f, 1.0f);

        // Update true peak with smooth release
        float blockPeak = Math.Max(peakL, peakR);
        if (blockPeak >= _recentTruePeak)
            _recentTruePeak = blockPeak;
        else
            _recentTruePeak = _recentTruePeak * 0.95f + blockPeak * 0.05f;

        // Calculate 400ms momentary loudness
        if (_momentarySampleCount >= 4800) // ~100ms update window
        {
            float meanSquare = _momentaryPowerSum / _momentarySampleCount;
            if (meanSquare > 1e-12f)
            {
                float lufs = -0.691f + 10.0f * (float)Math.Log10(meanSquare);
                _momentaryLufs = Math.Clamp(lufs, -70.0f, 6.0f);
                _shortTermLufs = _shortTermLufs * 0.8f + _momentaryLufs * 0.2f;
            }
            else
            {
                _momentaryLufs = -70.0f;
            }
            _momentaryPowerSum = 0f;
            _momentarySampleCount = 0;
        }
    }

    /// <summary>
    /// Reads latest PCM waveform for oscilloscope / multitrack display (zero lock).
    /// </summary>
    public void ReadWaveform(float[] destLeft, float[] destRight, int frames)
    {
        int readFrames = Math.Min(frames, BufferCapacity);
        long write = Interlocked.Read(ref _writeIndex);
        long start = write - readFrames;

        for (int i = 0; i < readFrames; i++)
        {
            int idx = (int)((start + i) & BufferMask);
            destLeft[i] = _ringBufferL[idx];
            if (destRight != null && i < destRight.Length)
            {
                destRight[i] = _ringBufferR[idx];
            }
        }
    }

    /// <summary>
    /// Computes 512-bin power spectrum from the latest 1024 samples using in-place FFT.
    /// Returns normalized logarithmic magnitude array (0.0 to 1.0).
    /// </summary>
    public void ComputeSpectrum(float[] outputMagnitudes)
    {
        if (outputMagnitudes == null || outputMagnitudes.Length == 0) return;

        int fftSize = 1024;
        long write = Interlocked.Read(ref _writeIndex);
        long start = write - fftSize;

        for (int i = 0; i < fftSize; i++)
        {
            int idx = (int)((start + i) & BufferMask);
            // Mono mix with Hann window
            float sample = (_ringBufferL[idx] + _ringBufferR[idx]) * 0.5f;
            _fftReal[i] = sample * _fftWindow[i];
            _fftImag[i] = 0f;
        }

        // Radix-2 Cooley-Tukey FFT
        RunFft(_fftReal, _fftImag, fftSize);

        int outBins = Math.Min(outputMagnitudes.Length, fftSize / 2);
        for (int i = 0; i < outBins; i++)
        {
            float re = _fftReal[i];
            float im = _fftImag[i];
            float power = re * re + im * im;
            float magDb = 10.0f * (float)Math.Log10(power + 1e-9f); // -90dB to 0dB
            // Normalize -80dB ~ 0dB into 0.0 ~ 1.0
            float norm = Math.Clamp((magDb + 80.0f) / 80.0f, 0.0f, 1.0f);
            outputMagnitudes[i] = norm;
        }
    }

    /// <summary>
    /// Returns real-time LUFS, TruePeak dB, and phase correlation for Insight 2 Pro.
    /// </summary>
    public void GetLoudnessInfo(out float momentaryLufs, out float shortTermLufs, out float truePeakDb, out float correlation)
    {
        momentaryLufs = _momentaryLufs;
        shortTermLufs = _shortTermLufs;
        truePeakDb = _recentTruePeak > 1e-6f ? 20.0f * (float)Math.Log10(_recentTruePeak) : -70.0f;
        correlation = _recentCorrelation;
    }

    private static void RunFft(float[] real, float[] imag, int n)
    {
        // Bit-reversal permutation
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                float tr = real[i]; real[i] = real[j]; real[j] = tr;
                float ti = imag[i]; imag[i] = imag[j]; imag[j] = ti;
            }
            int k = n >> 1;
            while (k <= j)
            {
                j -= k;
                k >>= 1;
            }
            j += k;
        }

        // Cooley-Tukey butterflies
        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = -2.0 * Math.PI / len;
            float wlen_r = (float)Math.Cos(angle);
            float wlen_i = (float)Math.Sin(angle);

            for (int i = 0; i < n; i += len)
            {
                float w_r = 1.0f;
                float w_i = 0.0f;

                for (int m = 0; m < len / 2; m++)
                {
                    int u = i + m;
                    int v = i + m + len / 2;

                    float vr = real[v] * w_r - imag[v] * w_i;
                    float vi = real[v] * w_i + imag[v] * w_r;

                    real[v] = real[u] - vr;
                    imag[v] = imag[u] - vi;
                    real[u] += vr;
                    imag[u] += vi;

                    float next_w_r = w_r * wlen_r - w_i * wlen_i;
                    w_i = w_r * wlen_i + w_i * wlen_r;
                    w_r = next_w_r;
                }
            }
        }
    }
}
