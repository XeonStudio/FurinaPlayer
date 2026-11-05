using System;

namespace SonicWave.Audio.Dsp;

public enum ChannelMatrixPreset
{
	Normal,
	SwapLeftRight,
	MonoSum,
	InvertPhaseLeft,
	InvertPhaseRight,
	Custom
}

/// <summary>
/// 向后兼容包装类，继承并对齐全新架构的 FurinaPlayerSpatialProcessor。
/// </summary>
public class EchoSpatialProcessor : FurinaPlayerSpatialProcessor
{
}
