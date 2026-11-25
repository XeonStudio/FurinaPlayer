namespace FurinaPlayer.Core.Services;

public static partial class Loc
{
	static partial void RegisterControls()
	{
		// MiniPlayer
		Add("顺序播放", "順序播放", "Sequential", "Lecture séquentielle", "Последовательно", "Secuencial", "順次再生", "순차 재생");
		Add("单曲循环", "單曲循環", "Repeat One", "Répéter un titre", "Повтор трека", "Repetir una", "1曲リピート", "한 곡 반복");
		Add("列表循环", "列表循環", "Repeat All", "Répéter la liste", "Повтор списка", "Repetir todo", "全曲リピート", "전체 반복");
		Add("随机播放", "隨機播放", "Shuffle", "Aléatoire", "Случайно", "Aleatorio", "シャッフル", "셔플 재생");
		Add("播放模式", "播放模式", "Playback Mode", "Mode de lecture", "Режим воспроизведения", "Modo de reproducción", "再生モード", "재생 모드");

		// InsightProWorkbenchControl
		Add("重置计量", "重置計量", "Reset Meters", "Réinitialiser les compteurs", "Сбросить измерители", "Restablecer medidores", "メーターをリセット", "미터 초기화");
		Add("所有分析模块已隐藏", "所有分析模組已隱藏", "All analysis modules are hidden", "Tous les modules d'analyse sont masqués", "Все модули анализа скрыты", "Todos los módulos de análisis están ocultos", "すべての解析モジュールが非表示です", "모든 분석 모듈이 숨겨졌습니다");
		Add("恢复全部仪表", "恢復全部儀表", "Restore All Meters", "Restaurer tous les compteurs", "Восстановить все измерители", "Restaurar todos los medidores", "すべてのメーターを復元", "모든 미터 복원");

		// Vst3RackQuickWindow
		Add("FurinaPlayer — VST3 效果机架与插件管理", "FurinaPlayer — VST3 效果機架與外掛程式管理", "FurinaPlayer — VST3 Rack & Plugin Manager", "FurinaPlayer — Rack VST3 et gestionnaire de plugins", "FurinaPlayer — Стойка VST3 и менеджер плагинов", "FurinaPlayer — Rack VST3 y gestor de plugins", "FurinaPlayer — VST3 ラック & プラグイン管理", "FurinaPlayer — VST3 랙 및 플러그인 관리자");
		Add("VST3 效果机架 / 插件管理快捷窗口", "VST3 效果機架 / 外掛程式管理快捷視窗", "VST3 Rack / Plugin Manager Quick Window", "Fenêtre rapide Rack VST3 / Gestionnaire de plugins", "Быстрое окно стойки VST3 / менеджера плагинов", "Ventana rápida de Rack VST3 / Gestor de plugins", "VST3 ラック / プラグイン管理クイックウィンドウ", "VST3 랙 / 플러그인 관리자 빠른 창");
		Add("关闭窗口", "關閉視窗", "Close Window", "Fermer la fenêtre", "Закрыть окно", "Cerrar ventana", "ウィンドウを閉じる", "창 닫기");

		// Vst3EditorWindow
		Add("插件加载失败（未知错误）", "外掛程式載入失敗（未知錯誤）", "Failed to load plugin (unknown error)", "Échec du chargement du plugin (erreur inconnue)", "Не удалось загрузить плагин (неизвестная ошибка)", "Error al cargar el plugin (error desconocido)", "プラグインの読み込みに失敗しました（不明なエラー）", "플러그인 로드 실패(알 수 없는 오류)");
		Add("VST3 插件界面 - Furina Player", "VST3 外掛程式介面 - Furina Player", "VST3 Plugin Editor - Furina Player", "Interface du plugin VST3 - Furina Player", "Редактор плагина VST3 - Furina Player", "Editor de plugin VST3 - Furina Player", "VST3 プラグイン画面 - Furina Player", "VST3 플러그인 화면 - Furina Player");
		Add("创建宿主窗口失败", "建立宿主視窗失敗", "Failed to create host window", "Échec de la création de la fenêtre hôte", "Не удалось создать окно хоста", "Error al crear la ventana anfitriona", "ホストウィンドウの作成に失敗しました", "호스트 창 생성 실패");
		Add("打开插件界面异常：{0}", "開啟外掛程式介面異常：{0}", "Error opening plugin editor: {0}", "Erreur lors de l'ouverture du plugin : {0}", "Ошибка открытия окна плагина: {0}", "Error al abrir la interfaz del plugin: {0}", "プラグイン画面を開く際のエラー: {0}", "플러그인 화면 열기 오류: {0}");

		// ParametricEqWorkbenchControl — Presets
		Add("平坦 (Flat)", "平坦 (Flat)", "Flat", "Plat (Flat)", "Ровная (Flat)", "Plano (Flat)", "フラット (Flat)", "플랫 (Flat)");
		Add("耳机校正 - Moondrop / Parameter (哈曼目标)", "耳機校正 - Moondrop / Parameter (哈曼目標)", "Headphone EQ - Moondrop / Parameter (Harman Target)", "Correction casque - Moondrop / Parameter (Cible Harman)", "Коррекция наушников - Moondrop / Parameter (Harman)", "Corrección auriculares - Moondrop / Parameter (Harman)", "ヘッドホン補正 - Moondrop / Parameter (Harman Target)", "헤드폰 보정 - Moondrop / Parameter (하만 타겟)");
		Add("耳机校正 - Sennheiser HD600 (弥散场优化)", "耳機校正 - Sennheiser HD600 (擴散場最佳化)", "Headphone EQ - Sennheiser HD600 (Diffuse Field)", "Correction casque - Sennheiser HD600 (Champ diffus)", "Коррекция наушников - Sennheiser HD600 (Диффузное поле)", "Corrección auriculares - Sennheiser HD600 (Campo difuso)", "ヘッドホン補正 - Sennheiser HD600 (拡散音場最適化)", "헤드폰 보정 - Sennheiser HD600 (확산 음장 최적화)");
		Add("耳机校正 - Sony WH-1000XM5 (下潜饱满)", "耳機校正 - Sony WH-1000XM5 (深沉飽滿)", "Headphone EQ - Sony WH-1000XM5 (Deep Bass)", "Correction casque - Sony WH-1000XM5 (Basses profondes)", "Коррекция наушников - Sony WH-1000XM5 (Глубокий бас)", "Corrección auriculares - Sony WH-1000XM5 (Graves profundos)", "ヘッドホン補正 - Sony WH-1000XM5 (豊かな低音)", "헤드폰 보정 - Sony WH-1000XM5 (깊은 저음)");
		Add("哈曼目标曲线 Harman Target 2019", "哈曼目標曲線 Harman Target 2019", "Harman Target Curve 2019", "Courbe cible Harman 2019", "Целевая кривая Harman 2019", "Curva objetivo Harman 2019", "Harman ターゲットカーブ 2019", "하만 타겟 커브 2019");
		Add("低频重低音增强 (Sub-Bass Boost +6dB)", "低頻重低音增強 (Sub-Bass Boost +6dB)", "Sub-Bass Boost (+6dB)", "Amplification Sub-Bass (+6 dB)", "Усиление суббаса (+6 дБ)", "Realce de subgraves (+6 dB)", "サブベースブースト (+6dB)", "서브베이스 부스트 (+6dB)");
		Add("高频通透空气感 (High Air Brilliance)", "高頻通透空氣感 (High Air Brilliance)", "High Air Brilliance", "Brillance et air des aigus", "Воздушность высоких частот", "Brillo y aire en agudos", "高域の抜け・エアー感 (High Air Brilliance)", "고음역 개방감 (High Air Brilliance)");
		Add("暖声人声突出 (Warm Vocal Presence)", "暖聲人聲突出 (Warm Vocal Presence)", "Warm Vocal Presence", "Présence vocale chaleureuse", "Тёплое присутствие вокала", "Presencia vocal cálida", "温かみのあるボーカル強調 (Warm Vocal Presence)", "따뜻한 보컬 강조 (Warm Vocal Presence)");
		Add("开阔立体声场 (Open Soundstage)", "開闊立體聲場 (Open Soundstage)", "Open Soundstage", "Scène sonore ouverte", "Широкая стереосцена", "Escena sonora abierta", "ワイドサウンドステージ (Open Soundstage)", "넓은 사운드스테이지 (Open Soundstage)");

		// ParametricEqWorkbenchControl — Top Bar & Modes
		Add("✨ 永久写入当前音轨 (Apply DSP)", "✨ 永久寫入目前音軌 (Apply DSP)", "✨ Apply DSP to Track", "✨ Appliquer le DSP à la piste", "✨ Применить DSP к треку", "✨ Aplicar DSP a la pista", "✨ トラックに DSP を永続適用", "✨ 트랙에 DSP 영구 적용");
		Add("参数 EQ", "參數 EQ", "Parametric EQ", "Égaliseur paramétrique", "Параметрический EQ", "Ecualizador paramétrico", "パラメトリック EQ", "파라메트릭 EQ");
		Add("参数 EQ (Parametric)", "參數 EQ (Parametric)", "Parametric EQ", "Paramétrique (EQ)", "Параметрический EQ", "EQ Paramétrico", "パラメトリック EQ", "파라메트릭 EQ");
		Add("图形均衡 & Mix Assistant", "圖形均衡 & Mix Assistant", "Graphic EQ & Mix Assistant", "Égaliseur graphique & Mix Assistant", "Графический EQ и Mix Assistant", "EQ Gráfico y Mix Assistant", "グラフィック EQ & Mix Assistant", "그래픽 EQ & Mix Assistant");
		Add("频谱", "頻譜", "Spectrum", "Spectre", "Спектр", "Espectro", "スペクトラム", "스펙트럼");
		Add("撤销 (Undo)", "復原 (Undo)", "Undo", "Annuler (Undo)", "Отменить (Undo)", "Deshacer (Undo)", "元に戻す (Undo)", "실행 취소 (Undo)");
		Add("重做 (Redo)", "重做 (Redo)", "Redo", "Rétablir (Redo)", "Повторить (Redo)", "Rehacer (Redo)", "やり直し (Redo)", "다시 실행 (Redo)");
		Add("重置 EQ 为平坦", "重置 EQ 為平坦", "Reset EQ to Flat", "Réinitialiser l'EQ à plat", "Сбросить EQ в ноль", "Restablecer EQ a plano", "EQ をフラットにリセット", "EQ 플랫으로 초기화");
		Add("自动", "自動", "Auto", "Auto", "Авто", "Auto", "自動", "자동");

		// ParametricEqWorkbenchControl — Headroom & A/B Compare
		Add("● 待确认 余量 -0.0 dB", "● 待確認 餘量 -0.0 dB", "● Pending Headroom -0.0 dB", "● En attente Marge -0.0 dB", "● Ожидание Запас -0.0 дБ", "● Pendiente Margen -0.0 dB", "● 待機中 ヘッドルーム -0.0 dB", "● 대기 중 헤드룸 -0.0 dB");
		Add("处理前 0.0 dB", "處理前 0.0 dB", "Pre 0.0 dB", "Avant 0.0 dB", "До 0.0 дБ", "Antes 0.0 dB", "処理前 0.0 dB", "처리 전 0.0 dB");
		Add("处理后 0.0 dB", "處理後 0.0 dB", "Post 0.0 dB", "Après 0.0 dB", "После 0.0 дБ", "Después 0.0 dB", "処理後 0.0 dB", "처리 후 0.0 dB");
		Add("⇄ A/B 对比", "⇄ A/B 對比", "⇄ A/B Compare", "⇄ Comparer A/B", "⇄ Сравнение A/B", "⇄ Comparar A/B", "⇄ A/B 比較", "⇄ A/B 비교");
		Add("实时等响对比", "即時等響對比", "Loudness Match", "Égalisation de volume", "Выравнивание громкости", "Igualar sonoridad", "ラウドネスマッチ", "라우드니스 매칭");
		Add("● 安全余量 {0:+0.0;-0.0;0.0} dB", "● 安全餘量 {0:+0.0;-0.0;0.0} dB", "● Headroom {0:+0.0;-0.0;0.0} dB", "● Marge {0:+0.0;-0.0;0.0} dB", "● Запас {0:+0.0;-0.0;0.0} dB", "● Margen {0:+0.0;-0.0;0.0} dB", "● ヘッドルーム {0:+0.0;-0.0;0.0} dB", "● 안전 헤드룸 {0:+0.0;-0.0;0.0} dB");
		Add("▲ 峰值超限 {0:+0.0;-0.0;0.0} dB (建议降低前级)", "▲ 峰值超限 {0:+0.0;-0.0;0.0} dB (建議降低前級)", "▲ Peak Over {0:+0.0;-0.0;0.0} dB (Lower Preamp)", "▲ Crête dépassée {0:+0.0;-0.0;0.0} dB (Baisser le préampli)", "▲ Пик превышен {0:+0.0;-0.0;0.0} dB (Снизьте преамп)", "▲ Pico excedido {0:+0.0;-0.0;0.0} dB (Bajar preamplificador)", "▲ ピーク超過 {0:+0.0;-0.0;0.0} dB (プリアンプ低下推奨)", "▲ 피크 초과 {0:+0.0;-0.0;0.0} dB (프리앰프 낮춤 권장)");
		Add("合成峰值 {0:+0.0;-0.0;0.0} dB", "合成峰值 {0:+0.0;-0.0;0.0} dB", "Sum Peak {0:+0.0;-0.0;0.0} dB", "Crête cumulée {0:+0.0;-0.0;0.0} dB", "Суммарный пик {0:+0.0;-0.0;0.0} dB", "Pico combinado {0:+0.0;-0.0;0.0} dB", "合成ピーク {0:+0.0;-0.0;0.0} dB", "합성 피크 {0:+0.0;-0.0;0.0} dB");
		Add("前级 {0:+0.0;-0.0;0.0} dB", "前級 {0:+0.0;-0.0;0.0} dB", "Preamp {0:+0.0;-0.0;0.0} dB", "Préampli {0:+0.0;-0.0;0.0} dB", "Преамп {0:+0.0;-0.0;0.0} dB", "Preamplificador {0:+0.0;-0.0;0.0} dB", "プリアンプ {0:+0.0;-0.0;0.0} dB", "프리앰프 {0:+0.0;-0.0;0.0} dB");
		Add("当前对比: A (原始直通)", "目前對比: A (原始直通)", "Active: A (Bypass)", "Actif : A (Direct)", "Активно: A (Обход)", "Activo: A (Original)", "比較中: A (バイパス)", "비교 중: A (원본 바이패스)");
		Add("当前对比: B (EQ 处理后)", "目前對比: B (EQ 處理後)", "Active: B (EQ Processed)", "Actif : B (EQ actif)", "Активно: B (EQ)", "Activo: B (EQ aplicado)", "比較中: B (EQ 適用)", "비교 중: B (EQ 적용)");
		Add("⇄ A: 原始 (点击切回 B)", "⇄ A: 原始 (點擊切回 B)", "⇄ A: Bypass (Click for B)", "⇄ A : Direct (Cliquer pour B)", "⇄ A: Обход (Нажмите для B)", "⇄ A: Original (Clic para B)", "⇄ A: 原音 (クリックで B へ)", "⇄ A: 원본 (클릭하여 B로 전환)");

		// ParametricEqWorkbenchControl — Band Inspector
		Add("峰值 (Peaking)", "峰值 (Peaking)", "Peaking", "Crête (Peaking)", "Колокол (Peaking)", "Pico (Peaking)", "ピーキング (Peaking)", "피킹 (Peaking)");
		Add("低架 (Low Shelf)", "低架 (Low Shelf)", "Low Shelf", "Plateau bas (Low Shelf)", "Низкий шельф (Low Shelf)", "Estante bajo (Low Shelf)", "ローシェルフ (Low Shelf)", "로우 쉘프 (Low Shelf)");
		Add("高架 (High Shelf)", "高架 (High Shelf)", "High Shelf", "Plateau haut (High Shelf)", "Высокий шельф (High Shelf)", "Estante alto (High Shelf)", "ハイシェルフ (High Shelf)", "하이 쉘프 (High Shelf)");
		Add("低通 (Low Pass)", "低通 (Low Pass)", "Low Pass", "Passe-bas (Low Pass)", "ФНЧ (Low Pass)", "Paso bajo (Low Pass)", "ローパス (Low Pass)", "로우 패스 (Low Pass)");
		Add("高通 (High Pass)", "高通 (High Pass)", "High Pass", "Passe-haut (High Pass)", "ФВЧ (High Pass)", "Paso alto (High Pass)", "ハイパス (High Pass)", "하이 패스 (High Pass)");
		Add("带通 (Band Pass)", "帶通 (Band Pass)", "Band Pass", "Passe-bande (Band Pass)", "Полосовой (Band Pass)", "Paso banda (Band Pass)", "バンドパス (Band Pass)", "밴드 패스 (Band Pass)");
		Add("陷波 (Notch)", "陷波 (Notch)", "Notch", "Coupe-bande (Notch)", "Режекторный (Notch)", "Muesca (Notch)", "ノッチ (Notch)", "노치 (Notch)");
		Add("启用此段", "啟用此段", "Enable Band", "Activer la bande", "Включить полосу", "Activar banda", "バンドを有効化", "밴드 활성화");
		Add("重置此段", "重置此段", "Reset Band", "Réinitialiser la bande", "Сбросить полосу", "Restablecer banda", "バンドをリセット", "밴드 초기화");
		Add("增益归零（或移除新增频段）", "增益歸零（或移除新增頻段）", "Zero Gain (or Remove Added Band)", "Mettre à zéro (ou supprimer la bande)", "Обнулить (или удалить полосу)", "Poner a cero (o eliminar banda)", "ゲインを 0 に戻す（または追加バンドを削除）", "게인 0으로 초기화(또는 추가 밴드 삭제)");
		Add("频率 Frequency", "頻率 Frequency", "Frequency", "Fréquence", "Частота", "Frecuencia", "周波数 Frequency", "주파수 Frequency");
		Add("增益 Gain", "增益 Gain", "Gain", "Gain", "Усиление", "Ganancia", "ゲイン Gain", "게인 Gain");
		Add("品质因数 Q", "品質因數 Q", "Q Factor", "Facteur Q", "Добротность Q", "Factor Q", "Q 値", "Q 계수");
		Add("宽 (0.5)", "寬 (0.5)", "Wide (0.5)", "Large (0.5)", "Широкая (0.5)", "Ancho (0.5)", "広 (0.5)", "넓게 (0.5)");
		Add("标准 (1.4)", "標準 (1.4)", "Std (1.4)", "Std (1.4)", "Станд. (1.4)", "Estándar (1.4)", "標準 (1.4)", "표준 (1.4)");
		Add("窄 (3.0)", "窄 (3.0)", "Narrow (3.0)", "Étroit (3.0)", "Узкая (3.0)", "Estrecho (3.0)", "狭 (3.0)", "좁게 (3.0)");
		Add("吸附到标准频点", "吸附到標準頻點", "Snap to Standard Freqs", "Aligner sur fréquences std", "Привязка к станд. частотам", "Ajustar a frecuencias estándar", "標準周波数にスナップ", "표준 주파수에 스냅");
		Add("作用域:", "作用域:", "Scope:", "Portée :", "Область:", "Ámbito:", "適用範囲:", "적용 범위:");
		Add("立体声", "立體聲", "Stereo", "Stéréo", "Стерео", "Estéreo", "ステレオ", "스테레오");
		Add("左", "左", "Left", "Gauche", "Левый", "Izquierda", "左", "좌");
		Add("右", "右", "Right", "Droite", "Правый", "Derecha", "右", "우");
		Add("中 (M)", "中 (M)", "Mid (M)", "Centre (M)", "Центр (M)", "Centro (M)", "ミッド (M)", "미드 (M)");
		Add("侧 (S)", "側 (S)", "Side (S)", "Côté (S)", "Стороны (S)", "Lateral (S)", "サイド (S)", "사이드 (S)");
		Add("EQ 前级增益:", "EQ 前級增益:", "EQ Preamp:", "Préampli EQ :", "Преамп EQ:", "Preamplificador EQ:", "EQ プリアンプ:", "EQ 프리앰프:");
		Add("合成峰值 0.0 dB", "合成峰值 0.0 dB", "Sum Peak 0.0 dB", "Crête cumulée 0.0 dB", "Суммарный пик 0.0 дБ", "Pico combinado 0.0 dB", "合成ピーク 0.0 dB", "합성 피크 0.0 dB");
		Add("添加频段", "新增頻段", "Add Band", "Ajouter bande", "Добавить полосу", "Añadir banda", "バンド追加", "밴드 추가");

		// ParametricEqWorkbenchControl — Graphic EQ & Mix Assistant
		Add("10 段图形均衡推子 (Graphic EQ)", "10 段圖形均衡推桿 (Graphic EQ)", "10-Band Graphic EQ Faders", "Curseurs EQ graphique 10 bandes", "10-полосный графический EQ", "Ecualizador gráfico de 10 bandas", "10 バンドグラフィック EQ フェーダー", "10밴드 그래픽 EQ 페이더");
		Add("与上方参数 EQ 曲线双向实时同步", "與上方參數 EQ 曲線雙向即時同步", "Synced bidirectionally with Parametric EQ curve above", "Synchronisé en temps réel avec la courbe EQ ci-dessus", "Двусторонняя синхронизация с кривой EQ выше", "Sincronizado en tiempo real con la curva EQ superior", "上部のパラメトリック EQ カーブと双方向リアルタイム同期", "상단 파라메트릭 EQ 커브와 양방향 실시간 동기화");
		Add("推子归零", "推桿歸零", "Zero Faders", "Zéro curseurs", "Обнулить фейдеры", "Faders a cero", "フェーダーをリセット", "페이더 초기화");
		Add("🎛️ Mix Assistant 智能混音平衡助手", "🎛️ Mix Assistant 智慧混音平衡助手", "🎛️ Mix Assistant", "🎛️ Mix Assistant", "🎛️ Mix Assistant", "🎛️ Mix Assistant", "🎛️ Mix Assistant スマートミキシング", "🎛️ Mix Assistant 스마트 믹싱");
		Add("源自 Neutron Pro Tonal Balance 感知学习 · De-Masking 防掩蔽 · Sculptor 音色塑形", "源自 Neutron Pro Tonal Balance 感知學習 · De-Masking 防遮蔽 · Sculptor 音色塑形", "Tonal Balance perceptual learning · De-Masking · Sculptor tone shaping", "Apprentissage Tonal Balance · De-Masking · Sculptor", "Перцептивное обучение Tonal Balance · De-Masking · Sculptor", "Aprendizaje Tonal Balance · De-Masking · Sculptor", "Tonal Balance 知覚学習 · De-Masking · Sculptor トーンシェイピング", "Tonal Balance 지각 학습 · De-Masking · Sculptor 톤 쉐이핑");
		Add("点击“学习监听”或“分析音轨”诊断频谱平衡", "點擊「學習監聽」或「分析音軌」診斷頻譜平衡", "Click 'Learn' or 'Analyze' to diagnose tonal balance", "Cliquez sur « Écouter » ou « Analyser » pour diagnostiquer l'équilibre", "Нажмите «Обучение» или «Анализ» для диагностики баланса", "Pulsa «Aprender» o «Analizar» para diagnosticar el balance", "「学習」または「トラック解析」でスペクトルバランスを診断", "‘학습’ 또는 ‘트랙 분석’을 클릭하여 스펙트럼 밸런스 진단");
		Add("点击“学习监听”或“分析音轨”以诊断频响并生成平衡曲线", "點擊「學習監聽」或「分析音軌」以診斷頻響並產生平衡曲線", "Click 'Learn' or 'Analyze' to diagnose frequency response and generate curve", "Cliquez sur « Écouter » ou « Analyser » pour générer la courbe d'équilibre", "Нажмите «Обучение» или «Анализ» для построения кривой баланса", "Pulsa «Aprender» o «Analizar» para generar la curva de equilibrio", "「学習」または「トラック解析」で周波数特性を診断しバランスカーブを生成", "‘학습’ 또는 ‘트랙 분석’을 클릭하여 주파수 응답 진단 및 밸런스 커브 생성");
		Add("正在监听音频流，请播放音乐...", "正在監聽音訊串流，請播放音樂...", "Listening to audio stream, please play music...", "Écoute du flux audio, veuillez lancer la lecture...", "Прослушивание аудиопотока, включите музыку...", "Escuchando el flujo de audio, reproduce música...", "オーディオストリームを監視中、音楽を再生してください...", "오디오 스트림 감지 중, 음악을 재생하세요...");
		Add("正在学习音频频谱平衡... ({0} 帧)", "正在學習音訊頻譜平衡... ({0} 幀)", "Learning tonal balance... ({0} frames)", "Apprentissage de l'équilibre tonal... ({0} trames)", "Изучение тонального баланса... ({0} кадров)", "Aprendiendo balance tonal... ({0} tramas)", "オーディオスペクトルバランスを学習中... ({0} フレーム)", "오디오 스펙트럼 밸런스 학습 중... ({0} 프레임)");
		Add("正在学习频谱 ({0:0}%)...", "正在學習頻譜 ({0:0}%)...", "Learning spectrum ({0:0}%)...", "Apprentissage du spectre ({0:0}%)...", "Изучение спектра ({0:0}%)...", "Aprendiendo espectro ({0:0}%)...", "スペクトル学習中 ({0:0}%)...", "스펙트럼 학습 중 ({0:0}%)...");
		Add("⏹ 完成学习", "⏹ 完成學習", "⏹ Finish Learn", "⏹ Terminer", "⏹ Завершить", "⏹ Finalizar", "⏹ 学習完了", "⏹ 학습 완료");
		Add("🎧 学习监听 (Learn)", "🎧 學習監聽 (Learn)", "🎧 Learn", "🎧 Écouter (Learn)", "🎧 Обучение (Learn)", "🎧 Aprender (Learn)", "🎧 学習 (Learn)", "🎧 학습 (Learn)");
		Add("尚未学习音频频谱", "尚未學習音訊頻譜", "Audio spectrum not learned yet", "Spectre audio non encore appris", "Спектр аудио ещё не изучен", "Espectro de audio aún no aprendido", "オーディオスペクトルは未学習です", "오디오 스펙트럼이 아직 학습되지 않음");
		Add("已完成分析 · {0} · 推荐前级 {1:+0.0;-0.0;0.0} dB", "已完成分析 · {0} · 推薦前級 {1:+0.0;-0.0;0.0} dB", "Analysis complete · {0} · Recommended Preamp {1:+0.0;-0.0;0.0} dB", "Analyse terminée · {0} · Préampli suggéré {1:+0.0;-0.0;0.0} dB", "Анализ завершён · {0} · Преамп {1:+0.0;-0.0;0.0} dB", "Análisis completo · {0} · Preamp recomendado {1:+0.0;-0.0;0.0} dB", "解析完了 · {0} · 推奨プリアンプ {1:+0.0;-0.0;0.0} dB", "분석 완료 · {0} · 권장 프리앰프 {1:+0.0;-0.0;0.0} dB");
		Add("等待频谱学习后显示声学诊断标签...", "等待頻譜學習後顯示聲學診斷標籤...", "Acoustic diagnostic tags will appear after learning...", "Les étiquettes de diagnostic apparaîtront après l'apprentissage...", "Теги акустической диагностики появятся после обучения...", "Las etiquetas de diagnóstico aparecerán tras el aprendizaje...", "スペクトル学習後に音響診断タグが表示されます...", "스펙트럼 학습 후 음향 진단 태그가 표시됩니다...");
		Add("🔍 分析音轨 (Analyze)", "🔍 分析音軌 (Analyze)", "🔍 Analyze Track", "🔍 Analyser la piste", "🔍 Анализ трека", "🔍 Analizar pista", "🔍 トラック解析 (Analyze)", "🔍 트랙 분석 (Analyze)");
		Add("目标风格:", "目標風格:", "Target Style:", "Style cible :", "Целевой стиль:", "Estilo objetivo:", "ターゲット:", "타겟 스타일:");
		Add("自动防掩蔽 (De-Mask)", "自動防遮蔽 (De-Mask)", "Auto De-Mask", "Anti-masquage (De-Mask)", "Авто De-Mask", "De-Mask automático", "自動デマスキング (De-Mask)", "자동 디마스킹 (De-Mask)");
		Add("开", "開", "On", "Activé", "Вкл", "Sí", "オン", "켜짐");
		Add("关", "關", "Off", "Désactivé", "Выкл", "No", "オフ", "꺼짐");
		Add("匹配强度 Amount", "匹配強度 Amount", "Match Amount", "Intensité (Amount)", "Сила подгонки", "Intensidad (Amount)", "マッチ強度 Amount", "매칭 강도 Amount");
		Add("低频 Sculpt (Low Shelf)", "低頻 Sculpt (Low Shelf)", "Low Sculpt (Low Shelf)", "Sculpt Graves (Low Shelf)", "Низкие Sculpt (Low Shelf)", "Sculpt Graves (Low Shelf)", "低域 Sculpt (Low Shelf)", "저음 Sculpt (Low Shelf)");
		Add("高频 Sculpt (High Shelf)", "高頻 Sculpt (High Shelf)", "High Sculpt (High Shelf)", "Sculpt Aigus (High Shelf)", "Высокие Sculpt (High Shelf)", "Sculpt Agudos (High Shelf)", "高域 Sculpt (High Shelf)", "고음 Sculpt (High Shelf)");
		Add("✨ 应用助手方案 Apply", "✨ 套用助手方案 Apply", "✨ Apply Assistant", "✨ Appliquer l'assistant", "✨ Применить решение", "✨ Aplicar asistente", "✨ アシスタント案を適用", "✨ 어시스턴트 적용");
		Add("🔄 重置", "🔄 重置", "🔄 Reset", "🔄 Réinitialiser", "🔄 Сброс", "🔄 Restablecer", "🔄 リセット", "🔄 초기화");

		// ParametricEqWorkbenchControl — Bottom Toolbar
		Add("保存为用户预设名称...", "儲存為使用者預設名稱...", "Save as preset name...", "Nom du préréglage...", "Имя пресета...", "Nombre del preajuste...", "ユーザー設定名を入力...", "사용자 프리셋 이름...");
		Add("保存到预设", "儲存到預設", "Save Preset", "Enregistrer", "Сохранить пресет", "Guardar preajuste", "プリセットに保存", "프리셋 저장");
		Add("🔄 重置曲线", "🔄 重置曲線", "🔄 Reset Curve", "🔄 Réinitialiser courbe", "🔄 Сбросить кривую", "🔄 Restablecer curva", "🔄 カーブをリセット", "🔄 커브 초기화");
		Add("📁 导入预设 / APO", "📁 匯入預設 / APO", "📁 Import Preset / APO", "📁 Importer préréglage / APO", "📁 Импорт пресета / APO", "📁 Importar preajuste / APO", "📁 プリセット / APO をインポート", "📁 프리셋 / APO 가져오기");
		Add("自定义预设 {0}", "自訂預設 {0}", "Custom Preset {0}", "Préréglage perso {0}", "Свой пресет {0}", "Preajuste personalizado {0}", "カスタムプリセット {0}", "사용자 프리셋 {0}");

		// MixAssistantEngine — Target Styles
		Add("平衡母带 (Balanced Master)", "平衡母帶 (Balanced Master)", "Balanced Master", "Master équilibré (Balanced Master)", "Сбалансированный мастер", "Máster equilibrado", "バランスドマスター (Balanced Master)", "밸런스 마스터 (Balanced Master)");
		Add("自然平衡的母带级频响目标，控制超低频冗余并保持细腻高频延伸", "自然平衡的母帶級頻響目標，控制超低頻冗餘並保持細膩高頻延伸", "Natural, balanced mastering target that tames sub-bass excess while preserving smooth airy highs.", "Cible de mastering naturelle et équilibrée qui maîtrise l'extrême-grave tout en préservant la finesse des aigus.", "Естественный сбалансированный мастеринг-профиль: контроль суббаса и плавный верх.", "Objetivo de masterización natural que controla el exceso de subgraves manteniendo agudos suaves.", "超低域の余分な膨らみを抑えつつ、繊細な高域の伸びを保つ自然なマスタリングターゲット", "초저역 과잉을 억제하고 섬세한 고역 확장을 유지하는 자연스러운 마스터링 타겟");

		Add("温暖人声 (Warm Vocals)", "溫暖人聲 (Warm Vocals)", "Warm Vocals", "Voix chaleureuses (Warm Vocals)", "Тёплый вокал (Warm Vocals)", "Voces cálidas (Warm Vocals)", "ウォームボーカル (Warm Vocals)", "따뜻한 보컬 (Warm Vocals)");
		Add("突出人声厚度与临场亲和力，柔化齿音与极高频刺激", "突出人聲厚度與臨場親和力，柔化齒音與極高頻刺激", "Enhances vocal body and intimate presence while softening sibilance and harsh top-end.", "Met en valeur le corps et la présence vocale tout en adoucissant les sifflantes et les aigus agressifs.", "Подчёркивает теплоту и присутствие вокала, смягчая сибилянты и резкий верх.", "Realza el cuerpo y la presencia vocal suavizando las sibilancias y los agudos agresivos.", "ボーカルの厚みと親密な存在感を引き立て、歯擦音や高域の刺激を和らげます", "보컬의 두께감과 친밀한 존재감을 살리고 치찰음과 자극적인 초고역을 완화합니다");

		Add("低频冲击 (Bass & Punch)", "低頻衝擊 (Bass & Punch)", "Bass & Punch", "Basses & Punch (Bass & Punch)", "Бас и панч (Bass & Punch)", "Graves y pegada (Bass & Punch)", "ベース & パンチ (Bass & Punch)", "베이스 & 펀치 (Bass & Punch)");
		Add("强化下潜与鼓点冲击力，清理中低频浑浊感", "強化下潛與鼓點衝擊力，清理中低頻渾濁感", "Boosts sub-bass extension and kick punch while clearing low-mid muddiness.", "Renforce l'infra-grave et l'impact tout en nettoyant la boue du bas-médium.", "Усиливает глубину суббаса и удар, очищая мутность нижней середины.", "Refuerza el subgrave y la pegada limpiando la turbidez en medios-graves.", "重低音の沈み込みとキックの迫力を強め、中低域のこもりをすっきり整えます", "서브베이스의 깊이와 펀치감을 강화하고 중저역의 탁함을 정리합니다");

		Add("鼓组节奏 (Drums / Transient)", "鼓組節奏 (Drums / Transient)", "Drums / Transient", "Batterie / Transitoires", "Ударные / Транзиенты", "Batería / Transitorios", "ドラム / トランジェント", "드럼 / 트랜지언트");
		Add("强化底鼓下盘与军鼓瞬态脆度，抑制箱音堆积", "強化底鼓下盤與軍鼓瞬態脆度，抑制箱音堆積", "Enhances kick weight and snare transient snap while taming boxy resonances.", "Accentue l'assise de la grosse caisse et le claquant de la caisse claire en réduisant la résonance de caisse.", "Подчёркивает плотность бочки и атаку рабочего барабана, подавляя коробочный призвук.", "Potencia el peso del bombo y la nitidez de la caja controlando resonancias encajonadas.", "キックの重心とスネアの歯切れ良いアタックを強調し、箱鳴りのこもりを抑えます", "킥의 무게감과 스네어의 탄력 있는 어택을 살리고 통울림 공진을 억제합니다");

		Add("流行现代 (Modern Pop)", "流行現代 (Modern Pop)", "Modern Pop", "Pop moderne (Modern Pop)", "Современный поп (Modern Pop)", "Pop moderno (Modern Pop)", "モダンポップ (Modern Pop)", "모던 팝 (Modern Pop)");
		Add("现代流媒体微笑轮廓：紧致低频、清透中高频与空气感", "現代串流微笑輪廓：緊緻低頻、清透中高頻與空氣感", "Modern streaming smile contour: tight bass, crisp upper-mids, and airy highs.", "Profil streaming moderne : graves tendus, hauts-médiums clairs et aigus aériens.", "Современный стриминговый профиль: плотный бас, чистая верхняя середина и воздух.", "Contorno moderno de streaming: graves firmes, medios-agudos claros y aire.", "現代的なストリーミング向けカーブ：引き締まった低音、クリアな中高音とエアー感", "현대 스트리밍용 스마일 커브: 단단한 저음, 선명한 중고음과 공기감");

		Add("ACG 动感 / 二次元 (ACG Vibrant)", "ACG 動感 / 二次元 (ACG Vibrant)", "ACG Vibrant", "ACG Dynamique (ACG Vibrant)", "ACG Яркий (ACG Vibrant)", "ACG Vibrante (ACG Vibrant)", "ACG バイブラント (ACG Vibrant)", "ACG 바이브런트 (ACG Vibrant)");
		Add("专为日系电音与女声优化：明亮通透的华丽感与弹性低频", "專為日系電音與女聲最佳化：明亮通透的華麗感與彈性低頻", "Tailored for J-electronic and female vocals: bright sparkle with bouncy bass.", "Optimisé pour l'électro japonaise et les voix féminines : brillance cristalline et basses rebondissantes.", "Оптимизировано для J-pop и женского вокала: яркость, прозрачность и упругий бас.", "Optimizado para electrónica japonesa y voces femeninas: brillo cristalino y graves elásticos.", "アニソン・日系エレクトロ・女性ボーカル向け：華やかな抜けと弾む低音", "일본풍 일렉트로닉과 여성 보컬에 최적화: 화사한 개방감과 탄력 있는 저음");

		Add("通透空气 (Bright & Airy)", "通透空氣 (Bright & Airy)", "Bright & Airy", "Clair & Aéré (Bright & Airy)", "Светлый и воздушный", "Brillante y aéreo", "ブライト & エアリー (Bright & Airy)", "브라이트 & 에어리 (Bright & Airy)");
		Add("轻盈低频配合宽广极高频空气感，提升空间解析力", "輕盈低頻配合寬廣極高頻空氣感，提升空間解析力", "Light bass paired with expansive top-end air for enhanced spatial clarity.", "Basses légères associées à un extrême-aigu aérien pour plus de clarté spatiale.", "Лёгкий бас и широкий воздушный верх для повышения пространственной детальности.", "Graves ligeros con amplio aire en agudos para mayor claridad espacial.", "軽やかな低音と広がりのある高域のエアー感で、空間の解像度を高めます", "가벼운 저음과 넓은 초고역 공기감으로 공간 해상력을 높입니다");

		Add("深夜轻听 (Late Night Fletcher-Munson)", "深夜輕聽 (Late Night Fletcher-Munson)", "Late Night (Fletcher-Munson)", "Écoute nocturne (Fletcher-Munson)", "Ночной режим (Флетчер-Мэнсон)", "Modo nocturno (Fletcher-Munson)", "レイトナイト (Fletcher-Munson)", "심야 감상 (Fletcher-Munson)");
		Add("基于 Fletcher-Munson 等响曲线补偿低音量下的低音与细节流失", "基於 Fletcher-Munson 等響曲線補償低音量下的低音與細節流失", "Fletcher-Munson equal-loudness compensation to restore bass and detail at low volume.", "Compensation isosonique Fletcher-Munson pour préserver les graves et les détails à faible volume.", "Компенсация по кривым Флетчера-Мэнсона для сохранения баса и деталей на низкой громкости.", "Compensación Fletcher-Munson para recuperar graves y detalle a bajo volumen.", "Fletcher-Munson 等ラウドネス曲線に基づき、小音量時の低音とディテール不足を補正", "Fletcher-Munson 등청감 곡선을 기반으로 작은 볼륨에서의 저음과 디테일 손실 보정");

		// MixAssistantEngine — Spectral Diagnostic Issues
		Add("低频轰鸣过量", "低頻轟鳴過量", "Excessive sub-bass rumble", "Excès de grondement dans le grave", "Избыточный гул суббаса", "Exceso de retumbo en subgraves", "超低域のブーミー過多", "초저역 부밍 과다");
		Add("低频下潜不足", "低頻下潛不足", "Lacking sub-bass extension", "Manque d'extension dans le grave", "Недостаточная глубина суббаса", "Falta de extensión en subgraves", "低域の沈み込み不足", "저역 깊이감 부족");
		Add("250 Hz 浑浊堆积", "250 Hz 混濁堆積", "250 Hz mud buildup", "Accumulation boueuse à 250 Hz", "Мутность в районе 250 Гц", "Acumulación turbia en 250 Hz", "250 Hz 付近のこもり蓄積", "250 Hz 대역 혼탁함 누적");
		Add("中低频偏薄", "中低頻偏薄", "Thin low-midrange", "Bas-médium un peu mince", "Тонкая нижняя середина", "Medios-graves delgados", "中低域が薄め", "중저역이 다소 얇음");
		Add("1 kHz 鼻音突出", "1 kHz 鼻音突出", "1 kHz nasal prominence", "Résonance nasale à 1 kHz", "Гнусавость на 1 кГц", "Prominencia nasal en 1 kHz", "1 kHz 付近の鼻詰まり感", "1 kHz 비음 대역 돌출");
		Add("中频人声凹陷", "中頻人聲凹陷", "Scooped midrange / vocals", "Médiums / voix en retrait", "Провал середины / вокала", "Medios / voces hundidos", "中域・ボーカルの引っ込み", "중역 / 보컬 함몰");
		Add("3.2 kHz 齿音尖刺", "3.2 kHz 齒音尖刺", "3.2 kHz sibilance spike", "Pic de sibilance à 3,2 kHz", "Резкие сибилянты на 3,2 кГц", "Pico de sibilancia en 3,2 kHz", "3.2 kHz 付近の歯擦音ピーク", "3.2 kHz 치찰음 피크");
		Add("高频延伸不足", "高頻延伸不足", "Rolled-off high frequencies", "Aigus en retrait", "Недостаток высоких частот", "Falta de extensión en agudos", "高域の伸び不足", "고역 확장 부족");
		Add("极高频偏亮刺耳", "極高頻偏亮刺耳", "Harsh / overly bright highs", "Extrême-aigu trop brillant / agressif", "Резкий, слишком яркий верх", "Agudos excesivamente brillantes", "超高域が過度に明るく耳障り", "초고역이 과도하게 밝고 자극적임");
		Add("频谱分布整体均衡", "頻譜分佈整體均衡", "Balanced overall spectrum", "Spectre globalement équilibré", "Спектр в целом сбалансирован", "Espectro general equilibrado", "全体的なスペクトルバランスは良好", "전체 스펙트럼 분포가 균형 잡힘");

		// ParametricEqCurveControl — Frequency Bands
		Add("低频", "低頻", "Bass", "Graves", "Бас", "Graves", "低域", "저역");
		Add("中低频", "中低頻", "Low-Mid", "Bas-médium", "Ниж. середина", "Medios-graves", "中低域", "중저역");
		Add("中频", "中頻", "Mid", "Médiums", "Середина", "Medios", "中域", "중역");
		Add("临场感", "臨場感", "Presence", "Présence", "Присутствие", "Presencia", "プレゼンス", "프레즌스");
		Add("明亮", "明亮", "Brilliance", "Brillance", "Яркость", "Brillo", "ブリリアンス", "브릴리언스");

		// PhaseAnalyzerControl / SpectrumAnalyzer / WaveformControl
		Add("相位差: {0:F1}°   相关度: {1:+0.00;-0.00;0.00}", "相位差: {0:F1}°   相關度: {1:+0.00;-0.00;0.00}", "Phase: {0:F1}°   Corr: {1:+0.00;-0.00;0.00}", "Phase : {0:F1}°   Corr. : {1:+0.00;-0.00;0.00}", "Фаза: {0:F1}°   Корр.: {1:+0.00;-0.00;0.00}", "Fase: {0:F1}°   Corr.: {1:+0.00;-0.00;0.00}", "位相差: {0:F1}°   相関: {1:+0.00;-0.00;0.00}", "위상차: {0:F1}°   상관도: {1:+0.00;-0.00;0.00}");
		Add("峰值: {0:F0} Hz · {1:F1} dBFS", "峰值: {0:F0} Hz · {1:F1} dBFS", "Peak: {0:F0} Hz · {1:F1} dBFS", "Crête : {0:F0} Hz · {1:F1} dBFS", "Пик: {0:F0} Гц · {1:F1} dBFS", "Pico: {0:F0} Hz · {1:F1} dBFS", "ピーク: {0:F0} Hz · {1:F1} dBFS", "피크: {0:F0} Hz · {1:F1} dBFS");
		Add("未加载音频文件，无法显示波形", "未載入音訊檔案，無法顯示波形", "No audio file loaded", "Aucun fichier audio chargé", "Аудиофайл не загружен", "No hay archivo de audio cargado", "音声ファイルが読み込まれていません", "오디오 파일이 로드되지 않음");
		Add("PK 峰值包络 · 线性振幅 (0 ~ -60 dB FS)", "PK 峰值包絡 · 線性振幅 (0 ~ -60 dB FS)", "PK Peak Envelope · Linear (0 ~ -60 dB FS)", "Enveloppe crête PK · Linéaire (0 ~ -60 dB FS)", "Пиковая огибающая PK · Линейная (0 ~ -60 dB FS)", "Envolvente de pico PK · Lineal (0 ~ -60 dB FS)", "PK ピークエンベロープ · リニア振幅 (0 ~ -60 dB FS)", "PK 피크 엔벨로프 · 선형 진폭 (0 ~ -60 dB FS)");
		Add("RMS 有效值包络 · 线性振幅 (0 ~ -60 dB FS)", "RMS 有效值包絡 · 線性振幅 (0 ~ -60 dB FS)", "RMS Envelope · Linear (0 ~ -60 dB FS)", "Enveloppe RMS · Linéaire (0 ~ -60 dB FS)", "Огибающая RMS · Линейная (0 ~ -60 dB FS)", "Envolvente RMS · Lineal (0 ~ -60 dB FS)", "RMS 実効値エンベロープ · リニア振幅 (0 ~ -60 dB FS)", "RMS 실효값 엔벨로프 · 선형 진폭 (0 ~ -60 dB FS)");

		// MixAssistantEngine Upgraded — Genre, Headphone Matching & Anti-Distortion
		Add("🧠 智能识别曲风 (Auto Match)", "🧠 智慧辨識曲風 (Auto Match)", "🧠 Auto Genre Match", "🧠 Auto-Détection du Genre", "🧠 Авто-подбор жанра", "🧠 Auto-Detección de Género", "🧠 ジャンル自動認識 (Auto Match)", "🧠 장르 자동 인식 (Auto Match)");
		Add("根据实时频谱、动态余量与斜率自动匹配最优曲风与母带目标曲线", "根據即時頻譜、動態餘量與斜率自動匹配最優曲風與母帶目標曲線", "Automatically detects genre, dynamics, and tilt to select the optimal target curve.", "Détecte le genre et la dynamique pour choisir la cible optimale.", "Автоматически определяет жанр и динамику для выбора оптимальной целевой кривой.", "Detecta género y dinámica para seleccionar la curva objetivo óptima.", "スペクトル・ダイナミクス・傾きから最適なターゲットカーブを自動選択します", "스펙트럼, 다이내믹, 기울기를 분석해 최적의 타겟 커브를 자동 선택합니다");
		Add("摇滚现场 (Rock & Metal)", "搖滾現場 (Rock & Metal)", "Rock & Metal", "Rock & Métal", "Рок и металл", "Rock & Metal", "ロック & メタル (Rock & Metal)", "록 & 메탈 (Rock & Metal)");
		Add("压制 250-400Hz 吉他厚闷感并柔化 4kHz 噪刺，突出拳拳到肉的底鼓能量", "壓制 250-400Hz 吉他厚悶感並柔化 4kHz 噪刺，突出拳拳到肉的底鼓能量", "Cuts 250-400 Hz guitar mud and smooths 4 kHz bite for tight punchy drums.", "Atténue la boue 250-400 Hz et adoucit les 4 kHz pour plus d'impact.", "Снижает мутность 250-400 Гц и смягчает 4 кГц для плотного удара.", "Reduce turbidez en 250-400 Hz y suaviza 4 kHz manteniendo pegada.", "250-400Hz のこもりを抑え 4kHz の刺さりを和らげ、タイトなドラムを引き立てます", "250-400Hz 탁함을 줄이고 4kHz 거친 대역을 다듬어 단단한 펀치를 살립니다");
		Add("古典原声 (Classical Reference)", "古典原聲 (Classical Reference)", "Classical Reference", "Référence Classique", "Классический референс", "Referencia Clásica", "クラシック・リファレンス", "클래식 레퍼런스");
		Add("保留大动态交响呼吸感与厅堂空气延伸，轻柔平滑中高频染色", "保留大動態交響呼吸感與廳堂空氣延伸，輕柔平滑中高頻染色", "Preserves orchestral dynamic breathing room and hall air with gentle treble polish.", "Préserve la dynamique orchestrale et l'air de salle sans coloration.", "Сохраняет оркестровую динамику и воздух зала с мягкой коррекцией.", "Preserva la dinámica orquestal y el aire de sala suavizando los medios-agudos.", "オーケストラの大ダイナミクスとホールの空気感を保ちながら滑らかに整えます", "오케스트라의 넓은 다이내믹과 홀의 공기감을 유지하며 부드럽게 다듬습니다");

		Add("现代流行 / 商业混音 (Modern Pop)", "現代流行 / 商業混音 (Modern Pop)", "Modern Pop", "Pop Moderne", "Современный поп", "Pop Moderno", "モダンポップ", "모던 팝");
		Add("ACG 二次元 / 日系电音 (ACG Vibrant)", "ACG 二次元 / 日系電音 (ACG Vibrant)", "ACG / J-Electronic", "ACG / Électro Japonaise", "ACG / J-Электроника", "ACG / J-Electrónica", "ACG / アニソン", "ACG / J-Pop");
		Add("抒情华语 / 原声人声 (Vocal Ballad)", "抒情華語 / 原聲人聲 (Vocal Ballad)", "Vocal Ballad", "Ballade Vocale", "Вокальная баллада", "Balada Vocal", "ボーカルバラード", "보컬 발라드");
		Add("电音 / 说唱重低音 (EDM & Hip-Hop)", "電音 / 說唱重低音 (EDM & Hip-Hop)", "EDM & Hip-Hop", "EDM & Hip-Hop", "EDM и Хип-хоп", "EDM y Hip-Hop", "EDM / ヒップホップ", "EDM & 힙합");
		Add("摇滚 / 金属现场 (Rock & Metal)", "搖滾 / 金屬現場 (Rock & Metal)", "Rock & Metal", "Rock & Métal", "Рок и металл", "Rock & Metal", "ロック & メタル", "록 & 메탈");
		Add("古典 / 交响大动态 (Classical)", "古典 / 交響大動態 (Classical)", "Classical / Orchestral", "Classique / Orchestre", "Классика / Оркестр", "Clásica / Orquesta", "クラシック / 交響曲", "클래식 / 오케스트라");
		Add("爵士 / 发烧器乐 (Jazz & Acoustic)", "爵士 / 發燒器樂 (Jazz & Acoustic)", "Jazz & Acoustic", "Jazz & Acoustique", "Джаз и акустика", "Jazz y Acústica", "ジャズ / アコースティック", "재즈 / 어쿠스틱");
		Add("Lo-Fi / 氛围温暖 (Lo-Fi Ambient)", "Lo-Fi / 氛圍溫暖 (Lo-Fi Ambient)", "Lo-Fi / Ambient", "Lo-Fi / Ambiant", "Lo-Fi / Эмбиент", "Lo-Fi / Ambient", "Lo-Fi / アンビエント", "Lo-Fi / 앰비언트");
		Add("综合均衡曲风 (Balanced)", "綜合均衡曲風 (Balanced)", "Balanced Genre", "Genre Équilibré", "Сбалансированный жанр", "Género Equilibrado", "バランスジャンル", "밸런스 장르");

		Add("🛡️ 柔和防失真 (Gentle)", "🛡️ 柔和防失真 (Gentle)", "🛡️ Gentle Safe", "🛡️ Doux Anti-Écrêtage", "🛡️ Мягкий без клиппинга", "🛡️ Suave Anticlip", "🛡️ ジェントル安全 (Gentle)", "🛡️ 저왜곡 젠틀 (Gentle)");
		Add("⚖️ 标准母带 (Balanced)", "⚖️ 標準母帶 (Balanced)", "⚖️ Balanced Studio", "⚖️ Studio Équilibré", "⚖️ Сбалансированный", "⚖️ Estudio Equilibrado", "⚖️ バランスド (Balanced)", "⚖️ 밸런스 스튜디오 (Balanced)");
		Add("✨ 鲜明饱满 (Vivid)", "✨ 鮮明飽滿 (Vivid)", "✨ Vivid Dynamic", "✨ Vif & Dynamique", "✨ Яркий и насыщенный", "✨ Vívido y Dinámico", "✨ ビビッド (Vivid)", "✨ 비비드 (Vivid)");

		Add("按佩戴方式 (200款)", "按佩戴方式 (200款)", "By Wearing (Top 200)", "Par Port (200)", "По посадке (200)", "Por Uso (200)", "装着方式別 (200種)", "착용 방식별 (200종)");
		Add("按发音单元 (200款)", "按發音單元 (200款)", "By Driver (Top 200)", "Par Transducteur (200)", "По драйверу (200)", "Por Driver (200)", "ドライバー別 (200種)", "드라이버별 (200종)");
		Add("按调音风格 (200款)", "按調音風格 (200款)", "By Tuning (Top 200)", "Par Signature (200)", "По тюнингу (200)", "Por Firma (200)", "チューニング別 (200種)", "튜닝 성향별 (200종)");
		Add("🔗 跟随耳机校正 / 自动匹配", "🔗 跟隨耳機校正 / 自動匹配", "🔗 Follow Calibration / Auto", "🔗 Suivre Calibration / Auto", "🔗 Авто / Калибровка", "🔗 Seguir Calibración / Auto", "🔗 ヘッドホン補正に連動 / 自動", "🔗 헤드폰 보정 연동 / 자동");

		Add("包耳式 (Over-Ear)", "包耳式 (Over-Ear)", "Over-Ear", "Circum-auriculaire", "Полноразмерные", "Circumaural", "オーバーイヤー", "오버이어");
		Add("压耳式 (On-Ear)", "壓耳式 (On-Ear)", "On-Ear", "Supra-auriculaire", "Накладные", "Supraaural", "オンイヤー", "온이어");
		Add("入耳监听 (IEM)", "入耳監聽 (IEM)", "In-Ear IEM", "Intra-auriculaire (IEM)", "Внутриканальные (IEM)", "In-Ear IEM", "インイヤー (IEM)", "인이어 모니터 (IEM)");
		Add("真无线 (TWS)", "真無線 (TWS)", "True Wireless (TWS)", "True Wireless (TWS)", "TWS", "True Wireless (TWS)", "完全ワイヤレス (TWS)", "완전 무선 (TWS)");
		Add("平头塞 (Earbud)", "平頭塞 (Earbud)", "Flat Earbud", "Écouteurs plats", "Вкладыши", "Botón (Earbud)", "インナーイヤー型", "오픈형 이어버드");
		Add("自动佩戴识别", "自動佩戴識別", "Auto Wearing", "Port Auto", "Авто посадка", "Uso Auto", "装着自動判定", "착용 자동 감지");

		Add("动圈单元 (DD)", "動圈單元 (DD)", "Dynamic Driver (DD)", "Dynamique (DD)", "Динамический (DD)", "Dinámico (DD)", "ダイナミック (DD)", "다이내믹 (DD)");
		Add("平板振膜 (Planar)", "平板振膜 (Planar)", "Planar Magnetic", "Planaire Magnétique", "Планарный", "Planar Magnético", "平面駆動 (Planar)", "평판형 (Planar)");
		Add("动铁单元 (BA)", "動鐵單元 (BA)", "Balanced Armature (BA)", "Armature Équilibrée", "Арматурный (BA)", "Armadura Balanceada", "バランスド・アーマチュア (BA)", "밸런스드 아마추어 (BA)");
		Add("圈铁/多单元混合 (Hybrid)", "圈鐵/多單元混合 (Hybrid)", "Hybrid / Tribrid", "Hybride / Tribride", "Гибридный", "Híbrido / Tríbrido", "ハイブリッド / トライブリッド", "하이브리드 / 트라이브리드");
		Add("静电单元 (EST)", "靜電單元 (EST)", "Electrostatic (EST)", "Électrostatique (EST)", "Электростатический", "Electrostático (EST)", "静電型 (EST)", "정전형 (EST)");
		Add("自动单元识别", "自動單元識別", "Auto Driver", "Transducteur Auto", "Авто драйвер", "Driver Auto", "ドライバー自動判定", "드라이버 자동 감지");

		Add("哈曼中性参考 (Harman Neutral)", "哈曼中性參考 (Harman Neutral)", "Harman Neutral", "Neutre Harman", "Нейтральный Harman", "Neutral Harman", "ハーマン・ニュートラル", "하만 뉴트럴");
		Add("暖声润泽 (Warm & Lush)", "暖聲潤澤 (Warm & Lush)", "Warm & Lush", "Chaleureux", "Тёплый и мягкий", "Cálido y rico", "ウォーム＆リッチ", "따뜻하고 풍부한 톤");
		Add("高解析偏亮 (Bright Analytical)", "高解析偏亮 (Bright Analytical)", "Bright Analytical", "Clair & Analytique", "Яркий аналитический", "Brillante Analítico", "ブライト・分析的", "밝고 고해상도 성향");
		Add("V型流行/刺激 (V-Shaped)", "V型流行/刺激 (V-Shaped)", "V-Shaped", "En V", "V-образный", "En V", "ドンシャリ (V字)", "V자형 사운드");
		Add("低频澎湃 (Bass-Heavy)", "低頻澎湃 (Bass-Heavy)", "Bass-Heavy", "Basses Renforcées", "Усиленный бас", "Graves Potentes", "重低音寄り", "저음 강조 성향");
		Add("中频人声见长 (Mid-Centric)", "中頻人聲見長 (Mid-Centric)", "Mid-Centric Vocal", "Centré Médiums", "Серединоцентричный", "Centrado en Medios", "中域・ボーカル重視", "중역·보컬 중심 성향");
		Add("自动调音识别", "自動調音識別", "Auto Tuning", "Signature Auto", "Авто тюнинг", "Firma Auto", "チューニング自動判定", "튜닝 자동 감지");

		Add("🎧 OPRA 耳机硬件与调音匹配 (600 款旗舰/热门已学习):", "🎧 OPRA 耳機硬體與調音匹配 (600 款旗艦/熱門已學習):", "🎧 OPRA Headphone Match (600 Flagships Learned):", "🎧 Adaptation Casque OPRA (600 modèles):", "🎧 Адаптация под наушники OPRA (600 моделей):", "🎧 Ajuste Auriculares OPRA (600 modelos):", "🎧 OPRA ヘッドホン最適化 (600 機種学習済):", "🎧 OPRA 헤드폰 매칭 (600종 플래그십 학습):");
		Add("拟合力度:", "擬合力度:", "Match:", "Force :", "Сила:", "Ajuste:", "適用度:", "적용도:");
		Add("防失真档位:", "防失真檔位:", "Safety:", "Sécurité :", "Защита:", "Seguridad:", "安全モード:", "왜곡 방지:");
		Add("识别曲风: 等待分析...", "辨識曲風: 等待分析...", "Genre: Waiting for analysis...", "Genre : en attente...", "Жанр: ожидание...", "Género: esperando...", "ジャンル: 解析待ち...", "장르: 분석 대기 중...");
		Add("🎵 识别曲风: {0} ({1:0}%)", "🎵 辨識曲風: {0} ({1:0}%)", "🎵 Genre: {0} ({1:0}%)", "🎵 Genre : {0} ({1:0}%)", "🎵 Жанр: {0} ({1:0}%)", "🎵 Género: {0} ({1:0}%)", "🎵 曲風: {0} ({1:0}%)", "🎵 장르: {0} ({1:0}%)");
		Add("峰值 +{0:0.0} dB · 前级保护 {1:+0.0;-0.0;0.0} dB · 无失真余量 +{2:0.0} dB", "峰值 +{0:0.0} dB · 前級保護 {1:+0.0;-0.0;0.0} dB · 無失真餘量 +{2:0.0} dB", "Peak +{0:0.0} dB · Preamp {1:+0.0;-0.0;0.0} dB · Margin +{2:0.0} dB", "Pic +{0:0.0} dB · Préampli {1:+0.0;-0.0;0.0} dB · Marge +{2:0.0} dB", "Пик +{0:0.0} дБ · Преамп {1:+0.0;-0.0;0.0} дБ · Запас +{2:0.0} дБ", "Pico +{0:0.0} dB · Preamp {1:+0.0;-0.0;0.0} dB · Margen +{2:0.0} dB", "ピーク +{0:0.0} dB · プリアンプ保護 {1:+0.0;-0.0;0.0} dB · 余白 +{2:0.0} dB", "피크 +{0:0.0} dB · 프리앰프 보호 {1:+0.0;-0.0;0.0} dB · 여유 +{2:0.0} dB");
		Add("🎧 校正联动: {0} (防重复叠加以保护失真)", "🎧 校正連動: {0} (防重複疊加以保護失真)", "🎧 Calibrated: {0} (Double-EQ Guard Active)", "🎧 Calibration active : {0}", "🎧 Калибровка: {0} (защита от двойного EQ)", "🎧 Calibrado: {0} (sin doble EQ)", "🎧 補正連動: {0} (二重EQ防止)", "🎧 보정 연동: {0} (이중 EQ 방지)");
		Add("🎧 匹配耳机: {0} (未开校正 · 已启用硬件补偿)", "🎧 匹配耳機: {0} (未開校正 · 已啟用硬體補償)", "🎧 Hardware Match: {0} (Calibration Off)", "🎧 Casque : {0} (compensation matérielle)", "🎧 Наушники: {0} (аппаратная коррекция)", "🎧 Auricular: {0} (ajuste físico)", "🎧 マッチ: {0} (校正オフ・ハード補正)", "🎧 매칭: {0} (보정 꺼짐·하드웨어 보상)");
		Add("🎧 未指定型号 · 按下方佩戴/单元/调音属性智能适配", "🎧 未指定型號 · 按下方佩戴/單元/調音屬性智慧適配", "🎧 Generic Mode · Adapts to Wearing / Driver / Tuning below", "🎧 Mode générique · S'adapte aux options ci-dessous", "🎧 Общий режим · Адаптация по выбранным параметрам", "🎧 Modo genérico · Se adapta a los atributos de abajo", "🎧 汎用モード · 下の装着/ドライバー/傾向に最適化", "🎧 일반 모드 · 아래 착용/드라이버/튜닝 속성에 맞춤");
		Add("已协同耳机校正 ({0})", "已協同耳機校正 ({0})", "Synced with Calibration ({0})", "Synchronisé ({0})", "Синхр. с калибровкой ({0})", "Sincronizado ({0})", "ヘッドホン補正と協調 ({0})", "헤드폰 보정과 연동 ({0})");
		Add("已适配耳机特性 ({0})", "已適配耳機特性 ({0})", "Matched to {0}", "Adapté à {0}", "Подогнано под {0}", "Ajustado a {0}", "イヤホン特性に最適化 ({0})", "이어폰 특성 맞춤 ({0})");
		Add("已开启耳机校正 · {0} · 防重复叠加以保护失真", "已開啟耳機校正 · {0} · 防重複疊加以保護失真", "Calibration ON · {0} · Anti-clipping & no double-EQ", "Calibration active · {0} · Protection anti-clipping", "Калибровка ВКЛ · {0} · Защита от искажений", "Calibración ACTIVA · {0} · Sin doble EQ ni distorsión", "補正ON · {0} · 重複EQ防止&歪み保護", "보정 켜짐 · {0} · 이중 EQ 및 왜곡 방지");
		Add("超低 {0:+0.0;-0.0;0.0} / 临场 {1:+0.0;-0.0;0.0} / 空气 {2:+0.0;-0.0;0.0} dB · 动态 {3:F1} dB", "超低 {0:+0.0;-0.0;0.0} / 臨場 {1:+0.0;-0.0;0.0} / 空氣 {2:+0.0;-0.0;0.0} dB · 動態 {3:F1} dB", "Sub {0:+0.0;-0.0;0.0} / Pres {1:+0.0;-0.0;0.0} / Air {2:+0.0;-0.0;0.0} dB · Crest {3:F1} dB", "Infra {0:+0.0;-0.0;0.0} / Prés {1:+0.0;-0.0;0.0} / Air {2:+0.0;-0.0;0.0} dB · Crête {3:F1} dB", "Суб {0:+0.0;-0.0;0.0} / През {1:+0.0;-0.0;0.0} / Воздух {2:+0.0;-0.0;0.0} дБ · Динам. {3:F1} дБ", "Sub {0:+0.0;-0.0;0.0} / Pres {1:+0.0;-0.0;0.0} / Aire {2:+0.0;-0.0;0.0} dB · Crest {3:F1} dB", "超低 {0:+0.0;-0.0;0.0} / プレゼンス {1:+0.0;-0.0;0.0} / エアー {2:+0.0;-0.0;0.0} dB · Crest {3:F1} dB", "서브 {0:+0.0;-0.0;0.0} / 프레즌스 {1:+0.0;-0.0;0.0} / 에어 {2:+0.0;-0.0;0.0} dB · 다이나믹 {3:F1} dB");
	}
}
