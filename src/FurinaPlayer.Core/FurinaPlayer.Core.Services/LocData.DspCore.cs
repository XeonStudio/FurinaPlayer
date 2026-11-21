namespace FurinaPlayer.Core.Services;

public static partial class Loc
{
	static partial void RegisterDspCore()
	{
		// EQ & Reverb Presets
		Add("平坦", "平坦", "Flat", "Plat", "Ровный", "Plano", "フラット", "플랫");
		Add("流行", "流行", "Pop", "Pop", "Поп", "Pop", "ポップ", "팝");
		Add("摇滚", "搖滾", "Rock", "Rock", "Рок", "Rock", "ロック", "록");
		Add("爵士", "爵士", "Jazz", "Jazz", "Джаз", "Jazz", "ジャズ", "재즈");
		Add("古典", "古典", "Classical", "Classique", "Классика", "Clásica", "クラシック", "클래식");
		Add("电子", "電子", "Electronic", "Électronique", "Электроника", "Electrónica", "エレクトロニック", "일렉트로닉");
		Add("乡村", "鄉村", "Country", "Country", "Кантри", "Country", "カントリー", "컨트리");
		Add("低音增强", "低音增強", "Bass Boost", "Amplification des basses", "Усиление басов", "Refuerzo de graves", "低音ブースト", "베이스 부스트");
		Add("高音增强", "高音增強", "Treble Boost", "Amplification des aigus", "Усиление высоких", "Refuerzo de agudos", "高音ブースト", "트레블 부스트");
		Add("房间", "房間", "Room", "Pièce", "Комната", "Habitación", "ルーム", "룸");
		Add("大厅", "大廳", "Hall", "Salle", "Зал", "Sala", "ホール", "홀");
		Add("板式", "板式", "Plate", "Plaque", "Пластина", "Placa", "プレート", "플레이트");
		Add("录音室", "錄音室", "Studio", "Studio", "Студия", "Estudio", "スタジオ", "스튜디오");
		Add("浴室", "浴室", "Bathroom", "Salle de bain", "Ванная", "Baño", "バスルーム", "욕실");
		Add("自定义", "自訂", "Custom", "Personnalisé", "Пользовательский", "Personalizado", "カスタム", "사용자 지정");
		Add("AutoEq 校准", "AutoEq 校準", "AutoEq Calibration", "Calibration AutoEq", "Калибровка AutoEq", "Calibración AutoEq", "AutoEq 補正", "AutoEq 보정");

		// DspViewModel Status & Messages
		Add("调节立即生效并随设置保存", "調節立即生效並隨設定儲存", "Adjustments take effect immediately and are saved with settings", "Les réglages prennent effet immédiatement et sont enregistrés", "Изменения применяются сразу и сохраняются в настройках", "Los ajustes se aplican de inmediato y se guardan con la configuración", "調整は即時反映され、設定とともに保存されます", "조정은 즉시 적용되며 설정과 함께 저장됩니다");
		Add("效果播放前先预渲染，渲染完成后开始播放", "效果播放前先預渲染，渲染完成後開始播放", "Pre-render effects before playback; playback starts once complete", "Prérégler les effets avant la lecture ; la lecture démarre une fois terminé", "Предварительный рендеринг эффектов перед воспроизведением", "Pre-renderizar efectos antes de reproducir; la reproducción inicia al finalizar", "再生前にエフェクトをプリレンダリングし、完了後に再生を開始します", "재생 전 효과를 사전 렌더링하며 완료 후 재생을 시작합니다");
		Add("Steinberg VST3 64位原生宿主就绪 · 扫描目录或加载 .vst3 插件即可实时渲染与交互", "Steinberg VST3 64位原生宿主就緒 · 掃描目錄或載入 .vst3 外掛即可即時渲染與互動", "Steinberg VST3 64-bit native host ready · Scan folders or load .vst3 plugins for real-time rendering", "Hôte natif Steinberg VST3 64 bits prêt · Scannez des dossiers ou chargez un plugin .vst3", "Нативный 64-битный хост Steinberg VST3 готов · Сканируйте папки или загрузите .vst3", "Host nativo Steinberg VST3 de 64 bits listo · Escanea carpetas o carga un plugin .vst3", "Steinberg VST3 64bit ネイティブホスト準備完了 · フォルダをスキャンまたは .vst3 を読み込みます", "Steinberg VST3 64비트 네이티브 호스트 준비됨 · 폴더를 스캔하거나 .vst3 플러그인을 로드하세요");
		Add("已添加目录：{0}，点击“扫描”发现插件", "已新增目錄：{0}，點擊「掃描」探索外掛", "Folder added: {0}. Click \"Scan\" to discover plugins", "Dossier ajouté : {0}. Cliquez sur « Scanner » pour découvrir les plugins", "Папка добавлена: {0}. Нажмите «Сканировать» для поиска плагинов", "Carpeta añadida: {0}. Haz clic en «Escanear» para buscar plugins", "フォルダを追加しました: {0}。「スキャン」をクリックしてプラグインを検出します", "폴더 추가됨: {0}. \"스캔\"을 클릭하여 플러그인을 찾으세요");
		Add("已移除目录：{0}", "已移除目錄：{0}", "Folder removed: {0}", "Dossier supprimé : {0}", "Папка удалена: {0}", "Carpeta eliminada: {0}", "フォルダを削除しました: {0}", "폴더 제거됨: {0}");
		Add("扫描完成：发现 {0} 个 VST3 插件", "掃描完成：發現 {0} 個 VST3 外掛", "Scan complete: found {0} VST3 plugin(s)", "Analyse terminée : {0} plugin(s) VST3 trouvé(s)", "Сканирование завершено: найдено плагинов VST3 — {0}", "Escaneo completo: se encontraron {0} plugin(s) VST3", "スキャン完了：{0} 個の VST3 プラグインが見つかりました", "스캔 완료: VST3 플러그인 {0}개 발견됨");
		Add("扫描完成：未在目录中发现 .vst3 插件（可添加自定义目录）", "掃描完成：未在目錄中發現 .vst3 外掛（可新增自訂目錄）", "Scan complete: no .vst3 plugins found (you can add custom folders)", "Analyse terminée : aucun plugin .vst3 trouvé (ajoutez des dossiers personnalisés)", "Сканирование завершено: плагины .vst3 не найдены (можно добавить свои папки)", "Escaneo completo: no se encontraron plugins .vst3 (puedes añadir carpetas)", "スキャン完了：.vst3 プラグインは見つかりませんでした（カスタムフォルダを追加できます）", "스캔 완료: .vst3 플러그인을 찾을 수 없습니다 (사용자 지정 폴더 추가 가능)");
		Add("无效的 VST3 插件路径：{0}", "無效的 VST3 外掛路徑：{0}", "Invalid VST3 plugin path: {0}", "Chemin de plugin VST3 invalide : {0}", "Неверный путь к плагину VST3: {0}", "Ruta de plugin VST3 no válida: {0}", "無効な VST3 プラグインパス: {0}", "잘못된 VST3 플러그인 경로: {0}");
		Add("已选中插件：{0} ({1})", "已選取外掛：{0} ({1})", "Plugin selected: {0} ({1})", "Plugin sélectionné : {0} ({1})", "Выбран плагин: {0} ({1})", "Plugin seleccionado: {0} ({1})", "プラグインを選択しました: {0} ({1})", "플러그인 선택됨: {0} ({1})");
		Add("已加载插件文件：{0} ({1})", "已載入外掛檔案：{0} ({1})", "Plugin file loaded: {0} ({1})", "Fichier plugin chargé : {0} ({1})", "Файл плагина загружен: {0} ({1})", "Archivo de plugin cargado: {0} ({1})", "プラグインファイルを読み込みました: {0} ({1})", "플러그인 파일 로드됨: {0} ({1})");
		Add("已恢复 {0} 个默认系统 VST3 扫描目录", "已恢復 {0} 個預設系統 VST3 掃描目錄", "Restored {0} default system VST3 scan folder(s)", "{0} dossier(s) VST3 système par défaut restauré(s)", "Восстановлено стандартных папок VST3: {0}", "Se restauraron {0} carpeta(s) VST3 predeterminada(s)", "{0} 個のデフォルトシステム VST3 スキャンフォルダを復元しました", "기본 시스템 VST3 스캔 폴더 {0}개 복원됨");
		Add("默认系统 VST3 扫描目录均已存在", "預設系統 VST3 掃描目錄均已存在", "All default system VST3 scan folders already exist", "Tous les dossiers VST3 système par défaut existent déjà", "Все стандартные системные папки VST3 уже добавлены", "Todas las carpetas VST3 predeterminadas ya existen", "デフォルトのシステム VST3 スキャンフォルダはすべて存在します", "기본 시스템 VST3 스캔 폴더가 이미 모두 존재합니다");
		Add("已同步 VST3 启用状态：{0}（实时处理中）", "已同步 VST3 啟用狀態：{0}（即時處理中）", "VST3 active state synced: {0} (processing live)", "État VST3 synchronisé : {0} (traitement en direct)", "Состояние VST3 синхронизировано: {0} (обработка в реальном времени)", "Estado VST3 sincronizado: {0} (procesando en vivo)", "VST3 有効状態を同期しました: {0}（リアルタイム処理中）", "VST3 활성 상태 동기화됨: {0} (실시간 처리 중)");
		Add("已同步 VST3 状态：当前未启用插件（直通）", "已同步 VST3 狀態：目前未啟用外掛（直通）", "VST3 state synced: no active plugin (bypass)", "État VST3 synchronisé : aucun plugin actif (bypass)", "Состояние VST3 синхронизировано: активных плагинов нет (обход)", "Estado VST3 sincronizado: sin plugins activos (bypass)", "VST3 状態を同期しました：現在有効なプラグインはありません（バイパス）", "VST3 상태 동기화됨: 활성 플러그인 없음 (바이패스)");
		Add("正在预渲染效果，完成后自动开始播放…", "正在預渲染效果，完成後自動開始播放…", "Pre-rendering effects; playback will start automatically…", "Préréglement des effets en cours, la lecture démarrera automatiquement…", "Предварительный рендеринг эффектов, воспроизведение начнётся автоматически…", "Pre-renderizando efectos; la reproducción comenzará automáticamente…", "エフェクトをプリレンダリング中、完了後に自動再生します…", "효과를 사전 렌더링 중입니다. 완료 후 자동으로 재생됩니다…");
		Add("预渲染完成，已开始播放", "預渲染完成，已開始播放", "Pre-render complete; playback started", "Préréglement terminé, lecture démarrée", "Предварительный рендеринг завершён, воспроизведение начато", "Pre-renderizado completo, reproducción iniciada", "プリレンダリング完了、再生を開始しました", "사전 렌더링 완료, 재생 시작됨");
		Add("预渲染已取消", "預渲染已取消", "Pre-render canceled", "Préréglement annulé", "Предварительный рендеринг отменён", "Pre-renderizado cancelado", "プリレンダリングがキャンセルされました", "사전 렌더링 취소됨");
		Add("预渲染中… {0:P0}，预计剩余 {1} 秒", "預渲染中… {0:P0}，預計剩餘 {1} 秒", "Pre-rendering… {0:P0}, ~{1} s remaining", "Préréglement… {0:P0}, ~{1} s restantes", "Рендеринг… {0:P0}, осталось ~{1} с", "Pre-renderizando… {0:P0}, ~{1} s restantes", "プリレンダリング中… {0:P0}、残り約 {1} 秒", "사전 렌더링 중… {0:P0}, 약 {1}초 남음");
		Add("已应用预设：{0}", "已套用預設：{0}", "Preset applied: {0}", "Préréglage appliqué : {0}", "Применён пресет: {0}", "Preajuste aplicado: {0}", "プリセットを適用しました: {0}", "프리셋 적용됨: {0}");
		Add("已重置全部 DSP 效果", "已重設全部 DSP 效果", "All DSP effects reset", "Tous les effets DSP réinitialisés", "Все DSP-эффекты сброшены", "Todos los efectos DSP restablecidos", "すべての DSP エフェクトをリセットしました", "모든 DSP 효과가 초기화되었습니다");
		Add("已恢复默认 DSP 设置", "已恢復預設 DSP 設定", "Default DSP settings restored", "Paramètres DSP par défaut restaurés", "Настройки DSP по умолчанию восстановлены", "Configuración DSP predeterminada restaurada", "デフォルトの DSP 設定を復元しました", "기본 DSP 설정이 복원되었습니다");
		Add("无数据", "無資料", "No data", "Aucune donnée", "Нет данных", "Sin datos", "データなし", "데이터 없음");
		Add("单曲", "單曲", "Track", "Piste", "Трек", "Pista", "トラック", "트랙");
		Add("就绪", "就緒", "Ready", "Prêt", "Готово", "Listo", "準備完了", "준비됨");
		Add("已导入 AutoEq 预设：{0} 段滤波，前级增益 {1:F1} dB", "已匯入 AutoEq 預設：{0} 段濾波，前級增益 {1:F1} dB", "AutoEq preset imported: {0} bands, preamp {1:F1} dB", "Préréglage AutoEq importé : {0} bandes, préampli {1:F1} dB", "Пресет AutoEq импортирован: полос — {0}, предусиление {1:F1} дБ", "Preajuste AutoEq importado: {0} bandas, preamplificación {1:F1} dB", "AutoEq プリセットをインポートしました：{0} バンド、プリアンプ {1:F1} dB", "AutoEq 프리셋 가져옴: {0}개 밴드, 프리앰프 {1:F1} dB");
		Add("AutoEq 预设导入失败：{0}", "AutoEq 預設匯入失敗：{0}", "Failed to import AutoEq preset: {0}", "Échec de l'importation AutoEq : {0}", "Ошибка импорта пресета AutoEq: {0}", "Error al importar preajuste AutoEq: {0}", "AutoEq プリセットのインポートに失敗しました: {0}", "AutoEq 프리셋 가져오기 실패: {0}");
		Add("已加载房间校正/耳机脉冲响应：{0}", "已載入房間校正/耳機脈衝響應：{0}", "Loaded room/headphone impulse response: {0}", "Réponse impulsionnelle chargée : {0}", "Загружен импульсный отклик (IR): {0}", "Respuesta al impulso cargada: {0}", "ルーム/ヘッドホン インパルス応答を読み込みました: {0}", "룸/헤드폰 임펄스 응답 로드됨: {0}");
		Add("脉冲响应加载失败，请检查文件格式", "脈衝響應載入失敗，請檢查檔案格式", "Failed to load impulse response; please check the file format", "Échec du chargement de la réponse impulsionnelle ; vérifiez le format", "Не удалось загрузить импульсный отклик; проверьте формат файла", "Error al cargar la respuesta al impulso; comprueba el formato", "インパルス応答の読み込みに失敗しました。ファイル形式を確認してください", "임펄스 응답 로드 실패. 파일 형식을 확인하세요");
		Add("加载脉冲响应出错: {0}", "載入脈衝響應出錯: {0}", "Error loading impulse response: {0}", "Erreur lors du chargement de la RI : {0}", "Ошибка загрузки импульсного отклика: {0}", "Error al cargar respuesta al impulso: {0}", "インパルス応答の読み込みエラー: {0}", "임펄스 응답 로드 오류: {0}");
		Add("已清除脉冲响应", "已清除脈衝響應", "Impulse response cleared", "Réponse impulsionnelle effacée", "Импульсный отклик очищен", "Respuesta al impulso borrada", "インパルス応答をクリアしました", "임펄스 응답 지워짐");
		Add("清除脉冲响应出错: {0}", "清除脈衝響應出錯: {0}", "Error clearing impulse response: {0}", "Erreur lors de l'effacement de la RI : {0}", "Ошибка очистки импульсного отклика: {0}", "Error al borrar respuesta al impulso: {0}", "インパルス応答のクリアエラー: {0}", "임펄스 응답 지우기 오류: {0}");
		Add("已导入 AutoEq 耳机预设 ({0} 个频点，前级 {1:F1} dB)", "已匯入 AutoEq 耳機預設 ({0} 個頻點，前級 {1:F1} dB)", "AutoEq headphone preset imported ({0} bands, preamp {1:F1} dB)", "Préréglage casque AutoEq importé ({0} bandes, préampli {1:F1} dB)", "Импортирован пресет наушников AutoEq ({0} полос, предусил. {1:F1} дБ)", "Preajuste AutoEq de auriculares importado ({0} bandas, preampl. {1:F1} dB)", "AutoEq ヘッドホンプリセットをインポートしました ({0} バンド、プリアンプ {1:F1} dB)", "AutoEq 헤드폰 프리셋 가져옴 ({0}개 밴드, 프리앰프 {1:F1} dB)");
		Add("导入 AutoEq 出错: {0}", "匯入 AutoEq 出錯: {0}", "Error importing AutoEq: {0}", "Erreur d'importation AutoEq : {0}", "Ошибка импорта AutoEq: {0}", "Error al importar AutoEq: {0}", "AutoEq インポートエラー: {0}", "AutoEq 가져오기 오류: {0}");
		Add("耳机偏音补偿", "耳機偏音補償", "Channel Balance Compensation", "Compensation d'équilibre", "Компенсация баланса", "Compensación de balance", "左右バランス補正", "좌우 밸런스 보정");
		Add("未知（模拟宿主）", "未知（模擬宿主）", "Unknown (Simulated Host)", "Inconnu (hôte simulé)", "Неизвестно (эмуляция хоста)", "Desconocido (host simulado)", "不明（シミュレートホスト）", "알 수 없음 (시뮬레이션 호스트)");

		// DspPage Dialogs, Panels & Controls
		Add("调整面板大小", "調整面板大小", "Resize Panel", "Redimensionner le panneau", "Изменить размер панели", "Cambiar tamaño del panel", "パネルのサイズ変更", "패널 크기 조정");
		Add("添加目录失败：{0}", "新增目錄失敗：{0}", "Failed to add folder: {0}", "Échec de l'ajout du dossier : {0}", "Не удалось добавить папку: {0}", "Error al añadir carpeta: {0}", "フォルダの追加に失敗しました: {0}", "폴더 추가 실패: {0}");
		Add("打开插件界面失败：{0}", "開啟外掛介面失敗：{0}", "Failed to open plugin UI: {0}", "Échec de l'ouverture de l'interface du plugin : {0}", "Не удалось открыть интерфейс плагина: {0}", "Error al abrir la interfaz del plugin: {0}", "プラグイン UI を開けませんでした: {0}", "플러그인 UI 열기 실패: {0}");
		Add("已打开插件原生界面：{0}，实时处理与播放当前歌曲", "已開啟外掛原生介面：{0}，即時處理與播放目前歌曲", "Native plugin UI opened: {0}, processing and playing live", "Interface native du plugin ouverte : {0}, traitement en direct", "Открыт нативный интерфейс плагина: {0}, обработка в реальном времени", "Interfaz nativa del plugin abierta: {0}, procesando en tiempo real", "プラグインのネイティブ UI を開きました: {0}（リアルタイム処理中）", "플러그인 기본 UI 열림: {0} (실시간 처리 중)");
		Add("打开插件界面异常：{0}", "開啟外掛介面異常：{0}", "Exception opening plugin UI: {0}", "Erreur lors de l'ouverture de l'interface : {0}", "Ошибка открытия интерфейса плагина: {0}", "Excepción al abrir la interfaz del plugin: {0}", "プラグイン UI の起動中に例外が発生しました: {0}", "플러그인 UI 열기 예외: {0}");
		Add("已关闭《{0}》原生窗口：插件仍在机架后台实时处理音频（可随时重新打开界面或停用）", "已關閉《{0}》原生視窗：外掛仍在機架背景即時處理音訊（可隨時重新開啟介面或停用）", "Closed \"{0}\" native window: plugin continues processing audio in the rack (reopen or disable anytime)", "Fenêtre native « {0} » fermée : le plugin continue le traitement en arrière-plan", "Окно «{0}» закрыто: плагин продолжает обработку звука в фоне", "Ventana nativa de «{0}» cerrada: el plugin sigue procesando audio en segundo plano", "「{0}」のネイティブウィンドウを閉じました：プラグインはバックグラウンドで引き続き音声処理を行います", "\"{0}\" 기본 창 닫힘: 플러그인이 백그라운드에서 계속 오디오를 처리합니다");
		Add("拖拽调整大小", "拖曳調整大小", "Drag to resize", "Glisser pour redimensionner", "Перетащите для изменения размера", "Arrastra para cambiar el tamaño", "ドラッグしてサイズ変更", "드래그하여 크기 조정");

		// Compressor Panel
		Add("启用母带动态压缩器", "啟用母帶動態壓縮器", "Enable Mastering Dynamics Compressor", "Activer le compresseur de mastering", "Включить мастеринговый компрессор", "Activar compresor dinámico de masterización", "マスタリングダイナミクスコンプレッサーを有効化", "마스터링 다이내믹 컴프레서 사용");
		Add("阈值 Threshold (-60 ~ 0 dB)", "閾值 Threshold (-60 ~ 0 dB)", "Threshold (-60 ~ 0 dB)", "Seuil Threshold (-60 ~ 0 dB)", "Порог Threshold (-60 ~ 0 дБ)", "Umbral Threshold (-60 ~ 0 dB)", "スレッショルド Threshold (-60 ~ 0 dB)", "임계값 Threshold (-60 ~ 0 dB)");
		Add("压缩比 Ratio (1:1 ~ 20:1)", "壓縮比 Ratio (1:1 ~ 20:1)", "Ratio (1:1 ~ 20:1)", "Ratio (1:1 ~ 20:1)", "Соотношение Ratio (1:1 ~ 20:1)", "Relación Ratio (1:1 ~ 20:1)", "レシオ Ratio (1:1 ~ 20:1)", "비율 Ratio (1:1 ~ 20:1)");
		Add("起音 Attack (ms)", "起音 Attack (ms)", "Attack (ms)", "Attaque Attack (ms)", "Атака Attack (мс)", "Ataque Attack (ms)", "アタック Attack (ms)", "어택 Attack (ms)");
		Add("释放 Release (ms)", "釋放 Release (ms)", "Release (ms)", "Relâchement Release (ms)", "Восстановление Release (мс)", "Liberación Release (ms)", "リリース Release (ms)", "릴리즈 Release (ms)");
		Add("软拐点 Knee (dB)", "軟拐點 Knee (dB)", "Knee (dB)", "Coude Knee (dB)", "Колено Knee (дБ)", "Codo Knee (dB)", "ニー Knee (dB)", "니 Knee (dB)");
		Add("增益补偿 Makeup (dB)", "增益補償 Makeup (dB)", "Makeup Gain (dB)", "Gain de compensation (dB)", "Компенсация усиления (дБ)", "Ganancia de compensación (dB)", "メイクアップゲイン (dB)", "메이크업 게인 (dB)");
		Add("实时减益 GR:", "即時減益 GR:", "Live GR:", "Réduction GR :", "Снижение GR:", "Reducción GR:", "リアルタイム GR:", "실시간 게인 리덕션 GR:");
		Add("母带动态压缩器 (FurinaPlayer Compressor)", "母帶動態壓縮器 (FurinaPlayer Compressor)", "Mastering Compressor (FurinaPlayer Compressor)", "Compresseur de mastering (FurinaPlayer Compressor)", "Мастеринговый компрессор (FurinaPlayer Compressor)", "Compresor de masterización (FurinaPlayer Compressor)", "マスタリングコンプレッサー (FurinaPlayer Compressor)", "마스터링 컴프레서 (FurinaPlayer Compressor)");

		// Spatial & Crossfeed Panel
		Add("启用双耳互馈 (Crossfeed)", "啟用雙耳互饋 (Crossfeed)", "Enable Binaural Crossfeed", "Activer le Crossfeed binaural", "Включить бинауральный Crossfeed", "Activar Crossfeed binaural", "バイノーラル クロスフィードを有効化", "바이노럴 크로스피드 사용");
		Add("互馈滤波曲线", "互饋濾波曲線", "Crossfeed Filter Curve", "Courbe de filtre Crossfeed", "Кривая фильтра Crossfeed", "Curva de filtro Crossfeed", "クロスフィード フィルターカーブ", "크로스피드 필터 커브");
		Add("Chu Moy (经典自然)", "Chu Moy (經典自然)", "Chu Moy (Classic Natural)", "Chu Moy (Classique naturel)", "Chu Moy (Классический естественный)", "Chu Moy (Clásico natural)", "Chu Moy (クラシック・ナチュラル)", "Chu Moy (클래식 내추럴)");
		Add("Jan Meier (宽深舒适)", "Jan Meier (寬深舒適)", "Jan Meier (Wide & Deep)", "Jan Meier (Large et profond)", "Jan Meier (Широкий и глубокий)", "Jan Meier (Amplio y profundo)", "Jan Meier (ワイド＆ディープ)", "Jan Meier (넓고 깊은 사운드)");
		Add("Linkwitz (温暖声场)", "Linkwitz (溫暖聲場)", "Linkwitz (Warm Stage)", "Linkwitz (Scène chaleureuse)", "Linkwitz (Тёплая сцена)", "Linkwitz (Escena cálida)", "Linkwitz (ウォームステージ)", "Linkwitz (따뜻한 음장)");
		Add("互馈深度 Amount (0% ~ 100%)", "互饋深度 Amount (0% ~ 100%)", "Crossfeed Amount (0% ~ 100%)", "Intensité Crossfeed (0% ~ 100%)", "Глубина Crossfeed (0% ~ 100%)", "Nivel Crossfeed (0% ~ 100%)", "クロスフィード量 Amount (0% ~ 100%)", "크로스피드 양 Amount (0% ~ 100%)");
		Add("低通截止 Cutoff (400 ~ 1000 Hz)", "低通截止 Cutoff (400 ~ 1000 Hz)", "Low-Pass Cutoff (400 ~ 1000 Hz)", "Coupure passe-bas (400 ~ 1000 Hz)", "Срез НЧ Cutoff (400 ~ 1000 Гц)", "Corte paso bajo (400 ~ 1000 Hz)", "ローパスカットオフ (400 ~ 1000 Hz)", "로우패스 컷오프 (400 ~ 1000 Hz)");
		Add("立体声宽度 Stereo Width (0% 单声道 ~ 200% 超宽)", "立體聲寬度 Stereo Width (0% 單聲道 ~ 200% 超寬)", "Stereo Width (0% Mono ~ 200% Ultra-Wide)", "Largeur stéréo (0% Mono ~ 200% Ultra-large)", "Ширина стерео (0% Моно ~ 200% Сверхширокое)", "Amplitud estéreo (0% Mono ~ 200% Ultra-ancho)", "ステレオ幅 (0% モノラル ~ 200% ウルトラワイド)", "스테레오 폭 (0% 모노 ~ 200% 초광대역)");
		Add("声道矩阵模式 (Channel Matrix)", "聲道矩陣模式 (Channel Matrix)", "Channel Matrix Mode", "Mode matrice de canaux (Channel Matrix)", "Режим матрицы каналов (Channel Matrix)", "Modo de matriz de canales (Channel Matrix)", "チャンネルマトリックスモード", "채널 매트릭스 모드");
		Add("正常立体声 (Normal)", "正常立體聲 (Normal)", "Normal Stereo", "Stéréo normale (Normal)", "Обычное стерео (Normal)", "Estéreo normal (Normal)", "通常ステレオ (Normal)", "일반 스테레오 (Normal)");
		Add("左右声道对调 (Swap L/R)", "左右聲道對調 (Swap L/R)", "Swap L/R Channels", "Inverser G/D (Swap L/R)", "Поменять L/R (Swap L/R)", "Intercambiar I/D (Swap L/R)", "左右チャンネル入替 (Swap L/R)", "좌우 채널 바꾸기 (Swap L/R)");
		Add("单声道混合 (Mono Mix)", "單聲道混合 (Mono Mix)", "Mono Mix", "Mixage mono (Mono Mix)", "Моно микс (Mono Mix)", "Mezcla mono (Mono Mix)", "モノラルミックス (Mono Mix)", "모노 믹스 (Mono Mix)");
		Add("反相 (Invert Phase)", "反相 (Invert Phase)", "Invert Phase", "Inverser la phase (Invert Phase)", "Инверсия фазы (Invert Phase)", "Invertir fase (Invert Phase)", "位相反転 (Invert Phase)", "위상 반전 (Invert Phase)");
		Add("空间声场与双耳互馈 (Crossfeed & Spatial)", "空間聲場與雙耳互饋 (Crossfeed & Spatial)", "Spatial & Crossfeed", "Spatialisation et Crossfeed", "Пространство и Crossfeed", "Espacial y Crossfeed", "空間＆クロスフィード", "공간 음장 및 크로스피드");

		// Convolution Panel
		Add("启用 FIR 卷积耳机/房间校正", "啟用 FIR 卷積耳機/房間校正", "Enable FIR Convolution Headphone/Room Correction", "Activer la convolution FIR (casque/pièce)", "Включить FIR-свёртку (коррекция наушников/комнаты)", "Activar convolución FIR (auriculares/sala)", "FIR コンボリューション補正を有効化", "FIR 컨볼루션 헤드폰/룸 보정 사용");
		Add("载入脉冲响应 IR (.wav)", "載入脈衝響應 IR (.wav)", "Load Impulse Response IR (.wav)", "Charger réponse impulsionnelle IR (.wav)", "Загрузить импульсный отклик IR (.wav)", "Cargar respuesta al impulso IR (.wav)", "インパルス応答 IR (.wav) を読み込む", "임펄스 응답 IR (.wav) 불러오기");
		Add("当前未载入脉冲响应文件（支持 44.1k/48k/96k 双声道 IR）", "目前未載入脈衝響應檔案（支援 44.1k/48k/96k 雙聲道 IR）", "No impulse response loaded (supports 44.1k/48k/96k stereo IR)", "Aucune réponse impulsionnelle chargée (IR stéréo 44.1k/48k/96k)", "Импульсный отклик не загружен (поддержка стерео IR 44.1k/48k/96k)", "Sin respuesta al impulso cargada (soporta IR estéreo 44.1k/48k/96k)", "インパルス応答未読み込み（44.1k/48k/96k ステレオ IR 対応）", "로드된 임펄스 응답 없음 (44.1k/48k/96k 스테레오 IR 지원)");
		Add("已载入 IR: {0}", "已載入 IR: {0}", "Loaded IR: {0}", "IR chargée : {0}", "Загружен IR: {0}", "IR cargada: {0}", "読み込み済み IR: {0}", "로드된 IR: {0}");
		Add("输出电平衰减 Trim (-24 ~ +6 dB)", "輸出電平衰減 Trim (-24 ~ +6 dB)", "Output Trim (-24 ~ +6 dB)", "Atténuation de sortie Trim (-24 ~ +6 dB)", "Выходной уровень Trim (-24 ~ +6 дБ)", "Ajuste de salida Trim (-24 ~ +6 dB)", "出力トリム (-24 ~ +6 dB)", "출력 트림 (-24 ~ +6 dB)");
		Add("FIR 卷积引擎支持高精度直接卷积与耳机逆滤波器，实时修正频响曲线与房间混响失真。", "FIR 卷積引擎支援高精度直接卷積與耳機逆濾波器，即時修正頻響曲線與房間混響失真。", "The FIR convolution engine supports high-precision direct convolution and inverse headphone filters to correct frequency response and room acoustics in real time.", "Le moteur de convolution FIR corrige en temps réel la courbe de réponse et l'acoustique de la pièce.", "Движок FIR-свёртки корректирует АЧХ и акустику помещения в реальном времени.", "El motor de convolución FIR corrige en tiempo real la respuesta en frecuencia y la acústica de la sala.", "FIR コンボリューションエンジンは高精度な畳み込みと逆フィルターにより、周波数特性や残響歪みをリアルタイムで補正します。", "FIR 컨볼루션 엔진은 고정밀 직접 컨볼루션과 역필터를 통해 주파수 응답과 룸 음향을 실시간으로 보정합니다.");
		Add("FIR 卷积房间/耳机校正 (Convolution)", "FIR 卷積房間/耳機校正 (Convolution)", "FIR Convolution Correction", "Correction par convolution FIR", "Коррекция FIR-свёрткой", "Corrección por convolución FIR", "FIR コンボリューション補正", "FIR 컨볼루션 보정");

		// Resampler & SDM Panel
		Add("启用实时多相高阶升频重采样 (Resampler)", "啟用即時多相高階升頻重採樣 (Resampler)", "Enable Real-time Polyphase Resampler", "Activer le rééchantillonnage polyphase (Resampler)", "Включить полифазный ресемплер (Resampler)", "Activar remuestreo polifásico (Resampler)", "リアルタイムポリフェーズリサンプラーを有効化", "실시간 다상 업샘플링 리샘플러 사용");
		Add("目标采样率 Target SampleRate", "目標取樣率 Target SampleRate", "Target Sample Rate", "Fréquence d'échantillonnage cible", "Целевая частота дискретизации", "Frecuencia de muestreo objetivo", "ターゲットサンプルレート", "대상 샘플레이트");
		Add("自动保持原始 (Auto)", "自動保持原始 (Auto)", "Auto (Keep Original)", "Auto (Conserver l'original)", "Авто (исходная)", "Auto (Mantener original)", "自動（元のまま）", "자동 (원본 유지)");
		Add("96.0 kHz (高解析)", "96.0 kHz (高解析)", "96.0 kHz (Hi-Res)", "96.0 kHz (Haute résolution)", "96.0 кГц (Hi-Res)", "96.0 kHz (Alta resolución)", "96.0 kHz (ハイレゾ)", "96.0 kHz (고해상도)");
		Add("192.0 kHz (母带级)", "192.0 kHz (母帶級)", "192.0 kHz (Mastering)", "192.0 kHz (Qualité mastering)", "192.0 кГц (Мастеринг)", "192.0 kHz (Calidad máster)", "192.0 kHz (マスタリング)", "192.0 kHz (마스터링급)");
		Add("384.0 kHz (极限高保真)", "384.0 kHz (極限高傳真)", "384.0 kHz (Ultra Hi-Fi)", "384.0 kHz (Ultra Hi-Fi)", "384.0 кГц (Ультра Hi-Fi)", "384.0 kHz (Ultra Hi-Fi)", "384.0 kHz (ウルトラ Hi-Fi)", "384.0 kHz (울트라 Hi-Fi)");
		Add("启用 PCM 转 DSD 调制器 (SDM Modulator)", "啟用 PCM 轉 DSD 調變器 (SDM Modulator)", "Enable PCM to DSD Modulator (SDM)", "Activer le modulateur PCM vers DSD (SDM)", "Включить модулятор PCM в DSD (SDM)", "Activar modulador PCM a DSD (SDM)", "PCM → DSD モジュレーター (SDM) を有効化", "PCM-to-DSD 모듈레이터 (SDM) 사용");
		Add("DSD 阶数模式", "DSD 階數模式", "DSD Rate Mode", "Mode de débit DSD", "Режим DSD", "Modo de tasa DSD", "DSD レートモード", "DSD 레이트 모드");
		Add("5阶五极点噪声整形滤波器持续运行，将量化噪声推向 50kHz 以上超声频段，保留纯净模拟质感。", "5階五極點雜訊整形濾波器持續運行，將量化雜訊推向 50kHz 以上超音波頻段，保留純淨類比質感。", "5th-order 5-pole noise-shaping filter pushes quantization noise above 50 kHz for pure analog warmth.", "Le filtre de mise en forme du bruit d'ordre 5 repousse le bruit au-delà de 50 kHz pour un rendu analogique pur.", "Фильтр формирования шума 5-го порядка сдвигает шум квантования выше 50 кГц, сохраняя чистое аналоговое звучание.", "El filtro de conformación de ruido de 5.º orden empuja el ruido por encima de 50 kHz para conservar un sonido analógico puro.", "5次5極ノイズシェーピングフィルターが量子化ノイズを50kHz以上の超音波帯域へ押し出し、純粋なアナログ質感を保ちます。", "5차 5극 노이즈 셰이핑 필터가 양자화 노이즈를 50kHz 이상 초음파 대역으로 밀어내어 순수한 아날로그 질감을 유지합니다.");
		Add("实时升频与 PCM 转 DSD (SDM Modulator)", "即時升頻與 PCM 轉 DSD (SDM Modulator)", "Resampler & PCM-to-DSD (SDM Modulator)", "Rééchantillonnage & PCM vers DSD (SDM)", "Ресемплинг и PCM в DSD (SDM)", "Remuestreo y PCM a DSD (SDM)", "リサンプラー＆PCM-DSD 変換 (SDM)", "리샘플링 및 PCM-DSD 변환 (SDM)");

		// ReplayGain & Headroom Legacy Panel
		Add("ReplayGain 响度均衡模式", "ReplayGain 響度均衡模式", "ReplayGain Loudness Mode", "Mode ReplayGain", "Режим громкости ReplayGain", "Modo de volumen ReplayGain", "ReplayGain ラウドネスモード", "ReplayGain 음량 균등화 모드");
		Add("关闭 (Off)", "關閉 (Off)", "Off", "Désactivé (Off)", "Выкл (Off)", "Desactivado (Off)", "オフ (Off)", "끄기 (Off)");
		Add("按曲目 (Track Gain)", "按曲目 (Track Gain)", "Track Gain", "Par piste (Track Gain)", "По треку (Track Gain)", "Por pista (Track Gain)", "トラック別 (Track Gain)", "트랙별 (Track Gain)");
		Add("按专辑 (Album Gain)", "按專輯 (Album Gain)", "Album Gain", "Par album (Album Gain)", "По альбому (Album Gain)", "Por álbum (Album Gain)", "アルバム別 (Album Gain)", "앨범별 (Album Gain)");
		Add("PreAmp 前置增益 (-12 ~ +12 dB)", "PreAmp 前置增益 (-12 ~ +12 dB)", "PreAmp Gain (-12 ~ +12 dB)", "Gain PreAmp (-12 ~ +12 dB)", "Предусиление PreAmp (-12 ~ +12 дБ)", "Ganancia PreAmp (-12 ~ +12 dB)", "PreAmp ゲイン (-12 ~ +12 dB)", "PreAmp 게인 (-12 ~ +12 dB)");
		Add("削波溢出防护 (Prevent Clipping)", "削波溢位防護 (Prevent Clipping)", "Prevent Clipping", "Protection contre l'écrêtage", "Защита от клиппинга", "Prevenir recorte (Prevent Clipping)", "クリッピング防止 (Prevent Clipping)", "클리핑 방지 (Prevent Clipping)");
		Add("数字余量衰减 Headroom (0 ~ -12 dB)", "數位餘量衰減 Headroom (0 ~ -12 dB)", "Digital Headroom Attenuation (0 ~ -12 dB)", "Atténuation Headroom numérique (0 ~ -12 dB)", "Запас Headroom (0 ~ -12 дБ)", "Atenuación Headroom digital (0 ~ -12 dB)", "デジタルヘッドルーム減衰 (0 ~ -12 dB)", "디지털 헤드룸 감쇠 (0 ~ -12 dB)");
		Add("在进入 DSP 链路前留出充足计算余量，配合峰值削波限制器杜绝数字破音。", "在進入 DSP 鏈路前留出充足計算餘量，配合峰值削波限制器杜絕數位破音。", "Reserves sufficient headroom before the DSP chain and works with the peak limiter to prevent digital clipping.", "Réserve une marge suffisante avant la chaîne DSP pour éviter tout écrêtage numérique.", "Оставляет достаточный запас перед цепью DSP и предотвращает цифровой клиппинг.", "Reserva margen suficiente antes de la cadena DSP para evitar el recorte digital.", "DSP チェーンに入る前に十分なヘッドルームを確保し、ピークリミッターと連携してデジタルクリッピングを防ぎます。", "DSP 체인 진입 전 충분한 헤드룸을 확보하고 피크 리미터와 함께 디지털 클리핑을 방지합니다.");
		Add("响度均衡与数字余量 (ReplayGain & Headroom)", "響度均衡與數位餘量 (ReplayGain & Headroom)", "ReplayGain & Headroom", "ReplayGain & Headroom", "ReplayGain и Headroom", "ReplayGain y Headroom", "ReplayGain＆ヘッドルーム", "ReplayGain 및 헤드룸");

		// Rack Order Panel
		Add("余量管理 (Headroom)", "餘量管理 (Headroom)", "Headroom Management", "Gestion du Headroom", "Управление Headroom", "Gestión de Headroom", "ヘッドルーム管理", "헤드룸 관리");
		Add("数字余量下压防削波", "數位餘量下壓防削波", "Digital headroom attenuation to prevent clipping", "Atténuation numérique anti-écrêtage", "Цифровое ослабление для защиты от клиппинга", "Atenuación digital para evitar recorte", "クリッピング防止のためのデジタル減衰", "클리핑 방지를 위한 디지털 헤드룸 감쇠");
		Add("响度均衡 (ReplayGain)", "響度均衡 (ReplayGain)", "ReplayGain", "ReplayGain", "ReplayGain", "ReplayGain", "ReplayGain", "ReplayGain");
		Add("曲目/专辑感知响度自动对齐", "曲目/專輯感知響度自動對齊", "Automatic track/album loudness normalization", "Normalisation automatique par piste/album", "Автовыравнивание громкости по треку/альбому", "Nivelación automática por pista o álbum", "トラック/アルバム単位の自動ラウドネス正規化", "트랙/앨범 단위 자동 음량 정규화");
		Add("10段图形EQ & AutoEq", "10段圖形EQ & AutoEq", "10-Band EQ & AutoEq", "Égaliseur 10 bandes & AutoEq", "10-полосный эквалайзер и AutoEq", "EQ de 10 bandas y AutoEq", "10バンド EQ & AutoEq", "10밴드 EQ & AutoEq");
		Add("多段高精度参数化均衡校准", "多段高精度參數化均衡校準", "Multi-band high-precision parametric equalization", "Égalisation paramétrique haute précision", "Многополосная точная параметрическая эквализация", "Ecualización paramétrica de alta precisión", "マルチバンド高精度パラメトリック補正", "다중 대역 고정밀 파라메트릭 이퀄라이제이션");
		Add("母带动态压缩器 (Compressor)", "母帶動態壓縮器 (Compressor)", "Mastering Compressor", "Compresseur de mastering", "Мастеринговый компрессор", "Compresor de masterización", "マスタリングコンプレッサー", "마스터링 컴프레서");
		Add("瞬态与动态范围塑造", "瞬態與動態範圍塑造", "Transient and dynamic range shaping", "Façonnage des transitoires et de la dynamique", "Формирование транзиентов и динамического диапазона", "Modelado de transitorios y rango dinámico", "トランジェントとダイナミックレンジの形成", "트랜지언트 및 다이내믹 레인지 셰이핑");
		Add("双耳互馈 (Crossfeed)", "雙耳互饋 (Crossfeed)", "Binaural Crossfeed", "Crossfeed binaural", "Бинауральный Crossfeed", "Crossfeed binaural", "バイノーラル クロスフィード", "바이노럴 크로스피드");
		Add("消除耳机听觉疲劳与头中效应", "消除耳機聽覺疲勞與頭中效應", "Reduces headphone listening fatigue and in-head localization", "Réduit la fatigue auditive au casque", "Снижает утомляемость при прослушивании в наушниках", "Reduce la fatiga auditiva con auriculares", "ヘッドホンの聴き疲れと頭内定位を軽減", "헤드폰 청취 피로 및 두내 정위감 완화");
		Add("声场宽度与声道矩阵", "聲場寬度與聲道矩陣", "Stereo Width & Channel Matrix", "Largeur stéréo & Matrice de canaux", "Ширина стерео и матрица каналов", "Amplitud estéreo y matriz de canales", "ステレオ幅＆チャンネルマトリックス", "스테레오 폭 및 채널 매트릭스");
		Add("0~200% 声场宽度与极性控制", "0~200% 聲場寬度與極性控制", "0–200% stereo width and polarity control", "Contrôle de largeur 0–200% et polarité", "Управление шириной 0–200% и полярностью", "Control de amplitud 0–200% y polaridad", "0〜200% のステレオ幅と極性コントロール", "0~200% 스테레오 폭 및 극성 제어");
		Add("FIR 卷积房间校正", "FIR 卷積房間校正", "FIR Convolution Room Correction", "Correction de pièce par convolution FIR", "FIR-коррекция акустики помещения", "Corrección de sala por convolución FIR", "FIR コンボリューション・ルーム補正", "FIR 컨볼루션 룸 보정");
		Add("脉冲响应逆滤波器修正", "脈衝響應逆濾波器修正", "Impulse response inverse filter correction", "Correction par filtre inverse de RI", "Коррекция обратным фильтром импульсного отклика", "Corrección por filtro inverso de respuesta al impulso", "インパルス応答逆フィルター補正", "임펄스 응답 역필터 보정");
		Add("多相升频重采样 (Resampler)", "多相升頻重採樣 (Resampler)", "Polyphase Resampler", "Rééchantillonneur polyphase", "Полифазный ресемплер", "Remuestreador polifásico", "ポリフェーズリサンプラー", "다상 리샘플러");
		Add("最高 384kHz 高解析升频", "最高 384kHz 高解析升頻", "Hi-Res upsampling up to 384 kHz", "Suréchantillonnage Hi-Res jusqu'à 384 kHz", "Hi-Res апсемплинг до 384 кГц", "Sobremuestreo Hi-Res hasta 384 kHz", "最大 384kHz ハイレゾアップサンプリング", "최대 384kHz 고해상도 업샘플링");
		Add("PCM 转 DSD (SDM 调制器)", "PCM 轉 DSD (SDM 調變器)", "PCM to DSD (SDM Modulator)", "PCM vers DSD (Modulateur SDM)", "PCM в DSD (SDM-модулятор)", "PCM a DSD (Modulador SDM)", "PCM → DSD (SDM モジュレーター)", "PCM-to-DSD (SDM 모듈레이터)");
		Add("DSD64/128/256 高阶噪声整形", "DSD64/128/256 高階雜訊整形", "DSD64/128/256 high-order noise shaping", "Mise en forme du bruit DSD64/128/256", "Формирование шума DSD64/128/256", "Conformación de ruido DSD64/128/256", "DSD64/128/256 高次ノイズシェーピング", "DSD64/128/256 고차 노이즈 셰이핑");
		Add("VST3 插件宿主", "VST3 外掛宿主", "VST3 Plugin Host", "Hôte de plugins VST3", "Хост плагинов VST3", "Host de plugins VST3", "VST3 プラグインホスト", "VST3 플러그인 호스트");
		Add("外接第三方专业音频效果器", "外接第三方專業音訊效果器", "Host third-party professional audio effects", "Héberger des effets audio tiers professionnels", "Подключение сторонних профессиональных эффектов", "Aloja efectos de audio profesionales de terceros", "サードパーティ製プロオーディオエフェクトを接続", "타사 전문 오디오 이펙터 연결");
		Add("处理链路活动中", "處理鏈路活動中", "Processing chain active", "Chaîne de traitement active", "Цепь обработки активна", "Cadena de procesamiento activa", "処理チェーン稼働中", "처리 체인 활성 중");
		Add("全链路 DSP Rack 机架编排 (DSP Order & Bypass)", "全鏈路 DSP Rack 機架編排 (DSP Order & Bypass)", "Full DSP Rack Order & Bypass", "Ordre du rack DSP & Bypass", "Порядок цепи DSP Rack и обход", "Orden del rack DSP y Bypass", "DSP ラック順序＆バイパス", "전체 DSP 랙 순서 및 바이패스");
		Add("导入 AutoEq 耳机预设", "匯入 AutoEq 耳機預設", "Import AutoEq Headphone Preset", "Importer un préréglage casque AutoEq", "Импорт пресета наушников AutoEq", "Importar preajuste AutoEq de auriculares", "AutoEq ヘッドホンプリセットをインポート", "AutoEq 헤드폰 프리셋 가져오기");

		// Sidebar Categories & Items
		Add("信号控制", "訊號控制", "SIGNAL CONTROL", "CONTRÔLE SIGNAL", "УПРАВЛЕНИЕ СИГНАЛОМ", "CONTROL DE SEÑAL", "信号コントロール", "신호 제어");
		Add("听音方案", "聽音方案", "Listening Plan", "Profil d'écoute", "Профиль прослушивания", "Perfil de escucha", "リスニングプラン", "청취 플랜");
		Add("电平", "電平", "LEVEL", "NIVEAU", "УРОВЕНЬ", "NIVEL", "レベル", "레벨");
		Add("音量均衡", "音量均衡", "Volume Leveling", "Égalisation du volume", "Выравнивание громкости", "Nivelación de volumen", "音量レベリング", "음량 평준화");
		Add("已旁路", "已旁路", "Bypassed", "Contourné", "В обходе", "En bypass", "バイパス中", "바이패스됨");
		Add("采样转换", "取樣轉換", "CONVERSION", "CONVERSION", "ПРЕОБРАЗОВАНИЕ", "CONVERSIÓN", "サンプル変換", "샘플 변환");
		Add("升频", "升頻", "Resampler", "Suréchantillonnage", "Ресемплер", "Remuestreo", "アップサンプリング", "업샘플링");
		Add("DSD 转换 测试版", "DSD 轉換 測試版", "DSD Converter Beta", "Convertisseur DSD Bêta", "Конвертер DSD (Бета)", "Conversor DSD Beta", "DSD 変換 Beta", "DSD 변환 베타");
		Add("音色", "音色", "TONE", "TIMBRE", "ТЕМБР", "TIMBRE", "音色", "음색");
		Add("参数 EQ", "參數 EQ", "Parametric EQ", "Égaliseur paramétrique", "Параметрический EQ", "EQ paramétrico", "パラメトリック EQ", "파라메트릭 EQ");
		Add("已启用 · 10频段", "已啟用 · 10頻段", "Active · 10-Band", "Actif · 10 bandes", "Активен · 10 полос", "Activo · 10 bandas", "有効 · 10バンド", "활성 · 10밴드");
		Add("耳机校正", "耳機校正", "Headphone EQ", "Correction casque", "Коррекция наушников", "Corrección auriculares", "ヘッドホン補正", "헤드폰 보정");
		Add("插件宿主", "外掛宿主", "PLUGIN HOST", "HÔTE PLUGINS", "ПЛАГИНЫ", "PLUGINS", "プラグインホスト", "플러그인 호스트");
		Add("VST3 机架", "VST3 機架", "VST3 Rack", "Rack VST3", "Стойка VST3", "Rack VST3", "VST3 ラック", "VST3 랙");
		Add("原生插件宿主", "原生外掛宿主", "Native Plugin Host", "Hôte de plugins natif", "Нативный хост плагинов", "Host nativo de plugins", "ネイティブプラグインホスト", "네이티브 플러그인 호스트");
		Add("空间与输出", "空間與輸出", "SPACE & OUTPUT", "ESPACE & SORTIE", "ПРОСТРАНСТВО И ВЫХОД", "ESPACIO Y SALIDA", "空間と出力", "공간 및 출력");
		Add("房间校正", "房間校正", "Room Correction", "Correction de pièce", "Коррекция комнаты", "Corrección de sala", "ルーム補正", "룸 보정");
		Add("无 IR", "無 IR", "No IR", "Aucune IR", "Нет IR", "Sin IR", "IR なし", "IR 없음");
		Add("已启用", "已啟用", "Enabled", "Activé", "Включено", "Activado", "有効", "활성화됨");
		Add("已载入", "已載入", "Loaded", "Chargé", "Загружен", "Cargado", "読み込み済み", "로드됨");
		Add("声道工具", "聲道工具", "Channel Tools", "Outils de canal", "Инструменты каналов", "Herramientas de canal", "チャンネルツール", "채널 도구");
		Add("立体声直通", "立體聲直通", "Stereo Direct", "Stéréo directe", "Стерео прямое", "Estéreo directo", "ステレオダイレクト", "스테레오 다이렉트");
		Add("输出安全", "輸出安全", "Output Safety", "Sécurité de sortie", "Безопасность выхода", "Seguridad de salida", "出力セーフティ", "출력 안전");
		Add("待命", "待命", "Standby", "En veille", "Ожидание", "En espera", "スタンバイ", "대기");
		Add("高级设置", "進階設定", "ADVANCED", "AVANCÉ", "ДОПОЛНИТЕЛЬНО", "AVANZADO", "詳細設定", "고급 설정");
		Add("压缩器", "壓縮器", "Compressor", "Compresseur", "Компрессор", "Compresor", "コンプレッサー", "컴프레서");
		Add("三频段压缩", "三頻段壓縮", "3-Band Comp", "Comp. 3 bandes", "3-полосн. компр.", "Compresor 3 bandas", "3バンドコンプ", "3밴드 컴프레서");
		Add("去齿音", "去齒音", "De-Esser", "Dé-esseur", "Де-эссер", "De-Esser", "ディエッサー", "디에서");
		Add("限幅", "限幅", "Limiter", "Limiteur", "Лимитер", "Limitador", "リミッター", "리미터");
		Add("自动上限", "自動上限", "Auto Ceiling", "Plafond auto", "Автопорог", "Límite automático", "自動上限", "자동 상한");
		Add("交叉馈送", "交叉饋送", "Crossfeed", "Crossfeed", "Crossfeed", "Crossfeed", "クロスフィード", "크로스피드");
		Add("虚拟声场", "虛擬聲場", "Virtual Stage", "Scène virtuelle", "Виртуальная сцена", "Escena virtual", "バーチャルステージ", "가상 음장");
		Add("立体声场", "立體聲場", "Stereo Field", "Champ stéréo", "Стереополе", "Campo estéreo", "ステレオフィールド", "스테레오 필드");
		Add("声道矩阵", "聲道矩陣", "Channel Matrix", "Matrice de canaux", "Матрица каналов", "Matriz de canales", "チャンネルマトリックス", "채널 매트릭스");
		Add("系统输出", "系統輸出", "System Output", "Sortie système", "Системный выход", "Salida del sistema", "システム出力", "시스템 출력");
		Add("自动 · 共享模式", "自動 · 共用模式", "Auto · Shared Mode", "Auto · Mode partagé", "Авто · Общий режим", "Auto · Modo compartido", "自動 · 共有モード", "자동 · 공유 모드");

		// Headroom Workbench
		Add("增益与峰值安全", "增益與峰值安全", "Gain & Peak Safety", "Gain et sécurité crête", "Усиление и защита пиков", "Ganancia y seguridad de picos", "ゲインとピークセーフティ", "게인 및 피크 안전");
		Add("等待当前宿主读数", "等待目前宿主讀數", "Waiting for host readings", "En attente des lectures hôte", "Ожидание показаний хоста", "Esperando lecturas del host", "ホストの読み取り待機中", "호스트 측정값 대기 중");
		Add("配置风险估计: 未报告", "配置風險估計: 未報告", "Config risk estimate: Not reported", "Risque config : Non signalé", "Оценка риска конфигурации: нет данных", "Riesgo de config.: Sin informe", "設定リスク推定: 未報告", "구성 위험 추정: 보고되지 않음");
		Add("实测峰值超限: 未知", "實測峰值超限: 未知", "Measured peak clipping: Unknown", "Dépassement crête mesuré : Inconnu", "Превышение пика: неизвестно", "Exceso de pico medido: Desconocido", "実測ピーク超過: 不明", "실측 피크 초과: 알 수 없음");
		Add("当前限幅: 未知", "目前限幅: 未知", "Current limiting: Unknown", "Limitation actuelle : Inconnue", "Текущее ограничение: неизвестно", "Limitación actual: Desconocida", "現在のリミット: 不明", "현재 리미팅: 알 수 없음");
		Add("限幅前", "限幅前", "Pre-Limiter", "Pré-limiteur", "До лимитера", "Pre-limitador", "リミッター前", "리미터 전");
		Add("未知", "未知", "Unknown", "Inconnu", "Неизвестно", "Desconocido", "不明", "알 수 없음");
		Add("DSP 后", "DSP 後", "Post-DSP", "Post-DSP", "После DSP", "Post-DSP", "DSP 後", "DSP 후");
		Add("Limiter 衰减", "Limiter 衰減", "Limiter GR", "Atténuation Limiter", "Спад Limiter", "Atenuación Limiter", "Limiter 減衰", "Limiter 감쇠");
		Add("历史输出超限次数", "歷史輸出超限次數", "Historical Overs", "Dépassements historiques", "История превышений", "Excesos históricos", "過去の出力超過回数", "누적 출력 초과 횟수");
		Add("限幅前超过 0 dBTP 不等于输出已被削波。历史次数不代表当前仍在削波；读数只需覆盖所报告的阶段。", "限幅前超過 0 dBTP 不等於輸出已被削波。歷史次數不代表目前仍在削波；讀數只需覆蓋所報告的階段。", "Exceeding 0 dBTP before the limiter does not mean the output is clipped. Historical counts do not indicate ongoing clipping.", "Dépasser 0 dBTP avant le limiteur ne signifie pas que la sortie est écrêtée.", "Превышение 0 dBTP до лимитера не означает клиппинг на выходе. История не означает текущий клиппинг.", "Superar 0 dBTP antes del limitador no implica recorte en la salida. El historial no indica recorte actual.", "リミッター前で 0 dBTP を超えても出力がクリップしたとは限りません。履歴回数は現在進行中を意味しません。", "리미터 전 0 dBTP 초과는 출력 클리핑을 의미하지 않습니다. 누적 횟수는 현재 클리핑 중임을 뜻하지 않습니다.");
		Add("▶ 处理位置与可见边界", "▶ 處理位置與可見邊界", "▶ Processing Position & Boundaries", "▶ Position de traitement et limites", "▶ Позиция обработки и границы", "▶ Posición de procesamiento y límites", "▶ 処理位置と可視境界", "▶ 처리 위치 및 가시 경계");
		Add("▼ 处理位置与可见边界", "▼ 處理位置與可見邊界", "▼ Processing Position & Boundaries", "▼ Position de traitement et limites", "▼ Позиция обработки и границы", "▼ Posición de procesamiento y límites", "▼ 処理位置と可視境界", "▼ 처리 위치 및 가시 경계");
		Add("在 32 位浮点架构下，DSP 信号链内部可保留大于 0 dBFS 的电平余量，不会发生数字硬削波截断。Headroom 控制位于主输出级，可在进入声卡 DAC 之前预留安全的采样间真实峰值 (True Peak) 余量，结合限幅器确保无失真高保真还原。", "在 32 位元浮點架構下，DSP 訊號鏈內部可保留大於 0 dBFS 的電平餘量，不會發生數位硬削波截斷。Headroom 控制位於主輸出級，可在進入音效卡 DAC 之前預留安全的取樣間真實峰值 (True Peak) 餘量，結合限幅器確保無失真高傳真還原。", "Under 32-bit floating-point processing, the internal DSP chain preserves headroom above 0 dBFS without hard clipping. Headroom control sits at the master output stage to reserve True Peak margin before the DAC, working with the limiter for distortion-free playback.", "En 32 bits virgule flottante, la chaîne DSP conserve une marge au-dessus de 0 dBFS sans écrêtage. Le contrôle Headroom réserve une marge True Peak avant le DAC.", "В 32-битной архитектуре с плавающей точкой цепь DSP сохраняет запас выше 0 dBFS без жёсткого клиппинга. Headroom на выходе оставляет запас True Peak перед ЦАП.", "En 32 bits en coma flotante, la cadena DSP conserva margen por encima de 0 dBFS sin recorte duro. Headroom reserva margen True Peak antes del DAC.", "32bit 浮動小数点処理により DSP 内部では 0 dBFS を超えるヘッドルームを保持できます。Headroom 制御はマスター出力段で DAC 前の True Peak 余裕を確保し、リミッターと合わせて歪みのない再生を実現します。", "32비트 부동소수점 아키텍처에서는 DSP 체인 내부에 0 dBFS 이상의 헤드룸을 유지해 하드 클리핑이 발생하지 않습니다. Headroom 제어는 마스터 출력단에서 DAC 이전 True Peak 여유를 확보하여 왜곡 없는 재생을 보장합니다.");
		Add("动态等响补偿", "動態等響補償", "Dynamic Loudness Compensation", "Compensation de sonie dynamique", "Динамическая тонкомпенсация", "Compensación dinámica de sonoridad", "ダイナミックラウドネス補正", "다이내믹 라우드니스 보정");
		Add("增益", "增益", "Gain", "Gain", "Усиление", "Ganancia", "ゲイン", "게인");
		Add("减少 Headroom 增益 0.5 dB", "減少 Headroom 增益 0.5 dB", "Decrease Headroom gain by 0.5 dB", "Diminuer le gain Headroom de 0,5 dB", "Уменьшить усиление Headroom на 0,5 дБ", "Reducir ganancia Headroom en 0,5 dB", "Headroom ゲインを 0.5 dB 下げる", "Headroom 게인 0.5 dB 감소");
		Add("步进减少 0.5 dB (下限 -12 dB)", "步進減少 0.5 dB (下限 -12 dB)", "Step down 0.5 dB (min -12 dB)", "Diminuer de 0,5 dB (min -12 dB)", "Шаг -0,5 дБ (мин. -12 дБ)", "Bajar 0,5 dB (mín. -12 dB)", "0.5 dB 下げる（下限 -12 dB）", "0.5 dB 감소 (최소 -12 dB)");
		Add("Headroom 增益控制滑块", "Headroom 增益控制滑桿", "Headroom gain slider", "Curseur de gain Headroom", "Ползунок усиления Headroom", "Deslizador de ganancia Headroom", "Headroom ゲインスライダー", "Headroom 게인 슬라이더");
		Add("调节输出 Headroom 增益 (-12 dB ~ +12 dB)", "調節輸出 Headroom 增益 (-12 dB ~ +12 dB)", "Adjust output Headroom gain (-12 dB ~ +12 dB)", "Ajuster le gain Headroom de sortie (-12 dB ~ +12 dB)", "Настройка выходного усиления Headroom (-12 ~ +12 дБ)", "Ajustar ganancia Headroom de salida (-12 dB ~ +12 dB)", "出力 Headroom ゲインを調整 (-12 dB ~ +12 dB)", "출력 Headroom 게인 조절 (-12 dB ~ +12 dB)");
		Add("增加 Headroom 增益 0.5 dB", "增加 Headroom 增益 0.5 dB", "Increase Headroom gain by 0.5 dB", "Augmenter le gain Headroom de 0,5 dB", "Увеличить усиление Headroom на 0,5 дБ", "Aumentar ganancia Headroom en 0,5 dB", "Headroom ゲインを 0.5 dB 上げる", "Headroom 게인 0.5 dB 증가");
		Add("步进增加 0.5 dB (上限 +12 dB)", "步進增加 0.5 dB (上限 +12 dB)", "Step up 0.5 dB (max +12 dB)", "Augmenter de 0,5 dB (max +12 dB)", "Шаг +0,5 дБ (макс. +12 дБ)", "Subir 0,5 dB (máx. +12 dB)", "0.5 dB 上げる（上限 +12 dB）", "0.5 dB 증가 (최대 +12 dB)");
		Add("快速应用 {0} {1} dB", "快速套用 {0} {1} dB", "Quick apply {0} {1} dB", "Appliquer rapidement {0} {1} dB", "Быстро применить {0} {1} дБ", "Aplicar rápido {0} {1} dB", "{0} {1} dB をクイック適用", "{0} {1} dB 빠른 적용");
		Add("快速切换至 {0} {1} dB", "快速切換至 {0} {1} dB", "Quick switch to {0} {1} dB", "Basculer vers {0} {1} dB", "Переключить на {0} {1} дБ", "Cambiar a {0} {1} dB", "{0} {1} dB に切り替え", "{0} {1} dB(으)로 전환");
		Add("直通", "直通", "Direct", "Direct", "Прямой", "Directo", "ダイレクト", "다이렉트");
		Add("日常", "日常", "Daily", "Quotidien", "Повседневный", "Diario", "デイリー", "일상");
		Add("按住听直通", "按住聽直通", "Hold to Bypass", "Maintenir pour contourner", "Удерж. для обхода", "Mantener para bypass", "長押しでバイパス試聴", "길게 눌러 바이패스 청취");
		Add("按住临时直通试听", "按住臨時直通試聽", "Hold for temporary bypass audition", "Maintenir pour écoute directe temporaire", "Удерживайте для временного обхода", "Mantener para audición directa temporal", "長押しで一時バイパス試聴", "길게 눌러 임시 바이패스 청취");
		Add("按住此按钮可临时旁路 Headroom 增益进行原声 A/B 试听，松开恢复预设", "按住此按鈕可臨時旁路 Headroom 增益進行原聲 A/B 試聽，鬆開恢復預設", "Hold to temporarily bypass Headroom gain for A/B audition; release to restore", "Maintenez pour contourner temporairement le gain Headroom (test A/B)", "Удерживайте для временного обхода Headroom и сравнения A/B", "Mantén pulsado para omitir temporalmente Headroom (prueba A/B)", "押している間 Headroom ゲインを一時バイパスして A/B 試聴、離すと元に戻ります", "누르고 있는 동안 Headroom 게인을 일시 바이패스하여 A/B 비교 청취, 놓으면 복원됩니다");
		Add("×1.000  100.0% amplitude (直通试听中)", "×1.000  100.0% amplitude (直通試听中)", "×1.000  100.0% amplitude (Bypass Audition)", "×1.000  100.0% amplitude (Écoute directe)", "×1.000  100.0% amplitude (Прослушивание обхода)", "×1.000  100.0% amplitude (Audición directa)", "×1.000  100.0% amplitude (バイパス試聴中)", "×1.000  100.0% amplitude (바이패스 청취 중)");
		Add("向右提高增益会直接加大音量。还没有峰值读数，安全范围先按不超过 0 dB。", "向右提高增益會直接加大音量。還沒有峰值讀數，安全範圍先按不超過 0 dB。", "Increasing gain to the right raises volume directly. Without peak readings yet, keep within 0 dB for safety.", "Augmenter le gain augmente le volume. Sans mesure de crête, restez sous 0 dB par sécurité.", "Увеличение усиления напрямую повышает громкость. Пока нет показаний пика, держите не выше 0 дБ.", "Subir la ganancia aumenta el volumen directamente. Sin lecturas de pico, mantén ≤ 0 dB por seguridad.", "右へゲインを上げると音量が直接大きくなります。ピーク値が未取得のため、安全のため 0 dB 以下を目安にしてください。", "오른쪽으로 게인을 높이면 볼륨이 직접 커집니다. 피크 측정값이 없으므로 안전을 위해 0 dB 이하로 유지하세요.");
		Add("实时余量", "即時餘量", "Live Margin", "Marge en direct", "Запас в реальном времени", "Margen en vivo", "リアルタイム余裕", "실시간 여유");
		Add("建议 · 安全", "建議 · 安全", "Advice · Safe", "Conseil · Sûr", "Совет · Безопасно", "Consejo · Seguro", "推奨 · 安全", "권장 · 안전");
		Add("保持待命", "保持待命", "Standby", "En veille", "В режиме ожидания", "En espera", "スタンバイ維持", "대기 유지");
		Add("有 DSP 模块开启，但暂未检测到风险。", "有 DSP 模組開啟，但暫未偵測到風險。", "DSP modules are active, but no clipping risk detected yet.", "Des modules DSP sont actifs, aucun risque détecté.", "DSP-модули активны, но риск клиппинга не обнаружен.", "Hay módulos DSP activos, sin riesgo detectado aún.", "DSP モジュールは有効ですが、現在のところリスクは検出されていません。", "DSP 모듈이 활성화되어 있지만 아직 위험이 감지되지 않았습니다.");
		Add("应用 -3 dB", "套用 -3 dB", "Apply -3 dB", "Appliquer -3 dB", "Применить -3 дБ", "Aplicar -3 dB", "-3 dB を適用", "-3 dB 적용");
		Add("设为 -6 dB", "設為 -6 dB", "Set to -6 dB", "Régler à -6 dB", "Установить -6 дБ", "Fijar en -6 dB", "-6 dB に設定", "-6 dB로 설정");
		Add("Headroom 预算", "Headroom 預算", "Headroom Budget", "Budget Headroom", "Бюджет Headroom", "Presupuesto Headroom", "Headroom バジェット", "Headroom 예산");
		Add("等待当前音乐流数据", "等待目前音樂串流資料", "Waiting for audio stream", "En attente du flux audio", "Ожидание аудиопотока", "Esperando flujo de audio", "オーディオストリーム待機中", "오디오 스트림 데이터 대기 중");
		Add("刷新状态", "重新整理狀態", "Refresh", "Actualiser", "Обновить", "Actualizar", "状態を更新", "상태 새로고침");
		Add("{0} 次", "{0} 次", "{0} time(s)", "{0} fois", "{0} раз", "{0} veces", "{0} 回", "{0}회");
		Add("无记录", "無記錄", "None", "Aucun", "Нет записей", "Sin registro", "記録なし", "기록 없음");
		Add("曾削波", "曾削波", "Clipped", "Écrêté", "Был клиппинг", "Hubo recorte", "クリップあり", "클리핑 발생");
		Add("输入峰值", "輸入峰值", "Input Peak", "Crête d'entrée", "Входной пик", "Pico de entrada", "入力ピーク", "입력 피크");
		Add("输出估算", "輸出估算", "Est. Output", "Sortie estimée", "Оценка выхода", "Salida est.", "推定出力", "출력 추정");
		Add("▼ 实时状态", "▼ 即時狀態", "▼ Live Status", "▼ État en direct", "▼ Текущий статус", "▼ Estado en vivo", "▼ リアルタイム状態", "▼ 실시간 상태");
		Add("▶ 实时状态", "▶ 即時狀態", "▶ Live Status", "▶ État en direct", "▶ Текущий статус", "▶ Estado en vivo", "▶ リアルタイム状態", "▶ 실시간 상태");
		Add("保护状态", "保護狀態", "Protection", "Protection", "Защита", "Protección", "保護状態", "보호 상태");
		Add("削波次数", "削波次數", "Clip Count", "Écrêtages", "Клиппинг", "Recortes", "クリップ回数", "클리핑 횟수");
		Add("最近削波", "最近削波", "Last Clip", "Dernier écrêtage", "Последний пик", "Último recorte", "最終クリップ", "최근 클리핑");
		Add("检测到过载", "偵測到過載", "Overload Detected", "Surcharge détectée", "Обнаружена перегрузка", "Sobrecarga detectada", "過負荷を検出", "과부하 감지됨");
		Add("运行正常", "運行正常", "Normal", "Normal", "Нормально", "Normal", "正常動作", "정상 작동");

		// Mix Assistant & AutoEq in DspPage
		Add("已保存自定义 EQ 预设：{0}", "已儲存自訂 EQ 預設：{0}", "Saved custom EQ preset: {0}", "Préréglage EQ personnalisé enregistré : {0}", "Сохранён пользовательский пресет EQ: {0}", "Preajuste EQ personalizado guardado: {0}", "カスタム EQ プリセットを保存しました: {0}", "사용자 지정 EQ 프리셋 저장됨: {0}");
		Add("正在学习音频频谱平衡... ({0} 帧)", "正在學習音訊頻譜平衡... ({0} 幀)", "Learning audio spectrum balance... ({0} frames)", "Apprentissage de l'équilibre spectral... ({0} trames)", "Анализ спектрального баланса... ({0} кадров)", "Aprendiendo balance espectral... ({0} tramas)", "音声スペクトルバランスを学習中... ({0} フレーム)", "오디오 스펙트럼 밸런스 학습 중... ({0} 프레임)");
		Add("请先加载或播放本地音轨后再进行分析", "請先載入或播放本地音軌後再進行分析", "Please load or play a local track before analyzing", "Veuillez charger ou lire une piste locale avant d'analyser", "Сначала загрузите или воспроизведите локальный трек", "Carga o reproduce una pista local antes de analizar", "分析する前にローカル楽曲を読み込むか再生してください", "분석하기 전에 로컬 트랙을 로드하거나 재생하세요");
		Add("正在后台解码并分析当前音轨频谱...", "正在背景解碼並分析目前音軌頻譜...", "Decoding and analyzing track spectrum in background...", "Décodage et analyse du spectre en arrière-plan...", "Декодирование и анализ спектра трека в фоне...", "Decodificando y analizando el espectro en segundo plano...", "バックグラウンドで現在の楽曲をデコード・スペクトル解析中...", "백그라운드에서 현재 트랙 스펙트럼 디코딩 및 분석 중...");
		Add("未能从当前音轨读取有效的音频采样数据", "未能從目前音軌讀取有效的音訊取樣資料", "Could not read valid audio samples from the current track", "Impossible de lire des échantillons audio valides", "Не удалось прочитать аудиоданные из текущего трека", "No se pudieron leer muestras de audio válidas", "現在の楽曲から有効なオーディオサンプルを読み取れませんでした", "현재 트랙에서 유효한 오디오 샘플 데이터를 읽지 못했습니다");
		Add("音轨分析失败：{0}", "音軌分析失敗：{0}", "Track analysis failed: {0}", "Échec de l'analyse de la piste : {0}", "Ошибка анализа трека: {0}", "Error al analizar la pista: {0}", "楽曲の解析に失敗しました: {0}", "트랙 분석 실패: {0}");
		Add("AutoEq 耳机校准曲线载入成功", "AutoEq 耳機校準曲線載入成功", "AutoEq Headphone Calibration Loaded", "Courbe AutoEq chargée", "Кривая калибровки AutoEq загружена", "Curva AutoEq cargada", "AutoEq ヘッドホン補正カーブを読み込みました", "AutoEq 헤드폰 보정 커브 로드 성공");
		Add("已从 {0} 成功导入并解析 {1} 段 ParametricEQ 滤波器（前级增益 {2:F1} dB）。", "已從 {0} 成功匯入並解析 {1} 段 ParametricEQ 濾波器（前級增益 {2:F1} dB）。", "Imported {1} ParametricEQ filter bands from {0} (Preamp {2:F1} dB).", "{1} bandes ParametricEQ importées depuis {0} (Préampli {2:F1} dB).", "Импортировано полос ParametricEQ из {0}: {1} (предусил. {2:F1} дБ).", "Importadas {1} bandas ParametricEQ desde {0} (Preampl. {2:F1} dB).", "{0} から {1} バンドの ParametricEQ フィルターをインポートしました（プリアンプ {2:F1} dB）。", "{0}에서 {1}개 ParametricEQ 필터 밴드를 가져왔습니다 (프리앰프 {2:F1} dB).");
		Add("好", "好", "OK", "OK", "ОК", "Aceptar", "OK", "확인");
		Add("导入未完全匹配", "匯入未完全符合", "Import Incomplete", "Importation incomplète", "Импорт не завершён", "Importación incompleta", "インポートが完全一致しませんでした", "가져오기 불완전");
		Add("文件未包含标准 AutoEq 参数，请确认是否为包含 Preamp / Filter 的 ParametricEQ 文本。", "檔案未包含標準 AutoEq 參數，請確認是否為包含 Preamp / Filter 的 ParametricEQ 文字。", "The file does not contain standard AutoEq parameters. Please verify it is a ParametricEQ text file with Preamp / Filter entries.", "Le fichier ne contient pas de paramètres AutoEq standard (Preamp / Filter).", "Файл не содержит стандартных параметров AutoEq (Preamp / Filter).", "El archivo no contiene parámetros AutoEq estándar (Preamp / Filter).", "ファイルに標準の AutoEq パラメータが含まれていません。Preamp / Filter を含む ParametricEQ テキストか確認してください。", "파일에 표준 AutoEq 매개변수가 없습니다. Preamp / Filter가 포함된 ParametricEQ 텍스트인지 확인하세요.");
		Add("知道了", "知道了", "Got it", "Compris", "Понятно", "Entendido", "了解", "확인");
	}
}
