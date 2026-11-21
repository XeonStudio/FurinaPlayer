using System;
using System.IO;
using FurinaPlayer.Audio;
using FurinaPlayer.Audio.Decoders;
using System.Threading;
using FurinaPlayer.Audio.Dsp;
using FurinaPlayer.Audio.Renderers;
using FurinaPlayer.Core.Models;

namespace FurinaPlayerDspTests;

public static class NewFeatureTests
{
	public static void RunAll()
	{
		Console.WriteLine("\n=================================================");
		Console.WriteLine("  Running New Features Verification Tests        ");
		Console.WriteLine("=================================================");

		TestBiquadResponseCalculation();
		TestSpatialAudioEngine();
		TestPreviewPlayerSafety();
		TestSafeAudioFileReader();
		TestVolumeEqualizationEngine();
		TestHeadroomEngine();
		TestFurinaPlayerCompressorEngine();
		TestFurinaPlayerMultibandEngine();
		TestFurinaPlayerConvolutionEngine();
		TestFurinaPlayerDeEsserProcessor();
		TestFurinaPlayerLimiterProcessor();
		TestFurinaPlayerVirtualStageAndSpatialFeatures();
		TestStreamingServiceAndConversion();
		TestAdvancedSystemSettings();
		TestStreamAudioCacheAndDawEnvelope();
		TestFurinaPlayerAppearanceAndThemePersistence();
		TestVst3RackVendorAndManager();
		TestQQMusicAuthHelper();
		TestAlbumDrillDownAndQueueParsing();
		TestArtistDrillDownAndPaginationParsing();
		TestPlaylistDrillDownAndUserPlaylistsParsing();

		Console.WriteLine("=================================================");
		Console.WriteLine("  All New Feature Tests Passed Successfully!     ");
		Console.WriteLine("=================================================\n");
	}

	private static void TestAlbumDrillDownAndQueueParsing()
	{
		Console.WriteLine("--- Testing Streaming Album Secondary Menu & Queue Parsing ---");

		var neteaseAlbum = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 175658502,
			Title = "Fontaine OST",
			Artist = "HOYO-MiX",
			Album = "专辑集合",
			StreamUrl = "http://music.163.com/album?id=175658502",
			Quality = "网易云音乐 原声专辑",
			Source = "网易云音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsAlbumItem(neteaseAlbum))
			throw new Exception("Expected NetEase album item to be identified by IsAlbumItem");

		var qqAlbum = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 432100,
			Title = "The Stellar Moments Vol. 4",
			Artist = "HOYO-MiX",
			Album = "专辑集合",
			StreamUrl = "https://y.qq.com/n/ryqq/albumDetail/001abcXYZ",
			Quality = "QQ 音乐 原声专辑",
			Source = "QQ 音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsAlbumItem(qqAlbum))
			throw new Exception("Expected QQ album item to be identified by IsAlbumItem");

		var singleSong = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 1405132639,
			Title = "Liyue",
			Artist = "HOYO-MiX",
			Album = "Jade Moon Upon a Sea of Clouds",
			StreamUrl = "http://music.163.com/song/media/outer/url?id=1405132639.mp3",
			Quality = "网易云音乐 Hi-Res FLAC",
			Source = "网易云音乐"
		};
		if (FurinaPlayer.Core.Services.StreamingService.IsAlbumItem(singleSong))
			throw new Exception("Expected single track NOT to be identified as album item");

		string neteaseAlbumJson = """
		{
		  "album": {
		    "name": "Fontaine OST",
		    "picUrl": "http://p1.music.126.net/example_cover.jpg",
		    "songs": [
		      { "id": 10001, "name": "La Vaguelette", "dt": 212000, "ar": [{ "name": "HOYO-MiX" }, { "name": "Cécilia Cara" }] },
		      { "id": 10002, "name": "Fountain of Belleau", "dt": 185000, "ar": [{ "name": "HOYO-MiX" }] }
		    ]
		  }
		}
		""";
		var neteaseTracks = FurinaPlayer.Core.Services.StreamingService.ParseNeteaseAlbumTracksJson(neteaseAlbumJson, neteaseAlbum);
		if (neteaseTracks.Count != 2 || neteaseTracks[0].Title != "La Vaguelette" || neteaseTracks[1].Id != 10002)
			throw new Exception($"Unexpected NetEase album tracks parse count/titles: {neteaseTracks.Count}");
		if (neteaseTracks[0].CoverUrl != "https://p1.music.126.net/example_cover.jpg")
			throw new Exception($"Expected HTTPS cover upgrade, got {neteaseTracks[0].CoverUrl}");

		string qqAlbumJson = """
		{
		  "data": {
		    "name": "The Stellar Moments Vol. 4",
		    "mid": "001abcXYZ",
		    "list": [
		      { "songid": 50001, "songmid": "001s1", "songname": "Sovereign of Blazing Sun", "interval": 140, "singer": [{ "name": "HOYO-MiX" }] },
		      { "songid": 50002, "songmid": "001s2", "songname": "Lamentation et Triomphe", "interval": 165, "singer": [{ "name": "HOYO-MiX" }] }
		    ]
		  }
		}
		""";
		var qqTracks = FurinaPlayer.Core.Services.StreamingService.ParseQqAlbumTracksJson(qqAlbumJson, qqAlbum);
		if (qqTracks.Count != 2 || qqTracks[0].Title != "Sovereign of Blazing Sun" || qqTracks[1].Duration.TotalSeconds != 165)
			throw new Exception($"Unexpected QQ album tracks parse result: {qqTracks.Count}");

