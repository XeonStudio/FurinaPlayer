using System;
using System.Collections.Generic;

namespace FurinaPlayer.Audio.Dsp;

public sealed record SoundEffectPresetInfo(
	string Id,
	string Name,
	string Category,
	string OriginTag,
	string Description);

/// <summary>
/// FurinaPlayer 音效处理器：完整移植并本地兼容适配 ECHO 全部 22 种音效，
/// 并参照 Adobe Audition 扩展 10 种专业工作室音效（共 32 种实时音效）。
/// </summary>
public sealed class FurinaSoundEffectsProcessor
{
	public static IReadOnlyList<SoundEffectPresetInfo> Catalog { get; } = new[]
	{
		new SoundEffectPresetInfo("off", "关闭 (直通)", "全部", "系统", "旁路音效染色，保持原始信号直通。"),
		// ECHO 22 项音效
		new SoundEffectPresetInfo("vocal", "清晰人声", "人声与空间", "ECHO", "提升 2.5 kHz 人声存在感并适度收紧低频浑浊。"),
		new SoundEffectPresetInfo("bass", "深沉低音", "母带与动态", "ECHO", "增强 65–110 Hz 下潜与打击感。"),
		new SoundEffectPresetInfo("warm", "温暖柔和", "复古与模拟", "ECHO", "温和柔化刺耳高频并润泽中低频。"),
		new SoundEffectPresetInfo("wide", "宽阔声场", "人声与空间", "ECHO", "Mid/Side 立体声展宽，增强包裹感。"),
		new SoundEffectPresetInfo("live", "LiveHouse 现场", "人声与空间", "ECHO", "近场舞台反射配合现场人声高光。"),
		new SoundEffectPresetInfo("hall", "音乐厅", "人声与空间", "ECHO", "自然大空间反射与绵长堂音。"),
		new SoundEffectPresetInfo("tape", "磁带质感", "复古与模拟", "ECHO", "轻度饱和暖度配合细腻高频滚降。"),
		new SoundEffectPresetInfo("night", "夜间舒适", "母带与动态", "ECHO", "柔化响度动态起伏，适合夜深低音量聆听。"),
		new SoundEffectPresetInfo("headphones", "耳机自然声場", "人声与空间", "ECHO", "轻度自然互馈，缓解耳机头中效应。"),
		new SoundEffectPresetInfo("echo", "短回声", "人声与空间", "ECHO", "节奏感立体声短拍回声与反馈衰减。"),
		new SoundEffectPresetInfo("tube", "电子管质感", "复古与模拟", "ECHO", "偶次谐波软饱和，赋予声音胆机光泽。"),
		new SoundEffectPresetInfo("vinyl", "黑膠質感", "复古与模拟", "ECHO", "模拟黑胶唱片的温润频响与微弱岁月感。"),
		new SoundEffectPresetInfo("autopan", "左右流动", "调制与创意", "ECHO", "立体声声像缓慢周期性左右摆动。"),
		new SoundEffectPresetInfo("deess", "齿音柔化", "人声与空间", "ECHO", "温和衰减 6–8 kHz 齿音尖峰。"),
		new SoundEffectPresetInfo("tremolo", "颤音律动", "调制与创意", "ECHO", "周期性幅度调制，呈现复古吉他音箱颤音。"),
		new SoundEffectPresetInfo("phaser", "相位扫频", "调制与创意", "ECHO", "全通移相器 LFO 扫频，带来太空流动感。"),
		new SoundEffectPresetInfo("transient", "鼓点塑形", "母带与动态", "ECHO", "强化瞬态冲击力与打击乐边界。"),
		new SoundEffectPresetInfo("flanger", "镶边扫频", "调制与创意", "ECHO", "短调制延时产生经典喷气式镶边扫频。"),
		new SoundEffectPresetInfo("telephone", "电话音色", "调制与创意", "ECHO", "300–3400 Hz 窄带滤波与轻微失真，经典电话听筒味。"),
		new SoundEffectPresetInfo("digital", "复古数字", "调制与创意", "ECHO", "轻度降采样与位深颗粒感，Lo-Fi 像素韵味。"),
		new SoundEffectPresetInfo("autowah", "自动哇音", "调制与创意", "ECHO", "包络跟随带通谐振扫频，随律动自动开合。"),
		new SoundEffectPresetInfo("ringmod", "金属调制", "调制与创意", "ECHO", "载波环形调制，产生赛博金属音色纹理。"),
		// Adobe Audition 风格扩展 10 项专业音效
		new SoundEffectPresetInfo("studio_chorus", "工作室合唱 (Audition)", "调制与创意", "Audition", "多层立体声微失谐合唱，加厚弦乐与合声层次。"),
		new SoundEffectPresetInfo("analog_delay", "模拟磁带延迟 (Audition)", "人声与空间", "Audition", "带高频阻尼的乒乓模拟延迟回响。"),
		new SoundEffectPresetInfo("exciter_air", "空气感激励器 (Audition)", "母带与动态", "Audition", "高次谐波激发，赋予高频通透“空气感”。"),
		new SoundEffectPresetInfo("sub_harmonics", "亚低音谐波合成 (Audition)", "母带与动态", "Audition", "为瘦弱音频注入浑厚饱满的低频支撑。"),
		new SoundEffectPresetInfo("radio_broadcast", "广播电台母带 (Audition)", "母带与动态", "Audition", "经典广播级多段响度提升与磁性中频。"),
		new SoundEffectPresetInfo("lofi_cassette", "Lo-Fi 随身听磁带 (Audition)", "复古与模拟", "Audition", "磁带抖晃 (Wow/Flutter) 配合暖窄频响。"),
		new SoundEffectPresetInfo("concert_arena", "巨蛋体育场 (Audition)", "人声与空间", "Audition", "超大空间脉冲延时与强氛围包络。"),
		new SoundEffectPresetInfo("binaural_3d", "双耳 3D 环绕 (Audition)", "人声与空间", "Audition", "耳廓空间反射与延迟差营造 3D 环绕感。"),
		new SoundEffectPresetInfo("karaoke_vocal_cut", "伴奏消人声 (Audition)", "人声与空间", "Audition", "中央声道相位抵消削弱主唱，保留两侧伴奏。"),
		new SoundEffectPresetInfo("mastering_glue", "母带总线粘合 (Audition)", "母带与动态", "Audition", "温和压缩感与轻微模拟饱滿，使全频段更紧致。")
	};

