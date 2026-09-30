using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SonicWave.Audio.Output;

public sealed class DeviceManager : IDisposable
{
	private MMDeviceEnumerator? _enumerator;

	public List<AudioDeviceInfo> EnumerateWasapiDevices()
	{
		if (_enumerator == null)
		{
			_enumerator = new MMDeviceEnumerator();
		}
		List<AudioDeviceInfo> list = new List<AudioDeviceInfo>();
		try
		{
			foreach (MMDevice item in _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
			{
				list.Add(new AudioDeviceInfo(item.ID, item.FriendlyName, IsDefault: false, "WASAPI"));
				item.Dispose();
			}
			try
			{
				using MMDevice mMDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
				for (int i = 0; i < list.Count; i++)
				{
					if (string.Equals(list[i].Id, mMDevice.ID, StringComparison.OrdinalIgnoreCase))
					{
						list[i] = list[i]with
						{
							IsDefault = true
						};
					}
				}
			}
			catch
			{
			}
		}
		catch
		{
		}
		return list;
	}

	public List<AudioDeviceInfo> EnumerateAsioDevices()
	{
		List<AudioDeviceInfo> list = new List<AudioDeviceInfo>();
		try
		{
			string[] driverNames = AsioOut.GetDriverNames();
			foreach (string text in driverNames)
			{
				list.Add(new AudioDeviceInfo(text, text, IsDefault: false, "ASIO"));
			}
		}
		catch
		{
		}
		return list;
	}

	public (int SampleRate, int BitsPerSample)? GetDeviceMixFormat(string deviceId)
	{
		try
		{
			if (_enumerator == null)
			{
				_enumerator = new MMDeviceEnumerator();
			}
			using MMDevice mMDevice = _enumerator.GetDevice(deviceId);
			WaveFormat mixFormat = mMDevice.AudioClient.MixFormat;
			return (mixFormat.SampleRate, mixFormat.BitsPerSample);
		}
		catch
		{
			return null;
		}
	}

	public void Dispose()
	{
		_enumerator?.Dispose();
	}
}
