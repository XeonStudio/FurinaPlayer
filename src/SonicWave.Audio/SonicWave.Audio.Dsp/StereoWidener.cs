using System;

namespace SonicWave.Audio.Dsp;

public sealed class StereoWidener
{
	public double Amount { get; set; }

	public void ProcessInterleaved(float[] samples, int channels)
	{
		if (channels >= 2 && samples.Length >= 2 && !(Math.Abs(Amount) < 0.01))
		{
			double num = Math.Clamp(Amount, -1.0, 1.0);
			for (int i = 0; i + 1 < samples.Length; i += channels)
			{
				double num2 = samples[i];
				double num3 = samples[i + 1];
				double num4 = (num2 + num3) * 0.5;
				double num5 = (num2 - num3) * 0.5 * (1.0 + num);
				samples[i] = (float)Math.Clamp(num4 + num5, -1.0, 1.0);
				samples[i + 1] = (float)Math.Clamp(num4 - num5, -1.0, 1.0);
			}
		}
	}
}