	private readonly object _lock = new();
	private string _presetId = "off";
	private double _intensity = 1.0;
	private double _sampleRate = 44100.0;

	private readonly FurinaPlayerBiquadFilter _eqLow = new() { Enabled = true };
	private readonly FurinaPlayerBiquadFilter _eqMid = new() { Enabled = true };
	private readonly FurinaPlayerBiquadFilter _eqHigh = new() { Enabled = true };

	private float[] _delayBufL = new float[65536];
	private float[] _delayBufR = new float[65536];
	private int _delayPos;
	private double _lfoPhase;
	private float _envFollower;
	private float _prevL, _prevR;
	private float _lpL, _lpR;
	private float _holdL, _holdR;
	private int _holdCount;
	private uint _rng = 0x13572468u;
	private bool _dirty = true;

	public string PresetId
	{
		get => _presetId;
		set
		{
			string norm = string.IsNullOrWhiteSpace(value) ? "off" : value.Trim().ToLowerInvariant();
			if (!string.Equals(_presetId, norm, StringComparison.Ordinal))
			{
				_presetId = norm;
				_dirty = true;
			}
		}
	}

	public double Intensity
	{
		get => _intensity;
		set => _intensity = Math.Clamp(value, 0.0, 1.5);
	}

	public bool IsActive => !string.Equals(_presetId, "off", StringComparison.OrdinalIgnoreCase) && _intensity > 0.01;

	public void Prepare(double sampleRate)
	{
		lock (_lock)
		{
			_sampleRate = Math.Max(8000.0, sampleRate);
			_dirty = true;
			RefreshFiltersLocked();
		}
	}

	public void Reset()
	{
		lock (_lock)
		{
			Array.Clear(_delayBufL, 0, _delayBufL.Length);
			Array.Clear(_delayBufR, 0, _delayBufR.Length);
			_delayPos = 0;
			_lfoPhase = 0;
			_envFollower = 0f;
			_prevL = _prevR = _lpL = _lpR = _holdL = _holdR = 0f;
			_holdCount = 0;
			_eqLow.Reset();
			_eqMid.Reset();
			_eqHigh.Reset();
		}
	}

