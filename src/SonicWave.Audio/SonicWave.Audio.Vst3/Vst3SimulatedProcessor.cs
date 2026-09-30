using System;
using SonicWave.Core.Models;

namespace SonicWave.Audio.Vst3;

public static class Vst3SimulatedProcessor
{
	public static void Process(float[] buffer, int channels, in Vst3Parameters p)
	{
		if (buffer == null || buffer.Length == 0 || channels <= 0)
		{
			return;
		}
		double num = Math.Clamp(p.Drive, 0.0, 1.0);
		double num2 = Math.Clamp(p.Tone, -1.0, 1.0);
		double num3 = Math.Clamp(p.Mix, 0.0, 1.0);
		if (num < 0.01 && Math.Abs(num2) < 0.01 && num3 < 0.01)
		{
			return;
		}
		double num4 = 0.0;
		double num5 = 0.0;
		double num6 = Math.Clamp(0.25 + 0.45 * num2, 0.05, 0.7);
		for (int i = 0; i < buffer.Length; i++)
		{
			double num7 = buffer[i];
			int num8 = i % channels;
			double num9 = 1.0 + 3.0 * num;
			double num10 = Math.Tanh(num7 * num9) / Math.Tanh(num9) * (0.5 + 0.5 * (1.0 - num));
			ref double reference = ref num8 == 0 ? ref num4 : ref num5;
			if (num8 == 0 || num8 == 1)
			{
				reference += num6 * (num10 - reference);
				num10 = num10 * (1.0 - num2) + reference * num2;
			}
			double value = num3 * num10 + (1.0 - num3) * num7;
			buffer[i] = (float)Math.Clamp(value, -1.0, 1.0);
		}
	}
}
