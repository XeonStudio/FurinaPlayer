using System;
using System.Collections.Generic;
using SonicWave.Audio.Core;
using SonicWave.Core.Models;

namespace SonicWave.Audio;

public sealed record PlaybackStatus(PlaybackState State, Track? Track, TimeSpan Position, TimeSpan Duration, string? OutputBackend, bool DspActive, PlaybackErrorKind ErrorKind, string? ErrorMessage, IReadOnlyList<string> Warnings, int RecoveryCount);
