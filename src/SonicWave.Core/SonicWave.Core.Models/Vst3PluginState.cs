using System.Text.Json.Serialization;

namespace SonicWave.Core.Models;

public sealed class Vst3PluginState
{
	public string Path { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Vendor { get; set; } = "未知（模拟宿主）";

	public string Version { get; set; } = "1.0.0";

	public bool Enabled { get; set; }

	public bool Bypass { get; set; }

	public double Drive { get; set; } = 0.25;

	public double Tone { get; set; } = 0.1;

	public double Mix { get; set; } = 0.35;

	[JsonIgnore]
	public bool IsActive
	{
		get
		{
			if (Enabled)
			{
				return !Bypass;
			}
			return false;
		}
	}
}
