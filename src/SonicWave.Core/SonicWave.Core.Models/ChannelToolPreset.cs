using System;

namespace SonicWave.Core.Models;

/// <summary>
/// FurinaPlayer 声道方案（耳机偏音与声道矩阵配置预设）。
/// </summary>
public sealed class ChannelToolPreset
{
	public string Id { get; set; } = Guid.NewGuid().ToString("N");

	public string Name { get; set; } = "耳机偏音补偿";

	public double Balance { get; set; } = 0.0;

	public double LeftGainDb { get; set; } = 0.0;

	public double RightGainDb { get; set; } = 0.0;

	public double TrimStepDb { get; set; } = 0.25;

	public bool ConstantPower { get; set; } = false;

	public double LeftDelayMs { get; set; } = 0.0;

	public double RightDelayMs { get; set; } = 0.0;

	public double LowLeftGainDb { get; set; } = 0.0;

	public double LowRightGainDb { get; set; } = 0.0;

	public double MidLeftGainDb { get; set; } = 0.0;

	public double MidRightGainDb { get; set; } = 0.0;

	public double HighLeftGainDb { get; set; } = 0.0;

	public double HighRightGainDb { get; set; } = 0.0;

	public string MonoMode { get; set; } = "off";

	public bool SwapLeftRight { get; set; } = false;

	public bool InvertLeft { get; set; } = false;

	public bool InvertRight { get; set; } = false;

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
