namespace SonicWave.Audio;

public readonly record struct SpatialParameters(double Width, double Modulation, double PredelayMs)
{
	public static SpatialParameters Default => new SpatialParameters(0.6, 0.25, 25.0);
}