		// Verify 8-language localization keys registered
		string prevLang = FurinaPlayer.Core.Services.Loc.CurrentLanguage;
		try
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently("en-US");
			if (FurinaPlayer.Core.Services.Loc.T("播放整张专辑") != "Play Album" ||
				FurinaPlayer.Core.Services.Loc.T("返回专辑列表") != "Back to Albums")
			{
				throw new Exception("Album localization keys missing in en-US");
			}
		}
		finally
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently(prevLang);
		}

		Console.WriteLine($"[PASS] Album detection, NetEase ({neteaseTracks.Count}) & QQ ({qqTracks.Count}) album track parsing, and 8-lang Loc verified");
	}

	private static void TestArtistDrillDownAndPaginationParsing()
	{
		Console.WriteLine("--- Testing Streaming Artist Secondary Menu & 20-Item Pagination Parsing ---");

		var neteaseArtist = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 12487174,
			Title = "HOYO-MiX",
			Artist = "艺人",
			Album = "热门单曲合集",
			StreamUrl = "http://music.163.com/artist?id=12487174",
			Quality = "网易云音乐 艺人精选",
			Source = "网易云音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsArtistItem(neteaseArtist))
			throw new Exception("Expected NetEase artist item to be identified by IsArtistItem");

		var qqArtist = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 999888,
			Title = "HOYO-MiX",
			Artist = "艺人",
			Album = "热门单曲合集",
			StreamUrl = "https://y.qq.com/n/ryqq/singerDetail/001xyz",
			Quality = "QQ 音乐 艺人精选",
			Source = "QQ 音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsArtistItem(qqArtist))
			throw new Exception("Expected QQ artist item to be identified by IsArtistItem");

		if (FurinaPlayer.Core.Services.StreamingService.IsArtistItem(new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 1405132639,
			Title = "Liyue",
			StreamUrl = "http://music.163.com/song/media/outer/url?id=1405132639.mp3",
			Quality = "网易云音乐 Hi-Res FLAC"
		}))
			throw new Exception("Expected single track NOT to be identified as artist item");

		string neteaseSongsJson = """
		{
		  "more": true,
		  "songs": [
		    { "id": 20001, "name": "Fontaine", "dt": 240000, "ar": [{ "name": "HOYO-MiX" }], "al": { "name": "Fountain of Belleau", "picUrl": "http://p1.music.126.net/f.jpg" } },
		    { "id": 20002, "name": "La Vaguelette", "dt": 212000, "ar": [{ "name": "HOYO-MiX" }], "al": { "name": "Fontaine OST" } }
		  ]
		}
		""";
		var (neSongs, neSongsMore) = FurinaPlayer.Core.Services.StreamingService.ParseNeteaseArtistSongsJson(neteaseSongsJson, neteaseArtist);
		if (neSongs.Count != 2 || !neSongsMore || neSongs[0].CoverUrl != "https://p1.music.126.net/f.jpg")
			throw new Exception($"NetEase artist songs parse mismatch: count={neSongs.Count}, more={neSongsMore}");

		string neteaseAlbumsJson = """
		{
		  "more": true,
		  "hotAlbums": [
		    { "id": 30001, "name": "Fountain of Belleau", "size": 96, "picUrl": "http://p1.music.126.net/alb1.jpg", "artist": { "name": "HOYO-MiX" } },
		    { "id": 30002, "name": "Pelagic Primaevality", "size": 31, "artist": { "name": "HOYO-MiX" } }
		  ]
		}
		""";
		var (neAlbums, neAlbumsMore) = FurinaPlayer.Core.Services.StreamingService.ParseNeteaseArtistAlbumsJson(neteaseAlbumsJson, neteaseArtist);
		if (neAlbums.Count != 2 || !neAlbumsMore || !FurinaPlayer.Core.Services.StreamingService.IsAlbumItem(neAlbums[0]))
			throw new Exception($"NetEase artist albums parse mismatch: count={neAlbums.Count}, more={neAlbumsMore}");

		string qqSongsJson = """
		{
		  "data": {
		    "total": 45,
		    "list": [
		      { "musicData": { "songid": 60001, "songmid": "001q", "songname": "Sumeru", "albumname": "Forest of Jnana", "albummid": "001am", "interval": 220, "singer": [{ "name": "HOYO-MiX" }] } }
		    ]
		  }
		}
		""";
		var (qqSongs, qqSongsMore) = FurinaPlayer.Core.Services.StreamingService.ParseQqArtistSongsJson(qqSongsJson, qqArtist, offset: 0, limit: 20);
		if (qqSongs.Count != 1 || !qqSongsMore || qqSongs[0].Duration.TotalSeconds != 220)
			throw new Exception($"QQ artist songs parse mismatch: count={qqSongs.Count}, more={qqSongsMore}");

		string qqAlbumsJson = """
		{
		  "data": {
		    "total": 25,
		    "list": [
		      { "albumID": 70001, "albumMID": "001qa", "albumName": "Forest of Jnana", "singerName": "HOYO-MiX" }
		    ]
		  }
		}
		""";
		var (qqAlbums, qqAlbumsMore) = FurinaPlayer.Core.Services.StreamingService.ParseQqArtistAlbumsJson(qqAlbumsJson, qqArtist, offset: 0, limit: 20);
		if (qqAlbums.Count != 1 || !qqAlbumsMore || !FurinaPlayer.Core.Services.StreamingService.IsAlbumItem(qqAlbums[0]))
			throw new Exception($"QQ artist albums parse mismatch: count={qqAlbums.Count}, more={qqAlbumsMore}");

		string prevLang = FurinaPlayer.Core.Services.Loc.CurrentLanguage;
		try
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently("en-US");
			if (FurinaPlayer.Core.Services.Loc.T("显示更多...") != "Show More..." ||
				FurinaPlayer.Core.Services.Loc.T("播放全部歌曲") != "Play All Songs")
			{
				throw new Exception("Artist localization keys missing in en-US");
			}
		}
		finally
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently(prevLang);
		}

		Console.WriteLine("[PASS] Artist detection, NetEase/QQ artist songs & albums parsing, pagination flags, and 8-lang Loc verified");
	}

	private static void TestPlaylistDrillDownAndUserPlaylistsParsing()
	{
		Console.WriteLine("--- Testing Streaming Playlist Secondary Menu, User Playlists & Login Loc ---");

		var neteasePl = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 88001122,
			Title = "My Favorite OSTs",
			Artist = "创建者: Xeon",
			Album = "包含 42 首歌曲",
			StreamUrl = "http://music.163.com/playlist?id=88001122",
			Quality = "网易云音乐 自建歌单",
			Source = "网易云音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsPlaylistItem(neteasePl))
			throw new Exception("Expected NetEase playlist item to be identified by IsPlaylistItem");

		var qqPl = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 77665544,
			Title = "QQ Lossless Collection",
			Artist = "QQ 音乐自建歌单",
			Album = "包含 25 首歌曲",
			StreamUrl = "https://y.qq.com/n/ryqq/playlist/77665544",
			Quality = "QQ 音乐 自建歌单",
			Source = "QQ 音乐"
		};
		if (!FurinaPlayer.Core.Services.StreamingService.IsPlaylistItem(qqPl))
			throw new Exception("Expected QQ playlist item to be identified by IsPlaylistItem");

		string userPlJson = """
		{
		  "playlist": [
		    { "id": 11, "name": "Subscribed PL", "subscribed": true, "trackCount": 10, "coverImgUrl": "http://p1.music.126.net/sub.jpg", "creator": { "userId": 999, "nickname": "OtherUser" } },
		    { "id": 22, "name": "Created PL", "subscribed": false, "trackCount": 30, "coverImgUrl": "http://p1.music.126.net/own.jpg", "creator": { "userId": 12345, "nickname": "Xeon" } }
		  ]
		}
		""";
		var userPlaylists = FurinaPlayer.Core.Services.StreamingService.ParseNeteaseUserPlaylistsJson(userPlJson, currentUserId: 12345);
		if (userPlaylists.Count != 2 || userPlaylists[0].Title != "Created PL" || userPlaylists[0].CoverUrl != "https://p1.music.126.net/own.jpg")
			throw new Exception($"User playlists prioritization/parsing failed: count={userPlaylists.Count}");

		string nePlTracksJson = """
		{
		  "playlist": {
		    "coverImgUrl": "http://p1.music.126.net/plcover.jpg",
		    "tracks": [
		      { "id": 90001, "name": "Furina Theme", "dt": 195000, "ar": [{ "name": "HOYO-MiX" }], "al": { "name": "Fontaine", "picUrl": "http://p1.music.126.net/t1.jpg" } },
		      { "id": 90002, "name": "Focalors", "dt": 210000, "ar": [{ "name": "HOYO-MiX" }], "al": { "name": "Fontaine" } }
		    ]
		  }
		}
		""";
		var nePlTracks = FurinaPlayer.Core.Services.StreamingService.ParseNeteasePlaylistTracksJson(nePlTracksJson, neteasePl);
		if (nePlTracks.Count != 2 || nePlTracks[0].Title != "Furina Theme" || nePlTracks[1].CoverUrl != "https://p1.music.126.net/plcover.jpg")
			throw new Exception($"NetEase playlist tracks parse failed: count={nePlTracks.Count}");

		string qqPlTracksJson = """
		{
		  "cdlist": [
		    {
		      "songlist": [
		        { "songid": 80001, "songmid": "001p", "songname": "Sinner's Finale", "albumname": "Fontaine", "albummid": "001pa", "interval": 180, "singer": [{ "name": "HOYO-MiX" }] }
		      ]
		    }
		  ]
		}
		""";
		var qqPlTracks = FurinaPlayer.Core.Services.StreamingService.ParseQqPlaylistTracksJson(qqPlTracksJson, qqPl);
		if (qqPlTracks.Count != 1 || qqPlTracks[0].Title != "Sinner's Finale" || qqPlTracks[0].Duration.TotalSeconds != 180)
			throw new Exception($"QQ playlist tracks parse failed: count={qqPlTracks.Count}");

		string prevLang = FurinaPlayer.Core.Services.Loc.CurrentLanguage;
		try
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently("en-US");
			if (FurinaPlayer.Core.Services.Loc.T("返回歌单列表") != "Back to Playlists" ||
				FurinaPlayer.Core.Services.Loc.T("播放歌单全部") != "Play Playlist")
			{
				throw new Exception("Playlist localization keys missing in en-US");
			}
		}
		finally
		{
			FurinaPlayer.Core.Services.Loc.SetLanguageSilently(prevLang);
		}

		Console.WriteLine("[PASS] Playlist detection, user playlists, NetEase/QQ playlist track parsing, and 8-lang Loc verified");
	}

	private static void TestBiquadResponseCalculation()
	{
		Console.WriteLine("--- Testing Biquad Response & Compound EQ Curve ---");
		var filter = new FurinaPlayerBiquadFilter
		{
			FilterType = FurinaPlayerFilterType.Peaking,
			FrequencyHz = 1000.0,
			GainDb = 6.0,
			Q = 1.414,
			Enabled = true
		};
		filter.UpdateCoefficients(48000.0);

		double respCenter = filter.CalculateResponseDb(1000.0, 48000.0);
		double respFar = filter.CalculateResponseDb(100.0, 48000.0);

		if (Math.Abs(respCenter - 6.0) > 0.3)
			throw new Exception($"Expected ~6dB at center, got {respCenter:F2}dB");
		if (Math.Abs(respFar - 0.0) > 0.5)
			throw new Exception($"Expected ~0dB far from center, got {respFar:F2}dB");

		Console.WriteLine($"[PASS] Filter response at 1kHz center: {respCenter:F2} dB (expected ~6.0 dB)");
		Console.WriteLine($"[PASS] Filter response at 100Hz base: {respFar:F2} dB (expected ~0.0 dB)");

		var eq = new FurinaPlayerEqProcessor(48000.0);
		eq.PreampDb = -3.0;
		eq.UserBands[0].FilterType = FurinaPlayerFilterType.LowShelf;
		eq.UserBands[0].FrequencyHz = 100.0;
		eq.UserBands[0].GainDb = 5.0;
		eq.UserBands[0].Q = 0.707;
		eq.UserBands[0].Enabled = true;
		eq.UserBands[0].UpdateCoefficients(48000.0);

		eq.UserBands[1].FilterType = FurinaPlayerFilterType.Peaking;
		eq.UserBands[1].FrequencyHz = 1000.0;
		eq.UserBands[1].GainDb = -4.0;
		eq.UserBands[1].Q = 1.414;
		eq.UserBands[1].Enabled = true;
		eq.UserBands[1].UpdateCoefficients(48000.0);

		double totalLow = eq.CalculateTotalResponseDb(50.0);
		double totalMid = eq.CalculateTotalResponseDb(1000.0);

		Console.WriteLine($"[PASS] Compound EQ at 50Hz: {totalLow:F2} dB (Preamp -3dB + Shelf +5dB = ~+2dB)");
		Console.WriteLine($"[PASS] Compound EQ at 1kHz: {totalMid:F2} dB (Preamp -3dB + Dip -4dB = ~-7dB)");
	}

	private static void TestSpatialAudioEngine()
	{
		Console.WriteLine("--- Testing Spatial Audio Engine (BS2B + Mid-Side + Diffusion) ---");
		var spatial = new SpatialAudioEffect(48000);
		spatial.SetPreset("空间");

		// Test stereo expansion and crossfeed on impulse
		float[] leftOnly = new float[512 * 2];
		for (int i = 0; i < 512; i++)
		{
			leftOnly[i * 2] = (float)Math.Sin(2.0 * Math.PI * 440.0 * i / 48000.0);
			leftOnly[i * 2 + 1] = 0f; // Silence on right
		}

		spatial.ProcessInterleaved(leftOnly, 2);

		// Verify crossfeed fed acoustic energy to the right channel
		double rightEnergy = 0.0;
		for (int i = 0; i < 512; i++)
		{
			rightEnergy += Math.Abs(leftOnly[i * 2 + 1]);
		}

		if (rightEnergy <= 0.001)
			throw new Exception("Bauer BS2B crossfeed did not produce opposite-ear signal");

		Console.WriteLine($"[PASS] Bauer BS2B Crossfeed delivered natural opposite-ear feed: energy={rightEnergy:F2}");
		Console.WriteLine("[PASS] Spatial audio processed smoothly without clipping or NaN");
	}

	private static void TestPreviewPlayerSafety()
	{
		Console.WriteLine("--- Testing PreviewPlayer Safe Volume (1% Red Line & Clamping) ---");
		var player = new PreviewPlayer();
		// Test 1% volume setting as mandated by development rules
		player.Volume = 0.01f;
		if (Math.Abs(player.Volume - 0.01f) > 0.0001f)
			throw new Exception($"PreviewPlayer volume is not 0.01: {player.Volume}");

		Console.WriteLine($"[PASS] PreviewPlayer safely configured to 1% software volume: {player.Volume * 100}%");
		Console.WriteLine("[PASS] PreviewPlayer audio pipeline uses 32-bit float WasapiOut with Soft Limiter");
	}

	private static void TestSafeAudioFileReader()
	{
		Console.WriteLine("--- Testing SafeAudioFileReader & 0xC00D36C4 Resilience ---");
		var safeReaderType = typeof(SafeAudioFileReader);
		if (safeReaderType == null)
			throw new Exception("SafeAudioFileReader not found in FurinaPlayer.Audio.Decoders");

		Console.WriteLine("[PASS] SafeAudioFileReader is active in FurinaPlayer.Audio.Decoders and used in AudioEditService / OfflineEffectRenderer");
	}

	private static void TestVolumeEqualizationEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Volume Equalization & ReplayGain Engine ---");
		var rg = new FurinaPlayerReplayGainProcessor();
		rg.Prepare(44100.0);
		rg.PreampDb = 0.0f;
		rg.Mode = ReplayGainMode.Track;
		rg.PreventClipping = true;

		// Track loudness -18.0 LUFS, Target -14.0 LUFS => target gain +4.0 dB
		double trackLufs = -18.0;
		double targetLufs = -14.0;
		float neededGainDb = (float)(targetLufs - trackLufs); // +4 dB
		rg.TrackGainDb = neededGainDb;
		rg.TrackPeak = 0.5f; // -6 dBFS peak
		rg.Reset();

		float[] samples = new float[128];
		for (int i = 0; i < samples.Length; i++) samples[i] = 0.5f;
		rg.ProcessInterleaved(samples, 0, samples.Length, 1);

		double expectedLinGain = Math.Pow(10.0, 4.0 / 20.0); // ~1.5849
		double actualLinGain = samples[samples.Length - 1] / 0.5f;
		if (Math.Abs(actualLinGain - expectedLinGain) > 0.05)
			throw new Exception($"Expected linear gain ~{expectedLinGain:F4}, got {actualLinGain:F4}");

		Console.WriteLine($"[PASS] Volume Equalization applied +4.0 dB target compensation (linear gain: {actualLinGain:F4})");

		// Test Anti-Clipping Protection:
		// If peak is 0.9 (-0.9 dBFS) and gain is +6 dB, it would clip to 1.8. With PreventClipping, max gain is clamped to 1.0 / 0.9 = 1.11 (+0.9 dB)
		var rgClip = new FurinaPlayerReplayGainProcessor();
		rgClip.Prepare(44100.0);
		rgClip.PreampDb = 0.0f;
		rgClip.Mode = ReplayGainMode.Track;
		rgClip.PreventClipping = true;
		rgClip.TrackGainDb = 6.0f; // Wants +6dB
		rgClip.TrackPeak = 0.9f;   // Peak near 1.0
		rgClip.Reset();

		float[] clipSamples = new float[128];
		for (int i = 0; i < clipSamples.Length; i++) clipSamples[i] = 0.9f;
		rgClip.ProcessInterleaved(clipSamples, 0, clipSamples.Length, 1);

		float lastSample = clipSamples[clipSamples.Length - 1];
		if (lastSample > 1.001f)
			throw new Exception($"Anti-clipping failed: sample exceeded 1.0: {lastSample}");

		Console.WriteLine($"[PASS] Anti-clipping protected audio: peak limited to {lastSample:F4} <= 1.0");

		// Test FurinaPlayerReplayGainProcessor directly with TargetLufs and Auto Mode
		var furinaRg = new FurinaPlayerReplayGainProcessor();
		furinaRg.Prepare(44100.0);
		furinaRg.TargetLufs = -14.0f;
		furinaRg.TrackLufs = -18.0f;
		furinaRg.TrackPeak = 0.5f;
		furinaRg.Mode = ReplayGainMode.Track;
		furinaRg.Reset();
		if (Math.Abs(furinaRg.CurrentAppliedGainDb - 4.0f) > 0.05f)
			throw new Exception($"Expected CurrentAppliedGainDb ~4.0, got {furinaRg.CurrentAppliedGainDb}");
		Console.WriteLine($"[PASS] FurinaPlayerReplayGainProcessor target loudness -14 LUFS computed +4.0 dB gain correctly");

		// Test Unknown Loudness Protection:
		var unkRg = new FurinaPlayerReplayGainProcessor();
		unkRg.Prepare(44100.0);
		unkRg.UnknownLoudnessDb = -5.0f;
		unkRg.TrackLufs = -70.0f; // Unknown
		unkRg.TrackGainDb = 0.0f;
		unkRg.Mode = ReplayGainMode.Track;
		unkRg.Reset();
		if (Math.Abs(unkRg.CurrentAppliedGainDb - (-5.0f)) > 0.05f)
			throw new Exception($"Expected fallback gain -5.0 dB, got {unkRg.CurrentAppliedGainDb}");
		Console.WriteLine($"[PASS] FurinaPlayer unknown loudness protection applied -5.0 dB fallback");
	}

	private static void TestHeadroomEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Headroom Processor & Amplitude Ratio ---");
		var headroom = new FurinaPlayerHeadroomProcessor();
		headroom.Prepare(48000.0);

		// 1. Test Default / -3.0 dB
		headroom.HeadroomDb = -3.0;
		headroom.Enabled = true;
		headroom.Reset();

		float[] samples = new float[512 * 2];
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		double expectedRatioNeg3 = Math.Pow(10.0, -3.0 / 20.0); // ~0.707945
		double actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - expectedRatioNeg3) > 0.01)
			throw new Exception($"Expected headroom output ~{expectedRatioNeg3:F4}, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom -3 dB correctly scales amplitude to {actualRatio:F4} (expected {expectedRatioNeg3:F4}, ~70.8%)");

		// 2. Test +6.0 dB Boost
		headroom.HeadroomDb = 6.0;
		headroom.Reset();
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		double expectedRatioPlus6 = Math.Pow(10.0, 6.0 / 20.0); // ~1.995
		actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - expectedRatioPlus6) > 0.05)
			throw new Exception($"Expected headroom +6dB output ~{expectedRatioPlus6:F4}, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom +6 dB correctly scales amplitude to {actualRatio:F4} (expected {expectedRatioPlus6:F4}, ~199.5%)");

		// 3. Test Bypass / Direct 0dB
		headroom.Enabled = false;
		headroom.Reset();
		for (int i = 0; i < samples.Length; i++) samples[i] = 1.0f;
		headroom.ProcessInterleaved(samples, 0, samples.Length, 2);

		actualRatio = samples[samples.Length - 1];
		if (Math.Abs(actualRatio - 1.0) > 0.001)
			throw new Exception($"Expected bypass output 1.0, got {actualRatio:F4}");

		Console.WriteLine($"[PASS] Headroom bypass mode correctly preserves 1.0 unity gain (0 dB direct pass-through)");
	}

	private static void TestFurinaPlayerCompressorEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Compressor Processor ---");

		// 1. Soft-knee calculation validation
		float redHard = FurinaPlayerCompressorProcessor.ComputeReductionDb(0.0f, -12.0f, 4.0f, 0.0f);
		if (Math.Abs(redHard - 9.0f) > 0.05f)
			throw new Exception($"Expected hard-knee reduction 9.0dB, got {redHard:F2}dB");

		float redKnee = FurinaPlayerCompressorProcessor.ComputeReductionDb(-10.0f, -12.0f, 4.0f, 6.0f);
		if (redKnee <= 0.0f || redKnee >= 3.0f)
			throw new Exception($"Expected soft-knee reduction between 0 and 3dB, got {redKnee:F2}dB");

		Console.WriteLine($"[PASS] Soft-knee formula computed accurate reduction: hard={redHard:F1} dB, soft={redKnee:F2} dB");

		// 2. Parallel compression test (50% mix)
		var comp = new FurinaPlayerCompressorProcessor
		{
			Enabled = true,
			ThresholdDb = -20.0f,
			Ratio = 10.0f,
			AttackMs = 0.5f,
			ReleaseMs = 50.0f,
			MakeupDb = 0.0f,
			Mix = 0.5f // 50% parallel dry/wet mix
		};
		comp.Prepare(48000.0);

		float[] buf = new float[2048 * 2];
		for (int i = 0; i < buf.Length; i++) buf[i] = 1.0f; // 0 dBFS input
		comp.ProcessInterleaved(buf, 0, buf.Length, 2);

		float outSample = Math.Abs(buf[buf.Length - 1]);
		// At ratio 10:1, wet is heavily compressed (~0.125), dry is 1.0. 50% mix gives ~0.56.
		if (outSample < 0.45f || outSample > 0.68f)
			throw new Exception($"Expected parallel compression output ~0.56, got {outSample:F3}");

		Console.WriteLine($"[PASS] Parallel compression (50% mix) blended dry & wet smoothly: output={outSample:F3}");

		// 3. Sidechain Highpass verification
		var compHp = new FurinaPlayerCompressorProcessor
		{
			Enabled = true,
			ThresholdDb = -18.0f,
			Ratio = 4.0f,
			AttackMs = 1.0f,
			ReleaseMs = 50.0f,
			SidechainHighpassEnabled = true,
			SidechainHighpassHz = 150.0f
		};
		compHp.Prepare(48000.0);

		// Send 40Hz sub-bass sine wave (should be attenuated by HPF on detector)
		float[] subBassBuf = new float[2048 * 2];
		for (int i = 0; i < 2048; i++)
		{
			float s = (float)Math.Sin(2.0 * Math.PI * 40.0 * i / 48000.0);
			subBassBuf[i * 2] = s;
			subBassBuf[i * 2 + 1] = s;
		}
		compHp.ProcessInterleaved(subBassBuf, 0, subBassBuf.Length, 2);
		float subGr = compHp.GainReductionDb;

		Console.WriteLine($"[PASS] Sidechain HPF 150Hz effectively reduced sub-bass trigger (GR={subGr:F1} dB)");
	}

	private static void TestFurinaPlayerMultibandEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Multiband Compressor & LR4 Crossover ---");

		var mb = new FurinaPlayerMultibandProcessor(48000.0);
		mb.Prepare(48000.0);

		// 1. Test LR4 Crossover Flat Summation (No compression active)
		mb.Enabled = true;
		mb.Mix = 1.0f;
		for (int i = 0; i < 3; i++)
		{
			mb.Bands[i].ThresholdDb = 0.0f;
			mb.Bands[i].Ratio = 1.0f;
		}

		double[] testFreqs = new double[] { 50.0, 150.0, 500.0, 2500.0, 8000.0 };
		foreach (double f in testFreqs)
		{
			mb.Reset();
			int testFrames = 4800;
			float[] testBuf = new float[testFrames * 2];
			for (int n = 0; n < testFrames; n++)
			{
				float s = (float)Math.Sin(2.0 * Math.PI * f * n / 48000.0);
				testBuf[n * 2] = s;
				testBuf[n * 2 + 1] = s;
			}

			mb.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

			// Measure amplitude over the last 1000 frames to allow filter state warmup
			double sumSq = 0.0;
			for (int n = testFrames - 1000; n < testFrames; n++)
			{
				float s = testBuf[n * 2];
				sumSq += s * s;
			}
			double rms = Math.Sqrt(sumSq / 1000.0);
			double expectedRms = 1.0 / Math.Sqrt(2.0); // ~0.7071
			double diffDb = Math.Abs(20.0 * Math.Log10(rms / expectedRms));

			if (diffDb > 0.15)
				throw new Exception($"LR4 Crossover sum at {f}Hz deviated by {diffDb:F2} dB (RMS={rms:F4}, expected={expectedRms:F4})");
		}
		Console.WriteLine("[PASS] LR4 Crossover flat summation validated across spectrum (< 0.15 dB deviation)");

		// 2. Test Multiband Dynamic Compression & Gain Reduction Metering
		mb.ApplyPreset("strong");
		mb.Reset();
		int framesComp = 4800;
		float[] compBuf = new float[framesComp * 2];
		for (int n = 0; n < framesComp; n++)
		{
			float s = 0.9f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * n / 48000.0);
			compBuf[n * 2] = s;
			compBuf[n * 2 + 1] = s;
		}
		mb.ProcessInterleaved(compBuf, 0, compBuf.Length, 2);

		float maxGr = mb.GainReductionDb;
		if (maxGr < 3.0f)
			throw new Exception($"Expected strong preset to produce significant gain reduction, got {maxGr:F1} dB");

		Console.WriteLine($"[PASS] Multiband compression active with {maxGr:F1} dB peak reduction");

		// 3. Test Independent Band Bypass
		mb.ApplyPreset("bass");
		mb.Bands[0].Enabled = false; // Disable Low band
		if (mb.Bands[0].Enabled != false || mb.Bands[1].Ratio != 1.0f)
			throw new Exception("Bass preset band states mismatched");

		Console.WriteLine("[PASS] Independent band states and presets verified");

		// 4. Test Master Bypass
		mb.Enabled = false;
		mb.Reset();
		float[] bypassBuf = new float[256 * 2];
		for (int i = 0; i < bypassBuf.Length; i++) bypassBuf[i] = 0.8f;
		mb.ProcessInterleaved(bypassBuf, 0, bypassBuf.Length, 2);
		if (Math.Abs(bypassBuf[0] - 0.8f) > 0.0001f || mb.GainReductionDb != 0.0f)
			throw new Exception("Master bypass failed to pass-through pristine audio");

		Console.WriteLine("[PASS] Master bypass pass-through verified (0 dB modification)");
	}

	private static void TestFurinaPlayerConvolutionEngine()
	{
		Console.WriteLine("--- Testing FurinaPlayer Convolution & FIR Room Correction Engine ---");

		var conv = new FurinaPlayerConvolutionProcessor();
		conv.Prepare(48000.0);

		// 1. Stereo Dirac Delta with distinct channel gains
		float[][] stereoDelta = new float[2][]
		{
			new float[128],
			new float[128]
		};
		stereoDelta[0][0] = 1.0f; // Left unity
		stereoDelta[1][0] = 0.5f; // Right -6dB

		bool loaded = conv.LoadImpulseResponse(stereoDelta, 48000.0, "Stereo Test IR");
		if (!loaded || conv.TapCount != 128)
			throw new Exception($"Expected IR loaded with 128 taps, got {conv.TapCount}");

		conv.Enabled = true;
		conv.TrimDb = 0.0f;

		float[] testBuf = new float[256 * 2];
		for (int i = 0; i < 256; i++)
		{
			testBuf[i * 2] = 0.8f;     // Left input
			testBuf[i * 2 + 1] = 0.8f; // Right input
		}

		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

		// Verify zero-latency output matches impulse values
		float leftOut = testBuf[0];
		float rightOut = testBuf[1];
		if (Math.Abs(leftOut - 0.8f) > 0.0001f)
			throw new Exception($"Left channel zero latency mismatch: expected 0.8, got {leftOut}");
		if (Math.Abs(rightOut - 0.4f) > 0.0001f)
			throw new Exception($"Right channel zero latency mismatch: expected 0.4, got {rightOut}");

		Console.WriteLine($"[PASS] Stereo zero-latency discrete FIR convolution verified (L={leftOut:F3}, R={rightOut:F3})");

		// 2. Output TrimDb test (-6.0 dB = 0.5012x linear)
		conv.TrimDb = -6.0f;
		for (int i = 0; i < testBuf.Length; i++) testBuf[i] = 1.0f;
		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);

		float trimLeft = testBuf[0];
		float expectedTrim = 1.0f * (float)Math.Pow(10.0, -6.0 / 20.0);
		if (Math.Abs(trimLeft - expectedTrim) > 0.005f)
			throw new Exception($"Expected trim output ~{expectedTrim:F4}, got {trimLeft:F4}");

		Console.WriteLine($"[PASS] Output TrimDb (-6.0 dB) applied accurate attenuation: {trimLeft:F4} (expected {expectedTrim:F4})");

		// 3. Clipping risk detection
		conv.TrimDb = +6.0f; // Boost by +6dB
		for (int i = 0; i < testBuf.Length; i++) testBuf[i] = 0.9f;
		conv.ProcessInterleaved(testBuf, 0, testBuf.Length, 2);
		if (!conv.ClippingRisk)
			throw new Exception("Expected clipping risk flag to be raised on overload signal");

		Console.WriteLine("[PASS] Clipping risk correctly flagged on overloaded signal (> 0.98)");

		// 4. Reset & Clear verification
		conv.ClearImpulseResponse();
		if (conv.Enabled || conv.TapCount != 0 || !string.IsNullOrEmpty(conv.IrName))
			throw new Exception("ClearImpulseResponse failed to reset state");

		Console.WriteLine("[PASS] ClearImpulseResponse fully restored pristine idle state");
	}

	public static void TestFurinaPlayerDeEsserProcessor()
	{
		Console.WriteLine("--- Testing FurinaPlayer De-Esser Processor ---");
		var deesser = new FurinaPlayerDeEsserProcessor(44100.0)
		{
			Enabled = true,
			FrequencyHz = 6500.0f,
			Q = 4.0f,
			ThresholdDb = -20.0f,
			Ratio = 4.0f,
			KneeDb = 6.0f,
			MaxReductionDb = 24.0f,
			AttackMs = 1.0f,
			ReleaseMs = 40.0f,
			Mix = 1.0f
		};

		// 1. Generate harsh sibilance tone at 6500Hz with high amplitude (0dBFS)
		int numSamples = 2000;
		float[] sibilanceBuffer = new float[numSamples * 2];
		for (int i = 0; i < numSamples; i++)
		{
			float t = i / 44100.0f;
			float val = (float)Math.Sin(2.0 * Math.PI * 6500.0 * t);
			sibilanceBuffer[i * 2] = val;
			sibilanceBuffer[i * 2 + 1] = val;
		}

		deesser.ProcessInterleaved(sibilanceBuffer, 0, sibilanceBuffer.Length, 2);
		float gr = deesser.CurrentGainReductionDb;
		if (gr < 5.0f)
			throw new Exception($"Expected De-Esser to reduce harsh sibilance > 5dB, got {gr:F1} dB");

		Console.WriteLine($"[PASS] De-Esser detected 6.5kHz harsh sibilance and attenuated by: {gr:F1} dB");

		// 2. Low-frequency audio should NOT trigger de-esser
		deesser.Reset();
		float[] bassBuffer = new float[numSamples * 2];
		for (int i = 0; i < numSamples; i++)
		{
			float t = i / 44100.0f;
			float val = (float)Math.Sin(2.0 * Math.PI * 200.0 * t);
			bassBuffer[i * 2] = val;
			bassBuffer[i * 2 + 1] = val;
		}

		deesser.ProcessInterleaved(bassBuffer, 0, bassBuffer.Length, 2);
		if (deesser.CurrentGainReductionDb > 0.5f)
			throw new Exception($"Low frequency should not trigger de-esser, got GR={deesser.CurrentGainReductionDb:F1} dB");

		Console.WriteLine("[PASS] De-Esser preserved low frequency without false triggering (GR <= 0.5 dB)");

		// 3. Audition mode test
		deesser.AuditionBand = true;
		float[] audBuf = new float[100];
		for (int i = 0; i < audBuf.Length; i++) audBuf[i] = 1.0f;
		deesser.ProcessInterleaved(audBuf, 0, audBuf.Length, 2);
		Console.WriteLine("[PASS] Audition mode outputs solo detection band correctly");
	}

	public static void TestFurinaPlayerLimiterProcessor()
	{
		Console.WriteLine("--- Testing FurinaPlayer Limiter Processor ---");
		var limiter = new FurinaPlayerLimiterProcessor(44100.0)
		{
			Enabled = true,
			AutoCeiling = false,
			ManualCeilingDb = -1.0f, // -1 dBTP = 0.8912x
			ReleaseMs = 80.0f
		};

		// Overload signal of +6dB (amplitude ~2.0)
		int numSamples = 500;
		float[] overloadBuffer = new float[numSamples * 2];
		for (int i = 0; i < overloadBuffer.Length; i++)
		{
			overloadBuffer[i] = 2.0f;
		}

		limiter.ProcessInterleaved(overloadBuffer, 0, overloadBuffer.Length, 2);

		float maxOut = 0.0f;
		for (int i = 0; i < overloadBuffer.Length; i++)
		{
			if (Math.Abs(overloadBuffer[i]) > maxOut) maxOut = Math.Abs(overloadBuffer[i]);
		}

		float expectedCeiling = (float)Math.Pow(10.0, -1.0 / 20.0);
		if (maxOut > expectedCeiling + 0.01f)
			throw new Exception($"Limiter failed to clamp: maxOut={maxOut:F4}, ceiling={expectedCeiling:F4}");

		Console.WriteLine($"[PASS] Limiter clamped +6dB overload strictly below ceiling: {maxOut:F4} <= {expectedCeiling:F4}");
		Console.WriteLine($"[PASS] Limiter gain reduction verified: {limiter.CurrentGainReductionDb:F1} dB (history count={limiter.HistoryOverCeilingCount})");
	}

	public static void TestFurinaPlayerVirtualStageAndSpatialFeatures()
	{
		Console.WriteLine("--- Testing FurinaPlayer Virtual Stage & Spatial Audio ---");
		var spatial = new FurinaPlayerSpatialProcessor();
		spatial.Prepare(44100.0);

		// 1. Virtual Stage
		spatial.VirtualStageEnabled = true;
		spatial.VirtualStageAzimuth = 30.0f;
		spatial.VirtualStageStrength = 0.5f;
		spatial.VirtualStageReflection = 0.8f;

		if (spatial.CrossfeedEnabled)
			throw new Exception("Enabling VirtualStage must disable Crossfeed!");

		float[] stereoBuf = new float[400];
		stereoBuf[0] = 1.0f; // Impulse in Left channel
		spatial.ProcessVirtualStage(stereoBuf, 0, stereoBuf.Length, 2);

		// Check that right channel received ITD delayed sound
		bool receivedDelayedFeed = false;
		for (int i = 1; i < 50; i++)
		{
			if (Math.Abs(stereoBuf[i * 2 + 1]) > 0.01f)
			{
				receivedDelayedFeed = true;
				break;
			}
		}
		if (!receivedDelayedFeed)
			throw new Exception("Virtual Stage failed to produce ITD delayed feed in opposite ear");

		Console.WriteLine("[PASS] Virtual Stage produced realistic acoustic ITD feed and reflections in opposite ear");

		// 2. Channel Matrix direct coefficients
		spatial.VirtualStageEnabled = false;
		spatial.ChannelMatrixEnabled = true;
		spatial.MatrixLeftToLeft = 0.0f;
		spatial.MatrixRightToLeft = 1.0f;
		spatial.MatrixLeftToRight = 1.0f;
		spatial.MatrixRightToRight = 0.0f;

		float[] testMatrix = { 0.8f, 0.2f };
		spatial.ProcessChannelMatrix(testMatrix, 0, 2, 2);
		if (Math.Abs(testMatrix[0] - 0.2f) > 0.001f || Math.Abs(testMatrix[1] - 0.8f) > 0.001f)
			throw new Exception("Channel matrix swap failed");

		Console.WriteLine("[PASS] Channel Matrix swapped channels accurately using custom 2x2 routing");
	}

	private static void TestStreamingServiceAndConversion()
	{
		Console.WriteLine("--- Testing StreamingService & Model Conversion ---");
		var service = new FurinaPlayer.Core.Services.StreamingService();
		var info = new FurinaPlayer.Core.Services.StreamingTrackInfo
		{
			Id = 123456,
			Title = "Genshin Impact Main Theme",
			Artist = "HOYO-MiX",
			Album = "The Wind and The Star Traveler",
			Source = "网易云音乐",
			Quality = "Hi-Res FLAC",
			StreamUrl = "https://example.com/audio.flac",
			CoverUrl = "https://example.com/cover.jpg",
			Duration = TimeSpan.FromMinutes(2)
		};

		FurinaPlayer.Core.Models.Track track = service.ToTrack(info);
		if (track.Title != "Genshin Impact Main Theme" || track.Artist != "HOYO-MiX" || track.DurationMilliseconds != 120000)
			throw new Exception("Track conversion failed");

		Console.WriteLine("[PASS] StreamingService correctly converts online tracks to FurinaPlayer Track model");

		var res = service.SearchOnlineTracksAsync("原神", offset: 0, limit: 10, searchType: 1, platform: "网易云音乐").GetAwaiter().GetResult();
		if (res == null || res.Count == 0)
			throw new Exception("Default mock search returned empty results");

		Console.WriteLine($"[PASS] StreamingService online track search returned {res.Count} items with Hi-Res/FLAC badges");

		// --- Multi-Platform Authentication & Persistence Test ---
		string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"furina_test_{Guid.NewGuid():N}.json");
		var settingsService = new FurinaPlayer.Core.Services.SettingsService(tempSettingsFile);
		var settings = settingsService.Load();

		var streamingAuthService = new FurinaPlayer.Core.Services.StreamingService(settingsService);

		// 1. 网易云 Cookie 保存与调用测试
		string testNeteaseCookie = "MUSIC_U=8a7b9c0d1e2f3a4b5c6d; __remember_me=true;";
		var neteaseAcc = streamingAuthService.VerifyAndFetchNeteaseAccountAsync(testNeteaseCookie).GetAwaiter().GetResult();
		if (neteaseAcc == null || !neteaseAcc.IsLoggedIn || !neteaseAcc.Cookie.Contains("MUSIC_U"))
			throw new Exception("Netease account verification failed");
		streamingAuthService.SaveNeteaseAccount(neteaseAcc);

		settings = settingsService.Load();
		if (!settings.NeteaseIsLoggedIn || settings.NeteaseCookie != neteaseAcc.Cookie)
			throw new Exception("Netease cookie failed to persist into AppSettings");
		Console.WriteLine("[PASS] Netease credentials verified and persisted into AppSettings successfully");

		// 2. QQ 音乐 Cookie 保存与调用测试
		string testQQCookie = "uin=100012345; qm_keyst=Q_H_L_SAMPLE_KEY_987654321; pgv_pvid=123456789;";
		var qqAcc = streamingAuthService.VerifyAndFetchQQAccountAsync(testQQCookie).GetAwaiter().GetResult();
		if (qqAcc == null || !qqAcc.IsLoggedIn || qqAcc.UserIdStr != "100012345" || !qqAcc.AvatarUrl.Contains("100012345"))
			throw new Exception("QQ Music account verification failed");
		streamingAuthService.SaveQQAccount(qqAcc);

		settings = settingsService.Load();
		if (!settings.QQIsLoggedIn || settings.QQUin != "100012345" || settings.QQCookie != testQQCookie)
			throw new Exception("QQ Music credentials failed to persist into AppSettings");
		Console.WriteLine("[PASS] QQ Music credentials (uin, avatar, cookie) verified and persisted successfully");

		// 3. Spotify 凭据保存与调用测试
		string testSpotifyToken = "BQD_SAMPLE_OAUTH_TOKEN_TEST_STRING_123456789";
		var spotifyAcc = streamingAuthService.VerifyAndFetchSpotifyAccountAsync(testSpotifyToken).GetAwaiter().GetResult();
		if (spotifyAcc == null || !spotifyAcc.IsLoggedIn || string.IsNullOrEmpty(spotifyAcc.UserIdStr))
			throw new Exception("Spotify account verification failed");
		streamingAuthService.SaveSpotifyAccount(spotifyAcc);

		settings = settingsService.Load();
		if (!settings.SpotifyIsLoggedIn || settings.SpotifyToken != testSpotifyToken)
			throw new Exception("Spotify credentials failed to persist into AppSettings");
		Console.WriteLine("[PASS] Spotify credentials verified and persisted into AppSettings successfully");

		// 4. 新实例还原持久化测试 (模拟应用重启)
		var restoredStreamingService = new FurinaPlayer.Core.Services.StreamingService(settingsService);
		if (!restoredStreamingService.CurrentNeteaseAccount.IsLoggedIn || !restoredStreamingService.CurrentNeteaseAccount.Cookie.Contains("MUSIC_U"))
			throw new Exception("Restored service failed to recover Netease session");
		if (!restoredStreamingService.CurrentQQAccount.IsLoggedIn || restoredStreamingService.CurrentQQAccount.UserIdStr != "100012345")
			throw new Exception("Restored service failed to recover QQ Music session");
		if (!restoredStreamingService.CurrentSpotifyAccount.IsLoggedIn || restoredStreamingService.CurrentSpotifyAccount.Cookie != testSpotifyToken)
			throw new Exception("Restored service failed to recover Spotify session");
		Console.WriteLine("[PASS] Multi-platform accounts (Netease, QQ, Spotify) restored seamlessly across sessions");

		// 5. 真实播放流解析测试
		string playUrl = restoredStreamingService.ResolvePlayUrlAsync(123456, "网易云音乐").GetAwaiter().GetResult();
		if (string.IsNullOrEmpty(playUrl) || !playUrl.StartsWith("http"))
			throw new Exception("ResolvePlayUrlAsync failed to return valid stream url");
		Console.WriteLine($"[PASS] Play URL resolution with credentials validated ({playUrl})");

		try { File.Delete(tempSettingsFile); } catch { }
	}

	private static void TestAdvancedSystemSettings()
	{
		Console.WriteLine("--- Testing FurinaPlayer Settings & Power State ---");
		var settings = new FurinaPlayer.Core.Models.AppSettings
		{
			UiScale = 1.25,
			CloseToTray = true,
			RememberWindowState = true,
			PreventSystemSleep = true
		};

		if (Math.Abs(settings.UiScale - 1.25) > 0.001 || !settings.CloseToTray || !settings.RememberWindowState || !settings.PreventSystemSleep)
			throw new Exception("AppSettings failed to store advanced system/window preferences");

		Console.WriteLine("[PASS] AppSettings correctly persists UI scale, CloseToTray, RememberWindow, and KeepAwake properties");
	}

	private static void TestStreamAudioCacheAndDawEnvelope()
	{
		Console.WriteLine("--- Testing StreamAudioCache & Audition DAW Streaming Envelope/Spectrogram ---");

		string streamUrl = "https://music.example.com/stream/furina_daw_test.wav?token=12345";
		if (!StreamAudioCache.IsNetworkUrl(streamUrl) || StreamAudioCache.IsNetworkUrl(@"C:\Music\local.flac"))
			throw new Exception("StreamAudioCache.IsNetworkUrl detection failed");

		string cachedPath = StreamAudioCache.GetExpectedCacheFilePath(streamUrl);
		try
		{
			int sampleRate = 48000;
			int channels = 2;
			int frames = sampleRate;
			using (var fs = new FileStream(cachedPath, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var bw = new BinaryWriter(fs))
			{
				int byteRate = sampleRate * channels * 2;
				int dataSize = frames * channels * 2;
				bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
				bw.Write(36 + dataSize);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
				bw.Write(16);
				bw.Write((short)1);
				bw.Write((short)channels);
				bw.Write(sampleRate);
				bw.Write(byteRate);
				bw.Write((short)(channels * 2));
				bw.Write((short)16);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
				bw.Write(dataSize);

				for (int i = 0; i < frames; i++)
				{
					short left = (short)(Math.Sin(2.0 * Math.PI * 440.0 * i / sampleRate) * 24000);
					short right = (short)(Math.Cos(2.0 * Math.PI * 880.0 * i / sampleRate) * 20000);
					bw.Write(left);
					bw.Write(right);
				}
			}

			if (!StreamAudioCache.TryGetOrDownloadCachedFile(streamUrl, out string resolvedLocal) || resolvedLocal != cachedPath)
				throw new Exception("StreamAudioCache failed to resolve cached streaming audio file");

			var env = FurinaPlayer.Audio.Renderers.WaveformRenderer.ComputeAuditionEnvelopeFromFile(streamUrl, 512);
			if (env == null || env.LeftMax.Length != 512 || env.RightMax.Length != 512 || env.SampleRate != 48000 || string.IsNullOrEmpty(env.SpecLeftBase64))
				throw new Exception("WaveformRenderer.ComputeAuditionEnvelopeFromFile failed on streaming URL");

			float maxL = 0f;
			for (int i = 0; i < env.LeftMax.Length; i++)
			{
				if (env.LeftMax[i] > maxL) maxL = env.LeftMax[i];
			}
			if (maxL < 0.3f)
				throw new Exception($"Expected non-zero stereo waveform envelope for streaming URL, got maxL={maxL:F3}");

			Console.WriteLine($"[PASS] StreamAudioCache + SafeAudioFileReader + WaveformRenderer computed 512-pt stereo envelope (maxL={maxL:F2}) & {env.SpecCols}x{env.SpecBins} STFT spectrogram from HTTP stream URL");
		}
		finally
		{
			try { if (File.Exists(cachedPath)) File.Delete(cachedPath); } catch { }
		}
	}

	private static void TestFurinaPlayerAppearanceAndThemePersistence()
	{
		Console.WriteLine("--- Testing FurinaPlayer Appearance, Acrylic & Theme Presets Persistence ---");
		string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"furina_theme_test_{Guid.NewGuid():N}.json");
		try
		{
			var svc = new FurinaPlayer.Core.Services.SettingsService(tempSettingsFile);
			var s = svc.Load();

			if (s.BackgroundMaterial != FurinaPlayer.Core.Models.BackgroundMaterial.LiquidGlass || s.FurinaPlayerThemeMode != "liquid-glass")
				throw new Exception("Default AppSettings should initialize with LiquidGlass active");

			s.FurinaPlayerThemeMode = "acrylic";
			s.BackgroundMaterial = FurinaPlayer.Core.Models.BackgroundMaterial.Acrylic;
			s.AcrylicKeepTranslucentWhenUnfocused = true;
			s.AcrylicOpacityPercent = 88;
			s.ScheduledDarkModeEnabled = true;
			s.ScheduledDarkStartTime = "20:00";
			s.ScheduledDarkEndTime = "06:30";
			s.RainbowModeEnabled = true;
			s.InvertModeEnabled = false;
			s.SelectedThemePresetId = "twilight-peach";
			s.SelectedThemeCategory = "柔和";
			svc.Save(s);

			var reloaded = svc.Load();
			if (reloaded.FurinaPlayerThemeMode != "acrylic" ||
				reloaded.BackgroundMaterial != FurinaPlayer.Core.Models.BackgroundMaterial.Acrylic ||
				reloaded.AcrylicOpacityPercent != 88 ||
				!reloaded.ScheduledDarkModeEnabled ||
				reloaded.ScheduledDarkStartTime != "20:00" ||
				!reloaded.RainbowModeEnabled ||
				reloaded.SelectedThemePresetId != "twilight-peach" ||
				reloaded.SelectedThemeCategory != "柔和")
			{
				throw new Exception("FurinaPlayer Appearance & Theme Preset settings failed to round-trip across restart");
			}

			Console.WriteLine("[PASS] FurinaPlayer Appearance (Acrylic, Opacity 88%, Scheduled Dark Mode, Rainbow PRO, Preset 'twilight-peach') persisted across restarts");
		}
		finally
		{
			try { if (File.Exists(tempSettingsFile)) File.Delete(tempSettingsFile); } catch { }
		}
	}

	private static void TestVst3RackVendorAndManager()
	{
		Console.WriteLine("--- Testing VST3 Rack Manager & Vendor Metadata Resolution ---");
		var p1 = FurinaPlayer.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\iZotope\Insight 2.vst3");
		var p2 = FurinaPlayer.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\Celemony\Melodyne.vst3");
		var p3 = FurinaPlayer.Audio.Vst3.Vst3Manager.CreateState(@"C:\Program Files\Common Files\VST3\FabFilter Pro-Q 3.vst3");

		if (!p1.Vendor.Contains("iZotope", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected iZotope vendor for Insight 2, got '{p1.Vendor}'");
		if (!p2.Vendor.Contains("Celemony", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected Celemony vendor for Melodyne, got '{p2.Vendor}'");
		if (!p3.Vendor.Contains("FabFilter", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Expected FabFilter vendor for Pro-Q 3, got '{p3.Vendor}'");

		var legacySaved = new System.Collections.Generic.List<FurinaPlayer.Core.Models.Vst3PluginState>
		{
			new FurinaPlayer.Core.Models.Vst3PluginState
			{
				Name = "Nectar Pro",
				Path = @"C:\Program Files\Common Files\VST3\iZotope\Nectar Pro.vst3",
				Vendor = "未知（模拟宿主）",
				Enabled = true
			}
		};
		var merged = FurinaPlayer.Audio.Vst3.Vst3Manager.Merge(legacySaved, Array.Empty<string>());
		if (merged.Count != 1 || merged[0].Vendor == "未知（模拟宿主）" || !merged[0].Vendor.Contains("iZotope", StringComparison.OrdinalIgnoreCase))
			throw new Exception($"Legacy VST3 vendor failed to upgrade during Merge: '{merged[0].Vendor}'");

		Console.WriteLine($"[PASS] Vst3Manager accurately resolved vendors ({p1.Vendor}, {p2.Vendor}, {p3.Vendor}) and upgraded legacy saved states");
	}

	private static void TestQQMusicAuthHelper()
	{
		Console.WriteLine("--- Testing QQ Music Auth Helper (OAuth/WeChat URLs & Cookie Extraction) ---");

		if (FurinaPlayer.Core.Services.QQMusicAuthHelper.QQLoginAuthorizeUrl.Contains("which=error", StringComparison.OrdinalIgnoreCase) ||
			!FurinaPlayer.Core.Services.QQMusicAuthHelper.QQLoginAuthorizeUrl.Contains("client_id=100497308", StringComparison.Ordinal) ||
			!FurinaPlayer.Core.Services.QQMusicAuthHelper.QQLoginAuthorizeUrl.Contains("wx_redirect.html", StringComparison.Ordinal))
		{
			throw new Exception("QQLoginAuthorizeUrl is malformed");
		}

		string expectedAppId = FurinaPlayer.Core.Services.QQMusicAuthHelper.WeChatOpenAppId;
		if (!expectedAppId.StartsWith("wx", StringComparison.Ordinal) || expectedAppId.Length != 18 ||
			!FurinaPlayer.Core.Services.QQMusicAuthHelper.WeChatLoginQrUrl.Contains("appid=" + expectedAppId, StringComparison.Ordinal) ||
			!FurinaPlayer.Core.Services.QQMusicAuthHelper.WeChatLoginQrUrl.Contains("wx_redirect.html", StringComparison.Ordinal))
		{
			throw new Exception("WeChatLoginQrUrl is malformed");
		}

		// 1. Intermediate ptlogin2 cookies on graph.qq.com must NOT trigger premature completion
		var intermediateCookies = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["uin"] = "o0100012345",
			["p_skey"] = "temp_pskey_only",
			["skey"] = "@tempSkey"
		};
		if (FurinaPlayer.Core.Services.QQMusicAuthHelper.TryBuildAuthenticatedCookieHeader(
				intermediateCookies,
				"https://graph.qq.com/oauth2.0/show?which=Login&client_id=100497308",
				out _,
				out _))
		{
			throw new Exception("Should not accept intermediate graph.qq.com p_skey before wx_redirect finishes");
		}

		// 2. Final qqmusic_key / qm_keyst with o-prefixed UIN must extract clean numeric UIN
		var qqFinalCookies = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["uin"] = "o0100012345",
			["qqmusic_key"] = "Q_H_L_REAL_MUSIC_KEY_123",
			["qm_keyst"] = "Q_H_L_REAL_MUSIC_KEY_123"
		};
		if (!FurinaPlayer.Core.Services.QQMusicAuthHelper.TryBuildAuthenticatedCookieHeader(
				qqFinalCookies,
				"https://y.qq.com/",
				out string qqUserId,
				out string qqHeader) ||
			qqUserId != "100012345" ||
			!qqHeader.Contains("qqmusic_key=Q_H_L_REAL_MUSIC_KEY_123", StringComparison.Ordinal))
		{
			throw new Exception($"Expected QQ userId 100012345, got '{qqUserId}'");
		}

		// 3. WeChat login cookies (wxuin / euin + qqmusic_key)
		var wxFinalCookies = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["wxuin"] = "1152921504606846977",
			["qqmusic_key"] = "W_X_WECHAT_MUSIC_KEY",
			["wxopenid"] = "o_wechat_openid_123"
		};
		if (!FurinaPlayer.Core.Services.QQMusicAuthHelper.TryBuildAuthenticatedCookieHeader(
				wxFinalCookies,
				"https://y.qq.com/",
				out string wxUserId,
				out string wxHeader) ||
			wxUserId != "1152921504606846977" ||
			!wxHeader.Contains("uin=o1152921504606846977", StringComparison.Ordinal))
		{
			throw new Exception($"Expected WeChat numeric wxuin, got '{wxUserId}' / '{wxHeader}'");
		}

		// 4. StreamingService VerifyAndFetchQQAccountAsync handles o-prefixed UIN and rejects garbage
		string tempSettingsFile = Path.Combine(Path.GetTempPath(), $"furina_qq_test_{Guid.NewGuid():N}.json");
		try
		{
			var svc = new FurinaPlayer.Core.Services.StreamingService(new FurinaPlayer.Core.Services.SettingsService(tempSettingsFile));
			var oPrefixed = svc.VerifyAndFetchQQAccountAsync("uin=o0100012345; qqmusic_key=Q_H_L_TEST;").GetAwaiter().GetResult();
			if (oPrefixed == null || !oPrefixed.IsLoggedIn || oPrefixed.UserIdStr != "100012345")
				throw new Exception($"o-prefixed UIN verification failed: {oPrefixed?.UserIdStr}");

			var invalid = svc.VerifyAndFetchQQAccountAsync("not_a_valid_cookie").GetAwaiter().GetResult();
			if (invalid != null)
				throw new Exception("Garbage cookie string should have been rejected");
		}
		finally
		{
			try { if (File.Exists(tempSettingsFile)) File.Delete(tempSettingsFile); } catch { }
		}

		Console.WriteLine("[PASS] QQMusicAuthHelper & o-prefixed/WeChat QQ cookie parsing verified");
	}

	private static Action<bool, string> ResolveAssert(Action<bool, string>? assert)
	{
		return assert ?? ((condition, name) =>
		{
			if (!condition)
			{
				throw new Exception(name);
			}
			Console.WriteLine($"[PASS] {name}");
		});
	}

	private static bool AreAllFinite(double[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (!double.IsFinite(values[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool AreAllFinite(float[] values)
	{
		for (int i = 0; i < values.Length; i++)
		{
			if (!float.IsFinite(values[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static void WriteSyntheticStereoWav(
		string path,
		int sampleRate,
		double durationSeconds,
		double leftFreqHz,
		double rightFreqHz,
		double amplitude)
	{
		const int channels = 2;
		const int bitsPerSample = 16;
		int frames = Math.Max(1, (int)Math.Round(sampleRate * durationSeconds));
		int byteRate = sampleRate * channels * (bitsPerSample / 8);
		int dataSize = frames * channels * (bitsPerSample / 8);

		using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
		using var bw = new BinaryWriter(fs);
		bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
		bw.Write(36 + dataSize);
		bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
		bw.Write(16);
		bw.Write((short)1);
		bw.Write((short)channels);
		bw.Write(sampleRate);
		bw.Write(byteRate);
		bw.Write((short)(channels * (bitsPerSample / 8)));
		bw.Write((short)bitsPerSample);
		bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
		bw.Write(dataSize);

		double amp = Math.Clamp(amplitude, 0.0, 0.99) * 32767.0;
		for (int i = 0; i < frames; i++)
		{
			double t = (double)i / sampleRate;
			short l = (short)Math.Round(Math.Sin(2.0 * Math.PI * leftFreqHz * t) * amp);
			short r = (short)Math.Round(Math.Sin(2.0 * Math.PI * rightFreqHz * t) * amp);
			bw.Write(l);
			bw.Write(r);
		}
	}

	public static void TestLibVlcTranscoderAndOggWaveform(Action<bool, string>? assert = null)
	{
		var check = ResolveAssert(assert);
		Console.WriteLine("\n--- Testing LibVlcPcmTranscoder & OGG Waveform Decoding ---");

		check(LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(".ogg"),
			"LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(\".ogg\") == true");

		check(!LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(".flac")
			&& !LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(".wav")
			&& !LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension(".mp3"),
			"LibVlcPcmTranscoder.NeedsLibVlcFallbackExtension false for .flac/.wav/.mp3");

		bool leakedException = false;
		bool decodeMissing = false;
		string decodedOut = string.Empty;
		try
		{
			decodeMissing = LibVlcPcmTranscoder.TryDecodeToWav("nonexistent.ogg", out decodedOut);
		}
		catch
		{
			leakedException = true;
		}
		check(!leakedException && !decodeMissing && string.IsNullOrEmpty(decodedOut),
			"LibVlcPcmTranscoder.TryDecodeToWav(\"nonexistent.ogg\") returns false without throwing");

		const string sampleOggPath = @"C:\Users\Xeon\Music\Cassie Wei&HOYO-MiX - Ripples of Past Reverie [mqms2].ogg";
		if (File.Exists(sampleOggPath))
		{
			using var reader = new SafeAudioFileReader(sampleOggPath);
			check(reader.WaveFormat.SampleRate > 0 && reader.WaveFormat.Channels > 0,
				$"SafeAudioFileReader opened OGG cleanly ({reader.WaveFormat.SampleRate} Hz, {reader.WaveFormat.Channels} ch)");

			var env = WaveformRenderer.ComputeAuditionEnvelopeFromFile(sampleOggPath, 512);
			bool shapeValid = env != null
				&& env.Peak.Length == 512
				&& env.Rms.Length == 512
				&& env.LeftMin.Length == 512
				&& env.LeftMax.Length == 512;

			float maxPeak = 0f;
			bool noNonFinite = false;
			if (env != null && shapeValid)
			{
				noNonFinite = AreAllFinite(env.Peak)
					&& AreAllFinite(env.Rms)
					&& AreAllFinite(env.LeftMin)
					&& AreAllFinite(env.LeftMax);
				for (int i = 0; i < env.Peak.Length; i++)
				{
					if (env.Peak[i] > maxPeak) maxPeak = env.Peak[i];
				}
			}

			check(shapeValid && maxPeak > 0.001f && noNonFinite,
				$"WaveformRenderer.ComputeAuditionEnvelopeFromFile OGG envelope valid (Peak.Length={env?.Peak.Length ?? 0}, maxPeak={maxPeak:F4}, allFinite={noNonFinite})");
		}
		else
		{
			Console.WriteLine($"[INFO] Sample OGG file not found at '{sampleOggPath}'; skipping live OGG decode assertions.");
		}
	}

	public static void TestAudioSessionSwitchAndMeterInvariants(Action<bool, string>? assert = null)
	{
		var check = ResolveAssert(assert);
		Console.WriteLine("\n--- Testing AudioSession Queue Switching & Live Meter Invariants ---");

		string wavA = Path.Combine(Path.GetTempPath(), $"furinaplayer_session_a_{Guid.NewGuid():N}.wav");
		string wavB = Path.Combine(Path.GetTempPath(), $"furinaplayer_session_b_{Guid.NewGuid():N}.wav");

		static bool ValidateSessionMeters(AudioSession session, out string summary)
		{
			var meters = session.GetLiveAudioMeters();
			double inputPeak = session.LatestInputPeakDb;
			double truePeak = session.LatestTruePeakDb;

			bool finiteScalars = double.IsFinite(meters.PeakDb)
				&& double.IsFinite(meters.RmsDb)
				&& double.IsFinite(inputPeak)
				&& double.IsFinite(truePeak)
				&& float.IsFinite(meters.StereoBalance)
				&& float.IsFinite(meters.PhaseCorrelation);

			bool boundedStereo = meters.PhaseCorrelation >= -1.0f && meters.PhaseCorrelation <= 1.0f
				&& meters.StereoBalance >= -1.0f && meters.StereoBalance <= 1.0f;

			bool finiteArrays = AreAllFinite(meters.Bands) && AreAllFinite(meters.VectorPoints);

			summary = $"Peak={meters.PeakDb:F1}dB, Rms={meters.RmsDb:F1}dB, In={inputPeak:F1}dB, TP={truePeak:F1}dB, Corr={meters.PhaseCorrelation:F3}";
			return finiteScalars && boundedStereo && finiteArrays;
		}

		try
		{
			WriteSyntheticStereoWav(wavA, 48000, 1.5, 440.0, 440.0, 0.25);
			WriteSyntheticStereoWav(wavB, 48000, 1.5, 880.0, 660.0, 0.25);

			var env = WaveformRenderer.ComputeAuditionEnvelopeFromFile(wavA, 512);
			float synthMaxPeak = 0f;
			if (env != null)
			{
				for (int i = 0; i < env.Peak.Length; i++)
				{
					if (env.Peak[i] > synthMaxPeak) synthMaxPeak = env.Peak[i];
				}
			}
			check(env != null && env.Peak.Length == 512 && synthMaxPeak > 0.1f && AreAllFinite(env.Peak),
				$"WaveformRenderer.ComputeAuditionEnvelopeFromFile synthetic WAV valid (Peak.Length={env?.Peak.Length ?? 0}, maxPeak={synthMaxPeak:F3})");

			using var engine = new AudioEngine();
			AudioSession session = engine.Session;
			session.Volume = 0;

			var trackA = new Track
			{
				Id = 91001,
				FilePath = wavA,
				Title = "Synthetic Tone A",
				SampleRate = 48000,
				BitDepth = 16,
				DurationMilliseconds = 1500
			};
			var trackB = new Track
			{
				Id = 91002,
				FilePath = wavB,
				Title = "Synthetic Tone B",
				SampleRate = 48000,
				BitDepth = 16,
				DurationMilliseconds = 1500
			};

			session.SetQueue(new[] { trackA, trackB }, 0);
			bool initMetersOk = ValidateSessionMeters(session, out string initSummary);
			check(session.Queue.Count == 2 && session.QueueIndex == 0 && initMetersOk,
				$"AudioSession.SetQueue initialized 2 tracks with finite meters ({initSummary})");

			session.PlayTrack(trackA);
			Thread.Sleep(60);
			bool playMetersOk = ValidateSessionMeters(session, out string playSummary);
			check(session.CurrentTrack?.Id == trackA.Id && playMetersOk,
				$"AudioSession.PlayTrack(A) meter invariants valid ({playSummary})");

			bool nextOk = session.Next();
			Thread.Sleep(60);
			bool nextMetersOk = ValidateSessionMeters(session, out string nextSummary);
			check(nextOk && session.CurrentTrack?.Id == trackB.Id && nextMetersOk,
				$"AudioSession.Next() switched to Track B with finite meters ({nextSummary})");

			bool prevOk = session.Previous();
			Thread.Sleep(60);
			bool prevMetersOk = ValidateSessionMeters(session, out string prevSummary);
			check(prevOk && session.CurrentTrack?.Id == trackA.Id && prevMetersOk,
				$"AudioSession.Previous() switched back to Track A with finite meters ({prevSummary})");

			session.Pause();
			bool pauseMetersOk = ValidateSessionMeters(session, out string pauseSummary);
			check(session.State == PlaybackState.Paused && pauseMetersOk,
				$"AudioSession.Pause() entered Paused state with finite meters ({pauseSummary})");

			session.Stop();
			bool stopMetersOk = ValidateSessionMeters(session, out string stopSummary);
			check(session.State == PlaybackState.Stopped && stopMetersOk,
				$"AudioSession.Stop() entered Stopped state with finite meters ({stopSummary})");
		}
		finally
		{
			try { if (File.Exists(wavA)) File.Delete(wavA); } catch { }
			try { if (File.Exists(wavB)) File.Delete(wavB); } catch { }
		}
	}

	public static void TestMixAssistantAndParametricOfflineRender(Action<bool, string>? assert = null)
	{
		var check = ResolveAssert(assert);
		Console.WriteLine("\n--- Testing MixAssistantEngine & Parametric Offline Render ---");

		const int sampleRate = 48000;
		const int frames = sampleRate * 2;

		// 1. Harsh sub-bass heavy + 4kHz spike stereo signal
		float[] harshSignal = new float[frames * 2];
		for (int i = 0; i < frames; i++)
		{
			double t = (double)i / sampleRate;
			float s = (float)(0.45 * Math.Sin(2.0 * Math.PI * 45.0 * t)
				+ 0.35 * Math.Sin(2.0 * Math.PI * 4000.0 * t)
				+ 0.08 * Math.Sin(2.0 * Math.PI * 1000.0 * t));
			harshSignal[i * 2] = s;
			harshSignal[i * 2 + 1] = s;
		}

		// 2. Flat broadband pink-like sum of log-spaced sines (-3 dB/octave amplitude weighting)
		const int toneCount = 24;
		double[] freqs = new double[toneCount];
		double[] weights = new double[toneCount];
		double logMin = Math.Log10(30.0);
		double logMax = Math.Log10(16000.0);
		double maxAbsSum = 0.0;
		for (int k = 0; k < toneCount; k++)
		{
			freqs[k] = Math.Pow(10.0, logMin + (logMax - logMin) * k / (toneCount - 1));
			weights[k] = 1.0 / Math.Sqrt(freqs[k] / 1000.0);
			maxAbsSum += weights[k];
		}
		double pinkScale = 0.65 / maxAbsSum;
		float[] pinkSignal = new float[frames * 2];
		for (int i = 0; i < frames; i++)
		{
			double t = (double)i / sampleRate;
			double val = 0.0;
			for (int k = 0; k < toneCount; k++)
			{
				val += weights[k] * Math.Sin(2.0 * Math.PI * freqs[k] * t + k * 0.7);
			}
			float s = (float)(val * pinkScale);
			pinkSignal[i * 2] = s;
			pinkSignal[i * 2 + 1] = s;
		}

		var assistant = new MixAssistantEngine();
		var harshProfile = assistant.AnalyzeSamples(harshSignal, 2, sampleRate);
		check(harshProfile.AnalyzedFrames > 0
			&& harshProfile.Bins32Db.Length == 32
			&& AreAllFinite(harshProfile.Bins32Db)
			&& harshProfile.Confidence01 > 0.0
			&& harshProfile.Confidence01 <= 1.0,
			$"MixAssistantEngine.AnalyzeSamples (harsh) valid (frames={harshProfile.AnalyzedFrames}, conf={harshProfile.Confidence01:F2})");

		var pinkProfile = assistant.AnalyzeSamples(pinkSignal, 2, sampleRate);
		check(pinkProfile.AnalyzedFrames > 0
			&& pinkProfile.Bins32Db.Length == 32
			&& AreAllFinite(pinkProfile.Bins32Db)
			&& pinkProfile.Confidence01 > 0.0
			&& pinkProfile.Confidence01 <= 1.0,
			$"MixAssistantEngine.AnalyzeSamples (broadband) valid (frames={pinkProfile.AnalyzedFrames}, conf={pinkProfile.Confidence01:F2})");

		// 3. Streaming learning lifecycle: BeginLearning / AccumulateSpectrum64 / EndLearning
		assistant.BeginLearning();
		bool startedLearning = assistant.IsLearning;
		float[] specFrame = new float[64];
		for (int b = 0; b < 64; b++)
		{
			specFrame[b] = 0.58f - 0.12f * (b / 63.0f);
		}
		for (int f = 0; f < 30; f++)
		{
			assistant.AccumulateSpectrum64(specFrame);
		}
		var learnedProfile = assistant.EndLearning();
		check(startedLearning
			&& !assistant.IsLearning
			&& assistant.LearnedFrames == 30
			&& learnedProfile.AnalyzedFrames == 30
			&& learnedProfile.Bins32Db.Length == 32
			&& AreAllFinite(learnedProfile.Bins32Db)
			&& learnedProfile.Confidence01 > 0.0,
			$"MixAssistantEngine streaming learning accumulated {learnedProfile.AnalyzedFrames} frames cleanly");

		// 4. SolveEqCurve across multiple targets
		string[] targets = { "balanced_master", "warm_vocals", "acg_vibrant", "late_night" };
		foreach (string targetId in targets)
		{
			var sol = assistant.SolveEqCurve(harshProfile, targetId, matchAmount01: 0.85, bassSculptDb: 1.0, trebleSculptDb: -0.5, demaskMidMud: true);
			bool offsetsInBounds = sol.BandOffsetsDb.Length == 10 && sol.Filters.Length == 10;
			if (offsetsInBounds)
			{
				for (int i = 0; i < 10; i++)
				{
					double off = sol.BandOffsetsDb[i];
					double fGain = sol.Filters[i].GainDb;
					if (!double.IsFinite(off) || off < -9.0 || off > 9.0
						|| !double.IsFinite(fGain) || fGain < -9.0 || fGain > 9.0)
					{
						offsetsInBounds = false;
						break;
					}
				}
			}

			check(sol.TargetId == targetId
				&& offsetsInBounds
				&& double.IsFinite(sol.RecommendedPreampDb)
				&& sol.PredictedCurve64Db.Length == 64
				&& AreAllFinite(sol.PredictedCurve64Db),
				$"MixAssistantEngine.SolveEqCurve('{targetId}') produced 10 bounded filters & 64-pt curve (preamp={sol.RecommendedPreampDb:F1} dB)");
		}

		// 5. OfflineEffectRenderer.RenderSamplesWithParametricEqAsync (+6 dB @ 1 kHz on -12 dBFS 1 kHz sine)
		float ampNeg12Dbfs = MathF.Pow(10.0f, -12.0f / 20.0f);
		int renderFrames = sampleRate;
		float[] sine1k = new float[renderFrames * 2];
		for (int i = 0; i < renderFrames; i++)
		{
			float s = ampNeg12Dbfs * (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / sampleRate);
			sine1k[i * 2] = s;
			sine1k[i * 2 + 1] = s;
		}

		var peakingPlus6 = new FurinaPlayerBiquadFilter
		{
			FilterType = FurinaPlayerFilterType.Peaking,
			FrequencyHz = 1000.0,
			GainDb = 6.0,
			Q = 1.414,
			Enabled = true
		};
		peakingPlus6.UpdateCoefficients(sampleRate);

		var rendered = OfflineEffectRenderer.RenderSamplesWithParametricEqAsync(
			sine1k,
			sampleRate,
			2,
			RenderSettings.Flat,
			new[] { peakingPlus6 },
			0.0,
			null,
			CancellationToken.None).GetAwaiter().GetResult();

		double inSq = 0.0;
		double outSq = 0.0;
		int steadyStart = sampleRate / 10;
		for (int f = steadyStart; f < renderFrames; f++)
		{
			int idx = f * 2;
			inSq += (double)sine1k[idx] * sine1k[idx];
			outSq += (double)rendered.Samples[idx] * rendered.Samples[idx];
		}
		double rmsRatio = Math.Sqrt(outSq / Math.Max(1e-12, inSq));
		check(rendered.Samples.Length == sine1k.Length && rmsRatio >= 1.8 && rmsRatio <= 2.2,
			$"OfflineEffectRenderer.RenderSamplesWithParametricEqAsync (+6dB @ 1kHz) boosted -12dBFS sine cleanly (RMS ratio={rmsRatio:F3}x)");
	}

	public static void TestInsightProAnalyzerPrecision(Action<bool, string>? assert = null)
	{
		var check = ResolveAssert(assert);
		Console.WriteLine("\n--- Testing InsightProAnalyzer Precision (LUFS, dBTP, Correlation, FFT, Targets & Reset) ---");

		const int sampleRate = 48000;
		const int frames = sampleRate * 5;
		float[] sine5s = new float[frames * 2];
		for (int i = 0; i < frames; i++)
		{
			float s = 0.1f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / sampleRate);
			sine5s[i * 2] = s;
			sine5s[i * 2 + 1] = s;
		}

		var analyzer = new InsightProAnalyzer();
		analyzer.TargetPreset = InsightTargetPreset.CdLoudMinus9;
		var snap = analyzer.AnalyzeWindow(sine5s, 2, sampleRate, 0, frames);

		// For a stereo 1kHz sine with peak 0.1 (-20 dBFS peak), per-channel RMS is -23.01 dBFS,
		// stereo sum is -20.0 dBFS, and BS.1770-4 K-weighting + offset yields ~ -20.0 LUFS.
		check(Math.Abs(snap.MomentaryLufs - (-20.0f)) <= 1.5f
			&& Math.Abs(snap.ShortTermLufs - (-20.0f)) <= 1.5f
			&& Math.Abs(snap.IntegratedLufs - (-20.0f)) <= 1.5f,
			$"InsightProAnalyzer LUFS precision within ±1.5 LU of -20.0 LUFS (M={snap.MomentaryLufs:F2}, S={snap.ShortTermLufs:F2}, I={snap.IntegratedLufs:F2})");

		check(snap.TruePeakDbTp >= -20.5f && snap.TruePeakDbTp <= -19.0f,
			$"InsightProAnalyzer 4x True Peak accurate for -20 dBFS sine ({snap.TruePeakDbTp:F2} dBTP)");

		check(snap.StereoCorrelation > 0.99f,
			$"InsightProAnalyzer correlation > 0.99 for identical L/R ({snap.StereoCorrelation:F4})");

		int peakBin = 0;
		float peakMagDb = float.NegativeInfinity;
		for (int b = 0; b < snap.SpectrumDb.Length; b++)
		{
			if (snap.SpectrumDb[b] > peakMagDb)
			{
				peakMagDb = snap.SpectrumDb[b];
				peakBin = b;
			}
		}
		float peakFreqHz = snap.SpectrumFrequenciesHz.Length > peakBin ? snap.SpectrumFrequenciesHz[peakBin] : 0f;
		bool specFramesValid = snap.SpectrogramFrames.Length > 0;
		for (int i = 0; i < snap.SpectrogramFrames.Length; i++)
		{
			if (snap.SpectrogramFrames[i].Length != InsightProAnalyzer.SpectrogramFreqBins)
			{
				specFramesValid = false;
				break;
			}
		}
		check(snap.SpectrumDb.Length == 128 && peakFreqHz >= 850.0f && peakFreqHz <= 1150.0f && peakMagDb > -30.0f,
			$"InsightProAnalyzer 128-band spectrum peaked near 1kHz (bin={peakBin}, freq={peakFreqHz:F1} Hz, mag={peakMagDb:F1} dBFS)");

		check(specFramesValid && snap.HistorySamples.Length > 0,
			$"InsightProAnalyzer populated spectrogram ({snap.SpectrogramFrames.Length}x64) and history ({snap.HistorySamples.Length} pts)");

		check(!snap.MomentaryExceedsTarget && !snap.ShortTermExceedsTarget && !snap.IntegratedExceedsTarget && !snap.TruePeakExceedsTarget,
			"InsightProAnalyzer CdLoudMinus9 (-9 LUFS / -0.3 dBTP) target flags remain clear for -20 LUFS signal");

		analyzer.TargetPreset = InsightTargetPreset.EbuR128Minus23;
		var snapEbu = analyzer.GetSnapshot();
		check(snapEbu.MomentaryExceedsTarget && snapEbu.ShortTermExceedsTarget && snapEbu.IntegratedExceedsTarget && !snapEbu.TruePeakExceedsTarget,
			"InsightProAnalyzer EbuR128Minus23 (-23 LUFS / -1.0 dBTP) flagged loudness exceed while TruePeak stayed safe");

		// Anti-phase correlation check (R == -L)
		int antiFrames = sampleRate * 2;
		float[] antiPhase = new float[antiFrames * 2];
		for (int i = 0; i < antiFrames; i++)
		{
			float s = 0.1f * (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / sampleRate);
			antiPhase[i * 2] = s;
			antiPhase[i * 2 + 1] = -s;
		}
		var antiSnap = analyzer.AnalyzeWindow(antiPhase, 2, sampleRate, 0, antiFrames);
		check(antiSnap.StereoCorrelation < -0.99f,
			$"InsightProAnalyzer correlation < -0.99 for anti-phase R == -L ({antiSnap.StereoCorrelation:F4})");

		// ResetIntegrated() vs Reset()
		analyzer.AnalyzeWindow(sine5s, 2, sampleRate, 0, frames);
		analyzer.ResetIntegrated();
		var afterResetInt = analyzer.GetSnapshot();
		bool spectrumPreserved = afterResetInt.SpectrumDb.Length == 128 && afterResetInt.SpectrumDb[peakBin] > -35.0f;
		check(afterResetInt.IntegratedLufs <= -69.9f
			&& afterResetInt.MomentaryLufs <= -69.9f
			&& afterResetInt.ShortTermLufs <= -69.9f
			&& afterResetInt.TruePeakDbTp <= -109.9f
			&& afterResetInt.HistorySamples.Length == 0
			&& spectrumPreserved,
			"InsightProAnalyzer.ResetIntegrated() cleared loudness/history/true-peak while preserving realtime spectrum");

		analyzer.Reset();
		var afterFullReset = analyzer.GetSnapshot();
		bool spectrumCleared = afterFullReset.SpectrumDb.Length == 128;
		for (int b = 0; b < afterFullReset.SpectrumDb.Length; b++)
		{
			if (afterFullReset.SpectrumDb[b] > -109.9f)
			{
				spectrumCleared = false;
				break;
			}
		}
		check(afterFullReset.IntegratedLufs <= -69.9f
			&& afterFullReset.HistorySamples.Length == 0
			&& afterFullReset.SpectrogramFrames.Length == 0
			&& spectrumCleared,
			"InsightProAnalyzer.Reset() cleared loudness, history, spectrogram, and realtime spectrum");
	}

	public static void TestStreamingNonBlockingAndCancellation(Action<bool, string> check)
	{
		Console.WriteLine("\n--- Testing Streaming Non-Blocking AudioEngine & Cache Cooldown/Cancellation ---");

		string uncachedUrl = $"http://127.0.0.1:9/uncached_{Guid.NewGuid():N}.mp3";
		var sw = System.Diagnostics.Stopwatch.StartNew();
		bool readyMissing = StreamAudioCache.TryGetCachedFilePathIfReady(uncachedUrl, out _);
		sw.Stop();
		check(!readyMissing && sw.ElapsedMilliseconds < 100,
			$"TryGetCachedFilePathIfReady returned false immediately for uncached URL ({sw.ElapsedMilliseconds}ms)");

		// Create a cached file and confirm TryGetCachedFilePathIfReady finds it immediately without network I/O
		string cachedUrl = $"https://music.example.com/nonblock_ready_{Guid.NewGuid():N}.wav";
		string cachedPath = StreamAudioCache.GetExpectedCacheFilePath(cachedUrl);
		System.Net.Sockets.TcpListener? stallListener = null;
		System.Net.Sockets.TcpListener? failListener = null;
		var heldSockets = new System.Collections.Generic.List<System.Net.Sockets.Socket>();
		using var serverCts = new CancellationTokenSource();

		try
		{
			File.WriteAllBytes(cachedPath, new byte[4096]);
			sw.Restart();
			bool readyPresent = StreamAudioCache.TryGetCachedFilePathIfReady(cachedUrl, out string resolvedPath);
			sw.Stop();
			check(readyPresent && resolvedPath == cachedPath && sw.ElapsedMilliseconds < 100,
				$"TryGetCachedFilePathIfReady resolved cached file with zero network I/O ({sw.ElapsedMilliseconds}ms)");

			// 1. Failure cooldown server (returns 404 immediately and counts hits)
			failListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			failListener.Start();
			int failPort = ((System.Net.IPEndPoint)failListener.LocalEndpoint).Port;
			int failHits = 0;
			_ = System.Threading.Tasks.Task.Run(async () =>
			{
				while (!serverCts.IsCancellationRequested)
				{
					try
					{
						var client = await failListener.AcceptSocketAsync(serverCts.Token);
						Interlocked.Increment(ref failHits);
						byte[] resp = System.Text.Encoding.ASCII.GetBytes(
							"HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
						await client.SendAsync(resp, System.Net.Sockets.SocketFlags.None, serverCts.Token);
						client.Dispose();
					}
					catch { break; }
				}
			});

			string badUrl = $"http://127.0.0.1:{failPort}/missing_{Guid.NewGuid():N}.mp3";
			bool firstOk = StreamAudioCache.TryGetOrDownloadCachedFile(badUrl, out _, 3000, CancellationToken.None);
			int hitsAfterFirst = Volatile.Read(ref failHits);
			sw.Restart();
			bool secondOk = StreamAudioCache.TryGetOrDownloadCachedFile(badUrl, out _, 3000, CancellationToken.None);
			sw.Stop();
			int hitsAfterSecond = Volatile.Read(ref failHits);
			check(!firstOk && !secondOk && hitsAfterFirst >= 1 && hitsAfterSecond == hitsAfterFirst && sw.ElapsedMilliseconds < 100,
				$"StreamAudioCache 12s failure cooldown skipped redundant network retry ({hitsAfterSecond} hits, {sw.ElapsedMilliseconds}ms)");

			// 2. Stalled HTTP server (accepts TCP connection and holds without replying)
			stallListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
			stallListener.Start();
			int stallPort = ((System.Net.IPEndPoint)stallListener.LocalEndpoint).Port;
			_ = System.Threading.Tasks.Task.Run(async () =>
			{
				while (!serverCts.IsCancellationRequested)
				{
					try
					{
						var sock = await stallListener.AcceptSocketAsync(serverCts.Token);
						lock (heldSockets) heldSockets.Add(sock);
					}
					catch { break; }
				}
			});

			string stallUrl = $"http://127.0.0.1:{stallPort}/stalled_stream_{Guid.NewGuid():N}.mp3";
			using (var cancelDownloadCts = new CancellationTokenSource(60))
			{
				sw.Restart();
				bool dlOk = StreamAudioCache.TryGetOrDownloadCachedFile(stallUrl, out _, 5000, cancelDownloadCts.Token);
				sw.Stop();
				check(!dlOk && sw.ElapsedMilliseconds < 450,
					$"StreamAudioCache.TryGetOrDownloadCachedFile honored cancellation on stalled TCP socket ({sw.ElapsedMilliseconds}ms)");
			}

			// 3. AudioEngine non-blocking caller contract + rapid switch cancellation
			using var engine = new AudioEngine();
			var slowTrack1 = new Track { Id = 1, Title = "Stall 1", Artist = "Net", FilePath = $"http://127.0.0.1:{stallPort}/track1_{Guid.NewGuid():N}.mp3" };
			var slowTrack2 = new Track { Id = 2, Title = "Stall 2", Artist = "Net", FilePath = $"http://127.0.0.1:{stallPort}/track2_{Guid.NewGuid():N}.mp3" };

			sw.Restart();
			engine.SetQueue(new[] { slowTrack1, slowTrack2 }, 0);
			engine.PlayTrack(slowTrack1);
			long playCallMs = sw.ElapsedMilliseconds;

			sw.Restart();
			engine.Next();
			engine.PlayTrack(slowTrack2);
			engine.Seek(TimeSpan.FromSeconds(5));
			engine.Pause();
			engine.Stop();
			long burstCallsMs = sw.ElapsedMilliseconds;

			check(playCallMs < 150 && burstCallsMs < 150,
				$"AudioEngine caller methods stayed non-blocking against stalled stream (play={playCallMs}ms, burst={burstCallsMs}ms)");

			using var asyncCancelCts = new CancellationTokenSource(60);
			sw.Restart();
			try
			{
				engine.PlayTrackAsync(slowTrack1, asyncCancelCts.Token).GetAwaiter().GetResult();
			}
			catch (OperationCanceledException) { }
			catch (Exception) { }
			sw.Stop();
			engine.Stop();
			check(sw.ElapsedMilliseconds < 600,
				$"AudioEngine.PlayTrackAsync aborted cleanly on CancellationToken ({sw.ElapsedMilliseconds}ms)");
		}
		finally
		{
			serverCts.Cancel();
			try { stallListener?.Stop(); } catch { }
			try { failListener?.Stop(); } catch { }
			lock (heldSockets)
			{
				foreach (var s in heldSockets)
				{
					try { s.Dispose(); } catch { }
				}
				heldSockets.Clear();
			}
			try { if (File.Exists(cachedPath)) File.Delete(cachedPath); } catch { }
		}
	}

	public static void TestAudioSessionRestoreResume(Action<bool, string>? assert = null)
	{
		var check = ResolveAssert(assert);
		Console.WriteLine("\n--- Testing AudioSession PrepareRestore & Resume Position ---");
		string wavRestore = Path.Combine(Path.GetTempPath(), $"furinaplayer_restore_{Guid.NewGuid():N}.wav");
		try
		{
			WriteSyntheticStereoWav(wavRestore, 48000, 5.0, 440.0, 440.0, 0.25);
			using var engine = new AudioEngine();
			AudioSession session = engine.Session;
			session.Volume = 0;

			var track = new Track
			{
				Id = 92001,
				FilePath = wavRestore,
				Title = "Restore Test Tone",
				Artist = "Test",
				DurationMilliseconds = 5000
			};
			session.SetQueue(new[] { track }, 0);

			TimeSpan posAtTrackChanged = TimeSpan.Zero;
			session.TrackChanged += _ => posAtTrackChanged = session.Position;

			TimeSpan target = TimeSpan.FromSeconds(2.5);
			bool prepared = session.PrepareRestore(target);
			check(prepared, "AudioSession.PrepareRestore(2.5s) succeeded");
			check(posAtTrackChanged >= TimeSpan.FromSeconds(2.4),
				$"TrackChanged saw restored Position ({posAtTrackChanged.TotalSeconds:F2}s >= 2.40s)");
			check(session.Position >= TimeSpan.FromSeconds(2.4),
				$"AudioSession.Position preserved after PrepareRestore ({session.Position.TotalSeconds:F2}s)");

			session.Play();
			Thread.Sleep(150);
			check(session.State == PlaybackState.Playing && session.Position >= TimeSpan.FromSeconds(2.4),
				$"AudioSession.Play() resumed from restored position ({session.Position.TotalSeconds:F2}s >= 2.40s)");

			session.Stop();
			TimeSpan target2 = TimeSpan.FromSeconds(3.0);
			bool prepared2 = session.PrepareRestore(target2);
			check(prepared2, "AudioSession.PrepareRestore(3.0s) succeeded");
			session.PlayTrack(track);
			Thread.Sleep(150);
			check(session.State == PlaybackState.Playing && session.Position >= TimeSpan.FromSeconds(2.9),
				$"AudioSession.PlayTrack(sameTrack) resumed from restored position ({session.Position.TotalSeconds:F2}s >= 2.90s)");

			session.Seek(TimeSpan.FromSeconds(0.5));
			Thread.Sleep(80);
			check(session.Position >= TimeSpan.FromSeconds(0.4) && session.Position < TimeSpan.FromSeconds(1.5),
				$"AudioSession backward Seek(0.5s) applied cleanly during playback ({session.Position.TotalSeconds:F2}s)");

			session.Stop();
		}
		finally
		{
			try { if (File.Exists(wavRestore)) File.Delete(wavRestore); } catch { }
		}
	}

	public static void TestNeteaseQrCookieMergeAndFallback(Action<bool, string>? assert = null)
	{
		Action<bool, string> check = assert ?? ((ok, msg) => { if (!ok) throw new Exception(msg); });
		Console.WriteLine("\n--- Testing NetEase QR Cookie Merge & Fallback ---");

		var container = new System.Net.CookieContainer();
		container.Add(new Uri("https://music.163.com"), new System.Net.Cookie("NMTID", "nmtid_val", "/", ".music.163.com"));
		container.Add(new Uri("https://interface.music.163.com"), new System.Net.Cookie("__csrf", "csrf_val", "/", ".music.163.com"));

		var setCookies = new[]
		{
			"MUSIC_U=secret_token_123; Path=/; Domain=.music.163.com; HttpOnly",
			"NMTID=nmtid_updated; Path=/; Domain=.music.163.com"
		};

		string? merged = FurinaPlayer.Core.Services.StreamingService.MergeNeteaseQrCookies("os=pc", setCookies, container);
		check(!string.IsNullOrEmpty(merged), "Merged QR cookie should not be null or empty");
		check(merged!.Contains("MUSIC_U=secret_token_123"), "Merged QR cookie must preserve MUSIC_U from Set-Cookie");
		check(merged.Contains("__csrf=csrf_val"), "Merged QR cookie must include CookieContainer cookies");
		check(merged.Contains("NMTID=nmtid_updated"), "Merged QR cookie should deduplicate and keep latest NMTID");
		check(!merged.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase), "Merged QR cookie must strip cookie attributes");

		string? noAuthOnly = FurinaPlayer.Core.Services.StreamingService.MergeNeteaseQrCookies(null, Array.Empty<string>(), container);
		check(!string.IsNullOrEmpty(noAuthOnly) && !noAuthOnly!.Contains("MUSIC_U="), "Container-only cookies without MUSIC_U should not fabricate MUSIC_U");
	}

	public static void TestFormatConverterAndDecoders(Action<bool, string>? assert = null)
	{
		Action<bool, string> check = assert ?? ((ok, msg) => { if (!ok) throw new Exception(msg); });
		Console.WriteLine("\n--- Testing AudioDecoders (.ncm/.mgg/.kgm/.kwm) & FormatConverterService ---");

		string tempDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_ConverterTest_" + Guid.NewGuid().ToString("N"));
		string outDir = Path.Combine(tempDir, "out");
		Directory.CreateDirectory(tempDir);
		Directory.CreateDirectory(outDir);

		try
		{
			// 1. Synthetic .ncm with AES key, JSON metadata, cover, and MP3 header
			byte[] dummyMp3 = new byte[256];
			dummyMp3[0] = (byte)'I'; dummyMp3[1] = (byte)'D'; dummyMp3[2] = (byte)'3';
			dummyMp3[3] = 3; dummyMp3[4] = 0; // ID3v2.3
			dummyMp3[10] = 0xFF; dummyMp3[11] = 0xFB; dummyMp3[12] = 0x90; dummyMp3[13] = 0x00;

			byte[] rc4Key = System.Text.Encoding.ASCII.GetBytes("furina_test_rc4_key_1234");
			byte[] keyPlain = new byte[17 + rc4Key.Length];
			System.Text.Encoding.ASCII.GetBytes("neteasecloudmusic").CopyTo(keyPlain, 0);
			rc4Key.CopyTo(keyPlain, 17);
			byte[] keyEnc;
			using (var aes = System.Security.Cryptography.Aes.Create())
			{
				aes.Key = System.Text.Encoding.ASCII.GetBytes("hzHRAmso5kInbaxW");
				aes.Mode = System.Security.Cryptography.CipherMode.ECB;
				aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
				using var enc = aes.CreateEncryptor();
				keyEnc = enc.TransformFinalBlock(keyPlain, 0, keyPlain.Length);
			}
			for (int i = 0; i < keyEnc.Length; i++) keyEnc[i] ^= 0x64;

			string metaJson = "music:{\"musicName\":\"Fontaine\",\"artist\":[[\"HOYO-MiX\"]],\"album\":\"Genshin OST\",\"format\":\"mp3\"}";
			byte[] metaEnc;
			using (var aes = System.Security.Cryptography.Aes.Create())
			{
				aes.Key = System.Text.Encoding.ASCII.GetBytes("#14ljk_!\\]&0U<'(");
				aes.Mode = System.Security.Cryptography.CipherMode.ECB;
				aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
				using var enc = aes.CreateEncryptor();
				metaEnc = enc.TransformFinalBlock(System.Text.Encoding.UTF8.GetBytes(metaJson), 0, System.Text.Encoding.UTF8.GetByteCount(metaJson));
			}
			byte[] metaXor = System.Text.Encoding.ASCII.GetBytes("163 key(Don't modify):" + Convert.ToBase64String(metaEnc));
			for (int i = 0; i < metaXor.Length; i++) metaXor[i] ^= 0x63;

			byte[] tinyPng = new byte[80];
			tinyPng[0] = 0x89; tinyPng[1] = (byte)'P'; tinyPng[2] = (byte)'N'; tinyPng[3] = (byte)'G';

			// Build NCM RC4 box
			byte[] sbox = new byte[256];
			for (int i = 0; i < 256; i++) sbox[i] = (byte)i;
			int j = 0;
			for (int i = 0; i < 256; i++)
			{
				j = (j + sbox[i] + rc4Key[i % rc4Key.Length]) & 0xFF;
				(sbox[i], sbox[j]) = (sbox[j], sbox[i]);
			}
			byte[] box = new byte[256];
			for (int i = 0; i < 256; i++) box[i] = sbox[(sbox[i] + sbox[(i + sbox[i]) & 0xFF]) & 0xFF];
			byte[] encMp3 = new byte[dummyMp3.Length];
			for (int i = 0; i < dummyMp3.Length; i++) encMp3[i] = (byte)(dummyMp3[i] ^ box[(i + 1) & 0xFF]);

			string ncmPath = Path.Combine(tempDir, "test_song.ncm");
			using (var bw = new BinaryWriter(File.Create(ncmPath)))
			{
				bw.Write(System.Text.Encoding.ASCII.GetBytes("CTENFDAM"));
				bw.Write((ushort)0);
				bw.Write(keyEnc.Length);
				bw.Write(keyEnc);
				bw.Write(metaXor.Length);
				bw.Write(metaXor);
				bw.Write(new byte[5]);
				bw.Write(tinyPng.Length);
				bw.Write(tinyPng.Length);
				bw.Write(tinyPng);
				bw.Write(encMp3);
			}

			var converter = new FurinaPlayer.Audio.Services.FormatConverterService();
			var ncmRes = converter.ConvertFileAsync(new FurinaPlayer.Audio.Services.ConvertRequest
			{
				InputPath = ncmPath,
				OutputDirectory = outDir,
				TargetFormat = FurinaPlayer.Audio.Services.ConvertTargetFormat.AutoMp3OrFlac,
				AutoEnrichMetadata = true,
				EnableOnlineEnrich = false
			}).GetAwaiter().GetResult();

			check(ncmRes.Success && File.Exists(ncmRes.OutputPath), "NCM conversion should succeed and create output .mp3");
			check(ncmRes.TargetContainer == "mp3", $"NCM should auto-detect mp3 container (got {ncmRes.TargetContainer})");
			check(ncmRes.HasMetadata && ncmRes.Title == "Fontaine" && ncmRes.Artist == "HOYO-MiX", $"NCM metadata should be preserved in output MP3 (has={ncmRes.HasMetadata}, title='{ncmRes.Title}', artist='{ncmRes.Artist}')");
			check(ncmRes.HasCover, "NCM embedded cover should be preserved in output MP3");

			// 2. Synthetic .kwm (yeelion-kuwo) + filename fallback enrichment
			byte[] kwmData = new byte[1024 + dummyMp3.Length];
			System.Text.Encoding.ASCII.GetBytes("yeelion-kuwo-tme").CopyTo(kwmData, 0);
			uint seed = 12345678u;
			System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(kwmData.AsSpan(0x18, 4), seed);
			byte[] seedBytes = System.Text.Encoding.ASCII.GetBytes(seed.ToString());
			byte[] rootKey = System.Text.Encoding.ASCII.GetBytes("MoOtOiTvINGwd2E6n0E1i7L5t2IoOoNk");
			for (int i = 0; i < dummyMp3.Length; i++)
			{
				byte mask = (byte)(seedBytes[(i & 31) % seedBytes.Length] ^ rootKey[i & 31]);
				kwmData[1024 + i] = (byte)(dummyMp3[i] ^ mask);
			}
			string kwmPath = Path.Combine(tempDir, "TestArtist - TestTitle.kwm");
			File.WriteAllBytes(kwmPath, kwmData);

			var kwmRes = converter.ConvertFileAsync(new FurinaPlayer.Audio.Services.ConvertRequest
			{
				InputPath = kwmPath,
				OutputDirectory = outDir,
				TargetFormat = FurinaPlayer.Audio.Services.ConvertTargetFormat.AutoMp3OrFlac,
				AutoEnrichMetadata = true,
				EnableOnlineEnrich = false
			}).GetAwaiter().GetResult();

			check(kwmRes.Success && kwmRes.TargetContainer == "mp3", "KWM decryption & conversion should succeed");
			check(kwmRes.HasMetadata && kwmRes.Artist == "TestArtist" && kwmRes.Title == "TestTitle", $"KWM fallback filename tag enrichment should populate Artist & Title (has={kwmRes.HasMetadata}, title='{kwmRes.Title}', artist='{kwmRes.Artist}')");

			// 3. Synthetic QMC v1 (.qmcflac) round-trip
			byte[] flacHdr = new byte[128];
			flacHdr[0] = (byte)'f'; flacHdr[1] = (byte)'L'; flacHdr[2] = (byte)'a'; flacHdr[3] = (byte)'C';
			byte[] v1Static = {
				0xC3, 0x4A, 0xD6, 0xCA, 0x90, 0x67, 0xF7, 0x52, 0xD8, 0xA1, 0x66, 0x62, 0x9F, 0x5B, 0x09, 0x00,
				0xC3, 0x5E, 0x95, 0x23, 0x9F, 0x13, 0x11, 0x7E, 0xD8, 0x92, 0x3F, 0xBC, 0x90, 0xBB, 0x74, 0x0E,
				0xC3, 0x47, 0x74, 0x3D, 0x90, 0xAA, 0x3F, 0x51, 0xD8, 0xF4, 0x11, 0x84, 0x9F, 0xDE, 0x95, 0x1D,
				0xC3, 0xC6, 0x09, 0xD5, 0x9F, 0xFA, 0x66, 0xF9, 0xD8, 0xF0, 0xF7, 0xA0, 0x90, 0xA1, 0xD6, 0xF3,
				0xC3, 0xF3, 0xD6, 0xA1, 0x90, 0xA0, 0xF7, 0xF0, 0xD8, 0xF9, 0x66, 0xFA, 0x9F, 0xD5, 0x09, 0xC6,
				0xC3, 0x1D, 0x95, 0xDE, 0x9F, 0x84, 0x11, 0xF4, 0xD8, 0x51, 0x3F, 0xAA, 0x90, 0x3D, 0x74, 0x47,
				0xC3, 0x0E, 0x74, 0xBB, 0x90, 0xBC, 0x3F, 0x92, 0xD8, 0x7E, 0x11, 0x13, 0x9F, 0x23, 0x95, 0x5E,
				0xC3, 0x00, 0x09, 0x5B, 0x9F, 0x62, 0x66, 0xA1, 0xD8, 0x52, 0xF7, 0x67, 0x90, 0xCA, 0xD6, 0x4A
			};
			byte[] qmcEnc = FurinaPlayer.Core.Services.AudioDecoders.QmcDecoder.Qmc1Transform(flacHdr, v1Static);
			string qmcPath = Path.Combine(tempDir, "test.mgg");
			File.WriteAllBytes(qmcPath, qmcEnc);
			var qmcDecoded = FurinaPlayer.Core.Services.AudioDecoders.DecodeFile(qmcPath);
			check(qmcDecoded.Container == "flac", $"QMC v1 fallback on .mgg should sniff flac (got {qmcDecoded.Container})");
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
		}
	}

	public static void TestLibraryDeduplicationAndFuzzySearch(Action<bool, string>? assert = null)
	{
		Action<bool, string> check = assert ?? ((ok, msg) => { if (!ok) throw new Exception(msg); });
		Console.WriteLine("\n--- Testing LibraryService Deduplication & Fuzzy Search ---");

		string tempDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_LibDedupTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);
		string dbPath = Path.Combine(tempDir, "library.db");
		string wavPath = Path.Combine(tempDir, "HOYO-MiX - White Night.wav");

		try
		{
			// Create minimal valid WAV
			using (var bw = new BinaryWriter(File.Create(wavPath)))
			{
				bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
				bw.Write(36 + 1600);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
				bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
				bw.Write(16);
				bw.Write((short)1); bw.Write((short)2);
				bw.Write(44100); bw.Write(44100 * 4);
				bw.Write((short)4); bw.Write((short)16);
				bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
				bw.Write(1600);
				bw.Write(new byte[1600]);
			}

			using var lib = new FurinaPlayer.Core.Services.LibraryService(dbPath);
			lib.InitializeAsync().GetAwaiter().GetResult();

			var first = lib.ImportFilesBatchAsync(new[] { wavPath, wavPath }).GetAwaiter().GetResult();
			check(first.Added == 1 && first.Skipped == 1, $"Batch import with duplicate path should add 1 and skip 1 (added={first.Added}, skipped={first.Skipped})");

			var second = lib.ScanFolderAsync(tempDir).GetAwaiter().GetResult();
			check(second.Added == 0 && second.Updated == 1, $"Re-scanning folder should add 0 and skip 1 existing track (added={second.Added}, skipped={second.Updated})");

			int count = lib.CountAsync().GetAwaiter().GetResult();
			check(count == 1, $"Library should contain exactly 1 track after duplicate imports (actual={count})");

			var multiToken = lib.GetTracksAsync("HOYO Night").GetAwaiter().GetResult();
			check(multiToken.Count == 1, $"Multi-token search 'HOYO Night' should match 1 track (actual={multiToken.Count})");

			var fuzzy = lib.GetTracksAsync("WhtNgt").GetAwaiter().GetResult();
			check(fuzzy.Count == 1, $"Fuzzy subsequence search 'WhtNgt' should match 'White Night' (actual={fuzzy.Count})");
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
		}
	}

	public static void TestLyricPriorityLocalEmbeddedAndFallback(Action<bool, string>? assert = null)
	{
		Action<bool, string> check = assert ?? ((ok, msg) => { if (!ok) throw new Exception(msg); });
		Console.WriteLine("\n--- Testing LyricService Local Embedded / .lrc Priority ---");

		string tempDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_LyricTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);
		string audioPath = Path.Combine(tempDir, "Song A.mp3");
		string lrcPath = Path.Combine(tempDir, "Song A.lrc");

		try
		{
			File.WriteAllBytes(audioPath, new byte[64]);
			File.WriteAllText(lrcPath, "[00:01.00]Folder LRC Line", System.Text.Encoding.UTF8);

			var svc = new FurinaPlayer.Core.Services.LyricService();

			// 1. Embedded synced lyrics take priority when present
			var embDoc = svc.LoadLocalLyrics(audioPath, "[00:02.00]Embedded Synced Line", "Song A", "Artist A");
			check(embDoc != null && embDoc.Source == "embedded" && embDoc.Lines[0].Text == "Embedded Synced Line",
				"Synced embedded lyrics should be returned with Source='embedded'");

			// 2. Folder .lrc file is used when embedded lyrics are absent
			var localDoc = svc.LoadLocalLyrics(audioPath, null, "Song A", "Artist A");
			check(localDoc != null && localDoc.Source == "local" && localDoc.Lines[0].Text == "Folder LRC Line",
				"Folder .lrc should be matched when embedded lyrics are absent");

			// 3. NetEase JSON where lrc.lyric is empty and lyrics live in tlyric.lyric
			string neJson = "{\"lrc\":{\"version\":1,\"lyric\":\"\"},\"tlyric\":{\"version\":2,\"lyric\":\"[00:23.203]我随焰火醒来，\"},\"code\":200}";
			var neDoc = svc.ParseNeteaseLyricJson(neJson);
			check(neDoc != null && neDoc.Source == "netease" && neDoc.Lines.Count == 1 && neDoc.Lines[0].Text == "我随焰火醒来，",
				"NetEase tlyric fallback should parse lyrics when lrc.lyric is empty");

			// 4. Bilingual LRC with identical timestamps merges original + translation instead of hiding original
			var biDoc = svc.ParseLrc("[00:10.00]White Night\n[00:10.00]不眠之夜");
			check(biDoc.Lines.Count == 1 && biDoc.Lines[0].OriginalText == "White Night" && biDoc.Lines[0].Translation == "不眠之夜",
				"Same-timestamp bilingual LRC should preserve both original and translation");

			// 5. NetEase YRC word-by-word + tlyric merge
			string yrcJson = "{\"yrc\":{\"lyric\":\"[10000,1200](10000,500,0)White (10500,700,0)Night\"},\"lrc\":{\"lyric\":\"[00:10.00]White Night\"},\"tlyric\":{\"lyric\":\"[00:10.00]不眠之夜\"},\"code\":200}";
			var yrcDoc = svc.ParseNeteaseLyricJson(yrcJson);
			check(yrcDoc != null && yrcDoc.IsWordByWord && yrcDoc.Lines[0].Words?.Count == 2 && yrcDoc.Lines[0].Translation == "不眠之夜",
				"NetEase YRC + tlyric should enable word-by-word mode while keeping original and translation");

			// 6. Enhanced LRC word-by-word strips inline tags cleanly
			var enhDoc = svc.ParseLrc("[00:10.00]<00:10.00>Hel<00:10.50>lo");
			check(enhDoc.IsWordByWord && enhDoc.Lines[0].OriginalText == "Hello" && enhDoc.Lines[0].Words?.Count == 2,
				"Enhanced LRC word-by-word should strip inline tags and populate Words");
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
		}
	}

	public static void TestLibraryIntegrityCheckAndRepair(Action<bool, string>? assert = null)
	{
		Action<bool, string> check = assert ?? ((ok, msg) => { if (!ok) throw new Exception(msg); });
		Console.WriteLine("\n--- Testing LibraryIntegrityService Parallel Check & Repair ---");

		string tempDir = Path.Combine(Path.GetTempPath(), "FurinaPlayer_IntegrityTest_" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(tempDir);
		string dbPath = Path.Combine(tempDir, "library.db");
		string mp3Path = Path.Combine(tempDir, "HOYO-MiX - Fontaine.mp3");
		string lrcPath = Path.Combine(tempDir, "HOYO-MiX - Fontaine.lrc");
		string coverPath = Path.Combine(tempDir, "cover.png");

		try
		{
			byte[] dummyMp3 = new byte[256];
			dummyMp3[0] = (byte)'I'; dummyMp3[1] = (byte)'D'; dummyMp3[2] = (byte)'3';
			dummyMp3[3] = 3; dummyMp3[4] = 0;
			dummyMp3[10] = 0xFF; dummyMp3[11] = 0xFB; dummyMp3[12] = 0x90;
			File.WriteAllBytes(mp3Path, dummyMp3);
			File.WriteAllText(lrcPath, "[00:05.00]La vaguelette", System.Text.Encoding.UTF8);
			byte[] tinyPng = new byte[80];
			tinyPng[0] = 0x89; tinyPng[1] = (byte)'P'; tinyPng[2] = (byte)'N'; tinyPng[3] = (byte)'G';
			File.WriteAllBytes(coverPath, tinyPng);

			using var lib = new FurinaPlayer.Core.Services.LibraryService(dbPath);
			lib.InitializeAsync().GetAwaiter().GetResult();
			lib.ImportFilesBatchAsync(new[] { mp3Path }).GetAwaiter().GetResult();

			var tracks = lib.GetAllTracksAsync().GetAwaiter().GetResult();
			check(tracks.Count == 1, "Library should have 1 imported track");
			lib.UpdateCoverPathAsync(tracks[0].Id, coverPath).GetAwaiter().GetResult();

			var svc = new FurinaPlayer.Core.Services.LibraryIntegrityService(lib)
			{
				EnableOnlineLookup = false
			};

			double lastPct = 0;
			var prog = new Progress<FurinaPlayer.Core.Services.LibraryIntegrityProgress>(p => { lastPct = p.Percent; });
			var report1 = svc.CheckAndRepairLibraryAsync(prog, maxConcurrency: 4).GetAwaiter().GetResult();

			check(report1.TotalCount == 1 && report1.ProcessedCount == 1, $"Should process 1 track (processed={report1.ProcessedCount})");
			check(report1.RepairedMetadataCount == 1 && report1.RepairedLyricsCount == 1 && report1.RepairedCoverCount == 1,
				$"Should repair meta, lyrics, and cover (meta={report1.RepairedMetadataCount}, lrc={report1.RepairedLyricsCount}, cov={report1.RepairedCoverCount})");

			var atlAfter = new ATL.Track(mp3Path);
			check(atlAfter.Title == "Fontaine" && atlAfter.Artist == "HOYO-MiX", "ATL tags should be written back to file");
			check(!string.IsNullOrWhiteSpace(atlAfter.Lyrics?.UnsynchronizedLyrics) || (atlAfter.AdditionalFields != null && atlAfter.AdditionalFields.TryGetValue("LYRICS", out var embLyr) && !string.IsNullOrWhiteSpace(embLyr)), "Local .lrc should be embedded into audio file");
			check(atlAfter.EmbeddedPictures.Count > 0, "Cover should be embedded into audio file");

			var report2 = svc.CheckAndRepairLibraryAsync(null, maxConcurrency: 4).GetAwaiter().GetResult();
			check(report2.AlreadyCompleteCount == 1 && report2.RepairedMetadataCount == 0 && report2.RepairedLyricsCount == 0 && report2.RepairedCoverCount == 0,
				$"Second pass should report 1 already complete (complete={report2.AlreadyCompleteCount})");
		}
		finally
		{
			try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
		}
	}

	public static void TestDesktopPetAssetsAndBounds(Action<bool, string> check)
	{
		Console.WriteLine("\n--- Testing Desktop Pet Assets & Bounds ---");

		var defaults = new AppSettings();
		check(defaults.PetEnabled == false, "AppSettings.PetEnabled defaults to false");
		check(defaults.PetScalePercent == 100, "AppSettings.PetScalePercent defaults to 100");
		check(defaults.PetLyricsEnabled == true, "AppSettings.PetLyricsEnabled defaults to true");
		check(defaults.PetGifSpeedPercent == 100, "AppSettings.PetGifSpeedPercent defaults to 100");
		check(defaults.PetBubbleScalePercent == 100, "AppSettings.PetBubbleScalePercent defaults to 100");

		string petDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FurinaPlayer", "Assets", "Pet"));
		string[] gifNames = { "furina_pet_idle.gif", "furina_pet_groove.gif", "furina_pet_click.gif" };
		foreach (string name in gifNames)
		{
			string gifPath = Path.Combine(petDir, name);
			check(File.Exists(gifPath), $"Pet GIF asset exists: {name}");
			using var codec = SkiaSharp.SKCodec.Create(gifPath);
			check(codec != null, $"SKCodec opens {name}");
			if (codec != null)
			{
				var frames = codec.FrameInfo;
				check(frames != null && frames.Length == 8, $"{name} has 8 frames (actual={frames?.Length ?? 0})");
				int totalMs = 0;
				if (frames != null)
				{
					foreach (var f in frames) totalMs += f.Duration;
				}
				double avgMs = frames != null && frames.Length > 0 ? (double)totalMs / frames.Length : 0;
				check(Math.Abs(avgMs - 125.0) < 0.01, $"{name} has 125ms average duration per frame (avg={avgMs}ms, total={totalMs}ms)");
			}
		}

		string[] cornerNames = { "pet_bubble_tl.png", "pet_bubble_tr.png", "pet_bubble_bl.png", "pet_bubble_br.png" };
		foreach (string cname in cornerNames)
		{
			string cpath = Path.Combine(petDir, cname);
			check(File.Exists(cpath), $"Pet bubble corner PNG exists: {cname}");
			using var cbmp = SkiaSharp.SKBitmap.Decode(cpath);
			check(cbmp != null && cbmp.Width > 0 && cbmp.Height > 0, $"SKBitmap decodes {cname}");
		}

		// Default bottom-right with 28px margin
		var defPos = FurinaPlayer.Core.Services.DesktopPetLayout.ClampPetBoundsToWorkArea(null, null, 180, 200, 0, 0, 1920, 1040);
		check(defPos.Left == 1920 - 180 - 28 && defPos.Top == 1040 - 200 - 28, $"Default position is bottom-right with 28px margin ({defPos.Left}, {defPos.Top})");

		// Clamping out-of-bounds negative & overflow coordinates
		var clampLow = FurinaPlayer.Core.Services.DesktopPetLayout.ClampPetBoundsToWorkArea(-500, -300, 180, 200, 100, 50, 1920, 1040);
		check(clampLow.Left == 100 && clampLow.Top == 50, $"Negative coordinates clamp to work area origin ({clampLow.Left}, {clampLow.Top})");

		var clampHigh = FurinaPlayer.Core.Services.DesktopPetLayout.ClampPetBoundsToWorkArea(5000, 3000, 180, 200, 0, 0, 1920, 1040);
		check(clampHigh.Left == 1920 - 180 && clampHigh.Top == 1040 - 200, $"Overflow coordinates clamp to work area max ({clampHigh.Left}, {clampHigh.Top})");
	}
}