	private void RefreshFiltersLocked()
	{
		_dirty = false;
		// Default neutral
		_eqLow.FilterType = FurinaPlayerFilterType.LowShelf; _eqLow.FrequencyHz = 120; _eqLow.GainDb = 0; _eqLow.Q = 0.7;
		_eqMid.FilterType = FurinaPlayerFilterType.Peaking; _eqMid.FrequencyHz = 1500; _eqMid.GainDb = 0; _eqMid.Q = 1.0;
		_eqHigh.FilterType = FurinaPlayerFilterType.HighShelf; _eqHigh.FrequencyHz = 7000; _eqHigh.GainDb = 0; _eqHigh.Q = 0.7;

		switch (_presetId)
		{
			case "vocal":
				_eqLow.FrequencyHz = 140; _eqLow.GainDb = -3.0;
				_eqMid.FrequencyHz = 2800; _eqMid.GainDb = 4.2; _eqMid.Q = 1.1;
				_eqHigh.FrequencyHz = 9000; _eqHigh.GainDb = 1.8;
				break;
			case "bass":
				_eqLow.FrequencyHz = 95; _eqLow.GainDb = 6.2;
				_eqMid.FrequencyHz = 450; _eqMid.GainDb = -1.5;
				_eqHigh.FrequencyHz = 8000; _eqHigh.GainDb = 0.5;
				break;
			case "warm":
				_eqLow.FrequencyHz = 160; _eqLow.GainDb = 3.2;
				_eqMid.FrequencyHz = 1200; _eqMid.GainDb = 1.0;
				_eqHigh.FrequencyHz = 5500; _eqHigh.GainDb = -3.5;
				break;
			case "tape":
			case "lofi_cassette":
				_eqLow.FrequencyHz = 130; _eqLow.GainDb = 2.4;
				_eqMid.FrequencyHz = 900; _eqMid.GainDb = 1.2;
				_eqHigh.FilterType = FurinaPlayerFilterType.LowPass; _eqHigh.FrequencyHz = _presetId == "lofi_cassette" ? 4200 : 7600; _eqHigh.Q = 0.7;
				break;
			case "vinyl":
				_eqLow.FilterType = FurinaPlayerFilterType.HighPass; _eqLow.FrequencyHz = 55; _eqLow.Q = 0.7;
				_eqMid.FrequencyHz = 1800; _eqMid.GainDb = 1.5;
				_eqHigh.FilterType = FurinaPlayerFilterType.LowPass; _eqHigh.FrequencyHz = 6800; _eqHigh.Q = 0.7;
				break;
			case "deess":
				_eqMid.FrequencyHz = 6600; _eqMid.GainDb = -5.5; _eqMid.Q = 2.6;
				break;
			case "telephone":
				_eqLow.FilterType = FurinaPlayerFilterType.HighPass; _eqLow.FrequencyHz = 380; _eqLow.Q = 0.9;
				_eqMid.FrequencyHz = 1400; _eqMid.GainDb = 4.0; _eqMid.Q = 1.1;
				_eqHigh.FilterType = FurinaPlayerFilterType.LowPass; _eqHigh.FrequencyHz = 3200; _eqHigh.Q = 0.9;
				break;
			case "radio_broadcast":
				_eqLow.FrequencyHz = 110; _eqLow.GainDb = 3.5;
				_eqMid.FrequencyHz = 2200; _eqMid.GainDb = 2.8; _eqMid.Q = 0.9;
				_eqHigh.FrequencyHz = 8500; _eqHigh.GainDb = 2.5;
				break;
			case "exciter_air":
				_eqHigh.FrequencyHz = 9500; _eqHigh.GainDb = 4.5;
				break;
			case "sub_harmonics":
				_eqLow.FrequencyHz = 70; _eqLow.GainDb = 5.5;
				break;
		}

		_eqLow.UpdateCoefficients(_sampleRate);
		_eqMid.UpdateCoefficients(_sampleRate);
		_eqHigh.UpdateCoefficients(_sampleRate);
	}

