namespace SonicWave.Audio.Output;

public sealed record AudioDeviceInfo(string Id, string Name, bool IsDefault, string Kind);
