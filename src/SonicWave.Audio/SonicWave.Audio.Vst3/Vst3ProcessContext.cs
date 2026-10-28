using System;
using System.Runtime.InteropServices;

namespace SonicWave.Audio.Vst3;

[Flags]
public enum ProcessContextStates : uint
{
    None = 0,
    kPlaying = 1 << 1,               // 0x0002
    kCycleActive = 1 << 2,          // 0x0004
    kRecording = 1 << 3,            // 0x0008
    kSystemTimeValid = 1 << 8,      // 0x0100
    kProjectTimeMusicValid = 1 << 9,// 0x0200
    kTempoValid = 1 << 10,          // 0x0400
    kBarPositionValid = 1 << 11,    // 0x0800
    kCycleValid = 1 << 12,          // 0x1000
    kTimeSigValid = 1 << 13,        // 0x2000
    kSmpteValid = 1 << 14,          // 0x4000
    kClockValid = 1 << 15,          // 0x8000
    kContTimeValid = 1 << 17,       // 0x20000
    kChordValid = 1 << 18           // 0x40000
}

[StructLayout(LayoutKind.Explicit, Size = 112)]
public struct Vst3ProcessContext
{
    [FieldOffset(0)] public uint State;
    [FieldOffset(8)] public double SampleRate;
    [FieldOffset(16)] public long ProjectTimeSamples;
    [FieldOffset(24)] public long SystemTime;
    [FieldOffset(32)] public long ContinousTimeSamples;
    [FieldOffset(40)] public double ProjectTimeMusic;
    [FieldOffset(48)] public double BarPositionMusic;
    [FieldOffset(56)] public double CycleStartMusic;
    [FieldOffset(64)] public double CycleEndMusic;
    [FieldOffset(72)] public double Tempo;
    [FieldOffset(80)] public int TimeSigNumerator;
    [FieldOffset(84)] public int TimeSigDenominator;
    [FieldOffset(88)] public byte ChordKey;
    [FieldOffset(89)] public byte ChordRoot;
    [FieldOffset(90)] public short ChordMask;
    [FieldOffset(92)] public int SmpteOffsetSubframes;
    [FieldOffset(96)] public uint FrameRateFramesPerSecond;
    [FieldOffset(100)] public uint FrameRateFlags;
    [FieldOffset(104)] public int SamplesToNextClock;
}