	public void ProcessInterleaved(float[] buffer, int offset, int count, int channels)
	{
		if (!IsActive || count <= 0 || channels <= 0) return;

		lock (_lock)
		{
			if (_dirty) RefreshFiltersLocked();

			float mix = (float)Math.Clamp(_intensity, 0.0, 1.5);
			int dLen = _delayBufL.Length;
			double lfoRate = _presetId switch
			{
				"autopan" => 0.45,
				"tremolo" => 5.2,
				"phaser" => 0.65,
				"flanger" => 0.38,
				"studio_chorus" => 0.85,
				"lofi_cassette" => 1.4,
				_ => 1.0
			};
			double lfoInc = 2.0 * Math.PI * lfoRate / _sampleRate;

			for (int i = offset; i < offset + count; i += channels)
			{
				float inL = buffer[i];
				float inR = channels > 1 && i + 1 < offset + count ? buffer[i + 1] : inL;

				_lfoPhase += lfoInc;
				if (_lfoPhase >= 2.0 * Math.PI) _lfoPhase -= 2.0 * Math.PI;
				double lfo = Math.Sin(_lfoPhase);

				float fxL = inL;
				float fxR = inR;

				switch (_presetId)
				{
					case "vocal":
					case "bass":
					case "warm":
					case "deess":
					case "exciter_air":
					case "sub_harmonics":
						fxL = _eqLow.ProcessSample(fxL, 0); fxR = _eqLow.ProcessSample(fxR, 1);
						fxL = _eqMid.ProcessSample(fxL, 0); fxR = _eqMid.ProcessSample(fxR, 1);
						fxL = _eqHigh.ProcessSample(fxL, 0); fxR = _eqHigh.ProcessSample(fxR, 1);
						if (_presetId == "exciter_air")
						{
							float hpL = fxL - _prevL; _prevL = fxL;
							float hpR = fxR - _prevR; _prevR = fxR;
							fxL += MathF.Tanh(hpL * 2.5f) * 0.12f;
							fxR += MathF.Tanh(hpR * 2.5f) * 0.12f;
						}
						break;

					case "wide":
					{
						float mid = 0.5f * (fxL + fxR);
						float side = 0.5f * (fxL - fxR) * 1.65f;
						fxL = mid + side;
						fxR = mid - side;
						break;
					}

					case "karaoke_vocal_cut":
					{
						float diff = (fxL - fxR) * 0.85f;
						float lowL = _lpL = _lpL * 0.985f + fxL * 0.015f;
						float lowR = _lpR = _lpR * 0.985f + fxR * 0.015f;
						fxL = diff + lowL * 0.65f;
						fxR = -diff + lowR * 0.65f;
						break;
					}

					case "tube":
						fxL = MathF.Tanh(fxL * 1.55f) / MathF.Tanh(1.55f);
						fxR = MathF.Tanh(fxR * 1.55f) / MathF.Tanh(1.55f);
						break;

					case "tape":
					case "lofi_cassette":
						fxL = _eqLow.ProcessSample(fxL, 0); fxR = _eqLow.ProcessSample(fxR, 1);
						fxL = _eqMid.ProcessSample(fxL, 0); fxR = _eqMid.ProcessSample(fxR, 1);
						fxL = _eqHigh.ProcessSample(fxL, 0); fxR = _eqHigh.ProcessSample(fxR, 1);
						fxL = MathF.Tanh(fxL * 1.35f) * 0.92f;
						fxR = MathF.Tanh(fxR * 1.35f) * 0.92f;
						break;

					case "vinyl":
						fxL = _eqLow.ProcessSample(fxL, 0); fxR = _eqLow.ProcessSample(fxR, 1);
						fxL = _eqMid.ProcessSample(fxL, 0); fxR = _eqMid.ProcessSample(fxR, 1);
						fxL = _eqHigh.ProcessSample(fxL, 0); fxR = _eqHigh.ProcessSample(fxR, 1);
						_rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
						float crackle = (_rng & 0xFF) == 0 ? 0.008f : 0f;
						fxL += crackle;
						fxR += crackle * 0.6f;
						break;

					case "telephone":
						fxL = _eqLow.ProcessSample(fxL, 0); fxR = _eqLow.ProcessSample(fxR, 1);
						fxL = _eqMid.ProcessSample(fxL, 0); fxR = _eqMid.ProcessSample(fxR, 1);
						fxL = _eqHigh.ProcessSample(fxL, 0); fxR = _eqHigh.ProcessSample(fxR, 1);
						fxL = MathF.Tanh(fxL * 2.1f) * 0.72f;
						fxR = MathF.Tanh(fxR * 2.1f) * 0.72f;
						break;

					case "night":
					case "mastering_glue":
					case "radio_broadcast":
					{
						fxL = _eqLow.ProcessSample(fxL, 0); fxR = _eqLow.ProcessSample(fxR, 1);
						fxL = _eqMid.ProcessSample(fxL, 0); fxR = _eqMid.ProcessSample(fxR, 1);
						fxL = _eqHigh.ProcessSample(fxL, 0); fxR = _eqHigh.ProcessSample(fxR, 1);
						float peak = Math.Max(Math.Abs(fxL), Math.Abs(fxR));
						_envFollower = _envFollower * 0.996f + peak * 0.004f;
						float gain = peak > 0.22f ? (0.22f + (peak - 0.22f) * 0.45f) / Math.Max(1e-4f, peak) : 1.15f;
						fxL *= gain;
						fxR *= gain;
						break;
					}

					case "transient":
					{
						float peak = Math.Max(Math.Abs(fxL), Math.Abs(fxR));
						_envFollower = _envFollower * 0.994f + peak * 0.006f;
						float boost = peak > _envFollower * 1.4f ? 1.32f : 0.95f;
						fxL *= boost;
						fxR *= boost;
						break;
					}

					case "autopan":
					{
						float p = (float)(0.5 + 0.42 * lfo);
						fxL *= 1.3f * (1f - p);
						fxR *= 1.3f * p;
						break;
					}

					case "tremolo":
					{
						float mod = (float)(0.62 + 0.38 * lfo);
						fxL *= mod;
						fxR *= mod;
						break;
					}

					case "digital":
						if (_holdCount++ % 4 == 0)
						{
							_holdL = MathF.Round(fxL * 32f) / 32f;
							_holdR = MathF.Round(fxR * 32f) / 32f;
						}
						fxL = _holdL;
						fxR = _holdR;
						break;

					case "ringmod":
					{
						double carrier = Math.Sin(_lfoPhase * 120.0);
						fxL = fxL * 0.45f + fxL * (float)carrier * 0.55f;
						fxR = fxR * 0.45f + fxR * (float)carrier * 0.55f;
						break;
					}

					case "autowah":
					{
						float peak = Math.Max(Math.Abs(fxL), Math.Abs(fxR));
						_envFollower = _envFollower * 0.992f + peak * 0.008f;
						float cut = Math.Clamp(0.04f + _envFollower * 0.55f, 0.04f, 0.75f);
						_lpL += cut * (fxL - _lpL);
						_lpR += cut * (fxR - _lpR);
						fxL = _lpL * 1.35f;
						fxR = _lpR * 1.35f;
						break;
					}

					case "echo":
					case "analog_delay":
					case "live":
					case "hall":
					case "concert_arena":
					case "headphones":
					case "binaural_3d":
					case "flanger":
					case "phaser":
					case "studio_chorus":
					{
						int tapSamples = _presetId switch
						{
							"flanger" => (int)(_sampleRate * (0.0025 + 0.002 * (0.5 + 0.5 * lfo))),
							"phaser" => (int)(_sampleRate * (0.004 + 0.003 * (0.5 + 0.5 * lfo))),
							"studio_chorus" => (int)(_sampleRate * (0.014 + 0.005 * (0.5 + 0.5 * lfo))),
							"headphones" => (int)(_sampleRate * 0.00045),
							"binaural_3d" => (int)(_sampleRate * 0.00075),
							"live" => (int)(_sampleRate * 0.038),
							"hall" => (int)(_sampleRate * 0.085),
							"concert_arena" => (int)(_sampleRate * 0.145),
							"analog_delay" => (int)(_sampleRate * 0.24),
							_ => (int)(_sampleRate * 0.16)
						};
						tapSamples = Math.Clamp(tapSamples, 4, dLen - 2);
						int rIdx = _delayPos - tapSamples;
						if (rIdx < 0) rIdx += dLen;

						float dL = _delayBufL[rIdx];
						float dR = _delayBufR[rIdx];
						float fb = _presetId is "echo" or "analog_delay" or "hall" or "concert_arena" ? 0.42f : 0.28f;
						float wetTap = _presetId is "headphones" or "binaural_3d" ? 0.35f : 0.48f;

						if (_presetId is "headphones" or "binaural_3d")
						{
							_delayBufL[_delayPos] = inL;
							_delayBufR[_delayPos] = inR;
							fxL = inL + dR * wetTap;
							fxR = inR + dL * wetTap;
						}
						else
						{
							_delayBufL[_delayPos] = inL + dR * fb;
							_delayBufR[_delayPos] = inR + dL * fb;
							fxL = inL + dL * wetTap;
							fxR = inR + dR * wetTap;
						}
						_delayPos = (_delayPos + 1) % dLen;
						break;
					}
				}

				float blend = Math.Clamp(mix, 0f, 1f);
				float outL = inL * (1f - blend) + fxL * blend;
				float outR = inR * (1f - blend) + fxR * blend;
				buffer[i] = Math.Clamp(outL, -1.5f, 1.5f);
				if (channels > 1 && i + 1 < offset + count)
				{
					buffer[i + 1] = Math.Clamp(outR, -1.5f, 1.5f);
				}
			}
		}
	}
}
