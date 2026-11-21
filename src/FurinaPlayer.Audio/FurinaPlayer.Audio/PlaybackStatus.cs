using System;
using System.Collections.Generic;
using FurinaPlayer.Audio.Core;
using FurinaPlayer.Core.Models;

namespace FurinaPlayer.Audio;

public sealed record PlaybackStatus(PlaybackState State, Track? Track, TimeSpan Position, TimeSpan Duration, string? OutputBackend, bool DspActive, PlaybackErrorKind ErrorKind, string? ErrorMessage, IReadOnlyList<string> Warnings, int RecoveryCount);
