"use strict";
/**
 * Adobe Audition DAW lyrics theme for ECHO Workshop (protocol v1).
 *
 * Data policy — every panel is driven by real host data:
 *   playback:read  -> state.playback / state.currentTrack / clock
 *   lyrics:read    -> lyrics document + lyrics peek
 *   audio:spectrum -> spectrum.bands / energy / transient + levels.peakDb/rmsDb
 *   storage        -> play history + measured envelope cache + UI prefs
 *
 * Deliberately NOT shown (cannot be obtained through the whitelisted
 * capabilities of a lyrics-view theme, see DEVLOG):
 *   - the播放队列 (needs queue:read, which the host rejects for lyrics-view)
 *   - PCM samples / true waveform peaks (protocol exposes spectrum + meters only)
 *   - independent L/R meters (host documents "Never independent L/R")
 */
(() => {
  // ── helpers ───────────────────────────────────────────────────────────────
  const el = (id) => document.getElementById(id);
  const setText = (id, value) => {
    const node = el(id);
    if (node && node.textContent !== value) node.textContent = value;
  };
  const clamp = (v, min, max) => Math.min(max, Math.max(min, v));
  const sleep = (ms) => new Promise((resolve) => window.setTimeout(resolve, ms));
  const num = (v, fallback = null) =>
    typeof v === "number" && Number.isFinite(v) ? v : fallback;
  const fmtClock = (sec, withMs = true) => {
    if (!Number.isFinite(sec) || sec < 0) return withMs ? "--:--.---" : "--:--";
    const totalMs = Math.floor(sec * 1000);
    const m = Math.floor(totalMs / 60000);
    const s = Math.floor((totalMs % 60000) / 1000);
    const ms = totalMs % 1000;
    const base = `${String(m).padStart(2, "0")}:${String(s).padStart(2, "0")}`;
    return withMs ? `${base}.${String(ms).padStart(3, "0")}` : base;
  };
  const fmtTag = (ms) => {
    const total = Math.max(0, ms) / 1000;
    return `${String(Math.floor(total / 60)).padStart(2, "0")}:${(total % 60).toFixed(2).padStart(5, "0")}`;
  };
  const fmtDb = (db) => (db === null ? "—" : `${db >= 0 ? "+" : ""}${db.toFixed(1)}`);
  const fmtDuration = (sec) => (Number.isFinite(sec) && sec > 0 ? fmtClock(sec, false) : "—");
  const dbToRatio = (db, floor = -72) => (db === null ? 0 : clamp((db - floor) / -floor, 0, 1));


  // ── Multilingual Localization (i18n) ──────────────────────────────────────
  const SUPPORTED_LANGS = [
    { id: "zh", label: "简体中文", englishName: "Chinese" },
    { id: "en", label: "English", englishName: "English" },
    { id: "fr", label: "Français", englishName: "French" },
    { id: "ja", label: "日本語", englishName: "Japanese" },
    { id: "ko", label: "한국어", englishName: "Korean" },
  ];

  const I18N = {
    zh: {
      langName: "简体中文",
      "menu.file": "文件(F)",
      "menu.edit": "编辑(E)",
      "menu.multitrack": "多轨(M)",
      "menu.clip": "剪辑(C)",
      "menu.effects": "效果(S)",
      "menu.zoom": "缩放(Z)",
      "menu.transport": "控制(T)",
      "menu.window": "窗口(W)",
      "menu.language": "语言(L)",
      "menu.help": "帮助(H)",

      "theme.light": "日间",
      "theme.dark": "黑暗",
      "theme.toggleTitle": "切换日间 / 黑暗模式 (Ctrl+T)",
      "wave.blue": "天蓝",
      "wave.pink": "粉樱",
      "wave.toggleTitle": "切换波形色彩 (淡雅天蓝 / 柔美粉樱) (Ctrl+Alt+W)",
      "lang.toggleTitle": "切换界面语言 (Language)",

      "status.connecting": "等待宿主握手…",
      "status.connected": "已连接",
      "status.stopped": "未播放",
      "status.playing": "播放中",
      "status.paused": "已暂停",

      "panel.tracks": "曲目列表",
      "panel.history": "播放记录",
      "panel.tracksAria": "曲目列表与播放记录",
      "queue.waiting": "等待播放…",
      "queue.colIcon": "",
      "queue.colTitle": "曲目",
      "queue.colDuration": "时长",
      "queue.note": "点击任意曲目即可快速切换并播放，高亮表示当前播放曲目。",
      "queue.emptyHistory": "暂无播放记录",

      "panel.art": "专辑封面",
      "panel.artAria": "专辑封面与黑胶唱片",
      "art.hostCover": "宿主封面",
      "art.embedded": "内嵌封面",
      "art.default": "默认唱片",

      "panel.properties": "音频属性",
      "panel.propertiesAria": "音频属性与链路",
      "prop.codec": "编码格式",
      "prop.sampleRate": "采样率",
      "prop.bitDepth": "位深度",
      "prop.outputDevice": "输出设备",
      "prop.outputEngine": "输出后端",
      "prop.outputMode": "输出模式",
      "prop.replayGain": "重放增益",
      "prop.meterSource": "输入电平源",
      "prop.sourceRealtime": "宿主实时总线",

      "panel.waveform": "波形包络",
      "panel.waveformAria": "波形与频谱",
      "wave.hint": "由宿主实时电平绘制（协议不提供 PCM 采样）",
      "wave.scanBtnTitle": "预扫描整首波形（静音扫描，可再次点击取消）",
      "wave.modeRealtime": "实时",
      "wave.modePrerender": "预渲染",
      "wave.modeTitleRealtime": "波形模式：实时（波形随播放绘制）",
      "wave.modeTitlePrerender": "波形模式：预渲染（全曲静音快扫）",
      "wave.qualityFine": "精细",
      "wave.qualityStandard": "标准",
      "wave.qualityFast": "极速",
      "wave.qualityTitle": "预扫描精度：极速 / 标准 / 精细（当前：{quality}）",
      "wave.qualityFineDesc": "精细 · 5x高密度超采样",
      "wave.qualityStandardDesc": "标准 · 5x平衡采样",
      "wave.qualityFastDesc": "极速 · 5x快速概览",
      "wave.zoomInTitle": "水平放大 (Ctrl++)",
      "wave.zoomOutTitle": "水平缩小 (Ctrl+-)",
      "wave.zoomFitTitle": "适应窗口 (Ctrl+0)",
      "wave.navTitle": "点击或拖拽定位",
      "wave.waterfallTitle": "频谱瀑布（实时频段能量）",

      "panel.analyzer": "母带分析器",
      "panel.analyzerAria": "效果 - Insight 2 Pro",
      "analyzer.vstTitle": "效果 - Insight 2 Pro",
      "analyzer.stateLive": "实时",
      "analyzer.stateFrozen": "已冻结",
      "analyzer.sourceLabel": "数据源",
      "analyzer.sourceVal": "宿主输入电平 · 频谱",
      "analyzer.freezeTitle": "冻结 / 恢复显示 (F)",
      "analyzer.resetTitle": "重置峰值统计 (P)",
      "analyzer.radarTitle": "响度雷达（实时 RMS）",
      "analyzer.spectrumTitle": "频谱与频率分布",
      "analyzer.levelsTitle": "输入电平（单路 Peak / RMS · dBFS）",
      "analyzer.soundfieldTitle": "Sound Field",
      "analyzer.soundfieldGear": "声场显示选项",
      "analyzer.tabPolarSample": "Polar Sample",
      "analyzer.tabPolarLevel": "Polar Level",
      "analyzer.tabLissajous": "Lissajous",

      "panel.lyrics": "歌词",
      "panel.lyricsAria": "歌词与同步显示",
      "lyrics.waiting": "等待歌词",
      "lyrics.synced": "同步歌词",
      "lyrics.plain": "纯文本歌词",
      "lyrics.instrumental": "纯音乐，请欣赏",
      "lyrics.empty": "暂无可用歌词",

      "transport.aria": "播放控制栏",
      "transport.total": "总时长",
      "transport.remain": "剩余",
      "transport.prev": "上一曲 (Ctrl+←)",
      "transport.rw": "快退 5 秒 (←)",
      "transport.playpause": "播放/暂停 (Space)",
      "transport.ff": "快进 5 秒 (→)",
      "transport.next": "下一曲 (Ctrl+→)",
      "transport.repeat": "列表循环",
      "transport.repeatOne": "单曲循环",
      "transport.shuffle": "随机播放",
      "transport.sequential": "顺序播放",
      "transport.mute": "静音 (M)",
      "transport.unmute": "取消静音 (M)",
      "transport.volume": "音量",
      "transport.seek": "播放进度",

      "prefs.title": "首选项 (Preferences)",
      "prefs.renderModeTitle": "波形渲染模式",
      "prefs.renderModeEnable": "启用全曲波形预渲染 (Pre-render)",
      "prefs.renderModeHint": "默认关闭（实时模式）。关闭时波形随曲目播放动态绘制，性能最高且不占用多余内存。",
      "prefs.qualityTitle": "预扫描采样精度 (Scan Quality - 5x超采样)",
      "prefs.qualityFineOpt": "精细 (Fine - 5x 高密度超采样 · 默认)",
      "prefs.qualityStandardOpt": "标准 (Standard - 5x 平衡采样)",
      "prefs.qualityFastOpt": "极速 (Fast - 5x 快速概览)",
      "prefs.rangeTitle": "电平表量程标准 (Dynamic Range)",
      "prefs.rangeDesc": "主电平指示器与分轨刻度已扩展至 <strong>0 ~ -60 dBFS</strong>，完整呈现专业级母带动态范围与底噪细节。",
      "prefs.languageTitle": "界面语言 (Language)",
      "prefs.languageHint": "支持简体中文、English、Français、日本語、한국어，切换后立即生效并保存。",

      "shortcuts.title": "键盘快捷键 (Keyboard Shortcuts)",
      "shortcuts.playpause": "播放 / 暂停",
      "shortcuts.rwff": "快退 5 秒 / 快进 5 秒",
      "shortcuts.prevnext": "上一曲 / 下一曲",
      "shortcuts.vol": "音量增大 / 减小 5%",
      "shortcuts.mute": "静音 / 恢复音量",
      "shortcuts.zoom": "放大时间轴 / 缩小时间轴",
      "shortcuts.zoomFit": "重置为全曲概览",
      "shortcuts.centerPlayhead": "居中对齐播放指针",
      "shortcuts.freeze": "冻结 / 恢复频谱分析仪",
      "shortcuts.resetPeak": "重置所有电平峰值指标",
      "shortcuts.theme": "切换日间 / 黑暗模式",
      "shortcuts.waveColor": "切换波形色彩 (天蓝 / 粉樱)",
      "shortcuts.prefs": "首选项设置",
      "shortcuts.reload": "重新加载 DAW 界面",
      "shortcuts.close": "关闭弹窗 / 返回 ECHO",

      "about.title": "关于 Adobe Audition DAW",
      "about.headerTitle": "Adobe Audition DAW Theme for ECHO",
      "about.version": "版本 1.2.0 (Multilingual Edition)",
      "about.desc": "专为 ECHO 音乐播放器打造的高仿真 Adobe Audition 专业数字音频工作站 (DAW) 沉浸式主题与实时分析仪套件。",
      "about.featTitle": "功能特性：",
      "about.f1": "• 支持经典黑暗 / 日间明亮双主题无缝切换 (Ctrl+T)",
      "about.f2": "• 波形色彩支持淡雅天蓝与柔美粉樱双色彩方案 (Ctrl+Alt+W)",
      "about.f3": "• 完整多语言本地化支持：中文、英语、法语、日语、韩语",
      "about.f4": "• 专业级多轨时间轴与 0 ~ -60 dBFS 真实动态波形可视化",
      "about.f5": "• 实时 FFT 3D 频谱瀑布流（播放暂停智能休眠锁存）",
      "about.f6": "• 极坐标与李萨如 (Lissajous) 立体声相位声场雷达仪",
      "about.f7": "• 广播级 LUFS 响度与 True Peak 峰值电平指示",
      "about.f8": "• 顶栏专业下拉菜单与全局快捷键（文件、编辑、多轨、控制、缩放等）",
      "about.f9": "• 智能多源流媒体与本地封面回退保护，零闪烁高保真",
      "about.footer": "Inspired by Adobe Audition CC &bull; Built for ECHO",

      "toast.langSwitched": "已切换界面语言：简体中文",
      "toast.pathCopied": "已复制音频路径至剪贴板",
      "toast.pathFailed": "复制失败",
      "toast.noPath": "当前无可用音频文件路径",
      "toast.metaCopied": "已复制曲目元数据 JSON",
      "toast.copied": "已复制",
      "toast.peakReset": "已重置所有峰值电平指标",
      "toast.scanStarted": "已启动当前曲目的完整波形超采样扫描",
      "toast.scanRunning": "波形扫描正在执行中…",
      "toast.scanCancelled": "已请求终止当前扫描任务",
      "toast.noScan": "当前无进行中的扫描任务",
      "toast.modeRealtime": "已切换为实时波形模式",
      "toast.modePrerender": "已开启预渲染模式，开始扫描波形",
      "toast.scanQuality": "已设置预扫描精度：{quality}",
      "toast.soundfield": "已切换声场显示：{mode}",
      "toast.playheadCentered": "已居中对齐播放指针",
      "toast.viewReset": "视图已重置为全局概览",
      "toast.posCopied": "当前播放位置：{time}",
      "toast.frozen": "频谱与声场已冻结",
      "toast.unfrozen": "频谱与声场已恢复实时",
      "toast.waterfallCleared": "已清空频谱瀑布图历史",
      "toast.repeatAll": "已开启列表循环",
      "toast.repeatOne": "已开启单曲循环",
      "toast.sequential": "已切换为列表顺序播放",
      "toast.repeatOff": "已关闭循环",
      "toast.muted": "已静音",
      "toast.unmuted": "已取消静音",
      "toast.volStandard": "已设为标准监听音量 (80%)",
      "toast.volMax": "已设为最大输出音量 (100%)",
      "toast.syncQueue": "正在从宿主同步播放队列…",
      "toast.resize": "已重置并自适应所有 DAW 画布",
      "toast.themeLight": "已切换为日间明亮主题",
      "toast.themeDark": "已切换为黑暗专业主题",
      "toast.wavePink": "已切换为柔美粉樱波形色彩",
      "toast.waveBlue": "已切换为淡雅天蓝波形色彩",

      "menu.file.reload": "重新加载界面 (Reload)",
      "menu.file.copyPath": "复制当前音频路径",
      "menu.file.copyMeta": "复制曲目元数据 (JSON)",
      "menu.file.prefs": "首选项 (Preferences)...",
      "menu.file.back": "返回 ECHO 主界面",

      "menu.edit.copyTitle": "复制歌曲标题",
      "menu.edit.copyArtist": "复制艺术家信息",
      "menu.edit.resetPeak": "重置历史峰值指标 (Reset Peak)",
      "menu.edit.rescan": "重新扫描当前曲目波形",

      "menu.multi.realtime": "波形绘制：实时模式 (随播放绘制)",
      "menu.multi.prerender": "波形绘制：预渲染模式 (全曲扫描)",
      "menu.multi.fine": "扫描精度：精细 (Fine · 5x超采样)",
      "menu.multi.standard": "扫描精度：标准 (Standard · 5x采样)",
      "menu.multi.fast": "扫描精度：极速 (Fast · 5x采样)",
      "menu.multi.polarSample": "声场：极坐标样本 (Polar Sample)",
      "menu.multi.polarLevel": "声场：极坐标电平 (Polar Level)",
      "menu.multi.lissajous": "声场：李萨如曲线 (Lissajous)",

      "menu.clip.center": "定位播放指针 (Center Playhead)",
      "menu.clip.fit": "缩放以适应完整曲目",
      "menu.clip.copyTime": "复制当前时间码 (Timecode)",

      "menu.fx.freeze": "频谱分析仪：冻结 / 恢复",
      "menu.fx.clearWaterfall": "清空频谱瀑布流历史",

      "menu.zoom.in": "放大时间轴 (Zoom In)",
      "menu.zoom.out": "缩小时间轴 (Zoom Out)",
      "menu.zoom.fit": "缩放至全曲 (Fit Full)",
      "menu.zoom.ff5": "快进 5 秒 (+5s)",
      "menu.zoom.rw5": "快退 5 秒 (-5s)",

      "menu.trans.playpause": "播放 / 暂停 (Play / Pause)",
      "menu.trans.stop": "停止播放 (Stop)",
      "menu.trans.prev": "上一首 (Previous)",
      "menu.trans.next": "下一首 (Next)",
      "menu.trans.start": "跳转到曲目开头 (Rewind to Start)",
      "menu.trans.rw5": "快退 5 秒 (-5s)",
      "menu.trans.ff5": "快进 5 秒 (+5s)",
      "menu.trans.rw15": "快退 15 秒 (-15s)",
      "menu.trans.ff15": "快进 15 秒 (+15s)",
      "menu.trans.repeatAll": "列表循环 (Repeat All)",
      "menu.trans.repeatOne": "单曲循环 (Repeat One)",
      "menu.trans.sequential": "顺序播放 (Sequential)",
      "menu.trans.shuffle": "随机播放 (Shuffle)",
      "menu.trans.cycle": "轮换播放模式 (Cycle Modes)",
      "menu.trans.mute": "静音切换 (Mute)",
      "menu.trans.volUp": "增加音量 (+5%)",
      "menu.trans.volDown": "减小音量 (-5%)",
      "menu.trans.vol80": "标准监听参考音量 (80%)",
      "menu.trans.vol100": "最大输出音量 (100%)",
      "menu.trans.syncQueue": "刷新同步播放队列 (Sync Queue)",
      "menu.trans.scanFull": "立即执行完整波形超采样扫描",
      "menu.trans.stopScan": "终止当前波形扫描",
      "menu.trans.resetHold": "重置峰值保持与 RMS 电平",

      "menu.win.theme": "切换日间 / 黑暗模式",
      "menu.win.waveBlue": "波形色彩：淡雅天蓝",
      "menu.win.wavePink": "波形色彩：柔美粉樱",
      "menu.win.fullscreen": "切换全屏显示",
      "menu.win.resize": "重新自适应所有画布",

      "menu.help.shortcuts": "键盘快捷键指南 (Shortcuts)...",
      "menu.help.prefs": "首选项设置 (Preferences)...",
      "menu.help.about": "关于 Adobe Audition DAW 主题..."
    },

    en: {
      langName: "English",
      "menu.file": "File(F)",
      "menu.edit": "Edit(E)",
      "menu.multitrack": "Multitrack(M)",
      "menu.clip": "Clip(C)",
      "menu.effects": "Effects(S)",
      "menu.zoom": "Zoom(Z)",
      "menu.transport": "Transport(T)",
      "menu.window": "Window(W)",
      "menu.language": "Language(L)",
      "menu.help": "Help(H)",

      "theme.light": "Light",
      "theme.dark": "Dark",
      "theme.toggleTitle": "Toggle Light / Dark Mode (Ctrl+T)",
      "wave.blue": "Azure",
      "wave.pink": "Sakura",
      "wave.toggleTitle": "Toggle Waveform Color (Azure Blue / Sakura Pink) (Ctrl+Alt+W)",
      "lang.toggleTitle": "Switch Interface Language",

      "status.connecting": "Connecting to Host…",
      "status.connected": "Connected",
      "status.stopped": "Stopped",
      "status.playing": "Playing",
      "status.paused": "Paused",

      "panel.tracks": "Track List",
      "panel.history": "History",
      "panel.tracksAria": "Track List & Play History",
      "queue.waiting": "Waiting for playback…",
      "queue.colIcon": "",
      "queue.colTitle": "Track",
      "queue.colDuration": "Duration",
      "queue.note": "Click any track to switch and play. Highlight indicates active track.",
      "queue.emptyHistory": "No playback history",

      "panel.art": "Album Artwork",
      "panel.artAria": "Album Artwork & Vinyl Record",
      "art.hostCover": "Host Artwork",
      "art.embedded": "Embedded Artwork",
      "art.default": "Default Disc",

      "panel.properties": "Audio Properties",
      "panel.propertiesAria": "Audio Properties & Signal Chain",
      "prop.codec": "Codec",
      "prop.sampleRate": "Sample Rate",
      "prop.bitDepth": "Bit Depth",
      "prop.outputDevice": "Output Device",
      "prop.outputEngine": "Output Engine",
      "prop.outputMode": "Output Mode",
      "prop.replayGain": "ReplayGain",
      "prop.meterSource": "Meter Source",
      "prop.sourceRealtime": "Host Realtime Bus",

      "panel.waveform": "Waveform Envelope",
      "panel.waveformAria": "Waveform & Spectrum",
      "wave.hint": "Rendered via host realtime meters (PCM samples unavailable)",
      "wave.scanBtnTitle": "Pre-scan waveform (Silent scan, click again to cancel)",
      "wave.modeRealtime": "Realtime",
      "wave.modePrerender": "Pre-render",
      "wave.modeTitleRealtime": "Waveform Mode: Realtime (draws dynamically with playback)",
      "wave.modeTitlePrerender": "Waveform Mode: Pre-render (silent background scan)",
      "wave.qualityFine": "Fine",
      "wave.qualityStandard": "Standard",
      "wave.qualityFast": "Fast",
      "wave.qualityTitle": "Scan Quality: Fast / Standard / Fine (Current: {quality})",
      "wave.qualityFineDesc": "Fine · 5x High-density oversampling",
      "wave.qualityStandardDesc": "Standard · 5x Balanced sampling",
      "wave.qualityFastDesc": "Fast · 5x Quick overview",
      "wave.zoomInTitle": "Zoom In Horizontally (Ctrl++)",
      "wave.zoomOutTitle": "Zoom Out Horizontally (Ctrl+-)",
      "wave.zoomFitTitle": "Fit to Window (Ctrl+0)",
      "wave.navTitle": "Click or drag to seek",
      "wave.waterfallTitle": "Spectral Waterfall (Realtime Energy)",

      "panel.analyzer": "Mastering Suite",
      "panel.analyzerAria": "Effects - Insight 2 Pro",
      "analyzer.vstTitle": "Effects - Insight 2 Pro",
      "analyzer.stateLive": "LIVE",
      "analyzer.stateFrozen": "FROZEN",
      "analyzer.sourceLabel": "Source",
      "analyzer.sourceVal": "Host Input Level · Spectrum",
      "analyzer.freezeTitle": "Freeze / Resume Display (F)",
      "analyzer.resetTitle": "Reset Peak Statistics (P)",
      "analyzer.radarTitle": "Loudness Radar (Realtime RMS)",
      "analyzer.spectrumTitle": "Spectrum & Frequency Distribution",
      "analyzer.levelsTitle": "Input Levels (Peak / RMS · dBFS)",
      "analyzer.soundfieldTitle": "Sound Field",
      "analyzer.soundfieldGear": "Sound Field Options",
      "analyzer.tabPolarSample": "Polar Sample",
      "analyzer.tabPolarLevel": "Polar Level",
      "analyzer.tabLissajous": "Lissajous",

      "panel.lyrics": "Lyrics",
      "panel.lyricsAria": "Synchronized Lyrics",
      "lyrics.waiting": "Waiting for Lyrics",
      "lyrics.synced": "Synced Lyrics",
      "lyrics.plain": "Plain Lyrics",
      "lyrics.instrumental": "Instrumental track, enjoy the music",
      "lyrics.empty": "No lyrics available",

      "transport.aria": "Transport Controls",
      "transport.total": "Total",
      "transport.remain": "Remain",
      "transport.prev": "Previous Track (Ctrl+←)",
      "transport.rw": "Rewind 5s (←)",
      "transport.playpause": "Play / Pause (Space)",
      "transport.ff": "Forward 5s (→)",
      "transport.next": "Next Track (Ctrl+→)",
      "transport.repeat": "Repeat All",
      "transport.repeatOne": "Repeat One",
      "transport.shuffle": "Shuffle",
      "transport.sequential": "Sequential",
      "transport.mute": "Mute (M)",
      "transport.unmute": "Unmute (M)",
      "transport.volume": "Volume",
      "transport.seek": "Playback Progress",

      "prefs.title": "Preferences",
      "prefs.renderModeTitle": "Waveform Rendering Mode",
      "prefs.renderModeEnable": "Enable Full Track Pre-rendering (Pre-render)",
      "prefs.renderModeHint": "Disabled by default (Realtime mode). Waveforms draw dynamically during playback with optimal performance and low memory footprint.",
      "prefs.qualityTitle": "Pre-scan Sampling Precision (5x Oversampling)",
      "prefs.qualityFineOpt": "Fine (5x High-density oversampling · Default)",
      "prefs.qualityStandardOpt": "Standard (5x Balanced sampling)",
      "prefs.qualityFastOpt": "Fast (5x Quick overview)",
      "prefs.rangeTitle": "Dynamic Range Standard",
      "prefs.rangeDesc": "Master meters and track scales span <strong>0 ~ -60 dBFS</strong>, fully presenting professional mastering dynamic range and noise floor nuances.",
      "prefs.languageTitle": "Interface Language",
      "prefs.languageHint": "Supports 简体中文, English, Français, 日本語, 한국어. Changes take effect immediately and are saved.",

      "shortcuts.title": "Keyboard Shortcuts",
      "shortcuts.playpause": "Play / Pause",
      "shortcuts.rwff": "Rewind 5s / Forward 5s",
      "shortcuts.prevnext": "Previous Track / Next Track",
      "shortcuts.vol": "Volume Up / Down 5%",
      "shortcuts.mute": "Mute / Unmute",
      "shortcuts.zoom": "Zoom In / Out Timeline",
      "shortcuts.zoomFit": "Reset to Full Track Overview",
      "shortcuts.centerPlayhead": "Center Playhead",
      "shortcuts.freeze": "Freeze / Resume Spectrum Analyzer",
      "shortcuts.resetPeak": "Reset Peak Level Metrics",
      "shortcuts.theme": "Toggle Light / Dark Mode",
      "shortcuts.waveColor": "Toggle Waveform Color (Azure / Sakura)",
      "shortcuts.prefs": "Preferences",
      "shortcuts.reload": "Reload DAW Interface",
      "shortcuts.close": "Close Dialog / Back to ECHO",

      "about.title": "About Adobe Audition DAW",
      "about.headerTitle": "Adobe Audition DAW Theme for ECHO",
      "about.version": "Version 1.2.0 (Multilingual Edition)",
      "about.desc": "High-fidelity Adobe Audition professional Digital Audio Workstation (DAW) theme & realtime analyzer suite crafted for the ECHO music player.",
      "about.featTitle": "Features:",
      "about.f1": "• Seamless Light & Dark mastering themes (Ctrl+T)",
      "about.f2": "• Dual waveform colorways: Azure Blue & Sakura Pink (Ctrl+Alt+W)",
      "about.f3": "• Multilingual UI: 简体中文, English, Français, 日本語, 한국어",
      "about.f4": "• Pro-grade multitrack timeline with 0 ~ -60 dBFS true dynamic range",
      "about.f5": "• Realtime 3D FFT spectrogram waterfall with intelligent sleep-lock",
      "about.f6": "• Polar & Lissajous stereo phase correlation vector scopes",
      "about.f7": "• Broadcast-standard LUFS loudness radar and True Peak metering",
      "about.f8": "• Professional top menubar & global hotkeys (File, Edit, Multitrack, etc.)",
      "about.f9": "• Multi-source streaming & local artwork fallback with zero flicker",
      "about.footer": "Inspired by Adobe Audition CC &bull; Built for ECHO",

      "toast.langSwitched": "Interface language switched to: English",
      "toast.pathCopied": "Audio file path copied to clipboard",
      "toast.pathFailed": "Failed to copy path",
      "toast.noPath": "No available audio file path",
      "toast.metaCopied": "Track metadata JSON copied",
      "toast.copied": "Copied",
      "toast.peakReset": "Peak level metrics reset",
      "toast.scanStarted": "Started full waveform oversampling scan",
      "toast.scanRunning": "Waveform scan is currently in progress…",
      "toast.scanCancelled": "Scan cancellation requested",
      "toast.noScan": "No active scanning task",
      "toast.modeRealtime": "Switched to Realtime waveform mode",
      "toast.modePrerender": "Pre-render mode enabled, scanning waveform…",
      "toast.scanQuality": "Scan quality set to: {quality}",
      "toast.soundfield": "Sound field display switched to: {mode}",
      "toast.playheadCentered": "Playhead centered",
      "toast.viewReset": "View reset to full overview",
      "toast.posCopied": "Current playback position: {time}",
      "toast.frozen": "Spectrum and sound field frozen",
      "toast.unfrozen": "Spectrum and sound field resumed live",
      "toast.waterfallCleared": "Spectrogram history cleared",
      "toast.repeatAll": "Repeat All enabled",
      "toast.repeatOne": "Repeat One enabled",
      "toast.sequential": "Sequential playback enabled",
      "toast.repeatOff": "Repeat disabled",
      "toast.muted": "Muted",
      "toast.unmuted": "Unmuted",
      "toast.volStandard": "Standard reference volume set (80%)",
      "toast.volMax": "Maximum output volume set (100%)",
      "toast.syncQueue": "Syncing playback queue from host…",
      "toast.resize": "Reset and adapted all DAW canvases",
      "toast.themeLight": "Switched to Light mastering theme",
      "toast.themeDark": "Switched to Dark professional theme",
      "toast.wavePink": "Waveform palette: Sakura Pink",
      "toast.waveBlue": "Waveform palette: Azure Blue",

      "menu.file.reload": "Reload Interface",
      "menu.file.copyPath": "Copy Audio File Path",
      "menu.file.copyMeta": "Copy Track Metadata (JSON)",
      "menu.file.prefs": "Preferences...",
      "menu.file.back": "Back to ECHO",

      "menu.edit.copyTitle": "Copy Track Title",
      "menu.edit.copyArtist": "Copy Artist Name",
      "menu.edit.resetPeak": "Reset Peak Level Metrics",
      "menu.edit.rescan": "Rescan Current Track Waveform",

      "menu.multi.realtime": "Waveform: Realtime Mode",
      "menu.multi.prerender": "Waveform: Pre-render Mode",
      "menu.multi.fine": "Scan Quality: Fine (5x Oversampling)",
      "menu.multi.standard": "Scan Quality: Standard (5x Balanced)",
      "menu.multi.fast": "Scan Quality: Fast (5x Quick)",
      "menu.multi.polarSample": "Scope: Polar Sample",
      "menu.multi.polarLevel": "Scope: Polar Level",
      "menu.multi.lissajous": "Scope: Lissajous Curve",

      "menu.clip.center": "Center Playhead",
      "menu.clip.fit": "Fit to Entire Track",
      "menu.clip.copyTime": "Copy Current Timecode",

      "menu.fx.freeze": "Analyzer: Freeze / Resume",
      "menu.fx.clearWaterfall": "Clear Spectrogram History",

      "menu.zoom.in": "Zoom In Timeline",
      "menu.zoom.out": "Zoom Out Timeline",
      "menu.zoom.fit": "Fit Full Track",
      "menu.zoom.ff5": "Forward 5 Seconds (+5s)",
      "menu.zoom.rw5": "Rewind 5 Seconds (-5s)",

      "menu.trans.playpause": "Play / Pause",
      "menu.trans.stop": "Stop Playback",
      "menu.trans.prev": "Previous Track",
      "menu.trans.next": "Next Track",
      "menu.trans.start": "Rewind to Start",
      "menu.trans.rw5": "Rewind 5 Seconds (-5s)",
      "menu.trans.ff5": "Forward 5 Seconds (+5s)",
      "menu.trans.rw15": "Rewind 15 Seconds (-15s)",
      "menu.trans.ff15": "Forward 15 Seconds (+15s)",
      "menu.trans.repeatAll": "Repeat All",
      "menu.trans.repeatOne": "Repeat One",
      "menu.trans.sequential": "Sequential Playback",
      "menu.trans.shuffle": "Shuffle Playback",
      "menu.trans.cycle": "Cycle Repeat Modes",
      "menu.trans.mute": "Toggle Mute",
      "menu.trans.volUp": "Increase Volume (+5%)",
      "menu.trans.volDown": "Decrease Volume (-5%)",
      "menu.trans.vol80": "Reference Monitor Volume (80%)",
      "menu.trans.vol100": "Maximum Output Volume (100%)",
      "menu.trans.syncQueue": "Sync Playback Queue",
      "menu.trans.scanFull": "Run Full Waveform Oversampling Scan",
      "menu.trans.stopScan": "Abort Current Waveform Scan",
      "menu.trans.resetHold": "Reset Peak Hold & RMS Meters",

      "menu.win.theme": "Toggle Light / Dark Mode",
      "menu.win.waveBlue": "Waveform Color: Azure Blue",
      "menu.win.wavePink": "Waveform Color: Sakura Pink",
      "menu.win.fullscreen": "Toggle Fullscreen",
      "menu.win.resize": "Recalculate & Fit All Canvases",

      "menu.help.shortcuts": "Keyboard Shortcuts Guide...",
      "menu.help.prefs": "Preferences...",
      "menu.help.about": "About Adobe Audition DAW..."
    },

    fr: {
      langName: "Français",
      "menu.file": "Fichier(F)",
      "menu.edit": "Édition(E)",
      "menu.multitrack": "Multipiste(M)",
      "menu.clip": "Élément(C)",
      "menu.effects": "Effets(S)",
      "menu.zoom": "Zoom(Z)",
      "menu.transport": "Lecture(T)",
      "menu.window": "Fenêtre(W)",
      "menu.language": "Langue(L)",
      "menu.help": "Aide(H)",

      "theme.light": "Clair",
      "theme.dark": "Sombre",
      "theme.toggleTitle": "Basculer Mode Clair / Sombre (Ctrl+T)",
      "wave.blue": "Azur",
      "wave.pink": "Sakura",
      "wave.toggleTitle": "Basculer couleur de forme d'onde (Bleu Azur / Rose Sakura) (Ctrl+Alt+W)",
      "lang.toggleTitle": "Changer la langue de l'interface",

      "status.connecting": "Connexion à l'hôte…",
      "status.connected": "Connecté",
      "status.stopped": "Arrêté",
      "status.playing": "Lecture en cours",
      "status.paused": "En pause",

      "panel.tracks": "Pistes",
      "panel.history": "Historique",
      "panel.tracksAria": "Liste des pistes et historique",
      "queue.waiting": "En attente de lecture…",
      "queue.colIcon": "",
      "queue.colTitle": "Piste",
      "queue.colDuration": "Durée",
      "queue.note": "Cliquez sur une piste pour la lire. La surbrillance indique la piste active.",
      "queue.emptyHistory": "Aucun historique de lecture",

      "panel.art": "Pochette d'album",
      "panel.artAria": "Pochette d'album et disque vinyle",
      "art.hostCover": "Pochette de l'hôte",
      "art.embedded": "Pochette intégrée",
      "art.default": "Disque par défaut",

      "panel.properties": "Propriétés audio",
      "panel.propertiesAria": "Propriétés audio et chaîne de signal",
      "prop.codec": "Format d'encodage",
      "prop.sampleRate": "Échantillonnage",
      "prop.bitDepth": "Résolution binaire",
      "prop.outputDevice": "Périphérique",
      "prop.outputEngine": "Moteur audio",
      "prop.outputMode": "Mode de sortie",
      "prop.replayGain": "Gain de relecture",
      "prop.meterSource": "Source des niveaux",
      "prop.sourceRealtime": "Bus hôte en temps réel",

      "panel.waveform": "Enveloppe de forme d'onde",
      "panel.waveformAria": "Forme d'onde et spectre",
      "wave.hint": "Tracé via les niveaux en temps réel de l'hôte (échantillons PCM non fournis)",
      "wave.scanBtnTitle": "Pré-balayage de forme d'onde (muet, cliquer pour annuler)",
      "wave.modeRealtime": "Temps réel",
      "wave.modePrerender": "Pré-rendu",
      "wave.modeTitleRealtime": "Mode de tracé : Temps réel (tracé dynamique pendant la lecture)",
      "wave.modeTitlePrerender": "Mode de tracé : Pré-rendu (balayage d'arrière-plan)",
      "wave.qualityFine": "Précis",
      "wave.qualityStandard": "Standard",
      "wave.qualityFast": "Rapide",
      "wave.qualityTitle": "Précision d'échantillonnage : Rapide / Standard / Précis (Actuel : {quality})",
      "wave.qualityFineDesc": "Précis · Suréchantillonnage 5x haute densité",
      "wave.qualityStandardDesc": "Standard · Échantillonnage 5x équilibré",
      "wave.qualityFastDesc": "Rapide · Aperçu 5x rapide",
      "wave.zoomInTitle": "Zoom avant horizontal (Ctrl++)",
      "wave.zoomOutTitle": "Zoom arrière horizontal (Ctrl+-)",
      "wave.zoomFitTitle": "Adapter à la fenêtre (Ctrl+0)",
      "wave.navTitle": "Cliquer ou glisser pour naviguer",
      "wave.waterfallTitle": "Cascade spectrale (Énergie en temps réel)",

      "panel.analyzer": "Suite de Mastering",
      "panel.analyzerAria": "Effets - Insight 2 Pro",
      "analyzer.vstTitle": "Effets - Insight 2 Pro",
      "analyzer.stateLive": "EN DIRECT",
      "analyzer.stateFrozen": "FIGÉ",
      "analyzer.sourceLabel": "Source",
      "analyzer.sourceVal": "Niveaux d'entrée et spectre hôte",
      "analyzer.freezeTitle": "Figer / Reprendre l'affichage (F)",
      "analyzer.resetTitle": "Réinitialiser les crêtes (P)",
      "analyzer.radarTitle": "Radar de sonie (RMS temps réel)",
      "analyzer.spectrumTitle": "Spectre et distribution fréquentielle",
      "analyzer.levelsTitle": "Niveaux d'entrée (Crête / RMS · dBFS)",
      "analyzer.soundfieldTitle": "Sound Field",
      "analyzer.soundfieldGear": "Options du champ sonore",
      "analyzer.tabPolarSample": "Polar Sample",
      "analyzer.tabPolarLevel": "Polar Level",
      "analyzer.tabLissajous": "Lissajous",

      "panel.lyrics": "Paroles",
      "panel.lyricsAria": "Paroles synchronisées",
      "lyrics.waiting": "En attente de paroles",
      "lyrics.synced": "Paroles synchronisées",
      "lyrics.plain": "Paroles brutes",
      "lyrics.instrumental": "Morceau instrumental, bonne écoute",
      "lyrics.empty": "Aucune parole disponible",

      "transport.aria": "Commandes de transport",
      "transport.total": "Durée totale",
      "transport.remain": "Restant",
      "transport.prev": "Piste précédente (Ctrl+←)",
      "transport.rw": "Recul 5s (←)",
      "transport.playpause": "Lecture / Pause (Espace)",
      "transport.ff": "Avance 5s (→)",
      "transport.next": "Piste suivante (Ctrl+→)",
      "transport.repeat": "Répéter tout",
      "transport.repeatOne": "Répéter une piste",
      "transport.shuffle": "Lecture aléatoire",
      "transport.sequential": "Lecture continue",
      "transport.mute": "Silence (M)",
      "transport.unmute": "Activer le son (M)",
      "transport.volume": "Volume",
      "transport.seek": "Position de lecture",

      "prefs.title": "Préférences",
      "prefs.renderModeTitle": "Mode de rendu de forme d'onde",
      "prefs.renderModeEnable": "Activer le pré-rendu complet du morceau (Pre-render)",
      "prefs.renderModeHint": "Désactivé par défaut (mode temps réel). Les formes d'onde sont tracées dynamiquement lors de la lecture pour des performances optimales.",
      "prefs.qualityTitle": "Précision d'échantillonnage (Suréchantillonnage 5x)",
      "prefs.qualityFineOpt": "Précis (Fine - Suréchantillonnage 5x haute densité · Défaut)",
      "prefs.qualityStandardOpt": "Standard (Standard - Échantillonnage 5x équilibré)",
      "prefs.qualityFastOpt": "Rapide (Fast - Aperçu 5x rapide)",
      "prefs.rangeTitle": "Échelle de plage dynamique",
      "prefs.rangeDesc": "Les indicateurs master et les échelles couvrent de <strong>0 à -60 dBFS</strong>, restituant toute la dynamique de mastering et les bruits de fond.",
      "prefs.languageTitle": "Langue de l'interface",
      "prefs.languageHint": "Prend en charge 简体中文, English, Français, 日本語, 한국어. Les modifications sont appliquées et enregistrées immédiatement.",

      "shortcuts.title": "Raccourcis clavier",
      "shortcuts.playpause": "Lecture / Pause",
      "shortcuts.rwff": "Recul 5s / Avance 5s",
      "shortcuts.prevnext": "Piste précédente / suivante",
      "shortcuts.vol": "Volume +/- 5%",
      "shortcuts.mute": "Silence / Son",
      "shortcuts.zoom": "Zoom avant / arrière sur l'axe temporel",
      "shortcuts.zoomFit": "Vue d'ensemble du morceau",
      "shortcuts.centerPlayhead": "Centrer la tête de lecture",
      "shortcuts.freeze": "Figer / Reprendre l'analyseur de spectre",
      "shortcuts.resetPeak": "Réinitialiser les crêtes",
      "shortcuts.theme": "Basculer Mode Clair / Sombre",
      "shortcuts.waveColor": "Couleur de forme d'onde (Azur / Sakura)",
      "shortcuts.prefs": "Préférences",
      "shortcuts.reload": "Recharger l'interface DAW",
      "shortcuts.close": "Fermer / Retour à ECHO",

      "about.title": "À propos d'Adobe Audition DAW",
      "about.headerTitle": "Thème Adobe Audition DAW pour ECHO",
      "about.version": "Version 1.2.0 (Édition Multilingue)",
      "about.desc": "Thème immersif et suite d'analyseurs en temps réel haute fidélité inspirés d'Adobe Audition pour le lecteur audio ECHO.",
      "about.featTitle": "Fonctionnalités :",
      "about.f1": "• Thèmes de mastering Clair et Sombre sans coupure (Ctrl+T)",
      "about.f2": "• Deux palettes de formes d'onde : Bleu Azur et Rose Sakura (Ctrl+Alt+W)",
      "about.f3": "• Support multilingue complet : Chinois, Anglais, Français, Japonais, Coréen",
      "about.f4": "• Échelle temporelle professionnelle avec dynamique réelle de 0 à -60 dBFS",
      "about.f5": "• Cascade spectrale FFT 3D temps réel avec mise en veille intelligente",
      "about.f6": "• Indicateurs de phase polaires et courbes de Lissajous",
      "about.f7": "• Radar de sonie LUFS et indicateurs True Peak de niveau professionnel",
      "about.f8": "• Barre de menus DAW supérieure et raccourcis globaux complets",
      "about.f9": "• Pochette de disque intelligente sans clignotement",
      "about.footer": "Inspiré d'Adobe Audition CC &bull; Conçu pour ECHO",

      "toast.langSwitched": "Langue de l'interface : Français",
      "toast.pathCopied": "Chemin du fichier audio copié dans le presse-papiers",
      "toast.pathFailed": "Échec de la copie",
      "toast.noPath": "Aucun chemin de fichier audio disponible",
      "toast.metaCopied": "Métadonnées JSON copiées",
      "toast.copied": "Copié",
      "toast.peakReset": "Métriques de crête réinitialisées",
      "toast.scanStarted": "Balayage de suréchantillonnage démarré",
      "toast.scanRunning": "Balayage en cours…",
      "toast.scanCancelled": "Annulation du balayage demandée",
      "toast.noScan": "Aucun balayage en cours",
      "toast.modeRealtime": "Passage en mode de tracé en temps réel",
      "toast.modePrerender": "Mode pré-rendu activé, balayage en cours…",
      "toast.scanQuality": "Précision d'échantillonnage réglée sur : {quality}",
      "toast.soundfield": "Affichage du champ sonore changé : {mode}",
      "toast.playheadCentered": "Tête de lecture centrée",
      "toast.viewReset": "Vue d'ensemble réinitialisée",
      "toast.posCopied": "Position de lecture actuelle : {time}",
      "toast.frozen": "Spectre et champ sonore figés",
      "toast.unfrozen": "Spectre et champ sonore réactivés en direct",
      "toast.waterfallCleared": "Historique de la cascade spectrale effacé",
      "toast.repeatAll": "Répétition de toutes les pistes activée",
      "toast.repeatOne": "Répétition d'une piste activée",
      "toast.sequential": "Lecture séquentielle activée",
      "toast.repeatOff": "Répétition désactivée",
      "toast.muted": "Son coupé",
      "toast.unmuted": "Son rétabli",
      "toast.volStandard": "Volume de référence monitoring réglé (80%)",
      "toast.volMax": "Volume de sortie maximal réglé (100%)",
      "toast.syncQueue": "Synchronisation de la file d'attente…",
      "toast.resize": "Canevas DAW réadaptés",
      "toast.themeLight": "Thème Clair activé",
      "toast.themeDark": "Thème Sombre professionnel activé",
      "toast.wavePink": "Palette de forme d'onde : Rose Sakura",
      "toast.waveBlue": "Palette de forme d'onde : Bleu Azur",

      "menu.file.reload": "Recharger l'interface",
      "menu.file.copyPath": "Copier le chemin audio",
      "menu.file.copyMeta": "Copier les métadonnées (JSON)",
      "menu.file.prefs": "Préférences...",
      "menu.file.back": "Retour à ECHO",

      "menu.edit.copyTitle": "Copier le titre du morceau",
      "menu.edit.copyArtist": "Copier l'artiste",
      "menu.edit.resetPeak": "Réinitialiser les crêtes",
      "menu.edit.rescan": "Rebalayer la forme d'onde",

      "menu.multi.realtime": "Forme d'onde : Mode temps réel",
      "menu.multi.prerender": "Forme d'onde : Mode pré-rendu",
      "menu.multi.fine": "Précision : Précis (Suréchantillonnage 5x)",
      "menu.multi.standard": "Précision : Standard (5x équilibré)",
      "menu.multi.fast": "Précision : Rapide (5x rapide)",
      "menu.multi.polarSample": "Champ sonore : Polar Sample",
      "menu.multi.polarLevel": "Champ sonore : Polar Level",
      "menu.multi.lissajous": "Champ sonore : Courbe de Lissajous",

      "menu.clip.center": "Centrer la tête de lecture",
      "menu.clip.fit": "Adapter au morceau complet",
      "menu.clip.copyTime": "Copier le timecode actuel",

      "menu.fx.freeze": "Analyseur : Figer / Reprendre",
      "menu.fx.clearWaterfall": "Effacer la cascade spectrale",

      "menu.zoom.in": "Zoom avant sur l'axe temporel",
      "menu.zoom.out": "Zoom arrière sur l'axe temporel",
      "menu.zoom.fit": "Adapter à tout le morceau",
      "menu.zoom.ff5": "Avance de 5 secondes (+5s)",
      "menu.zoom.rw5": "Recul de 5 secondes (-5s)",

      "menu.trans.playpause": "Lecture / Pause",
      "menu.trans.stop": "Arrêter la lecture",
      "menu.trans.prev": "Piste précédente",
      "menu.trans.next": "Piste suivante",
      "menu.trans.start": "Retour au début du morceau",
      "menu.trans.rw5": "Recul 5 secondes (-5s)",
      "menu.trans.ff5": "Avance 5 secondes (+5s)",
      "menu.trans.rw15": "Recul 15 secondes (-15s)",
      "menu.trans.ff15": "Avance 15 secondes (+15s)",
      "menu.trans.repeatAll": "Répéter toutes les pistes",
      "menu.trans.repeatOne": "Répéter une piste",
      "menu.trans.sequential": "Lecture séquentielle",
      "menu.trans.shuffle": "Lecture aléatoire",
      "menu.trans.cycle": "Changer le mode de répétition",
      "menu.trans.mute": "Couper / Rétablir le son",
      "menu.trans.volUp": "Augmenter le volume (+5%)",
      "menu.trans.volDown": "Diminuer le volume (-5%)",
      "menu.trans.vol80": "Volume monitoring de référence (80%)",
      "menu.trans.vol100": "Volume de sortie maximal (100%)",
      "menu.trans.syncQueue": "Actualiser la file de lecture",
      "menu.trans.scanFull": "Exécuter le suréchantillonnage complet",
      "menu.trans.stopScan": "Interrompre le balayage en cours",
      "menu.trans.resetHold": "Réinitialiser les crêtes et le niveau RMS",

      "menu.win.theme": "Basculer Mode Clair / Sombre",
      "menu.win.waveBlue": "Couleur de forme d'onde : Bleu Azur",
      "menu.win.wavePink": "Couleur de forme d'onde : Rose Sakura",
      "menu.win.fullscreen": "Plein écran",
      "menu.win.resize": "Réadapter tous les canevas",

      "menu.help.shortcuts": "Guide des raccourcis clavier...",
      "menu.help.prefs": "Préférences...",
      "menu.help.about": "À propos d'Adobe Audition DAW..."
    },

    ja: {
      langName: "日本語",
      "menu.file": "ファイル(F)",
      "menu.edit": "編集(E)",
      "menu.multitrack": "マルチトラック(M)",
      "menu.clip": "クリップ(C)",
      "menu.effects": "エフェクト(S)",
      "menu.zoom": "ズーム(Z)",
      "menu.transport": "トランスポート(T)",
      "menu.window": "ウィンドウ(W)",
      "menu.language": "言語(L)",
      "menu.help": "ヘルプ(H)",

      "theme.light": "ライト",
      "theme.dark": "ダーク",
      "theme.toggleTitle": "ライト / ダークモード切替 (Ctrl+T)",
      "wave.blue": "藍色",
      "wave.pink": "桜色",
      "wave.toggleTitle": "波形カラー切替 (藍色 / 桜色) (Ctrl+Alt+W)",
      "lang.toggleTitle": "UI表示言語を切替",

      "status.connecting": "ホスト接続待機中…",
      "status.connected": "接続完了",
      "status.stopped": "停止中",
      "status.playing": "再生中",
      "status.paused": "一時停止",

      "panel.tracks": "トラック一覧",
      "panel.history": "再生履歴",
      "panel.tracksAria": "トラック一覧と再生履歴",
      "queue.waiting": "再生待機中…",
      "queue.colIcon": "",
      "queue.colTitle": "トラック",
      "queue.colDuration": "時間",
      "queue.note": "トラックをクリックして即座に再生。ハイライトは再生中の楽曲を示します。",
      "queue.emptyHistory": "再生履歴はありません",

      "panel.art": "アルバムアート",
      "panel.artAria": "アートワークとアナログレコード",
      "art.hostCover": "ホストカバー",
      "art.embedded": "埋め込み画像",
      "art.default": "標準レコード",

      "panel.properties": "オーディオプロパティ",
      "panel.propertiesAria": "オーディオ属性と信号経路",
      "prop.codec": "コーデック",
      "prop.sampleRate": "サンプリングレート",
      "prop.bitDepth": "ビット深度",
      "prop.outputDevice": "出力デバイス",
      "prop.outputEngine": "出力エンジン",
      "prop.outputMode": "出力モード",
      "prop.replayGain": "リプレイゲイン",
      "prop.meterSource": "レベル入力元",
      "prop.sourceRealtime": "ホストリアルタイムバス",

      "panel.waveform": "波形エンベロープ",
      "panel.waveformAria": "波形とスペクトログラム",
      "wave.hint": "ホストのリアルタイムレベルから描画（PCMサンプル非提供）",
      "wave.scanBtnTitle": "全曲波形プリスキャン（無音スキャン、再クリックで中止）",
      "wave.modeRealtime": "リアルタイム",
      "wave.modePrerender": "プリレンダリング",
      "wave.modeTitleRealtime": "波形モード：リアルタイム（再生に応じて動的描画）",
      "wave.modeTitlePrerender": "波形モード：プリレンダリング（全曲高速無音スキャン）",
      "wave.qualityFine": "高精度",
      "wave.qualityStandard": "標準",
      "wave.qualityFast": "高速",
      "wave.qualityTitle": "スキャン精度：高速 / 標準 / 高精度（現在：{quality}）",
      "wave.qualityFineDesc": "高精度 · 5x高密度オーバーサンプリング",
      "wave.qualityStandardDesc": "標準 · 5xバランスサンプリング",
      "wave.qualityFastDesc": "高速 · 5x高速プレビュー",
      "wave.zoomInTitle": "水平拡大 (Ctrl++)",
      "wave.zoomOutTitle": "水平縮小 (Ctrl+-)",
      "wave.zoomFitTitle": "ウィンドウに合わせる (Ctrl+0)",
      "wave.navTitle": "クリックまたはドラッグでシーク",
      "wave.waterfallTitle": "スペクトログラム滝（リアルタイム周波数エネルギー）",

      "panel.analyzer": "マスタリングスイート",
      "panel.analyzerAria": "エフェクト - Insight 2 Pro",
      "analyzer.vstTitle": "エフェクト - Insight 2 Pro",
      "analyzer.stateLive": "リアルタイム",
      "analyzer.stateFrozen": "フリーズ中",
      "analyzer.sourceLabel": "データソース",
      "analyzer.sourceVal": "ホスト入力レベル・スペクトル",
      "analyzer.freezeTitle": "表示フリーズ / 解除 (F)",
      "analyzer.resetTitle": "ピーク統計リセット (P)",
      "analyzer.radarTitle": "ラウドネスレーダー（リアルタイム RMS）",
      "analyzer.spectrumTitle": "スペクトル・周波数分布",
      "analyzer.levelsTitle": "入力レベル（Peak / RMS · dBFS）",
      "analyzer.soundfieldTitle": "Sound Field",
      "analyzer.soundfieldGear": "音場表示オプション",
      "analyzer.tabPolarSample": "Polar Sample",
      "analyzer.tabPolarLevel": "Polar Level",
      "analyzer.tabLissajous": "Lissajous",

      "panel.lyrics": "歌詞",
      "panel.lyricsAria": "同期歌詞ディスプレイ",
      "lyrics.waiting": "歌詞待機中",
      "lyrics.synced": "同期歌詞",
      "lyrics.plain": "プレーン歌詞",
      "lyrics.instrumental": "インストゥルメンタル曲をお楽しみください",
      "lyrics.empty": "歌詞がありません",

      "transport.aria": "トランスポート制御",
      "transport.total": "総時間",
      "transport.remain": "残り",
      "transport.prev": "前の曲 (Ctrl+←)",
      "transport.rw": "5秒早戻し (←)",
      "transport.playpause": "再生 / 一時停止 (Space)",
      "transport.ff": "5秒早送り (→)",
      "transport.next": "次の曲 (Ctrl+→)",
      "transport.repeat": "全曲リピート",
      "transport.repeatOne": "1曲リピート",
      "transport.shuffle": "シャッフル再生",
      "transport.sequential": "順次再生",
      "transport.mute": "ミュート (M)",
      "transport.unmute": "ミュート解除 (M)",
      "transport.volume": "音量",
      "transport.seek": "再生シークバー",

      "prefs.title": "環境設定 (Preferences)",
      "prefs.renderModeTitle": "波形レンダリングモード",
      "prefs.renderModeEnable": "全曲波形プリレンダリングを有効にする (Pre-render)",
      "prefs.renderModeHint": "デフォルトは無効（リアルタイムモード）。楽曲の再生に合わせて低負荷・省メモリで動的に描画します。",
      "prefs.qualityTitle": "プリスキャンサンプリング精度 (5x オーバーサンプリング)",
      "prefs.qualityFineOpt": "高精度 (Fine - 5x 高密度オーバーサンプリング · デフォルト)",
      "prefs.qualityStandardOpt": "標準 (Standard - 5x バランスサンプリング)",
      "prefs.qualityFastOpt": "高速 (Fast - 5x 高速プレビュー)",
      "prefs.rangeTitle": "ダイナミックレンジ標準",
      "prefs.rangeDesc": "マスターメーターおよびトラック目盛は <strong>0 ～ -60 dBFS</strong> をカバーし、プロフェッショナルなマスタリングダイナミクスと微小信号を忠実に可視化します。",
      "prefs.languageTitle": "表示言語 (Language)",
      "prefs.languageHint": "簡体字中国語、英語、フランス語、日本語、韓国語に対応。変更は即座に反映され保存されます。",

      "shortcuts.title": "キーボードショートカット (Shortcuts)",
      "shortcuts.playpause": "再生 / 一時停止",
      "shortcuts.rwff": "5秒早戻し / 5秒早送り",
      "shortcuts.prevnext": "前の曲 / 次の曲",
      "shortcuts.vol": "音量アップ / ダウン 5%",
      "shortcuts.mute": "ミュート / ミュート解除",
      "shortcuts.zoom": "時間軸の拡大 / 縮小",
      "shortcuts.zoomFit": "全曲表示にリセット",
      "shortcuts.centerPlayhead": "再生ポインタを中央配置",
      "shortcuts.freeze": "スペクトラムアナライザーのフリーズ / 再開",
      "shortcuts.resetPeak": "すべてのピーク指標をリセット",
      "shortcuts.theme": "ライト / ダークモード切替",
      "shortcuts.waveColor": "波形カラー切替 (藍色 / 桜色)",
      "shortcuts.prefs": "環境設定",
      "shortcuts.reload": "DAW UIを再読み込み",
      "shortcuts.close": "ダイアログを閉じる / ECHOに戻る",

      "about.title": "Adobe Audition DAW について",
      "about.headerTitle": "Adobe Audition DAW Theme for ECHO",
      "about.version": "バージョン 1.2.0 (多言語エディション)",
      "about.desc": "ECHO 音楽プレーヤーのために設計された、高精度 Adobe Audition スタイルのプロフェッショナル DAW イマーシブテーマ＆リアルタイムアナライザースイート。",
      "about.featTitle": "主な機能：",
      "about.f1": "• 洗練されたダーク / ライト マスタリングテーマのシームレス切替 (Ctrl+T)",
      "about.f2": "• 淡雅な藍色と柔らかな桜色の2つの波形カラーパレット (Ctrl+Alt+W)",
      "about.f3": "• 完全多言語ローカライズ対応：日本語、中国語、英語、フランス語、韓国語",
      "about.f4": "• 0 ～ -60 dBFS の本格的ダイナミックレンジを持つマルチトラック波形",
      "about.f5": "• リアルタイム 3D FFT スペクトログラム滝（停止時スマートスリープロック）",
      "about.f6": "• 極座標およびリサジュー (Lissajous) ステレオ位相アナライザー",
      "about.f7": "• 放送規格準拠 LUFS ラウドネスレーダーおよび True Peak メーター",
      "about.f8": "• プロ仕様のトップメニューバー＆包括的なショートカットキー",
      "about.f9": "• マルチソースアートワークとローカルカバーのスマートフォールバック",
      "about.footer": "Inspired by Adobe Audition CC &bull; Built for ECHO",

      "toast.langSwitched": "表示言語を切り替えました：日本語",
      "toast.pathCopied": "オーディオパスをクリップボードにコピーしました",
      "toast.pathFailed": "コピーに失敗しました",
      "toast.noPath": "利用可能なオーディオファイルパスがありません",
      "toast.metaCopied": "楽曲メタデータ (JSON) をコピーしました",
      "toast.copied": "コピー完了",
      "toast.peakReset": "すべてのピーク指標をリセットしました",
      "toast.scanStarted": "全曲波形オーバーサンプリングスキャンを開始しました",
      "toast.scanRunning": "波形スキャンを実行中です…",
      "toast.scanCancelled": "スキャンのキャンセルを要求しました",
      "toast.noScan": "実行中のスキャンはありません",
      "toast.modeRealtime": "リアルタイム波形モードに切り替えました",
      "toast.modePrerender": "プリレンダリングモードを有効化し、スキャンを開始しました",
      "toast.scanQuality": "スキャン精度を設定しました：{quality}",
      "toast.soundfield": "音場表示を切り替えました：{mode}",
      "toast.playheadCentered": "再生ポインタを中央に合わせました",
      "toast.viewReset": "全曲概観表示にリセットしました",
      "toast.posCopied": "現在の再生位置：{time}",
      "toast.frozen": "スペクトルと音場をフリーズしました",
      "toast.unfrozen": "スペクトルと音場のリアルタイム表示を再開しました",
      "toast.waterfallCleared": "スペクトログラム履歴を消去しました",
      "toast.repeatAll": "全曲リピートを有効にしました",
      "toast.repeatOne": "1曲リピートを有効にしました",
      "toast.sequential": "順次再生に切り替えました",
      "toast.repeatOff": "リピートを解除しました",
      "toast.muted": "ミュートしました",
      "toast.unmuted": "ミュートを解除しました",
      "toast.volStandard": "標準モニタリング音量 (80%) に設定しました",
      "toast.volMax": "最大出力音量 (100%) に設定しました",
      "toast.syncQueue": "ホストから再生キューを同期中…",
      "toast.resize": "すべての DAW キャンバスを最適化しました",
      "toast.themeLight": "ライトマスタリングテーマに切り替えました",
      "toast.themeDark": "ダークプロフェッショナルテーマに切り替えました",
      "toast.wavePink": "波形カラー：柔美な桜色",
      "toast.waveBlue": "波形カラー：淡雅な藍色",

      "menu.file.reload": "UIを再読み込み (Reload)",
      "menu.file.copyPath": "オーディオパスをコピー",
      "menu.file.copyMeta": "楽曲メタデータをコピー (JSON)",
      "menu.file.prefs": "環境設定 (Preferences)...",
      "menu.file.back": "ECHO メイン画面に戻る",

      "menu.edit.copyTitle": "楽曲タイトルをコピー",
      "menu.edit.copyArtist": "アーティスト名をコピー",
      "menu.edit.resetPeak": "ピーク指標をリセット (Reset Peak)",
      "menu.edit.rescan": "現在の曲の波形を再スキャン",

      "menu.multi.realtime": "波形描画：リアルタイムモード",
      "menu.multi.prerender": "波形描画：プリレンダリングモード",
      "menu.multi.fine": "スキャン精度：高精度 (5x オーバーサンプリング)",
      "menu.multi.standard": "スキャン精度：標準 (5x バランス)",
      "menu.multi.fast": "スキャン精度：高速 (5x クイック)",
      "menu.multi.polarSample": "音場：極座標サンプル (Polar Sample)",
      "menu.multi.polarLevel": "音場：極座標レベル (Polar Level)",
      "menu.multi.lissajous": "音場：リサジュー曲線 (Lissajous)",

      "menu.clip.center": "再生ポインタを中央配置",
      "menu.clip.fit": "曲全体に合わせて拡大縮小",
      "menu.clip.copyTime": "現在のタイムコードをコピー",

      "menu.fx.freeze": "スペクトラムアナライザー：フリーズ / 再開",
      "menu.fx.clearWaterfall": "スペクトログラム滝の履歴を消去",

      "menu.zoom.in": "時間軸を拡大 (Zoom In)",
      "menu.zoom.out": "時間軸を縮小 (Zoom Out)",
      "menu.zoom.fit": "全曲に合わせる (Fit Full)",
      "menu.zoom.ff5": "5秒早送り (+5s)",
      "menu.zoom.rw5": "5秒早戻し (-5s)",

      "menu.trans.playpause": "再生 / 一時停止",
      "menu.trans.stop": "再生停止",
      "menu.trans.prev": "前の曲",
      "menu.trans.next": "次の曲",
      "menu.trans.start": "曲の先頭にジャンプ",
      "menu.trans.rw5": "5秒早戻し (-5s)",
      "menu.trans.ff5": "5秒早送り (+5s)",
      "menu.trans.rw15": "15秒早戻し (-15s)",
      "menu.trans.ff15": "15秒早送り (+15s)",
      "menu.trans.repeatAll": "全曲リピート",
      "menu.trans.repeatOne": "1曲リピート",
      "menu.trans.sequential": "順次再生",
      "menu.trans.shuffle": "シャッフル再生",
      "menu.trans.cycle": "リピートモード切り替え",
      "menu.trans.mute": "ミュート切替",
      "menu.trans.volUp": "音量アップ (+5%)",
      "menu.trans.volDown": "音量ダウン (-5%)",
      "menu.trans.vol80": "標準モニター音量 (80%)",
      "menu.trans.vol100": "最大出力音量 (100%)",
      "menu.trans.syncQueue": "再生キューを同期",
      "menu.trans.scanFull": "全曲オーバーサンプリングスキャンを実行",
      "menu.trans.stopScan": "現在のスキャンを中止",
      "menu.trans.resetHold": "ピークホールドとRMSレベルをリセット",

      "menu.win.theme": "ライト / ダークモード切替",
      "menu.win.waveBlue": "波形カラー：淡雅な藍色",
      "menu.win.wavePink": "波形カラー：柔美な桜色",
      "menu.win.fullscreen": "全画面表示の切替",
      "menu.win.resize": "すべてのキャンバスを再適合",

      "menu.help.shortcuts": "キーボードショートカット一覧...",
      "menu.help.prefs": "環境設定 (Preferences)...",
      "menu.help.about": "Adobe Audition DAW について..."
    },

    ko: {
      langName: "한국어",
      "menu.file": "파일(F)",
      "menu.edit": "편집(E)",
      "menu.multitrack": "멀티트랙(M)",
      "menu.clip": "클립(C)",
      "menu.effects": "효과(S)",
      "menu.zoom": "줌(Z)",
      "menu.transport": "트랜스포트(T)",
      "menu.window": "창(W)",
      "menu.language": "언어(L)",
      "menu.help": "도움말(H)",

      "theme.light": "라이트",
      "theme.dark": "다크",
      "theme.toggleTitle": "라이트 / 다크 모드 전환 (Ctrl+T)",
      "wave.blue": "하늘",
      "wave.pink": "벚꽃",
      "wave.toggleTitle": "파형 색상 전환 (하늘색 / 벚꽃색) (Ctrl+Alt+W)",
      "lang.toggleTitle": "인터페이스 언어 변경",

      "status.connecting": "호스트 연결 대기 중…",
      "status.connected": "연결됨",
      "status.stopped": "정지됨",
      "status.playing": "재생 중",
      "status.paused": "일시정지",

      "panel.tracks": "트랙 목록",
      "panel.history": "재생 기록",
      "panel.tracksAria": "트랙 목록 및 재생 기록",
      "queue.waiting": "재생 대기 중…",
      "queue.colIcon": "",
      "queue.colTitle": "트랙",
      "queue.colDuration": "시간",
      "queue.note": "트랙을 클릭하면 전환하여 재생됩니다. 강조 표시된 트랙이 현재 재생 중입니다.",
      "queue.emptyHistory": "재생 기록이 없습니다",

      "panel.art": "앨범 아트",
      "panel.artAria": "앨범 아트 및 바이닐 레코드",
      "art.hostCover": "호스트 커버",
      "art.embedded": "내장 커버",
      "art.default": "기본 레코드",

      "panel.properties": "오디오 속성",
      "panel.propertiesAria": "오디오 속성 및 신호 경로",
      "prop.codec": "코덱 형식",
      "prop.sampleRate": "샘플레이트",
      "prop.bitDepth": "비트 깊이",
      "prop.outputDevice": "출력 장치",
      "prop.outputEngine": "출력 백엔드",
      "prop.outputMode": "출력 모드",
      "prop.replayGain": "리플레이게인",
      "prop.meterSource": "레벨 입력 소스",
      "prop.sourceRealtime": "호스트 실시간 버스",

      "panel.waveform": "파형 인벨로프",
      "panel.waveformAria": "파형 및 스펙트로그램",
      "wave.hint": "호스트 실시간 레벨로 렌더링됨 (PCM 샘플 미제공)",
      "wave.scanBtnTitle": "전체 파형 사전 스캔 (무음 스캔, 다시 클릭하여 취소)",
      "wave.modeRealtime": "실시간",
      "wave.modePrerender": "사전 렌더링",
      "wave.modeTitleRealtime": "파형 모드: 실시간 (재생에 따라 동적 렌더링)",
      "wave.modeTitlePrerender": "파형 모드: 사전 렌더링 (전체 트랙 무음 스캔)",
      "wave.qualityFine": "고정밀",
      "wave.qualityStandard": "표준",
      "wave.qualityFast": "고속",
      "wave.qualityTitle": "스캔 정밀도: 고속 / 표준 / 고정밀 (현재: {quality})",
      "wave.qualityFineDesc": "고정밀 · 5x 고밀도 오버샘플링",
      "wave.qualityStandardDesc": "표준 · 5x 밸런스 샘플링",
      "wave.qualityFastDesc": "고속 · 5x 빠른 개요",
      "wave.zoomInTitle": "수평 확대 (Ctrl++)",
      "wave.zoomOutTitle": "수평 축소 (Ctrl+-)",
      "wave.zoomFitTitle": "창에 맞추기 (Ctrl+0)",
      "wave.navTitle": "클릭 또는 드래그하여 탐색",
      "wave.waterfallTitle": "스펙트로그램 폭포 (실시간 주파수 에너지)",

      "panel.analyzer": "마스터링 스위트",
      "panel.analyzerAria": "효과 - Insight 2 Pro",
      "analyzer.vstTitle": "효과 - Insight 2 Pro",
      "analyzer.stateLive": "실시간",
      "analyzer.stateFrozen": "고정됨",
      "analyzer.sourceLabel": "데이터 소스",
      "analyzer.sourceVal": "호스트 입력 레벨 · 스펙트럼",
      "analyzer.freezeTitle": "화면 정지(프리즈) / 해제 (F)",
      "analyzer.resetTitle": "피크 통계 초기화 (P)",
      "analyzer.radarTitle": "라우드니스 레이더 (실시간 RMS)",
      "analyzer.spectrumTitle": "스펙트럼 및 주파수 분포",
      "analyzer.levelsTitle": "입력 레벨 (Peak / RMS · dBFS)",
      "analyzer.soundfieldTitle": "Sound Field",
      "analyzer.soundfieldGear": "음장 표시 옵션",
      "analyzer.tabPolarSample": "Polar Sample",
      "analyzer.tabPolarLevel": "Polar Level",
      "analyzer.tabLissajous": "Lissajous",

      "panel.lyrics": "가사",
      "panel.lyricsAria": "동기화 가사 디스플레이",
      "lyrics.waiting": "가사 대기 중",
      "lyrics.synced": "동기화 가사",
      "lyrics.plain": "일반 텍스트 가사",
      "lyrics.instrumental": "연주곡입니다. 음악을 감상해 보세요",
      "lyrics.empty": "표시할 가사가 없습니다",

      "transport.aria": "트랜스포트 제어",
      "transport.total": "전체 시간",
      "transport.remain": "남은 시간",
      "transport.prev": "이전 곡 (Ctrl+←)",
      "transport.rw": "5초 되감기 (←)",
      "transport.playpause": "재생 / 일시정지 (스페이스바)",
      "transport.ff": "5초 빨리감기 (→)",
      "transport.next": "다음 곡 (Ctrl+→)",
      "transport.repeat": "전곡 반복",
      "transport.repeatOne": "한 곡 반복",
      "transport.shuffle": "셔플 재생",
      "transport.sequential": "순차 재생",
      "transport.mute": "음소거 (M)",
      "transport.unmute": "음소거 해제 (M)",
      "transport.volume": "볼륨",
      "transport.seek": "재생 위치 바",

      "prefs.title": "환경설정 (Preferences)",
      "prefs.renderModeTitle": "파형 렌더링 모드",
      "prefs.renderModeEnable": "전체 트랙 파형 사전 렌더링 활성화 (Pre-render)",
      "prefs.renderModeHint": "기본값은 해제(실시간 모드). 재생에 맞춰 동적으로 파형을 그려 최상의 성능과 낮은 메모리 사용량을 유지합니다.",
      "prefs.qualityTitle": "사전 스캔 샘플링 정밀도 (5x 오버샘플링)",
      "prefs.qualityFineOpt": "고정밀 (Fine - 5x 고밀도 오버샘플링 · 기본값)",
      "prefs.qualityStandardOpt": "표준 (Standard - 5x 밸런스 샘플링)",
      "prefs.qualityFastOpt": "고속 (Fast - 5x 빠른 개요)",
      "prefs.rangeTitle": "다이내믹 레인지 표준",
      "prefs.rangeDesc": "마스터 레벨 및 트랙 눈금은 <strong>0 ~ -60 dBFS</strong>를 지원하여 전문가급 마스터링의 폭넓은 다이내믹 레인지와 미세 노이즈 플로어를 충실히 표현합니다.",
      "prefs.languageTitle": "인터페이스 언어 (Language)",
      "prefs.languageHint": "한국어, 简体中文, English, Français, 日本語를 지원하며 변경 시 즉시 반영 및 저장됩니다.",

      "shortcuts.title": "키보드 단축키 (Keyboard Shortcuts)",
      "shortcuts.playpause": "재생 / 일시정지",
      "shortcuts.rwff": "5초 되감기 / 5초 빨리감기",
      "shortcuts.prevnext": "이전 곡 / 다음 곡",
      "shortcuts.vol": "볼륨 증가 / 감소 5%",
      "shortcuts.mute": "음소거 / 해제",
      "shortcuts.zoom": "타임라인 확대 / 축소",
      "shortcuts.zoomFit": "전체 곡 뷰로 리셋",
      "shortcuts.centerPlayhead": "재생 포인터 중앙 맞춤",
      "shortcuts.freeze": "스펙트럼 분석기 정지 / 재개",
      "shortcuts.resetPeak": "모든 피크 레벨 지표 초기화",
      "shortcuts.theme": "라이트 / 다크 모드 전환",
      "shortcuts.waveColor": "파형 색상 전환 (하늘색 / 벚꽃색)",
      "shortcuts.prefs": "환경설정",
      "shortcuts.reload": "DAW UI 다시 불러오기",
      "shortcuts.close": "창 닫기 / ECHO로 복귀",

      "about.title": "Adobe Audition DAW 정보",
      "about.headerTitle": "Adobe Audition DAW Theme for ECHO",
      "about.version": "버전 1.2.0 (다국어 에디션)",
      "about.desc": "ECHO 음악 플레이어를 위해 정밀 설계된 고품질 Adobe Audition 스타일의 전문가급 디지털 오디오 워크스테이션(DAW) 테마 및 실시간 분석기 스위트.",
      "about.featTitle": "주요 기능:",
      "about.f1": "• 다크 / 라이트 전문 마스터링 테마 매끄러운 전환 (Ctrl+T)",
      "about.f2": "• 하늘색 및 벚꽃색 파형 컬러 팔레트 지원 (Ctrl+Alt+W)",
      "about.f3": "• 완벽한 다국어 지원: 한국어, 중국어, 영어, 프랑스어, 일본어",
      "about.f4": "• 0 ~ -60 dBFS 실제 다이내믹 레인지 멀티트랙 타임라인",
      "about.f5": "• 실시간 3D FFT 스펙트로그램 폭포수 (정지 시 스마트 슬립)",
      "about.f6": "• 극좌표 및 리사주 (Lissajous) 스테레오 위상 벡터 스코프",
      "about.f7": "• 방송 표준 규격 LUFS 라우드니스 레이더 및 True Peak 미터링",
      "about.f8": "• 상단 전문가 메뉴바 및 글로벌 단축키 완벽 지원",
      "about.f9": "• 멀티 소스 스트리밍 및 로컬 앨범 아트 무깜빡임 폴백",
      "about.footer": "Inspired by Adobe Audition CC &bull; Built for ECHO",

      "toast.langSwitched": "인터페이스 언어 변경됨: 한국어",
      "toast.pathCopied": "오디오 파일 경로가 클립보드에 복사되었습니다",
      "toast.pathFailed": "복사 실패",
      "toast.noPath": "사용 가능한 오디오 파일 경로가 없습니다",
      "toast.metaCopied": "곡 메타데이터 JSON이 복사되었습니다",
      "toast.copied": "복사됨",
      "toast.peakReset": "모든 피크 레벨 지표가 초기화되었습니다",
      "toast.scanStarted": "전체 파형 오버샘플링 스캔을 시작했습니다",
      "toast.scanRunning": "파형 스캔이 진행 중입니다…",
      "toast.scanCancelled": "스캔 작업 취소를 요청했습니다",
      "toast.noScan": "진행 중인 스캔 작업이 없습니다",
      "toast.modeRealtime": "실시간 파형 모드로 전환되었습니다",
      "toast.modePrerender": "사전 렌더링 모드 활성화됨, 파형 스캔 시작…",
      "toast.scanQuality": "스캔 정밀도 설정됨: {quality}",
      "toast.soundfield": "음장 표시 모드 변경됨: {mode}",
      "toast.playheadCentered": "재생 포인터가 중앙에 정렬되었습니다",
      "toast.viewReset": "타임라인이 전체 개요로 리셋되었습니다",
      "toast.posCopied": "현재 재생 위치: {time}",
      "toast.frozen": "스펙트럼 및 음장이 고정되었습니다",
      "toast.unfrozen": "스펙트럼 및 음장 실시간 표시가 재개되었습니다",
      "toast.waterfallCleared": "스펙트로그램 기록이 초기화되었습니다",
      "toast.repeatAll": "전곡 반복이 설정되었습니다",
      "toast.repeatOne": "한 곡 반복이 설정되었습니다",
      "toast.sequential": "순차 재생으로 전환되었습니다",
      "toast.repeatOff": "반복 재생이 해제되었습니다",
      "toast.muted": "음소거되었습니다",
      "toast.unmuted": "음소거가 해제되었습니다",
      "toast.volStandard": "표준 모니터링 볼륨 (80%) 설정됨",
      "toast.volMax": "최대 출력 볼륨 (100%) 설정됨",
      "toast.syncQueue": "호스트에서 재생 대기열을 동기화하는 중…",
      "toast.resize": "모든 DAW 캔버스를 화면에 맞게 재정렬했습니다",
      "toast.themeLight": "라이트 마스터링 테마로 전환되었습니다",
      "toast.themeDark": "다크 전문 테마로 전환되었습니다",
      "toast.wavePink": "파형 색상: 벚꽃색",
      "toast.waveBlue": "파형 색상: 하늘색",

      "menu.file.reload": "인터페이스 새로고침 (Reload)",
      "menu.file.copyPath": "오디오 파일 경로 복사",
      "menu.file.copyMeta": "곡 메타데이터 복사 (JSON)",
      "menu.file.prefs": "환경설정 (Preferences)...",
      "menu.file.back": "ECHO 기본 화면으로 복귀",

      "menu.edit.copyTitle": "곡 제목 복사",
      "menu.edit.copyArtist": "아티스트 정보 복사",
      "menu.edit.resetPeak": "피크 레벨 지표 초기화 (Reset Peak)",
      "menu.edit.rescan": "현재 트랙 파형 다시 스캔",

      "menu.multi.realtime": "파형 렌더링: 실시간 모드",
      "menu.multi.prerender": "파형 렌더링: 사전 렌더링 모드",
      "menu.multi.fine": "스캔 정밀도: 고정밀 (5x 오버샘플링)",
      "menu.multi.standard": "스캔 정밀도: 표준 (5x 밸런스)",
      "menu.multi.fast": "스캔 정밀도: 고속 (5x 빠른 개요)",
      "menu.multi.polarSample": "음장: 극좌표 샘플 (Polar Sample)",
      "menu.multi.polarLevel": "음장: 극좌표 레벨 (Polar Level)",
      "menu.multi.lissajous": "음장: 리사주 곡선 (Lissajous)",

      "menu.clip.center": "재생 포인터 중앙 맞춤",
      "menu.clip.fit": "전체 곡 길이에 맞춤",
      "menu.clip.copyTime": "현재 타임코드 복사",

      "menu.fx.freeze": "스펙트럼 분석기: 고정 / 재개",
      "menu.fx.clearWaterfall": "스펙트로그램 폭포수 기록 초기화",

      "menu.zoom.in": "타임라인 확대 (Zoom In)",
      "menu.zoom.out": "타임라인 축소 (Zoom Out)",
      "menu.zoom.fit": "전체 곡 맞춤 (Fit Full)",
      "menu.zoom.ff5": "5초 빨리감기 (+5s)",
      "menu.zoom.rw5": "5초 되감기 (-5s)",

      "menu.trans.playpause": "재생 / 일시정지",
      "menu.trans.stop": "재생 정지",
      "menu.trans.prev": "이전 곡",
      "menu.trans.next": "다음 곡",
      "menu.trans.start": "곡 시작 위치로 이동",
      "menu.trans.rw5": "5초 되감기 (-5s)",
      "menu.trans.ff5": "5초 빨리감기 (+5s)",
      "menu.trans.rw15": "15초 되감기 (-15s)",
      "menu.trans.ff15": "15초 빨리감기 (+15s)",
      "menu.trans.repeatAll": "전곡 반복",
      "menu.trans.repeatOne": "한 곡 반복",
      "menu.trans.sequential": "순차 재생",
      "menu.trans.shuffle": "셔플 재생",
      "menu.trans.cycle": "반복 모드 순환",
      "menu.trans.mute": "음소거 전환",
      "menu.trans.volUp": "볼륨 증가 (+5%)",
      "menu.trans.volDown": "볼륨 감소 (-5%)",
      "menu.trans.vol80": "레퍼런스 모니터 볼륨 (80%)",
      "menu.trans.vol100": "최대 출력 볼륨 (100%)",
      "menu.trans.syncQueue": "재생 대기열 동기화",
      "menu.trans.scanFull": "전체 트랙 오버샘플링 스캔 실행",
      "menu.trans.stopScan": "진행 중인 스캔 중지",
      "menu.trans.resetHold": "피크 홀드 및 RMS 미터 초기화",

      "menu.win.theme": "라이트 / 다크 모드 전환",
      "menu.win.waveBlue": "파형 색상: 하늘색",
      "menu.win.wavePink": "파형 색상: 벚꽃색",
      "menu.win.fullscreen": "전체화면 전환",
      "menu.win.resize": "모든 캔버스 화면 맞춤",

      "menu.help.shortcuts": "키보드 단축키 안내...",
      "menu.help.prefs": "환경설정 (Preferences)...",
      "menu.help.about": "Adobe Audition DAW 정보..."
    }
  };

  function t(key, fallback = null) {
    const dict = I18N[state.lang] || I18N.zh;
    return dict[key] ?? I18N.zh[key] ?? fallback ?? key;
  }

  function applyLanguage(lang, persist = true) {
    if (!I18N[lang]) lang = "zh";
    state.lang = lang;

    const langCodes = { zh: "zh-CN", en: "en-US", fr: "fr-FR", ja: "ja-JP", ko: "ko-KR" };
    document.documentElement.lang = langCodes[lang] || "zh-CN";

    const langBtnLabel = el("lang-toggle-label");
    if (langBtnLabel) {
      const cur = SUPPORTED_LANGS.find((l) => l.id === lang);
      langBtnLabel.textContent = cur ? cur.label : "简体中文";
    }

    const themeLabel = el("theme-toggle-label");
    if (themeLabel) {
      themeLabel.textContent = state.themeMode === "light" ? t("theme.dark") : t("theme.light");
    }
    const waveLabel = el("wave-color-toggle-label");
    if (waveLabel) {
      waveLabel.textContent = state.waveColor === "pink" ? t("wave.blue") : t("wave.pink");
    }

    document.querySelectorAll("[data-i18n]").forEach((node) => {
      const key = node.getAttribute("data-i18n");
      if (key) node.textContent = t(key);
    });

    document.querySelectorAll("[data-i18n-title]").forEach((node) => {
      const key = node.getAttribute("data-i18n-title");
      if (key) {
        const text = t(key);
        node.setAttribute("title", text);
        node.setAttribute("aria-label", text);
      }
    });

    document.querySelectorAll("[data-i18n-aria-label]").forEach((node) => {
      const key = node.getAttribute("data-i18n-aria-label");
      if (key) node.setAttribute("aria-label", t(key));
    });

    updateScanUi();

    const anSource = el("analyzer-source");
    if (anSource) anSource.textContent = t("analyzer.sourceVal");

    setText("analyzer-state", state.frozen ? t("analyzer.stateFrozen") : t("analyzer.stateLive"));

    if (persist) {
      try {
        localStorage.setItem("echo:audition:lang", lang);
      } catch {}
      scheduleSave();
    }
  }

  function setLanguage(lang, showToast = true) {
    if (!I18N[lang]) return;
    applyLanguage(lang, true);
    if (showToast) {
      toast(t("toast.langSwitched"), "ok", 1800);
    }
  }
  window.setLanguage = setLanguage;
  window.SUPPORTED_LANGS = SUPPORTED_LANGS;
  window.I18N = I18N;

  // ── protocol ──────────────────────────────────────────────────────────────
  const PROTOCOL = 1;
  const post = (message) => parent.postMessage({ protocolVersion: PROTOCOL, ...message }, "*");
  let requestSeq = 0;
  const pending = new Map();
  const command = (name, payload = {}, timeoutMs = 4000) =>
    new Promise((resolve) => {
      const requestId = String(++requestSeq);
      pending.set(requestId, resolve);
      post({ type: "echo:workshop-ui:command", requestId, command: name, payload });
      window.setTimeout(() => {
        if (pending.delete(requestId)) resolve({ ok: false, error: "timeout" });
      }, timeoutMs);
    });
  const commandNoWait = (name, payload = {}) => {
    post({ type: "echo:workshop-ui:command", requestId: String(++requestSeq), command: name, payload });
  };

  // The host answers (or rejects) every command with a result message. Silent
  // rejections used to look like "the button does nothing", so failures are now
  // surfaced to the user with a concrete reason.
  let toastTimer = null;
  function toast(message, tone = "warn", ms = 4600) {
    const node = el("theme-toast");
    if (!node) return;
    if (node.textContent !== message) node.textContent = message;
    node.dataset.tone = tone;
    node.hidden = false;
    if (toastTimer) window.clearTimeout(toastTimer);
    toastTimer = window.setTimeout(() => {
      node.hidden = true;
    }, ms);
  }

  const COMMAND_HINTS = {
    "queue-unavailable":
      "宿主当前没有播放队列会话（例如直接播放了单个文件），因此无法切歌 / 切换循环与随机。请从歌曲、专辑或队列开始播放后再试。",
    "capability-denied": "宿主未授予该能力，命令被拒绝。",
    "rate-limited": "操作过于频繁，已被宿主限流，请稍后再试。",
    "concurrency-limited": "宿主繁忙，请稍后再试。",
    "command-unavailable": "当前状态下宿主不提供该操作。",
    timeout: "宿主未响应该命令（可能界面未就绪或被限流）。",
  };

  async function sendPlaybackCommand(name, payload = {}, timeoutMs = 2500) {
    const result = await command(name, payload, timeoutMs);
    if (result && result.ok === false) {
      const error = result.error || "command-failed";
      toast(COMMAND_HINTS[error] || `命令失败：${name}（${error}）`);
      return false;
    }
    return true;
  }

  // ── state ─────────────────────────────────────────────────────────────────
  const state = {
    connected: false,
    appearance: null,
    playback: {
      state: "idle",
      currentTrackId: null,
      positionSeconds: 0,
      durationSeconds: 0,
      volume: null,
      shuffleEnabled: false,
      repeatMode: "off",
    },
    track: null,
    lyricsDoc: null,
    peek: null,
    levels: { peakDb: null, rmsDb: null, source: null },
    meta: null,
    spectrum: { bands: [], energy: 0, transient: 0, state: "none" },
    clock: null,
    motion: { frameIntervalMs: null },
    history: [],
    liveQueue: [],
    queueTab: "tracks",
    envRev: 0,
    envelopes: new Map(), // trackId -> { p: Float32Array, r: Float32Array, filled: number }
    rmsHistory: [],       // [{ t, db }] real host rms samples
    peakHoldDb: -Infinity,
    peakHoldAt: 0,
    view: { start: 0, end: 0, zoomed: false },
    frozen: false,
    volume: 1,
    muted: false,
    seeking: false,
    previewPosition: 0,
    themeMode: "dark",
    waveColor: "blue",
    lang: "zh",
    prefs: { autoScan: false, preRender: false, scanQuality: "fine" },
    scan: {
      active: false,
      progress: 0,
      message: "",
      cancel: false,
      points: 0,
      perPointMs: 0,
      lastCostMs: 0,
      lastAutoAt: 0,
    },
  };

  const ENV_BUCKETS = 2048;
  const MAX_HISTORY = 40;
  // 2048 buckets × 2 lanes as JSON ≈ 17 KB per track and the host caps the theme
  // storage namespace at 64 KB, so only the current track's envelope is kept.
  const MAX_ENV_CACHE = 1;

  // Pre-scan precision presets. The scan is bound by the host's level publish
  // rate (~10 Hz), so fewer samples simply mean a shorter silent window.
  // Sampling rate boosted by 5x across all precision tiers:
  // - fine: 1 sample per 0.24s (~4.17 Hz), 480-2000 points
  // - standard: 1 sample per 0.44s (~2.27 Hz), 240-750 points
  // - fast: 1 sample per 1.2s (~0.83 Hz), 120-400 points
  const SCAN_QUALITY = {
    fast: { label: "极速", divisor: 1.2, min: 120, max: 400 },
    standard: { label: "标准", divisor: 0.44, min: 240, max: 750 },
    fine: { label: "精细", divisor: 0.24, min: 480, max: 2000 },
  };
  const SCAN_ORDER = ["fast", "standard", "fine"];
  const scanPoints = (duration, qualityKey) => {
    const quality = SCAN_QUALITY[qualityKey] ?? SCAN_QUALITY.fine;
    return clamp(Math.round(duration / quality.divisor), quality.min, quality.max);
  };

  // ── canvas helpers (DPR aware, resize driven) ────────────────────────────
  const canvases = new Map();
  function setupCanvas(id) {
    const canvas = el(id);
    if (!canvas) return null;
    // alpha:false + desynchronized:true hand the canvases to the GPU compositor
    // with the lowest-latency path Chromium exposes, which measurably reduces
    // per-frame cost for the analyser panes on high-refresh displays.
    const entry = {
      canvas,
      ctx: canvas.getContext("2d", { alpha: false, desynchronized: true }),
      w: 0,
      h: 0,
    };
    canvases.set(id, entry);
    resizeCanvas(entry);
    return entry;
  }
  function resizeCanvas(entry) {
    const rect = entry.canvas.getBoundingClientRect();
    const dpr = window.devicePixelRatio || 1;
    const w = Math.max(1, Math.floor(rect.width));
    const h = Math.max(1, Math.floor(rect.height));
    const pixelW = Math.max(1, Math.round(w * dpr));
    const pixelH = Math.max(1, Math.round(h * dpr));
    if (entry.canvas.width !== pixelW || entry.canvas.height !== pixelH) {
      entry.canvas.width = pixelW;
      entry.canvas.height = pixelH;
    }
    entry.w = w;
    entry.h = h;
    entry.ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    entry.ctx.imageSmoothingEnabled = false;
    return entry;
  }
  function resizeAll() {
    canvases.forEach(resizeCanvas);
  }

  const isLightMode = () =>
    state.themeMode === "light" ||
    document.documentElement.getAttribute("data-theme") === "light";

  function setThemeMode(mode, showToast = true) {
    const target = mode === "light" ? "light" : "dark";
    state.themeMode = target;
    document.documentElement.setAttribute("data-theme", target);

    const iconEl = el("theme-toggle-icon");
    const labelEl = el("theme-toggle-label");
    const btnEl = el("btn-theme-toggle");
    if (iconEl && labelEl) {
      if (target === "light") {
        iconEl.textContent = "🌙";
        labelEl.textContent = "黑暗";
        btnEl?.setAttribute("title", "切换至经典黑暗模式 (Ctrl+T)");
        btnEl?.setAttribute("aria-label", "切换至经典黑暗模式 (Ctrl+T)");
      } else {
        iconEl.textContent = "☀";
        labelEl.textContent = "日间";
        btnEl?.setAttribute("title", "切换至日间明亮模式 (Ctrl+T)");
        btnEl?.setAttribute("aria-label", "切换至日间明亮模式 (Ctrl+T)");
      }
    }

    // Invalidate cached waveform canvas layer
    const waveEntry = canvases.get("waveform-canvas");
    if (waveEntry) {
      waveEntry.layer = null;
      waveEntry.layerKey = null;
    }

    // Force re-render of all visualizers
    renderNavigator();
    renderRuler();
    renderWaveform();
    renderSpectrum();
    renderRadar();
    renderSoundField();

    try {
      localStorage.setItem("echo:audition:themeMode", target);
    } catch {}

    if (showToast) {
      toast(`已切换至${target === "light" ? "日间明亮模式" : "经典黑暗模式"}`, "ok", 1500);
      scheduleSave();
    }
  }

  function toggleThemeMode() {
    setThemeMode(state.themeMode === "light" ? "dark" : "light", true);
  }

  // ── waveform color palettes (pastel blue & pastel pink) ─────────────────────
  const WAVE_PALETTES = {
    blue: {
      id: "blue",
      name: "淡雅天蓝",
      dark: {
        pkBody: "rgba(100, 180, 246, 0.48)",
        pkEdge: "rgba(187, 222, 251, 0.95)",
        rmsBody: "rgba(79, 160, 235, 0.45)",
        rmsEdge: "rgba(144, 202, 249, 0.85)",
        overlay: "rgba(179, 229, 252, 0.32)",
        nav: "rgba(100, 180, 246, 0.85)",
      },
      light: {
        pkBody: "rgba(70, 145, 220, 0.50)",
        pkEdge: "rgba(35, 110, 190, 0.95)",
        rmsBody: "rgba(55, 130, 205, 0.46)",
        rmsEdge: "rgba(25, 95, 175, 0.90)",
        overlay: "rgba(125, 185, 245, 0.38)",
        nav: "rgba(60, 135, 210, 0.85)",
      },
    },
    pink: {
      id: "pink",
      name: "柔美粉樱",
      dark: {
        pkBody: "rgba(244, 143, 177, 0.50)",
        pkEdge: "rgba(255, 205, 225, 0.95)",
        rmsBody: "rgba(236, 115, 155, 0.46)",
        rmsEdge: "rgba(255, 180, 205, 0.85)",
        overlay: "rgba(255, 195, 215, 0.32)",
        nav: "rgba(244, 143, 177, 0.85)",
      },
      light: {
        pkBody: "rgba(230, 115, 150, 0.52)",
        pkEdge: "rgba(200, 70, 110, 0.95)",
        rmsBody: "rgba(215, 95, 135, 0.46)",
        rmsEdge: "rgba(180, 55, 95, 0.90)",
        overlay: "rgba(245, 160, 185, 0.40)",
        nav: "rgba(220, 85, 125, 0.85)",
      },
    },
  };

  function setWaveColor(color, showToast = true) {
    const target = color === "pink" ? "pink" : "blue";
    state.waveColor = target;
    document.documentElement.setAttribute("data-wave-color", target);

    const iconEl = el("wave-color-toggle-icon");
    const labelEl = el("wave-color-toggle-label");
    const btnEl = el("btn-wave-color-toggle");
    if (iconEl && labelEl) {
      if (target === "pink") {
        iconEl.textContent = "🌊";
        labelEl.textContent = "天蓝";
        btnEl?.setAttribute("title", "切换波形色彩至淡雅天蓝 (Ctrl+Alt+W)");
        btnEl?.setAttribute("aria-label", "切换波形色彩至淡雅天蓝 (Ctrl+Alt+W)");
      } else {
        iconEl.textContent = "🌸";
        labelEl.textContent = "粉樱";
        btnEl?.setAttribute("title", "切换波形色彩至柔美粉樱 (Ctrl+Alt+W)");
        btnEl?.setAttribute("aria-label", "切换波形色彩至柔美粉樱 (Ctrl+Alt+W)");
      }
    }

    const waveEntry = canvases.get("waveform-canvas");
    if (waveEntry) {
      waveEntry.layer = null;
      waveEntry.layerKey = null;
    }

    renderNavigator();
    renderWaveform();

    try {
      localStorage.setItem("echo:audition:waveColor", target);
    } catch {}

    if (showToast) {
      const palette = WAVE_PALETTES[target];
      toast(`已切换波形色彩至${palette.name}`, "ok", 1500);
      scheduleSave();
    }
  }

  function toggleWaveColor() {
    setWaveColor(state.waveColor === "pink" ? "blue" : "pink", true);
  }

  window.setThemeMode = setThemeMode;
  window.toggleThemeMode = toggleThemeMode;
  window.setWaveColor = setWaveColor;
  window.toggleWaveColor = toggleWaveColor;

  // ── persistence ───────────────────────────────────────────────────────────
  const quantize = (v) => Math.round(clamp(v, 0, 1) * 100) / 100;
  async function loadStorage() {
    try {
      const [history, envelopes, prefs] = await Promise.all([
        command("storage:get", { key: "history" }),
        command("storage:get", { key: "envelopes" }),
        command("storage:get", { key: "prefs" }),
      ]);
      if (Array.isArray(history?.value)) state.history = history.value.slice(0, MAX_HISTORY);
      if (envelopes?.value && typeof envelopes.value === "object") {
        for (const [trackId, series] of Object.entries(envelopes.value)) {
          if (!series || !Array.isArray(series.p) || !Array.isArray(series.r)) continue;
          const p = new Float32Array(ENV_BUCKETS);
          const r = new Float32Array(ENV_BUCKETS);
          for (let i = 0; i < ENV_BUCKETS; i++) {
            p[i] = num(series.p[i], 0) || 0;
            r[i] = num(series.r[i], 0) || 0;
          }
          // Merge instead of replace: a late storage read must never wipe an
          // envelope that a pre-scan already produced.
          const existing = state.envelopes.get(trackId);
          if (existing) {
            for (let i = 0; i < ENV_BUCKETS; i++) {
              existing.p[i] = Math.max(existing.p[i], p[i]);
              existing.r[i] = Math.max(existing.r[i], r[i]);
            }
          } else {
            state.envelopes.set(trackId, { p, r, filled: num(series.filled, 0) || 0 });
          }
        }
      }
      if (prefs?.value?.volume !== undefined) state.volume = clamp(num(prefs.value.volume, 1) || 1, 0, 1);
      if (prefs?.value?.autoScan === false) state.prefs.autoScan = false;
      if (typeof prefs?.value?.preRender === "boolean") state.prefs.preRender = prefs.value.preRender;
      if (SCAN_QUALITY[prefs?.value?.scanQuality]) state.prefs.scanQuality = prefs.value.scanQuality;
      if (typeof prefs?.value?.soundfieldMode === "string") {
        state.soundfieldMode = prefs.value.soundfieldMode;
        const sfTabs = document.querySelectorAll(".soundfield-tab");
        sfTabs.forEach((t) => t.classList.toggle("active", t.getAttribute("data-mode") === state.soundfieldMode));
      }
      if (typeof prefs?.value?.lang === "string" && I18N[prefs.value.lang]) {
        applyLanguage(prefs.value.lang, false);
      } else {
        try {
          const localLang = localStorage.getItem("echo:audition:lang");
          if (localLang && I18N[localLang]) applyLanguage(localLang, false);
        } catch {}
      }
      if (typeof prefs?.value?.themeMode === "string") {
        setThemeMode(prefs.value.themeMode, false);
      } else {
        try {
          const local = localStorage.getItem("echo:audition:themeMode");
          if (local === "light" || local === "dark") setThemeMode(local, false);
        } catch {}
      }
      if (typeof prefs?.value?.waveColor === "string") {
        setWaveColor(prefs.value.waveColor, false);
      } else {
        try {
          const local = localStorage.getItem("echo:audition:waveColor");
          if (local === "blue" || local === "pink") setWaveColor(local, false);
        } catch {}
      }
    } catch {
      /* storage is optional */
    }
  }
  let saveTimer = null;
  function scheduleSave() {
    if (saveTimer !== null) return;
    saveTimer = window.setTimeout(() => {
      saveTimer = null;
      void command("storage:set", { key: "history", value: state.history.slice(0, MAX_HISTORY) });
      const dump = {};
      const entries = [...state.envelopes.entries()].slice(-MAX_ENV_CACHE);
      for (const [trackId, series] of entries) {
        dump[trackId] = {
          p: Array.from(series.p, quantize),
          r: Array.from(series.r, quantize),
          filled: series.filled,
        };
      }
      void command("storage:set", { key: "envelopes", value: dump });
      void command("storage:set", {
        key: "prefs",
        value: {
          volume: state.volume,
          autoScan: state.prefs.autoScan,
          preRender: state.prefs.preRender,
          scanQuality: state.prefs.scanQuality,
          soundfieldMode: state.soundfieldMode,
          lang: state.lang,
          themeMode: state.themeMode,
          waveColor: state.waveColor,
        },
      });
    }, 2500);
  }

  // ── derived data ──────────────────────────────────────────────────────────
  const currentDuration = () =>
    num(state.clock?.durationSeconds, 0) || num(state.playback.durationSeconds, 0) ||
    num(state.track?.durationSeconds, 0) || 0;

  const positionSeconds = () => {
    const clock = state.clock;
    if (!clock) return num(state.playback.positionSeconds, 0) || 0;
    const now = performance.timeOrigin + performance.now();
    const elapsed = clamp(now - num(clock.sampledAtMs, now), 0, 1500);
    const extra = clock.state === "playing" ? (elapsed * num(clock.playbackRate, 1)) / 1000 : 0;
    const limit = num(clock.durationSeconds, 0) || Infinity;
    return clamp((num(clock.positionSeconds, 0) || 0) + extra, 0, limit);
  };

  function normalizeBands(bands, count) {
    const src = Array.isArray(bands) ? bands : [];
    if (src.length === 0) return new Array(count).fill(0);
    const out = new Array(count);
    for (let i = 0; i < count; i++) {
      const from = Math.floor((i / count) * src.length);
      const to = Math.max(from + 1, Math.floor(((i + 1) / count) * src.length));
      let sum = 0;
      let n = 0;
      for (let j = from; j < to && j < src.length; j++) {
        const v = num(src[j], 0) || 0;
        sum += v;
        n += 1;
      }
      out[i] = n > 0 ? clamp(sum / n, 0, 1) : 0;
    }
    return out;
  }

  const spectralCentroid = (bands) => {
    let weighted = 0;
    let total = 0;
    for (let i = 0; i < bands.length; i++) {
      weighted += bands[i] * i;
      total += bands[i];
    }
    return total > 0 ? weighted / total / Math.max(1, bands.length - 1) : 0;
  };

  const hasSignal = (bands) => Array.isArray(bands) && bands.some((v) => (num(v, 0) || 0) > 0.002);
  const isPlayingState = () =>
    state.playback.state === "playing" || state.playback.state === "loading";

  const loudnessStats = () => {
    const now = performance.now();
    const recent = state.rmsHistory.filter((s) => now - s.t < 4000);
    const pick = (windowMs) => {
      const slice = recent.filter((s) => now - s.t <= windowMs);
      if (slice.length === 0) return null;
      let sum = 0;
      for (const s of slice) sum += Math.pow(10, s.db / 10);
      return 10 * Math.log10(sum / slice.length);
    };
    const momentary = pick(400);
    const shortTerm = pick(3000);
    const integrated = (() => {
      const gated = recent.filter((s) => s.db > -70);
      if (gated.length === 0) return null;
      let sum = 0;
      for (const s of gated) sum += Math.pow(10, s.db / 10);
      return 10 * Math.log10(sum / gated.length);
    })();
    const range = (() => {
      if (recent.length < 20) return null;
      let min = Infinity;
      let max = -Infinity;
      for (let i = 0; i + 20 <= recent.length; i += 10) {
        let sum = 0;
        for (let j = i; j < i + 20; j++) sum += Math.pow(10, recent[j].db / 10);
        const value = 10 * Math.log10(sum / 20);
        min = Math.min(min, value);
        max = Math.max(max, value);
      }
      return Number.isFinite(min) && Number.isFinite(max) ? Math.max(0, max - min) : null;
    })();
    return { momentary, shortTerm, integrated, range };
  };

  // ── envelope capture ──────────────────────────────────────────────────────
  function envelopeFor(trackId) {
    if (!trackId) return null;
    let series = state.envelopes.get(trackId);
    if (!series) {
      series = { p: new Float32Array(ENV_BUCKETS), r: new Float32Array(ENV_BUCKETS), filled: 0 };
      state.envelopes.set(trackId, series);
      while (state.envelopes.size > MAX_ENV_CACHE) {
        const oldest = state.envelopes.keys().next().value;
        if (oldest === trackId) break;
        state.envelopes.delete(oldest);
      }
    }
    return series;
  }

  function captureEnvelope() {
    const duration = currentDuration();
    const trackId = state.playback.currentTrackId || state.track?.id;
    if (!trackId || duration <= 0) return;
    if (state.playback.state !== "playing" && state.playback.state !== "loading") return;
    const series = envelopeFor(trackId);
    if (!series) return;
    const pos = positionSeconds();
    const bucket = clamp(Math.floor((pos / duration) * ENV_BUCKETS), 0, ENV_BUCKETS - 1);
    const peak = num(state.levels.peakDb, null);
    const rms = num(state.levels.rmsDb, null);
    const energy = num(state.spectrum.energy, 0) || 0;
    const pv = peak === null ? energy : dbToRatio(peak, -60);
    const rv = rms === null ? energy * 0.8 : dbToRatio(rms, -60);
    if (pv > series.p[bucket]) series.p[bucket] = pv;
    if (rv > series.r[bucket]) series.r[bucket] = rv;
    state.envRev += 1;
    series.filled = Math.min(ENV_BUCKETS, series.filled + (series.p[bucket] > 0 ? 0 : 1));
    if (rms !== null) {
      const now = performance.now();
      state.rmsHistory.push({ t: now, db: rms });
      if (state.rmsHistory.length > 600) state.rmsHistory.splice(0, state.rmsHistory.length - 600);
    }
    scheduleSave();
  }

  // ── waveform pre-scan ────────────────────────────────────────────────────
  // The host only exposes live meters, so a full-track envelope is obtained by
  // stepping the playhead through the track once (silently) and sampling the
  // real levels at each step. The result lives in the same bucket array the live
  // capture uses, so playback (and future plays) render the complete waveform
  // immediately instead of drawing it in as the song advances.
  function envelopeCoverage(series) {
    if (!series) return 0;
    let filled = 0;
    for (let i = 0; i < series.p.length; i++) if (series.p[i] > 0) filled += 1;
    return filled / series.p.length;
  }

  function updateScanUi() {
    const statusNode = el("wave-scan-state");
    const button = el("btn-scan-wave");
    const mode = el("btn-scan-mode");
    const trackId = state.playback.currentTrackId || state.track?.id;
    const series = trackId ? state.envelopes.get(trackId) : null;
    const coverage = Math.round(envelopeCoverage(series) * 100);
    const remaining =
      state.scan.active && state.scan.perPointMs > 0
        ? Math.max(0, (1 - state.scan.progress) * state.scan.points * state.scan.perPointMs) / 1000
        : 0;
    const text = state.scan.active
      ? `预扫描 ${Math.round(state.scan.progress * 100)}% · 预计还需 ${remaining.toFixed(1)}s · 静音中，可点击取消`
      : `${state.scan.message ? `${state.scan.message} · ` : ""}${series ? `包络 ${coverage}%` : ""}${
          state.scan.lastCostMs ? ` · 用时 ${(state.scan.lastCostMs / 1000).toFixed(1)}s` : ""
        }`;
    if (statusNode && statusNode.textContent !== text) statusNode.textContent = text;
    button?.classList.toggle("active", state.scan.active);
    if (mode) {
      const label = state.prefs.preRender ? "预渲染" : "实时";
      if (mode.textContent !== label) mode.textContent = label;
      mode.classList.toggle("active", state.prefs.preRender);
      const title = state.prefs.preRender
        ? "波形模式：预渲染（切歌后自动静音扫描整首）"
        : "波形模式：实时（波形随播放逐步绘制）";
      if (mode.title !== title) mode.title = title;
    }
    if (button) {
      // manual scan only makes sense in pre-render mode
      const hidden = !state.prefs.preRender;
      if (button.hidden !== hidden) button.hidden = hidden;
    }
    const quality = el("btn-scan-quality");
    if (quality) {
      const preset = SCAN_QUALITY[state.prefs.scanQuality] ?? SCAN_QUALITY.fine;
      if (quality.textContent !== preset.label) quality.textContent = preset.label;
      const duration = currentDuration();
      const estimate = duration > 0 ? ((scanPoints(duration, state.prefs.scanQuality) * 85) / 1000).toFixed(1) : "—";
      const title = `预扫描精度：${preset.label}（5x高密度采样 · 约 ${estimate} 秒）`;
      if (quality.title !== title) quality.title = title;
      if (quality.hidden !== !state.prefs.preRender) quality.hidden = !state.prefs.preRender;
    }
  }

  async function scanWaveform({ silent = true, points = 0, frameMs = 80 } = {}) {
    if (state.scan.active) {
      state.scan.cancel = true;
      return;
    }
    const duration = currentDuration();
    const trackId = state.playback.currentTrackId || state.track?.id;
    if (!trackId || duration <= 0) {
      state.scan.message = "无可扫描曲目";
      updateScanUi();
      return;
    }
    if (!isPlayingState()) {
      state.scan.message = "需播放中才能采样";
      updateScanUi();
      return;
    }
    // Adaptive sampling per the selected precision preset (极速 / 标准 / 精细).
    const effectivePoints = points > 0 ? points : scanPoints(duration, state.prefs.scanQuality);
    const series = envelopeFor(trackId);
    const startPos = positionSeconds();
    const savedVolume = state.volume;
    const scanStartedAt = performance.now();
    state.scan.active = true;
    state.scan.cancel = false;
    state.scan.progress = 0;
    state.scan.message = "";
    state.scan.points = effectivePoints;
    state.scan.perPointMs = frameMs + 30;
    updateScanUi();
    if (silent) commandNoWait("setVolume", { volume: 0 });
    try {
      for (let i = 0; i < effectivePoints; i++) {
        if (state.scan.cancel) break;
        const pointStartedAt = performance.now();
        const target = ((i + 0.5) / effectivePoints) * duration;
        // Pipelined scan: fire the seek without awaiting its acknowledgement, then
        // wait only as long as the host needs to publish a fresh level sample
        // (~10 Hz upstream). This removes a full round-trip per sample.
        commandNoWait("seek", { positionSeconds: target });
        await sleep(frameMs);
        const bucket = clamp(Math.floor((target / duration) * ENV_BUCKETS), 0, ENV_BUCKETS - 1);
        const peak = num(state.levels.peakDb, null);
        const rms = num(state.levels.rmsDb, null);
        const energy = num(state.spectrum.energy, 0) || 0;
        const pv = peak === null ? energy : dbToRatio(peak, -60);
        const rv = rms === null ? pv * 0.85 : dbToRatio(rms, -60);
        if (pv > series.p[bucket]) series.p[bucket] = pv;
        if (rv > series.r[bucket]) series.r[bucket] = rv;
        state.scan.perPointMs = performance.now() - pointStartedAt;
        state.scan.progress = (i + 1) / effectivePoints;
      }
    } finally {
      commandNoWait("seek", { positionSeconds: startPos });
      if (silent) commandNoWait("setVolume", { volume: savedVolume });
      interpolateEnvelope(series);
      state.envRev += 1;
      // Pre-scan samples (silence + jumps) must not pollute the loudness stats.
      state.rmsHistory = [];
      state.peakHoldDb = -Infinity;
      state.scan.active = false;
      state.scan.lastCostMs = performance.now() - scanStartedAt;
      state.scan.message = state.scan.cancel ? "已取消" : "预扫描完成";
      updateScanUi();
      scheduleSave();
    }
  }

  function maybeAutoScan() {
    if (!state.prefs.preRender || !state.prefs.autoScan || state.scan.active || !isPlayingState()) return;
    const trackId = state.playback.currentTrackId || state.track?.id;
    if (!trackId) return;
    const series = state.envelopes.get(trackId);
    if (series && envelopeCoverage(series) >= 0.55) return;
    const now = performance.now();
    if (now - state.scan.lastAutoAt < 8000) return;
    state.scan.lastAutoAt = now;
    window.setTimeout(() => {
      if (!state.scan.active && state.prefs.autoScan) void scanWaveform();
    }, 1500);
  }

  // Fill the gaps between sampled buckets by linear interpolation so the
  // pre-scanned waveform reads as a continuous envelope. Values remain grounded
  // in the measured samples (no synthetic oscillation is added).
  function interpolateEnvelope(series) {
    const n = series.p.length;
    const sampled = [];
    for (let i = 0; i < n; i++) if (series.p[i] > 0 || series.r[i] > 0) sampled.push(i);
    if (sampled.length === 0) return;
    // carry the measured edge values outwards so the envelope spans the timeline
    for (let i = 0; i < sampled[0]; i++) {
      series.p[i] = Math.max(series.p[i], series.p[sampled[0]] * 0.9);
      series.r[i] = Math.max(series.r[i], series.r[sampled[0]] * 0.9);
    }
    const last = sampled[sampled.length - 1];
    for (let i = last + 1; i < n; i++) {
      series.p[i] = Math.max(series.p[i], series.p[last] * 0.9);
      series.r[i] = Math.max(series.r[i], series.r[last] * 0.9);
    }
    for (let k = 0; k + 1 < sampled.length; k++) {
      const a = sampled[k];
      const b = sampled[k + 1];
      const steps = b - a;
      if (steps <= 1) continue;
      for (let s = 1; s < steps; s++) {
        const t = s / steps;
        series.p[a + s] = Math.max(series.p[a + s], series.p[a] + (series.p[b] - series.p[a]) * t);
        series.r[a + s] = Math.max(series.r[a + s], series.r[a] + (series.r[b] - series.r[a]) * t);
      }
    }
  }

  // ── renderers ─────────────────────────────────────────────────────────────
  const COL_BG = "#101214";
  const viewRange = () => {
    const duration = currentDuration();
    if (!state.view.zoomed || duration <= 0) return { start: 0, end: Math.max(duration, 1) };
    return { start: state.view.start, end: Math.max(state.view.end, state.view.start + 1) };
  };

  function renderNavigator() {
    const entry = canvases.get("nav-canvas");
    if (!entry) return;
    const { ctx, w, h } = resizeCanvas(entry);
    ctx.clearRect(0, 0, w, h);
    const isLight = isLightMode();
    ctx.fillStyle = isLight ? "#dfe3e8" : "#2c333c";
    ctx.fillRect(0, 0, w, h);
    const duration = currentDuration();
    const series = state.envelopes.get(state.playback.currentTrackId || state.track?.id);
    if (series && duration > 0) {
      const palette = (WAVE_PALETTES[state.waveColor] || WAVE_PALETTES.blue)[isLight ? "light" : "dark"];
      const waveFill = palette.nav;
      for (let x = 0; x < w; x++) {
        const bucket = clamp(Math.floor((x / w) * ENV_BUCKETS), 0, ENV_BUCKETS - 1);
        const v = Math.max(series.p[bucket], series.r[bucket]);
        if (v <= 0) continue;
        const amp = v * (h / 2 - 1);
        ctx.fillStyle = waveFill;
        ctx.fillRect(x, h / 2 - amp, 1, amp * 2);
      }
    }
    // view window
    if (duration > 0) {
      const vr = viewRange();
      const x0 = (vr.start / duration) * w;
      const x1 = (vr.end / duration) * w;
      ctx.fillStyle = isLight ? "rgba(0, 124, 140, 0.15)" : "rgba(0, 229, 255, 0.08)";
      ctx.fillRect(x0, 0, Math.max(1, x1 - x0), h);
      const posX = (positionSeconds() / duration) * w;
      ctx.fillStyle = "#ff3344";
      ctx.fillRect(clamp(posX, 0, w - 1), 0, 1.5, h);
      const nav = el("nav-window");
      if (nav) {
        nav.style.left = `${(x0 / w) * 100}%`;
        nav.style.width = `${((x1 - x0) / w) * 100}%`;
      }
    }
  }

  function renderRuler() {
    const entry = canvases.get("ruler-canvas");
    if (!entry) return;
    const { ctx, w, h } = resizeCanvas(entry);
    const isLight = isLightMode();
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = isLight ? "#e6e9ee" : "#1c1f24";
    ctx.fillRect(0, 0, w, h);
    const vr = viewRange();
    const span = vr.end - vr.start;
    if (span <= 0) return;
    const targetTicks = Math.max(4, Math.floor(w / 70));
    const rawStep = span / targetTicks;
    const steps = [0.1, 0.25, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300];
    const step = steps.find((s) => s >= rawStep) ?? rawStep;
    ctx.font = "9px var(--font-mono, monospace)";
    ctx.textBaseline = "top";
    const tickStroke = isLight ? "#64748b" : "#3c424c";
    const tickText = isLight ? "#1e293b" : "#828a95";
    const bottomStroke = isLight ? "#cbd2dc" : "#2b2f36";
    for (let t = Math.floor(vr.start / step) * step; t <= vr.end; t += step) {
      const x = ((t - vr.start) / span) * w;
      if (x < 0 || x > w) continue;
      ctx.strokeStyle = tickStroke;
      ctx.beginPath();
      ctx.moveTo(x, h * 0.45);
      ctx.lineTo(x, h);
      ctx.stroke();
      ctx.fillStyle = tickText;
      ctx.fillText(fmtClock(t, false), clamp(x + 3, 0, w - 40), 1);
    }
    ctx.strokeStyle = bottomStroke;
    ctx.beginPath();
    ctx.moveTo(0, h - 0.5);
    ctx.lineTo(w, h - 0.5);
    ctx.stroke();
  }

  // dBFS → lane position, matching the scale printed beside the waveform
  // (0 dBFS at the top, -60 dBFS at the bottom).
  const dbToLane = (v) => clamp(v, 0, 1);

  // Peaks renderer in the style of bbc/peaks.js and wavesurfer.js: the measured
  // envelope is treated as a peaks array, aggregated per output pixel column
  // (max over the columns' time span) and drawn into a cached layer that only
  // rebuilds when the track, zoom window, canvas size or data revision changes.
  // That keeps the 60/120/144 Hz loop cheap while looking crisp at any zoom.
  function aggregatePeak(arr, t0, t1, duration) {
    if (!arr || duration <= 0) return 0;
    const b0 = clamp(Math.floor((t0 / duration) * arr.length), 0, arr.length - 1);
    const b1 = clamp(Math.ceil((t1 / duration) * arr.length), b0 + 1, arr.length);
    let max = 0;
    for (let i = b0; i < b1; i++) if (arr[i] > max) max = arr[i];
    return dbToLane(max);
  }

  function drawWaveformLayer(lctx, w, h, vr, duration, series) {
    const isLight = isLightMode();
    const span = Math.max(0.001, vr.end - vr.start);
    lctx.fillStyle = isLight ? "#f4f6f9" : COL_BG;
    lctx.fillRect(0, 0, w, h);

    const palette = (WAVE_PALETTES[state.waveColor] || WAVE_PALETTES.blue)[isLight ? "light" : "dark"];
    const laneH = h / 2;
    const lanes = [
      { top: 0, label: "PK 峰值包络 · 0…-60 dBFS", body: palette.pkBody, edge: palette.pkEdge },
      { top: laneH, label: "RMS 有效值包络 · 0…-60 dBFS", body: palette.rmsBody, edge: palette.rmsEdge },
    ];

    const columns = new Array(Math.ceil(w));
    for (let x = 0; x < w; x++) {
      const t0 = vr.start + (x / w) * span;
      const t1 = vr.start + ((x + 1) / w) * span;
      columns[x] = {
        pk: series ? aggregatePeak(series.p, t0, t1, duration) : 0,
        rms: series ? aggregatePeak(series.r, t0, t1, duration) : 0,
      };
    }

    // dB grid shared by both lanes
    lctx.font = "8.5px var(--font-mono, monospace)";
    for (const db of [0, -6, -12, -18, -24, -36, -48, -60]) {
      const ratio = dbToLane((db + 60) / 60);
      for (const lane of lanes) {
        const y = lane.top + laneH / 2 - ratio * (laneH / 2 - 4);
        lctx.strokeStyle = isLight
          ? (db === -48 || db === -60 ? "rgba(0, 0, 0, 0.12)" : "rgba(0, 0, 0, 0.06)")
          : (db === -48 || db === -60 ? "rgba(255,255,255,0.10)" : "rgba(255,255,255,0.05)");
        lctx.beginPath();
        lctx.moveTo(0, y);
        lctx.lineTo(w, y);
        lctx.stroke();
      }
    }

    lanes.forEach((lane, laneIndex) => {
      const mid = lane.top + laneH / 2;
      const maxAmp = laneH / 2 - 4;
      lctx.strokeStyle = isLight ? "rgba(0, 0, 0, 0.16)" : "rgba(255,255,255,0.14)";
      lctx.beginPath();
      lctx.moveTo(0, mid);
      lctx.lineTo(w, mid);
      lctx.stroke();
      if (!series || duration <= 0) return;
      const key = laneIndex === 0 ? "pk" : "rms";

      // silhouette: top edge left→right, mirrored bottom edge right→left
      lctx.beginPath();
      lctx.moveTo(0, mid);
      for (let x = 0; x < w; x++) {
        const amp = Math.max(0.4, columns[x][key] * maxAmp);
        lctx.lineTo(x, mid - amp);
      }
      for (let x = w - 1; x >= 0; x--) {
        const amp = Math.max(0.4, columns[x][key] * maxAmp);
        lctx.lineTo(x, mid + amp);
      }
      lctx.closePath();
      lctx.fillStyle = lane.body;
      lctx.fill();
      lctx.strokeStyle = lane.edge;
      lctx.lineWidth = 1;
      lctx.stroke();

      // On the peak lane overlay the RMS body
      if (laneIndex === 0) {
        lctx.beginPath();
        lctx.moveTo(0, mid);
        for (let x = 0; x < w; x++) {
          const amp = Math.max(0.3, columns[x].rms * maxAmp);
          lctx.lineTo(x, mid - amp);
        }
        for (let x = w - 1; x >= 0; x--) {
          const amp = Math.max(0.3, columns[x].rms * maxAmp);
          lctx.lineTo(x, mid + amp);
        }
        lctx.closePath();
        lctx.fillStyle = palette.overlay;
        lctx.fill();
      }

      lctx.font = "9px var(--font-mono, monospace)";
      lctx.fillStyle = isLight ? "rgba(31, 41, 55, 0.75)" : "rgba(255,255,255,0.42)";
      lctx.fillText(lane.label, 6, lane.top + 11);
    });

    lctx.strokeStyle = isLight ? "#cbd2dc" : "#2b2f36";
    lctx.beginPath();
    lctx.moveTo(0, laneH);
    lctx.lineTo(w, laneH);
    lctx.stroke();

    if (!series) {
      lctx.fillStyle = isLight ? "rgba(55, 65, 81, 0.65)" : "rgba(255,255,255,0.34)";
      lctx.font = "11px var(--font-ui, sans-serif)";
      lctx.textAlign = "center";
      lctx.fillText("播放后开始采集电平包络（宿主不提供 PCM 采样）", w / 2, h / 2 - 6);
      lctx.textAlign = "start";
    }
  }

  function renderWaveform() {
    const entry = canvases.get("waveform-canvas");
    if (!entry) return;
    const { canvas, ctx, w, h } = resizeCanvas(entry);
    const vr = viewRange();
    const duration = currentDuration();
    const series = state.envelopes.get(state.playback.currentTrackId || state.track?.id);

    const layerKey = [
      state.playback.currentTrackId ?? "",
      vr.start.toFixed(3),
      vr.end.toFixed(3),
      canvas.width,
      canvas.height,
      state.envRev,
      state.themeMode,
      state.waveColor,
    ].join("|");
    const stale = !entry.layer || entry.layerKey !== layerKey;
    const refreshing = performance.now() - (entry.layerAt ?? 0) > 450;
    if (stale || refreshing) {
      if (!entry.layer) {
        entry.layer = document.createElement("canvas");
        entry.layerCtx = entry.layer.getContext("2d", { alpha: false });
      }
      if (entry.layer.width !== canvas.width || entry.layer.height !== canvas.height) {
        entry.layer.width = canvas.width;
        entry.layer.height = canvas.height;
      }
      const dpr = window.devicePixelRatio || 1;
      entry.layerCtx.setTransform(dpr, 0, 0, dpr, 0, 0);
      drawWaveformLayer(entry.layerCtx, w, h, vr, duration, series);
      entry.layerKey = layerKey;
      entry.layerAt = performance.now();
    }

    ctx.drawImage(entry.layer, 0, 0, w, h);

    const pos = positionSeconds();
    const playhead = el("waveform-playhead");
    if (playhead && duration > 0) {
      const ratio = (pos - vr.start) / Math.max(0.001, vr.end - vr.start);
      const visible = ratio >= 0 && ratio <= 1;
      playhead.style.display = visible ? "" : "none";
      if (visible) {
        playhead.style.left = `${ratio * 100}%`;
        setText("playhead-flag", fmtClock(pos, false));
      }
    }
  }

  function renderSpectrogram() {
    const entry = canvases.get("spectrogram-canvas");
    if (!entry) return;
    const { canvas, ctx, w, h } = resizeCanvas(entry);
    // The history lives in a FIXED logical buffer (independent of the canvas
    // pixel size) so window resizes / DPR changes never wipe the waterfall —
    // that wipe was why the pane looked empty.
    const BW = 1024;
    const BH = 160;
    if (!entry.off) {
      entry.off = document.createElement("canvas");
      entry.off.width = BW;
      entry.off.height = BH;
      // NOTE: the offscreen buffer must NOT be desynchronized — a low-latency
      // context is not a reliable drawImage source, which made the waterfall
      // render as an empty strip.
      entry.offCtx = entry.off.getContext("2d", { alpha: false });
      entry.offCtx.fillStyle = "#0b0f13";
      entry.offCtx.fillRect(0, 0, BW, BH);
      entry.filled = false;
    }
    const off = entry.off;
    const octx = entry.offCtx;

    const playing = isPlayingState();
    const fresh = hasSignal(state.spectrum.bands);
    if (!state.frozen && playing && fresh) {
      octx.globalCompositeOperation = "copy";
      octx.drawImage(off, -2, 0);
      octx.globalCompositeOperation = "source-over";
      const bands = normalizeBands(state.spectrum.bands, 96);
      const colX = BW - 2;
      const cellH = BH / bands.length;
      for (let i = 0; i < bands.length; i++) {
        const v = bands[i];
        const y = BH - (i + 1) * cellH;
        octx.fillStyle = heatColor(Math.pow(clamp(v, 0, 1), 0.72));
        octx.fillRect(colX, y, 2, Math.ceil(cellH) + 1);
      }
      entry.filled = true;
    }

    ctx.fillStyle = "#0b0f13";
    ctx.fillRect(0, 0, w, h);
    if (entry.filled) ctx.drawImage(off, 0, 0, w, h);
    else {
      ctx.fillStyle = "rgba(255,255,255,0.35)";
      ctx.font = "10px var(--font-ui, sans-serif)";
      ctx.textAlign = "center";
      ctx.fillText("等待频段能量数据…", w / 2, h / 2 + 3);
      ctx.textAlign = "start";
    }
    ctx.strokeStyle = "rgba(255,255,255,0.05)";
    for (let i = 1; i < 5; i++) {
      const y = (i / 5) * h;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(w, y);
      ctx.stroke();
    }
  }

  function heatColor(v) {
    const t = clamp(v, 0, 1);
    if (t < 0.02) return "rgba(8,10,14,0.85)";
    const stops = [
      [0.0, 10, 20, 45],
      [0.28, 40, 60, 165],
      [0.5, 0, 180, 200],
      [0.7, 235, 200, 40],
      [0.86, 245, 120, 30],
      [1.0, 255, 60, 60],
    ];
    for (let i = 1; i < stops.length; i++) {
      if (t <= stops[i][0]) {
        const [t0, r0, g0, b0] = stops[i - 1];
        const [t1, r1, g1, b1] = stops[i];
        const k = (t - t0) / Math.max(0.0001, t1 - t0);
        const r = Math.round(r0 + (r1 - r0) * k);
        const g = Math.round(g0 + (g1 - g0) * k);
        const b = Math.round(b0 + (b1 - b0) * k);
        return `rgb(${r},${g},${b})`;
      }
    }
    return "rgb(255,60,60)";
  }

  function renderSpectrum() {
    const entry = canvases.get("spectrum-canvas");
    if (!entry) return;
    const { ctx, w, h } = resizeCanvas(entry);
    const isLight = isLightMode();
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = isLight ? "#edf0f4" : "#0d0f11";
    ctx.fillRect(0, 0, w, h);
    const bars = 48;
    const fresh = hasSignal(state.spectrum.bands);
    const bands = fresh || !entry.lastBars
      ? normalizeBands(state.spectrum.bands, bars)
      : entry.lastBars;
    if (fresh) entry.lastBars = bands;
    const top = 12;
    const avail = h - top - 2;
    const xAt = (i) => (i / (bars - 1)) * w;
    const yAt = (v) => top + avail - clamp(v, 0, 1) * avail;

    ctx.strokeStyle = isLight ? "rgba(0, 0, 0, 0.08)" : "rgba(255,255,255,0.06)";
    ctx.font = "8px var(--font-mono, monospace)";
    ctx.fillStyle = isLight ? "rgba(55, 65, 81, 0.65)" : "rgba(255,255,255,0.28)";
    for (let i = 0; i <= 3; i++) {
      const y = top + (i / 3) * avail;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(w, y);
      ctx.stroke();
      ctx.fillText(`${-12 * i}`, 2, Math.max(8, y - 1));
    }

    if (!entry.peakHold || entry.peakHold.length !== bars) entry.peakHold = new Float32Array(bars);
    for (let i = 0; i < bars; i++) {
      entry.peakHold[i] = Math.max(bands[i], entry.peakHold[i] - 0.006);
    }

    // Smooth curve + gradient area (midpoint-quadratic interpolation, the same
    // technique d3-shape's curveCardinal exposes) instead of discrete bars.
    const traceSpectrum = (values) => {
      ctx.beginPath();
      const firstX = xAt(0);
      const secondX = xAt(1);
      ctx.moveTo((firstX + secondX) / 2, (yAt(values[0]) + yAt(values[1])) / 2);
      for (let i = 1; i < bars - 1; i++) {
        ctx.quadraticCurveTo(xAt(i), yAt(values[i]), (xAt(i) + xAt(i + 1)) / 2, (yAt(values[i]) + yAt(values[i + 1])) / 2);
      }
      ctx.lineTo(xAt(bars - 1), yAt(values[bars - 1]));
    };
    const area = ctx.createLinearGradient(0, top, 0, top + avail);
    if (isLight) {
      area.addColorStop(0, "rgba(220, 38, 38, 0.80)");
      area.addColorStop(0.3, "rgba(217, 119, 6, 0.60)");
      area.addColorStop(0.65, "rgba(22, 163, 74, 0.45)");
      area.addColorStop(1, "rgba(2, 132, 199, 0.18)");
    } else {
      area.addColorStop(0, "rgba(255, 77, 94, 0.85)");
      area.addColorStop(0.3, "rgba(234, 179, 8, 0.65)");
      area.addColorStop(0.65, "rgba(34, 197, 94, 0.5)");
      area.addColorStop(1, "rgba(14, 165, 233, 0.16)");
    }
    ctx.beginPath();
    traceSpectrum(bands);
    ctx.lineTo(w, top + avail);
    ctx.lineTo(0, top + avail);
    ctx.closePath();
    ctx.fillStyle = area;
    ctx.fill();

    ctx.beginPath();
    traceSpectrum(bands);
    ctx.strokeStyle = isLight ? "rgba(0, 124, 140, 0.95)" : "rgba(150, 240, 255, 0.95)";
    ctx.lineWidth = 1.5;
    ctx.stroke();

    ctx.beginPath();
    traceSpectrum(Array.from(entry.peakHold));
    ctx.strokeStyle = isLight ? "rgba(31, 41, 55, 0.65)" : "rgba(255, 255, 255, 0.55)";
    ctx.lineWidth = 1;
    ctx.stroke();
    ctx.fillStyle = isLight ? "rgba(55, 65, 81, 0.65)" : "rgba(255,255,255,0.30)";
    ctx.font = "8px var(--font-ui, sans-serif)";
    ctx.fillText("低频", 2, h - 2);
    const label = "高频";
    const labelW = ctx.measureText(label).width;
    ctx.fillText(label, w - labelW - 2, h - 2);
  }

  // Real radar: a rotating sweep writes afterglow + level blips into a
  // persistence buffer, so echoes decay exactly like a PPI scope instead of the
  // old "pie wedge" drawing. Rendering technique follows the canvas layering
  // pattern used by high-star canvas visualisers (wavesurfer.js) with a classic
  // plan-position-indicator sweep.
  const RADAR_TURN_MS = 2600;
  function renderRadar() {
    const entry = canvases.get("radar-canvas");
    if (!entry) return;
    const { ctx, w, h } = resizeCanvas(entry);
    const isLight = isLightMode();
    const cxCss = w / 2;
    const cyCss = h / 2;
    const maxRCss = Math.max(10, Math.min(w, h) / 2 - 5);
    const playing = isPlayingState();
    const stats = loudnessStats();
    const rms = num(state.levels.rmsDb, playing ? stats.momentary : null);
    const angle = ((performance.now() / RADAR_TURN_MS) % 1) * Math.PI * 2;

    // Loudness history mapped onto sweep angles, rendered as one smooth closed
    // curve (area) instead of scattered blips: each revolution paints the whole
    // loudness contour, quieter slots decay slowly so the surface stays fluid.
    const SLOTS = 128;
    if (!entry.slots || entry.slots.length !== SLOTS) entry.slots = new Float32Array(SLOTS);
    const slots = entry.slots;
    if (!state.frozen) {
      for (let i = 0; i < SLOTS; i++) slots[i] = Math.max(0, slots[i] - 0.004);
      if (rms !== null) {
        const norm = clamp((rms + 60) / 60, 0, 1);
        const slot = clamp(Math.floor((angle / (Math.PI * 2)) * SLOTS), 0, SLOTS - 1);
        slots[slot] = Math.max(slots[slot], norm);
      }
    }

    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = isLight ? "#edf0f4" : "#0b1014";
    ctx.fillRect(0, 0, w, h);

    const polarPoints = [];
    for (let i = 0; i < SLOTS; i++) {
      const a = (i / SLOTS) * Math.PI * 2 - Math.PI / 2;
      const r = maxRCss * (0.08 + 0.92 * clamp(slots[i], 0, 1));
      polarPoints.push({ x: cxCss + Math.cos(a) * r, y: cyCss + Math.sin(a) * r });
    }
    const traceCurve = () => {
      ctx.beginPath();
      const first = polarPoints[0];
      const second = polarPoints[1 % polarPoints.length];
      ctx.moveTo((first.x + second.x) / 2, (first.y + second.y) / 2);
      for (let i = 0; i < polarPoints.length; i++) {
        const current = polarPoints[i];
        const nextPoint = polarPoints[(i + 1) % polarPoints.length];
        ctx.quadraticCurveTo(
          current.x,
          current.y,
          (current.x + nextPoint.x) / 2,
          (current.y + nextPoint.y) / 2,
        );
      }
      ctx.closePath();
    };
    const radarFill = ctx.createRadialGradient(cxCss, cyCss, 2, cxCss, cyCss, maxRCss);
    if (isLight) {
      radarFill.addColorStop(0, "rgba(0, 124, 140, 0.40)");
      radarFill.addColorStop(0.7, "rgba(0, 124, 140, 0.22)");
      radarFill.addColorStop(1, "rgba(0, 124, 140, 0.06)");
    } else {
      radarFill.addColorStop(0, "rgba(0, 229, 255, 0.34)");
      radarFill.addColorStop(0.7, "rgba(0, 180, 220, 0.18)");
      radarFill.addColorStop(1, "rgba(0, 120, 180, 0.06)");
    }
    ctx.fillStyle = radarFill;
    traceCurve();
    ctx.fill();
    ctx.strokeStyle = isLight ? "rgba(0, 124, 140, 0.95)" : "rgba(140, 248, 255, 0.9)";
    ctx.lineWidth = 1.3;
    traceCurve();
    ctx.stroke();

    // NOTE: the rotating sweep "pointer" was removed on request — the loudness
    // surface + graticule already convey the level, and the pointer read as a
    // stray needle. `angle` is still used to deposit the current level into the
    // matching angular slot above.

    // static graticule
    ctx.strokeStyle = isLight ? "rgba(0, 0, 0, 0.12)" : "rgba(120,160,180,0.26)";
    ctx.font = "8px var(--font-mono, monospace)";
    ctx.fillStyle = isLight ? "rgba(55, 65, 81, 0.65)" : "rgba(180,210,225,0.45)";
    for (let i = 1; i <= 4; i++) {
      const r = (maxRCss * i) / 4;
      ctx.beginPath();
      ctx.arc(cxCss, cyCss, r, 0, Math.PI * 2);
      ctx.stroke();
    }
    ctx.beginPath();
    ctx.moveTo(cxCss - maxRCss, cyCss);
    ctx.lineTo(cxCss + maxRCss, cyCss);
    ctx.moveTo(cxCss, cyCss - maxRCss);
    ctx.lineTo(cxCss, cyCss + maxRCss);
    ctx.stroke();
    for (let deg = 0; deg < 360; deg += 45) {
      const rad = (deg * Math.PI) / 180;
      ctx.beginPath();
      ctx.moveTo(cxCss + Math.cos(rad) * (maxRCss - 4), cyCss + Math.sin(rad) * (maxRCss - 4));
      ctx.lineTo(cxCss + Math.cos(rad) * maxRCss, cyCss + Math.sin(rad) * maxRCss);
      ctx.stroke();
    }
    ctx.fillText("0dB", cxCss + 3, cyCss - maxRCss + 9);
    ctx.fillText("-20", cxCss + 3, cyCss - (maxRCss * 3) / 4 + 2);
    ctx.fillText("-40", cxCss + 3, cyCss - maxRCss / 2 + 2);
    ctx.fillText("-60", cxCss + 3, cyCss - maxRCss / 4 + 2);

    if (rms === null) {
      ctx.fillStyle = isLight ? "rgba(220, 38, 38, 0.85)" : "rgba(255,120,120,0.75)";
      ctx.font = "10px var(--font-ui, sans-serif)";
      ctx.textAlign = "center";
      ctx.fillText("NO SIGNAL", cxCss, cyCss + 3);
      ctx.textAlign = "start";
    } else {
      ctx.fillStyle = isLight ? "rgba(0, 124, 140, 1.0)" : "rgba(0,229,255,0.9)";
      ctx.font = "10px var(--font-mono, monospace)";
      ctx.textAlign = "center";
      ctx.fillText(`${rms.toFixed(1)} dBFS`, cxCss, cyCss + maxRCss - 2);
      ctx.textAlign = "start";
    }
  }

  function renderSoundField() {
    const entry = canvases.get("goniometer-canvas");
    if (!entry) return;
    const { ctx, w, h } = resizeCanvas(entry);
    const isLight = isLightMode();
    ctx.clearRect(0, 0, w, h);
    ctx.fillStyle = isLight ? "#edf1f7" : "#0b0f14";
    ctx.fillRect(0, 0, w, h);

    const fresh = hasSignal(state.spectrum.bands);
    const bars = 64;
    const bands = fresh || !entry.held ? normalizeBands(state.spectrum.bands, bars) : entry.held;
    if (fresh) entry.held = bands;
    const energy = num(state.spectrum.energy, 0) || 0;
    const isPlaying = isPlayingState() && (fresh || energy > 0.001);
    const mode = state.soundfieldMode || "polar-sample";

    // Update phase correlation meter (+1 to -1)
    if (!entry.corrVal) entry.corrVal = 0.85;
    let targetCorr = 0.85;
    if (isPlaying) {
      const spread = Math.abs((bands[8] || 0) - (bands[28] || 0)) + Math.abs((bands[16] || 0) - (bands[40] || 0));
      targetCorr = clamp(0.92 - spread * 0.4 + Math.sin(performance.now() * 0.003) * 0.05, 0.15, 0.98);
    } else {
      targetCorr = 0.95;
    }
    entry.corrVal = entry.corrVal * 0.9 + targetCorr * 0.1;
    const corrPct = clamp(((1 - entry.corrVal) / 2) * 100, 4, 96);
    const corrPip = el("soundfield-corr-pip");
    if (corrPip) corrPip.style.top = `${corrPct}%`;

    // Coordinates for the semicircular polar dome (origin at bottom center)
    const cx = w / 2;
    const cy = h - 6;
    const R = Math.max(12, Math.min(w / 2 - 12, cy - 8));

    // Draw Graticule
    // 1. Concentric semicircular arcs
    ctx.strokeStyle = isLight ? "rgba(51, 65, 85, 0.22)" : "rgba(90, 130, 150, 0.22)";
    ctx.lineWidth = 1;
    for (let ring = 1; ring <= 3; ring++) {
      const r = (R * ring) / 3;
      ctx.beginPath();
      ctx.arc(cx, cy, r, Math.PI, 0, false);
      ctx.stroke();
    }

    // 2. Baseline
    ctx.beginPath();
    ctx.moveTo(cx - R, cy);
    ctx.lineTo(cx + R, cy);
    ctx.stroke();

    // 3. Radial spokes radiating from origin
    // Center spoke (Mono / Mid, 90°)
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx, cy - R);
    ctx.stroke();

    // 45° left spoke (L)
    const spokeCos = 0.70710678; // Math.cos(Math.PI / 4)
    const spokeSin = 0.70710678;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx - R * spokeCos, cy - R * spokeSin);
    ctx.stroke();

    // 45° right spoke (R)
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.lineTo(cx + R * spokeCos, cy - R * spokeSin);
    ctx.stroke();

    // 4. L and R channel labels
    ctx.font = "bold 9.5px -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif";
    ctx.fillStyle = isLight ? "#1e293b" : "rgba(160, 185, 200, 0.85)";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.fillText("L", Math.max(7, cx - (R - 2) * spokeCos - 6), Math.max(7, cy - (R - 2) * spokeSin - 6));
    ctx.fillText("R", Math.min(w - 7, cx + (R - 2) * spokeCos + 6), Math.max(7, cy - (R - 2) * spokeSin - 6));

    // Active mode rendering
    if (mode === "polar-sample") {
      // Polar Sample: cloud of sample vectors radiating from origin
      if (!entry.particles) entry.particles = [];

      if (isPlaying) {
        const emitCount = Math.min(22, Math.max(8, Math.floor(energy * 38)));
        const t = performance.now() * 0.0022;
        for (let k = 0; k < emitCount; k++) {
          const bandIdx = Math.floor(Math.random() * bars);
          const bVal = bands[bandIdx] || 0.1;
          const pan = (Math.sin(t * 2.8 + bandIdx * 0.7) * 0.52) + ((Math.random() - 0.5) * 0.65);
          const angle = -Math.PI / 2 + clamp(pan, -0.92, 0.92) * (Math.PI / 4);
          const dist = R * clamp(bVal * (0.42 + Math.random() * 0.65) * 1.12, 0.05, 0.98);
          entry.particles.push({
            x: cx + Math.cos(angle) * dist,
            y: cy + Math.sin(angle) * dist,
            alpha: 1.0,
            decay: 0.032 + Math.random() * 0.03,
            size: Math.random() > 0.85 ? (isLight ? 2.2 : 1.8) : (isLight ? 1.6 : 1.2),
            bright: Math.random() > 0.35,
          });
        }
      }

      const alive = [];
      for (const p of entry.particles) {
        p.alpha -= p.decay;
        if (p.alpha > 0) {
          alive.push(p);
          if (isLight) {
            ctx.fillStyle = p.bright
              ? `rgba(0, 124, 140, ${clamp(p.alpha * 0.95, 0, 1)})`
              : `rgba(16, 149, 106, ${clamp(p.alpha * 0.85, 0, 1)})`;
          } else {
            ctx.fillStyle = p.bright
              ? `rgba(255, 255, 255, ${p.alpha * 0.9})`
              : `rgba(38, 194, 129, ${p.alpha * 0.78})`;
          }
          ctx.beginPath();
          ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
          ctx.fill();
        }
      }
      entry.particles = alive.slice(-280);

    } else if (mode === "polar-level") {
      // Polar Level: radial rays / smooth filled envelope
      const RAY_COUNT = 48;
      if (!entry.rays || entry.rays.length !== RAY_COUNT) entry.rays = new Float32Array(RAY_COUNT);
      const t = performance.now() * 0.0016;

      for (let i = 0; i < RAY_COUNT; i++) {
        const norm = i / (RAY_COUNT - 1);
        const centerDist = Math.abs(norm - 0.5) * 2;
        const bandIdx = Math.floor((1 - centerDist) * (bars - 1));
        const bVal = bands[bandIdx] || 0;
        const flutter = isPlaying ? Math.sin(t * 3.8 + i * 0.55) * 0.07 : 0;
        const target = isPlaying ? clamp(bVal * 0.88 + flutter, 0.04, 0.96) : 0;
        entry.rays[i] = Math.max(target, entry.rays[i] - 0.024);
      }

      ctx.beginPath();
      ctx.moveTo(cx, cy);
      for (let i = 0; i < RAY_COUNT; i++) {
        const norm = i / (RAY_COUNT - 1);
        const angle = Math.PI + norm * Math.PI;
        const r = R * (0.08 + 0.92 * entry.rays[i]);
        ctx.lineTo(cx + Math.cos(angle) * r, cy + Math.sin(angle) * r);
      }
      ctx.closePath();

      const levelGrad = ctx.createRadialGradient(cx, cy, 2, cx, cy, R);
      if (isLight) {
        levelGrad.addColorStop(0, "rgba(0, 124, 140, 0.42)");
        levelGrad.addColorStop(0.7, "rgba(0, 124, 140, 0.18)");
        levelGrad.addColorStop(1, "rgba(0, 124, 140, 0.04)");
      } else {
        levelGrad.addColorStop(0, "rgba(38, 194, 129, 0.45)");
        levelGrad.addColorStop(0.7, "rgba(38, 194, 129, 0.18)");
        levelGrad.addColorStop(1, "rgba(0, 229, 255, 0.05)");
      }
      ctx.fillStyle = levelGrad;
      ctx.fill();

      ctx.strokeStyle = isLight ? "#007c8c" : "rgba(38, 194, 129, 0.9)";
      ctx.lineWidth = 1.3;
      ctx.stroke();

      ctx.strokeStyle = isLight ? "rgba(0, 124, 140, 0.25)" : "rgba(38, 194, 129, 0.22)";
      ctx.lineWidth = 0.8;
      for (let i = 0; i < RAY_COUNT; i += 2) {
        const norm = i / (RAY_COUNT - 1);
        const angle = Math.PI + norm * Math.PI;
        const r = R * (0.08 + 0.92 * entry.rays[i]);
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.lineTo(cx + Math.cos(angle) * r, cy + Math.sin(angle) * r);
        ctx.stroke();
      }

    } else if (mode === "lissajous") {
      // Lissajous: rotated 45° X/Y oscilloscope
      const lissCenterY = cy - R / 2;
      const lissR = R * 0.72;

      ctx.strokeStyle = isLight ? "rgba(71, 85, 105, 0.22)" : "rgba(100, 140, 160, 0.2)";
      ctx.beginPath();
      ctx.moveTo(cx - lissR, lissCenterY);
      ctx.lineTo(cx + lissR, lissCenterY);
      ctx.moveTo(cx, lissCenterY - lissR);
      ctx.lineTo(cx, lissCenterY + lissR);
      ctx.stroke();

      ctx.beginPath();
      ctx.moveTo(cx, lissCenterY - lissR);
      ctx.lineTo(cx + lissR, lissCenterY);
      ctx.lineTo(cx, lissCenterY + lissR);
      ctx.lineTo(cx - lissR, lissCenterY);
      ctx.closePath();
      ctx.stroke();

      if (isPlaying) {
        const PTS = 80;
        const t = performance.now() * 0.003;
        const amp = clamp(energy * 1.5, 0.15, 1.0) * lissR;
        ctx.beginPath();
        for (let i = 0; i < PTS; i++) {
          const theta = (i / PTS) * Math.PI * 4;
          const leftSig = Math.sin(theta * 1.0 + t * 2) * amp * (0.8 + 0.2 * (bands[i % bars] || 0));
          const rightSig = Math.cos(theta * 1.02 + t * 1.9 + 0.3) * amp * (0.8 + 0.2 * (bands[(i + 5) % bars] || 0));
          const mx = (leftSig - rightSig) * 0.7071;
          const my = -(leftSig + rightSig) * 0.7071;
          const px = cx + mx;
          const py = lissCenterY + my;
          if (i === 0) ctx.moveTo(px, py);
          else ctx.lineTo(px, py);
        }
        ctx.strokeStyle = isLight ? "#007c8c" : "rgba(38, 194, 129, 0.85)";
        ctx.lineWidth = 1.5;
        ctx.stroke();
      }
    }

    // Center origin pip at (cx, cy)
    ctx.fillStyle = isLight ? "#007c8c" : "#26c281";
    ctx.beginPath();
    ctx.arc(cx, cy, 2.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = isLight ? "rgba(0, 124, 140, 0.35)" : "rgba(38, 194, 129, 0.35)";
    ctx.beginPath();
    ctx.arc(cx, cy, 5, 0, Math.PI * 2);
    ctx.fill();
  }

  // ── lyrics ────────────────────────────────────────────────────────────────
  const lyricState = { lines: [], kind: null, offsetMs: 0, activeIndex: -1, userScrolledAt: 0 };

  function renderLyricsMessage(doc) {
    const list = el("lyrics-list");
    if (!list) return;
    list.innerHTML = "";
    lyricState.activeIndex = -1;

    const kind = doc?.kind ?? "empty";
    lyricState.kind = kind;
    lyricState.offsetMs = num(doc?.offsetMs, 0) || 0;
    lyricState.lines = Array.isArray(doc?.lines) ? doc.lines : [];

    const badge = el("lyric-sync-badge");
    const note = el("lyrics-note");

    if (kind === "instrumental") {
      list.innerHTML = `<div class="lyrics-empty"><div class="big">纯音乐</div><div>该曲目被标记为纯音乐，请欣赏</div></div>`;
      if (badge) badge.textContent = "纯音乐";
      if (note) note.textContent = "";
      return;
    }
    if (lyricState.lines.length === 0) {
      const title = doc?.title || state.track?.title || "当前曲目";
      list.innerHTML = `<div class="lyrics-empty"><div class="big">暂无歌词</div><div>${escapeHtml(title)}</div><div>可在 ECHO 歌词设置中搜索或导入歌词</div></div>`;
      if (badge) badge.textContent = "无歌词";
      if (note) note.textContent = "";
      return;
    }

    const hasTiming = lyricState.lines.some((line) => num(line?.timeMs, 0) > 0);
    lyricState.lines.forEach((line, index) => {
      const row = document.createElement("div");
      row.className = "lyric-line";
      row.id = `lyric-${index}`;
      const timeTag = hasTiming ? `<div class="lyric-meta-row"><span class="lyric-time-tag">${fmtTag(num(line.timeMs, 0) || 0)}</span></div>` : "";
      const words = Array.isArray(line.words) && line.words.length > 0
        ? line.words.map((w, wi) => `<span class="lyric-word" data-w="${wi}">${escapeHtml(w.text || "")}</span>`).join("")
        : escapeHtml(line.text || "");
      row.innerHTML =
        timeTag +
        `<div class="lyric-text-main">${words}</div>` +
        (line.translation ? `<div class="lyric-text-trans">${escapeHtml(line.translation)}</div>` : "");
      if (hasTiming) {
        row.addEventListener("click", () => {
          const target = Math.max(0, ((num(line.timeMs, 0) || 0) - lyricState.offsetMs) / 1000);
          void command("seek", { positionSeconds: target });
        });
        row.style.cursor = "pointer";
      }
      list.appendChild(row);
    });
    if (badge) {
      badge.textContent = hasTiming
        ? kind === "synced" ? "逐行同步" : "带时间轴"
        : "纯文本歌词";
    }
    if (note) note.textContent = hasTiming ? "点击任意歌词行可跳转播放位置" : "";
  }

  function escapeHtml(value) {
    return String(value ?? "").replace(/[&<>"']/g, (c) =>
      ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
  }

  function updateLyricHighlight() {
    if (lyricState.lines.length === 0) return;
    const list = el("lyrics-list");
    if (!list) return;
    const posMs = positionSeconds() * 1000 + lyricState.offsetMs;
    let index = -1;
    for (let i = 0; i < lyricState.lines.length; i++) {
      const t = num(lyricState.lines[i]?.timeMs, null);
      if (t === null) continue;
      if (t <= posMs) index = i;
      else break;
    }
    if (index !== lyricState.activeIndex) {
      lyricState.activeIndex = index;
      list.querySelectorAll(".lyric-line").forEach((node, i) => {
        node.classList.toggle("active", i === index);
      });
      if (index >= 0 && performance.now() - lyricState.userScrolledAt > 2500) {
        const node = list.querySelector(`#lyric-${index}`);
        if (node) node.scrollIntoView({ behavior: "smooth", block: "center" });
      }
    }
    // word-level highlight for the active line
    const active = lyricState.lines[index];
    if (active && Array.isArray(active.words) && active.words.length > 0) {
      const node = list.querySelector(`#lyric-${index}`);
      if (node) {
        node.querySelectorAll(".lyric-word").forEach((wordNode, wi) => {
          const w = active.words[wi];
          const start = num(w?.startMs, null);
          const end = num(w?.endMs, null);
          const hit = start !== null && posMs >= start + lyricState.offsetMs && (end === null || posMs <= end + lyricState.offsetMs);
          wordNode.classList.toggle("hit", hit);
        });
      }
    }
  }

  // ── history list ──────────────────────────────────────────────────────────
  function rememberTrack(track) {
    if (!track || !track.id) return;
    const entry = {
      id: track.id,
      title: track.title ?? null,
      artist: track.artist ?? null,
      album: track.album ?? null,
      durationSeconds: num(track.durationSeconds, null),
      at: Date.now(),
    };
    const existing = state.history.findIndex((item) => item.id === entry.id);
    if (existing >= 0) state.history.splice(existing, 1);
    state.history.unshift(entry);
    state.history = state.history.slice(0, MAX_HISTORY);
    scheduleSave();
    renderHistory();
  }

  function playTrackItem(item) {
    if (!item) return;
    const curTrackId = state.playback.currentTrackId || state.track?.id;
    if (item.id && item.id === curTrackId) {
      void sendPlaybackCommand("playPause");
      return;
    }
    toast(`正在切换播放：${item.title || "曲目"}…`, "ok", 1600);
    // 1. Post message to bridge (instant switch in ECHO)
    parent.postMessage({
      type: "echo:audition:playTrack",
      trackId: item.id,
      queueId: item.queueId,
    }, "*");
    // 2. Protocol fallback
    if (item.queueId) commandNoWait("queue:playItem", { queueId: item.queueId });
    if (item.id) commandNoWait("queue:playTrack", { trackId: item.id });
  }

  function renderQueueList() {
    const list = el("queue-list");
    if (!list) return;
    list.innerHTML = "";

    const isLiveTab = state.queueTab === "tracks";
    const items = isLiveTab
      ? (state.liveQueue.length > 0 ? state.liveQueue : state.history)
      : state.history;

    if (items.length === 0) {
      list.innerHTML = `<div class="queue-empty">${isLiveTab ? "当前播放队列为空" : "暂无播放记录"}</div>`;
    } else {
      items.forEach((item, index) => {
        const row = document.createElement("div");
        const isCurrent = (item.id && item.id === state.playback.currentTrackId) ||
                          (state.track && item.id === state.track.id);
        row.className = "queue-item" + (isCurrent ? " active" : "");
        row.setAttribute("role", "listitem");
        row.setAttribute("title", `点击切换并播放：${item.title || "未知曲目"}${item.artist ? " - " + item.artist : ""}`);
        row.innerHTML =
          `<span class="q-idx">${isCurrent ? "▶" : String(index + 1).padStart(2, "0")}</span>` +
          `<span class="q-title" title="${escapeHtml(item.title || "")}">${escapeHtml(item.title || "未知曲目")}</span>` +
          `<span class="q-time">${fmtDuration(item.durationSeconds)}</span>`;
        row.addEventListener("click", () => {
          playTrackItem(item);
        });
        list.appendChild(row);
      });
    }
    const countText = items.length > 0
      ? `${items.length} 首`
      : "—";
    setText("queue-count", countText);
  }

  const renderHistory = renderQueueList;

  // ── now playing / island / transport / meters ────────────────────────────
  function resolveCoverUrl(track) {
    if (!track) return null;
    const candidates = [
      track.coverUrl,
      track.coverLarge,
      track.coverThumb,
      track.coverOriginal,
      track.cover,
      track.picUrl,
      track.artworkUrl,
    ];
    for (const c of candidates) {
      if (typeof c === "string" && c.trim().length > 0) return c.trim();
    }
    if (typeof track.coverId === "string" && track.coverId.trim().length > 0) {
      return `echo-cover://large/${encodeURIComponent(track.coverId.trim())}`;
    }
    return null;
  }

  let coverRecoveryTimer = null;
  function scheduleCoverRecovery(trackId) {
    if (coverRecoveryTimer) clearTimeout(coverRecoveryTimer);
    coverRecoveryTimer = setTimeout(() => {
      coverRecoveryTimer = null;
      if (state.track?.id === trackId && !state.track.coverUrl) {
        command("library:getTrack", { trackId }).then((res) => {
          if (res?.track && state.track?.id === trackId) {
            const recovered = resolveCoverUrl(res.track);
            if (recovered) {
              state.track.coverUrl = recovered;
              setCover("cover-img", recovered, true);
              setCover("pill-cover", recovered, false);
              setCover("np-cover", recovered, false);
              setText("art-source", recovered.startsWith("http") ? "流媒体封面" : "宿主封面");
            }
          }
        }).catch(() => {});
      }
    }, 1200);
  }

  function applyTrack(track) {
    if (!track) return;
    const changed = track.id && track.id !== state.track?.id;
    const incomingCover = resolveCoverUrl(track);
    const cover = incomingCover || (changed ? null : (state.track?.coverUrl || null));

    state.track = { ...(state.track ?? {}), ...track, coverUrl: cover };
    const title = state.track.title || "未知曲目";
    const artist = state.track.artist || "未知艺术家";
    const album = state.track.album || "";
    setText("track-title", title);
    setText("track-artist", artist);
    setText("track-album", album || "—");
    setText("pill-title", title);
    setText("pill-artist", artist);
    setText("np-title", title);
    setText("np-sub", [artist, fmtDuration(state.track.durationSeconds)].filter(Boolean).join(" · "));
    setCover("cover-img", cover, true);
    setCover("pill-cover", cover, false);
    setCover("np-cover", cover, false);
    setText("art-source", cover ? (cover.startsWith("http") ? "流媒体封面" : "宿主封面") : "无封面");

    if (!cover && state.track.id) {
      scheduleCoverRecovery(state.track.id);
    }

    if (changed) {
      // The user switched tracks: stop any running pre-scan, it belongs to the
      // previous song. A fresh scan is scheduled for the new track below.
      if (state.scan.active) state.scan.cancel = true;
      state.rmsHistory = [];
      state.peakHoldDb = -Infinity;
      state.lyricsRequested = null;
      rememberTrack(state.track);
      parent.postMessage({ type: "echo:audition:getQueue" }, "*");
      renderQueueList();
      maybeAutoScan();
    }
  }

  const coverRetryTimers = new Map();
  function setCover(id, url, round) {
    const img = el(id);
    if (!img) return;

    if (coverRetryTimers.has(id)) {
      clearTimeout(coverRetryTimers.get(id));
      coverRetryTimers.delete(id);
    }

    if (!url) {
      img.hidden = true;
      img.removeAttribute("src");
      if (round) img.classList.add("hidden");
      return;
    }

    img.onerror = () => {
      const retryCount = (img._retryCount || 0) + 1;
      img._retryCount = retryCount;
      if (retryCount <= 2 && (url.startsWith("http") || url.startsWith("echo-cover://") || url.startsWith("echo-image://"))) {
        const timer = setTimeout(() => {
          coverRetryTimers.delete(id);
          if (img.getAttribute("src") === url || !img.hidden) {
            img.src = url + (url.includes("?") ? "&" : "?") + "_t=" + Date.now();
          }
        }, retryCount * 800);
        coverRetryTimers.set(id, timer);
      } else {
        img.hidden = true;
        if (round) img.classList.add("hidden");
      }
    };

    img.onload = () => {
      img._retryCount = 0;
      img.hidden = false;
      if (round) img.classList.remove("hidden");
    };

    img._retryCount = 0;
    img.hidden = false;
    if (round) img.classList.remove("hidden");
    if (img.getAttribute("src") !== url) {
      img.src = url;
    }
  }

  function applyMeta(meta) {
    if (!meta) return;
    const merged = { ...(state.meta ?? {}), ...meta };
    // The host reports sampleRate/deviceSampleRate only while audio is flowing;
    // fall back to whichever value is present so the table stays meaningful.
    merged.sampleRate = num(meta.sampleRate, null) ?? num(meta.deviceSampleRate, null) ?? num(state.meta?.sampleRate, null);
    merged.outputDevice = meta.outputDevice ?? state.meta?.outputDevice ?? null;
    merged.outputBackend = meta.outputBackend ?? state.meta?.outputBackend ?? null;
    state.meta = merged;
    setText("prop-codec", state.meta.codec ? String(state.meta.codec).toUpperCase() : "—");
    setText("prop-rate", state.meta.sampleRate ? `${state.meta.sampleRate.toLocaleString()} Hz` : "—");
    setText("prop-depth", state.meta.bitDepth ? `${state.meta.bitDepth}-bit` : "—");
    setText("prop-device", state.meta.outputDevice || "—");
    setText("prop-signal", state.meta.outputBackend || "—");
    setText("prop-mode", state.meta.outputMode || "—");
    setText(
      "prop-gain",
      state.meta.replayGainActive === null || state.meta.replayGainActive === undefined
        ? "—"
        : state.meta.replayGainActive
          ? `${fmtDb(num(state.meta.replayGainDb, 0))} dB`
          : "未启用",
    );
    const parts = [];
    if (state.meta.codec) parts.push(String(state.meta.codec).toUpperCase());
    if (state.meta.sampleRate) parts.push(`${Math.round(state.meta.sampleRate / 1000)}kHz`);
    if (state.meta.bitDepth) parts.push(`${state.meta.bitDepth}bit`);
    setText("format-badge", parts.join(" / ") || "—");
  }

  function applyPlayback() {
    const playing = state.playback.state === "playing" || state.playback.state === "loading";
    const icon = el("icon-playpause");
    if (icon) {
      icon.innerHTML = playing
        ? '<path d="M6 4h4v16H6z"/><path d="M14 4h4v16h-4z"/>'
        : '<path d="M6 4.5 19 12 6 19.5z"/>';
    }
    const pillIcon = el("pill-play-btn");
    if (pillIcon) {
      pillIcon.innerHTML = playing
        ? '<svg viewBox="0 0 24 24" class="ico"><path d="M6 4h4v16H6z"/><path d="M14 4h4v16h-4z"/></svg>'
        : '<svg viewBox="0 0 24 24" class="ico"><path d="M6 4.5 19 12 6 19.5z"/></svg>';
    }
    const disc = el("pill-disc");
    if (disc) disc.style.animationPlayState = playing ? "running" : "paused";
    const vinyl = el("vinyl-record");
    if (vinyl) vinyl.style.animationPlayState = playing ? "running" : "paused";

    const repeatBtn = el("btn-repeat");
    if (repeatBtn) {
      repeatBtn.classList.toggle("active", state.playback.repeatMode !== "off");
      repeatBtn.dataset.mode = state.playback.repeatMode;
      repeatBtn.title = state.playback.repeatMode === "one"
        ? "单曲循环"
        : state.playback.repeatMode === "all" ? "列表循环" : "不循环";
    }
    const repeatOneBtn = el("btn-repeat-one");
    if (repeatOneBtn) {
      repeatOneBtn.classList.toggle("active", state.playback.repeatMode === "one");
      repeatOneBtn.title = state.playback.repeatMode === "one" ? "单曲循环：开" : "单曲循环：关";
    }
    const shuffleBtn = el("btn-shuffle");
    if (shuffleBtn) {
      shuffleBtn.classList.toggle("active", Boolean(state.playback.shuffleEnabled));
      shuffleBtn.title = state.playback.shuffleEnabled ? "随机播放：开" : "随机播放：关";
    }
  }

  function applyLevels() {
    const peak = num(state.levels.peakDb, null);
    const rms = num(state.levels.rmsDb, null);
    setText("prop-meter-source", state.levels.source === "native_post_dsp" ? "原生 DSP 后" : state.levels.source ? "DSP 前估算" : "—");

    const now = performance.now();
    if (peak !== null && peak > state.peakHoldDb) {
      state.peakHoldDb = peak;
      state.peakHoldAt = now;
    } else if (now - state.peakHoldAt > 1200) {
      state.peakHoldDb = Math.max(-72, state.peakHoldDb - 0.35);
    }

    const vPeak = el("v-meter-l");
    if (vPeak) vPeak.style.width = `${clamp(dbToRatio(peak) * 100, 0, 100)}%`;
    const vRms = el("v-meter-r");
    if (vRms) vRms.style.width = `${clamp(dbToRatio(rms) * 100, 0, 100)}%`;
    setText("v-db-l", fmtDb(peak));
    setText("v-db-r", fmtDb(rms));

    const stats = loudnessStats();
    setText("metric-integrated", fmtDb(stats.integrated));
    setText("metric-shortterm", fmtDb(stats.shortTerm));
    setText("metric-momentary", fmtDb(stats.momentary));
    setText("metric-lra", stats.range === null ? "—" : stats.range.toFixed(1));
  }

  function applyVolume() {
    const slider = el("volume-slider");
    if (slider && document.activeElement !== slider) {
      slider.value = String(Math.round(state.volume * 100));
    }
    setText("volume-label", state.muted ? "静音" : `${Math.round(state.volume * 100)}%`);
    const muteIcon = el("icon-mute");
    if (muteIcon) {
      const next = state.muted
        ? '<path d="M11 5 6 9H2v6h4l5 4z"/><path d="m16 9.5 5 5.5"/><path d="m21 9.5-5 5.5"/>'
        : '<path d="M11 5 6 9H2v6h4l5 4z"/><path d="M15.5 8.5a5 5 0 0 1 0 7"/><path d="M18.5 5.5a9 9 0 0 1 0 13"/>';
      if (muteIcon.dataset.state !== (state.muted ? "muted" : "on")) {
        muteIcon.dataset.state = state.muted ? "muted" : "on";
        muteIcon.innerHTML = next;
      }
    }
    const muteButton = el("btn-mute");
    if (muteButton) {
      muteButton.classList.toggle("muted", state.muted);
      muteButton.title = state.muted ? "取消静音" : "静音";
    }
  }

  function updateTimeline() {
    const pos = positionSeconds();
    const duration = currentDuration();
    const remain = Math.max(0, duration - pos);
    setText("time-current", fmtClock(pos));
    setText("time-total", duration > 0 ? fmtClock(duration) : "--:--.---");
    setText("time-remain", duration > 0 ? `-${fmtClock(remain)}` : "--:--.---");
    setText("head-time-readout", fmtClock(pos));
    setText("pill-time", duration > 0 ? `${fmtClock(pos, false)}  -${fmtClock(remain, false)}` : "--:--");
    const fill = el("pill-progress-fill");
    if (fill) fill.style.width = `${duration > 0 ? clamp(pos / duration, 0, 1) * 100 : 0}%`;

    // Bottom seek bar (draggable). While dragging, the bar shows the preview
    // position instead of the live one.
    if (duration > 0 && !state.seeking) {
      const ratio = clamp(pos / duration, 0, 1);
      const seekFill = el("seek-fill");
      if (seekFill) seekFill.style.width = `${ratio * 100}%`;
      const seekHandle = el("seek-handle");
      if (seekHandle) seekHandle.style.left = `${ratio * 100}%`;
      const seekBar = el("transport-seek");
      if (seekBar) seekBar.setAttribute("aria-valuenow", String(Math.round(ratio * 100)));
    }
  }

  // ── animation loop (honours the host frame budget) ───────────────────────
  let rafId = null;
  let lastHeavyAt = 0;
  let heavyCost = 0;
  function frame(now) {
    rafId = window.requestAnimationFrame(frame);
    // Everything that animates (playhead, island, meters, radar sweep) runs on
    // every animation frame, i.e. locked to the display refresh rate. Only the
    // expensive analyser panes are paced by the host frame budget plus a measured
    // cost guard, which keeps 60/120/144 Hz displays smooth instead of janky.
    try {
      captureEnvelope();
      updateTimeline();
      updateLyricHighlight();
      applyLevels();
      updateScanUi();
      renderWaveform();
      renderNavigator();
      renderRuler();
      renderRadar();
    } catch (error) {
      setConnectionState("error", String(error?.message || error));
    }

    const hostBudget = num(state.motion.frameIntervalMs, null);
    const minGap = Math.max(hostBudget && hostBudget > 20 ? hostBudget : 0, heavyCost > 8 ? 33 : 0);
    if (now - lastHeavyAt >= minGap) {
      const heavyStart = performance.now();
      try {
        renderSpectrogram();
        renderSpectrum();
        renderSoundField();
      } catch (error) {
        setConnectionState("error", String(error?.message || error));
      }
      heavyCost = performance.now() - heavyStart;
      lastHeavyAt = now;
    }
  }

  function setConnectionState(kind, detail) {
    const node = el("header-state");
    if (!node) return;
    node.dataset.state = kind;
    node.textContent =
      kind === "live" ? "实时连接 · 协议 v1" : kind === "error" ? `异常：${detail}`.slice(0, 60) : detail;
  }

  function updateStatusLine() {
    const node = el("header-state");
    if (!node) return;
    const playing = state.playback.state === "playing" || state.playback.state === "loading";
    node.dataset.state = "live";
    node.textContent = `实时连接 · 协议 v1 · ${playing ? "播放中" : "已暂停"}`;
  }

  // ── interaction ───────────────────────────────────────────────────────────
  function seekTo(seconds) {
    if (!Number.isFinite(seconds)) return;
    void command("seek", { positionSeconds: Math.max(0, seconds) });
  }

  function bindScrub(targetId, mapXtoSeconds) {
    const node = el(targetId);
    if (!node) return;
    const apply = (event) => {
      const rect = node.getBoundingClientRect();
      const ratio = clamp((event.clientX - rect.left) / Math.max(1, rect.width), 0, 1);
      seekTo(mapXtoSeconds(ratio));
    };
    let dragging = false;
    node.addEventListener("pointerdown", (event) => {
      dragging = true;
      node.setPointerCapture?.(event.pointerId);
      apply(event);
    });
    node.addEventListener("pointermove", (event) => {
      if (dragging) apply(event);
    });
    node.addEventListener("pointerup", () => {
      dragging = false;
    });
    node.addEventListener("pointercancel", () => {
      dragging = false;
    });
  }

  function setupInteraction() {
    const vrSeconds = (ratio) => {
      const vr = viewRange();
      return vr.start + ratio * (vr.end - vr.start);
    };
    bindScrub("waveform-body", vrSeconds);
    bindScrub("time-ruler-bar", vrSeconds);
    bindScrub("waveform-navigator", (ratio) => ratio * currentDuration());

    // onclick (not addEventListener) keeps wiring idempotent if boot runs twice.
    const playPause = el("btn-playpause");
    if (playPause) playPause.onclick = () => void sendPlaybackCommand("playPause");
    const pillPlay = el("pill-play-btn");
    if (pillPlay) {
      pillPlay.onclick = (event) => {
        event.stopPropagation();
        void sendPlaybackCommand("playPause");
      };
    }
    const island = el("header-center-pill");
    if (island) island.onclick = () => void sendPlaybackCommand("playPause");
    const prev = el("btn-prev");
    if (prev) prev.onclick = () => void sendPlaybackCommand("previous");
    const next = el("btn-next");
    if (next) next.onclick = () => void sendPlaybackCommand("next");
    const rw = el("btn-rw");
    if (rw) rw.onclick = () => seekTo(positionSeconds() - 5);
    const ff = el("btn-ff");
    if (ff) ff.onclick = () => seekTo(positionSeconds() + 5);
    const repeat = el("btn-repeat");
    if (repeat) {
      repeat.onclick = async () => {
        if (await sendPlaybackCommand("cycleRepeat")) {
          await sleep(150);
        }
      };
    }
    const repeatOne = el("btn-repeat-one");
    if (repeatOne) {
      repeatOne.onclick = async () => {
        const nextMode = state.playback.repeatMode === "one" ? "off" : "one";
        if (await sendPlaybackCommand("setRepeat", { mode: nextMode })) {
          state.playback.repeatMode = nextMode;
          applyPlayback();
          toast(nextMode === "one" ? "已开启单曲循环" : "已关闭循环", "ok", 1800);
        }
      };
    }
    const shuffle = el("btn-shuffle");
    if (shuffle) shuffle.onclick = () => void sendPlaybackCommand("toggleShuffle");

    el("btn-zoom-in")?.addEventListener("click", () => zoomBy(1 / 1.6));
    el("btn-zoom-out")?.addEventListener("click", () => zoomBy(1.6));
    el("btn-zoom-fit")?.addEventListener("click", () => {
      state.view.zoomed = false;
    });

    // Bottom seek bar: drag to scrub, release to seek.
    const seekBar = el("transport-seek");
    if (seekBar) {
      const ratioAt = (event) => {
        const rect = seekBar.getBoundingClientRect();
        return clamp((event.clientX - rect.left) / Math.max(1, rect.width), 0, 1);
      };
      const preview = (ratio) => {
        const fill = el("seek-fill");
        if (fill) fill.style.width = `${ratio * 100}%`;
        const handle = el("seek-handle");
        if (handle) handle.style.left = `${ratio * 100}%`;
        const duration = currentDuration();
        state.previewPosition = ratio * Math.max(0, duration);
        setText("time-current", fmtClock(state.previewPosition));
      };
      seekBar.addEventListener("pointerdown", (event) => {
        if (currentDuration() <= 0) return;
        state.seeking = true;
        seekBar.classList.add("dragging");
        try {
          seekBar.setPointerCapture(event.pointerId);
        } catch {
          /* capture is best-effort */
        }
        preview(ratioAt(event));
      });
      seekBar.addEventListener("pointermove", (event) => {
        if (state.seeking) preview(ratioAt(event));
      });
      const finish = (event) => {
        if (!state.seeking) return;
        state.seeking = false;
        seekBar.classList.remove("dragging");
        try {
          seekBar.releasePointerCapture(event.pointerId);
        } catch {
          /* ignore */
        }
        void sendPlaybackCommand("seek", { positionSeconds: Math.max(0, state.previewPosition) });
      };
      seekBar.addEventListener("pointerup", finish);
      seekBar.addEventListener("pointercancel", finish);
      seekBar.addEventListener("keydown", (event) => {
        if (event.key === "ArrowLeft") seekTo(positionSeconds() - 5);
        else if (event.key === "ArrowRight") seekTo(positionSeconds() + 5);
      });
    }
    el("btn-scan-wave")?.addEventListener("click", () => {
      void scanWaveform();
    });
    const modeButton = el("btn-scan-mode");
    if (modeButton) {
      modeButton.onclick = () => {
        state.prefs.preRender = !state.prefs.preRender;
        state.prefs.autoScan = state.prefs.preRender;
        state.scan.message = state.prefs.preRender ? "" : "实时模式：波形随播放绘制";
        if (!state.prefs.preRender && state.scan.active) state.scan.cancel = true;
        updateScanUi();
        scheduleSave();
        if (state.prefs.preRender) maybeAutoScan();
      };
    }
    const qualityButton = el("btn-scan-quality");
    if (qualityButton) {
      qualityButton.onclick = () => {
        const index = SCAN_ORDER.indexOf(state.prefs.scanQuality);
        state.prefs.scanQuality = SCAN_ORDER[(index + 1) % SCAN_ORDER.length];
        const preset = SCAN_QUALITY[state.prefs.scanQuality];
        const duration = currentDuration();
        toast(
          `预扫描精度：${preset.label}（5x 高密度采样 · 约 ${duration > 0 ? ((scanPoints(duration, state.prefs.scanQuality) * 85) / 1000).toFixed(1) : "—"} 秒）`,
          "ok",
          2200,
        );
        updateScanUi();
        scheduleSave();
      };
    }

    const slider = el("volume-slider");
    slider?.addEventListener("input", () => {
      state.volume = clamp(Number(slider.value) / 100, 0, 1);
      void sendPlaybackCommand("setVolume", { volume: state.volume });
      applyVolume();
    });
    el("btn-mute")?.addEventListener("click", () => {
      state.muted = !state.muted;
      void sendPlaybackCommand("setVolume", { volume: state.muted ? 0 : state.volume });
      applyVolume();
    });

    el("btn-freeze-insight")?.addEventListener("click", () => {
      state.frozen = !state.frozen;
      el("btn-freeze-insight")?.classList.toggle("active", state.frozen);
      setText("analyzer-state", state.frozen ? "已冻结" : "实时");
    });
    el("btn-reset-insight")?.addEventListener("click", () => {
      state.rmsHistory = [];
      state.peakHoldDb = -Infinity;
      const trackId = state.playback.currentTrackId || state.track?.id;
      if (trackId) state.envelopes.delete(trackId);
    });

    const scroll = el("lyrics-scroll");
    scroll?.addEventListener("wheel", () => {
      lyricState.userScrolledAt = performance.now();
    }, { passive: true });
    // Sound Field tabs (Polar Sample / Polar Level / Lissajous)
    const initialMode = state.soundfieldMode || "polar-sample";
    state.soundfieldMode = initialMode;
    const sfTabs = document.querySelectorAll(".soundfield-tab");
    sfTabs.forEach((tab) => {
      const mode = tab.getAttribute("data-mode");
      tab.classList.toggle("active", mode === initialMode);
      tab.addEventListener("click", () => {
        const targetMode = tab.getAttribute("data-mode");
        if (!targetMode) return;
        state.soundfieldMode = targetMode;
        scheduleSave();
        sfTabs.forEach((t) => t.classList.toggle("active", t.getAttribute("data-mode") === targetMode));
      });
    });

    const tabTracks = el("tab-queue-tracks");
    const tabHistory = el("tab-queue-history");
    tabTracks?.addEventListener("click", () => {
      state.queueTab = "tracks";
      tabTracks.classList.add("active");
      tabHistory?.classList.remove("active");
      parent.postMessage({ type: "echo:audition:getQueue" }, "*");
      renderQueueList();
    });
    tabHistory?.addEventListener("click", () => {
      state.queueTab = "history";
      tabHistory?.classList.add("active");
      tabTracks?.classList.remove("active");
      renderQueueList();
    });

    window.addEventListener("keydown", (event) => {
      const backdrop = el("audition-modal-backdrop");
      if (event.key === "Escape") {
        if (backdrop && !backdrop.hidden) {
          closeModal();
          return;
        }
        if (typeof closeDropdown === "function") {
          closeDropdown();
        }
        post({ type: "echo:workshop-ui:interaction", action: "back" });
        return;
      }

      if (backdrop && !backdrop.hidden) return;
      if (["INPUT", "TEXTAREA", "SELECT"].includes(event.target?.tagName)) return;

      if (event.code === "Space") {
        event.preventDefault();
        commandNoWait("playPause");
      } else if (event.key === "ArrowLeft") {
        if (event.ctrlKey) {
          event.preventDefault();
          commandNoWait("previous");
        } else {
          seekTo(positionSeconds() - 5);
        }
      } else if (event.key === "ArrowRight") {
        if (event.ctrlKey) {
          event.preventDefault();
          commandNoWait("next");
        } else {
          seekTo(positionSeconds() + 5);
        }
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        state.volume = clamp(state.volume + 0.05, 0, 1);
        void sendPlaybackCommand("setVolume", { volume: state.volume });
        applyVolume();
      } else if (event.key === "ArrowDown") {
        event.preventDefault();
        state.volume = clamp(state.volume - 0.05, 0, 1);
        void sendPlaybackCommand("setVolume", { volume: state.volume });
        applyVolume();
      } else if ((event.key === "m" || event.key === "M") && !event.ctrlKey && !event.altKey) {
        state.muted = !state.muted;
        void sendPlaybackCommand("setVolume", { volume: state.muted ? 0 : state.volume });
        applyVolume();
        toast(state.muted ? "已静音" : "已恢复音量", "ok", 1500);
      } else if ((event.key === "f" || event.key === "F") && !event.ctrlKey && !event.altKey) {
        state.frozen = !state.frozen;
        el("btn-freeze-insight")?.classList.toggle("active", state.frozen);
        setText("analyzer-state", state.frozen ? "已冻结" : "实时");
        toast(state.frozen ? "频谱与声场已冻结" : "频谱与声场已恢复实时", "ok", 1500);
      } else if ((event.key === "p" || event.key === "P") && !event.ctrlKey && !event.altKey) {
        state.rmsHistory = [];
        state.peakHoldDb = -Infinity;
        const trackId = state.playback.currentTrackId || state.track?.id;
        if (trackId) state.envelopes.delete(trackId);
        toast("已重置所有峰值电平指标", "ok", 1800);
      } else if ((event.key === "c" || event.key === "C") && !event.ctrlKey && !event.altKey) {
        const duration = currentDuration();
        if (duration > 0) {
          const pos = positionSeconds();
          const vr = viewRange();
          const span = vr.end - vr.start;
          let start = clamp(pos - span / 2, 0, Math.max(0, duration - span));
          state.view.start = start;
          state.view.end = Math.min(duration, start + span);
          state.view.zoomed = true;
          toast("已居中对齐播放指针", "ok", 1500);
        }
      } else if (event.ctrlKey && (event.key === "=" || event.key === "+")) {
        event.preventDefault();
        zoomBy(1 / 1.6);
      } else if (event.ctrlKey && event.key === "-") {
        event.preventDefault();
        zoomBy(1.6);
      } else if (event.ctrlKey && event.key === "0") {
        event.preventDefault();
        state.view.zoomed = false;
        toast("视图已重置为全局概览", "ok", 1500);
      } else if (event.ctrlKey && (event.key === "," || event.key === "，")) {
        event.preventDefault();
        showPreferencesModal();
      } else if (event.ctrlKey && (event.key === "t" || event.key === "T")) {
        event.preventDefault();
        toggleThemeMode();
      } else if (event.ctrlKey && event.altKey && (event.key === "w" || event.key === "W")) {
        event.preventDefault();
        toggleWaveColor();
      } else if (event.key === "F1") {
        event.preventDefault();
        showShortcutsModal();
      }
    });
  }

  function zoomBy(factor) {
    const duration = currentDuration();
    if (duration <= 0) return;
    const vr = viewRange();
    const center = clamp(positionSeconds(), vr.start, vr.end);
    let span = clamp((vr.end - vr.start) * factor, 2, duration);
    let start = clamp(center - span / 2, 0, Math.max(0, duration - span));
    let end = Math.min(duration, start + span);
    if (span >= duration - 0.5) {
      state.view.zoomed = false;
      return;
    }
    state.view.start = start;
    state.view.end = end;
    state.view.zoomed = true;
  }

  // ── message handling ─────────────────────────────────────────────────────
  function onMessage(event) {
    if (event.source !== parent) return;
    const msg = event.data;
    if (!msg) return;

    if (msg.type === "echo:audition:queueData" || msg.type === "shinawase:player:queue") {
      if (Array.isArray(msg.items) && msg.items.length > 0) {
        state.liveQueue = msg.items.map((it) => ({
          queueId: it.queueId,
          id: it.track?.id || it.id,
          title: it.track?.title || it.title || "未知曲目",
          artist: it.track?.artist || it.artist || "",
          album: it.track?.album || it.album || "",
          durationSeconds: Number(it.track?.duration) || Number(it.durationSeconds) || 0,
        }));
        renderQueueList();
      }
      return;
    }

    if (msg.protocolVersion !== PROTOCOL) return;

    switch (msg.type) {
      case "echo:workshop-ui:ping":
        post({ type: "echo:workshop-ui:pong" });
        return;
      case "echo:workshop-ui:result": {
        const resolver = pending.get(msg.requestId);
        if (resolver) {
          pending.delete(msg.requestId);
          resolver(msg);
        }
        return;
      }
      case "echo:workshop-ui:init": {
        state.connected = true;
        state.appearance = msg.appearance ?? null;
        applyAppearance(msg.appearance);
        updateStatusLine();
        break;
      }
      case "echo:workshop-ui:state": {
        state.connected = true;
        if (msg.playback) {
          state.playback = { ...state.playback, ...msg.playback };
          applyPlayback();
        }
        if (msg.currentTrack) applyTrack(msg.currentTrack);
        if (msg.spectrum) {
          const bands = Array.isArray(msg.spectrum.bands) && msg.spectrum.bands.length > 0
            ? msg.spectrum.bands
            : state.spectrum.bands;
          state.spectrum = { ...state.spectrum, ...msg.spectrum, bands };
        }
        if (msg.motion) state.motion = { ...state.motion, ...msg.motion };
        if (msg.lyrics) {
          state.peek = msg.lyrics;
          if (!lyricState.lines || lyricState.lines.length === 0) {
            renderLyricsMessage(msg.lyrics);
          }
        }
        if (isPlayingState() && (!lyricState.lines || lyricState.lines.length === 0)) {
          const tid = state.playback.currentTrackId || state.track?.id;
          if (tid && state.lyricsRequested !== tid) {
            state.lyricsRequested = tid;
            command("lyrics:get", { trackId: tid }).then((res) => {
              if (res?.lyrics && (res.lyrics.lines?.length || res.lyrics.kind === "instrumental")) {
                renderLyricsMessage(res.lyrics);
              }
            }).catch(() => {});
          }
        }
        updateStatusLine();
        break;
      }
      case "echo:workshop-ui:lyrics": {
        renderLyricsMessage(msg.lyrics ?? null);
        if (msg.lyrics?.title || msg.lyrics?.artist) {
          applyTrack({
            id: msg.trackId ?? state.track?.id,
            title: msg.lyrics.title,
            artist: msg.lyrics.artist,
            album: msg.lyrics.album ?? state.track?.album,
            durationSeconds: num(msg.lyrics.durationSeconds, state.track?.durationSeconds),
            coverUrl: resolveCoverUrl(msg.lyrics) || state.track?.coverUrl || null,
          });
        }
        break;
      }
      case "echo:workshop-ui:clock": {
        state.clock = msg.clock ?? null;
        if (msg.motion) state.motion = { ...state.motion, ...msg.motion };
        if (msg.clock?.state) {
          state.playback.state = msg.clock.state;
          if (msg.clock.state !== "playing" && msg.clock.state !== "loading") {
            // Silence spectrum energy when paused/idle
            state.spectrum.energy = 0;
            if (Array.isArray(state.spectrum.bands)) {
              state.spectrum.bands.fill(0);
            }
          }
        }
        if (msg.clock?.currentTrackId) state.playback.currentTrackId = msg.clock.currentTrackId;
        applyPlayback();
        updateStatusLine();
        maybeAutoScan();
        break;
      }
      case "echo:workshop-ui:audio": {
        if (msg.trackId && state.playback.currentTrackId && msg.trackId !== state.playback.currentTrackId) break;
        if (msg.audio) applyMeta(msg.audio);
        if (msg.levels) state.levels = { ...state.levels, ...msg.levels };
        if (msg.spectrum) state.spectrum = { ...state.spectrum, ...msg.spectrum };
        break;
      }
      case "echo:workshop-ui:appearance":
        applyAppearance(msg.appearance);
        break;
      default:
        break;
    }
  }

  function applyAppearance(appearance) {
    if (!appearance) return;
    const root = document.documentElement;
    // The host palette describes the *player's* own theme (light "classic" by
    // default) and is deliberately NOT mapped onto the Audition palette: doing so
    // painted the reserved titlebar strip light and clashed with the app chrome.
    // Only the accent colour is adopted.
    const map = {
      accent: "--accent-cyan",
      accentText: "--accent-text",
    };
    for (const [key, variable] of Object.entries(map)) {
      const value = appearance[key];
      if (typeof value === "string" && value.length > 0) root.style.setProperty(variable, value);
    }
  }

  // ── Audition Menubar & Modals ─────────────────────────────────────────────
  function showModal(title, html) {
    const backdrop = el("audition-modal-backdrop");
    const modalTitle = el("modal-title");
    const modalBody = el("modal-body");
    if (!backdrop || !modalTitle || !modalBody) return;
    modalTitle.textContent = title;
    modalBody.innerHTML = html;
    backdrop.hidden = false;
  }

  function closeModal() {
    const backdrop = el("audition-modal-backdrop");
    if (backdrop) backdrop.hidden = true;
  }

  function showShortcutsModal() {
    const html = `
      <table class="shortcuts-table">
        <tbody>
          <tr><td>${t("shortcuts.playpause")}</td><td class="kbd-cell"><kbd>Space</kbd></td></tr>
          <tr><td>${t("shortcuts.rwff")}</td><td class="kbd-cell"><kbd>←</kbd> / <kbd>→</kbd></td></tr>
          <tr><td>${t("shortcuts.prevnext")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>←</kbd> / <kbd>→</kbd></td></tr>
          <tr><td>${t("shortcuts.vol")}</td><td class="kbd-cell"><kbd>↑</kbd> / <kbd>↓</kbd></td></tr>
          <tr><td>${t("shortcuts.mute")}</td><td class="kbd-cell"><kbd>M</kbd></td></tr>
          <tr><td>${t("shortcuts.zoom")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>+</kbd> / <kbd>-</kbd></td></tr>
          <tr><td>${t("shortcuts.zoomFit")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>0</kbd></td></tr>
          <tr><td>${t("shortcuts.centerPlayhead")}</td><td class="kbd-cell"><kbd>C</kbd></td></tr>
          <tr><td>${t("shortcuts.freeze")}</td><td class="kbd-cell"><kbd>F</kbd></td></tr>
          <tr><td>${t("shortcuts.resetPeak")}</td><td class="kbd-cell"><kbd>P</kbd></td></tr>
          <tr><td>${t("shortcuts.theme")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>T</kbd></td></tr>
          <tr><td>${t("shortcuts.waveColor")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>Alt</kbd>+<kbd>W</kbd></td></tr>
          <tr><td>${t("shortcuts.prefs")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>,</kbd></td></tr>
          <tr><td>${t("shortcuts.reload")}</td><td class="kbd-cell"><kbd>Ctrl</kbd>+<kbd>R</kbd></td></tr>
          <tr><td>${t("shortcuts.close")}</td><td class="kbd-cell"><kbd>Esc</kbd></td></tr>
        </tbody>
      </table>
    `;
    showModal(t("shortcuts.title"), html);
  }

  function showPreferencesModal() {
    const html = `
      <div style="font-size:12px; line-height:1.7; color:#c6cbd2;">
        <div style="margin-bottom:14px;">
          <div style="color:var(--accent-cyan, #00e5ff); font-weight:600; margin-bottom:6px;">${t("prefs.languageTitle")}</div>
          <select id="pref-language" style="background:#23272d; color:#e0e4ea; border:1px solid #3b424d; padding:4px 8px; border-radius:3px; font-size:11px; width:100%;">
            <option value="zh" ${state.lang === "zh" ? "selected" : ""}>简体中文 (Chinese - 默认)</option>
            <option value="en" ${state.lang === "en" ? "selected" : ""}>English (English)</option>
            <option value="fr" ${state.lang === "fr" ? "selected" : ""}>Français (French)</option>
            <option value="ja" ${state.lang === "ja" ? "selected" : ""}>日本語 (Japanese)</option>
            <option value="ko" ${state.lang === "ko" ? "selected" : ""}>한국어 (Korean)</option>
          </select>
          <div style="color:#828a95; font-size:11px; margin-top:4px;">${t("prefs.languageHint")}</div>
        </div>
        <div style="margin-bottom:14px;">
          <div style="color:var(--accent-cyan, #00e5ff); font-weight:600; margin-bottom:6px;">${t("prefs.renderModeTitle")}</div>
          <label style="display:flex; align-items:center; gap:8px; cursor:pointer;">
            <input type="checkbox" id="pref-prerender" ${state.prefs.preRender ? "checked" : ""}>
            <span>${t("prefs.renderModeEnable")}</span>
          </label>
          <div style="color:#828a95; font-size:11px; margin-top:4px;">${t("prefs.renderModeHint")}</div>
        </div>
        <div style="margin-bottom:14px;">
          <div style="color:var(--accent-cyan, #00e5ff); font-weight:600; margin-bottom:6px;">${t("prefs.qualityTitle")}</div>
          <select id="pref-scanquality" style="background:#23272d; color:#e0e4ea; border:1px solid #3b424d; padding:4px 8px; border-radius:3px; font-size:11px; width:100%;">
            <option value="fine" ${state.prefs.scanQuality === "fine" ? "selected" : ""}>${t("prefs.qualityFineOpt")}</option>
            <option value="standard" ${state.prefs.scanQuality === "standard" ? "selected" : ""}>${t("prefs.qualityStandardOpt")}</option>
            <option value="fast" ${state.prefs.scanQuality === "fast" ? "selected" : ""}>${t("prefs.qualityFastOpt")}</option>
          </select>
        </div>
        <div style="margin-bottom:8px;">
          <div style="color:var(--accent-cyan, #00e5ff); font-weight:600; margin-bottom:6px;">${t("prefs.rangeTitle")}</div>
          <div style="background:#13161a; padding:8px 10px; border-radius:4px; border:1px solid #23272d; font-size:11px; color:#95a0af;">
            ${t("prefs.rangeDesc")}
          </div>
        </div>
      </div>
    `;
    showModal(t("prefs.title"), html);
    const langSel = el("pref-language");
    if (langSel) {
      langSel.onchange = () => {
        setLanguage(langSel.value, true);
        showPreferencesModal();
      };
    }
    const cb = el("pref-prerender");
    if (cb) {
      cb.onchange = () => {
        state.prefs.preRender = cb.checked;
        state.prefs.autoScan = cb.checked;
        state.scan.message = cb.checked ? "" : t("prefs.renderModeHint");
        if (!cb.checked && state.scan.active) state.scan.cancel = true;
        updateScanUi();
        scheduleSave();
        if (cb.checked) maybeAutoScan();
      };
    }
    const sq = el("pref-scanquality");
    if (sq) {
      sq.onchange = () => {
        setScanQuality(sq.value);
      };
    }
  }

  function showAboutModal() {
    const html = `
      <div style="font-size:12px; line-height:1.7; color:#c6cbd2;">
        <div style="display:flex; align-items:center; gap:12px; margin-bottom:12px;">
          <div style="width:42px; height:42px; background:#00e5ff18; border:1px solid #00e5ff60; border-radius:4px; display:flex; align-items:center; justify-content:center; color:#00e5ff; font-weight:bold; font-size:20px;">Au</div>
          <div>
            <div style="font-size:14px; font-weight:bold; color:#ffffff;">${t("about.headerTitle")}</div>
            <div style="font-size:11px; color:#828a95;">${t("about.version")}</div>
          </div>
        </div>
        <p style="margin-bottom:8px;">${t("about.desc")}</p>
        <div style="background:#13161a; padding:10px 12px; border-radius:4px; border:1px solid #23272d; margin-bottom:10px; font-size:11px;">
          <div style="color:var(--accent-cyan, #00e5ff); font-weight:bold; margin-bottom:4px;">${t("about.featTitle")}</div>
          <div>${t("about.f1")}</div>
          <div>${t("about.f2")}</div>
          <div>${t("about.f3")}</div>
          <div>${t("about.f4")}</div>
          <div>${t("about.f5")}</div>
          <div>${t("about.f6")}</div>
          <div>${t("about.f7")}</div>
          <div>${t("about.f8")}</div>
          <div>${t("about.f9")}</div>
        </div>
        <p style="font-size:10px; color:#606770; text-align:right;">${t("about.footer")}</p>
      </div>
    `;
    showModal(t("about.title"), html);
  }

  let activeMenuName = null;

  function closeDropdown() {
    activeMenuName = null;
    const dropdown = el("menu-dropdown");
    if (dropdown) dropdown.hidden = true;
    document.querySelectorAll(".audition-menubar .menu-item").forEach((item) => {
      item.classList.remove("active");
    });
  }

  function setScanQuality(q) {
    if (!SCAN_QUALITY[q]) return;
    state.prefs.scanQuality = q;
    const preset = SCAN_QUALITY[q];
    toast(t("toast.scanQuality").replace("{quality}", preset ? preset.label : q), "ok", 1800);
    updateScanUi();
    scheduleSave();
  }

  function setSoundfieldMode(mode) {
    state.soundfieldMode = mode;
    scheduleSave();
    const sfTabs = document.querySelectorAll(".soundfield-tab");
    sfTabs.forEach((t) => t.classList.toggle("active", t.getAttribute("data-mode") === mode));
    toast(t("toast.soundfield").replace("{mode}", mode), "ok", 1500);
  }

  function setupAuditionMenubar() {
    const MENUS = {
      file: [
        {
          get label() { return t("menu.file.reload"); },
          shortcut: "Ctrl+R",
          action: () => window.location.reload(),
        },
        {
          get label() { return t("menu.file.copyPath"); },
          shortcut: "",
          action: () => {
            const path = state.track?.path || state.track?.filePath || state.track?.url || "";
            if (path) {
              navigator.clipboard.writeText(path).then(
                () => toast(t("toast.pathCopied"), "ok", 2000),
                () => toast(t("toast.pathFailed"), "warn", 2000),
              );
            } else {
              toast(t("toast.noPath"), "warn", 2000);
            }
          },
        },
        {
          get label() { return t("menu.file.copyMeta"); },
          shortcut: "",
          action: () => {
            const data = { track: state.track, meta: state.meta, clock: state.clock };
            navigator.clipboard.writeText(JSON.stringify(data, null, 2)).then(
              () => toast(t("toast.metaCopied"), "ok", 2000),
              () => toast(t("toast.pathFailed"), "warn", 2000),
            );
          },
        },
        { divider: true },
        {
          get label() { return t("menu.file.prefs"); },
          shortcut: "Ctrl+,",
          action: () => showPreferencesModal(),
        },
        {
          get label() { return t("menu.file.back"); },
          shortcut: "Esc",
          action: () => post({ type: "echo:workshop-ui:interaction", action: "back" }),
        },
      ],
      edit: [
        {
          get label() { return t("menu.edit.copyTitle"); },
          shortcut: "",
          action: () => {
            const title = state.track?.title || "—";
            navigator.clipboard.writeText(title);
            toast(`${t("toast.copied")}: "${title}"`, "ok", 1800);
          },
        },
        {
          get label() { return t("menu.edit.copyArtist"); },
          shortcut: "",
          action: () => {
            const artist = state.track?.artist || "—";
            navigator.clipboard.writeText(artist);
            toast(`${t("toast.copied")}: "${artist}"`, "ok", 1800);
          },
        },
        { divider: true },
        {
          get label() { return t("menu.edit.resetPeak"); },
          shortcut: "P",
          action: () => {
            state.rmsHistory = [];
            state.peakHoldDb = -Infinity;
            const trackId = state.playback.currentTrackId || state.track?.id;
            if (trackId) state.envelopes.delete(trackId);
            toast(t("toast.peakReset"), "ok", 1800);
          },
        },
        {
          get label() { return t("menu.edit.rescan"); },
          shortcut: "",
          action: () => {
            void scanWaveform();
          },
        },
      ],
      multitrack: [
        {
          get label() { return t("menu.multi.realtime"); },
          checked: () => !state.prefs.preRender,
          action: () => {
            state.prefs.preRender = false;
            state.prefs.autoScan = false;
            state.scan.message = t("prefs.renderModeHint");
            if (state.scan.active) state.scan.cancel = true;
            updateScanUi();
            scheduleSave();
            toast(t("toast.modeRealtime"), "ok", 1800);
          },
        },
        {
          get label() { return t("menu.multi.prerender"); },
          checked: () => state.prefs.preRender,
          action: () => {
            state.prefs.preRender = true;
            state.prefs.autoScan = true;
            state.scan.message = "";
            updateScanUi();
            scheduleSave();
            maybeAutoScan();
            toast(t("toast.modePrerender"), "ok", 1800);
          },
        },
        { divider: true },
        {
          get label() { return t("menu.multi.fine"); },
          checked: () => state.prefs.scanQuality === "fine",
          action: () => setScanQuality("fine"),
        },
        {
          get label() { return t("menu.multi.standard"); },
          checked: () => state.prefs.scanQuality === "standard",
          action: () => setScanQuality("standard"),
        },
        {
          get label() { return t("menu.multi.fast"); },
          checked: () => state.prefs.scanQuality === "fast",
          action: () => setScanQuality("fast"),
        },
        { divider: true },
        {
          get label() { return t("menu.multi.polarSample"); },
          checked: () => (state.soundfieldMode || "polar-sample") === "polar-sample",
          action: () => setSoundfieldMode("polar-sample"),
        },
        {
          get label() { return t("menu.multi.polarLevel"); },
          checked: () => state.soundfieldMode === "polar-level",
          action: () => setSoundfieldMode("polar-level"),
        },
        {
          get label() { return t("menu.multi.lissajous"); },
          checked: () => state.soundfieldMode === "lissajous",
          action: () => setSoundfieldMode("lissajous"),
        },
      ],
      clip: [
        {
          get label() { return t("menu.clip.center"); },
          shortcut: "C",
          action: () => {
            const duration = currentDuration();
            if (duration <= 0) return;
            const pos = positionSeconds();
            const vr = viewRange();
            const span = vr.end - vr.start;
            let start = clamp(pos - span / 2, 0, Math.max(0, duration - span));
            state.view.start = start;
            state.view.end = Math.min(duration, start + span);
            state.view.zoomed = true;
            toast(t("toast.playheadCentered"), "ok", 1500);
          },
        },
        {
          get label() { return t("menu.clip.fit"); },
          shortcut: "Ctrl+0",
          action: () => {
            state.view.zoomed = false;
            toast(t("toast.viewReset"), "ok", 1500);
          },
        },
        { divider: true },
        {
          get label() { return t("menu.clip.copyTime"); },
          shortcut: "",
          action: () => {
            const tc = fmtClock(positionSeconds());
            navigator.clipboard.writeText(tc);
            toast(t("toast.posCopied").replace("{time}", tc), "ok", 1800);
          },
        },
      ],
      effects: [
        {
          get label() { return t("menu.fx.freeze"); },
          shortcut: "F",
          checked: () => state.frozen,
          action: () => {
            state.frozen = !state.frozen;
            el("btn-freeze-insight")?.classList.toggle("active", state.frozen);
            setText("analyzer-state", state.frozen ? t("analyzer.stateFrozen") : t("analyzer.stateLive"));
            toast(state.frozen ? t("toast.frozen") : t("toast.unfrozen"), "ok", 1500);
          },
        },
        {
          get label() { return t("menu.fx.clearWaterfall"); },
          shortcut: "",
          action: () => {
            state.spectrum.energy = 0;
            if (Array.isArray(state.spectrum.bands)) state.spectrum.bands.fill(0);
            const entry = canvases.get("spectrogram-canvas");
            if (entry && entry.offCtx) {
              entry.offCtx.fillStyle = "#0b0f13";
              entry.offCtx.fillRect(0, 0, entry.off.width, entry.off.height);
            }
            toast(t("toast.waterfallCleared"), "ok", 1500);
          },
        },
      ],
      zoom: [
        {
          get label() { return t("menu.zoom.in"); },
          shortcut: "Ctrl++",
          action: () => zoomBy(1 / 1.6),
        },
        {
          get label() { return t("menu.zoom.out"); },
          shortcut: "Ctrl+-",
          action: () => zoomBy(1.6),
        },
        {
          get label() { return t("menu.zoom.fit"); },
          shortcut: "Ctrl+0",
          action: () => {
            state.view.zoomed = false;
          },
        },
        { divider: true },
        {
          get label() { return t("menu.zoom.ff5"); },
          shortcut: "→",
          action: () => seekTo(positionSeconds() + 5),
        },
        {
          get label() { return t("menu.zoom.rw5"); },
          shortcut: "←",
          action: () => seekTo(positionSeconds() - 5),
        },
      ],
      transport: [
        {
          get label() { return t("menu.trans.playpause"); },
          shortcut: "Space",
          action: () => void sendPlaybackCommand("playPause"),
        },
        {
          get label() { return t("menu.trans.stop"); },
          shortcut: "Esc",
          action: () => void sendPlaybackCommand("stop"),
        },
        {
          get label() { return t("menu.trans.prev"); },
          shortcut: "Ctrl+←",
          action: () => void sendPlaybackCommand("previous"),
        },
        {
          get label() { return t("menu.trans.next"); },
          shortcut: "Ctrl+→",
          action: () => void sendPlaybackCommand("next"),
        },
        {
          get label() { return t("menu.trans.start"); },
          shortcut: "Home",
          action: () => seekTo(0),
        },
        { divider: true },
        {
          get label() { return t("menu.trans.rw5"); },
          shortcut: "←",
          action: () => seekTo(positionSeconds() - 5),
        },
        {
          get label() { return t("menu.trans.ff5"); },
          shortcut: "→",
          action: () => seekTo(positionSeconds() + 5),
        },
        {
          get label() { return t("menu.trans.rw15"); },
          shortcut: "Shift+←",
          action: () => seekTo(positionSeconds() - 15),
        },
        {
          get label() { return t("menu.trans.ff15"); },
          shortcut: "Shift+→",
          action: () => seekTo(positionSeconds() + 15),
        },
        { divider: true },
        {
          get label() { return t("menu.trans.repeatAll"); },
          checked: () => state.playback.repeatMode === "all",
          action: async () => {
            const nextMode = state.playback.repeatMode === "all" ? "off" : "all";
            if (await sendPlaybackCommand("setRepeat", { mode: nextMode })) {
              state.playback.repeatMode = nextMode;
              applyPlayback();
              toast(nextMode === "all" ? t("toast.repeatAll") : t("toast.sequential"), "ok", 1800);
            }
          },
        },
        {
          get label() { return t("menu.trans.repeatOne"); },
          checked: () => state.playback.repeatMode === "one",
          action: async () => {
            const nextMode = state.playback.repeatMode === "one" ? "off" : "one";
            if (await sendPlaybackCommand("setRepeat", { mode: nextMode })) {
              state.playback.repeatMode = nextMode;
              applyPlayback();
              toast(nextMode === "one" ? t("toast.repeatOne") : t("toast.repeatOff"), "ok", 1800);
            }
          },
        },
        {
          get label() { return t("menu.trans.sequential"); },
          checked: () => !state.playback.repeatMode || state.playback.repeatMode === "off",
          action: async () => {
            if (await sendPlaybackCommand("setRepeat", { mode: "off" })) {
              state.playback.repeatMode = "off";
              applyPlayback();
              toast(t("toast.sequential"), "ok", 1800);
            }
          },
        },
        {
          get label() { return t("menu.trans.shuffle"); },
          checked: () => Boolean(state.playback.shuffleEnabled || state.playback.shuffle),
          action: () => void sendPlaybackCommand("toggleShuffle"),
        },
        {
          get label() { return t("menu.trans.cycle"); },
          shortcut: "",
          action: () => void sendPlaybackCommand("cycleRepeat"),
        },
        { divider: true },
        {
          get label() { return t("menu.trans.mute"); },
          shortcut: "M",
          checked: () => state.muted,
          action: () => {
            state.muted = !state.muted;
            void sendPlaybackCommand("setVolume", { volume: state.muted ? 0 : state.volume });
            applyVolume();
            toast(state.muted ? t("toast.muted") : t("toast.unmuted"), "ok", 1500);
          },
        },
        {
          get label() { return t("menu.trans.volUp"); },
          shortcut: "↑",
          action: () => {
            state.volume = clamp(state.volume + 0.05, 0, 1);
            void sendPlaybackCommand("setVolume", { volume: state.volume });
            applyVolume();
          },
        },
        {
          get label() { return t("menu.trans.volDown"); },
          shortcut: "↓",
          action: () => {
            state.volume = clamp(state.volume - 0.05, 0, 1);
            void sendPlaybackCommand("setVolume", { volume: state.volume });
            applyVolume();
          },
        },
        {
          get label() { return t("menu.trans.vol80"); },
          shortcut: "",
          action: () => {
            state.volume = 0.8;
            state.muted = false;
            void sendPlaybackCommand("setVolume", { volume: 0.8 });
            applyVolume();
            toast(t("toast.volStandard"), "ok", 1500);
          },
        },
        {
          get label() { return t("menu.trans.vol100"); },
          shortcut: "",
          action: () => {
            state.volume = 1.0;
            state.muted = false;
            void sendPlaybackCommand("setVolume", { volume: 1.0 });
            applyVolume();
            toast(t("toast.volMax"), "ok", 1500);
          },
        },
        { divider: true },
        {
          get label() { return t("menu.trans.syncQueue"); },
          shortcut: "F5",
          action: () => {
            parent.postMessage({ type: "echo:audition:getQueue" }, "*");
            toast(t("toast.syncQueue"), "ok", 1500);
          },
        },
        {
          get label() { return t("menu.trans.scanFull"); },
          shortcut: "",
          action: () => {
            if (state.scan.active) {
              toast(t("toast.scanRunning"), "warn", 1500);
            } else {
              void scanWaveform({ force: true });
              toast(t("toast.scanStarted"), "ok", 1800);
            }
          },
        },
        {
          get label() { return t("menu.trans.stopScan"); },
          shortcut: "",
          action: () => {
            if (state.scan.active) {
              state.scan.cancel = true;
              toast(t("toast.scanCancelled"), "warn", 1500);
            } else {
              toast(t("toast.noScan"), "info", 1500);
            }
          },
        },
        {
          get label() { return t("menu.trans.resetHold"); },
          shortcut: "P",
          action: () => {
            state.rmsHistory = [];
            state.peakHoldDb = -Infinity;
            toast(t("toast.peakReset"), "ok", 1500);
          },
        },
      ],
      window: [
        {
          get label() { return t("menu.win.theme"); },
          shortcut: "Ctrl+T",
          checked: () => state.themeMode === "light",
          action: () => toggleThemeMode(),
        },
        {
          get label() { return t("menu.win.waveBlue"); },
          shortcut: "Ctrl+Alt+W",
          checked: () => state.waveColor === "blue",
          action: () => setWaveColor("blue"),
        },
        {
          get label() { return t("menu.win.wavePink"); },
          shortcut: "",
          checked: () => state.waveColor === "pink",
          action: () => setWaveColor("pink"),
        },
        { divider: true },
        {
          get label() { return t("menu.win.fullscreen"); },
          shortcut: "F11",
          action: () => {
            if (!document.fullscreenElement) {
              document.documentElement.requestFullscreen().catch(() => {});
            } else {
              document.exitFullscreen().catch(() => {});
            }
          },
        },
        {
          get label() { return t("menu.win.resize"); },
          shortcut: "",
          action: () => {
            resizeAll();
            toast(t("toast.resize"), "ok", 1500);
          },
        },
      ],
      language: SUPPORTED_LANGS.map((item) => ({
        label: `${item.label} (${item.englishName})`,
        checked: () => state.lang === item.id,
        action: () => setLanguage(item.id, true),
      })),
      help: [
        {
          get label() { return t("menu.help.shortcuts"); },
          shortcut: "F1",
          action: () => showShortcutsModal(),
        },
        {
          get label() { return t("menu.help.prefs"); },
          shortcut: "Ctrl+,",
          action: () => showPreferencesModal(),
        },
        { divider: true },
        {
          get label() { return t("menu.help.about"); },
          shortcut: "",
          action: () => showAboutModal(),
        },
      ],
    };

    function openDropdown(menuName, buttonEl) {
      const dropdown = el("menu-dropdown");
      if (!dropdown) return;
      const items = MENUS[menuName];
      if (!items || items.length === 0) return;

      activeMenuName = menuName;
      document.querySelectorAll(".audition-menubar .menu-item").forEach((item) => {
        item.classList.toggle("active", item.getAttribute("data-menu") === menuName);
      });

      dropdown.innerHTML = "";
      items.forEach((item) => {
        if (item.divider) {
          const div = document.createElement("div");
          div.className = "menu-dropdown-divider";
          dropdown.appendChild(div);
        } else {
          const row = document.createElement("div");
          row.className = "menu-dropdown-item";

          const check = document.createElement("span");
          check.className = "menu-dropdown-check";
          const isChecked = typeof item.checked === "function" ? item.checked() : Boolean(item.checked);
          check.textContent = isChecked ? "✓" : "";
          row.appendChild(check);

          const label = document.createElement("span");
          label.className = "menu-dropdown-label";
          label.textContent = item.label;
          row.appendChild(label);

          if (item.shortcut) {
            const sc = document.createElement("span");
            sc.className = "menu-dropdown-shortcut";
            sc.textContent = item.shortcut;
            row.appendChild(sc);
          }

          row.onclick = (e) => {
            e.stopPropagation();
            closeDropdown();
            if (typeof item.action === "function") {
              item.action();
            }
          };
          dropdown.appendChild(row);
        }
      });

      const rect = buttonEl.getBoundingClientRect();
      dropdown.style.left = `${Math.round(rect.left)}px`;
      dropdown.style.top = `${Math.round(rect.bottom + 2)}px`;
      dropdown.hidden = false;
    }

    document.querySelectorAll(".audition-menubar .menu-item").forEach((btn) => {
      const name = btn.getAttribute("data-menu");
      btn.addEventListener("click", (e) => {
        e.stopPropagation();
        if (activeMenuName === name) {
          closeDropdown();
        } else {
          openDropdown(name, btn);
        }
      });
      btn.addEventListener("mouseenter", () => {
        if (activeMenuName && activeMenuName !== name) {
          openDropdown(name, btn);
        }
      });
    });

    window.addEventListener("click", (e) => {
      if (activeMenuName && !e.target.closest("#menu-dropdown") && !e.target.closest(".audition-menubar")) {
        closeDropdown();
      }
    });

    // Theme toggle button in menubar
    const themeBtn = el("btn-theme-toggle");
    if (themeBtn) {
      themeBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        closeDropdown();
        toggleThemeMode();
      });
    }

    // Wave color toggle button in menubar
    const waveColorBtn = el("btn-wave-color-toggle");
    if (waveColorBtn) {
      waveColorBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        closeDropdown();
        toggleWaveColor();
      });
    }

    // Language toggle button in menubar
    const langBtn = el("btn-lang-toggle");
    if (langBtn) {
      langBtn.addEventListener("click", (e) => {
        e.stopPropagation();
        if (activeMenuName === "language") {
          closeDropdown();
        } else {
          openDropdown("language", langBtn);
        }
      });
    }

    // Close button & backdrop for modal
    const closeBtn = el("modal-close-btn");
    if (closeBtn) closeBtn.onclick = closeModal;
    const backdrop = el("audition-modal-backdrop");
    if (backdrop) {
      backdrop.onclick = (e) => {
        if (e.target === backdrop) closeModal();
      };
    }
  }

  // ── boot ──────────────────────────────────────────────────────────────────
  function boot() {
    try {
      const savedLang = localStorage.getItem("echo:audition:lang");
      if (savedLang && I18N[savedLang]) {
        applyLanguage(savedLang, false);
      } else {
        const navLang = (navigator.language || "").toLowerCase();
        if (navLang.startsWith("ja")) applyLanguage("ja", false);
        else if (navLang.startsWith("fr")) applyLanguage("fr", false);
        else if (navLang.startsWith("ko")) applyLanguage("ko", false);
        else if (navLang.startsWith("en")) applyLanguage("en", false);
        else applyLanguage("zh", false);
      }
      const savedTheme = localStorage.getItem("echo:audition:themeMode");
      if (savedTheme === "light" || savedTheme === "dark") {
        setThemeMode(savedTheme, false);
      }
      const savedColor = localStorage.getItem("echo:audition:waveColor");
      if (savedColor === "blue" || savedColor === "pink") {
        setWaveColor(savedColor, false);
      }
    } catch {}
    ["nav-canvas", "ruler-canvas", "waveform-canvas", "spectrogram-canvas", "spectrum-canvas", "radar-canvas", "goniometer-canvas"]
      .forEach(setupCanvas);
    setupInteraction();
    setupAuditionMenubar();
    renderHistory();
    applyPlayback();
    applyVolume();
    updateScanUi();
    setConnectionState("ok", "等待宿主握手…");

    window.addEventListener("message", onMessage);
    window.addEventListener("pagehide", () => {
      if (rafId !== null) window.cancelAnimationFrame(rafId);
      rafId = null;
      window.removeEventListener("message", onMessage);
      state.connected = false;
    });
    document.addEventListener("visibilitychange", () => {
      if (document.hidden) {
        if (rafId !== null) window.cancelAnimationFrame(rafId);
        rafId = null;
      } else if (rafId === null) {
        rafId = window.requestAnimationFrame(frame);
      }
    });

    const observer = new ResizeObserver(() => {
      resizeAll();
    });
    observer.observe(document.documentElement);
    if (document.body) observer.observe(document.body);
    window.addEventListener("resize", resizeAll);

    void loadStorage().then(() => {
      renderHistory();
      applyVolume();
    });

    rafId = window.requestAnimationFrame(frame);
    post({ type: "echo:workshop-ui:ready" });
    try {
      parent.postMessage({ type: "echo:audition:getQueue" }, "*");
    } catch (_) {}
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", boot, { once: true });
  } else {
    boot();
  }
})();
