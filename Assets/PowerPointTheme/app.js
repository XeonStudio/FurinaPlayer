/**
 * Microsoft PowerPoint Theme for ECHO Workshop
 * Presentation Skin - Fluent UI SVG Edition
 * v1.1.0
 */

(function () {
  'use strict';

  // --- Global State ---
  const state = {
    playback: {
      state: 'stopped', // 'playing', 'paused', 'stopped'
      currentTrackId: null,
      positionSeconds: 0,
      durationSeconds: 0,
      volume: 1,
      shuffleEnabled: false,
      repeatMode: 'off' // 'off', 'one', 'all'
    },
    currentTrack: null,
    queue: {
      items: [],
      currentQueueId: null,
      currentIndex: 0
    },
    lyrics: {
      kind: 'empty',
      lines: [],
      currentIndex: -1
    },
    audio: {
      peakDb: -60,
      rmsDb: -60,
      frozen: false,
      codec: 'FLAC',
      sampleRate: '44.1 kHz',
      bitDepth: '24-bit',
      bitrate: '1411 kbps',
      channels: '双声道立体声',
      engine: 'WASAPI Shared'
    },
    settings: {
      tone: 'light',
      bgOverlayOpacity: 20,
      bgBlur: 16,
      vinylSpeed: 'normal',
      showTranslation: true,
      zoomPercent: 100,
      slideLayout: 'ppt-vinyl',
      activeRibbonTab: 'home',
      leftQueueVisible: true,
      notesVisible: true,
      autoSave: true
    },
    libraryTracks: [],
    playlists: [],
    folders: [],
    albums: [],
    playlistCache: new Map(),
    albumCache: new Map(),
    currentLang: 'zh-CN'
  };

  const queueCoverCache = new Map();

  // --- DOM Elements Cache ---
  const $ = (id) => document.getElementById(id);

  const els = {
    docTitle: $('ppt-doc-title'),
    btnThemeToggle: $('btn-theme-toggle'),
    themeLabel: $('theme-label'),

    // Search Box Elements
    searchWrap: $('ppt-search-box-wrap'),
    searchInput: $('ppt-search-input'),
    btnSearchClear: $('btn-search-clear'),
    searchDropdown: $('ppt-search-dropdown'),
    searchResultsList: $('search-results-list'),

    // Quick Access Toolbar
    chkAutosave: $('chk-autosave'),
    btnQaSave: $('btn-qa-save'),
    btnQaUndo: $('btn-qa-undo'),
    btnQaRedo: $('btn-qa-redo'),
    btnQaSlideshow: $('btn-qa-slideshow'),

    // Ribbon Tabs
    ribbonTabs: document.querySelectorAll('.ribbon-tab'),
    ribbonPanels: document.querySelectorAll('.ribbon-panel'),

    // Ribbon Home Controls
    btnCopyTitle: $('btn-copy-title'),
    btnCopyLyrics: $('btn-copy-lyrics'),
    btnNextSlide: $('btn-next-slide'),
    btnPrevSlide: $('btn-prev-slide'),
    btnResetLayout: $('btn-reset-layout'),
    selLyricsFont: $('sel-lyrics-font'),
    selLyricsSize: $('sel-lyrics-size'),
    btnFontInc: $('btn-font-increase'),
    btnFontDec: $('btn-font-decrease'),
    btnStyleBold: $('btn-style-bold'),
    btnStyleItalic: $('btn-style-italic'),
    btnStyleUnderline: $('btn-style-underline'),
    btnStyleShadow: $('btn-style-shadow'),
    colorLyricsText: $('color-lyrics-text'),
    textColorBar: $('text-color-bar'),
    colorLyricsHl: $('color-lyrics-highlight'),
    hlColorBar: $('hl-color-bar'),
    btnAlignLeft: $('btn-align-left'),
    btnAlignCenter: $('btn-align-center'),
    btnAlignRight: $('btn-align-right'),
    selLineHeight: $('sel-line-height'),
    btnToggleTranslation: $('btn-toggle-translation'),
    rngBgOverlay: $('rng-bg-overlay'),
    lblOverlayVal: $('lbl-overlay-val'),
    selVinylSpeed: $('sel-vinyl-speed'),
    btnFullscreenShow: $('btn-fullscreen-show'),

    // Ribbon Edit Controls
    btnEditCopyTitle: $('btn-edit-copy-title'),
    btnEditCopyArtist: $('btn-edit-copy-artist'),
    btnEditCopyMeta: $('btn-edit-copy-meta'),
    btnEditCopyTimecode: $('btn-edit-copy-timecode'),
    btnEditResetPeak: $('btn-edit-reset-peak'),
    btnEditRescan: $('btn-edit-rescan'),
    btnEditClearLyrics: $('btn-edit-clear-lyrics'),

    // Ribbon Multitrack Controls
    btnMultiSyncQueue: $('btn-multi-sync-queue'),
    btnMultiRealtime: $('btn-multi-realtime'),
    btnMultiHighQuality: $('btn-multi-high-quality'),
    btnMultiStereo: $('btn-multi-stereo'),
    btnMultiSpectrumMode: $('btn-multi-spectrum-mode'),

    // Ribbon Clip Controls
    btnClipCenter: $('btn-clip-center'),
    btnClipStart: $('btn-clip-start'),
    btnClipEnd: $('btn-clip-end'),
    btnClipRw5: $('btn-clip-rw5'),
    btnClipRw15: $('btn-clip-rw15'),
    btnClipFf5: $('btn-clip-ff5'),
    btnClipFf15: $('btn-clip-ff15'),

    // Ribbon Effects Controls
    btnFxVstRack: $('btn-fx-vst-rack'),
    btnFxVstMgr: $('btn-fx-vst-mgr'),
    btnFxFreeze: $('btn-fx-freeze'),
    btnFxResetSpec: $('btn-fx-reset-spec'),

    // Ribbon Zoom Controls
    btnZoomFitRibbon: $('btn-zoom-fit-ribbon'),
    btnZoomInRibbon: $('btn-zoom-in-ribbon'),
    btnZoomOutRibbon: $('btn-zoom-out-ribbon'),
    btnViewToggleQueue: $('btn-view-toggle-queue'),
    btnViewToggleNotes: $('btn-view-toggle-notes'),

    // Ribbon Transport Controls
    btnTransPlaypause: $('btn-trans-playpause'),
    svgTransPlaypause: $('svg-trans-playpause'),
    btnTransPrev: $('btn-trans-prev'),
    btnTransNext: $('btn-trans-next'),
    btnTransCycleRepeat: $('btn-trans-cycle-repeat'),
    btnTransToggleShuffle: $('btn-trans-toggle-shuffle'),
    btnTransMuteToggle: $('btn-trans-mute-toggle'),
    btnTransVol100: $('btn-trans-vol-100'),

    // Ribbon Window Controls
    btnWinThemeLight: $('btn-win-theme-light'),
    btnWinThemeDark: $('btn-win-theme-dark'),
    btnWinModalOptions: $('btn-win-modal-options'),

    // Ribbon Help Controls
    btnHelpAbout: $('btn-help-about'),
    btnHelpShortcuts: $('btn-help-shortcuts'),
    btnHelpReload: $('btn-help-reload'),

    // Workspace & Slide
    pptThumbnailsPanel: $('ppt-thumbnails-panel'),
    thumbnailsList: $('thumbnails-list'),
    queueCountBadge: $('queue-count-badge'),
    slideContainer: $('ppt-slide-container'),
    pptSlide: $('ppt-slide'),
    slideBgCover: $('slide-bg-cover'),
    slideBgOverlay: $('slide-bg-overlay'),

    // Vinyl
    vinylRecordDisc: $('vinyl-record-disc'),
    coverImg: $('cover-img'),
    turntableTonearm: $('turntable-tonearm'),
    vinylStatusPill: $('vinyl-status-pill'),
    metaTrackTitle: $('meta-track-title'),
    metaTrackArtist: $('meta-track-artist'),
    metaTrackAlbum: $('meta-track-album'),

    // Audio Props
    propCodec: $('prop-codec'),
    propSamplerate: $('prop-samplerate'),
    propBitdepth: $('prop-bitdepth'),
    propBitrate: $('prop-bitrate'),
    propChannels: $('prop-channels'),
    propBackend: $('prop-backend'),
    vuDbText: $('vu-db-text'),
    vuMeterFill: $('vu-meter-fill'),

    // Lyrics
    lyricsScrollContainer: $('lyrics-scroll-container'),
    lyricsLinesList: $('lyrics-lines-list'),
    lyricStateHint: $('lyric-state-hint'),

    // Bottom Notes & Player Bar
    pptNotesBar: $('ppt-notes-bar'),
    notesInput: $('notes-input'),
    pcTitle: $('pc-title'),
    pcArtist: $('pc-artist'),
    btnTShuffle: $('btn-t-shuffle'),
    btnTPrev: $('btn-t-prev'),
    btnTPlay: $('btn-t-play'),
    icoPlayMain: $('ico-play-main'),
    btnTNext: $('btn-t-next'),
    btnTRepeat: $('btn-t-repeat'),
    timeCurrent: $('time-current'),
    timeTotal: $('time-total'),
    rngProgress: $('rng-progress'),
    btnMute: $('btn-mute'),
    rngVolume: $('rng-volume'),
    lblVolume: $('lbl-volume'),

    // Statusbar
    sbSlideCounter: $('sb-slide-counter'),
    sbLyricStatsText: $('sb-lyric-stats-text'),
    btnSbNotes: $('btn-sb-notes'),
    btnViewSlideshow: $('btn-view-slideshow'),
    rngZoom: $('rng-zoom'),

    // Floating Slideshow HUD
    slideshowHud: $('slideshow-hud'),
    btnHudPrev: $('btn-hud-prev'),
    btnHudPlay: $('btn-hud-play'),
    btnHudNext: $('btn-hud-next'),
    btnHudExit: $('btn-hud-exit'),
    lblZoom: $('lbl-zoom'),
    btnZoomIn: $('btn-zoom-in'),
    btnZoomOut: $('btn-zoom-out'),
    btnFitWindow: $('btn-fit-window'),

    // Stage Floating Controls
    turntableComponent: $('turntable-component'),
    rngBgBlur: $('rng-bg-blur'),
    lblBlurVal: $('lbl-blur-val'),

    // Slide Layouts & Elements
    slideLayoutVinyl: $('slide-layout-vinyl'),
    slideLayoutNetease: $('slide-layout-netease'),
    slideLayoutAppleMusic: $('slide-layout-applemusic'),
    amFluidMeshBg: $('am-fluid-mesh-bg'),
    btnLayoutPptVinyl: $('btn-layout-ppt-vinyl'),
    btnLayoutNetease: $('btn-layout-netease'),
    btnLayoutAppleMusic: $('btn-layout-applemusic'),

    // NetEase Layout Elements
    neCoverImg: $('ne-cover-img'),
    neTrackTitle: $('ne-track-title'),
    neMetaAlbum: $('ne-meta-album'),
    neMetaArtist: $('ne-meta-artist'),
    neMetaSource: $('ne-meta-source'),
    neLyricsScroll: $('ne-lyrics-scroll'),
    neLyricsLines: $('ne-lyrics-lines'),

    // Apple Music Layout Elements
    amCoverImg: $('am-cover-img'),
    amTrackTitle: $('am-track-title'),
    amTrackArtistAlbum: $('am-track-artist-album'),
    amProgressSlider: $('am-progress-slider'),
    amTimeCur: $('am-time-cur'),
    amTimeRem: $('am-time-rem'),
    amBtnShuffle: $('am-btn-shuffle'),
    amBtnPrev: $('am-btn-prev'),
    amBtnPlay: $('am-btn-play'),
    amIcoPlay: $('am-ico-play'),
    amBtnNext: $('am-btn-next'),
    amBtnRepeat: $('am-btn-repeat'),
    amVolumeSlider: $('am-volume-slider'),
    amLyricsScroll: $('am-lyrics-scroll'),
    amLyricsLines: $('am-lyrics-lines'),
    amLyricsBubbleBtn: $('am-lyrics-bubble-btn'),

    // Design Ribbon Controls
    designRngOverlay: $('design-rng-overlay'),
    designLblOverlay: $('design-lbl-overlay'),
    designRngBlur: $('design-rng-blur'),
    designLblBlur: $('design-lbl-blur'),
    btnDesignThemeToggle: $('btn-design-theme-toggle'),
    btnDesignResetBg: $('btn-design-reset-bg'),

    // Language Switcher
    sbLangPill: $('sb-lang-pill'),
    sbLangText: $('sb-lang-text'),
    sbLangDropdown: $('sb-lang-dropdown'),

    // Modal Backstage
    modalBackdrop: $('modal-backdrop'),
    btnModalBack: $('btn-modal-back'),
    mTrackTitle: $('m-track-title'),
    mTrackArtist: $('m-track-artist'),
    mTrackAlbum: $('m-track-album'),
    mTrackPath: $('m-track-path'),
    mPlayState: $('m-play-state'),
    mAudioSpecs: $('m-audio-specs'),
    mAudioCodec: $('m-audio-codec'),
    mAudioSamplerate: $('m-audio-samplerate'),
    mAudioBitdepth: $('m-audio-bitdepth'),
    mAudioChannels: $('m-audio-channels'),
    mAudioBitrate: $('m-audio-bitrate'),
    mAudioBackend: $('m-audio-backend'),
    mAudioProgress: $('m-audio-progress'),
    mAudioLevels: $('m-audio-levels'),
    mAudioVolume: $('m-audio-volume'),
    mLyricsStatus: $('m-lyrics-status'),
    mLyricsWords: $('m-lyrics-words'),
    mPresZoom: $('m-pres-zoom'),
    mPresBlur: $('m-pres-blur'),

    // Modal Playlists Explorer
    paneModalPlaylists: $('pane-modal-playlists'),
    modalPlaylistsTree: $('modal-playlists-tree'),
    iptPlaylistsSearch: $('ipt-playlists-search'),
    btnRefreshPlaylists: $('btn-refresh-playlists'),

    // Toast
    toast: $('ppt-toast')
  };

  // --- Utility Functions ---
  function formatSeconds(secs) {
    if (isNaN(secs) || secs < 0) return '00:00';
    const m = Math.floor(secs / 60);
    const s = Math.floor(secs % 60);
    return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  }

  function showToast(message, durationMs = 2400) {
    if (!els.toast) return;
    els.toast.textContent = message;
    els.toast.hidden = false;
    clearTimeout(els.toast._timer);
    els.toast._timer = setTimeout(() => {
      els.toast.hidden = true;
    }, durationMs);
  }

  // --- Internationalization (i18n) Engine ---
  const I18N = {
    'zh-CN': {
      tabFile: '文件(F)', tabHome: '开始(H)', tabEdit: '编辑(E)', tabMultitrack: '多轨(M)', tabClip: '剪辑(C)',
      tabEffects: '效果(S)', tabZoom: '缩放(Z)', tabTransport: '控制(T)', tabWindow: '窗口(W)', tabHelp: '帮助(H)', tabDesign: '设计(G)',
      autosave: '自动保存', autoSave: '自动保存', searchResultsHeader: '曲库与队列搜索结果', searchHeader: '曲库与队列搜索结果',
      btnNewSlide: '下一首', subNextTrack: '幻灯片',
      grpClipboard: '剪贴板', grpSlides: '幻灯片', grpFont: '字体 (歌词定制)', grpParagraph: '段落与对齐',
      grpDesignMask: '设计与遮罩', grpSlideShow: '放映', grpClipData: '剪贴板数据', grpStateReset: '状态重置',
      grpRenderQueue: '渲染与队列', grpSoundstage: '音频声场', grpPlayhead: '播放针定位', grpTimeJump: '时间跳转',
      grpVstPlugins: 'VST3 音频插件', grpAnalyzerCtrl: '分析器控制', grpSlideZoom: '幻灯片缩放', grpWorkspaceView: '工作区视图',
      grpBasicTransport: '基础走带', grpPlayModeVol: '播放模式与音量', grpColorTheme: '色彩主题', grpSettingsCenter: '设置中心',
      grpDocsSupport: '文档与支持', grpSlideLayoutStyle: '幻灯片版式样式', grpBgVariants: '背景与变体',
      btnCopyLyrics: '复制歌词', btnCopyTitle: '复制歌名', btnCopyArtist: '复制歌手', btnCopyMeta: '复制元数据', btnCopyTimecode: '复制时间码',
      btnNextSlide: '下一首', btnPrevSlide: '上一首', btnResetLayout: '重置版式', transPill: '译/音',
      lblOverlay: '底纹遮罩:', lblBlur: '高斯模糊:', lblVinylSpeed: '唱片转速:', btnSlideShow: '幻灯片放映',
      btnResetPeak: '重置峰值', btnRescan: '重新扫描', btnRefreshLyrics: '刷新歌词',
      btnSyncQueue: '同步队列', btnRealtime: '实时监听', btnHiRes: '高精采样', btnStereoOut: '立体声输出', btnSpectrumBars: '频谱分析柱',
      btnCenterPin: '居中指针', btnCenterPinSub: '定位高亮', btnSeekStart: '回到开头', btnSeekEnd: '快进至尾',
      btnRw5: '快退 5 秒', btnRw15: '快退 15 秒', btnFf5: '快进 5 秒', btnFf15: '快进 15 秒',
      btnVstRack: 'VST3 机架', btnVstMgr: '插件管理', btnFreezeSpec: '冻结分析', btnClearLevels: '清空电平',
      btnZoomFit: '适合窗口', btnZoomIn: '放大', btnZoomOut: '缩小', btnToggleQueue: '左侧队列栏', btnToggleNotes: '演讲备注栏',
      btnPlayPause: '播放 / 暂停', btnPrevTrack: '上一首', btnNextTrack: '下一首',
      btnRepeatMode: '循环模式', btnShufflePlay: '随机播放', btnMuteToggle: '静音切换', btnMaxVolume: '最大音量',
      btnLightMode: '明亮模式', btnLightModeSub: '经典办公', btnDarkMode: '暗黑模式', btnDarkModeSub: '护眼深色',
      btnPersonalizeSub: '个性化', btnShortcuts: '快捷键指南', btnReloadRuntime: '重新加载',
      layoutVinylTitle: '经典黑胶', layoutVinylDesc: '唱机与参数', layoutNeteaseTitle: '网易云样式', layoutNeteaseDesc: '大封面与歌词',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: '动感渐变全景', btnThemeMode: '主题模式', btnResetBg: '重置背景',
      queuePanelTitle: '幻灯片播放队列', neAlbum: '专辑:', neArtist: '歌手:', neSource: '来源:', lyricsWord: '歌词',
      spellCheckOk: '无拼写错误', playedWordPrefix: '已播', totalWordPrefix: '共', charUnit: '字',
      lh12: '单倍行距', lh15: '1.5 倍行距', lh18: '1.8 倍行距', lh20: '2.0 双倍行距',
      rpm33: '33 RPM (沉浸)', rpm45: '45 RPM (标准)', rpm78: '78 RPM (轻快)', rpmStop: '暂停旋转',
      fontYaHei: '微软雅黑 (Microsoft YaHei)', fontSimSun: '宋体 (SimSun)', fontKaiTi: '楷体 (KaiTi)',
      fontFangSong: '仿宋 (FangSong)', fontGeorgia: 'Georgia (优雅衬线)', fontConsolas: 'Consolas (等宽极简)',
      optTitle: 'PowerPoint 主题选项与个性化设置', optSub: '定制幻灯片放映视觉效果、排版偏好与黑胶播放器运行行为。',
      optThemeCard: '外观主题色彩', optToneLight: 'Office 经典明亮 (白/红)', optToneDark: 'Office 经典暗黑 (黑/红)',
      optOverlayCard: '幻灯片专辑底图白色遮罩', optOverlayHint: '调节专辑底图之上的白色遮罩不透明度，兼顾专辑艺术视觉与歌词排版对比度。',
      optVinylCard: '黑胶唱片机旋转选项', optTransCard: '歌词翻译与罗马音', optChkTrans: '开启双语翻译 / 罗马音副标题展示',
      aboutTitle: '关于 Microsoft PowerPoint ECHO 创意工坊外观', aboutSub: '专为 ECHO 音乐播放器打造的 Microsoft PowerPoint 经典演示文稿主题界面。',
      aboutNameLbl: '插件名称：', aboutNameVal: 'Microsoft PowerPoint · 演示文稿外观主题',
      aboutIdLbl: '插件 ID：', aboutVerLbl: '当前版本：', aboutProtoLbl: '设计原型：',
      aboutProtoVal: 'PowerPoint Fluent Ribbon + 左侧缩略图队列 + 旋转黑胶 + 底图遮罩 + 动态歌词排版',
      aboutRefLbl: '蓝本参考：', aboutAuthorLbl: '作者：', aboutProtocolLbl: '工坊协议：',
      notesPlaceholder: '单击此处添加演示文稿备注 (当前曲目信息与备忘录)',
      searchPlaceholder: '搜索演示文稿与曲库 (Alt+Q)...', notes: '备注', comments: '批注', fitWindow: '适应',
      slideCounter: '幻灯片', of: '共', sheetUnit: '张', songUnit: '首', pureMusic: '纯音乐',
      nowPlaying: '正在播放', badgeQueue: '当前队列', badgeLibrary: '本地曲库', localFolder: '本地目录',
      navInfo: '信息', navPlaylists: '打开与导入歌单', navOptions: '主题选项', navAbout: '关于插件',
      infoTitle: 'Microsoft PowerPoint · 音频与文档信息',
      infoSub: '当前演示文稿音轨属性、硬件规格与宿主运行状态概览。',
      infoSecTrack: '曲目与元数据', infoTrackTitle: '当前曲目：', infoTrackArtist: '艺术家：',
      infoTrackAlbum: '专辑：', infoTrackPath: '存储路径：', infoSecAudio: '音频硬件与解码规格',
      infoCodec: '编码格式：', infoSampleRate: '采样率：', infoBitDepth: '量化位深：',
      infoChannels: '声道配置：', infoBitrate: '瞬时比特率：', infoBackend: '音频输出后端：',
      infoSecEngine: '播放引擎与渲染状态', infoPlayState: '播放状态：', infoProgress: '当前进度：',
      infoLevels: '峰值与 RMS：', infoVolume: '主音量：', infoLyricsType: '歌词状态：',
      infoWordStats: '歌词字数：', infoPresentationZoom: '画布缩放：', infoBlur: '高斯模糊：',
      playlistsTitle: '打开与导入歌单',
      playlistsSub: '浏览本地曲库、用户歌单与本地专辑组合，以文件夹目录树层级呈现，支持一键载入幻灯片播放队列。',
      filterPlaylists: '输入关键字过滤歌单、专辑或曲目...', btnRefresh: '刷新曲库', btnReturn: '返回演示文稿',
      localAlbums: '本地专辑组合', customPlaylists: '自定义歌单', albumUnit: '张',
      importAndPlay: '全部导入并播放', appendToQueue: '追加至队列', loadingPlaylists: '正在读取曲库、歌单与专辑目录...',
      loadingTracks: '正在展开曲目清单...', emptyPlaylist: '歌单为空', emptyLibraryHint: '当前曲库或歌单暂无曲目，请先在播放器中添加本地音乐或歌单。',
      toastVstRack: '已唤起 VST3 效果机架', toastVstMgr: '正在打开 VST3 插件管理器...',
      toastImporting: '正在导入歌单并播放', toastAppended: '已追加至队列',
      fontGroupCurated: '常用精选字体', fontGroupSystem: '系统已安装字体',
      pointerCentered: '播放指针已居中定位', infoPlayStatePaused: '已暂停',
      noSyncedLyrics: '暂无同步滚动歌词', enjoyMelody: '纯音乐，请享受旋律'
    },
    'en-US': {
      tabFile: 'File(F)', tabHome: 'Home(H)', tabEdit: 'Edit(E)', tabMultitrack: 'Multitrack(M)', tabClip: 'Clip(C)',
      tabEffects: 'Effects(S)', tabZoom: 'Zoom(Z)', tabTransport: 'Transport(T)', tabWindow: 'Window(W)', tabHelp: 'Help(H)', tabDesign: 'Design(G)',
      autosave: 'AutoSave', autoSave: 'AutoSave', searchResultsHeader: 'Library & Queue Search Results', searchHeader: 'Library & Queue Search Results',
      btnNewSlide: 'Next Track', subNextTrack: 'Slide',
      grpClipboard: 'Clipboard', grpSlides: 'Slides', grpFont: 'Font (Lyrics)', grpParagraph: 'Paragraph & Align',
      grpDesignMask: 'Design & Mask', grpSlideShow: 'Slide Show', grpClipData: 'Clipboard Data', grpStateReset: 'State Reset',
      grpRenderQueue: 'Render & Queue', grpSoundstage: 'Soundstage', grpPlayhead: 'Playhead Position', grpTimeJump: 'Time Jump',
      grpVstPlugins: 'VST3 Audio Plugins', grpAnalyzerCtrl: 'Analyzer Control', grpSlideZoom: 'Slide Zoom', grpWorkspaceView: 'Workspace View',
      grpBasicTransport: 'Transport', grpPlayModeVol: 'Play Mode & Volume', grpColorTheme: 'Color Theme', grpSettingsCenter: 'Settings Center',
      grpDocsSupport: 'Docs & Support', grpSlideLayoutStyle: 'Slide Layout Style', grpBgVariants: 'Background & Variants',
      btnCopyLyrics: 'Copy Lyrics', btnCopyTitle: 'Copy Title', btnCopyArtist: 'Copy Artist', btnCopyMeta: 'Copy Metadata', btnCopyTimecode: 'Copy Timecode',
      btnNextSlide: 'Next Slide', btnPrevSlide: 'Prev Slide', btnResetLayout: 'Reset Layout', transPill: 'Trans',
      lblOverlay: 'Mask Overlay:', lblBlur: 'Gaussian Blur:', lblVinylSpeed: 'Vinyl Speed:', btnSlideShow: 'Slide Show',
      btnResetPeak: 'Reset Peak', btnRescan: 'Rescan', btnRefreshLyrics: 'Refresh Lyrics',
      btnSyncQueue: 'Sync Queue', btnRealtime: 'Monitor', btnHiRes: 'Hi-Res Audio', btnStereoOut: 'Stereo Out', btnSpectrumBars: 'Spectrum Bars',
      btnCenterPin: 'Center Playhead', btnCenterPinSub: 'Focus Active', btnSeekStart: 'To Start', btnSeekEnd: 'To End',
      btnRw5: 'Rewind 5s', btnRw15: 'Rewind 15s', btnFf5: 'Forward 5s', btnFf15: 'Forward 15s',
      btnVstRack: 'VST3 Rack', btnVstMgr: 'Plugin Mgr', btnFreezeSpec: 'Freeze Spec', btnClearLevels: 'Clear Levels',
      btnZoomFit: 'Fit Window', btnZoomIn: 'Zoom In', btnZoomOut: 'Zoom Out', btnToggleQueue: 'Queue Pane', btnToggleNotes: 'Notes Pane',
      btnPlayPause: 'Play / Pause', btnPrevTrack: 'Previous', btnNextTrack: 'Next',
      btnRepeatMode: 'Repeat Mode', btnShufflePlay: 'Shuffle', btnMuteToggle: 'Mute', btnMaxVolume: 'Max Volume',
      btnLightMode: 'Light Mode', btnLightModeSub: 'Classic Office', btnDarkMode: 'Dark Mode', btnDarkModeSub: 'Night Dark',
      btnPersonalizeSub: 'Personalize', btnShortcuts: 'Shortcuts Guide', btnReloadRuntime: 'Reload UI',
      layoutVinylTitle: 'Classic Vinyl', layoutVinylDesc: 'Turntable & Specs', layoutNeteaseTitle: 'NetEase Style', layoutNeteaseDesc: 'Cover & Lyrics',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'Fluid Mesh Stage', btnThemeMode: 'Theme Mode', btnResetBg: 'Reset Bg',
      queuePanelTitle: 'Slide Playback Queue', neAlbum: 'Album:', neArtist: 'Artist:', neSource: 'Source:', lyricsWord: 'Lyrics',
      spellCheckOk: 'No Proofing Errors', playedWordPrefix: 'Played', totalWordPrefix: 'Total', charUnit: 'chars',
      lh12: '1.0x Single', lh15: '1.5x Line Space', lh18: '1.8x Line Space', lh20: '2.0x Double',
      rpm33: '33 RPM (Slow)', rpm45: '45 RPM (Standard)', rpm78: '78 RPM (Fast)', rpmStop: 'Pause Spin',
      fontYaHei: 'Microsoft YaHei', fontSimSun: 'SimSun Serif', fontKaiTi: 'KaiTi Brush',
      fontFangSong: 'FangSong Serif', fontGeorgia: 'Georgia Serif', fontConsolas: 'Consolas Mono',
      optTitle: 'PowerPoint Theme Options & Personalization', optSub: 'Customize slide visual effects, typography preferences, and turntable behavior.',
      optThemeCard: 'Appearance Theme Tone', optToneLight: 'Office Classic Light (White/Red)', optToneDark: 'Office Classic Dark (Black/Red)',
      optOverlayCard: 'Slide Cover White Mask Overlay', optOverlayHint: 'Adjust translucent mask opacity over album cover art for optimal lyric contrast.',
      optVinylCard: 'Vinyl Turntable Rotation Speed', optTransCard: 'Lyrics Translation & Romanization', optChkTrans: 'Show bilingual translation / romanized subtitle lines',
      aboutTitle: 'About Microsoft PowerPoint ECHO Workshop Skin', aboutSub: 'Classic Microsoft PowerPoint presentation workspace skin built for ECHO Music Player.',
      aboutNameLbl: 'Plugin Name: ', aboutNameVal: 'Microsoft PowerPoint · Presentation Skin',
      aboutIdLbl: 'Plugin ID: ', aboutVerLbl: 'Version: ', aboutProtoLbl: 'Design Prototype: ',
      aboutProtoVal: 'PowerPoint Fluent Ribbon + Thumbnail Queue + Vinyl Turntable + Dynamic Lyrics',
      aboutRefLbl: 'Reference Blueprint: ', aboutAuthorLbl: 'Author: ', aboutProtocolLbl: 'Workshop Protocol: ',
      notesPlaceholder: 'Click to add presenter notes (Current track info & memo)',
      searchPlaceholder: 'Search presentations & library (Alt+Q)...', notes: 'Notes', comments: 'Comments', fitWindow: 'Fit',
      slideCounter: 'Slide', of: 'of', sheetUnit: 'slides', songUnit: 'tracks', pureMusic: 'Instrumental',
      nowPlaying: 'Now Playing', badgeQueue: 'Queue', badgeLibrary: 'Library', localFolder: 'Local Folder',
      navInfo: 'Info', navPlaylists: 'Open & Import Playlists', navOptions: 'Theme Options', navAbout: 'About Plugin',
      infoTitle: 'Microsoft PowerPoint · Audio & Document Info',
      infoSub: 'Overview of presentation audio track, hardware specs and host runtime state.',
      infoSecTrack: 'Track & Metadata', infoTrackTitle: 'Current Track: ', infoTrackArtist: 'Artist: ',
      infoTrackAlbum: 'Album: ', infoTrackPath: 'Storage Path: ', infoSecAudio: 'Audio Hardware & Decoding',
      infoCodec: 'Codec: ', infoSampleRate: 'Sample Rate: ', infoBitDepth: 'Bit Depth: ',
      infoChannels: 'Channels: ', infoBitrate: 'Bitrate: ', infoBackend: 'Output Backend: ',
      infoSecEngine: 'Engine & Render State', infoPlayState: 'Playback State: ', infoProgress: 'Progress: ',
      infoLevels: 'Peak & RMS: ', infoVolume: 'Master Volume: ', infoLyricsType: 'Lyrics Status: ',
      infoWordStats: 'Lyric Words: ', infoPresentationZoom: 'Slide Zoom: ', infoBlur: 'Gaussian Blur: ',
      playlistsTitle: 'Open & Import Playlists',
      playlistsSub: 'Browse local library, albums, and playlists in a folder tree and import directly to slide queue.',
      filterPlaylists: 'Filter playlists, albums, or tracks...', btnRefresh: 'Refresh Library', btnReturn: 'Return to Presentation',
      localAlbums: 'Local Albums', customPlaylists: 'Custom Playlists', albumUnit: 'albums',
      importAndPlay: 'Import All & Play', appendToQueue: 'Append to Queue', loadingPlaylists: 'Reading library, albums, and playlists...',
      loadingTracks: 'Loading track list...', emptyPlaylist: 'Playlist is empty', emptyLibraryHint: 'No playlists or library tracks found. Add tracks in ECHO player first.',
      toastVstRack: 'VST3 Effects Rack toggled', toastVstMgr: 'Opening VST3 Plugin Manager...',
      toastImporting: 'Importing playlist and playing', toastAppended: 'Appended to queue',
      fontGroupCurated: 'Curated Fonts', fontGroupSystem: 'Installed System Fonts',
      pointerCentered: 'Playback pointer centered', infoPlayStatePaused: 'Paused',
      noSyncedLyrics: 'No synchronized lyrics available', enjoyMelody: 'Instrumental, enjoy the music'
    },
    'fr-FR': {
      tabFile: 'Fichier(F)', tabHome: 'Accueil(H)', tabEdit: 'Édition(E)', tabMultitrack: 'Multipiste(M)', tabClip: 'Clip(C)',
      tabEffects: 'Effets(S)', tabZoom: 'Zoom(Z)', tabTransport: 'Contrôle(T)', tabWindow: 'Fenêtre(W)', tabHelp: 'Aide(H)', tabDesign: 'Conception(G)',
      autosave: 'Enreg. auto', autoSave: 'Enreg. auto', searchResultsHeader: 'Résultats de recherche', searchHeader: 'Résultats de recherche',
      btnNewSlide: 'Suivante', subNextTrack: 'Diapositive',
      grpClipboard: 'Presse-papiers', grpSlides: 'Diapositives', grpFont: 'Police (Paroles)', grpParagraph: 'Paragraphe',
      grpDesignMask: 'Masque et flou', grpSlideShow: 'Diaporama', grpClipData: 'Données copiées', grpStateReset: 'Réinitialisation',
      grpRenderQueue: 'Rendu et file', grpSoundstage: 'Scène sonore', grpPlayhead: 'Position', grpTimeJump: 'Saut temporel',
      grpVstPlugins: 'Plugins VST3', grpAnalyzerCtrl: 'Analyseur', grpSlideZoom: 'Zoom diapo', grpWorkspaceView: 'Affichage',
      grpBasicTransport: 'Transport', grpPlayModeVol: 'Mode et volume', grpColorTheme: 'Thème couleur', grpSettingsCenter: 'Paramètres',
      grpDocsSupport: 'Support', grpSlideLayoutStyle: 'Disposition des diapos', grpBgVariants: 'Arrière-plan',
      btnCopyLyrics: 'Copier paroles', btnCopyTitle: 'Copier titre', btnCopyArtist: 'Copier artiste', btnCopyMeta: 'Métadonnées', btnCopyTimecode: 'Code temporel',
      btnNextSlide: 'Suivante', btnPrevSlide: 'Précédente', btnResetLayout: 'Réinitialiser', transPill: 'Trad.',
      lblOverlay: 'Masque:', lblBlur: 'Flou:', lblVinylSpeed: 'Vitesse:', btnSlideShow: 'Diaporama',
      btnResetPeak: 'Réinit. crête', btnRescan: 'rescanner', btnRefreshLyrics: 'Actualiser paroles',
      btnSyncQueue: 'Sync. file', btnRealtime: 'Moniteur', btnHiRes: 'Haute rés.', btnStereoOut: 'Stéréo', btnSpectrumBars: 'Spectre',
      btnCenterPin: 'Centrer curseur', btnCenterPinSub: 'Focus actif', btnSeekStart: 'Au début', btnSeekEnd: 'À la fin',
      btnRw5: 'Reculer 5s', btnRw15: 'Reculer 15s', btnFf5: 'Avancer 5s', btnFf15: 'Avancer 15s',
      btnVstRack: 'Rack VST3', btnVstMgr: 'Gestion VST3', btnFreezeSpec: 'Figer analyse', btnClearLevels: 'Vider niveaux',
      btnZoomFit: 'Ajuster', btnZoomIn: 'Zoom avant', btnZoomOut: 'Zoom arrière', btnToggleQueue: 'Volet file', btnToggleNotes: 'Volet notes',
      btnPlayPause: 'Lecture / Pause', btnPrevTrack: 'Précédent', btnNextTrack: 'Suivant',
      btnRepeatMode: 'Répétition', btnShufflePlay: 'Aléatoire', btnMuteToggle: 'Muet', btnMaxVolume: 'Volume max',
      btnLightMode: 'Mode clair', btnLightModeSub: 'Classique', btnDarkMode: 'Mode sombre', btnDarkModeSub: 'Nuit',
      btnPersonalizeSub: 'Options', btnShortcuts: 'Raccourcis', btnReloadRuntime: 'Recharger',
      layoutVinylTitle: 'Vinyle classique', layoutVinylDesc: 'Platine et specs', layoutNeteaseTitle: 'Style NetEase', layoutNeteaseDesc: 'Pochette et paroles',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'Dégradé fluide', btnThemeMode: 'Mode thème', btnResetBg: 'Réinit. fond',
      queuePanelTitle: 'File de diapositives', neAlbum: 'Album:', neArtist: 'Artiste:', neSource: 'Source:', lyricsWord: 'Paroles',
      spellCheckOk: 'Aucune faute', playedWordPrefix: 'Lu', totalWordPrefix: 'Total', charUnit: 'car.',
      lh12: 'Simple (1.0)', lh15: '1.5 ligne', lh18: '1.8 ligne', lh20: 'Double (2.0)',
      rpm33: '33 RPM (Lent)', rpm45: '45 RPM (Normal)', rpm78: '78 RPM (Rapide)', rpmStop: 'Arrêt',
      fontYaHei: 'Microsoft YaHei', fontSimSun: 'SimSun Serif', fontKaiTi: 'KaiTi Script',
      fontFangSong: 'FangSong Serif', fontGeorgia: 'Georgia Serif', fontConsolas: 'Consolas Mono',
      optTitle: 'Options du thème PowerPoint', optSub: 'Personnalisez les effets visuels, la typographie et la platine vinyle.',
      optThemeCard: 'Couleur du thème', optToneLight: 'Office Clair (Blanc/Rouge)', optToneDark: 'Office Sombre (Noir/Rouge)',
      optOverlayCard: 'Masque blanc de diapositive', optOverlayHint: 'Ajustez l opacité du masque sur la pochette pour le contraste des paroles.',
      optVinylCard: 'Vitesse du vinyle', optTransCard: 'Traduction des paroles', optChkTrans: 'Afficher la traduction bilingue',
      aboutTitle: 'À propos du thème Microsoft PowerPoint ECHO', aboutSub: 'Interface de présentation classique PowerPoint conçue pour ECHO.',
      aboutNameLbl: 'Nom du plugin : ', aboutNameVal: 'Microsoft PowerPoint · Thème de présentation',
      aboutIdLbl: 'ID du plugin : ', aboutVerLbl: 'Version : ', aboutProtoLbl: 'Prototype : ',
      aboutProtoVal: 'Ruban Fluent PowerPoint + File de vignettes + Platine vinyle + Paroles dynamiques',
      aboutRefLbl: 'Référence : ', aboutAuthorLbl: 'Auteur : ', aboutProtocolLbl: 'Protocole Workshop : ',
      notesPlaceholder: 'Cliquez pour ajouter des notes de présentation',
      searchPlaceholder: 'Rechercher présentations et bibliothèque (Alt+Q)...', notes: 'Commentaires', comments: 'Notes', fitWindow: 'Ajuster',
      slideCounter: 'Diapositive', of: 'sur', sheetUnit: 'diapos', songUnit: 'pistes', pureMusic: 'Instrumental',
      nowPlaying: 'Lecture en cours', badgeQueue: 'File', badgeLibrary: 'Bibliothèque', localFolder: 'Dossier local',
      navInfo: 'Informations', navPlaylists: 'Ouvrir & Importer', navOptions: 'Options du thème', navAbout: 'À propos',
      infoTitle: 'Microsoft PowerPoint · Informations audio et document',
      infoSub: 'Vue d ensemble de la piste audio, des spécifications et de l état hôte.',
      infoSecTrack: 'Piste et Métadonnées', infoTrackTitle: 'Piste actuelle : ', infoTrackArtist: 'Artiste : ',
      infoTrackAlbum: 'Album : ', infoTrackPath: 'Chemin : ', infoSecAudio: 'Matériel audio et décodage',
      infoCodec: 'Codec : ', infoSampleRate: 'Échantillonnage : ', infoBitDepth: 'Résolution : ',
      infoChannels: 'Canaux : ', infoBitrate: 'Débit : ', infoBackend: 'Moteur de sortie : ',
      infoSecEngine: 'Moteur de lecture et rendu', infoPlayState: 'État de lecture : ', infoProgress: 'Progression : ',
      infoLevels: 'Crête & RMS : ', infoVolume: 'Volume principal : ', infoLyricsType: 'État des paroles : ',
      infoWordStats: 'Mots des paroles : ', infoPresentationZoom: 'Zoom diapo : ', infoBlur: 'Flou gaussien : ',
      playlistsTitle: 'Ouvrir & Importer des listes',
      playlistsSub: 'Parcourez la bibliothèque en arborescence et chargez dans la file de diapositives.',
      filterPlaylists: 'Filtrer les listes ou pistes...', btnRefresh: 'Actualiser', btnReturn: 'Retour à la présentation',
      localAlbums: 'Albums locaux', customPlaylists: 'Listes personnalisées', albumUnit: 'albums',
      importAndPlay: 'Tout importer et lire', appendToQueue: 'Ajouter à la file', loadingPlaylists: 'Chargement des listes...',
      loadingTracks: 'Chargement des pistes...', emptyPlaylist: 'Liste vide', emptyLibraryHint: 'Aucune liste ou piste trouvée dans la bibliothèque.',
      toastVstRack: 'Rack d effets VST3 activé', toastVstMgr: 'Ouverture du gestionnaire VST3...',
      toastImporting: 'Importation et lecture', toastAppended: 'Ajouté à la file',
      fontGroupCurated: 'Polices sélectionnées', fontGroupSystem: 'Polices système installées',
      pointerCentered: 'Curseur de lecture centré', infoPlayStatePaused: 'En pause',
      noSyncedLyrics: 'Aucune parole synchronisée', enjoyMelody: 'Instrumental, profitez de la mélodie'
    },
    'es-ES': {
      tabFile: 'Archivo(F)', tabHome: 'Inicio(H)', tabEdit: 'Edición(E)', tabMultitrack: 'Multipista(M)', tabClip: 'Clip(C)',
      tabEffects: 'Efectos(S)', tabZoom: 'Zoom(Z)', tabTransport: 'Transporte(T)', tabWindow: 'Ventana(W)', tabHelp: 'Ayuda(H)', tabDesign: 'Diseño(G)',
      autosave: 'Autoguardado', autoSave: 'Autoguardado', searchResultsHeader: 'Resultados de búsqueda', searchHeader: 'Resultados de búsqueda',
      btnNewSlide: 'Siguiente', subNextTrack: 'Diapositiva',
      grpClipboard: 'Portapapeles', grpSlides: 'Diapositivas', grpFont: 'Fuente (Letras)', grpParagraph: 'Párrafo',
      grpDesignMask: 'Diseño y máscara', grpSlideShow: 'Presentación', grpClipData: 'Datos copiados', grpStateReset: 'Restablecer',
      grpRenderQueue: 'Render y cola', grpSoundstage: 'Escena sonora', grpPlayhead: 'Posición', grpTimeJump: 'Salto temporal',
      grpVstPlugins: 'Plugins VST3', grpAnalyzerCtrl: 'Analizador', grpSlideZoom: 'Zoom diapositiva', grpWorkspaceView: 'Vista',
      grpBasicTransport: 'Transporte', grpPlayModeVol: 'Modo y volumen', grpColorTheme: 'Tema de color', grpSettingsCenter: 'Ajustes',
      grpDocsSupport: 'Soporte', grpSlideLayoutStyle: 'Diseño de diapositiva', grpBgVariants: 'Fondo y variantes',
      btnCopyLyrics: 'Copiar letras', btnCopyTitle: 'Copiar título', btnCopyArtist: 'Copiar artista', btnCopyMeta: 'Metadatos', btnCopyTimecode: 'Código tiempo',
      btnNextSlide: 'Siguiente', btnPrevSlide: 'Anterior', btnResetLayout: 'Restablecer', transPill: 'Trad.',
      lblOverlay: 'Máscara:', lblBlur: 'Desenfoque:', lblVinylSpeed: 'Velocidad:', btnSlideShow: 'Presentación',
      btnResetPeak: 'Reiniciar pico', btnRescan: 'Reescanear', btnRefreshLyrics: 'Actualizar letras',
      btnSyncQueue: 'Sinc. cola', btnRealtime: 'Monitor', btnHiRes: 'Alta res.', btnStereoOut: 'Estéreo', btnSpectrumBars: 'Espectro',
      btnCenterPin: 'Centrar aguja', btnCenterPinSub: 'Enfocar', btnSeekStart: 'Al inicio', btnSeekEnd: 'Al final',
      btnRw5: 'Retroceder 5s', btnRw15: 'Retroceder 15s', btnFf5: 'Avanzar 5s', btnFf15: 'Avanzar 15s',
      btnVstRack: 'Rack VST3', btnVstMgr: 'Gestor VST3', btnFreezeSpec: 'Congelar', btnClearLevels: 'Limpiar niveles',
      btnZoomFit: 'Ajustar ventana', btnZoomIn: 'Acercar', btnZoomOut: 'Alejar', btnToggleQueue: 'Panel de cola', btnToggleNotes: 'Panel de notas',
      btnPlayPause: 'Reproducir / Pausa', btnPrevTrack: 'Anterior', btnNextTrack: 'Siguiente',
      btnRepeatMode: 'Modo repetir', btnShufflePlay: 'Aleatorio', btnMuteToggle: 'Silenciar', btnMaxVolume: 'Volumen máx.',
      btnLightMode: 'Modo claro', btnLightModeSub: 'Clásico', btnDarkMode: 'Modo oscuro', btnDarkModeSub: 'Noche',
      btnPersonalizeSub: 'Opciones', btnShortcuts: 'Atajos', btnReloadRuntime: 'Recargar',
      layoutVinylTitle: 'Vinilo clásico', layoutVinylDesc: 'Tocadiscos y datos', layoutNeteaseTitle: 'Estilo NetEase', layoutNeteaseDesc: 'Portada y letras',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'Fondo dinámico', btnThemeMode: 'Modo tema', btnResetBg: 'Reiniciar fondo',
      queuePanelTitle: 'Cola de diapositivas', neAlbum: 'Álbum:', neArtist: 'Artista:', neSource: 'Fuente:', lyricsWord: 'Letras',
      spellCheckOk: 'Sin errores ortográficos', playedWordPrefix: 'Leído', totalWordPrefix: 'Total', charUnit: 'car.',
      lh12: 'Sencillo (1.0)', lh15: '1.5 líneas', lh18: '1.8 líneas', lh20: 'Doble (2.0)',
      rpm33: '33 RPM (Lento)', rpm45: '45 RPM (Estándar)', rpm78: '78 RPM (Rápido)', rpmStop: 'Pausar giro',
      fontYaHei: 'Microsoft YaHei', fontSimSun: 'SimSun Serif', fontKaiTi: 'KaiTi Script',
      fontFangSong: 'FangSong Serif', fontGeorgia: 'Georgia Serif', fontConsolas: 'Consolas Mono',
      optTitle: 'Opciones de tema PowerPoint', optSub: 'Personalice los efectos visuales, tipografía y rotación del vinilo.',
      optThemeCard: 'Color del tema', optToneLight: 'Office Claro (Blanco/Rojo)', optToneDark: 'Office Oscuro (Negro/Rojo)',
      optOverlayCard: 'Máscara blanca de diapositiva', optOverlayHint: 'Ajuste la opacidad sobre la portada para mejorar el contraste.',
      optVinylCard: 'Rotación del tocadiscos', optTransCard: 'Traducción de letras', optChkTrans: 'Mostrar subtítulos bilingües',
      aboutTitle: 'Acerca del tema Microsoft PowerPoint ECHO', aboutSub: 'Interfaz clásica de presentación PowerPoint creada para ECHO.',
      aboutNameLbl: 'Nombre del plugin: ', aboutNameVal: 'Microsoft PowerPoint · Tema de presentación',
      aboutIdLbl: 'ID del plugin: ', aboutVerLbl: 'Versión: ', aboutProtoLbl: 'Prototipo de diseño: ',
      aboutProtoVal: 'Cinta Fluent PowerPoint + Cola de miniaturas + Tocadiscos de vinilo + Letras dinámicas',
      aboutRefLbl: 'Referencia: ', aboutAuthorLbl: 'Autor: ', aboutProtocolLbl: 'Protocolo Workshop: ',
      notesPlaceholder: 'Haga clic para agregar notas del orador',
      searchPlaceholder: 'Buscar presentaciones y biblioteca (Alt+Q)...', notes: 'Notas', comments: 'Comentarios', fitWindow: 'Ajustar',
      slideCounter: 'Diapositiva', of: 'de', sheetUnit: 'diapositivas', songUnit: 'pistas', pureMusic: 'Instrumental',
      nowPlaying: 'Reproduciendo', badgeQueue: 'Cola', badgeLibrary: 'Biblioteca', localFolder: 'Carpeta local',
      navInfo: 'Información', navPlaylists: 'Abrir e importar', navOptions: 'Opciones de tema', navAbout: 'Acerca del plugin',
      infoTitle: 'Microsoft PowerPoint · Información de audio y documento',
      infoSub: 'Resumen de pista de audio, especificaciones de hardware y estado del host.',
      infoSecTrack: 'Pista y metadatos', infoTrackTitle: 'Pista actual: ', infoTrackArtist: 'Artista: ',
      infoTrackAlbum: 'Álbum: ', infoTrackPath: 'Ruta de almacenamiento: ', infoSecAudio: 'Hardware y decodificación',
      infoCodec: 'Códec: ', infoSampleRate: 'Muestreo: ', infoBitDepth: 'Profundidad de bits: ',
      infoChannels: 'Canales: ', infoBitrate: 'Tasa de bits: ', infoBackend: 'Motor de salida: ',
      infoSecEngine: 'Motor de reproducción y renderizado', infoPlayState: 'Estado de reproducción: ', infoProgress: 'Progreso: ',
      infoLevels: 'Pico y RMS: ', infoVolume: 'Volumen maestro: ', infoLyricsType: 'Estado de letras: ',
      infoWordStats: 'Palabras de letras: ', infoPresentationZoom: 'Zoom diapositiva: ', infoBlur: 'Desenfoque gaussiano: ',
      playlistsTitle: 'Abrir e importar listas de reproducción',
      playlistsSub: 'Explore la biblioteca en estructura de carpetas e importe a la cola de diapositivas.',
      filterPlaylists: 'Filtrar listas o pistas...', btnRefresh: 'Actualizar', btnReturn: 'Volver a la presentación',
      localAlbums: 'Álbumes locales', customPlaylists: 'Listas personalizadas', albumUnit: 'álbumes',
      importAndPlay: 'Importar todo y reproducir', appendToQueue: 'Añadir a cola', loadingPlaylists: 'Cargando biblioteca y listas...',
      loadingTracks: 'Cargando lista de pistas...', emptyPlaylist: 'Lista vacía', emptyLibraryHint: 'No se encontraron listas ni pistas en la biblioteca.',
      toastVstRack: 'Rack de efectos VST3 abierto', toastVstMgr: 'Abriendo administrador VST3...',
      toastImporting: 'Importando y reproduciendo', toastAppended: 'Añadido a la cola',
      fontGroupCurated: 'Fuentes seleccionadas', fontGroupSystem: 'Fuentes del sistema instaladas',
      pointerCentered: 'Puntero de reproducción centrado', infoPlayStatePaused: 'En pausa',
      noSyncedLyrics: 'Sin letras sincronizadas', enjoyMelody: 'Instrumental, disfrute la música'
    },
    'ru-RU': {
      tabFile: 'Файл(F)', tabHome: 'Главная(H)', tabEdit: 'Правка(E)', tabMultitrack: 'Мультитрек(M)', tabClip: 'Клип(C)',
      tabEffects: 'Эффекты(S)', tabZoom: 'Масштаб(Z)', tabTransport: 'Управление(T)', tabWindow: 'Окно(W)', tabHelp: 'Справка(H)', tabDesign: 'Дизайн(G)',
      autosave: 'Автосохранение', autoSave: 'Автосохранение', searchResultsHeader: 'Результаты поиска', searchHeader: 'Результаты поиска',
      btnNewSlide: 'Следующий', subNextTrack: 'Слайд',
      grpClipboard: 'Буфер обмена', grpSlides: 'Слайды', grpFont: 'Шрифт (Текст)', grpParagraph: 'Абзац',
      grpDesignMask: 'Маска и размытие', grpSlideShow: 'Показ слайдов', grpClipData: 'Данные буфера', grpStateReset: 'Сброс',
      grpRenderQueue: 'Очередь', grpSoundstage: 'Звуковая сцена', grpPlayhead: 'Позиция', grpTimeJump: 'Переход',
      grpVstPlugins: 'Плагины VST3', grpAnalyzerCtrl: 'Анализатор', grpSlideZoom: 'Масштаб слайда', grpWorkspaceView: 'Вид окна',
      grpBasicTransport: 'Транспорт', grpPlayModeVol: 'Режим и громкость', grpColorTheme: 'Цветовая тема', grpSettingsCenter: 'Настройки',
      grpDocsSupport: 'Поддержка', grpSlideLayoutStyle: 'Макет слайда', grpBgVariants: 'Фон и варианты',
      btnCopyLyrics: 'Копир. текст', btnCopyTitle: 'Копир. трек', btnCopyArtist: 'Копир. автора', btnCopyMeta: 'Метаданные', btnCopyTimecode: 'Таймкод',
      btnNextSlide: 'Следующий', btnPrevSlide: 'Предыдущий', btnResetLayout: 'Сброс макета', transPill: 'Перевод',
      lblOverlay: 'Маска фона:', lblBlur: 'Размытие:', lblVinylSpeed: 'Скорость:', btnSlideShow: 'Показ слайдов',
      btnResetPeak: 'Сброс пиков', btnRescan: 'Обновить', btnRefreshLyrics: 'Обновить текст',
      btnSyncQueue: 'Синхр. очередь', btnRealtime: 'Мониторинг', btnHiRes: 'Hi-Res звук', btnStereoOut: 'Стерео выход', btnSpectrumBars: 'Спектр',
      btnCenterPin: 'Центр курсора', btnCenterPinSub: 'Фокус строки', btnSeekStart: 'В начало', btnSeekEnd: 'В конец',
      btnRw5: 'Назад 5с', btnRw15: 'Назад 15с', btnFf5: 'Вперед 5с', btnFf15: 'Вперед 15с',
      btnVstRack: 'Стойка VST3', btnVstMgr: 'Менеджер VST3', btnFreezeSpec: 'Заморозить', btnClearLevels: 'Сброс уровней',
      btnZoomFit: 'Вписать', btnZoomIn: 'Увеличить', btnZoomOut: 'Уменьшить', btnToggleQueue: 'Панель очереди', btnToggleNotes: 'Панель заметок',
      btnPlayPause: 'Старт / Пауза', btnPrevTrack: 'Предыдущий', btnNextTrack: 'Следующий',
      btnRepeatMode: 'Повтор', btnShufflePlay: 'Случайно', btnMuteToggle: 'Без звука', btnMaxVolume: 'Макс. звук',
      btnLightMode: 'Светлая тема', btnLightModeSub: 'Классика', btnDarkMode: 'Темная тема', btnDarkModeSub: 'Ночная',
      btnPersonalizeSub: 'Параметры', btnShortcuts: 'Горячие клавиши', btnReloadRuntime: 'Перезагрузить',
      layoutVinylTitle: 'Винил', layoutVinylDesc: 'Проигрыватель', layoutNeteaseTitle: 'Стиль NetEase', layoutNeteaseDesc: 'Обложка и текст',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'Живой градиент', btnThemeMode: 'Тема', btnResetBg: 'Сброс фона',
      queuePanelTitle: 'Очередь слайдов', neAlbum: 'Альбом:', neArtist: 'Артист:', neSource: 'Источник:', lyricsWord: 'Текст',
      spellCheckOk: 'Ошибок нет', playedWordPrefix: 'Пропето', totalWordPrefix: 'Всего', charUnit: 'симв.',
      lh12: 'Одинарный (1.0)', lh15: '1.5 интервал', lh18: '1.8 интервал', lh20: 'Двойной (2.0)',
      rpm33: '33 RPM (Медленно)', rpm45: '45 RPM (Стандарт)', rpm78: '78 RPM (Быстро)', rpmStop: 'Пауза',
      fontYaHei: 'Microsoft YaHei', fontSimSun: 'SimSun Serif', fontKaiTi: 'KaiTi Script',
      fontFangSong: 'FangSong Serif', fontGeorgia: 'Georgia Serif', fontConsolas: 'Consolas Mono',
      optTitle: 'Параметры темы PowerPoint', optSub: 'Настройка визуальных эффектов слайда, шрифтов и вращения винила.',
      optThemeCard: 'Цветовая тема оформления', optToneLight: 'Светлая Office (Белый/Красный)', optToneDark: 'Темная Office (Черный/Красный)',
      optOverlayCard: 'Белая маска фона слайда', optOverlayHint: 'Регулировка прозрачности маски поверх обложки альбома.',
      optVinylCard: 'Скорость вращения пластинки', optTransCard: 'Перевод и транскрипция', optChkTrans: 'Показывать перевод текста песен',
      aboutTitle: 'О теме Microsoft PowerPoint ECHO', aboutSub: 'Классический интерфейс презентации PowerPoint для плеера ECHO.',
      aboutNameLbl: 'Название плагина: ', aboutNameVal: 'Microsoft PowerPoint · Тема презентации',
      aboutIdLbl: 'ID плагина: ', aboutVerLbl: 'Версия: ', aboutProtoLbl: 'Прототип дизайна: ',
      aboutProtoVal: 'Лента Fluent PowerPoint + Очередь миниатюр + Виниловый проигрыватель + Динамический текст',
      aboutRefLbl: 'Эталон: ', aboutAuthorLbl: 'Автор: ', aboutProtocolLbl: 'Протокол мастерской: ',
      notesPlaceholder: 'Нажмите, чтобы добавить заметки докладчика',
      searchPlaceholder: 'Поиск презентаций и треков (Alt+Q)...', notes: 'Заметки', comments: 'Примечания', fitWindow: 'По размеру',
      slideCounter: 'Слайд', of: 'из', sheetUnit: 'слайдов', songUnit: 'треков', pureMusic: 'Инструментал',
      nowPlaying: 'Сейчас играет', badgeQueue: 'Очередь', badgeLibrary: 'Медиатека', localFolder: 'Папка',
      navInfo: 'Сведения', navPlaylists: 'Открыть и импортировать', navOptions: 'Параметры темы', navAbout: 'О плагине',
      infoTitle: 'Microsoft PowerPoint · Сведения об аудио и документе',
      infoSub: 'Обзор параметров аудиотрека, оборудования и состояния хоста.',
      infoSecTrack: 'Трек и метаданные', infoTrackTitle: 'Текущий трек: ', infoTrackArtist: 'Исполнитель: ',
      infoTrackAlbum: 'Альбом: ', infoTrackPath: 'Путь к файлу: ', infoSecAudio: 'Аудиооборудование и декодер',
      infoCodec: 'Кодек: ', infoSampleRate: 'Частота дискретизации: ', infoBitDepth: 'Разрядность: ',
      infoChannels: 'Каналы: ', infoBitrate: 'Битрейт: ', infoBackend: 'Вывод звука: ',
      infoSecEngine: 'Движок воспроизведения', infoPlayState: 'Состояние: ', infoProgress: 'Прогресс: ',
      infoLevels: 'Пик и RMS: ', infoVolume: 'Громкость: ', infoLyricsType: 'Текст песни: ',
      infoWordStats: 'Количество слов: ', infoPresentationZoom: 'Масштаб слайда: ', infoBlur: 'Размытие Гаусса: ',
      playlistsTitle: 'Открыть и импортировать плейлисты',
      playlistsSub: 'Просмотр медиатеки и плейлистов в виде дерева папок с загрузкой в слайды.',
      filterPlaylists: 'Фильтр плейлистов или треков...', btnRefresh: 'Обновить', btnReturn: 'Вернуться к презентации',
      localAlbums: 'Локальные альбомы', customPlaylists: 'Плейлисты', albumUnit: 'альб.',
      importAndPlay: 'Импортировать и играть', appendToQueue: 'В очередь', loadingPlaylists: 'Чтение плейлистов...',
      loadingTracks: 'Загрузка треков...', emptyPlaylist: 'Плейлист пуст', emptyLibraryHint: 'Плейлисты и треки не найдены в медиатеке.',
      toastVstRack: 'Стойка VST3 открыта', toastVstMgr: 'Открытие диспетчера VST3...',
      toastImporting: 'Импорт и воспроизведение', toastAppended: 'Добавлено в очередь',
      fontGroupCurated: 'Популярные шрифты', fontGroupSystem: 'Установленные шрифты системы',
      pointerCentered: 'Указатель воспроизведения по центру', infoPlayStatePaused: 'Приостановлено',
      noSyncedLyrics: 'Нет синхронизированного текста', enjoyMelody: 'Инструментальная композиция'
    },
    'ar-SA': {
      tabFile: 'ملف(F)', tabHome: 'الرئيسية(H)', tabEdit: 'تحرير(E)', tabMultitrack: 'متعدد(M)', tabClip: 'مقطع(C)',
      tabEffects: 'تأثيرات(S)', tabZoom: 'تكبير(Z)', tabTransport: 'تشغيل(T)', tabWindow: 'نافذة(W)', tabHelp: 'تعليمات(H)', tabDesign: 'تصميم(G)',
      autosave: 'حفظ تلقائي', autoSave: 'حفظ تلقائي', searchResultsHeader: 'نتائج البحث في المكتبة', searchHeader: 'نتائج البحث في المكتبة',
      btnNewSlide: 'التالي', subNextTrack: 'شريحة',
      grpClipboard: 'الحافظة', grpSlides: 'الشرائح', grpFont: 'الخط (الكلمات)', grpParagraph: 'فقرة ومحاذاة',
      grpDesignMask: 'التصميم والقناع', grpSlideShow: 'عرض الشرائح', grpClipData: 'بيانات الحافظة', grpStateReset: 'إعادة ضبط',
      grpRenderQueue: 'العرض والقائمة', grpSoundstage: 'المجال الصوتي', grpPlayhead: 'مؤشر التشغيل', grpTimeJump: 'قفز زمني',
      grpVstPlugins: 'إضافات VST3', grpAnalyzerCtrl: 'المحلل الطيفي', grpSlideZoom: 'تكبير الشريحة', grpWorkspaceView: 'عرض مساحة العمل',
      grpBasicTransport: 'التحكم الأساسي', grpPlayModeVol: 'الوضع والصوت', grpColorTheme: 'سمة اللون', grpSettingsCenter: 'الإعدادات',
      grpDocsSupport: 'الدعم والوثائق', grpSlideLayoutStyle: 'نمط تخطيط الشريحة', grpBgVariants: 'الخلفية والأنماط',
      btnCopyLyrics: 'نسخ الكلمات', btnCopyTitle: 'نسخ العنوان', btnCopyArtist: 'نسخ الفنان', btnCopyMeta: 'نسخ البيانات', btnCopyTimecode: 'نسخ الوقت',
      btnNextSlide: 'الشريحة التالية', btnPrevSlide: 'الشريحة السابقة', btnResetLayout: 'إعادة التخطيط', transPill: 'ترجمة',
      lblOverlay: 'قناع الخلفية:', lblBlur: 'ضبابية:', lblVinylSpeed: 'سرعة القرص:', btnSlideShow: 'عرض الشرائح',
      btnResetPeak: 'تصفير الذروة', btnRescan: 'إعادة فحص', btnRefreshLyrics: 'تحديث الكلمات',
      btnSyncQueue: 'مزامنة القائمة', btnRealtime: 'مراقبة حية', btnHiRes: 'جودة عالية', btnStereoOut: 'صوت ستيريو', btnSpectrumBars: 'أعمدة الطيف',
      btnCenterPin: 'توسيط المؤشر', btnCenterPinSub: 'تركيز السطر', btnSeekStart: 'إلى البداية', btnSeekEnd: 'إلى النهاية',
      btnRw5: 'رجوع 5ث', btnRw15: 'رجوع 15ث', btnFf5: 'تقديم 5ث', btnFf15: 'تقديم 15ث',
      btnVstRack: 'رف VST3', btnVstMgr: 'إدارة الإضافات', btnFreezeSpec: 'تجميد التحليل', btnClearLevels: 'مسح المستويات',
      btnZoomFit: 'ملاءمة النافذة', btnZoomIn: 'تكبير', btnZoomOut: 'تصغير', btnToggleQueue: 'لوحة القائمة', btnToggleNotes: 'لوحة الملاحظات',
      btnPlayPause: 'تشغيل / إيقاف', btnPrevTrack: 'السابق', btnNextTrack: 'التالي',
      btnRepeatMode: 'وضع التكرار', btnShufflePlay: 'عشوائي', btnMuteToggle: 'كتم الصوت', btnMaxVolume: 'أقصى صوت',
      btnLightMode: 'الوضع الفاتح', btnLightModeSub: 'كلاسيكي', btnDarkMode: 'الوضع الداكن', btnDarkModeSub: 'ليلي',
      btnPersonalizeSub: 'تخصيص', btnShortcuts: 'دليل الاختصارات', btnReloadRuntime: 'إعادة تحميل',
      layoutVinylTitle: 'أسطوانة كلاسيكية', layoutVinylDesc: 'قرص دوار ومواصفات', layoutNeteaseTitle: 'نمط NetEase', layoutNeteaseDesc: 'غلاف وكلمات',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'خلفية متدرجة', btnThemeMode: 'وضع السمة', btnResetBg: 'إعادة الخلفية',
      queuePanelTitle: 'قائمة تشغيل الشرائح', neAlbum: 'الألبوم:', neArtist: 'الفنان:', neSource: 'المصدر:', lyricsWord: 'الكلمات',
      spellCheckOk: 'لا توجد أخطاء إملائية', playedWordPrefix: 'تم تشغيل', totalWordPrefix: 'من', charUnit: 'حرف',
      lh12: 'تباعد مفرد (1.0)', lh15: 'تباعد 1.5', lh18: 'تباعد 1.8', lh20: 'تباعد مزدوج (2.0)',
      rpm33: '33 دورة (بطيء)', rpm45: '45 دورة (قياسي)', rpm78: '78 دورة (سريع)', rpmStop: 'إيقاف الدوران',
      fontYaHei: 'Microsoft YaHei', fontSimSun: 'SimSun Serif', fontKaiTi: 'KaiTi Script',
      fontFangSong: 'FangSong Serif', fontGeorgia: 'Georgia Serif', fontConsolas: 'Consolas Mono',
      optTitle: 'خيارات سمة PowerPoint والتخصيص', optSub: 'تخصيص التأثيرات المرئية للشرائح والخطوط وسرعة دوران الأسطوانة.',
      optThemeCard: 'لون السمة', optToneLight: 'Office فاتح (أبيض/أحمر)', optToneDark: 'Office داكن (أسود/أحمر)',
      optOverlayCard: 'قناع الخلفية الأبيض', optOverlayHint: 'ضبط شفافية القناع فوق صورة الغلاف لتباين الكلمات.',
      optVinylCard: 'خيارات دوران الأسطوانة', optTransCard: 'ترجمة الكلمات', optChkTrans: 'عرض الترجمة الثنائية تحت الكلمات',
      aboutTitle: 'حول واجهة Microsoft PowerPoint ECHO', aboutSub: 'واجهة عرض تقديمي كلاسيكية مصممة لمشغل الموسيقى ECHO.',
      aboutNameLbl: 'اسم الإضافة: ', aboutNameVal: 'Microsoft PowerPoint · سمة العرض التقديمي',
      aboutIdLbl: 'معرف الإضافة: ', aboutVerLbl: 'الإصدار: ', aboutProtoLbl: 'نموذج التصميم: ',
      aboutProtoVal: 'شريط PowerPoint Fluent + قائمة الشرائح + مشغل أسطوانات + كلمات متزامنة',
      aboutRefLbl: 'المرجع: ', aboutAuthorLbl: 'المؤلف: ', aboutProtocolLbl: 'بروتوكول الورشة: ',
      notesPlaceholder: 'انقر لإضافة ملاحظات العرض التقديمي',
      searchPlaceholder: 'البحث في العروض والمكتبة (Alt+Q)...', notes: 'ملاحظات', comments: 'تعليقات', fitWindow: 'احتواء',
      slideCounter: 'شريحة', of: 'من', sheetUnit: 'شرائح', songUnit: 'مسارات', pureMusic: 'موسيقى نقية',
      nowPlaying: 'قيد التشغيل', badgeQueue: 'قائمة الانتظار', badgeLibrary: 'المكتبة', localFolder: 'مجلد محلي',
      navInfo: 'معلومات', navPlaylists: 'فتح واستيراد القوائم', navOptions: 'خيارات السمة', navAbout: 'حول الإضافة',
      infoTitle: 'Microsoft PowerPoint · معلومات الصوت والمستند',
      infoSub: 'نظرة عامة على خصائص مسار الصوت ومواصفات الأجهزة وحالة التشغيل.',
      infoSecTrack: 'المسار والبيانات الوصفية', infoTrackTitle: 'المسار الحالي: ', infoTrackArtist: 'الفنان: ',
      infoTrackAlbum: 'الألبوم: ', infoTrackPath: 'مسار التخزين: ', infoSecAudio: 'عتاد الصوت وفك التشفير',
      infoCodec: 'الترميز: ', infoSampleRate: 'معدل العينة: ', infoBitDepth: 'عمق البت: ',
      infoChannels: 'القنوات: ', infoBitrate: 'معدل البت: ', infoBackend: 'محرك الإخراج: ',
      infoSecEngine: 'محرك التشغيل وحالة العرض', infoPlayState: 'حالة التشغيل: ', infoProgress: 'التقدم: ',
      infoLevels: 'الذروة و RMS: ', infoVolume: 'مستوى الصوت: ', infoLyricsType: 'حالة الكلمات: ',
      infoWordStats: 'عدد الكلمات: ', infoPresentationZoom: 'تكبير الشريحة: ', infoBlur: 'الضبابية: ',
      playlistsTitle: 'فتح واستيراد قوائم التشغيل',
      playlistsSub: 'تصفح المكتبة وقوائم التشغيل بهيكل شجرة المجلدات واستيرادها إلى الشرائح.',
      filterPlaylists: 'تصفية القوائم أو المسارات...', btnRefresh: 'تحديث', btnReturn: 'العودة إلى العرض',
      localAlbums: 'الألبومات المحلية', customPlaylists: 'قوائم التشغيل', albumUnit: 'ألبوم',
      importAndPlay: 'استيراد وتشغيل الكل', appendToQueue: 'إضافة للقائمة', loadingPlaylists: 'جاري القراءة...',
      loadingTracks: 'جاري التحميل...', emptyPlaylist: 'القائمة فارغة', emptyLibraryHint: 'لا توجد قوائم تشغيل أو مسارات في المكتبة حاليا.',
      toastVstRack: 'تم تبديل رف VST3', toastVstMgr: 'جاري فتح مدير VST3...',
      toastImporting: 'جاري الاستيراد والتشغيل', toastAppended: 'تمت الإضافة للقائمة',
      fontGroupCurated: 'الخطوط المختارة', fontGroupSystem: 'خطوط النظام المثبتة',
      pointerCentered: 'تم توسيط مؤشر التشغيل', infoPlayStatePaused: 'متوقف مؤقتا',
      noSyncedLyrics: 'لا توجد كلمات متزامنة', enjoyMelody: 'موسيقى فقط، استمتع بالألحان'
    },
    'ja-JP': {
      tabFile: 'ファイル(F)', tabHome: 'ホーム(H)', tabEdit: '編集(E)', tabMultitrack: 'マルチトラック(M)', tabClip: 'クリップ(C)',
      tabEffects: 'エフェクト(S)', tabZoom: 'ズーム(Z)', tabTransport: 'コントロール(T)', tabWindow: 'ウィンドウ(W)', tabHelp: 'ヘルプ(H)', tabDesign: 'デザイン(G)',
      autosave: '自動保存', autoSave: '自動保存', searchResultsHeader: 'ライブラリとキューの検索結果', searchHeader: 'ライブラリとキューの検索結果',
      btnNewSlide: '次の曲へ', subNextTrack: 'スライド',
      grpClipboard: 'クリップボード', grpSlides: 'スライド', grpFont: 'フォント (歌詞設定)', grpParagraph: '段落',
      grpDesignMask: 'デザインとマスク', grpSlideShow: 'スライドショー', grpClipData: 'コピー項目', grpStateReset: 'リセット',
      grpRenderQueue: 'レンダーとキュー', grpSoundstage: '音場設定', grpPlayhead: '再生ヘッド位置', grpTimeJump: '時間ジャンプ',
      grpVstPlugins: 'VST3 プラグイン', grpAnalyzerCtrl: 'アナライザー', grpSlideZoom: 'スライド倍率', grpWorkspaceView: '表示切り替え',
      grpBasicTransport: '基本トランスポート', grpPlayModeVol: '再生モードと音量', grpColorTheme: 'カラーテーマ', grpSettingsCenter: '設定センター',
      grpDocsSupport: 'サポート', grpSlideLayoutStyle: 'スライドレイアウト', grpBgVariants: '背景とバリエーション',
      btnCopyLyrics: '歌詞をコピー', btnCopyTitle: '曲名をコピー', btnCopyArtist: '歌手名をコピー', btnCopyMeta: 'メタデータ', btnCopyTimecode: 'タイムコード',
      btnNextSlide: '次の曲へ', btnPrevSlide: '前の曲へ', btnResetLayout: 'レイアウト初期化', transPill: '訳/ルビ',
      lblOverlay: '背景マスク:', lblBlur: 'ガウスぼかし:', lblVinylSpeed: '回転速度:', btnSlideShow: 'スライドショー',
      btnResetPeak: 'ピーク初期化', btnRescan: '再スキャン', btnRefreshLyrics: '歌詞を再取得',
      btnSyncQueue: 'キュー同期', btnRealtime: 'リアルタイム監視', btnHiRes: 'ハイレゾ出力', btnStereoOut: 'ステレオ出力', btnSpectrumBars: 'スペクトラム',
      btnCenterPin: 'ポインタ中央', btnCenterPinSub: '現在行へ移動', btnSeekStart: '曲の先頭へ', btnSeekEnd: '曲の終盤へ',
      btnRw5: '5秒戻す', btnRw15: '15秒戻す', btnFf5: '5秒進める', btnFf15: '15秒進める',
      btnVstRack: 'VST3 ラック', btnVstMgr: 'プラグイン管理', btnFreezeSpec: '分析を一時停止', btnClearLevels: 'レベル消去',
      btnZoomFit: '画面に合わせる', btnZoomIn: '拡大', btnZoomOut: '縮小', btnToggleQueue: '左キューパネル', btnToggleNotes: 'ノートパネル',
      btnPlayPause: '再生 / 一時停止', btnPrevTrack: '前の曲', btnNextTrack: '次の曲',
      btnRepeatMode: 'リピート切替', btnShufflePlay: 'シャッフル', btnMuteToggle: 'ミュート切替', btnMaxVolume: '最大音量',
      btnLightMode: 'ライトモード', btnLightModeSub: 'クラシック', btnDarkMode: 'ダークモード', btnDarkModeSub: '目に優しい暗色',
      btnPersonalizeSub: 'カスタマイズ', btnShortcuts: 'ショートカット一覧', btnReloadRuntime: '再読み込み',
      layoutVinylTitle: 'クラシックレコード', layoutVinylDesc: 'ターンテーブルと仕様', layoutNeteaseTitle: 'NetEase スタイル', layoutNeteaseDesc: '大型カバーと歌詞',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: 'ダイナミックメッシュ', btnThemeMode: 'テーマ切替', btnResetBg: '背景リセット',
      queuePanelTitle: 'スライド再生キュー', neAlbum: 'アルバム:', neArtist: 'アーティスト:', neSource: 'ソース:', lyricsWord: '歌詞',
      spellCheckOk: '校正エラーなし', playedWordPrefix: '再生済', totalWordPrefix: '全', charUnit: '文字',
      lh12: '1.0倍 行間', lh15: '1.5倍 行間', lh18: '1.8倍 行間', lh20: '2.0倍 行間',
      rpm33: '33 RPM (ゆったり)', rpm45: '45 RPM (標準)', rpm78: '78 RPM (高速)', rpmStop: '回転停止',
      fontYaHei: 'Microsoft YaHei (ゴシック)', fontSimSun: 'SimSun (明朝体)', fontKaiTi: 'KaiTi (楷書体)',
      fontFangSong: 'FangSong (仿宋)', fontGeorgia: 'Georgia (欧文セリフ)', fontConsolas: 'Consolas (等幅)',
      optTitle: 'PowerPoint テーマオプションと個人設定', optSub: 'スライドの視覚効果、フォント設定、レコードの回転動作をカスタマイズします。',
      optThemeCard: '外観カラーテーマ', optToneLight: 'Office クラシックライト (白/赤)', optToneDark: 'Office クラシックダーク (黒/赤)',
      optOverlayCard: 'スライド背景ホワイトマスク', optOverlayHint: '背景カバー上の白マスク不透明度を調整し、歌詞の視認性を高めます。',
      optVinylCard: 'レコード回転オプション', optTransCard: '歌詞の翻訳とルビ表示', optChkTrans: '対訳・ローマ字サブタイトルを表示する',
      aboutTitle: 'Microsoft PowerPoint ECHO ワークショップスキンについて', aboutSub: 'ECHO 音楽プレーヤー向けに設計された Microsoft PowerPoint プレゼンテーションテーマ。',
      aboutNameLbl: 'プラグイン名：', aboutNameVal: 'Microsoft PowerPoint · プレゼンテーションテーマ',
      aboutIdLbl: 'プラグイン ID：', aboutVerLbl: 'バージョン：', aboutProtoLbl: 'デザインプロトタイプ：',
      aboutProtoVal: 'PowerPoint Fluent リボン + サムネイルキュー + レコードプレーヤー + 同期歌詞',
      aboutRefLbl: 'デザイン参照：', aboutAuthorLbl: '作成者：', aboutProtocolLbl: 'ワークショップ仕様：',
      notesPlaceholder: 'クリックして発表者ノートを追加 (現在の曲情報とメモ)',
      searchPlaceholder: 'プレゼンテーションと曲庫を検索 (Alt+Q)...', notes: 'ノート', comments: 'コメント', fitWindow: '合わせる',
      slideCounter: 'スライド', of: '/', sheetUnit: '枚', songUnit: '曲', pureMusic: 'インストゥルメンタル',
      nowPlaying: '再生中', badgeQueue: 'キュー', badgeLibrary: 'ライブラリ', localFolder: 'ローカルフォルダ',
      navInfo: '情報', navPlaylists: 'プレイリストを開く/インポート', navOptions: 'テーマオプション', navAbout: 'プラグインについて',
      infoTitle: 'Microsoft PowerPoint · オーディオとドキュメント情報',
      infoSub: 'プレゼンテーション音源の属性、ハードウェア仕様、ホスト状態の概要。',
      infoSecTrack: 'トラックとメタデータ', infoTrackTitle: '現在の曲：', infoTrackArtist: 'アーティスト：',
      infoTrackAlbum: 'アルバム：', infoTrackPath: '保存パス：', infoSecAudio: 'オーディオ機器とデコード仕様',
      infoCodec: 'コーデック：', infoSampleRate: 'サンプリングレート：', infoBitDepth: '量子化ビット数：',
      infoChannels: 'チャンネル構成：', infoBitrate: 'ビットレート：', infoBackend: '出力バックエンド：',
      infoSecEngine: '再生エンジンと描画状態', infoPlayState: '再生状態：', infoProgress: '再生進行：',
      infoLevels: 'ピークとRMS：', infoVolume: 'マスター音量：', infoLyricsType: '歌詞の状態：',
      infoWordStats: '歌詞の文字数：', infoPresentationZoom: 'スライド倍率：', infoBlur: 'ガウスぼかし：',
      playlistsTitle: 'プレイリストを開く・インポート',
      playlistsSub: 'フォルダツリー構造でライブラリとプレイリストを閲覧し、スライドキューに一括インポート。',
      filterPlaylists: 'プレイリストや曲名を絞り込み...', btnRefresh: '更新', btnReturn: 'プレゼンテーションに戻る',
      localAlbums: 'ローカルアルバム', customPlaylists: 'カスタムプレイリスト', albumUnit: '枚',
      importAndPlay: '全曲インポートして再生', appendToQueue: 'キューに追加', loadingPlaylists: 'プレイリストを読み込み中...',
      loadingTracks: '曲リストを展開中...', emptyPlaylist: 'プレイリストは空です', emptyLibraryHint: 'ライブラリまたはプレイリストに曲がありません。',
      toastVstRack: 'VST3エフェクトラックを切り替えました', toastVstMgr: 'VST3プラグインマネージャーを開いています...',
      toastImporting: 'プレイリストをインポートして再生中', toastAppended: 'キューに追加しました',
      fontGroupCurated: 'おすすめ標準フォント', fontGroupSystem: 'システム内蔵フォント',
      pointerCentered: '再生ポインタを中央に配置しました', infoPlayStatePaused: '一時停止',
      noSyncedLyrics: '同期された歌詞はありません', enjoyMelody: 'インストゥルメンタル、メロディをお楽しみください'
    },
    'ko-KR': {
      tabFile: '파일(F)', tabHome: '홈(H)', tabEdit: '편집(E)', tabMultitrack: '멀티트랙(M)', tabClip: '클립(C)',
      tabEffects: '효과(S)', tabZoom: '확대/축소(Z)', tabTransport: '컨트롤(T)', tabWindow: '창(W)', tabHelp: '도움말(H)', tabDesign: '디자인(G)',
      autosave: '자동 저장', autoSave: '자동 저장', searchResultsHeader: '라이브러리 및 대기열 검색 결과', searchHeader: '라이브러리 및 대기열 검색 결과',
      btnNewSlide: '다음 곡', subNextTrack: '슬라이드',
      grpClipboard: '클립보드', grpSlides: '슬라이드', grpFont: '글꼴 (가사 맞춤)', grpParagraph: '단락 및 정렬',
      grpDesignMask: '디자인 및 마스크', grpSlideShow: '슬라이드 쇼', grpClipData: '클립보드 데이터', grpStateReset: '상태 초기화',
      grpRenderQueue: '렌더링 및 대기열', grpSoundstage: '오디오 음장', grpPlayhead: '재생 헤드 위치', grpTimeJump: '시간 이동',
      grpVstPlugins: 'VST3 플러그인', grpAnalyzerCtrl: '분석기 제어', grpSlideZoom: '슬라이드 확대/축소', grpWorkspaceView: '작업 영역 보기',
      grpBasicTransport: '기본 전송', grpPlayModeVol: '재생 모드 및 볼륨', grpColorTheme: '색상 테마', grpSettingsCenter: '설정 센터',
      grpDocsSupport: '문서 및 지원', grpSlideLayoutStyle: '슬라이드 레이아웃 스타일', grpBgVariants: '배경 및 변형',
      btnCopyLyrics: '가사 복사', btnCopyTitle: '곡명 복사', btnCopyArtist: '가수 복사', btnCopyMeta: '메타데이터 복사', btnCopyTimecode: '타임코드 복사',
      btnNextSlide: '다음 슬라이드', btnPrevSlide: '이전 슬라이드', btnResetLayout: '레이아웃 초기화', transPill: '번역/발음',
      lblOverlay: '배경 마스크:', lblBlur: '가우시안 블러:', lblVinylSpeed: '회전 속도:', btnSlideShow: '슬라이드 쇼',
      btnResetPeak: '피크 초기화', btnRescan: '다시 스캔', btnRefreshLyrics: '가사 새로고침',
      btnSyncQueue: '대기열 동기화', btnRealtime: '실시간 모니터링', btnHiRes: '고해상도 샘플링', btnStereoOut: '스테레오 출력', btnSpectrumBars: '스펙트럼 막대',
      btnCenterPin: '포인터 중앙', btnCenterPinSub: '하이라이트 이동', btnSeekStart: '처음으로', btnSeekEnd: '끝부분으로',
      btnRw5: '5초 뒤로', btnRw15: '15초 뒤로', btnFf5: '5초 앞으로', btnFf15: '15초 앞으로',
      btnVstRack: 'VST3 랙', btnVstMgr: '플러그인 관리', btnFreezeSpec: '분석 동결', btnClearLevels: '레벨 지우기',
      btnZoomFit: '창에 맞춤', btnZoomIn: '확대', btnZoomOut: '축소', btnToggleQueue: '왼쪽 대기열 창', btnToggleNotes: '발표자 노트 창',
      btnPlayPause: '재생 / 일시정지', btnPrevTrack: '이전 곡', btnNextTrack: '다음 곡',
      btnRepeatMode: '반복 모드', btnShufflePlay: '셔플 재생', btnMuteToggle: '음소거 전환', btnMaxVolume: '최대 볼륨',
      btnLightMode: '라이트 모드', btnLightModeSub: '클래식 오피스', btnDarkMode: '다크 모드', btnDarkModeSub: '어두운 테마',
      btnPersonalizeSub: '개인 설정', btnShortcuts: '단축키 안내', btnReloadRuntime: '다시 로드',
      layoutVinylTitle: '클래식 바이닐', layoutVinylDesc: '턴테이블 및 사양', layoutNeteaseTitle: 'NetEase 스타일', layoutNeteaseDesc: '대형 커버 및 가사',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: '다이내믹 그라데이션', btnThemeMode: '테마 모드', btnResetBg: '배경 초기화',
      queuePanelTitle: '슬라이드 재생 대기열', neAlbum: '앨범:', neArtist: '아티스트:', neSource: '출처:', lyricsWord: '가사',
      spellCheckOk: '맞춤법 오류 없음', playedWordPrefix: '재생됨', totalWordPrefix: '전체', charUnit: '자',
      lh12: '1.0배 줄 간격', lh15: '1.5배 줄 간격', lh18: '1.8배 줄 간격', lh20: '2.0배 줄 간격',
      rpm33: '33 RPM (느림)', rpm45: '45 RPM (표준)', rpm78: '78 RPM (빠름)', rpmStop: '회전 멈춤',
      fontYaHei: '맑은 고딕 / YaHei', fontSimSun: '명조체 (SimSun)', fontKaiTi: '해서체 (KaiTi)',
      fontFangSong: '방송체 (FangSong)', fontGeorgia: 'Georgia (세리프)', fontConsolas: 'Consolas (고정폭)',
      optTitle: 'PowerPoint 테마 옵션 및 개인 설정', optSub: '슬라이드 쇼 시각 효과, 타이포그래피 및 바이닐 턴테이블 동작을 사용자 지정합니다.',
      optThemeCard: '테마 색상', optToneLight: 'Office 클래식 라이트 (화이트/레드)', optToneDark: 'Office 클래식 다크 (블랙/레드)',
      optOverlayCard: '슬라이드 배경 화이트 마스크', optOverlayHint: '앨범 아트워크 위의 마스크 투명도를 조절하여 가사 가독성을 높입니다.',
      optVinylCard: '턴테이블 회전 속도 옵션', optTransCard: '가사 번역 및 발음 표시', optChkTrans: '이중 언어 번역 / 로마자 부제 표시 켜기',
      aboutTitle: 'Microsoft PowerPoint ECHO 워크샵 스킨 정보', aboutSub: 'ECHO 뮤직 플레이어를 위해 제작된 클래식 PowerPoint 프레젠테이션 테마입니다.',
      aboutNameLbl: '플러그인 이름: ', aboutNameVal: 'Microsoft PowerPoint · 프레젠테이션 스킨',
      aboutIdLbl: '플러그인 ID: ', aboutVerLbl: '버전: ', aboutProtoLbl: '디자인 프로토타입: ',
      aboutProtoVal: 'PowerPoint Fluent 리본 + 슬라이드 대기열 + 바이닐 턴테이블 + 실시간 가사',
      aboutRefLbl: '디자인 참조: ', aboutAuthorLbl: '제작자: ', aboutProtocolLbl: '워크샵 프로토콜: ',
      notesPlaceholder: '클릭하여 발표자 노트 추가 (현재 곡 정보 및 메모)',
      searchPlaceholder: '프레젠테이션 및 라이브러리 검색 (Alt+Q)...', notes: '슬라이드 노트', comments: '메모', fitWindow: '맞춤',
      slideCounter: '슬라이드', of: '/', sheetUnit: '장', songUnit: '곡', pureMusic: '연주곡',
      nowPlaying: '재생 중', badgeQueue: '대기열', badgeLibrary: '라이브러리', localFolder: '로컬 폴더',
      navInfo: '정보', navPlaylists: '재생목록 열기 및 가져오기', navOptions: '테마 옵션', navAbout: '플러그인 정보',
      infoTitle: 'Microsoft PowerPoint · 오디오 및 문서 정보',
      infoSub: '프레젠테이션 오디오 트랙 속성, 하드웨어 사양 및 호스트 런타임 상태 개요입니다.',
      infoSecTrack: '트랙 및 메타데이터', infoTrackTitle: '현재 곡: ', infoTrackArtist: '아티스트: ',
      infoTrackAlbum: '앨범: ', infoTrackPath: '저장 경로: ', infoSecAudio: '오디오 하드웨어 및 디코딩 사양',
      infoCodec: '코덱: ', infoSampleRate: '샘플링 레이트: ', infoBitDepth: '비트 심도: ',
      infoChannels: '채널 구성: ', infoBitrate: '비트레이트: ', infoBackend: '출력 백엔드: ',
      infoSecEngine: '재생 엔진 및 렌더링 상태', infoPlayState: '재생 상태: ', infoProgress: '현재 진행 시간: ',
      infoLevels: '피크 및 RMS: ', infoVolume: '마스터 볼륨: ', infoLyricsType: '가사 상태: ',
      infoWordStats: '가사 글자 수: ', infoPresentationZoom: '슬라이드 배율: ', infoBlur: '가우시안 블러: ',
      playlistsTitle: '재생목록 열기 및 가져오기',
      playlistsSub: '폴더 트리 계층 구조로 라이브러리와 재생목록을 탐색하고 슬라이드 대기열로 가져옵니다.',
      filterPlaylists: '재생목록 또는 곡 이름 필터...', btnRefresh: '새로고침', btnReturn: '프레젠테이션으로 돌아가기',
      localAlbums: '로컬 앨범', customPlaylists: '사용자 재생목록', albumUnit: '장',
      importAndPlay: '전체 가져와 재생', appendToQueue: '대기열에 추가', loadingPlaylists: '재생목록 읽는 중...',
      loadingTracks: '트랙 목록 불러오는 중...', emptyPlaylist: '재생목록이 비어 있습니다', emptyLibraryHint: '라이브러리에 재생목록이나 트랙이 없습니다.',
      toastVstRack: 'VST3 이펙트 랙이 전환되었습니다', toastVstMgr: 'VST3 플러그인 관리자를 여는 중...',
      toastImporting: '재생목록을 가져와 재생합니다', toastAppended: '대기열에 추가되었습니다',
      fontGroupCurated: '추천 서체', fontGroupSystem: '시스템 설치 글꼴',
      pointerCentered: '재생 위치가 중앙으로 지정되었습니다', infoPlayStatePaused: '일시정지됨',
      noSyncedLyrics: '동기화된 가사가 없습니다', enjoyMelody: '연주곡입니다, 멜로디를 감상하세요'
    },
    'zh-TW': {
      tabFile: '檔案(F)', tabHome: '常用(H)', tabEdit: '編輯(E)', tabMultitrack: '多軌(M)', tabClip: '剪輯(C)',
      tabEffects: '效果(S)', tabZoom: '縮放(Z)', tabTransport: '控制(T)', tabWindow: '視窗(W)', tabHelp: '說明(H)', tabDesign: '設計(G)',
      autosave: '自動儲存', autoSave: '自動儲存', searchResultsHeader: '曲庫與佇列搜尋結果', searchHeader: '曲庫與佇列搜尋結果',
      btnNewSlide: '下一首', subNextTrack: '投影片',
      grpClipboard: '剪貼簿', grpSlides: '投影片', grpFont: '字型 (歌詞自訂)', grpParagraph: '段落與對齊',
      grpDesignMask: '設計與遮罩', grpSlideShow: '放映', grpClipData: '剪貼簿資料', grpStateReset: '狀態重設',
      grpRenderQueue: '轉譯與佇列', grpSoundstage: '音訊聲場', grpPlayhead: '播放針定位', grpTimeJump: '時間跳轉',
      grpVstPlugins: 'VST3 音訊外掛', grpAnalyzerCtrl: '分析器控制', grpSlideZoom: '投影片縮放', grpWorkspaceView: '工作區檢視',
      grpBasicTransport: '基礎走帶', grpPlayModeVol: '播放模式與音量', grpColorTheme: '色彩佈景主題', grpSettingsCenter: '設定中心',
      grpDocsSupport: '文件與支援', grpSlideLayoutStyle: '投影片版面配置樣式', grpBgVariants: '背景與變體',
      btnCopyLyrics: '複製歌詞', btnCopyTitle: '複製歌名', btnCopyArtist: '複製歌手', btnCopyMeta: '複製中繼資料', btnCopyTimecode: '複製時間碼',
      btnNextSlide: '下一首', btnPrevSlide: '上一首', btnResetLayout: '重設版面', transPill: '譯/音',
      lblOverlay: '底紋遮罩:', lblBlur: '高斯模糊:', lblVinylSpeed: '唱片轉速:', btnSlideShow: '投影片放映',
      btnResetPeak: '重設峰值', btnRescan: '重新掃描', btnRefreshLyrics: '重新整理歌詞',
      btnSyncQueue: '同步佇列', btnRealtime: '即時監聽', btnHiRes: '高精取樣', btnStereoOut: '立體聲輸出', btnSpectrumBars: '頻譜分析柱',
      btnCenterPin: '置中指標', btnCenterPinSub: '定位醒目提示', btnSeekStart: '回到開頭', btnSeekEnd: '快轉至尾',
      btnRw5: '倒轉 5 秒', btnRw15: '倒轉 15 秒', btnFf5: '快轉 5 秒', btnFf15: '快轉 15 秒',
      btnVstRack: 'VST3 機架', btnVstMgr: '外掛管理', btnFreezeSpec: '凍結分析', btnClearLevels: '清空電平',
      btnZoomFit: '最適視窗', btnZoomIn: '放大', btnZoomOut: '縮小', btnToggleQueue: '左側佇列欄', btnToggleNotes: '備忘稿欄',
      btnPlayPause: '播放 / 暫停', btnPrevTrack: '上一首', btnNextTrack: '下一首',
      btnRepeatMode: '循環模式', btnShufflePlay: '隨機播放', btnMuteToggle: '靜音切換', btnMaxVolume: '最大音量',
      btnLightMode: '明亮模式', btnLightModeSub: '經典辦公', btnDarkMode: '暗黑模式', btnDarkModeSub: '護眼深色',
      btnPersonalizeSub: '個人化', btnShortcuts: '快速鍵指南', btnReloadRuntime: '重新載入',
      layoutVinylTitle: '經典黑膠', layoutVinylDesc: '唱機與參數', layoutNeteaseTitle: '網易雲樣式', layoutNeteaseDesc: '大封面與歌詞',
      layoutAppleTitle: 'Apple Music', layoutAppleDesc: '動感漸層全景', btnThemeMode: '主題模式', btnResetBg: '重設背景',
      queuePanelTitle: '投影片播放佇列', neAlbum: '專輯:', neArtist: '歌手:', neSource: '來源:', lyricsWord: '歌詞',
      spellCheckOk: '無拼字錯誤', playedWordPrefix: '已播', totalWordPrefix: '共', charUnit: '字',
      lh12: '單倍行距', lh15: '1.5 倍行距', lh18: '1.8 倍行距', lh20: '2.0 雙倍行距',
      rpm33: '33 RPM (沉浸)', rpm45: '45 RPM (標準)', rpm78: '78 RPM (輕快)', rpmStop: '暫停旋轉',
      fontYaHei: '微軟正黑體 / 雅黑', fontSimSun: '新細明體 (SimSun)', fontKaiTi: '標楷體 (KaiTi)',
      fontFangSong: '仿宋體 (FangSong)', fontGeorgia: 'Georgia (優雅襯線)', fontConsolas: 'Consolas (等寬極簡)',
      optTitle: 'PowerPoint 佈景主題選項與個人化設定', optSub: '自訂投影片放映視覺效果、排版偏好與黑膠播放器執行行為。',
      optThemeCard: '外觀佈景主題色彩', optToneLight: 'Office 經典明亮 (白/紅)', optToneDark: 'Office 經典暗黑 (黑/紅)',
      optOverlayCard: '投影片專輯底圖白色遮罩', optOverlayHint: '調節專輯底圖之上的白色遮罩不透明度，兼顧專輯藝術視覺與歌詞排版對比度。',
      optVinylCard: '黑膠唱片機旋轉選項', optTransCard: '歌詞翻譯與羅馬音', optChkTrans: '開啟雙語翻譯 / 羅馬音副標題展示',
      aboutTitle: '關於 Microsoft PowerPoint ECHO 工作坊外觀', aboutSub: '專為 ECHO 音樂播放器打造的 Microsoft PowerPoint 經典簡報佈景主題介面。',
      aboutNameLbl: '外掛名稱：', aboutNameVal: 'Microsoft PowerPoint · 簡報外觀佈景主題',
      aboutIdLbl: '外掛 ID：', aboutVerLbl: '目前版本：', aboutProtoLbl: '設計原型：',
      aboutProtoVal: 'PowerPoint Fluent Ribbon + 左側縮圖佇列 + 旋轉黑膠 + 底圖遮罩 + 動態歌詞排版',
      aboutRefLbl: '藍本參考：', aboutAuthorLbl: '作者：', aboutProtocolLbl: '工作坊協定：',
      notesPlaceholder: '按一下此處新增簡報備忘稿 (目前曲目資訊與備忘錄)',
      searchPlaceholder: '搜尋簡報與曲庫 (Alt+Q)...', notes: '備忘稿', comments: '註解', fitWindow: '最適大小',
      slideCounter: '投影片', of: '共', sheetUnit: '張', songUnit: '首', pureMusic: '純音樂',
      nowPlaying: '正在播放', badgeQueue: '目前佇列', badgeLibrary: '本機曲庫', localFolder: '本機目錄',
      navInfo: '資訊', navPlaylists: '開啟與匯入歌單', navOptions: '佈景主題選項', navAbout: '關於外掛程式',
      infoTitle: 'Microsoft PowerPoint · 音訊與文件資訊',
      infoSub: '目前簡報音軌屬性、硬體規格與主機執行狀態概覽。',
      infoSecTrack: '曲目與中繼資料', infoTrackTitle: '目前曲目：', infoTrackArtist: '演出者：',
      infoTrackAlbum: '專輯：', infoTrackPath: '儲存路徑：', infoSecAudio: '音訊硬體與解碼規格',
      infoCodec: '編碼格式：', infoSampleRate: '取樣率：', infoBitDepth: '位元深度：',
      infoChannels: '聲道設定：', infoBitrate: '瞬時位元率：', infoBackend: '音訊輸出後端：',
      infoSecEngine: '播放引擎與轉譯狀態', infoPlayState: '播放狀態：', infoProgress: '目前進度：',
      infoLevels: '峰值與 RMS：', infoVolume: '主音量：', infoLyricsType: '歌詞狀態：',
      infoWordStats: '歌詞字數：', infoPresentationZoom: '畫布縮放：', infoBlur: '高斯模糊：',
      playlistsTitle: '開啟與匯入歌單',
      playlistsSub: '瀏覽本機曲庫與使用者歌單，以資料夾目錄樹層級呈現，支援一鍵載入投影片播放佇列。',
      filterPlaylists: '輸入關鍵字篩選歌單或曲目...', btnRefresh: '重新整理曲庫', btnReturn: '返回簡報',
      localAlbums: '本機專輯組合', customPlaylists: '自訂歌單', albumUnit: '張',
      importAndPlay: '全部匯入並播放', appendToQueue: '追加至佇列', loadingPlaylists: '正在讀取曲庫與歌單目錄...',
      loadingTracks: '正在展開曲目清單...', emptyPlaylist: '歌單為空', emptyLibraryHint: '目前曲庫或歌單暫無曲目。',
      toastVstRack: '已呼叫 VST3 效果機架', toastVstMgr: '正在開啟 VST3 外掛程式管理員...',
      toastImporting: '正在匯入歌單並播放', toastAppended: '已追加至佇列',
      fontGroupCurated: '常用精選字型', fontGroupSystem: '系統已安裝字型',
      pointerCentered: '播放指標已置中定位', infoPlayStatePaused: '已暫停',
      noSyncedLyrics: '暫無同步捲動歌詞', enjoyMelody: '純音樂，請享受旋律'
    }
  };

  const LANG_NAMES = {
    'zh-CN': '简体中文',
    'en-US': 'English',
    'fr-FR': 'Français',
    'es-ES': 'Español',
    'ru-RU': 'Русский',
    'ar-SA': 'العربية',
    'ja-JP': '日本語',
    'ko-KR': '한국어',
    'zh-TW': '繁體中文'
  };

  function t(key) {
    const lang = state.currentLang || 'zh-CN';
    return I18N[lang]?.[key] || I18N['zh-CN']?.[key] || '';
  }

  function setLanguage(lang, save = true) {
    if (!I18N[lang]) lang = 'zh-CN';
    state.currentLang = lang;
    window.__pptSetLanguage = setLanguage;
    safeStorageSet('echo-ppt-lang', lang);
    if (save) savePresetSettings();

    // Update status bar language pill text
    if (els.sbLangText) {
      els.sbLangText.textContent = LANG_NAMES[lang] || lang;
    }

    // Update dropdown active highlight
    if (els.sbLangDropdown) {
      els.sbLangDropdown.querySelectorAll('.sb-lang-item').forEach(it => {
        it.classList.toggle('active', it.dataset.lang === lang);
      });
    }

    // Update search placeholder
    if (els.searchInput) {
      els.searchInput.placeholder = t('searchPlaceholder') || '搜索演示文稿与曲库 (Alt+Q)...';
    }
    if (els.iptPlaylistsSearch) {
      els.iptPlaylistsSearch.placeholder = t('filterPlaylists') || '输入关键字过滤歌单或曲目...';
    }
    if (els.notesInput) {
      const ph = t('notesPlaceholder') || '单击此处添加演示文稿备注 (当前曲目信息与备忘录)';
      els.notesInput.placeholder = ph;
      if (!state.currentTrack || els.notesInput.value.startsWith('单击此处') || els.notesInput.value.startsWith('Click to') || els.notesInput.value.startsWith('Cliquez') || els.notesInput.value.startsWith('Haga clic') || els.notesInput.value.startsWith('Нажмите') || els.notesInput.value.startsWith('انقر') || els.notesInput.value.startsWith('クリック') || els.notesInput.value.startsWith('클릭') || els.notesInput.value.startsWith('按一下')) {
        els.notesInput.value = state.currentTrack ? `${state.currentTrack.title || ''} - ${state.currentTrack.artist || ''} (${state.currentTrack.album || ''})` : ph;
      }
    }

    // Update Ribbon Tab names
    const tabMapping = {
      'tab-file': 'tabFile',
      'tab-home': 'tabHome',
      'tab-edit': 'tabEdit',
      'tab-multitrack': 'tabMultitrack',
      'tab-clip': 'tabClip',
      'tab-effects': 'tabEffects',
      'tab-zoom': 'tabZoom',
      'tab-transport': 'tabTransport',
      'tab-window': 'tabWindow',
      'tab-help': 'tabHelp',
      'tab-design': 'tabDesign'
    };
    Object.keys(tabMapping).forEach(tabId => {
      const el = $(tabId);
      if (el) el.textContent = t(tabMapping[tabId]);
    });

    // Update all elements with data-i18n
    document.querySelectorAll('[data-i18n]').forEach(el => {
      const key = el.dataset.i18n;
      const text = t(key);
      if (text) el.textContent = text;
    });

    // Update top right theme toggle label
    if (els.themeLabel) {
      els.themeLabel.textContent = state.settings.tone === 'dark' ? (t('btnDarkMode') || '暗黑模式') : (t('btnLightMode') || '明亮模式');
    }

    // Update queue count badge and slide counter
    if (els.queueCountBadge) {
      els.queueCountBadge.textContent = `${state.queue.items.length} ${t('songUnit') || '首'}`;
    }
    syncActiveThumbnail();
    updateLyricWordStats(state.lyrics.currentIndex);
    renderLyricsList();
    if (state.playlistsLoaded || state.playlists.length > 0 || state.albums.length > 0 || state.folders.length > 0) {
      renderPlaylistsTree();
    }
  }


  // --- Inter-Process Communication (IPC) ---
  let reqSeq = 1;
  const pendingHostRequests = new Map();

  function sendHostCommand(command, payload = {}) {
    const requestId = `req_${Date.now()}_${reqSeq++}`;
    const msg = {
      type: 'echo:workshop-ui:command',
      requestId,
      command,
      payload
    };
    try {
      window.parent.postMessage(msg, '*');
    } catch {}
    return requestId;
  }

  function requestHostCommand(command, payload = {}, timeoutMs = 4000) {
    return new Promise((resolve) => {
      const requestId = sendHostCommand(command, payload);
      const timer = setTimeout(() => {
        pendingHostRequests.delete(requestId);
        resolve({ ok: false, error: 'timeout' });
      }, timeoutMs);
      pendingHostRequests.set(requestId, { resolve, timer });
    });
  }

  function sendModBridge(type, payload = {}) {
    try {
      window.parent.postMessage({ type, ...payload }, '*');
    } catch {}
  }

  
  // --- Safe Storage Shim (handles sandboxed iframe SecurityError) ---
  const memoryStorage = Object.create(null);
  function safeStorageGet(key) {
    try {
      const val = window.localStorage.getItem(key);
      if (val !== null && val !== undefined) return val;
    } catch {}
    return Object.prototype.hasOwnProperty.call(memoryStorage, key) ? memoryStorage[key] : null;
  }
  function safeStorageSet(key, value) {
    memoryStorage[key] = String(value);
    try {
      window.localStorage.setItem(key, String(value));
    } catch {}
  }
  function safeStorageRemove(key) {
    delete memoryStorage[key];
    try {
      window.localStorage.removeItem(key);
    } catch {}
  }

  // --- Preset Memory & Persistence System ---
  let isPresetLoaded = false;
  const PRESET_SETTINGS_KEY = 'ppt_user_preset_settings';

  function savePresetSettings() {
    if (!isPresetLoaded) return;
    try {
      const preset = {
        tone: state.settings.tone || 'light',
        activeRibbonTab: state.settings.activeRibbonTab || 'home',
        slideLayout: state.settings.slideLayout || 'ppt-vinyl',
        zoomPercent: typeof state.settings.zoomPercent === 'number' ? state.settings.zoomPercent : 100,
        bgOverlayOpacity: typeof state.settings.bgOverlayOpacity === 'number' ? state.settings.bgOverlayOpacity : 20,
        bgBlur: typeof state.settings.bgBlur === 'number' ? state.settings.bgBlur : 16,
        vinylSpeed: state.settings.vinylSpeed || 'normal',
        showTranslation: state.settings.showTranslation !== false,
        leftQueueVisible: els.pptThumbnailsPanel ? !els.pptThumbnailsPanel.classList.contains('collapsed') : true,
        notesVisible: els.pptNotesBar ? !els.pptNotesBar.classList.contains('collapsed') : true,
        autoSave: els.chkAutosave ? els.chkAutosave.checked : true,
        lang: state.currentLang || 'zh-CN'
      };
      const jsonStr = JSON.stringify(preset);
      safeStorageSet(PRESET_SETTINGS_KEY, jsonStr);
      sendHostCommand('storage:set', { key: PRESET_SETTINGS_KEY, value: jsonStr });
      sendModBridge('echo:powerpoint:savePreset', { preset });
    } catch {}
  }

  function applyBgOverlayOpacity(val, save = true) {
    val = Math.max(0, Math.min(100, parseInt(val, 10) || 0));
    state.settings.bgOverlayOpacity = val;
    if (els.rngBgOverlay) els.rngBgOverlay.value = val;
    if (els.lblOverlayVal) els.lblOverlayVal.textContent = `${val}%`;
    if (els.designRngOverlay) els.designRngOverlay.value = val;
    if (els.designLblOverlay) els.designLblOverlay.textContent = `${val}%`;
    const optRange = $('opt-bg-overlay-range');
    if (optRange) optRange.value = val;
    const optVal = $('opt-bg-overlay-val');
    if (optVal) optVal.textContent = `${val}%`;

    const op = val / 100;
    const color = state.settings.tone === 'dark' ? `rgba(0, 0, 0, ${op})` : `rgba(255, 255, 255, ${op})`;
    document.documentElement.style.setProperty('--slide-bg-overlay', color);
    if (els.slideBgOverlay) {
      els.slideBgOverlay.classList.toggle('no-overlay', val === 0);
      els.slideBgOverlay.style.opacity = val === 0 ? '0' : '1';
    }
    if (save) savePresetSettings();
  }

  function applyBgBlur(val, save = true) {
    val = Math.max(0, Math.min(64, parseInt(val, 10) || 0));
    state.settings.bgBlur = val;
    if (els.rngBgBlur) els.rngBgBlur.value = val;
    if (els.lblBlurVal) els.lblBlurVal.textContent = `${val}px`;
    if (els.designRngBlur) els.designRngBlur.value = val;
    if (els.designLblBlur) els.designLblBlur.textContent = `${val}px`;
    document.documentElement.style.setProperty('--slide-blur', `${val}px`);
    if (els.slideBgCover) {
      els.slideBgCover.classList.toggle('no-blur', val === 0);
      if (val === 0) {
        els.slideBgCover.style.filter = 'none';
        els.slideBgCover.style.transform = 'none';
        els.slideBgCover.style.opacity = '1';
      } else {
        els.slideBgCover.style.filter = '';
        els.slideBgCover.style.transform = '';
        els.slideBgCover.style.opacity = '';
      }
    }
    if (save) savePresetSettings();
  }

  function applyVinylSpeed(val, save = true) {
    state.settings.vinylSpeed = val;
    if (els.selVinylSpeed) els.selVinylSpeed.value = val;
    if (els.designSelVinylSpeed) els.designSelVinylSpeed.value = val;
    const optSpeed = $('opt-vinyl-speed-select');
    if (optSpeed) optSpeed.value = val;

    if (val === 'slow') {
      document.documentElement.style.setProperty('--vinyl-speed', '24s');
    } else if (val === 'fast') {
      document.documentElement.style.setProperty('--vinyl-speed', '10s');
    } else if (val === 'stop') {
      document.documentElement.style.setProperty('--vinyl-speed', '0s');
    } else {
      document.documentElement.style.setProperty('--vinyl-speed', '16s');
    }
    if (els.vinylRecordDisc) {
      const isPlaying = state.playback?.status === 'playing';
      const shouldPause = !(isPlaying && val !== 'stop');
      els.vinylRecordDisc.classList.toggle('paused', shouldPause);
    }
    if (save) savePresetSettings();
  }

  function switchRibbonTab(tabKey, save = true) {
    if (!tabKey || tabKey === 'file') return;
    state.settings.activeRibbonTab = tabKey;

    const tabs = els.ribbonTabs || document.querySelectorAll('.ribbon-tab');
    if (tabs) {
      tabs.forEach(t => {
        const isMatch = (t.dataset.tab === tabKey);
        t.classList.toggle('active', isMatch);
        t.setAttribute('aria-selected', isMatch ? 'true' : 'false');
      });
    }

    const panels = els.ribbonPanels || document.querySelectorAll('.ribbon-panel');
    if (panels) {
      panels.forEach(panel => {
        panel.classList.toggle('active', panel.id === `panel-${tabKey}`);
      });
    }

    if (save) savePresetSettings();
  }

  function loadAndApplyPresetSettings(presetObj) {
    try {
      let p = presetObj;
      if (!p) {
        const raw = safeStorageGet(PRESET_SETTINGS_KEY);
        p = raw ? JSON.parse(raw) : null;
      }
      if (!p) return;

      // 1. Language
      try {
        const savedLang = p.lang || safeStorageGet('echo-ppt-lang') || 'zh-CN';
        setLanguage(savedLang, false);
      } catch {}

      // 2. Tone (light / dark)
      try {
        const savedTone = p.tone || safeStorageGet('ppt_theme_tone') || 'light';
        updateTheme(savedTone, false);
      } catch {}

      // 3. Slide Layout (ppt-vinyl / netease / applemusic)
      try {
        const savedLayout = p.slideLayout || safeStorageGet('ppt_slide_layout') || 'ppt-vinyl';
        setSlideLayout(savedLayout, false, false);
      } catch {}

      // 4. Zoom percentage
      try {
        const savedZoom = typeof p.zoomPercent === 'number' ? p.zoomPercent : 100;
        setZoom(savedZoom, false);
      } catch {}

      // 5. Overlay opacity
      try {
        const savedOverlay = typeof p.bgOverlayOpacity === 'number' ? p.bgOverlayOpacity : 20;
        applyBgOverlayOpacity(savedOverlay, false);
      } catch {}

      // 6. Background blur
      try {
        const savedBlur = typeof p.bgBlur === 'number' ? p.bgBlur : 16;
        applyBgBlur(savedBlur, false);
      } catch {}

      // 7. Vinyl speed
      try {
        const savedSpeed = p.vinylSpeed || 'normal';
        applyVinylSpeed(savedSpeed, false);
      } catch {}

      // 8. Translation toggle
      try {
        if (typeof p.showTranslation === 'boolean') {
          state.settings.showTranslation = p.showTranslation;
          if (els.btnToggleTranslation) {
            els.btnToggleTranslation.classList.toggle('active', p.showTranslation);
          }
          const chk = $('chk-opt-show-trans');
          if (chk) chk.checked = p.showTranslation;
        }
      } catch {}

      // 9. Left queue visibility
      try {
        if (typeof p.leftQueueVisible === 'boolean') {
          if (els.pptThumbnailsPanel) {
            els.pptThumbnailsPanel.classList.toggle('collapsed', !p.leftQueueVisible);
          }
          if (els.btnViewToggleQueue) {
            els.btnViewToggleQueue.classList.toggle('active', p.leftQueueVisible);
          }
        }
      } catch {}

      // 10. Notes bar visibility
      try {
        if (typeof p.notesVisible === 'boolean') {
          if (els.pptNotesBar) {
            els.pptNotesBar.classList.toggle('collapsed', !p.notesVisible);
          }
          if (els.btnViewToggleNotes) {
            els.btnViewToggleNotes.classList.toggle('active', p.notesVisible);
          }
          if (els.btnSbNotes) {
            els.btnSbNotes.classList.toggle('active', p.notesVisible);
          }
        }
      } catch {}

      // 11. AutoSave checkbox
      try {
        if (typeof p.autoSave === 'boolean' && els.chkAutosave) {
          els.chkAutosave.checked = p.autoSave;
        }
      } catch {}

      // 12. Active Ribbon Tab
      try {
        const savedTab = p.activeRibbonTab || 'home';
        switchRibbonTab(savedTab, false);
      } catch {}

      // 13. Lyrics font & styles
      try {
        loadAndApplyLyricsFontSettings();
      } catch {}
    } catch (err) {
      console.warn('Failed to restore preset settings:', err);
    }
  }

  // --- Theme Controller ---
  function updateTheme(tone, save = true) {
    state.settings.tone = tone;
    if (tone === 'dark') {
      document.body.classList.remove('tone-light');
      document.body.classList.add('tone-dark');
      if (els.themeLabel) els.themeLabel.textContent = t('btnDarkMode') || '暗黑模式';
      document.documentElement.style.setProperty('--lyrics-color', '#f3f2f1');
      const op = (typeof state.settings.bgOverlayOpacity === 'number' ? state.settings.bgOverlayOpacity : 20) / 100;
      document.documentElement.style.setProperty('--slide-bg-overlay', `rgba(0, 0, 0, ${op})`);
    } else {
      document.body.classList.remove('tone-dark');
      document.body.classList.add('tone-light');
      if (els.themeLabel) els.themeLabel.textContent = t('btnLightMode') || '明亮模式';
      document.documentElement.style.setProperty('--lyrics-color', '#201f1e');
      const op = (typeof state.settings.bgOverlayOpacity === 'number' ? state.settings.bgOverlayOpacity : 20) / 100;
      document.documentElement.style.setProperty('--slide-bg-overlay', `rgba(255, 255, 255, ${op})`);
    }
    if (save) {
      safeStorageSet('ppt_theme_tone', tone);
      savePresetSettings();
    }
  }

  // --- Slide Presentation Layout Switcher (PowerPoint / NetEase / Apple Music) ---
  function setSlideLayout(layoutName, notify = true, save = true) {
    if (!layoutName) layoutName = 'ppt-vinyl';
    state.settings.slideLayout = layoutName;
    if (save) {
      safeStorageSet('ppt_slide_layout', layoutName);
      savePresetSettings();
    }

    // Switch class on pptSlide
    if (els.pptSlide) {
      els.pptSlide.classList.remove('layout-ppt-vinyl', 'layout-netease', 'layout-applemusic');
      els.pptSlide.classList.add(`layout-${layoutName}`);
    }

    // Toggle container elements
    if (els.slideLayoutVinyl) els.slideLayoutVinyl.hidden = (layoutName !== 'ppt-vinyl');
    if (els.slideLayoutNetease) els.slideLayoutNetease.hidden = (layoutName !== 'netease');
    if (els.slideLayoutAppleMusic) els.slideLayoutAppleMusic.hidden = (layoutName !== 'applemusic');

    // Update active state on ribbon cards
    if (els.btnLayoutPptVinyl) els.btnLayoutPptVinyl.classList.toggle('active', layoutName === 'ppt-vinyl');
    if (els.btnLayoutNetease) els.btnLayoutNetease.classList.toggle('active', layoutName === 'netease');
    if (els.btnLayoutAppleMusic) els.btnLayoutAppleMusic.classList.toggle('active', layoutName === 'applemusic');

    // Re-render and synchronize immediately
    state.lyrics.currentIndex = -1;
    renderLyricsList();
    if (state.playback.positionSeconds) {
      syncLyricsWithTime(state.playback.positionSeconds * 1000);
    }
    if (state.currentTrack) {
      updateTrackMetadata(state.currentTrack);
    }
    if (state.currentCoverUrl) {
      applyCoverArt(state.currentCoverUrl);
    }

    if (notify) {
      const names = {
        'ppt-vinyl': 'PowerPoint 经典黑胶唱机版式',
        'netease': '网易云音乐歌词页样式',
        'applemusic': 'Apple Music 歌词页样式'
      };
      showToast(`已切换至 ${names[layoutName] || layoutName}`);
    }
  }

  // --- Track Metadata & Cover Art ---
  let lastLoadedTrackId = null;
  let lastMetadataSig = '';
  let lastCoverTrackId = null;
  let lastExtractedColorUrl = '';

  function setTextIfDiff(el, text) {
    if (el && el.textContent !== text) {
      el.textContent = text;
    }
  }

  function updateTrackMetadata(track) {
    if (!track) return;
    const isNewTrack = Boolean(track.id && track.id !== lastLoadedTrackId);
    if (isNewTrack) {
      lastLoadedTrackId = track.id;
      lastPosSec = 0;
    }
    state.currentTrack = track;
    const title = track.title || '等待播放歌曲';
    const artist = track.artist || '—';
    const album = track.album || '—';
    const sourceText = track.source || (track.id && track.id.startsWith('streaming:') ? '网易云音乐' : 'ECHO 本地曲库');
    const metaSig = `${track.id || ''}|${title}|${artist}|${album}|${sourceText}`;

    if (metaSig !== lastMetadataSig) {
      lastMetadataSig = metaSig;

      // Top window & document titles
      setTextIfDiff(els.docTitle, title);
      const nextDocTitle = `${title} - Microsoft PowerPoint`;
      if (document.title !== nextDocTitle) document.title = nextDocTitle;

      // Slide Stage Vinyl captions
      setTextIfDiff(els.metaTrackTitle, title);
      setTextIfDiff(els.metaTrackArtist, artist);
      setTextIfDiff(els.metaTrackAlbum, album);

      // NetEase captions
      setTextIfDiff(els.neTrackTitle, title);
      setTextIfDiff(els.neMetaAlbum, album);
      setTextIfDiff(els.neMetaArtist, artist);
      setTextIfDiff(els.neMetaSource, sourceText);

      // Apple Music captions
      setTextIfDiff(els.amTrackTitle, title);
      setTextIfDiff(els.amTrackArtistAlbum, `${artist} — ${album}`);

      // Bottom Player bar
      setTextIfDiff(els.pcTitle, title);
      setTextIfDiff(els.pcArtist, artist);

      // Notes bar placeholder
      if (els.notesInput && els.notesInput.value.startsWith('单击此处添加')) {
        els.notesInput.value = `${title} - ${artist} (${album})`;
      }

      // Modal details
      setTextIfDiff(els.mTrackTitle, title);
      setTextIfDiff(els.mTrackArtist, artist);
      setTextIfDiff(els.mTrackAlbum, album);
    }

    // Cover art application: upgrade to high-resolution large (768x768) and deduplicate by trackId
    let coverSource = track.coverUrl || track.coverThumb;
    if (coverSource && typeof coverSource === 'string') {
      coverSource = coverSource.replace('echo-cover://thumb/', 'echo-cover://large/').replace('echo-cover://album/', 'echo-cover://large/');
    }
    const needCoverForTrack = isNewTrack || !state.currentCoverUrl || (track.id && lastCoverTrackId !== track.id);
    if (needCoverForTrack) {
      if (coverSource) {
        applyCoverArt(coverSource, track.id);
      } else if (isNewTrack) {
        sendModBridge('echo:powerpoint:getCover', { trackId: track.id });
      }
    }

    // Request lyrics ONLY when track actually changes to eliminate pause-flicker
    if (isNewTrack) {
      sendModBridge('echo:powerpoint:getLyrics', { trackId: track.id });
      requestFullQueue();
    }

    // If queue is empty, immediately render current track as Slide 1 and request full queue
    if (!state.queue.items || state.queue.items.length === 0) {
      state.queue.items = [track];
      updateQueueList([track], true);
      requestFullQueue();
    } else if (state.queue.items.length === 1 && state.queue.items[0]._isSingleFallback) {
      const fbTrack = { ...track, _isSingleFallback: true };
      state.queue.items = [fbTrack];
      updateQueueList([fbTrack], true);
      requestFullQueue();
    }

    // Highlight active left thumbnail
    if (isNewTrack) {
      syncActiveThumbnail();
    }
  }

  let pendingCoverToken = 0;
  function applyCoverArt(url, trackId = null) {
    if (!url || typeof url !== 'string') return;
    // Reject tiny cross-mod WebP thumbnails (<12KB) when we already have a cover
    if (url.startsWith('data:image/webp;base64,') && url.length < 12000 && state.currentCoverUrl) {
      return;
    }
    if (url.includes('echo-cover://thumb/') || url.includes('echo-cover://album/')) {
      url = url.replace('echo-cover://thumb/', 'echo-cover://large/').replace('echo-cover://album/', 'echo-cover://large/');
    }

    const effTrackId = trackId || state.currentTrack?.id || null;
    // Strict deduplication: if exact same URL is already applied, do nothing
    if (url === state.currentCoverUrl) {
      if (effTrackId) lastCoverTrackId = effTrackId;
      return;
    }
    // If we already loaded a verified high-res cover for this exact trackId, do not swap between echo-cover://large and data:image/jpeg on state ticks
    if (effTrackId && effTrackId === lastCoverTrackId && state.currentCoverUrl && els.coverImg && els.coverImg.naturalWidth >= 256) {
      return;
    }

    state.currentCoverUrl = url;
    if (effTrackId) lastCoverTrackId = effTrackId;

    const myToken = ++pendingCoverToken;
    const preImg = new Image();
    preImg.crossOrigin = 'anonymous';
    const commitCoverDom = () => {
      if (myToken !== pendingCoverToken) return;
      if (els.coverImg && els.coverImg.getAttribute('src') !== url) {
        els.coverImg.src = url;
      }
      if (els.neCoverImg && els.neCoverImg.getAttribute('src') !== url) {
        els.neCoverImg.src = url;
      }
      if (els.amCoverImg && els.amCoverImg.getAttribute('src') !== url) {
        els.amCoverImg.src = url;
      }
      const nextBg = `url("${url}")`;
      if (els.slideBgCover && els.slideBgCover.style.backgroundImage !== nextBg) {
        els.slideBgCover.style.backgroundImage = nextBg;
      }
      // Only sync the slide thumbnail that matches the exact track currently being updated
      if (effTrackId) {
        updateThumbnailCardCover(effTrackId, null, null, url);
      }
      if (preImg.naturalWidth > 0) {
        extractVibrantThemeColorsFromImage(preImg, url);
      }
    };

    preImg.onload = () => {
      if (typeof preImg.decode === 'function') {
        preImg.decode().then(commitCoverDom).catch(commitCoverDom);
      } else {
        commitCoverDom();
      }
    };
    preImg.onerror = () => {
      commitCoverDom();
    };
    preImg.src = url;
  }

  // --- Apple Music Perceptual Color Extraction & Fluid Mesh Grading ---
  function rgbToHsl(r, g, b) {
    r /= 255; g /= 255; b /= 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h = 0, s = 0;
    const l = (max + min) / 2;
    if (max !== min) {
      const d = max - min;
      s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
      switch (max) {
        case r: h = ((g - b) / d + (g < b ? 6 : 0)) / 6; break;
        case g: h = ((b - r) / d + 2) / 6; break;
        case b: h = ((r - g) / d + 4) / 6; break;
      }
    }
    return { h: h * 360, s, l };
  }

  function hslToRgb(h, s, l) {
    h = ((h % 360) + 360) % 360 / 360;
    let r, g, b;
    if (s === 0) {
      r = g = b = l;
    } else {
      const hue2rgb = (p, q, t) => {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1 / 6) return p + (q - p) * 6 * t;
        if (t < 1 / 2) return q;
        if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
        return p;
      };
      const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
      const p = 2 * l - q;
      r = hue2rgb(p, q, h + 1 / 3);
      g = hue2rgb(p, q, h);
      b = hue2rgb(p, q, h - 1 / 3);
    }
    return {
      r: Math.round(Math.max(0, Math.min(255, r * 255))),
      g: Math.round(Math.max(0, Math.min(255, g * 255))),
      b: Math.round(Math.max(0, Math.min(255, b * 255)))
    };
  }

  function gradeAppleMusicColor(rgb, minL, maxL, minS, maxS, hueShift = 0) {
    const hsl = rgbToHsl(rgb.r, rgb.g, rgb.b);
    const h = (hsl.h + hueShift + 360) % 360;
    // Preserve monochrome character if cover is genuinely grayscale, otherwise boost saturation into Apple Music's lush palette range
    const isGray = hsl.s < 0.07;
    const s = isGray ? hsl.s : Math.max(minS, Math.min(maxS, hsl.s * 1.38 + 0.14));
    const l = Math.max(minL, Math.min(maxL, hsl.l * 0.85 + 0.08));
    return hslToRgb(h, s, l);
  }

  function extractVibrantThemeColorsFromImage(img, url) {
    if (!img || !url || url === lastExtractedColorUrl) return;
    lastExtractedColorUrl = url;
    try {
      const canvas = document.createElement('canvas');
      const size = 48;
      canvas.width = size;
      canvas.height = size;
      const ctx = canvas.getContext('2d', { willReadFrequently: true });
      if (!ctx) return;
      ctx.drawImage(img, 0, 0, size, size);
      const data = ctx.getImageData(0, 0, size, size).data;

      // 24 Hue buckets (15 deg each) + 4 spatial quadrants (TL, TR, BL, BR)
      const hueBins = Array.from({ length: 24 }, () => ({ count: 0, weight: 0, r: 0, g: 0, b: 0, sSum: 0 }));
      const quadBest = [null, null, null, null];
      const half = size / 2;

      for (let y = 0; y < size; y++) {
        for (let x = 0; x < size; x++) {
          const idx = (y * size + x) * 4;
          const a = data[idx + 3];
          if (a < 128) continue;
          const r = data[idx];
          const g = data[idx + 1];
          const b = data[idx + 2];

          const hsl = rgbToHsl(r, g, b);
          // Skip extreme near-black or washed-out pure white border pixels
          if (hsl.l < 0.07 || (hsl.l > 0.93 && hsl.s < 0.15)) continue;

          // Perceptual vibrancy score: strongly favors saturated mid-tones (Apple Music focal colors)
          const midToneWeight = 1 - Math.abs(hsl.l - 0.48) * 1.35;
          const vibrancy = (hsl.s * 1.85 + 0.15) * Math.max(0.15, midToneWeight);

          const binIdx = Math.min(23, Math.floor(hsl.h / 15));
          const bin = hueBins[binIdx];
          bin.count++;
          bin.weight += vibrancy;
          bin.r += r * vibrancy;
          bin.g += g * vibrancy;
          bin.b += b * vibrancy;
          bin.sSum += hsl.s;

          const qIdx = (y >= half ? 2 : 0) + (x >= half ? 1 : 0);
          if (!quadBest[qIdx] || vibrancy > quadBest[qIdx].score) {
            quadBest[qIdx] = { r, g, b, h: hsl.h, s: hsl.s, l: hsl.l, score: vibrancy };
          }
        }
      }

      // Collect ranked hue clusters
      const clusters = hueBins
        .filter(b => b.count > 0 && b.weight > 0)
        .map(b => ({
          r: Math.round(b.r / b.weight),
          g: Math.round(b.g / b.weight),
          b: Math.round(b.b / b.weight),
          score: b.weight * (1 + (b.sSum / b.count))
        }))
        .sort((a, b) => b.score - a.score);

      const distinct = [];
      for (const c of clusters) {
        const isDistinct = distinct.every(d => {
          const dr = c.r - d.r, dg = c.g - d.g, db = c.b - d.b;
          return Math.sqrt(dr * dr + dg * dg + db * db) > 38;
        });
        if (isDistinct) {
          distinct.push(c);
          if (distinct.length >= 5) break;
        }
      }

      const primary = distinct[0] || quadBest[0] || { r: 35, g: 95, b: 165 };
      // Combine spatial quadrant fidelity with dominant hue clusters
      const raw1 = quadBest[0] && quadBest[0].score > 0.35 ? quadBest[0] : primary;
      const raw2 = distinct[1] || quadBest[1] || primary;
      const raw3 = distinct[2] || quadBest[2] || raw1;
      const raw4 = distinct[3] || quadBest[3] || raw2;
      const raw5 = distinct[4] || primary;

      // Grade each blob into Apple Music's lush, legible dynamic range
      const c1 = gradeAppleMusicColor(raw1, 0.28, 0.45, 0.58, 0.88, 0);
      const c2 = gradeAppleMusicColor(raw2, 0.26, 0.44, 0.55, 0.86, distinct.length < 2 ? 22 : 0);
      const c3 = gradeAppleMusicColor(raw3, 0.22, 0.38, 0.52, 0.85, distinct.length < 3 ? -24 : 0);
      const c4 = gradeAppleMusicColor(raw4, 0.25, 0.42, 0.54, 0.86, distinct.length < 4 ? 34 : 0);
      const c5 = gradeAppleMusicColor(raw5, 0.30, 0.48, 0.60, 0.90, 12);
      const base = gradeAppleMusicColor(primary, 0.10, 0.15, 0.35, 0.65, 0);

      // Scope CSS variables directly to amFluidMeshBg & pptSlide (avoid mutating document.documentElement!)
      const targets = [els.amFluidMeshBg, els.pptSlide].filter(Boolean);
      for (const target of targets) {
        target.style.setProperty('--am-theme-c1', `rgba(${c1.r}, ${c1.g}, ${c1.b}, 0.88)`);
        target.style.setProperty('--am-theme-c2', `rgba(${c2.r}, ${c2.g}, ${c2.b}, 0.82)`);
        target.style.setProperty('--am-theme-c3', `rgba(${c3.r}, ${c3.g}, ${c3.b}, 0.86)`);
        target.style.setProperty('--am-theme-c4', `rgba(${c4.r}, ${c4.g}, ${c4.b}, 0.82)`);
        target.style.setProperty('--am-theme-c5', `rgba(${c5.r}, ${c5.g}, ${c5.b}, 0.62)`);
        target.style.setProperty('--am-theme-base', `rgb(${base.r}, ${base.g}, ${base.b})`);
      }
    } catch (eCanvas) {}
  }

  // --- Playback State & Clock ---
  let lastRenderedIsPlaying = null;
  let lastRenderedShuffle = null;
  let lastRenderedRepeatMode = null;

  function updatePlaybackState(playback) {
    if (!playback) return;
    Object.assign(state.playback, playback);

    const isPlaying = state.playback.state === 'playing';

    if (isPlaying !== lastRenderedIsPlaying) {
      lastRenderedIsPlaying = isPlaying;

      // Play/Pause icon in bottom player bar
      if (els.icoPlayMain) {
        if (isPlaying) {
          els.icoPlayMain.innerHTML = '<defs><linearGradient id="tpa-grad-main" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" stop-color="#f26522"/><stop offset="100%" stop-color="#c43e1c"/></linearGradient></defs><rect x="18" y="14" width="9" height="36" rx="4.5" fill="url(#tpa-grad-main)"/><rect x="37" y="14" width="9" height="36" rx="4.5" fill="url(#tpa-grad-main)"/>';
        } else {
          els.icoPlayMain.innerHTML = '<defs><linearGradient id="tpl-grad-main" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" stop-color="#f26522"/><stop offset="100%" stop-color="#c43e1c"/></linearGradient></defs><path d="M20 15a3 3 0 0 1 4.6-2.5l28 17a3 3 0 0 1 0 5l-28 17A3 3 0 0 1 20 49V15z" fill="url(#tpl-grad-main)"/>';
        }
      }

      // Play/Pause icon in transport ribbon panel
      if (els.btnTransPlaypause) {
        els.btnTransPlaypause.classList.toggle('active', isPlaying);
      }

      // Turntable Vinyl disc rotation & Tonearm
      if (els.vinylRecordDisc) {
        const shouldPause = !(isPlaying && state.settings.vinylSpeed !== 'stop');
        els.vinylRecordDisc.classList.toggle('paused', shouldPause);
      }
      if (els.turntableTonearm) {
        els.turntableTonearm.classList.toggle('resting', !isPlaying);
      }
      setTextIfDiff(els.vinylStatusPill, isPlaying ? '播放中' : '已暂停');
      setTextIfDiff(els.mPlayState, isPlaying ? '正在播放' : '已暂停');

      // Apple Music play button icon
      if (els.amIcoPlay) {
        if (isPlaying) {
          els.amIcoPlay.innerHTML = '<path d="M6 19h4V5H6v14zm8-14v14h4V5h-4z"/>';
        } else {
          els.amIcoPlay.innerHTML = '<path d="M8 5v14l11-7z"/>';
        }
      }
    }

    // Shuffle & Repeat states (only mutate DOM when changed)
    const curShuffle = Boolean(state.playback.shuffleEnabled);
    if (curShuffle !== lastRenderedShuffle) {
      lastRenderedShuffle = curShuffle;
      if (els.btnTShuffle) els.btnTShuffle.classList.toggle('active', curShuffle);
    }
    const rm = state.playback.repeatMode || 'off';
    if (rm !== lastRenderedRepeatMode) {
      lastRenderedRepeatMode = rm;
      if (els.btnTRepeat) {
        els.btnTRepeat.classList.toggle('active', rm !== 'off');
        els.btnTRepeat.title = rm === 'one' ? '单曲循环' : (rm === 'all' ? '列表循环' : '顺序播放');
      }
    }

    updateClock(state.playback.positionSeconds, state.playback.durationSeconds);
  }

  let isDraggingProgress = false;
  let lastPosSec = 0;
  function updateClock(posSec, durSec) {
    if (typeof posSec !== 'number' || isNaN(posSec)) return;
    if (typeof durSec === 'number' && !isNaN(durSec) && durSec > 0) {
      state.playback.durationSeconds = durSec;
    }

    // Jitter suppression: if playing and not dragging, ignore small backward ticks (< 1.5s)
    const isPlaying = state.playback.state === 'playing';
    // If paused, avoid microsecond clock tick triggering redundant re-syncs
    if (!isPlaying && !isDraggingProgress && Math.abs(posSec - (state.playback.positionSeconds || 0)) < 0.05) {
      return;
    }

    if (isPlaying && !isDraggingProgress && posSec < lastPosSec && (lastPosSec - posSec) < 1.5) {
      posSec = lastPosSec;
    } else {
      lastPosSec = posSec;
    }

    state.playback.positionSeconds = posSec;

    if (els.timeCurrent) els.timeCurrent.textContent = formatSeconds(posSec);
    if (els.timeTotal && state.playback.durationSeconds > 0) {
      els.timeTotal.textContent = formatSeconds(state.playback.durationSeconds);
    }

    if (state.playback.durationSeconds > 0 && !isDraggingProgress && els.rngProgress) {
      const pct = Math.max(0, Math.min(1000, Math.round((posSec / state.playback.durationSeconds) * 1000)));
      els.rngProgress.value = pct;
    }

    // Apple Music In-Slide Clock & Slider
    if (els.amTimeCur) els.amTimeCur.textContent = formatSeconds(posSec);
    if (els.amTimeRem && state.playback.durationSeconds > 0) {
      const remSec = Math.max(0, state.playback.durationSeconds - posSec);
      els.amTimeRem.textContent = `-${formatSeconds(remSec)}`;
    }
    if (state.playback.durationSeconds > 0 && !isDraggingProgress && els.amProgressSlider) {
      const pct = Math.max(0, Math.min(1000, Math.round((posSec / state.playback.durationSeconds) * 1000)));
      els.amProgressSlider.value = pct;
    }

    // Sync active lyric line
    syncLyricsWithTime(posSec * 1000);
  }

  // --- Synchronized Lyrics Display ---
  let lastLyricsHash = '';
  function setLyricsData(lyrics) {
    if (!lyrics) return;
    const lines = Array.isArray(lyrics.lines) ? lyrics.lines : [];
    const hash = `${lyrics.kind || 'synced'}_${lines.length}_${lines[0]?.text || ''}_${lines[lines.length - 1]?.text || ''}`;
    if (hash === lastLyricsHash && state.lyrics.lines && state.lyrics.lines.length > 0) {
      // Deduplicated: same lyrics already loaded, do not wipe DOM or reset stats!
      return;
    }
    lastLyricsHash = hash;
    state.lyrics.kind = lyrics.kind || 'synced';
    state.lyrics.lines = lines;
    state.lyrics.currentIndex = -1;

    if (els.lyricStateHint) {
      if (lyrics.kind === 'instrumental') {
        els.lyricStateHint.textContent = '纯音乐伴奏';
      } else if (state.lyrics.lines.length > 0) {
        els.lyricStateHint.textContent = `同步歌词 (${state.lyrics.lines.length} 行)`;
      } else {
        els.lyricStateHint.textContent = '纯文本歌词';
      }
    }

    renderLyricsList();
    if (state.playback.positionSeconds) {
      syncLyricsWithTime(state.playback.positionSeconds * 1000);
    } else {
      updateLyricWordStats(-1);
    }
  }

  function updateLyricWordStats(activeLineIdx = -1) {
    if (!els.sbLyricStatsText) return;
    const lines = state.lyrics.lines || [];
    const lyrLbl = t('lyricsWord') || '歌词';
    const charUnit = t('charUnit') || '字';
    if (state.lyrics.kind === 'instrumental' || lines.length === 0) {
      els.sbLyricStatsText.textContent = `${t('pureMusic') || '纯音乐'} (0 / 0 ${charUnit})`;
      return;
    }

    let totalWords = 0;
    let playedWords = 0;

    for (let i = 0; i < lines.length; i++) {
      const text = (lines[i].text || '').replace(/\s+/g, '');
      const len = text.length;
      totalWords += len;
      if (i <= activeLineIdx && activeLineIdx >= 0) {
        playedWords += len;
      }
    }

    state.lyrics.playedWords = playedWords;
    state.lyrics.totalWords = totalWords;
    els.sbLyricStatsText.textContent = `${lyrLbl}: ${t('playedWordPrefix') || '已播'} ${playedWords} ${charUnit} / ${t('totalWordPrefix') || '共'} ${totalWords} ${charUnit}`;
  }

  function renderLyricsList() {
    const isInst = state.lyrics.kind === 'instrumental';
    const hasLines = Array.isArray(state.lyrics.lines) && state.lyrics.lines.length > 0;
    const instHint = t('enjoyMelody') || '纯音乐，请享受旋律';
    const emptyHint = t('noSyncedLyrics') || '暂无同步滚动歌词';

    // 1. PowerPoint Classic
    if (els.lyricsLinesList) {
      els.lyricsLinesList.innerHTML = '';
      if (isInst) {
        const notice = document.createElement('div');
        notice.className = 'lyric-initial-notice';
        notice.innerHTML = `<p>${instHint}</p><small>Microsoft PowerPoint</small>`;
        els.lyricsLinesList.appendChild(notice);
      } else if (!hasLines) {
        const notice = document.createElement('div');
        notice.className = 'lyric-initial-notice';
        notice.innerHTML = `<p>${emptyHint}</p><small>Microsoft PowerPoint</small>`;
        els.lyricsLinesList.appendChild(notice);
      } else {
        state.lyrics.lines.forEach((line, index) => {
          const row = document.createElement('div');
          row.className = 'lyric-row';
          row.dataset.index = index;
          row.dataset.timeMs = line.timeMs;
          const mainText = document.createElement('span');
          mainText.className = 'lyric-main-text';
          mainText.textContent = line.text;
          row.appendChild(mainText);

          if (state.settings.showTranslation && line.translation) {
            const transText = document.createElement('span');
            transText.className = 'lyric-translation';
            transText.textContent = line.translation;
            row.appendChild(transText);
          }

          row.addEventListener('click', () => {
            const targetSec = (line.timeMs || 0) / 1000;
            sendHostCommand('seek', { positionSeconds: targetSec });
            sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: targetSec });
          });
          els.lyricsLinesList.appendChild(row);
        });
      }
    }

    // 2. NetEase Style
    if (els.neLyricsLines) {
      els.neLyricsLines.innerHTML = '';
      if (isInst || !hasLines) {
        const notice = document.createElement('div');
        notice.className = 'ne-empty-hint';
        notice.innerHTML = `<p>${isInst ? instHint : emptyHint}</p>`;
        els.neLyricsLines.appendChild(notice);
      } else {
        state.lyrics.lines.forEach((line, index) => {
          const row = document.createElement('div');
          row.className = 'ne-lyric-row';
          row.dataset.index = index;
          row.dataset.timeMs = line.timeMs;
          row.textContent = line.text;

          row.addEventListener('click', () => {
            const targetSec = (line.timeMs || 0) / 1000;
            sendHostCommand('seek', { positionSeconds: targetSec });
            sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: targetSec });
          });
          els.neLyricsLines.appendChild(row);
        });
      }
    }

    // 3. Apple Music Style
    if (els.amLyricsLines) {
      els.amLyricsLines.innerHTML = '';
      if (isInst || !hasLines) {
        const notice = document.createElement('div');
        notice.className = 'ne-empty-hint';
        notice.innerHTML = `<p>${isInst ? instHint : emptyHint}</p>`;
        els.amLyricsLines.appendChild(notice);
      } else {
        state.lyrics.lines.forEach((line, index) => {
          const row = document.createElement('div');
          row.className = 'am-lyric-row';
          row.dataset.index = index;
          row.dataset.timeMs = line.timeMs;
          row.textContent = line.text;

          row.addEventListener('click', () => {
            const targetSec = (line.timeMs || 0) / 1000;
            sendHostCommand('seek', { positionSeconds: targetSec });
            sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: targetSec });
          });
          els.amLyricsLines.appendChild(row);
        });
      }
    }
  }

  function syncLyricsWithTime(currentMs, force = false) {
    const lines = state.lyrics.lines;
    if (!lines || lines.length === 0) return;

    let activeIdx = -1;
    for (let i = 0; i < lines.length; i++) {
      if (lines[i].timeMs <= currentMs + 200) {
        activeIdx = i;
      } else {
        break;
      }
    }

    if ((activeIdx !== state.lyrics.currentIndex || force) && activeIdx >= 0) {
      state.lyrics.currentIndex = activeIdx;
      updateLyricWordStats(activeIdx);
      const curLayout = state.settings.slideLayout || 'ppt-vinyl';

      // 1. PowerPoint Classic sync
      if (els.lyricsLinesList) {
        const allRows = els.lyricsLinesList.querySelectorAll('.lyric-row');
        allRows.forEach((r, idx) => {
          const isActive = (idx === activeIdx);
          if (r.classList.contains('active') !== isActive) {
            r.classList.toggle('active', isActive);
          }
          const dist = Math.abs(idx - activeIdx);
          const distBucket = dist <= 3 ? String(dist) : 'far';
          if (r.dataset.focusDistance !== distBucket) {
            r.dataset.focusDistance = distBucket;
          }
        });
        const activeEl = allRows[activeIdx];
        if (activeEl && els.lyricsScrollContainer && curLayout === 'ppt-vinyl') {
          const containerH = els.lyricsScrollContainer.clientHeight;
          const targetTop = activeEl.offsetTop - (containerH / 2) + (activeEl.clientHeight / 2);
          els.lyricsScrollContainer.scrollTo({ top: Math.max(0, targetTop), behavior: 'smooth' });
        }
      }

      // 2. NetEase Style sync
      if (els.neLyricsLines) {
        const neRows = els.neLyricsLines.querySelectorAll('.ne-lyric-row');
        neRows.forEach((r, idx) => {
          const isActive = (idx === activeIdx);
          if (r.classList.contains('active') !== isActive) {
            r.classList.toggle('active', isActive);
          }
        });
        const activeNe = neRows[activeIdx];
        if (activeNe && els.neLyricsScroll && curLayout === 'netease') {
          const containerH = els.neLyricsScroll.clientHeight;
          const targetTop = activeNe.offsetTop - (containerH / 2) + (activeNe.clientHeight / 2);
          els.neLyricsScroll.scrollTo({ top: Math.max(0, targetTop), behavior: 'smooth' });
        }
      }

      // 3. Apple Music Style sync
      if (els.amLyricsLines) {
        const amRows = els.amLyricsLines.querySelectorAll('.am-lyric-row');
        amRows.forEach((r, idx) => {
          const isActive = (idx === activeIdx);
          if (r.classList.contains('active') !== isActive) {
            r.classList.toggle('active', isActive);
          }
        });
        const activeAm = amRows[activeIdx];
        if (activeAm && els.amLyricsScroll && curLayout === 'applemusic') {
          const containerH = els.amLyricsScroll.clientHeight;
          const targetTop = activeAm.offsetTop - (containerH / 2) + (activeAm.clientHeight / 2);
          els.amLyricsScroll.scrollTo({ top: Math.max(0, targetTop), behavior: 'smooth' });
        }
      }
    }
  }

  function centerActiveLyric(force = true) {
    const lines = state.lyrics.lines;
    const curMs = (state.playback.positionSeconds || 0) * 1000;
    if (lines && lines.length > 0) {
      syncLyricsWithTime(curMs, true);
    }
    showToast(t('pointerCentered') || '播放指针已居中定位');
  }

  // --- Left Slide Thumbnails Playlist Queue ---
  let lastQueueFetchAt = 0;
  function requestFullQueue(force = false) {
    const now = Date.now();
    if (!force && now - lastQueueFetchAt < 1200) return;
    lastQueueFetchAt = now;
    sendModBridge('echo:powerpoint:getQueue');
    requestHostCommand('queue:get', {}).then((res) => {
      const q = res?.result || res?.data || res;
      if (q && Array.isArray(q.items) && q.items.length > 0) {
        updateQueueList(q.items);
      } else if (Array.isArray(q) && q.length > 0) {
        updateQueueList(q);
      }
    }).catch(() => {});
  }

  function normalizeQueueItem(it, idx) {
    if (!it || typeof it !== 'object') return null;
    const tr = (it.track && typeof it.track === 'object') ? it.track : it;
    const id = tr.id || it.id || it.trackId || null;
    const queueId = it.queueId || tr.queueId || (id ? `${id}_${idx}` : `q_${idx}`);
    const title = tr.title || it.title || tr.name || it.name || '';
    const artist = tr.artist || it.artist || '—';
    const album = tr.album || it.album || '—';
    const durationSeconds = Number(tr.durationSeconds ?? it.durationSeconds ?? tr.duration ?? it.duration ?? 0) || 0;
    const rawCover = it.coverUrl || it.rawCoverUrl || it.coverThumb || tr.coverUrl || tr.coverThumb || '';
    return {
      ...it,
      id,
      queueId,
      title,
      artist,
      album,
      durationSeconds,
      coverUrl: rawCover || it.coverUrl || tr.coverUrl || '',
      coverThumb: it.coverThumb || tr.coverThumb || rawCover || '',
      rawCoverUrl: it.rawCoverUrl || rawCover || '',
      track: tr,
      _isSingleFallback: Boolean(it._isSingleFallback)
    };
  }

  function updateQueueList(items = [], isSingleFallback = false) {
    if (!Array.isArray(items)) return;
    const normalized = items.map((it, idx) => normalizeQueueItem(it, idx)).filter(Boolean);
    if (isSingleFallback && normalized.length === 1) {
      normalized[0]._isSingleFallback = true;
    }

    // Never overwrite a populated multi-track queue with a single-track fallback or empty array
    const existing = state.queue.items || [];
    if (existing.length > 1 && normalized.length <= 1 && (isSingleFallback || normalized.length === 0)) {
      syncActiveThumbnail();
      return;
    }

    const prevSig = existing.map(it => `${it.queueId || ''}:${it.id || ''}:${it.title || ''}`).join('|');
    const newSig = normalized.map(it => `${it.queueId || ''}:${it.id || ''}:${it.title || ''}`).join('|');
    state.queue.items = normalized;
    const count = normalized.length;
    if (els.queueCountBadge) els.queueCountBadge.textContent = `${count} ${t('songUnit') || '首'}`;

    syncActiveThumbnail();
    if (prevSig !== newSig || !els.thumbnailsList || els.thumbnailsList.children.length !== count) {
      renderSlideThumbnails();
    }
  }

  function syncActiveThumbnail() {
    const items = state.queue.items || [];
    const curId = state.currentTrack?.id || state.playback?.currentTrackId;
    const curQueueId = state.queue.currentQueueId;
    let activeIdx = -1;
    if (curQueueId) {
      activeIdx = items.findIndex(it => it.queueId === curQueueId);
    }
    if (activeIdx === -1 && curId) {
      activeIdx = items.findIndex(it => (it.id === curId || it.track?.id === curId));
    }
    if (activeIdx === -1 && state.currentTrack?.title) {
      activeIdx = items.findIndex(it => (it.title || it.track?.title) === state.currentTrack.title);
    }
    if (activeIdx === -1) activeIdx = 0;
    state.queue.currentIndex = activeIdx;

    if (els.sbSlideCounter) {
      const prefix = t('slideCounter') || '幻灯片';
      const ofStr = t('of') || '共';
      const sheet = t('sheetUnit') || '张';
      els.sbSlideCounter.textContent = `${prefix} ${activeIdx + 1} ${ofStr} ${Math.max(1, items.length)} ${sheet}`;
    }

    if (els.thumbnailsList) {
      const children = els.thumbnailsList.querySelectorAll('.slide-thumbnail-item');
      children.forEach((child, idx) => {
        child.classList.toggle('active', idx === activeIdx);
      });
    }
  }

  function renderSlideThumbnails() {
    if (!els.thumbnailsList) return;
    els.thumbnailsList.innerHTML = '';

    const items = state.queue.items || [];
    if (items.length === 0) {
      const fallbackItem = normalizeQueueItem(state.currentTrack || {
        title: t('nowPlaying') || '当前曲目',
        artist: '—',
        durationSeconds: state.playback.durationSeconds || 0,
        _isSingleFallback: true
      }, 0);
      const card = createThumbnailCard(1, fallbackItem, true);
      els.thumbnailsList.appendChild(card);
      return;
    }

    items.forEach((item, index) => {
      const slideNum = index + 1;
      const isActive = index === state.queue.currentIndex;
      const card = createThumbnailCard(slideNum, item, isActive);
      els.thumbnailsList.appendChild(card);
    });
  }

  function createThumbnailCard(slideNum, item, isActive) {
    const tr = (item && item.track && typeof item.track === 'object') ? item.track : (item || {});
    const trackId = item?.id || tr?.id || null;
    const queueId = item?.queueId || tr?.queueId || null;

    const wrap = document.createElement('div');
    wrap.className = `slide-thumbnail-item ${isActive ? 'active' : ''}`;
    wrap.dataset.index = slideNum - 1;
    if (trackId) wrap.dataset.trackId = trackId;
    if (queueId) wrap.dataset.queueId = queueId;

    const numSpan = document.createElement('span');
    numSpan.className = 'slide-num';
    numSpan.textContent = slideNum;
    wrap.appendChild(numSpan);

    const card = document.createElement('div');
    card.className = 'slide-card';

    let rawCover = item?.coverUrl || item?.rawCoverUrl || item?.coverThumb || tr?.coverUrl || tr?.coverThumb || '';
    if (typeof rawCover === 'string' && rawCover.includes('echo-cover://thumb/')) {
      rawCover = rawCover.replace('echo-cover://thumb/', 'echo-cover://album/');
    }
    let coverSrc = (trackId ? queueCoverCache.get(trackId) : '') || (queueId ? queueCoverCache.get(queueId) : '') || (rawCover ? queueCoverCache.get(rawCover) : '') || '';
    if (!coverSrc) {
      if (rawCover.startsWith('data:') || rawCover.startsWith('echo-cover:')) {
        coverSrc = rawCover;
      } else if (isActive && state.currentCoverUrl && (trackId && (state.currentTrack?.id === trackId || state.playback?.currentTrackId === trackId))) {
        coverSrc = state.currentCoverUrl;
      }
    }

    if (coverSrc) {
      const img = document.createElement('img');
      img.className = 'thumb-slide-bg';
      img.src = coverSrc;
      img.alt = '';
      card.appendChild(img);
    } else {
      const placeholder = document.createElement('div');
      placeholder.className = 'thumb-slide-bg thumb-placeholder';
      placeholder.innerHTML = '<svg viewBox="0 0 64 64" class="ico fluent-svg" fill="none"><defs><linearGradient id="fl90_note_grad" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" stop-color="#b4b2af"/><stop offset="100%" stop-color="#797775"/></linearGradient></defs><rect x="6" y="6" width="52" height="52" rx="10" fill="#e1dfdd" fill-opacity="0.2" stroke="#8a8886" stroke-width="2" stroke-dasharray="4 4"/><circle cx="32" cy="32" r="14" fill="#a19f9d" fill-opacity="0.3"/><circle cx="32" cy="32" r="5" fill="#8a8886"/><path d="M42 20v18a6 6 0 1 1-5-5.9V26l-14 3v13a6 6 0 1 1-5-5.9V24a3 3 0 0 1 2.4-2.9l18-3.9a3 3 0 0 1 3.6 2.8z" fill="url(#fl90_note_grad)"/></svg>';
      card.appendChild(placeholder);

      // Request host bridge to resolve remote streaming cover into CSP-allowed dataUrl
      if (rawCover && (rawCover.startsWith('http://') || rawCover.startsWith('https://'))) {
        sendModBridge('echo:powerpoint:requestCover', {
          trackId,
          queueId,
          url: rawCover,
          isThumb: true
        });
      }
    }

    // 30% Opacity White Overlay as required
    const overlay = document.createElement('div');
    overlay.className = 'thumb-slide-overlay';
    card.appendChild(overlay);

    // Slide Content Float
    const content = document.createElement('div');
    content.className = 'thumb-slide-content';

    const titleDiv = document.createElement('div');
    titleDiv.className = 'thumb-slide-title';
    titleDiv.textContent = item?.title || tr?.title || `${t('slideCounter') || '幻灯片'} ${slideNum}`;
    content.appendChild(titleDiv);

    const metaRow = document.createElement('div');
    metaRow.className = 'thumb-slide-meta-row';

    const artistDiv = document.createElement('div');
    artistDiv.className = 'thumb-slide-artist';
    artistDiv.textContent = item?.artist || tr?.artist || '—';
    metaRow.appendChild(artistDiv);

    const durDiv = document.createElement('div');
    durDiv.className = 'thumb-duration thumb-slide-duration';
    durDiv.textContent = formatSeconds(item?.durationSeconds || tr?.durationSeconds || tr?.duration || 0);
    metaRow.appendChild(durDiv);

    content.appendChild(metaRow);
    card.appendChild(content);
    wrap.appendChild(card);

    // Switch track on click (debounced)
    let lastCardClickTime = 0;
    wrap.addEventListener('click', () => {
      const now = Date.now();
      if (now - lastCardClickTime < 220) return;
      lastCardClickTime = now;
      if (queueId && !String(queueId).startsWith('q_')) {
        sendHostCommand('queue:playItem', { queueId });
      } else if (trackId) {
        sendHostCommand('queue:playTrack', { trackId });
      }
      sendModBridge('echo:powerpoint:playTrack', { trackId, queueId });
    });

    return wrap;
  }

  function updateThumbnailCardCover(trackId, queueId, rawUrl, coverDataUrl) {
    if (!els.thumbnailsList || !coverDataUrl) return;
    const items = state.queue.items || [];
    items.forEach((it, idx) => {
      let match = false;
      if (trackId) {
        match = (it.id === trackId || it.track?.id === trackId);
      } else if (queueId) {
        match = (it.queueId === queueId);
      } else if (rawUrl) {
        match = (it.rawCoverUrl === rawUrl || it.coverThumb === rawUrl || it.coverUrl === rawUrl);
      }
      if (match) {
        it.coverThumb = coverDataUrl;
        const cardItem = els.thumbnailsList.querySelector(`.slide-thumbnail-item[data-index="${idx}"]`);
        if (cardItem) {
          const card = cardItem.querySelector('.slide-card');
          if (card) {
            let img = card.querySelector('img.thumb-slide-bg');
            if (!img) {
              img = document.createElement('img');
              img.className = 'thumb-slide-bg';
              img.alt = '';
              card.insertBefore(img, card.firstChild);
              const placeholder = card.querySelector('.thumb-placeholder');
              if (placeholder) placeholder.remove();
            }
            img.src = coverDataUrl;
          }
        }
      }
    });
  }

  // --- Zoom Transform Controller ---
  function setZoom(pct, save = true) {
    pct = Math.max(50, Math.min(200, pct));
    state.settings.zoomPercent = pct;

    if (els.rngZoom) els.rngZoom.value = pct;
    if (els.lblZoom) els.lblZoom.textContent = `${pct}%`;

    if (els.pptSlide) {
      els.pptSlide.style.transform = `scale(${(pct / 100).toFixed(2)})`;
    }
    if (save) {
      savePresetSettings();
    }
  }

  // --- Universal Robust Clipboard Helper ---
  async function copyToClipboard(text, successToastMsg) {
    if (!text) {
      showToast('暂无内容可复制');
      return false;
    }
    let copied = false;
    try {
      window.focus();
      if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
        await navigator.clipboard.writeText(text);
        copied = true;
      }
    } catch {}

    if (!copied) {
      try {
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.style.position = 'fixed';
        textarea.style.left = '-9999px';
        textarea.style.top = '0';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        textarea.focus();
        textarea.select();
        copied = document.execCommand('copy');
        document.body.removeChild(textarea);
      } catch {}
    }

    try {
      sendModBridge('echo:powerpoint:copyText', { text });
    } catch {}

    if (successToastMsg) {
      showToast(successToastMsg);
    }
    return copied;
  }

  // --- Search Functionality (Alt+Q) ---
  function initSearch() {
    if (!els.searchInput) return;

    if (els.searchWrap) {
      els.searchWrap.addEventListener('click', (e) => {
        if (els.btnSearchClear && (e.target === els.btnSearchClear || els.btnSearchClear.contains(e.target))) {
          return;
        }
        if (els.searchInput) {
          els.searchInput.focus();
        }
      });
    }

    function doSearch(query) {
      query = (query || '').trim().toLowerCase();
      if (!query) {
        if (els.btnSearchClear) els.btnSearchClear.hidden = true;
        if (els.searchDropdown) els.searchDropdown.hidden = true;
        return;
      }

      if (els.btnSearchClear) els.btnSearchClear.hidden = false;

      const queueItems = state.queue.items || [];
      const libraryItems = state.libraryTracks || [];

      const seen = new Set();
      const combined = [];

      for (const it of queueItems) {
        const id = it.id || it.track?.id;
        if (id && !seen.has(id)) {
          seen.add(id);
          combined.push({ ...it, fromQueue: true });
        }
      }

      for (const it of libraryItems) {
        const id = it.id || it.track?.id;
        if (id && !seen.has(id)) {
          seen.add(id);
          combined.push({ ...it, fromQueue: false });
        }
      }

      const matched = combined.filter(it => {
        const t = (it.title || it.track?.title || '').toLowerCase();
        const a = (it.artist || it.track?.artist || '').toLowerCase();
        const alb = (it.album || it.track?.album || '').toLowerCase();
        return t.includes(query) || a.includes(query) || alb.includes(query);
      });

      renderSearchResults(matched, query);

      // Trigger Host Full Library Search Bridge
      sendModBridge('echo:powerpoint:searchTracks', { query });
    }

    function mergeGlobalSearchResults(globalTracks, query) {
      if (!query || !els.searchInput || els.searchInput.value.trim().toLowerCase() !== query.toLowerCase()) return;
      const queueItems = state.queue.items || [];
      const seen = new Set();
      const combined = [];

      // 1. Current Queue Matches First
      for (const it of queueItems) {
        const id = it.id || it.track?.id;
        if (id && !seen.has(id)) {
          const t = (it.title || it.track?.title || '').toLowerCase();
          const a = (it.artist || it.track?.artist || '').toLowerCase();
          const alb = (it.album || it.track?.album || '').toLowerCase();
          if (t.includes(query) || a.includes(query) || alb.includes(query)) {
            seen.add(id);
            combined.push({ ...it, fromQueue: true });
          }
        }
      }

      // 2. Global Library Matches
      for (const it of (globalTracks || [])) {
        const id = it.id || it.track?.id;
        if (id && !seen.has(id)) {
          seen.add(id);
          combined.push({ ...it, fromQueue: false });
        }
      }

      renderSearchResults(combined, query);
    }

    function renderSearchResults(results, query) {
      if (!els.searchResultsList || !els.searchDropdown) return;
      els.searchResultsList.innerHTML = '';
      els.searchDropdown.hidden = false;

      if (results.length === 0) {
        const empty = document.createElement('div');
        empty.className = 'search-empty-msg';
        empty.textContent = `未找到与 "${query}" 匹配的曲目或歌手`;
        els.searchResultsList.appendChild(empty);
        return;
      }

      results.slice(0, 15).forEach((item, idx) => {
        const row = document.createElement('div');
        row.className = 'search-item';
        row.dataset.trackId = item.id || item.track?.id;
        if (item.queueId) row.dataset.queueId = item.queueId;

        const idxSpan = document.createElement('span');
        idxSpan.className = 'search-item-index';
        idxSpan.textContent = idx + 1;
        row.appendChild(idxSpan);

        const info = document.createElement('div');
        info.className = 'search-item-info';

        const tDiv = document.createElement('div');
        tDiv.className = 'search-item-title';
        tDiv.textContent = item.title || item.track?.title || '曲目';

        const badge = document.createElement('span');
        badge.className = 'search-result-badge' + (item.fromQueue ? ' badge-queue' : '');
        badge.textContent = item.fromQueue ? (t('badgeQueue') || '当前队列') : (t('badgeLibrary') || '全曲库');
        tDiv.appendChild(badge);

        info.appendChild(tDiv);

        const aDiv = document.createElement('div');
        aDiv.className = 'search-item-artist';
        const artistName = item.artist || item.track?.artist || '—';
        aDiv.textContent = artistName;
        if (artistName.toLowerCase().includes(query)) {
          aDiv.style.fontWeight = 'bold';
          aDiv.style.color = '#c43e1c';
        }
        info.appendChild(aDiv);

        row.appendChild(info);

        row.addEventListener('click', () => {
          const trackId = item.id || item.track?.id;
          const queueId = item.queueId;
          if (item.fromQueue && queueId && !String(queueId).startsWith('q_')) {
            sendHostCommand('queue:playItem', { queueId });
          } else if (trackId) {
            sendHostCommand('queue:playTrack', { trackId });
          }
          sendModBridge('echo:powerpoint:playTrack', { trackId, queueId });
          els.searchDropdown.hidden = true;
          showToast(`${t('nowPlaying') || '正在播放'}: ${item.title || item.track?.title || ''}`);
        });

        els.searchResultsList.appendChild(row);
      });
    }

    els.searchInput.addEventListener('input', (e) => {
      doSearch(e.target.value);
    });

    if (els.btnSearchClear) {
      els.btnSearchClear.addEventListener('click', () => {
        els.searchInput.value = '';
        els.btnSearchClear.hidden = true;
        if (els.searchDropdown) els.searchDropdown.hidden = true;
        els.searchInput.focus();
      });
    }

    // Close search dropdown on click outside
    document.addEventListener('click', (e) => {
      if (els.searchWrap && !els.searchWrap.contains(e.target)) {
        if (els.searchDropdown) els.searchDropdown.hidden = true;
      }
    });

    // Enter & Escape navigation
    els.searchInput.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        if (els.searchDropdown) els.searchDropdown.hidden = true;
        els.searchInput.blur();
      } else if (e.key === 'Enter') {
        const first = els.searchResultsList?.querySelector('.search-item');
        if (first) first.click();
      }
    });
  }

  // --- Ribbon Secondary Menu Panel Switcher ---
  function initRibbonTabs() {
    if (!els.ribbonTabs) return;

    els.ribbonTabs.forEach(tab => {
      tab.addEventListener('click', () => {
        const tabKey = tab.dataset.tab;
        if (tabKey === 'file') {
          openModal('info');
          return;
        }
        switchRibbonTab(tabKey, true);
      });
    });
  }

  // --- Fullscreen Slideshow (F5) ---
  function toggleSlideshow(force) {
    const isCurrentlySlideshow = document.body.classList.contains('slideshow-mode');
    const willBeSlideshow = (typeof force === 'boolean') ? force : !isCurrentlySlideshow;

    if (willBeSlideshow) {
      document.body.classList.add('slideshow-mode');
      sendModBridge('echo:powerpoint:toggleFullscreen', { fullscreen: true });
      try {
        if (!document.fullscreenElement) {
          document.documentElement.requestFullscreen().catch(() => {});
        }
      } catch {}
      showToast('已开启 PowerPoint 全屏放映模式 (按 Esc 退出)');
    } else {
      document.body.classList.remove('slideshow-mode');
      sendModBridge('echo:powerpoint:toggleFullscreen', { fullscreen: false });
      try {
        if (document.fullscreenElement) {
          document.exitFullscreen().catch(() => {});
        }
      } catch {}
      showToast('已退出全屏放映模式');
    }
  }

  // --- Backstage Office Modal Controller ---
  function openModal(targetTab = 'info') {
    if (!els.modalBackdrop) return;
    els.modalBackdrop.hidden = false;

    // Refresh Rich Track & Hardware Specs
    if (els.mTrackTitle) els.mTrackTitle.textContent = state.currentTrack?.title || '等待播放';
    if (els.mTrackArtist) els.mTrackArtist.textContent = state.currentTrack?.artist || '—';
    if (els.mTrackAlbum) els.mTrackAlbum.textContent = state.currentTrack?.album || '—';
    if (els.mTrackPath) els.mTrackPath.textContent = state.audio.filePath || '本地音乐曲库';

    if (els.mAudioCodec) els.mAudioCodec.textContent = state.audio.codec || 'FLAC';
    if (els.mAudioSamplerate) els.mAudioSamplerate.textContent = state.audio.sampleRate || '44.1 kHz';
    if (els.mAudioBitdepth) els.mAudioBitdepth.textContent = state.audio.bitDepth || '24-bit';
    if (els.mAudioChannels) els.mAudioChannels.textContent = state.audio.channels || '双声道立体声';
    if (els.mAudioBitrate) els.mAudioBitrate.textContent = state.audio.bitrate || '1411 kbps';
    if (els.mAudioBackend) els.mAudioBackend.textContent = state.audio.engine || 'ECHO Core Audio Engine';

    if (els.mPlayState) {
      els.mPlayState.textContent = state.playback.state === 'playing' ? (t('nowPlaying') || '正在播放') : (t('infoPlayStatePaused') || '已暂停');
    }
    if (els.mAudioProgress) {
      els.mAudioProgress.textContent = `${formatSeconds(state.playback.positionSeconds)} / ${formatSeconds(state.playback.durationSeconds)}`;
    }
    if (els.mAudioLevels) {
      els.mAudioLevels.textContent = `${(state.audio.peakDb || -60).toFixed(1)} dB / ${(state.audio.rmsDb || -60).toFixed(1)} dB`;
    }
    if (els.mAudioVolume) {
      els.mAudioVolume.textContent = `${Math.round((state.playback.volume ?? 1) * 100)}%`;
    }
    if (els.mLyricsStatus) {
      els.mLyricsStatus.textContent = state.lyrics.kind === 'instrumental' ? (t('pureMusic') || '纯音乐') : (state.lyrics.kind === 'synced' ? '逐句同步歌词' : '纯文本/无歌词');
    }
    if (els.mLyricsWords) {
      els.mLyricsWords.textContent = `已播 ${state.lyrics.playedWords || 0} 字 / 共 ${state.lyrics.totalWords || 0} 字 (${state.lyrics.lines?.length || 0} 行)`;
    }
    if (els.mPresZoom) {
      els.mPresZoom.textContent = `${state.settings.zoomPercent || 100}%`;
    }
    if (els.mPresBlur) {
      const curBlur = els.rngBgBlur ? els.rngBgBlur.value : 16;
      els.mPresBlur.textContent = `${curBlur}px`;
    }

    switchModalTab(targetTab);
  }

  function closeModal() {
    if (!els.modalBackdrop) return;
    els.modalBackdrop.hidden = true;

    // Restore Home tab active
    const tabHome = $('tab-home');
    if (tabHome) tabHome.click();
  }

  function switchModalTab(tabKey) {
    document.querySelectorAll('.modal-nav-items .nav-item').forEach(item => {
      item.classList.toggle('active', item.dataset.tab === tabKey);
    });

    const panes = {
      info: $('pane-modal-info'),
      playlists: $('pane-modal-playlists'),
      options: $('pane-modal-options'),
      about: $('pane-modal-about')
    };

    Object.keys(panes).forEach(k => {
      if (panes[k]) {
        const isTarget = k === tabKey;
        panes[k].hidden = !isTarget;
        panes[k].classList.toggle('active', isTarget);
      }
    });

    if (tabKey === 'playlists') {
      fetchPlaylistsAndLibrary(true);
      renderPlaylistsTree();
    }
  }

  // --- Playlist and Library Folder Explorer ---
  function synthesizeAlbumsFromTracks(tracks) {
    if (!Array.isArray(tracks) || tracks.length === 0) return [];
    const map = new Map();
    tracks.forEach((raw) => {
      const tr = raw?.track || raw;
      if (!tr || typeof tr !== 'object') return;
      const albTitle = tr.album || t('badgeLibrary') || '本地曲库';
      const key = tr.albumId || `synth_alb_${albTitle}`;
      if (!map.has(key)) {
        map.set(key, {
          id: key,
          title: albTitle,
          artist: tr.artist || '—',
          year: tr.year || '',
          coverThumb: tr.coverThumb || tr.coverUrl || '',
          itemCount: 0,
          tracks: []
        });
      }
      const entry = map.get(key);
      entry.itemCount++;
      entry.tracks.push({
        id: tr.id || raw.id,
        title: tr.title || tr.name || 'Track',
        artist: tr.artist || '—',
        album: albTitle,
        durationSeconds: Number(tr.durationSeconds ?? tr.duration ?? 0) || 0,
        coverThumb: tr.coverThumb || tr.coverUrl || ''
      });
    });
    const list = Array.from(map.values());
    list.forEach((a) => {
      if (!state.albumCache.has(a.id)) {
        state.albumCache.set(a.id, a.tracks);
      }
    });
    return list;
  }

  function fetchPlaylistsAndLibrary(force = false) {
    sendModBridge('echo:powerpoint:getPlaylists');

    // Parallel native Workshop UI Host protocol fallback so it never hangs even if mod.js is still loading
    Promise.all([
      requestHostCommand('library:listPlaylists', {}),
      requestHostCommand('library:listAlbums', { page: 1, pageSize: 100 }),
      requestHostCommand('library:listTracks', { page: 1, pageSize: 100 })
    ]).then(([plRes, albRes, trkRes]) => {
      const extractArr = (r, key) => {
        const d = r?.result ?? r?.data ?? r;
        if (Array.isArray(d)) return d;
        if (d && Array.isArray(d.items)) return d.items;
        if (d && key && Array.isArray(d[key])) return d[key];
        return [];
      };

      const rawPl = extractArr(plRes, 'playlists');
      if (rawPl.length > 0) {
        state.playlists = rawPl.map(p => ({
          id: p.id,
          name: p.name || p.title || '歌单',
          itemCount: p.itemCount ?? p.trackCount ?? p.count ?? (Array.isArray(p.items) ? p.items.length : 0)
        }));
      }

      const rawAlb = extractArr(albRes, 'albums');
      if (rawAlb.length > 0) {
        state.albums = rawAlb.map(a => ({
          id: a.id || a.albumId || a.title || a.name,
          title: a.title || a.name || '未知专辑',
          artist: a.artist || a.albumArtist || '未知艺术家',
          year: a.year || '',
          coverThumb: a.coverThumb || a.coverUrl || (a.id ? `echo-cover://album/${a.id}` : ''),
          itemCount: a.trackCount ?? a.itemCount ?? a.count ?? 0
        }));
      }

      const rawTrk = extractArr(trkRes, 'tracks');
      if (rawTrk.length > 0) {
        state.libraryTracks = rawTrk.map(item => {
          const tObj = item?.track || item;
          return {
            id: tObj.id,
            title: tObj.title || tObj.name || '未知曲目',
            artist: tObj.artist || '未知艺术家',
            album: tObj.album || '—',
            albumId: tObj.albumId || '',
            durationSeconds: Number(tObj.durationSeconds ?? tObj.duration ?? 0) || 0,
            coverThumb: tObj.coverThumb || tObj.coverUrl || (tObj.id ? `echo-cover://thumb/${tObj.id}` : '')
          };
        });
      }

      if ((!state.albums || state.albums.length === 0) && (state.libraryTracks?.length > 0 || state.queue.items?.length > 0)) {
        const sourceTracks = state.libraryTracks?.length > 0 ? state.libraryTracks : state.queue.items;
        state.albums = synthesizeAlbumsFromTracks(sourceTracks);
      }

      state.playlistsLoaded = true;
      renderPlaylistsTree();
    }).catch(() => {
      state.playlistsLoaded = true;
      renderPlaylistsTree();
    });

    // Safety timeout: ensure loading spinner is replaced within 1200ms even in standalone mode
    setTimeout(() => {
      if (!state.playlistsLoaded) {
        state.playlistsLoaded = true;
        if ((!state.albums || state.albums.length === 0) && (state.libraryTracks?.length > 0 || state.queue.items?.length > 0)) {
          state.albums = synthesizeAlbumsFromTracks(state.libraryTracks?.length > 0 ? state.libraryTracks : state.queue.items);
        }
        renderPlaylistsTree();
      }
    }, 1200);
  }

  function renderPlaylistsTree() {
    if (!els.modalPlaylistsTree) return;
    const filterText = (els.iptPlaylistsSearch?.value || '').trim().toLowerCase();
    els.modalPlaylistsTree.innerHTML = '';

    const playlists = state.playlists || [];
    const folders = state.folders || [];
    let albums = state.albums || [];
    if (albums.length === 0 && (state.libraryTracks?.length > 0 || state.queue.items?.length > 0)) {
      albums = synthesizeAlbumsFromTracks(state.libraryTracks?.length > 0 ? state.libraryTracks : state.queue.items);
      state.albums = albums;
    }

    if (playlists.length === 0 && folders.length === 0 && albums.length === 0) {
      const msgText = state.playlistsLoaded
        ? (t('emptyLibraryHint') || '当前曲库或歌单暂无曲目，请先在播放器中添加本地音乐或歌单。')
        : (t('loadingPlaylists') || '正在读取曲库、歌单与专辑目录...');
      els.modalPlaylistsTree.innerHTML = `<div style="padding: 24px; text-align: center; color: var(--text-muted);">${msgText}</div>`;
      return;
    }

    // 1. Local Library Folders
    const filteredFolders = folders.filter(f => !filterText || (f.name || '').toLowerCase().includes(filterText));
    if (filteredFolders.length > 0) {
      const fSec = document.createElement('div');
      fSec.className = 'tree-section-header';
      fSec.innerHTML = `<svg viewBox="0 0 64 64" class="fluent-svg" style="width:16px;height:16px;" fill="none"><path d="M6 18a4 4 0 0 1 4-4h14a4 4 0 0 1 2.8 1.2L31 20h23a4 4 0 0 1 4 4v24a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V18z" fill="#f8a428"/><path d="M6 26a4 4 0 0 1 4-4h44a4 4 0 0 1 4 4v22a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V26z" fill="#ffc83b"/></svg><span>${t('localFolder') || '本地曲库文件夹'} (${filteredFolders.length})</span>`;
      els.modalPlaylistsTree.appendChild(fSec);

      filteredFolders.forEach((folder) => {
        const node = document.createElement('div');
        node.className = 'tree-node';

        const header = document.createElement('div');
        header.className = 'tree-folder-header';
        header.innerHTML = `
          <svg viewBox="0 0 64 64" class="tree-folder-arrow fluent-svg" fill="none"><path d="M24 16l16 16-16 16" stroke="currentColor" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <svg viewBox="0 0 64 64" class="tree-folder-icon fluent-svg" fill="none"><path d="M6 18a4 4 0 0 1 4-4h14a4 4 0 0 1 2.8 1.2L31 20h23a4 4 0 0 1 4 4v24a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V18z" fill="#f8a428"/><path d="M6 26a4 4 0 0 1 4-4h44a4 4 0 0 1 4 4v22a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V26z" fill="#ffc83b"/></svg>
          <span class="tree-folder-title">${folder.name}</span>
          <span class="tree-folder-count">[${t('localFolder') || '本地目录'}]</span>
        `;
        node.appendChild(header);
        els.modalPlaylistsTree.appendChild(node);
      });
    }

    // 2. Local Album Compilations
    const filteredAlbums = albums.filter(alb => !filterText || (alb.title || '').toLowerCase().includes(filterText) || (alb.artist || '').toLowerCase().includes(filterText));
    if (filteredAlbums.length > 0) {
      const albSec = document.createElement('div');
      albSec.className = 'tree-section-header';
      albSec.innerHTML = `<svg viewBox="0 0 64 64" class="fluent-svg" style="width:16px;height:16px;" fill="none"><circle cx="32" cy="32" r="28" fill="#1e1e1e"/><circle cx="32" cy="32" r="10" fill="#d4af37"/><circle cx="32" cy="32" r="3" fill="#111"/></svg><span>${t('localAlbums') || '本地专辑组合'} (${filteredAlbums.length})</span>`;
      els.modalPlaylistsTree.appendChild(albSec);

      filteredAlbums.forEach((alb) => {
        const node = document.createElement('div');
        node.className = 'tree-node';
        node.dataset.albumId = alb.id;

        const coverHtml = alb.coverThumb ? `<img src="${alb.coverThumb}" class="tree-album-thumb" alt="">` : `<svg viewBox="0 0 64 64" class="tree-folder-icon fluent-svg" fill="none"><circle cx="32" cy="32" r="28" fill="#1e1e1e"/><circle cx="32" cy="32" r="10" fill="#d4af37"/><circle cx="32" cy="32" r="3" fill="#111"/></svg>`;

        const header = document.createElement('div');
        header.className = 'tree-folder-header';
        header.innerHTML = `
          <svg viewBox="0 0 64 64" class="tree-folder-arrow fluent-svg" fill="none"><path d="M24 16l16 16-16 16" stroke="currentColor" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/></svg>
          ${coverHtml}
          <span class="tree-folder-title">${alb.title}<span class="tree-album-artist"> - ${alb.artist}${alb.year ? ` (${alb.year})` : ''}</span></span>
          <span class="tree-folder-count">(${alb.itemCount} ${t('songUnit') || '首'})</span>
          <div class="tree-folder-actions">
            <button class="tree-mini-btn btn-import-play" title="${t('importAndPlay') || '全部导入并播放'}">${t('importAndPlay') || '全部导入并播放'}</button>
            <button class="tree-mini-btn btn-import-append" title="${t('appendToQueue') || '追加至队列'}">${t('appendToQueue') || '追加至队列'}</button>
          </div>
        `;

        const children = document.createElement('div');
        children.className = 'tree-node-children';
        children.innerHTML = `<div style="padding: 6px; color: var(--text-muted); font-size: 11px;">${t('loadingTracks') || '正在展开曲目清单...'}</div>`;

        // Expand / Collapse
        header.addEventListener('click', (ev) => {
          if (ev.target.classList.contains('tree-mini-btn')) return;
          const isExp = node.classList.toggle('expanded');
          if (isExp && !state.albumCache.has(alb.id)) {
            sendModBridge('echo:powerpoint:getAlbumTracks', { albumId: alb.id });
            requestHostCommand('library:listAlbumTracks', { albumId: alb.id, page: 1, pageSize: 100 }).then((res) => {
              const d = res?.result ?? res?.data ?? res;
              const arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : (Array.isArray(d?.tracks) ? d.tracks : []));
              if (arr.length > 0) {
                state.albumCache.set(alb.id, arr);
                renderPlaylistChildren(children, arr);
              }
            }).catch(() => {});
          } else if (isExp && state.albumCache.has(alb.id)) {
            renderPlaylistChildren(children, state.albumCache.get(alb.id));
          }
        });

        // Import actions
        const btnPlay = header.querySelector('.btn-import-play');
        if (btnPlay) {
          btnPlay.addEventListener('click', async (ev) => {
            ev.stopPropagation();
            if (!String(alb.id).startsWith('synth_alb_')) {
              sendHostCommand('queue:playAlbum', { albumId: alb.id });
            }
            let arr = state.albumCache.get(alb.id);
            if (!arr || !arr.length) {
              try {
                const res = await requestHostCommand('library:listAlbumTracks', { albumId: alb.id, page: 1, pageSize: 200 });
                const d = res?.result ?? res?.data ?? res;
                arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : (Array.isArray(d?.tracks) ? d.tracks : []));
                if (arr.length) state.albumCache.set(alb.id, arr);
              } catch {}
            }
            if (arr && arr.length) {
              playTrackList(arr);
            }
            sendModBridge('echo:powerpoint:importAlbum', { albumId: alb.id, mode: 'replace' });
            setTimeout(() => requestFullQueue(true), 350);
            showToast(`${t('toastImporting') || '正在导入专辑并播放'}: ${alb.title}`);
          });
        }

        const btnAppend = header.querySelector('.btn-import-append');
        if (btnAppend) {
          btnAppend.addEventListener('click', async (ev) => {
            ev.stopPropagation();
            let arr = state.albumCache.get(alb.id);
            if (!arr || !arr.length) {
              try {
                const res = await requestHostCommand('library:listAlbumTracks', { albumId: alb.id, page: 1, pageSize: 200 });
                const d = res?.result ?? res?.data ?? res;
                arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : (Array.isArray(d?.tracks) ? d.tracks : []));
                if (arr.length) state.albumCache.set(alb.id, arr);
              } catch {}
            }
            if (arr && arr.length) {
              enqueueTrackList(arr);
            }
            sendModBridge('echo:powerpoint:importAlbum', { albumId: alb.id, mode: 'append' });
            setTimeout(() => requestFullQueue(true), 350);
            showToast(`${t('toastAppended') || '已追加至队列'}: ${alb.title}`);
          });
        }

        node.appendChild(header);
        node.appendChild(children);
        els.modalPlaylistsTree.appendChild(node);
      });
    }

    // 3. Playlists
    const filteredPlaylists = playlists.filter(pl => !filterText || (pl.name || '').toLowerCase().includes(filterText));
    if (filteredPlaylists.length > 0) {
      const plSec = document.createElement('div');
      plSec.className = 'tree-section-header';
      plSec.innerHTML = `<svg viewBox="0 0 64 64" class="fluent-svg" style="width:16px;height:16px;" fill="none"><rect x="10" y="12" width="44" height="40" rx="4" fill="#c43e1c"/><path d="M22 24h20M22 32h20M22 40h12" stroke="#ffffff" stroke-width="4" stroke-linecap="round"/></svg><span>${t('customPlaylists') || '自定义歌单'} (${filteredPlaylists.length})</span>`;
      els.modalPlaylistsTree.appendChild(plSec);

      filteredPlaylists.forEach((pl) => {
        const node = document.createElement('div');
        node.className = 'tree-node';
        node.dataset.playlistId = pl.id;

        const header = document.createElement('div');
        header.className = 'tree-folder-header';
        header.innerHTML = `
          <svg viewBox="0 0 64 64" class="tree-folder-arrow fluent-svg" fill="none"><path d="M24 16l16 16-16 16" stroke="currentColor" stroke-width="6" stroke-linecap="round" stroke-linejoin="round"/></svg>
          <svg viewBox="0 0 64 64" class="tree-folder-icon fluent-svg" fill="none"><defs><linearGradient id="fl92_tf_back" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" stop-color="#f8a428"/><stop offset="100%" stop-color="#d86e00"/></linearGradient><linearGradient id="fl92_tf_front" x1="0%" y1="0%" x2="100%" y2="100%"><stop offset="0%" stop-color="#ffc83b"/><stop offset="100%" stop-color="#f8a428"/></linearGradient><filter id="fl92_tf_sh" x="-10%" y="-10%" width="120%" height="130%"><feDropShadow dx="0" dy="2" stdDeviation="2.5" flood-color="#a65400" flood-opacity="0.3"/></filter></defs><path d="M6 18a4 4 0 0 1 4-4h14a4 4 0 0 1 2.8 1.2L31 20h23a4 4 0 0 1 4 4v24a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V18z" fill="url(#fl92_tf_back)"/><rect x="12" y="20" width="40" height="12" rx="2" fill="#ffffff" fill-opacity="0.85"/><path d="M6 26a4 4 0 0 1 4-4h44a4 4 0 0 1 4 4v22a4 4 0 0 1-4 4H10a4 4 0 0 1-4-4V26z" fill="url(#fl92_tf_front)" filter="url(#fl92_tf_sh)"/></svg>
          <span class="tree-folder-title">${pl.name}</span>
          <span class="tree-folder-count">(${pl.itemCount} ${t('songUnit') || '首'})</span>
          <div class="tree-folder-actions">
            <button class="tree-mini-btn btn-import-play" title="${t('importAndPlay') || '全部导入并播放'}">${t('importAndPlay') || '全部导入并播放'}</button>
            <button class="tree-mini-btn btn-import-append" title="${t('appendToQueue') || '追加至队列'}">${t('appendToQueue') || '追加至队列'}</button>
          </div>
        `;

        const children = document.createElement('div');
        children.className = 'tree-node-children';
        children.innerHTML = `<div style="padding: 6px; color: var(--text-muted); font-size: 11px;">${t('loadingTracks') || '正在展开曲目清单...'}</div>`;

        // Expand / Collapse
        header.addEventListener('click', (ev) => {
          if (ev.target.classList.contains('tree-mini-btn')) return;
          const isExp = node.classList.toggle('expanded');
          if (isExp && !state.playlistCache.has(pl.id)) {
            sendModBridge('echo:powerpoint:getPlaylistItems', { playlistId: pl.id });
            requestHostCommand('library:listPlaylistItems', { playlistId: pl.id, page: 1, pageSize: 100 }).then((res) => {
              const d = res?.result ?? res?.data ?? res;
              const arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : []);
              if (arr.length > 0) {
                state.playlistCache.set(pl.id, arr);
                renderPlaylistChildren(children, arr);
              }
            }).catch(() => {});
          } else if (isExp && state.playlistCache.has(pl.id)) {
            renderPlaylistChildren(children, state.playlistCache.get(pl.id));
          }
        });

        // Import actions
        const btnPlay = header.querySelector('.btn-import-play');
        if (btnPlay) {
          btnPlay.addEventListener('click', async (ev) => {
            ev.stopPropagation();
            sendHostCommand('queue:playPlaylist', { playlistId: pl.id });
            let arr = state.playlistCache.get(pl.id);
            if (!arr || !arr.length) {
              try {
                const res = await requestHostCommand('library:listPlaylistItems', { playlistId: pl.id, page: 1, pageSize: 200 });
                const d = res?.result ?? res?.data ?? res;
                arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : []);
                if (arr.length) state.playlistCache.set(pl.id, arr);
              } catch {}
            }
            if (arr && arr.length) {
              playTrackList(arr);
            }
            sendModBridge('echo:powerpoint:importPlaylist', { playlistId: pl.id, mode: 'replace' });
            setTimeout(() => requestFullQueue(true), 350);
            showToast(`${t('toastImporting') || '正在导入歌单并播放'}: ${pl.name}`);
          });
        }

        const btnAppend = header.querySelector('.btn-import-append');
        if (btnAppend) {
          btnAppend.addEventListener('click', async (ev) => {
            ev.stopPropagation();
            let arr = state.playlistCache.get(pl.id);
            if (!arr || !arr.length) {
              try {
                const res = await requestHostCommand('library:listPlaylistItems', { playlistId: pl.id, page: 1, pageSize: 200 });
                const d = res?.result ?? res?.data ?? res;
                arr = Array.isArray(d) ? d : (Array.isArray(d?.items) ? d.items : []);
                if (arr.length) state.playlistCache.set(pl.id, arr);
              } catch {}
            }
            if (arr && arr.length) {
              enqueueTrackList(arr);
            }
            sendModBridge('echo:powerpoint:importPlaylist', { playlistId: pl.id, mode: 'append' });
            setTimeout(() => requestFullQueue(true), 350);
            showToast(`${t('toastAppended') || '已追加至队列'}: ${pl.name}`);
          });
        }

        node.appendChild(header);
        node.appendChild(children);
        els.modalPlaylistsTree.appendChild(node);
      });
    }
  }

  function enqueueTrackList(tracks) {
    if (!Array.isArray(tracks) || tracks.length === 0) return;
    tracks.forEach(raw => {
      const tr = (raw && raw.track && typeof raw.track === 'object') ? raw.track : raw;
      const tid = tr?.id || raw?.id || raw?.trackId;
      if (tid) sendHostCommand('queue:enqueueTrack', { trackId: tid });
    });
  }

  function playTrackList(tracks) {
    if (!Array.isArray(tracks) || tracks.length === 0) return;
    const first = (tracks[0] && tracks[0].track && typeof tracks[0].track === 'object') ? tracks[0].track : tracks[0];
    const firstId = first?.id || tracks[0]?.id || tracks[0]?.trackId;
    if (firstId) {
      sendHostCommand('queue:playTrack', { trackId: firstId });
    }
    for (let i = 1; i < tracks.length; i++) {
      const tr = (tracks[i] && tracks[i].track && typeof tracks[i].track === 'object') ? tracks[i].track : tracks[i];
      const tid = tr?.id || tracks[i]?.id || tracks[i]?.trackId;
      if (tid) sendHostCommand('queue:enqueueTrack', { trackId: tid });
    }
  }

  function renderPlaylistChildren(container, items) {
    container.innerHTML = '';
    if (!items || items.length === 0) {
      container.innerHTML = `<div style="padding: 6px; color: var(--text-muted); font-size: 11px;">${t('emptyPlaylist') || '歌单为空'}</div>`;
      return;
    }
    items.forEach((rawItem, idx) => {
      const item = (rawItem && rawItem.track && typeof rawItem.track === 'object') ? rawItem.track : rawItem;
      const trackId = item?.id || rawItem?.id || rawItem?.trackId;
      const title = item?.title || item?.name || 'Track';
      const artist = item?.artist || '—';
      const dur = Number(item?.durationSeconds ?? item?.duration ?? 0) || 0;
      const row = document.createElement('div');
      row.className = 'tree-track-item';
      row.innerHTML = `
        <span class="tree-track-num">${idx + 1}</span>
        <span class="tree-track-title">${title}</span>
        <span class="tree-track-artist">${artist}</span>
        <span class="tree-track-duration">${formatSeconds(dur)}</span>
      `;
      row.addEventListener('click', () => {
        if (trackId) {
          sendHostCommand('queue:playTrack', { trackId });
          sendModBridge('echo:powerpoint:playTrack', { trackId });
        }
        setTimeout(() => requestFullQueue(true), 300);
        showToast(`${t('nowPlaying') || '正在播放'}: ${title}`);
      });
      container.appendChild(row);
    });
  }

  // --- Audio VU Meter and Specs ---
  function updateAudioProperties(status) {
    if (!status) return;
    // Only update duration from status if valid; active clock is driven by workshop-ui:clock
    if (typeof status.durationSeconds === 'number' && status.durationSeconds > 0) {
      state.playback.durationSeconds = status.durationSeconds;
      setTextIfDiff(els.timeTotal, formatSeconds(status.durationSeconds));
    }
    if (status.currentFilePath) {
      state.audio.filePath = status.currentFilePath;
    }
    if (status.codec && els.propCodec) {
      state.audio.codec = status.codec.toUpperCase();
      setTextIfDiff(els.propCodec, state.audio.codec);
    }
    if (status.fileSampleRate || status.sampleRate) {
      const rate = status.fileSampleRate || status.sampleRate;
      state.audio.sampleRate = `${(rate / 1000).toFixed(1)} kHz`;
      setTextIfDiff(els.propSamplerate, state.audio.sampleRate);
    }
    if (status.bitDepth && els.propBitdepth) {
      state.audio.bitDepth = `${status.bitDepth}-bit`;
      setTextIfDiff(els.propBitdepth, state.audio.bitDepth);
    }
    if (status.bitrate && els.propBitrate) {
      state.audio.bitrate = `${status.bitrate} kbps`;
      setTextIfDiff(els.propBitrate, state.audio.bitrate);
    }
    if (status.channels && els.propChannels) {
      state.audio.channels = status.channels === 2 ? '双声道立体声' : `${status.channels} 声道`;
      setTextIfDiff(els.propChannels, state.audio.channels);
    }
    if (status.audioLevels) {
      const peak = status.audioLevels.inputPeakDb ?? -60;
      const rms = status.audioLevels.inputRmsDb ?? -60;
      updateAudioLevels(peak, rms);
    }
    if (status.outputFacts?.delivery || status.outputDevice) {
      state.audio.engine = status.outputDevice || (status.outputFacts?.delivery === 'local' ? 'ECHO Core Audio' : 'DirectSound');
      setTextIfDiff(els.propBackend, state.audio.engine);
    }
  }

  function updateAudioLevels(peakDb, rmsDb) {
    if (state.audio.frozen) return;
    state.audio.peakDb = peakDb;
    state.audio.rmsDb = rmsDb;

    if (els.vuMeterFill && els.vuDbText) {
      const clamped = Math.max(-60, Math.min(0, peakDb));
      const pct = Math.round(((clamped + 60) / 60) * 100);
      els.vuMeterFill.style.width = `${pct}%`;
      els.vuDbText.textContent = `${peakDb.toFixed(1)} dBFS`;
    }
  }

  const LYRICS_FONT_STORAGE_KEY = 'ppt_lyrics_font_settings';

  function saveLyricsFontSettings() {
    if (!isPresetLoaded) return;
    try {
      const settings = {
        fontFamily: els.selLyricsFont ? els.selLyricsFont.value : undefined,
        fontSize: els.selLyricsSize ? parseInt(els.selLyricsSize.value, 10) : 24,
        isBold: els.btnStyleBold ? els.btnStyleBold.classList.contains('active') : false,
        isItalic: els.btnStyleItalic ? els.btnStyleItalic.classList.contains('active') : false,
        isUnderline: els.btnStyleUnderline ? els.btnStyleUnderline.classList.contains('active') : false,
        isShadow: els.btnStyleShadow ? els.btnStyleShadow.classList.contains('active') : false,
        customColor: els.colorLyricsText ? els.colorLyricsText.value : undefined,
        customHl: els.colorLyricsHl ? els.colorLyricsHl.value : undefined,
        textAlign: state.settings.lyricsTextAlign || 'center',
        lineHeight: els.selLineHeight ? els.selLineHeight.value : '1.4'
      };
      const jsonStr = JSON.stringify(settings);
      safeStorageSet(LYRICS_FONT_STORAGE_KEY, jsonStr);
      sendHostCommand('storage:set', { key: LYRICS_FONT_STORAGE_KEY, value: jsonStr });
      sendModBridge('echo:powerpoint:saveLyricsFont', { fontSettings: settings });
      savePresetSettings();
    } catch {}
  }

  function loadAndApplyLyricsFontSettings(fontObj) {
    try {
      let s = fontObj;
      if (!s) {
        const raw = safeStorageGet(LYRICS_FONT_STORAGE_KEY);
        if (raw) s = JSON.parse(raw);
      }
      if (!s) return;

      if (s.fontFamily) {
        document.documentElement.style.setProperty('--lyrics-font-family', s.fontFamily);
        if (els.selLyricsFont) {
          let found = false;
          for (const opt of els.selLyricsFont.options) {
            if (opt.value === s.fontFamily) { found = true; break; }
          }
          if (!found) {
            const opt = document.createElement('option');
            opt.value = s.fontFamily;
            opt.textContent = s.fontFamily.replace(/['",]|sans-serif|serif|monospace/g, '').trim() || s.fontFamily;
            els.selLyricsFont.appendChild(opt);
          }
          els.selLyricsFont.value = s.fontFamily;
        }
      }

      if (s.fontSize) {
        document.documentElement.style.setProperty('--lyrics-font-size', `${s.fontSize}px`);
        if (els.selLyricsSize) els.selLyricsSize.value = s.fontSize;
      }

      if (s.isBold !== undefined) {
        if (els.btnStyleBold) els.btnStyleBold.classList.toggle('active', !!s.isBold);
        document.documentElement.style.setProperty('--lyrics-font-weight', s.isBold ? '700' : '400');
      }

      if (s.isItalic !== undefined) {
        if (els.btnStyleItalic) els.btnStyleItalic.classList.toggle('active', !!s.isItalic);
        document.documentElement.style.setProperty('--lyrics-font-style', s.isItalic ? 'italic' : 'normal');
      }

      if (s.isUnderline !== undefined) {
        if (els.btnStyleUnderline) els.btnStyleUnderline.classList.toggle('active', !!s.isUnderline);
        document.documentElement.style.setProperty('--lyrics-text-decoration', s.isUnderline ? 'underline' : 'none');
      }

      if (s.isShadow !== undefined) {
        if (els.btnStyleShadow) els.btnStyleShadow.classList.toggle('active', !!s.isShadow);
        document.documentElement.style.setProperty('--lyrics-text-shadow', s.isShadow ? '0 1px 3px rgba(0, 0, 0, 0.35)' : 'none');
      }

      if (s.customColor) {
        if (els.colorLyricsText) els.colorLyricsText.value = s.customColor;
        if (els.textColorBar) els.textColorBar.style.backgroundColor = s.customColor;
        document.documentElement.style.setProperty('--lyrics-color', s.customColor);
        document.documentElement.style.setProperty('--lyrics-custom-color', s.customColor);
      }

      if (s.customHl) {
        if (els.colorLyricsHl) els.colorLyricsHl.value = s.customHl;
        if (els.hlColorBar) els.hlColorBar.style.backgroundColor = s.customHl;
        document.documentElement.style.setProperty('--lyrics-highlight-color', s.customHl);
        document.documentElement.style.setProperty('--lyrics-custom-hl', s.customHl);
      }

      if (s.textAlign) {
        state.settings.lyricsTextAlign = s.textAlign;
        document.documentElement.style.setProperty('--lyrics-text-align', s.textAlign);
        if (els.btnAlignLeft) els.btnAlignLeft.classList.toggle('active', s.textAlign === 'left');
        if (els.btnAlignCenter) els.btnAlignCenter.classList.toggle('active', s.textAlign === 'center');
        if (els.btnAlignRight) els.btnAlignRight.classList.toggle('active', s.textAlign === 'right');
      }

      if (s.lineHeight) {
        if (els.selLineHeight) els.selLineHeight.value = s.lineHeight;
        document.documentElement.style.setProperty('--lyrics-line-height', s.lineHeight);
      }
    } catch {}
  }

  window._pptLoadAndApplyLyricsFontSettings = loadAndApplyLyricsFontSettings;

  // --- Attach All Event Listeners ---
  function setupListeners() {
    // 1. Theme Toggles
    if (els.btnThemeToggle) {
      els.btnThemeToggle.addEventListener('click', () => {
        const next = state.settings.tone === 'light' ? 'dark' : 'light';
        updateTheme(next);
        showToast(`已切换至 Office ${next === 'dark' ? '暗黑' : '经典明亮'}主题`);
      });
    }
    if (els.btnWinThemeLight) {
      els.btnWinThemeLight.addEventListener('click', () => {
        updateTheme('light');
        showToast('已切换至 Office 经典明亮主题');
      });
    }
    if (els.btnWinThemeDark) {
      els.btnWinThemeDark.addEventListener('click', () => {
        updateTheme('dark');
        showToast('已切换至 Office 暗黑夜间主题');
      });
    }

    // 2. Quick Access Toolbar
    if (els.btnQaSave) els.btnQaSave.addEventListener('click', () => showToast('演示文稿已自动保存至本地缓存'));
    if (els.btnQaUndo) els.btnQaUndo.addEventListener('click', () => showToast('已撤消上一步版式操作'));
    if (els.btnQaRedo) els.btnQaRedo.addEventListener('click', () => showToast('已恢复上一步版式操作'));
    if (els.btnQaSlideshow) els.btnQaSlideshow.addEventListener('click', () => toggleSlideshow());
    if (els.btnFullscreenShow) els.btnFullscreenShow.addEventListener('click', () => toggleSlideshow());
    if (els.btnViewSlideshow) els.btnViewSlideshow.addEventListener('click', () => toggleSlideshow());
    if (els.btnHudPrev) els.btnHudPrev.addEventListener('click', () => handlePrev());
    if (els.btnHudPlay) els.btnHudPlay.addEventListener('click', () => handlePlayPause());
    if (els.btnHudNext) els.btnHudNext.addEventListener('click', () => handleNext());
    if (els.btnHudExit) els.btnHudExit.addEventListener('click', () => toggleSlideshow(false));

    // 3. Home Panel Controls
    if (els.btnCopyTitle) {
      els.btnCopyTitle.addEventListener('click', () => {
        const txt = `${state.currentTrack?.title || ''} - ${state.currentTrack?.artist || ''}`;
        copyToClipboard(txt, '已复制歌曲名称到剪贴板');
      });
    }
    if (els.btnCopyLyrics) {
      els.btnCopyLyrics.addEventListener('click', () => {
        const txt = state.lyrics.lines.map(l => l.text).join('\n');
        copyToClipboard(txt, '已复制全部歌词到剪贴板');
      });
    }
    if (els.btnNextSlide) els.btnNextSlide.addEventListener('click', () => sendHostCommand('next'));
    if (els.btnPrevSlide) els.btnPrevSlide.addEventListener('click', () => sendHostCommand('previous'));

    if (els.btnResetLayout) {
      els.btnResetLayout.addEventListener('click', () => {
        safeStorageRemove(LYRICS_FONT_STORAGE_KEY);
        sendModBridge('echo:powerpoint:saveLyricsFont', { fontSettings: null });
        savePresetSettings();
        if (els.selLyricsSize) els.selLyricsSize.value = 24;
        document.documentElement.style.setProperty('--lyrics-font-size', '24px');
        document.documentElement.style.setProperty('--lyrics-font-family', "'Segoe UI', 'Microsoft YaHei', sans-serif");
        document.documentElement.style.setProperty('--lyrics-font-weight', '700');
        document.documentElement.style.setProperty('--lyrics-font-style', 'normal');
        document.documentElement.style.setProperty('--lyrics-text-decoration', 'none');
        document.documentElement.style.setProperty('--lyrics-text-shadow', '0 1px 2px rgba(0, 0, 0, 0.25)');
        document.documentElement.style.setProperty('--lyrics-text-align', 'center');
        if (els.btnAlignCenter) els.btnAlignCenter.click();
        if (els.btnStyleBold) els.btnStyleBold.classList.remove('active');
        if (els.btnStyleItalic) els.btnStyleItalic.classList.remove('active');
        if (els.btnStyleUnderline) els.btnStyleUnderline.classList.remove('active');
        if (els.btnStyleShadow) els.btnStyleShadow.classList.remove('active');
        if (els.selLyricsFont) els.selLyricsFont.value = "'Segoe UI', 'Microsoft YaHei', sans-serif";
        setZoom(100);
        showToast('已重置幻灯片排版与字号');
      });
    }

    // Font family, size, styles
    if (els.selLyricsFont) {
      els.selLyricsFont.addEventListener('change', (e) => {
        if (e.target.value === '__custom__') {
          const font = prompt('请输入您在 Windows 系统中安装的字体名称 (如 LXGW WenKai, OPPO Sans 等):');
          if (font && font.trim()) {
            const fontName = font.trim();
            const val = `'${fontName}', sans-serif`;
            const opt = document.createElement('option');
            opt.value = val;
            opt.textContent = fontName;
            let group = els.selLyricsFont.querySelector('optgroup[data-group="custom"]');
            if (!group) {
              group = document.createElement('optgroup');
              group.dataset.group = 'custom';
              group.label = '自定义系统字体';
              els.selLyricsFont.appendChild(group);
            }
            group.appendChild(opt);
            els.selLyricsFont.value = val;
            document.documentElement.style.setProperty('--lyrics-font-family', val);
            saveLyricsFontSettings();
            showToast(`已应用自定义字体: ${fontName}`);
            return;
          } else {
            loadAndApplyLyricsFontSettings();
            return;
          }
        }
        document.documentElement.style.setProperty('--lyrics-font-family', e.target.value);
        saveLyricsFontSettings();
        showToast(`已更换歌词字体: ${e.target.options[e.target.selectedIndex].text}`);
      });
    }
    if (els.selLyricsSize) {
      els.selLyricsSize.addEventListener('change', (e) => {
        document.documentElement.style.setProperty('--lyrics-font-size', `${e.target.value}px`);
        saveLyricsFontSettings();
      });
    }
    if (els.btnFontInc) {
      els.btnFontInc.addEventListener('click', () => {
        const cur = parseInt(els.selLyricsSize.value, 10) || 24;
        const next = Math.min(48, cur + 2);
        els.selLyricsSize.value = next;
        document.documentElement.style.setProperty('--lyrics-font-size', `${next}px`);
        saveLyricsFontSettings();
      });
    }
    if (els.btnFontDec) {
      els.btnFontDec.addEventListener('click', () => {
        const cur = parseInt(els.selLyricsSize.value, 10) || 24;
        const next = Math.max(14, cur - 2);
        els.selLyricsSize.value = next;
        document.documentElement.style.setProperty('--lyrics-font-size', `${next}px`);
        saveLyricsFontSettings();
      });
    }
    if (els.btnStyleBold) {
      els.btnStyleBold.addEventListener('click', () => {
        els.btnStyleBold.classList.toggle('active');
        const isB = els.btnStyleBold.classList.contains('active');
        document.documentElement.style.setProperty('--lyrics-font-weight', isB ? '700' : '400');
        saveLyricsFontSettings();
      });
    }
    if (els.btnStyleItalic) {
      els.btnStyleItalic.addEventListener('click', () => {
        els.btnStyleItalic.classList.toggle('active');
        const isI = els.btnStyleItalic.classList.contains('active');
        document.documentElement.style.setProperty('--lyrics-font-style', isI ? 'italic' : 'normal');
        saveLyricsFontSettings();
      });
    }
    if (els.btnStyleUnderline) {
      els.btnStyleUnderline.addEventListener('click', () => {
        els.btnStyleUnderline.classList.toggle('active');
        const isU = els.btnStyleUnderline.classList.contains('active');
        document.documentElement.style.setProperty('--lyrics-text-decoration', isU ? 'underline' : 'none');
        saveLyricsFontSettings();
      });
    }
    if (els.btnStyleShadow) {
      els.btnStyleShadow.addEventListener('click', () => {
        els.btnStyleShadow.classList.toggle('active');
        const isS = els.btnStyleShadow.classList.contains('active');
        document.documentElement.style.setProperty('--lyrics-text-shadow', isS ? '0 1px 3px rgba(0, 0, 0, 0.35)' : 'none');
        saveLyricsFontSettings();
      });
    }

    // Color Pickers
    if (els.colorLyricsText) {
      els.colorLyricsText.addEventListener('input', (e) => {
        const c = e.target.value;
        if (els.textColorBar) els.textColorBar.style.backgroundColor = c;
        document.documentElement.style.setProperty('--lyrics-color', c);
        document.documentElement.style.setProperty('--lyrics-custom-color', c);
        saveLyricsFontSettings();
      });
    }
    if (els.colorLyricsHl) {
      els.colorLyricsHl.addEventListener('input', (e) => {
        const c = e.target.value;
        if (els.hlColorBar) els.hlColorBar.style.backgroundColor = c;
        document.documentElement.style.setProperty('--lyrics-highlight-color', c);
        document.documentElement.style.setProperty('--lyrics-custom-hl', c);
        saveLyricsFontSettings();
      });
    }

    // Alignments
    if (els.btnAlignLeft) {
      els.btnAlignLeft.addEventListener('click', () => {
        els.btnAlignLeft.classList.add('active');
        if (els.btnAlignCenter) els.btnAlignCenter.classList.remove('active');
        if (els.btnAlignRight) els.btnAlignRight.classList.remove('active');
        document.documentElement.style.setProperty('--lyrics-text-align', 'left');
        state.settings.lyricsTextAlign = 'left';
        saveLyricsFontSettings();
      });
    }
    if (els.btnAlignCenter) {
      els.btnAlignCenter.addEventListener('click', () => {
        if (els.btnAlignLeft) els.btnAlignLeft.classList.remove('active');
        els.btnAlignCenter.classList.add('active');
        if (els.btnAlignRight) els.btnAlignRight.classList.remove('active');
        document.documentElement.style.setProperty('--lyrics-text-align', 'center');
        state.settings.lyricsTextAlign = 'center';
        saveLyricsFontSettings();
      });
    }
    if (els.btnAlignRight) {
      els.btnAlignRight.addEventListener('click', () => {
        if (els.btnAlignLeft) els.btnAlignLeft.classList.remove('active');
        if (els.btnAlignCenter) els.btnAlignCenter.classList.remove('active');
        els.btnAlignRight.classList.add('active');
        document.documentElement.style.setProperty('--lyrics-text-align', 'right');
        state.settings.lyricsTextAlign = 'right';
        saveLyricsFontSettings();
      });
    }

    if (els.selLineHeight) {
      els.selLineHeight.addEventListener('change', (e) => {
        document.documentElement.style.setProperty('--lyrics-line-height', e.target.value);
        saveLyricsFontSettings();
      });
    }

    if (els.btnToggleTranslation) {
      els.btnToggleTranslation.addEventListener('click', () => {
        state.settings.showTranslation = !state.settings.showTranslation;
        els.btnToggleTranslation.classList.toggle('active', state.settings.showTranslation);
        const chk = $('chk-opt-show-trans');
        if (chk) chk.checked = state.settings.showTranslation;
        savePresetSettings();
        renderLyricsList();
        showToast(state.settings.showTranslation ? '已开启双语翻译/罗马音' : '已隐藏双语翻译/罗马音');
      });
    }

    // White Overlay Slider
    if (els.rngBgOverlay) {
      els.rngBgOverlay.addEventListener('input', (e) => {
        applyBgOverlayOpacity(e.target.value, true);
      });
    }

    // Gaussian Blur Slider
    if (els.rngBgBlur) {
      els.rngBgBlur.addEventListener('input', (e) => {
        applyBgBlur(e.target.value, true);
      });
    }

    // Vinyl Speed
    if (els.selVinylSpeed) {
      els.selVinylSpeed.addEventListener('change', (e) => {
        const val = e.target.value;
        state.settings.vinylSpeed = val;
        if (val === 'slow') {
          document.documentElement.style.setProperty('--vinyl-speed', '24s');
        } else if (val === 'fast') {
          document.documentElement.style.setProperty('--vinyl-speed', '10s');
        } else if (val === 'stop') {
          if (els.vinylRecordDisc) els.vinylRecordDisc.classList.add('paused');
        } else {
          document.documentElement.style.setProperty('--vinyl-speed', '16s');
        }
      });
    }

    // 4. Edit Panel Controls
    if (els.btnEditCopyTitle) {
      els.btnEditCopyTitle.addEventListener('click', () => {
        copyToClipboard(state.currentTrack?.title || '', `已复制歌名到剪贴板: ${state.currentTrack?.title || ''}`);
      });
    }
    if (els.btnEditCopyArtist) {
      els.btnEditCopyArtist.addEventListener('click', () => {
        copyToClipboard(state.currentTrack?.artist || '', `已复制歌手到剪贴板: ${state.currentTrack?.artist || ''}`);
      });
    }
    if (els.btnEditCopyMeta) {
      els.btnEditCopyMeta.addEventListener('click', () => {
        const json = JSON.stringify(state.currentTrack || {}, null, 2);
        copyToClipboard(json, '已复制元数据 JSON 到剪贴板');
      });
    }
    if (els.btnEditCopyTimecode) {
      els.btnEditCopyTimecode.addEventListener('click', () => {
        const cur = formatSeconds(state.playback.positionSeconds);
        const dur = formatSeconds(state.playback.durationSeconds);
        copyToClipboard(`${cur} / ${dur}`, `已复制时间码: ${cur} / ${dur}`);
      });
    }
    if (els.btnEditResetPeak) {
      els.btnEditResetPeak.addEventListener('click', () => {
        updateAudioLevels(-60, -60);
        showToast('已重置峰值电平指示');
      });
    }
    if (els.btnEditRescan) {
      els.btnEditRescan.addEventListener('click', () => {
        sendModBridge('echo:powerpoint:getAudioStatus');
        sendModBridge('echo:powerpoint:getQueue');
        showToast('已重新扫描流媒体与音频参数');
      });
    }
    if (els.btnEditClearLyrics) {
      els.btnEditClearLyrics.addEventListener('click', () => {
        sendModBridge('echo:powerpoint:getLyrics', { trackId: state.currentTrack?.id });
        showToast('正在重新获取流媒体歌词...');
      });
    }

    // 5. Multitrack Panel Controls
    if (els.btnMultiSyncQueue) {
      els.btnMultiSyncQueue.addEventListener('click', () => {
        requestFullQueue(true);
        showToast(t('btnSyncQueue') || '已同步 ECHO 播放列表队列');
      });
    }
    if (els.btnMultiRealtime) els.btnMultiRealtime.addEventListener('click', () => showToast('已开启 WASAPI 实时监听'));
    if (els.btnMultiHighQuality) els.btnMultiHighQuality.addEventListener('click', () => showToast('高精度音频输出已激活'));
    if (els.btnMultiStereo) els.btnMultiStereo.addEventListener('click', () => showToast('双声道立体声声道就绪'));
    if (els.btnMultiSpectrumMode) els.btnMultiSpectrumMode.addEventListener('click', () => showToast('已切换频谱显示模式'));

    // 6. Clip Panel Controls (Seeking & Transport)
    function jumpRelative(delta) {
      const dur = state.playback.durationSeconds || 0;
      const cur = state.playback.positionSeconds || 0;
      let target = cur + delta;
      if (dur > 0) target = Math.max(0, Math.min(dur, target));
      else target = Math.max(0, target);
      state.playback.positionSeconds = target;
      lastPosSec = target;
      updateClock(target, dur);
      sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: target });
      sendHostCommand('seek', { time: target, positionSeconds: target });
      showToast(`${delta > 0 ? '+' : ''}${delta}s -> ${formatSeconds(target)}`);
    }

    if (els.btnClipCenter) {
      els.btnClipCenter.addEventListener('click', () => {
        centerActiveLyric(true);
      });
    }
    if (els.btnClipStart) {
      els.btnClipStart.addEventListener('click', () => {
        state.playback.positionSeconds = 0;
        lastPosSec = 0;
        updateClock(0, state.playback.durationSeconds);
        sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: 0 });
        sendHostCommand('seek', { time: 0, positionSeconds: 0 });
        showToast('00:00');
      });
    }
    if (els.btnClipEnd) {
      els.btnClipEnd.addEventListener('click', () => {
        const dur = state.playback.durationSeconds || 0;
        const target = Math.max(0, dur - 5);
        state.playback.positionSeconds = target;
        updateClock(target, dur);
        sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: target });
        sendHostCommand('seek', { time: target, positionSeconds: target });
        showToast(formatSeconds(target));
      });
    }
    if (els.btnClipRw5) els.btnClipRw5.addEventListener('click', () => jumpRelative(-5));
    if (els.btnClipRw15) els.btnClipRw15.addEventListener('click', () => jumpRelative(-15));
    if (els.btnClipFf5) els.btnClipFf5.addEventListener('click', () => jumpRelative(5));
    if (els.btnClipFf15) els.btnClipFf15.addEventListener('click', () => jumpRelative(15));

    // 7. Effects Panel Controls (VST3 Rack & Plugin Manager)
    if (els.btnFxVstRack) {
      els.btnFxVstRack.addEventListener('click', () => {
        sendModBridge('echo:powerpoint:toggleVstRack');
        try { window.parent.postMessage({ type: 'echo:vst3:toggleRack' }, '*'); } catch (e) {}
        showToast(t('toastVstRack') || '已唤起 VST3 效果机架');
      });
    }
    if (els.btnFxVstMgr) {
      els.btnFxVstMgr.addEventListener('click', () => {
        sendModBridge('echo:powerpoint:openVstManager');
        try { window.parent.postMessage({ type: 'echo:vst3:openPluginManager' }, '*'); } catch (e) {}
        showToast(t('toastVstMgr') || '正在打开 VST3 插件管理器...');
      });
    }
    if (els.btnFxFreeze) {
      els.btnFxFreeze.addEventListener('click', () => {
        state.audio.frozen = !state.audio.frozen;
        els.btnFxFreeze.classList.toggle('active', state.audio.frozen);
        showToast(state.audio.frozen ? '已冻结实时电平' : '已恢复实时电平');
      });
    }
    if (els.btnFxResetSpec) {
      els.btnFxResetSpec.addEventListener('click', () => {
        updateAudioLevels(-60, -60);
        showToast('已清空历史电平');
      });
    }

    // 8. Zoom Panel & Controls
    if (els.btnZoomFitRibbon) els.btnZoomFitRibbon.addEventListener('click', () => setZoom(100));
    if (els.btnZoomInRibbon) els.btnZoomInRibbon.addEventListener('click', () => setZoom(state.settings.zoomPercent + 10));
    if (els.btnZoomOutRibbon) els.btnZoomOutRibbon.addEventListener('click', () => setZoom(state.settings.zoomPercent - 10));

    if (els.btnViewToggleQueue) {
      els.btnViewToggleQueue.addEventListener('click', () => {
        if (els.pptThumbnailsPanel) {
          els.pptThumbnailsPanel.classList.toggle('collapsed');
          const isCollapsed = els.pptThumbnailsPanel.classList.contains('collapsed');
          els.btnViewToggleQueue.classList.toggle('active', !isCollapsed);
          savePresetSettings();
        }
      });
    }
    if (els.btnViewToggleNotes) {
      els.btnViewToggleNotes.addEventListener('click', () => {
        if (els.pptNotesBar) {
          els.pptNotesBar.classList.toggle('collapsed');
          const isCollapsed = els.pptNotesBar.classList.contains('collapsed');
          els.btnViewToggleNotes.classList.toggle('active', !isCollapsed);
          if (els.btnSbNotes) {
            els.btnSbNotes.classList.toggle('active', !isCollapsed);
          }
          savePresetSettings();
        }
      });
    }
    if (els.btnSbNotes) {
      els.btnSbNotes.addEventListener('click', () => {
        if (els.pptNotesBar) {
          els.pptNotesBar.classList.toggle('collapsed');
          const isCollapsed = els.pptNotesBar.classList.contains('collapsed');
          els.btnSbNotes.classList.toggle('active', !isCollapsed);
          if (els.btnViewToggleNotes) {
            els.btnViewToggleNotes.classList.toggle('active', !isCollapsed);
          }
          savePresetSettings();
        }
      });
    }
    if (els.chkAutosave) {
      els.chkAutosave.addEventListener('change', () => {
        savePresetSettings();
      });
    }

    // Statusbar zoom slider and buttons
    if (els.rngZoom) {
      els.rngZoom.addEventListener('input', (e) => setZoom(parseInt(e.target.value, 10)));
    }
    if (els.btnZoomIn) {
      els.btnZoomIn.addEventListener('click', () => setZoom(state.settings.zoomPercent + 10));
    }
    if (els.btnZoomOut) {
      els.btnZoomOut.addEventListener('click', () => setZoom(state.settings.zoomPercent - 10));
    }
    if (els.btnFitWindow) {
      els.btnFitWindow.addEventListener('click', () => setZoom(100));
    }

    // 9. Transport Controls (Ribbon + Bottom Player Bar)
    const handlePlayPause = () => {
      sendHostCommand('playPause');
      sendModBridge('echo:powerpoint:command', { action: 'playPause' });
    };
    if (els.btnTPlay) els.btnTPlay.addEventListener('click', handlePlayPause);
    if (els.btnTransPlaypause) els.btnTransPlaypause.addEventListener('click', handlePlayPause);
    if (els.vinylRecordDisc) els.vinylRecordDisc.addEventListener('click', handlePlayPause);
    if (els.turntableComponent) els.turntableComponent.addEventListener('click', (e) => {
      // Avoid firing twice if clicked directly on vinyl disc
      if (e.target.closest('.vinyl-record-disc')) return;
      handlePlayPause();
    });

    let lastNavTime = 0;
    const handlePrev = () => {
      const now = Date.now();
      if (now - lastNavTime < 220) return;
      lastNavTime = now;
      sendModBridge('echo:powerpoint:command', { action: 'previous' });
      sendHostCommand('previous');
    };
    if (els.btnTPrev) els.btnTPrev.addEventListener('click', handlePrev);
    if (els.btnTransPrev) els.btnTransPrev.addEventListener('click', handlePrev);

    const handleNext = () => {
      const now = Date.now();
      if (now - lastNavTime < 220) return;
      lastNavTime = now;
      sendModBridge('echo:powerpoint:command', { action: 'next' });
      sendHostCommand('next');
    };
    if (els.btnTNext) els.btnTNext.addEventListener('click', handleNext);
    if (els.btnTransNext) els.btnTransNext.addEventListener('click', handleNext);

    const handleCycleRepeat = () => {
      sendHostCommand('cycleRepeat');
      sendModBridge('echo:powerpoint:command', { action: 'cycleRepeat' });
    };
    if (els.btnTRepeat) els.btnTRepeat.addEventListener('click', handleCycleRepeat);
    if (els.btnTransCycleRepeat) els.btnTransCycleRepeat.addEventListener('click', handleCycleRepeat);

    const handleToggleShuffle = () => {
      sendHostCommand('toggleShuffle');
      sendModBridge('echo:powerpoint:command', { action: 'toggleShuffle' });
    };
    if (els.btnTShuffle) els.btnTShuffle.addEventListener('click', handleToggleShuffle);
    if (els.btnTransToggleShuffle) els.btnTransToggleShuffle.addEventListener('click', handleToggleShuffle);

    // Bottom Seek Slider
    if (els.rngProgress) {
      els.rngProgress.addEventListener('mousedown', () => { isDraggingProgress = true; });
      els.rngProgress.addEventListener('input', (e) => {
        isDraggingProgress = true;
        const val = parseInt(e.target.value, 10);
        const dur = state.playback.durationSeconds || 0;
        const cur = (val / 1000) * dur;
        if (els.timeCurrent) els.timeCurrent.textContent = formatSeconds(cur);
      });
      els.rngProgress.addEventListener('change', (e) => {
        isDraggingProgress = false;
        const val = parseInt(e.target.value, 10);
        const dur = state.playback.durationSeconds || 0;
        const cur = (val / 1000) * dur;
        sendHostCommand('seek', { positionSeconds: cur });
        sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: cur });
      });
    }

    // Volume
    if (els.rngVolume) {
      els.rngVolume.addEventListener('input', (e) => {
        const vol = parseInt(e.target.value, 10);
        if (els.lblVolume) els.lblVolume.textContent = `${vol}%`;
        sendHostCommand('setVolume', { volume: vol / 100 });
      });
    }
    const handleMute = () => {
      const isMuted = parseInt(els.rngVolume.value, 10) === 0;
      const nextVal = isMuted ? 100 : 0;
      els.rngVolume.value = nextVal;
      if (els.lblVolume) els.lblVolume.textContent = `${nextVal}%`;
      sendHostCommand('setVolume', { volume: nextVal / 100 });
    };
    if (els.btnMute) els.btnMute.addEventListener('click', handleMute);
    if (els.btnTransMuteToggle) els.btnTransMuteToggle.addEventListener('click', handleMute);
    if (els.btnTransVol100) {
      els.btnTransVol100.addEventListener('click', () => {
        if (els.rngVolume) els.rngVolume.value = 100;
        if (els.lblVolume) els.lblVolume.textContent = '100%';
        sendHostCommand('setVolume', { volume: 1.0 });
      });
    }

    // 10. Window Panel & Modal Options
    if (els.btnWinModalOptions) els.btnWinModalOptions.addEventListener('click', () => openModal('options'));
    if (els.btnHelpAbout) els.btnHelpAbout.addEventListener('click', () => openModal('about'));
    if (els.btnHelpShortcuts) {
      els.btnHelpShortcuts.addEventListener('click', () => {
        showToast('快捷键: 空格(播放/暂停), F5(放映), Alt+Q(搜索), Ctrl+Left/Right(切歌)');
      });
    }
    if (els.btnHelpReload) {
      els.btnHelpReload.addEventListener('click', () => {
        location.reload();
      });
    }

    // Modal navigation
    document.querySelectorAll('.modal-nav-items .nav-item').forEach(item => {
      item.addEventListener('click', () => {
        switchModalTab(item.dataset.tab || 'info');
      });
    });
    if (els.btnModalBack) {
      els.btnModalBack.addEventListener('click', closeModal);
    }
    document.querySelectorAll('.btn-modal-return, #btn-modal-close').forEach(btn => {
      btn.addEventListener('click', closeModal);
    });
    if (els.modalBackdrop) {
      els.modalBackdrop.addEventListener('click', (e) => {
        if (e.target === els.modalBackdrop) closeModal();
      });
    }

    // Modal Options Controls
    const optToneRadios = document.querySelectorAll('input[name="opt-theme-tone"]');
    optToneRadios.forEach(r => {
      r.addEventListener('change', (e) => {
        if (e.target.checked) updateTheme(e.target.value);
      });
    });
    const optOverlayRange = $('opt-bg-overlay-range');
    if (optOverlayRange) {
      optOverlayRange.addEventListener('input', (e) => {
        const val = parseInt(e.target.value, 10);
        state.settings.bgOverlayOpacity = val;
        const optValSpan = $('opt-bg-overlay-val');
        if (optValSpan) optValSpan.textContent = `${val}% (标准构想)`;
        if (els.rngBgOverlay) els.rngBgOverlay.value = val;
        if (els.lblOverlayVal) els.lblOverlayVal.textContent = `${val}%`;
        const op = val / 100;
        const color = state.settings.tone === 'dark' ? `rgba(0, 0, 0, ${op})` : `rgba(255, 255, 255, ${op})`;
        document.documentElement.style.setProperty('--slide-bg-overlay', color);
      });
    }
    const optSpeed = $('opt-vinyl-speed-select');
    if (optSpeed) {
      optSpeed.addEventListener('change', (e) => {
        const val = e.target.value;
        state.settings.vinylSpeed = val;
        if (els.selVinylSpeed) els.selVinylSpeed.value = val;
        if (val === 'slow') {
          document.documentElement.style.setProperty('--vinyl-speed', '24s');
        } else if (val === 'fast') {
          document.documentElement.style.setProperty('--vinyl-speed', '10s');
        } else if (val === 'stop') {
          if (els.vinylRecordDisc) els.vinylRecordDisc.classList.add('paused');
        } else {
          document.documentElement.style.setProperty('--vinyl-speed', '16s');
        }
      });
    }
    const optTrans = $('opt-chk-translation');
    if (optTrans) {
      optTrans.addEventListener('change', (e) => {
        state.settings.showTranslation = e.target.checked;
        if (els.btnToggleTranslation) els.btnToggleTranslation.classList.toggle('active', state.settings.showTranslation);
        renderLyricsList();
      });
    }

    // Status Bar Language Switcher
    if (els.sbLangPill) {
      els.sbLangPill.addEventListener('click', (e) => {
        e.stopPropagation();
        if (els.sbLangDropdown) {
          els.sbLangDropdown.hidden = !els.sbLangDropdown.hidden;
        }
      });
    }
    if (els.sbLangDropdown) {
      els.sbLangDropdown.querySelectorAll('.sb-lang-item').forEach(it => {
        it.addEventListener('click', (e) => {
          e.stopPropagation();
          const lang = it.dataset.lang;
          setLanguage(lang);
          els.sbLangDropdown.hidden = true;
          showToast(`Language: ${LANG_NAMES[lang] || lang}`);
        });
      });
    }
    document.addEventListener('click', (e) => {
      if (els.sbLangDropdown && !els.sbLangPill?.contains(e.target)) {
        els.sbLangDropdown.hidden = true;
      }
    });

    // Playlists Filter & Refresh
    if (els.iptPlaylistsSearch) {
      els.iptPlaylistsSearch.addEventListener('input', () => {
        renderPlaylistsTree();
      });
    }
    if (els.btnRefreshPlaylists) {
      els.btnRefreshPlaylists.addEventListener('click', () => {
        state.playlistCache.clear();
        state.albumCache.clear();
        fetchPlaylistsAndLibrary(true);
        showToast(t('btnRefresh') || '已刷新曲库与歌单');
      });
    }

    // 11. Slide Layout Selection (PowerPoint Classic, NetEase, Apple Music)
    if (els.btnLayoutPptVinyl) els.btnLayoutPptVinyl.addEventListener('click', () => setSlideLayout('ppt-vinyl'));
    if (els.btnLayoutNetease) els.btnLayoutNetease.addEventListener('click', () => setSlideLayout('netease'));
    if (els.btnLayoutAppleMusic) els.btnLayoutAppleMusic.addEventListener('click', () => setSlideLayout('applemusic'));

    // Apple Music Controls
    if (els.amBtnPlay) els.amBtnPlay.addEventListener('click', handlePlayPause);
    if (els.amBtnPrev) els.amBtnPrev.addEventListener('click', handlePrev);
    if (els.amBtnNext) els.amBtnNext.addEventListener('click', handleNext);
    if (els.amBtnShuffle) els.amBtnShuffle.addEventListener('click', handleToggleShuffle);
    if (els.amBtnRepeat) els.amBtnRepeat.addEventListener('click', handleCycleRepeat);
    if (els.amCoverImg) els.amCoverImg.parentElement?.addEventListener('click', handlePlayPause);
    if (els.neCoverImg) els.neCoverImg.parentElement?.addEventListener('click', handlePlayPause);

    if (els.amProgressSlider) {
      els.amProgressSlider.addEventListener('mousedown', () => { isDraggingProgress = true; });
      els.amProgressSlider.addEventListener('input', (e) => {
        isDraggingProgress = true;
        const val = parseInt(e.target.value, 10);
        const dur = state.playback.durationSeconds || 0;
        const cur = (val / 1000) * dur;
        if (els.amTimeCur) els.amTimeCur.textContent = formatSeconds(cur);
        if (els.amTimeRem) els.amTimeRem.textContent = `-${formatSeconds(Math.max(0, dur - cur))}`;
      });
      els.amProgressSlider.addEventListener('change', (e) => {
        isDraggingProgress = false;
        const val = parseInt(e.target.value, 10);
        const dur = state.playback.durationSeconds || 0;
        const cur = (val / 1000) * dur;
        sendHostCommand('seek', { positionSeconds: cur });
        sendModBridge('echo:powerpoint:command', { action: 'seek', positionSeconds: cur });
      });
    }

    if (els.amVolumeSlider) {
      els.amVolumeSlider.addEventListener('input', (e) => {
        const vol = parseInt(e.target.value, 10);
        sendHostCommand('setVolume', { volume: vol / 100 });
        if (els.rngVolume) els.rngVolume.value = vol;
        if (els.lblVolume) els.lblVolume.textContent = `${vol}%`;
      });
    }

    // Design Ribbon Format Controls
    if (els.designRngOverlay) {
      els.designRngOverlay.addEventListener('input', (e) => {
        const val = parseInt(e.target.value, 10);
        state.settings.bgOverlayOpacity = val;
        if (els.designLblOverlay) els.designLblOverlay.textContent = `${val}%`;
        const tone = state.settings.tone;
        const color = tone === 'dark' ? `rgba(0, 0, 0, ${val / 100})` : `rgba(255, 255, 255, ${val / 100})`;
        document.documentElement.style.setProperty('--slide-bg-overlay', color);
      });
    }
    if (els.designRngBlur) {
      els.designRngBlur.addEventListener('input', (e) => {
        applyBgBlur(e.target.value, true);
      });
    }
    if (els.btnDesignThemeToggle) {
      els.btnDesignThemeToggle.addEventListener('click', () => {
        const next = state.settings.tone === 'light' ? 'dark' : 'light';
        updateTheme(next);
        showToast(`已切换至 Office ${next === 'dark' ? '暗黑' : '经典明亮'}主题`);
      });
    }
    if (els.btnDesignResetBg) {
      els.btnDesignResetBg.addEventListener('click', () => {
        if (els.designRngOverlay) els.designRngOverlay.value = 25;
        if (els.designLblOverlay) els.designLblOverlay.textContent = '25%';
        if (els.designRngBlur) els.designRngBlur.value = 16;
        if (els.designLblBlur) els.designLblBlur.textContent = '16px';
        document.documentElement.style.setProperty('--slide-blur', '16px');
        document.documentElement.style.setProperty('--slide-bg-overlay', 'rgba(255, 255, 255, 0.25)');
        showToast('已重置幻灯片背景格式');
      });
    }

    // Global Keybindings
    window.addEventListener('keydown', (e) => {
      // Alt+Q or Ctrl+F: Search
      if ((e.altKey && (e.key === 'q' || e.key === 'Q')) || (e.ctrlKey && (e.key === 'f' || e.key === 'F'))) {
        e.preventDefault();
        if (els.searchInput) {
          els.searchInput.focus();
          els.searchInput.select();
        }
        return;
      }

      // If typing in input, ignore playback keys
      if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT') {
        return;
      }

      if (e.code === 'Escape') {
        if (document.body.classList.contains('slideshow-mode')) {
          e.preventDefault();
          toggleSlideshow(false);
          return;
        }
      } else if (e.code === 'Space') {
        e.preventDefault();
        handlePlayPause();
      } else if (e.code === 'F5') {
        e.preventDefault();
        toggleSlideshow();
      } else if (e.code === 'KeyP') {
        e.preventDefault();
        if (els.btnEditResetPeak) els.btnEditResetPeak.click();
      } else if (e.code === 'ArrowLeft' && e.ctrlKey) {
        e.preventDefault();
        handlePrev();
      } else if (e.code === 'ArrowRight' && e.ctrlKey) {
        e.preventDefault();
        handleNext();
      } else if (e.code === 'ArrowLeft') {
        e.preventDefault();
        if (els.btnClipRw5) els.btnClipRw5.click();
      } else if (e.code === 'ArrowRight') {
        e.preventDefault();
        if (els.btnClipFf5) els.btnClipFf5.click();
      }
    });
  }

  // --- Message Protocol Dispatcher ---
  function initMessageDispatcher() {
    window.addEventListener('message', (e) => {
      const msg = e.data;
      if (!msg || typeof msg !== 'object') return;

      switch (msg.type) {
        case 'echo:workshop-ui:result': {
          const reqId = msg.requestId;
          if (reqId && pendingHostRequests.has(reqId)) {
            const pending = pendingHostRequests.get(reqId);
            pendingHostRequests.delete(reqId);
            clearTimeout(pending.timer);
            pending.resolve(msg);
          }
          break;
        }

        case 'echo:workshop-ui:init':
          if (!isPresetLoaded && msg.appearance?.tone && !state.settings.tone) {
            updateTheme(msg.appearance.tone, false);
          }
          break;

        case 'echo:workshop-ui:state':
          if (msg.playback) updatePlaybackState(msg.playback);
          if (msg.queue && Array.isArray(msg.queue.items) && msg.queue.items.length > 0) {
            if (msg.queue.currentQueueId) state.queue.currentQueueId = msg.queue.currentQueueId;
            updateQueueList(msg.queue.items, false);
          } else if (!state.queue.items || state.queue.items.length <= 1) {
            requestFullQueue();
          }
          if (msg.currentTrack) updateTrackMetadata(msg.currentTrack);
          break;

        case 'echo:workshop-ui:lyrics':
          setLyricsData(msg.lyrics);
          break;

        case 'echo:workshop-ui:clock':
          updateClock(msg.positionSeconds, msg.durationSeconds);
          break;

        case 'echo:workshop-ui:audio':
          if (msg.levels) {
            updateAudioLevels(msg.levels.peakDb || -60, msg.levels.rmsDb || -60);
          }
          break;

        case 'echo:workshop-ui:appearance':
          if (msg.appearance?.tone) updateTheme(msg.appearance.tone, false);
          break;

        case 'echo:workshop-ui:ping':
          try {
            window.parent.postMessage({ type: 'echo:workshop-ui:pong', protocolVersion: 1 }, '*');
          } catch {}
          break;

        // Mod Bridge Messages (strictly scoped to echo:powerpoint:* to prevent cross-mod cover/status pollution)
        case 'echo:powerpoint:coverData':
          if (msg.coverDataUrl) {
            applyCoverArt(msg.coverDataUrl, msg.trackId || null);
            if (msg.trackId) {
              queueCoverCache.set(msg.trackId, msg.coverDataUrl);
              updateThumbnailCardCover(msg.trackId, null, null, msg.coverDataUrl);
            }
          }
          break;

        case 'echo:powerpoint:queueCoverItem':
          if (msg.coverDataUrl) {
            if (msg.trackId) queueCoverCache.set(msg.trackId, msg.coverDataUrl);
            if (msg.queueId) queueCoverCache.set(msg.queueId, msg.coverDataUrl);
            if (msg.rawUrl) queueCoverCache.set(msg.rawUrl, msg.coverDataUrl);
            updateThumbnailCardCover(msg.trackId, msg.queueId, msg.rawUrl, msg.coverDataUrl);
          }
          break;

        case 'echo:powerpoint:lyricsData':
          if (msg.lyrics) setLyricsData(msg.lyrics);
          break;

        case 'echo:powerpoint:queueData':
          if (Array.isArray(msg.items) && msg.items.length > 0) {
            updateQueueList(msg.items, false);
          }
          break;

        case 'echo:powerpoint:audioStatus':
          if (msg.status) updateAudioProperties(msg.status);
          break;

        case 'echo:powerpoint:systemFonts':
          if (Array.isArray(msg.fonts)) populateFontSelector(msg.fonts);
          break;

        case 'echo:powerpoint:fullLibrary':
          if (Array.isArray(msg.tracks)) {
            state.libraryTracks = msg.tracks;
            if ((!state.albums || state.albums.length === 0) && msg.tracks.length > 0) {
              state.albums = synthesizeAlbumsFromTracks(msg.tracks);
              if (state.playlistsLoaded) renderPlaylistsTree();
            }
          }
          break;

        case 'echo:powerpoint:playlists':
          if (Array.isArray(msg.playlists) && msg.playlists.length > 0) state.playlists = msg.playlists;
          if (Array.isArray(msg.folders) && msg.folders.length > 0) state.folders = msg.folders;
          if (Array.isArray(msg.albums) && msg.albums.length > 0) state.albums = msg.albums;
          if ((!state.albums || state.albums.length === 0) && (state.libraryTracks?.length > 0 || state.queue.items?.length > 0)) {
            state.albums = synthesizeAlbumsFromTracks(state.libraryTracks?.length > 0 ? state.libraryTracks : state.queue.items);
          }
          state.playlistsLoaded = true;
          renderPlaylistsTree();
          break;

        case 'echo:powerpoint:playlistItems':
          state.playlistCache.set(msg.playlistId, msg.items || []);
          if (els.modalPlaylistsTree) {
            const node = els.modalPlaylistsTree.querySelector(`.tree-node[data-playlist-id="${msg.playlistId}"]`);
            const children = node?.querySelector('.tree-node-children');
            if (children) renderPlaylistChildren(children, msg.items || []);
          }
          break;

        case 'echo:powerpoint:albumItems':
          state.albumCache.set(msg.albumId, msg.items || []);
          if (els.modalPlaylistsTree) {
            const node = els.modalPlaylistsTree.querySelector(`.tree-node[data-album-id="${msg.albumId}"]`);
            const children = node?.querySelector('.tree-node-children');
            if (children) renderPlaylistChildren(children, msg.items || []);
          }
          break;

        case 'echo:powerpoint:searchResults':
          if (Array.isArray(msg.tracks) && els.searchInput && els.searchInput.value.trim().toLowerCase() === (msg.query || '').trim().toLowerCase()) {
            mergeGlobalSearchResults(msg.tracks, msg.query);
          }
          break;

        case 'echo:powerpoint:importTracksResult':
          if (Array.isArray(msg.tracks) && msg.tracks.length > 0) {
            if (msg.playlistId) state.playlistCache.set(msg.playlistId, msg.tracks);
            if (msg.albumId) state.albumCache.set(msg.albumId, msg.tracks);
            if (msg.mode === 'append') {
              msg.tracks.forEach(tr => {
                const trId = tr.id || tr.track?.id;
                if (trId) sendHostCommand('queue:enqueueTrack', { trackId: trId });
              });
            }
          }
          break;

        case 'echo:powerpoint:setLayout':
          if (msg.layout) setSlideLayout(msg.layout);
          break;

        case 'echo:powerpoint:setRibbonTab':
          if (msg.tab) {
            const tabEl = document.getElementById(msg.tab);
            if (tabEl) tabEl.click();
          }
          break;

        case 'echo:powerpoint:presetData':
          isPresetLoaded = true;
          if (msg.preset) {
            loadAndApplyPresetSettings(msg.preset);
          }
          if (msg.fontSettings) {
            loadAndApplyLyricsFontSettings(msg.fontSettings);
          }
          break;
      }
    });

    // Notify Host UI Runtime
    try {
      window.parent.postMessage({ type: 'echo:workshop-ui:ready', protocolVersion: 1 }, '*');
    } catch {}

    // Request initial data from both Mod Bridge and Native Workshop UI Runtime
    setTimeout(() => {
      sendModBridge('echo:powerpoint:getPreset');
      sendModBridge('echo:powerpoint:getAudioStatus');
      requestFullQueue(true);
      sendModBridge('echo:powerpoint:getCover');
      sendModBridge('echo:powerpoint:getLyrics');
      sendModBridge('echo:powerpoint:getFullLibrary');
      sendModBridge('echo:powerpoint:getSystemFonts');
      fetchPlaylistsAndLibrary(true);
    }, 200);
  }

  // --- Local & System Font Access ---
  function detectInstalledSystemFonts() {
    const candidateFonts = [
      // Windows 基础中文字体
      'Microsoft YaHei', 'Microsoft YaHei UI', 'Microsoft YaHei Light',
      'SimSun', 'NSimSun', 'SimHei', 'KaiTi', 'FangSong', 'DengXian', 'DengXian Light', 'YouYuan',
      // 华文经典字体
      'STXihei', 'STKaiti', 'STSong', 'STFangsong', 'STZhongsong', 'STHupo', 'STXinwei', 'STLiti', 'STXingkai', 'STCaiyun',
      // 方正与汉仪
      'FZShuSong-Z01', 'FZKai-Z03', 'FZHei-B01', 'FZFangSong-Z02',
      // Windows / Office 常用西文字体
      'Segoe UI', 'Segoe UI Variable', 'Segoe UI Variable Text', 'Segoe UI Variable Display', 'Segoe UI Semibold', 'Segoe UI Light',
      'Arial', 'Arial Black', 'Calibri', 'Calibri Light', 'Cambria', 'Cambria Math', 'Candara', 'Corbel', 'Constantia',
      'Aptos', 'Aptos Display',
      // 等宽与编程代码字体
      'Consolas', 'Courier New', 'Cascadia Code', 'Cascadia Mono', 'Fira Code', 'JetBrains Mono', 'Source Code Pro', 'Inconsolata', 'Lucida Console',
      // 衬线经典印刷字体
      'Times New Roman', 'Georgia', 'Garamond', 'Palatino Linotype', 'Book Antiqua', 'Baskerville',
      // 无衬线现代字体
      'Verdana', 'Tahoma', 'Trebuchet MS', 'Century Gothic', 'Franklin Gothic Medium', 'Impact', 'Comic Sans MS',
      // 开源及移动端高质量字体
      'Source Han Sans SC', 'Source Han Serif SC', 'Source Han Sans CN', 'Source Han Serif CN',
      'Noto Sans SC', 'Noto Serif SC', 'Noto Sans CJK SC', 'Noto Serif CJK SC',
      'PingFang SC', 'Hiragino Sans GB', 'HarmonyOS Sans SC', 'HarmonyOS Sans',
      'MiSans', 'Alibaba PuHuiTi', 'Alibaba PuHuiTi 2.0', 'OPPO Sans', 'LXGW WenKai', 'LXGW Neo XiHei'
    ];
    try {
      const testString = 'mmmmmmmmmmlliWWWWWWWW1234567890\u4e00\u4e8c\u4e09\u6c49\u5b57\u6d4b\u8bd5';
      const baseFonts = ['monospace', 'sans-serif', 'serif'];
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return candidateFonts.slice(0, 16);
      const baseWidths = {};
      baseFonts.forEach(baseFont => {
        ctx.font = `72px ${baseFont}`;
        baseWidths[baseFont] = ctx.measureText(testString).width;
      });

      const installed = [];
      candidateFonts.forEach(fontName => {
        const isPresent = baseFonts.some(baseFont => {
          ctx.font = `72px "${fontName}", ${baseFont}`;
          return ctx.measureText(testString).width !== baseWidths[baseFont];
        });
        if (isPresent) installed.push(fontName);
      });
      return installed;
    } catch {
      return candidateFonts.slice(0, 12);
    }
  }

  function initSystemFonts() {
    const detected = detectInstalledSystemFonts();
    if (detected && detected.length > 0) {
      populateFontSelector(detected);
    }
    sendModBridge('echo:powerpoint:getSystemFonts');
    if (typeof window.queryLocalFonts === 'function') {
      window.queryLocalFonts().then(fonts => {
        const set = new Set(detected);
        fonts.forEach(f => { if (f.family) set.add(f.family); });
        populateFontSelector(Array.from(set).sort());
      }).catch(() => {});
    }
  }

  function populateFontSelector(fonts) {
    if (!els.selLyricsFont || !fonts || fonts.length === 0) return;
    const currentVal = els.selLyricsFont.value;
    let savedVal = '';
    try {
      const raw = safeStorageGet(LYRICS_FONT_STORAGE_KEY);
      if (raw) {
        const s = JSON.parse(raw);
        if (s && s.fontFamily) savedVal = s.fontFamily;
      }
    } catch {}

    const curated = [
      { label: '微软雅黑 (Microsoft YaHei)', val: "'Microsoft YaHei', 'Segoe UI', sans-serif" },
      { label: 'Segoe UI (Office Modern)', val: "'Segoe UI', sans-serif" },
      { label: '黑体 (SimHei)', val: "'SimHei', sans-serif" },
      { label: '宋体 (SimSun)', val: "'SimSun', serif" },
      { label: '楷体 (KaiTi)', val: "'KaiTi', serif" },
      { label: '仿宋 (FangSong)', val: "'FangSong', serif" },
      { label: 'Consolas (等宽代码)', val: "'Consolas', monospace" },
      { label: 'Arial', val: "Arial, sans-serif" },
      { label: 'Georgia (优雅衬线)', val: "Georgia, serif" }
    ];

    els.selLyricsFont.innerHTML = '';
    const groupCurated = document.createElement('optgroup');
    groupCurated.label = t('fontGroupCurated') || '常用精选字体';
    curated.forEach(item => {
      const opt = document.createElement('option');
      opt.value = item.val;
      opt.textContent = item.label;
      groupCurated.appendChild(opt);
    });
    els.selLyricsFont.appendChild(groupCurated);

    const groupSystem = document.createElement('optgroup');
    groupSystem.label = `${t('fontGroupSystem') || '系统已安装字体'} (${fonts.length})`;
    fonts.forEach(name => {
      const opt = document.createElement('option');
      opt.value = `'${name}', sans-serif`;
      opt.textContent = name;
      groupSystem.appendChild(opt);
    });
    els.selLyricsFont.appendChild(groupSystem);

    const groupCustom = document.createElement('optgroup');
    groupCustom.dataset.group = 'custom';
    groupCustom.label = '个性化与自定义字体';
    if (savedVal && !curated.some(c => c.val === savedVal) && !fonts.some(f => `'${f}', sans-serif` === savedVal)) {
      const customOpt = document.createElement('option');
      customOpt.value = savedVal;
      customOpt.textContent = savedVal.replace(/['",]/g, '').trim();
      groupCustom.appendChild(customOpt);
    }
    const optAddCustom = document.createElement('option');
    optAddCustom.value = '__custom__';
    optAddCustom.textContent = '输入自定义系统字体...';
    groupCustom.appendChild(optAddCustom);
    els.selLyricsFont.appendChild(groupCustom);

    const targetVal = savedVal || currentVal;
    if (targetVal) {
      els.selLyricsFont.value = targetVal;
      document.documentElement.style.setProperty('--lyrics-font-family', targetVal);
    }
  }

  // --- Bootstrap ---
  function init() {
    initMessageDispatcher();
    try { setupListeners(); } catch (e) { console.error('setupListeners error:', e); }
    try { initSearch(); } catch (e) { console.error('initSearch error:', e); }
    try { initRibbonTabs(); } catch (e) { console.error('initRibbonTabs error:', e); }
    try { initSystemFonts(); } catch (e) { console.error('initSystemFonts error:', e); }

    // 1. Synchronous Cache Restore
    loadAndApplyPresetSettings();
    loadAndApplyLyricsFontSettings();

    // 2. Workshop Native Host Persistent Storage
    requestHostCommand('storage:get', { key: PRESET_SETTINGS_KEY }).then(res => {
      const val = res?.result ?? res?.data?.value ?? res?.value;
      if (val) {
        try {
          const p = typeof val === 'string' ? JSON.parse(val) : val;
          if (p && typeof p === 'object') {
            loadAndApplyPresetSettings(p);
            isPresetLoaded = true;
          }
        } catch {}
      }
    }).catch(() => {});

    requestHostCommand('storage:get', { key: LYRICS_FONT_STORAGE_KEY }).then(res => {
      const val = res?.result ?? res?.data?.value ?? res?.value;
      if (val) {
        try {
          const f = typeof val === 'string' ? JSON.parse(val) : val;
          if (f && typeof f === 'object') {
            loadAndApplyLyricsFontSettings(f);
          }
        } catch {}
      }
    }).catch(() => {});

    // 3. Mod Bridge Redundant Restore
    sendModBridge('echo:powerpoint:getPreset');
    setTimeout(() => { isPresetLoaded = true; }, 1200);

    // Default mock data when running standalone
    setTimeout(() => {
      if (!state.currentTrack) {
        updateTrackMetadata({
          title: '原神-漩涡、落星与冰山',
          artist: '陈致逸 / HOYO-MiX',
          album: '原神-风与牧歌之城'
        });
        updateAudioProperties({
          codec: 'FLAC',
          sampleRate: 44100,
          bitDepth: 24,
          bitrate: 1411,
          channels: 2,
          outputDevice: 'WASAPI Shared'
        });
        // Default 1 slide card in queue
        updateQueueList([
          {
            title: '原神-漩涡、落星与冰山',
            artist: '陈致逸 / HOYO-MiX',
            durationSeconds: 195
          }
        ]);
      }
    }, 500);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
