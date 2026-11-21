namespace FurinaPlayer.Core.Services;

public static partial class Loc
{
	static partial void RegisterSettings()
	{
		// 语言设置
		Add(
			"界面语言",
			"介面語言",
			"Interface Language",
			"Langue de l'interface",
			"Язык интерфейса",
			"Idioma de la interfaz",
			"表示言語",
			"인터페이스 언어");
		Add(
			"切换播放器所有界面与主题插件的显示语言",
			"切換播放器所有介面與主題外掛的顯示語言",
			"Switch the display language for all player screens and theme plugins",
			"Changer la langue d'affichage de toutes les interfaces et extensions de thème du lecteur",
			"Переключить язык отображения всех экранов плеера и плагинов тем",
			"Cambiar el idioma de visualización de todas las pantallas y complementos de tema del reproductor",
			"プレーヤーのすべての画面とテーマプラグインの表示言語を切り替えます",
			"플레이어의 모든 화면과 테마 플러그인 표시 언어를 전환합니다");

		// 系统与窗口设置
		Add(
			"系统与窗口设置",
			"系統與視窗設定",
			"System & Window Settings",
			"Paramètres système et fenêtre",
			"Настройки системы и окна",
			"Configuración del sistema y ventana",
			"システムとウィンドウ設定",
			"시스템 및 창 설정");
		Add(
			"界面缩放 (UI Scale)",
			"介面縮放 (UI Scale)",
			"UI Scale",
			"Mise à l'échelle (UI Scale)",
			"Масштаб интерфейса (UI Scale)",
			"Escala de interfaz (UI Scale)",
			"UI スケール",
			"UI 배율");
		Add(
			"（标准）",
			"（標準）",
			" (Default)",
			" (Standard)",
			" (Стандарт)",
			" (Estándar)",
			"（標準）",
			" (표준)");
		Add(
			"窗口关闭时隐藏到托盘",
			"視窗關閉時隱藏至系統匣",
			"Minimize to tray on close",
			"Réduire dans la zone de notification à la fermeture",
			"Сворачивать в трей при закрытии окна",
			"Ocultar en la bandeja al cerrar la ventana",
			"ウィンドウを閉じるときトレイに最小化",
			"창을 닫을 때 트레이로 숨기기");
		Add(
			"点击右上角关闭按钮时，保持应用在后台运行，双击托盘图标恢复窗口。",
			"點擊右上角關閉按鈕時，保持應用程式在背景執行，雙擊系統匣圖示恢復視窗。",
			"Keep the app running in the background when closing the window; double-click the tray icon to restore.",
			"Garder l'application en arrière-plan lors de la fermeture ; double-cliquez sur l'icône de la barre d'état pour restaurer.",
			"Оставлять приложение в фоне при закрытии окна; дважды щёлкните значок в трее, чтобы восстановить окно.",
			"Mantener la aplicación en segundo plano al cerrar la ventana; haz doble clic en el icono de la bandeja para restaurar.",
			"右上の閉じるボタンをクリックしてもバックグラウンドで実行を続け、トレイアイコンのダブルクリックで復元します。",
			"오른쪽 위 닫기 버튼을 눌러도 백그라운드에서 계속 실행하며, 트레이 아이콘을 두 번 클릭하면 창이 복원됩니다.");
		Add(
			"记住窗口大小与位置",
			"記住視窗大小與位置",
			"Remember window size and position",
			"Mémoriser la taille et la position de la fenêtre",
			"Запоминать размер и положение окна",
			"Recordar tamaño y posición de la ventana",
			"ウィンドウのサイズと位置を記憶する",
			"창 크기 및 위치 기억");
		Add(
			"退出时记录窗口尺寸与屏幕位置，并在下次启动时精准恢复。",
			"結束時記錄視窗尺寸與螢幕位置，並在下次啟動時精準恢復。",
			"Save window dimensions and screen position on exit, and restore them accurately on next launch.",
			"Enregistre la taille et la position de la fenêtre à la fermeture pour les restaurer au prochain lancement.",
			"Сохранять размер и положение окна при выходе и точно восстанавливать при следующем запуске.",
			"Guarda las dimensiones y posición de la ventana al salir para restaurarlas en el próximo inicio.",
			"終了時にウィンドウサイズと画面位置を記録し、次回起動時に正確に復元します。",
			"종료 시 창 크기와 화면 위치를 기록하여 다음 실행 시 정확히 복원합니다.");
		Add(
			"播放音乐时阻止系统休眠",
			"播放音樂時阻止系統休眠",
			"Prevent system sleep during playback",
			"Empêcher la mise en veille pendant la lecture",
			"Не давать системе засыпать во время воспроизведения",
			"Evitar suspensión del sistema durante la reproducción",
			"音楽再生中はシステムのスリープを防止",
			"음악 재생 중 시스템 절전 모드 방지");
		Add(
			"在音频高保真播放期间保持系统唤醒，避免 Windows 进入睡眠导致播放中断。",
			"在音訊高保真播放期間保持系統喚醒，避免 Windows 進入睡眠導致播放中斷。",
			"Keep the system awake during hi-fi audio playback so Windows sleep does not interrupt listening.",
			"Maintient le système éveillé pendant la lecture audio haute fidélité pour éviter toute interruption.",
			"Поддерживать активность системы при воспроизведении Hi-Fi аудио, чтобы спящий режим Windows не прерывал звук.",
			"Mantiene el sistema activo durante la reproducción de audio de alta fidelidad para evitar interrupciones por suspensión.",
			"ハイレゾオーディオ再生中にシステムのスリープを防ぎ、Windows のスリープによる再生中断を回避します。",
			"고음질 오디오 재생 중 시스템이 절전 모드로 전환되어 재생이 중단되는 것을 방지합니다.");
		Add("开启", "開啟", "On", "Activé", "Вкл", "Activado", "オン", "켜기");
		Add("关闭", "關閉", "Off", "Désactivé", "Выкл", "Desactivado", "オフ", "끄기");

		// 音频输出与均衡器状态
		Add(
			"软件解码",
			"軟體解碼",
			"Software Decoding",
			"Décodage logiciel",
			"Программное декодирование",
			"Decodificación por software",
			"ソフトウェアデコード",
			"소프트웨어 디코딩");
		Add(
			"WASAPI 共享",
			"WASAPI 共用",
			"WASAPI Shared",
			"WASAPI Partagé",
			"WASAPI Общий",
			"WASAPI Compartido",
			"WASAPI 共有",
			"WASAPI 공유");
		Add(
			"WASAPI 独占",
			"WASAPI 獨佔",
			"WASAPI Exclusive",
			"WASAPI Exclusif",
			"WASAPI Эксклюзивный",
			"WASAPI Exclusivo",
			"WASAPI 排他",
			"WASAPI 단독");
		Add(
			"{0}（默认）",
			"{0}（預設）",
			"{0} (Default)",
			"{0} (Par défaut)",
			"{0} (По умолчанию)",
			"{0} (Predeterminado)",
			"{0}（既定）",
			"{0} (기본값)");
		Add("平坦", "平坦", "Flat", "Plat", "Ровный", "Plano", "フラット", "플랫");
		Add("自定义", "自訂", "Custom", "Personnalisé", "Пользовательский", "Personalizado", "カスタム", "사용자 지정");

		// 外观、主题与材质
		Add(
			"外观与个性化",
			"外觀與個人化",
			"Appearance & Personalization",
			"Apparence et personnalisation",
			"Внешний вид и персонализация",
			"Apariencia y personalización",
			"外観とパーソナライズ",
			"모양 및 개인 설정");
		Add("主题模式", "主題模式", "Theme Mode", "Mode de thème", "Режим темы", "Modo de tema", "テーマモード", "테마 모드");
		Add("浅色", "淺色", "Light", "Clair", "Светлая", "Claro", "ライト", "라이트");
		Add("深色", "深色", "Dark", "Sombre", "Тёмная", "Oscuro", "ダーク", "다크");
		Add("跟随系统", "跟隨系統", "Follow System", "Système", "Как в системе", "Sistema", "システム設定に従う", "시스템 설정 따름");
		Add("亚克力", "壓克力", "Acrylic", "Acrylique", "Акрил", "Acrílico", "アクリル", "아크릴");
		Add("液态玻璃", "液態玻璃", "Liquid Glass", "Verre liquide", "Жидкое стекло", "Cristal líquido", "リキッドグラス", "리퀴드 글래스");
		Add(
			"液态玻璃（新版·重启生效）",
			"液態玻璃（新版·重啟生效）",
			"Liquid Glass (New · Restart required)",
			"Verre liquide (Nouveau · Redémarrage requis)",
			"Жидкое стекло (новая · требуется перезапуск)",
			"Cristal líquido (Nuevo · Requiere reinicio)",
			"リキッドグラス（新版・再起動で有効）",
			"리퀴드 글래스 (신버전 · 재시작 시 적용)");
		Add(
			"液态玻璃（新版）",
			"液態玻璃（新版）",
			"Liquid Glass (New)",
			"Verre liquide (Nouveau)",
			"Жидкое стекло (новая)",
			"Cristal líquido (Nuevo)",
			"リキッドグラス（新版）",
			"리퀴드 글래스 (신버전)");
		Add("本地图片", "本機圖片", "Local Image", "Image locale", "Локальное изображение", "Imagen local", "ローカル画像", "로컬 이미지");
		Add("纯色", "純色", "Solid Color", "Couleur unie", "Сплошной цвет", "Color sólido", "単色", "단색");
		Add(
			"Bing 每日壁纸",
			"Bing 每日桌布",
			"Bing Daily Wallpaper",
			"Fond d'écran quotidien Bing",
			"Ежедневные обои Bing",
			"Fondo diario de Bing",
			"Bing 日替わり壁紙",
			"Bing 오늘의 배경 화면");
		Add(
			"选择失败：{0}",
			"選擇失敗：{0}",
			"Selection failed: {0}",
			"Échec de la sélection : {0}",
			"Ошибка выбора: {0}",
			"Error al seleccionar: {0}",
			"選択に失敗しました: {0}",
			"선택 실패: {0}");

		// 窗口亚克力主题卡片
		Add(
			"窗口亚克力主题",
			"視窗壓克力主題",
			"Window Acrylic Theme",
			"Thème acrylique de la fenêtre",
			"Акриловая тема окна",
			"Tema acrílico de ventana",
			"ウィンドウアクリルテーマ",
			"창 아크릴 테마");
		Add(
			"开启后会立即使用系统亚克力材质，让桌面背景从窗口后方透出；界面会保留可读遮罩，Windows 11 22H2 及以上效果最佳。",
			"開啟後會立即使用系統壓克力材質，讓桌面背景從視窗後方透出；介面會保留可讀遮罩，Windows 11 22H2 及以上效果最佳。",
			"Enables system Acrylic material immediately so your desktop shows through behind the window while keeping UI text readable. Best on Windows 11 22H2+.",
			"Active immédiatement le matériau Acrylique système pour laisser transparaître le bureau tout en gardant l'interface lisible. Idéal sous Windows 11 22H2+.",
			"Сразу включает системный материал Acrylic, делая окно полупрозрачным с сохранением читаемости текста. Лучше всего работает в Windows 11 22H2+.",
			"Activa de inmediato el material Acrílico del sistema para que el escritorio se trasluzca manteniendo la legibilidad. Óptimo en Windows 11 22H2+.",
			"有効にすると即座にシステムのアクリル素材を適用し、ウィンドウ背後にデスクトップが透けて見えます（可読性マスク維持、Windows 11 22H2以降推奨）。",
			"켜면 즉시 시스템 아크릴 재질이 적용되어 창 뒤로 바탕 화면이 비치며 가독성 마스크가 유지됩니다. Windows 11 22H2 이상에서 최적입니다.");
		Add(
			"失焦时保持通透",
			"失去焦點時保持通透",
			"Keep translucent when unfocused",
			"Rester translucide sans focus",
			"Сохранять прозрачность без фокуса",
			"Mantener translucidez sin foco",
			"非フォーカス時も透過を維持",
			"비활성 상태에서도 투명도 유지");
		Add(
			"切到别的软件时，不收回窗口亚克力透明效果",
			"切換至其他軟體時，不收回視窗壓克力透明效果",
			"Do not disable Acrylic transparency when switching to another app",
			"Ne pas désactiver la transparence acrylique en changeant d'application",
			"Не отключать акриловую прозрачность при переключении на другие программы",
			"No desactivar la transparencia acrílica al cambiar a otra aplicación",
			"他のアプリに切り替えてもウィンドウのアクリル透過を維持します",
			"다른 앱으로 전환해도 창의 아크릴 투명 효과를 끄지 않습니다");
		Add("透明度", "透明度", "Opacity", "Opacité", "Прозрачность", "Opacidad", "不透明度", "투명도");
		Add(
			"0% 为不透明，100% 为最通透",
			"0% 為不透明，100% 為最通透",
			"0% is opaque, 100% is most translucent",
			"0 % opaque, 100 % le plus translucide",
			"0% — непрозрачно, 100% — максимальная прозрачность",
			"0% opaco, 100% más translúcido",
			"0% で不透明、100% で最大透過",
			"0%는 불투명, 100%는 최대 투명");

		// 歌词字体
		Add("歌词字体", "歌詞字型", "Lyric Font", "Police des paroles", "Шрифт текста песен", "Fuente de letras", "歌詞フォント", "가사 글꼴");
		Add("系统默认", "系統預設", "System Default", "Par défaut du système", "Системный", "Predeterminado del sistema", "システム既定", "시스템 기본값");
		Add("已导入自定义字体", "已匯入自訂字型", "Imported Custom Font", "Police personnalisée importée", "Импортированный шрифт", "Fuente personalizada importada", "インポート済みカスタムフォント", "가져온 사용자 지정 글꼴");
		Add(
			"已导入字体：{0}（优先生效）",
			"已匯入字型：{0}（優先套用）",
			"Imported font: {0} (takes priority)",
			"Police importée : {0} (prioritaire)",
			"Импортирован шрифт: {0} (приоритет)",
			"Fuente importada: {0} (prioritaria)",
			"インポート済みフォント: {0}（優先適用）",
			"가져온 글꼴: {0} (우선 적용)");
		Add(
			"正在播放页歌词使用所选字体；导入的 .ttf/.otf 字体文件优先生效并复制到应用数据目录。",
			"現正播放頁歌詞使用所選字型；匯入的 .ttf/.otf 字型檔優先套用並複製到應用程式資料目錄。",
			"Now Playing lyrics use the selected font; imported .ttf/.otf files take priority and are copied to the app data folder.",
			"Les paroles utilisent la police sélectionnée ; les fichiers .ttf/.otf importés sont prioritaires et copiés dans les données de l'application.",
			"Текст песни использует выбранный шрифт; импортированные .ttf/.otf файлы имеют приоритет и копируются в папку данных приложения.",
			"Las letras usan la fuente seleccionada; los archivos .ttf/.otf importados tienen prioridad y se copian al directorio de datos.",
			"再生中ページの歌詞に選択したフォントを使用します。インポートした .ttf/.otf ファイルが優先され、アプリデータフォルダにコピーされます。",
			"지금 재생 중 화면의 가사에 선택한 글꼴을 사용합니다. 가져온 .ttf/.otf 파일이 우선 적용되며 앱 데이터 폴더에 복사됩니다.");
		Add("已导入：{0}", "已匯入：{0}", "Imported: {0}", "Importé : {0}", "Импорт: {0}", "Importado: {0}", "インポート済み: {0}", "가져옴: {0}");
		Add("导入字体失败：{0}", "匯入字型失敗：{0}", "Failed to import font: {0}", "Échec de l'importation de la police : {0}", "Ошибка импорта шрифта: {0}", "Error al importar la fuente: {0}", "フォントのインポートに失敗: {0}", "글꼴 가져오기 실패: {0}");

		// 定时深色、Rainbow 与反色
		Add("定时切换深色模式", "定時切換深色模式", "Scheduled Dark Mode", "Mode sombre programmé", "Тёмная тема по расписанию", "Modo oscuro programado", "ダークモードのスケジュール", "예약된 다크 모드");
		Add("已启用", "已啟用", "Enabled", "Activé", "Включено", "Activado", "有効", "사용 중");
		Add("未启用", "未啟用", "Disabled", "Désactivé", "Отключено", "Desactivado", "無効", "사용 안 함");
		Add("切到深色", "切至深色", "Switch to Dark", "Passer en sombre", "На тёмную", "Cambiar a oscuro", "ダークへ切替", "다크로 전환");
		Add("切回浅色", "切回淺色", "Switch to Light", "Passer en clair", "На светлую", "Cambiar a claro", "ライトへ切替", "라이트로 전환");
		Add("Rainbow 模式", "Rainbow 模式", "Rainbow Mode", "Mode Rainbow", "Режим Rainbow", "Modo Rainbow", "Rainbow モード", "Rainbow 모드");
		Add("反色模式", "反色模式", "Invert Colors", "Mode inversé", "Инверсия цветов", "Modo invertido", "色反転モード", "색상 반전 모드");

		// 主题预设与自定义主题
		Add("主题预设", "主題預設", "Theme Presets", "Préréglages de thème", "Предустановки тем", "Ajustes preestablecidos de tema", "テーマプリセット", "테마 프리셋");
		Add("收起主题预设 ∧", "收起主題預設 ∧", "Collapse Presets ∧", "Réduire les préréglages ∧", "Свернуть пресеты ∧", "Contraer temas ∧", "プリセットを折りたたむ ∧", "테마 프리셋 접기 ∧");
		Add("展开主题预设 ∨", "展開主題預設 ∨", "Expand Presets ∨", "Développer les préréglages ∨", "Развернуть пресеты ∨", "Expandir temas ∨", "プリセットを展開 ∨", "테마 프리셋 펼치기 ∨");
		Add("自定义主题", "自訂主題", "Custom Themes", "Thèmes personnalisés", "Пользовательские темы", "Temas personalizados", "カスタムテーマ", "사용자 지정 테마");
		Add(
			"新主题会自动收入「我的」，支持配色编辑和 JSON 参数导入",
			"新主題會自動收錄至「我的」，支援配色編輯與 JSON 參數匯入",
			"New themes are automatically saved under \"Mine\", supporting color editing and JSON parameter import",
			"Les nouveaux thèmes sont enregistrés dans « Mes thèmes », avec édition des couleurs et import JSON",
			"Новые темы автоматически сохраняются в «Мои», поддерживается редактирование цветов и импорт JSON",
			"Los temas nuevos se guardan en «Míos», con edición de colores e importación de parámetros JSON",
			"新しいテーマは自動的に「マイテーマ」に保存され、配色編集と JSON パラメータのインポートに対応しています",
			"새 테마는 자동으로 '내 테마'에 저장되며 색상 편집 및 JSON 매개변수 가져오기를 지원합니다");
		Add("✎ 进入编辑", "✎ 進入編輯", "✎ Edit Current", "✎ Modifier", "✎ Редактировать", "✎ Editar actual", "✎ 編集", "✎ 편집하기");
		Add("+ 新建我的主题", "+ 新增我的主題", "+ New Theme", "+ Nouveau thème", "+ Новая тема", "+ Nuevo tema", "+ 新規テーマ", "+ 새 테마 만들기");
		Add("📁 导入参数", "📁 匯入參數", "📁 Import JSON", "📁 Importer JSON", "📁 Импорт JSON", "📁 Importar JSON", "📁 パラメータ読込", "📁 매개변수 가져오기");

		// 主题分类
		Add("全部", "全部", "All", "Tous", "Все", "Todos", "すべて", "전체");
		Add("我的", "我的", "Mine", "Mes thèmes", "Мои", "Míos", "マイテーマ", "내 테마");
		Add("简洁", "簡潔", "Minimal", "Épuré", "Минимализм", "Minimalista", "シンプル", "심플");
		Add("柔和", "柔和", "Soft", "Doux", "Мягкие", "Suave", "ソフト", "소프트");
		Add("和纸", "和紙", "Washi", "Washi", "Васи", "Washi", "和紙", "와시");
		Add("夜色", "夜色", "Night", "Nuit", "Ночь", "Noche", "ナイト", "나이트");
		Add("工作室", "工作室", "Studio", "Studio", "Студия", "Estudio", "スタジオ", "스튜디오");
		Add("个性", "個性", "Vibrant", "Expressif", "Яркие", "Expresivo", "個性派", "개성");

		// 随机主题
		Add("随机主题", "隨機主題", "Random Theme", "Thème aléatoire", "Случайная тема", "Tema aleatorio", "ランダムテーマ", "랜덤 테마");
		Add("随机主题 · {0}", "隨機主題 · {0}", "Random Theme · {0}", "Thème aléatoire · {0}", "Случайная тема · {0}", "Tema aleatorio · {0}", "ランダムテーマ · {0}", "랜덤 테마 · {0}");
		Add(
			"随机生成一套颜色并自动调整对比度，保证文字和按钮可读。",
			"隨機產生一組配色並自動調整對比度，確保文字與按鈕清晰可讀。",
			"Generates a random color palette and automatically adjusts contrast so text and buttons stay legible.",
			"Génère une palette aléatoire et ajuste le contraste pour garder les textes et boutons lisibles.",
			"Генерирует случайную палитру и автоматически настраивает контрастность для читаемости текста и кнопок.",
			"Genera una paleta aleatoria y ajusta el contraste automáticamente para mantener la legibilidad.",
			"ランダムな配色を生成し、文字とボタンが読みやすいようコントラストを自動調整します。",
			"무작위 색상 조합을 생성하고 텍스트와 버튼의 가독성을 위해 대비를 자동 조정합니다.");

		// 自定义主题弹窗 & JSON 导入弹窗
		Add("主题名称", "主題名稱", "Theme Name", "Nom du thème", "Название темы", "Nombre del tema", "テーマ名", "테마 이름");
		Add("{0} · 定制", "{0} · 訂製", "{0} · Custom", "{0} · Perso", "{0} · Своя", "{0} · Pers.", "{0} · カスタム", "{0} · 맞춤");
		Add("我的水神主题", "我的水神主題", "My Furina Theme", "Mon thème Furina", "Моя тема Furina", "Mi tema Furina", "マイフリーナテーマ", "나만의 푸리나 테마");
		Add("背景主色 (Hex)", "背景主色 (Hex)", "Background Primary (Hex)", "Couleur principale du fond (Hex)", "Основной цвет фона (Hex)", "Color principal de fondo (Hex)", "背景メイン色 (Hex)", "배경 기본 색상 (Hex)");
		Add("面板渐变色 (Hex)", "面板漸層色 (Hex)", "Panel Gradient (Hex)", "Dégradé du panneau (Hex)", "Градиент панели (Hex)", "Degradado del panel (Hex)", "パネルグラデーション色 (Hex)", "패널 그라데이션 색상 (Hex)");
		Add("强调色 (Hex)", "強調色 (Hex)", "Accent Color (Hex)", "Couleur d'accentuation (Hex)", "Акцентный цвет (Hex)", "Color de énfasis (Hex)", "アクセントカラー (Hex)", "강조 색상 (Hex)");
		Add("深色基底主题", "深色基底主題", "Dark Base Theme", "Thème de base sombre", "Тёмная базовая тема", "Tema base oscuro", "ダークベーステーマ", "다크 베이스 테마");
		Add("编辑并保存为我的主题", "編輯並儲存為我的主題", "Edit & Save to My Themes", "Modifier et enregistrer dans Mes thèmes", "Изменить и сохранить в Мои темы", "Editar y guardar en Mis temas", "編集してマイテーマに保存", "편집 후 내 테마에 저장");
		Add("新建我的主题", "新增我的主題", "Create New Theme", "Créer un nouveau thème", "Создать новую тему", "Crear nuevo tema", "新規テーマを作成", "새 테마 만들기");
		Add("保存并应用", "儲存並套用", "Save & Apply", "Enregistrer et appliquer", "Сохранить и применить", "Guardar y aplicar", "保存して適用", "저장 및 적용");
		Add("取消", "取消", "Cancel", "Annuler", "Отмена", "Cancelar", "キャンセル", "취소");
		Add("导入", "匯入", "Imported", "Importé", "Импорт", "Importado", "インポート", "가져옴");
		Add("JSON 导入主题预设", "JSON 匯入主題預設", "JSON Imported Theme Preset", "Préréglage importé par JSON", "Тема из JSON", "Tema importado por JSON", "JSON インポートテーマ", "JSON 가져오기 테마");
		Add(
			"主题预设 JSON 参数（支持复制导出或粘贴导入）",
			"主題預設 JSON 參數（支援複製匯出或貼上匯入）",
			"Theme Preset JSON Parameters (copy to export or paste to import)",
			"Paramètres JSON du thème (copier pour exporter ou coller pour importer)",
			"JSON-параметры темы (скопируйте для экспорта или вставьте для импорта)",
			"Parámetros JSON del tema (copia para exportar o pega para importar)",
			"テーマプリセット JSON パラメータ（コピーでエクスポート、貼り付けでインポート）",
			"테마 프리셋 JSON 매개변수 (복사하여 내보내기 또는 붙여넣어 가져오기)");
		Add("导入 / 导出自定义主题参数", "匯入 / 匯出自訂主題參數", "Import / Export Custom Theme JSON", "Importer / Exporter les paramètres de thème", "Импорт / Экспорт параметров темы", "Importar / Exportar parámetros del tema", "カスタムテーマのインポート / エクスポート", "사용자 지정 테마 매개변수 가져오기 / 내보내기");
		Add("导入并应用", "匯入並套用", "Import & Apply", "Importer et appliquer", "Импортировать и применить", "Importar y aplicar", "インポートして適用", "가져오기 및 적용");

		// 曲库封面补全 & 状态
		Add(
			"未执行（手动触发，每分钟最多 2 次）",
			"未執行（手動觸發，每分鐘最多 2 次）",
			"Idle (manual trigger, max 2 per minute)",
			"Inactif (déclenchement manuel, max 2/min)",
			"Не запускалось (вручную, макс. 2 раза/мин)",
			"Sin ejecutar (manual, máx. 2 por minuto)",
			"未実行（手動実行・1分あたり最大2回）",
			"실행 안 함 (수동 실행, 분당 최대 2회)");
		Add("扫描曲库文件完整性并补齐", "掃描曲庫檔案完整性並補齊", "Scan & Repair Library Integrity", "Analyser et réparer l'intégrité", "Проверить и дополнить медиатеку", "Analizar y reparar biblioteca", "ライブラリ整合性スキャン・補完", "라이브러리 무결성 검사 및 보완");
		Add("停止完整性扫描", "停止完整性掃描", "Stop Integrity Scan", "Arrêter l'analyse", "Остановить проверку", "Detener análisis", "スキャン停止", "검사 중지");
		Add("尚未扫描（多线程并行检查元数据、内嵌歌词与专辑封面）", "尚未掃描（多執行緒並行檢查中繼資料、內嵌歌詞與專輯封面）", "Not scanned yet (multi-threaded check for tags, embedded lyrics & cover art)", "Non analysé (vérification parallèle des tags, paroles et pochettes)", "Не проверялось (многопоточная проверка тегов, текста и обложек)", "Sin analizar (verificación paralela de metadatos, letras y carátulas)", "未スキャン（タグ・歌詞・カバーアートを並列チェック＆補完）", "검사 안 됨 (메타데이터, 내장 가사, 앨범 아트 병렬 검사 및 보완)");
		Add("曲库内暂无可检查的音乐文件", "曲庫內暫無可檢查的音樂檔案", "No tracks in library to inspect", "Aucune piste dans la bibliothèque", "В медиатеке нет треков для проверки", "No hay pistas en la biblioteca", "ライブラリに検査対象の曲がありません", "라이브러리에 검사할 트랙이 없습니다");
		Add("正在多线程检查曲库文件完整性…", "正在多執行緒檢查曲庫檔案完整性…", "Checking library integrity in parallel…", "Vérification parallèle en cours…", "Параллельная проверка медиатеки…", "Comprobando integridad en paralelo…", "ライブラリの整合性を並列チェック中…", "라이브러리 무결성 병렬 검사 중…");
		Add("进度 {0}/{1} ({2:0}%) · 正在检查：{3} | 补齐 元数据:{4} 歌词:{5} 封面:{6}", "進度 {0}/{1} ({2:0}%) · 正在檢查：{3} | 補齊 中繼資料:{4} 歌詞:{5} 封面:{6}", "Progress {0}/{1} ({2:0}%) · Checking: {3} | Repaired Tags:{4} Lyrics:{5} Cover:{6}", "Progression {0}/{1} ({2:0}%) · {3} | Tags:{4} Paroles:{5} Pochettes:{6}", "Прогресс {0}/{1} ({2:0}%) · {3} | Теги:{4} Текст:{5} Обложки:{6}", "Progreso {0}/{1} ({2:0}%) · {3} | Metadatos:{4} Letras:{5} Carátula:{6}", "進捗 {0}/{1} ({2:0}%) · 検査中:{3} | 補完 タグ:{4} 歌詞:{5} カバー:{6}", "진행 {0}/{1} ({2:0}%) · 검사 중:{3} | 보완 태그:{4} 가사:{5} 커버:{6}");
		Add("完整性检查完成（共 {0} 首）：完整 {1} 首，补齐 元数据 {2} / 歌词 {3} / 封面 {4}，失败 {5} 首", "完整性檢查完成（共 {0} 首）：完整 {1} 首，補齊 中繼資料 {2} / 歌詞 {3} / 封面 {4}，失敗 {5} 首", "Integrity check finished ({0} tracks): {1} already complete, repaired Tags {2} / Lyrics {3} / Cover {4}, failed {5}", "Analyse terminée ({0} titres) : {1} complets, réparés Tags {2} / Paroles {3} / Pochettes {4}, échecs {5}", "Проверка завершена ({0} треков): полных {1}, дополнено теги {2} / текст {3} / обложки {4}, ошибок {5}", "Análisis finalizado ({0} pistas): {1} completas, reparados Metadatos {2} / Letras {3} / Carátulas {4}, fallos {5}", "整合性チェック完了（全 {0} 曲）：完全 {1} 曲、補完 タグ {2} / 歌詞 {3} / カバー {4}、失敗 {5} 曲", "무결성 검사 완료 (총 {0}곡): 정상 {1}곡, 보완 태그 {2} / 가사 {3} / 커버 {4}, 실패 {5}곡");
		Add("完整性检查已取消（已处理 {0}/{1} 首，补齐 元数据 {2} / 歌词 {3} / 封面 {4}）", "完整性檢查已取消（已處理 {0}/{1} 首，補齊 中繼資料 {2} / 歌詞 {3} / 封面 {4}）", "Integrity check cancelled ({0}/{1} processed, repaired Tags {2} / Lyrics {3} / Cover {4})", "Analyse annulée ({0}/{1} traités, réparés Tags {2} / Paroles {3} / Pochettes {4})", "Проверка отменена ({0}/{1} обработано, дополнено теги {2} / текст {3} / обложки {4})", "Análisis cancelado ({0}/{1} procesados, reparados Metadatos {2} / Letras {3} / Carátulas {4})", "スキャンをキャンセルしました（処理済 {0}/{1} 曲、補完 タグ {2} / 歌詞 {3} / カバー {4}）", "검사 취소됨 ({0}/{1} 처리, 보완 태그 {2} / 가사 {3} / 커버 {4})");
		Add(
			"下次可刷新：{0} 秒后",
			"下次可重新整理：{0} 秒後",
			"Next refresh available in {0}s",
			"Prochaine actualisation dans {0} s",
			"Следующее обновление через {0} с",
			"Próxima actualización en {0} s",
			"次回更新可能まで: {0} 秒",
			"다음 새로 고침까지 {0}초");
		Add(
			"曲库中没有缺少封面的歌曲",
			"曲庫中沒有缺少封面的歌曲",
			"No tracks in the library are missing album art",
			"Aucun morceau de la bibliothèque ne manque de pochette",
			"В медиатеке нет треков без обложки",
			"No hay canciones sin carátula en la biblioteca",
			"ライブラリにジャケット画像が不足している曲はありません",
			"라이브러리에 앨범 아트가 누락된 곡이 없습니다");
		Add(
			"正在为《{0}》搜索封面…",
			"正在為《{0}》搜尋封面…",
			"Searching cover art for \"{0}\"...",
			"Recherche de pochette pour « {0} »...",
			"Поиск обложки для «{0}»...",
			"Buscando carátula para «{0}»...",
			"「{0}」のジャケット画像を検索中…",
			"\"{0}\" 앨범 아트 검색 중…");
		Add(
			"已达限流上限，请稍后再试：{0}",
			"已達限流上限，請稍後再試：{0}",
			"Rate limit reached, please try again later: {0}",
			"Limite de requêtes atteinte, veuillez réessayer plus tard : {0}",
			"Превышен лимит запросов, повторите позже: {0}",
			"Límite de solicitudes alcanzado, inténtalo más tarde: {0}",
			"レート制限に達しました。しばらくしてから再試行してください: {0}",
			"요청 한도에 도달했습니다. 잠시 후 다시 시도하세요: {0}");
		Add(
			"封面已添加：《{0}》",
			"封面已新增：《{0}》",
			"Cover added: \"{0}\"",
			"Pochette ajoutée : « {0} »",
			"Обложка добавлена: «{0}»",
			"Carátula añadida: «{0}»",
			"ジャケット画像を追加しました: 「{0}」",
			"앨범 아트가 추가되었습니다: \"{0}\"");
		Add(
			"操作异常：{0}",
			"操作異常：{0}",
			"Operation error: {0}",
			"Erreur d'opération : {0}",
			"Ошибка операции: {0}",
			"Error de operación: {0}",
			"操作エラー: {0}",
			"작업 오류: {0}");

		// 恢复默认设置弹窗
		Add("恢复默认设置", "恢復預設設定", "Restore Default Settings", "Rétablir les paramètres par défaut", "Сбросить настройки", "Restaurar valores predeterminados", "既定の設定に戻す", "기본 설정 복원");
		Add(
			"将重置音量、均衡器、空间音频、音色、主题、材质、壁纸、输出设备、歌词字体、VST3 插件配置等所有自定义参数为出厂默认值（类似清洁安装）。\n此操作保留已导入的音乐与专辑，不会删除音乐库与本地文件，是否继续？",
			"將重置音量、等化器、空間音訊、音色、主題、材質、桌布、輸出裝置、歌詞字型、VST3 外掛設定等所有自訂參數為出廠預設值（類似全新安裝）。\n此操作保留已匯入的音樂與專輯，不會刪除音樂庫與本機檔案，是否繼續？",
			"This will reset volume, equalizer, spatial audio, tone, theme, material, wallpaper, output device, lyric font, VST3 plugin settings, and all other custom parameters to factory defaults.\nImported music and albums will be kept and local files will not be deleted. Continue?",
			"Réinitialise le volume, l'égaliseur, l'audio spatial, le timbre, le thème, le matériau, le fond d'écran, le périphérique de sortie, la police des paroles et les paramètres VST3.\nLes musiques importées et fichiers locaux seront conservés. Continuer ?",
			"Сбросит громкость, эквалайзер, пространственный звук, тембр, тему, материал, обои, устройство вывода, шрифт текста и настройки VST3 до заводских значений.\nИмпортированная музыка и локальные файлы не будут удалены. Продолжить?",
			"Restablecerá volumen, ecualizador, audio espacial, timbre, tema, material, fondo, dispositivo de salida, fuente de letras y ajustes VST3 a los valores de fábrica.\nLa música importada y los archivos locales se conservarán. ¿Continuar?",
			"音量、イコライザー、空間オーディオ、音色、テーマ、マテリアル、壁紙、出力デバイス、歌詞フォント、VST3 プラグイン設定などのすべてのカスタム設定を初期値に戻します。\nインポート済みの音楽やローカルファイルは削除されません。続行しますか？",
			"음량, 이퀄라이저, 공간 음향, 음색, 테마, 재질, 배경 화면, 출력 장치, 가사 글꼴, VST3 플러그인 설정 등 모든 사용자 지정 매개변수를 초기값으로 재설정합니다.\n가져온 음악과 로컬 파일은 삭제되지 않습니다. 계속하시겠습니까?");
		Add("恢复", "恢復", "Restore", "Rétablir", "Сбросить", "Restaurar", "復元", "복원");

		// 关于与开源组件
		Add("软件版本：{0}", "軟體版本：{0}", "Version: {0}", "Version : {0}", "Версия: {0}", "Versión: {0}", "バージョン: {0}", "버전: {0}");
		Add("LRCLIB 在线歌词 API", "LRCLIB 線上歌詞 API", "LRCLIB Online Lyrics API", "API de paroles en ligne LRCLIB", "Онлайн-API текстов песен LRCLIB", "API de letras en línea LRCLIB", "LRCLIB オンライン歌詞 API", "LRCLIB 온라인 가사 API");
		Add("网易云音乐歌词接口（非官方）", "網易雲音樂歌詞介面（非官方）", "NetEase Cloud Music Lyrics API (Unofficial)", "API de paroles NetEase Cloud Music (non officielle)", "API текстов песен NetEase Cloud Music (неофициальный)", "API de letras de NetEase Cloud Music (no oficial)", "NetEase Cloud Music 歌詞 API（非公式）", "NetEase Cloud Music 가사 API (비공식)");
		Add("VST3 SDK（Steinberg，接口规范参考）", "VST3 SDK（Steinberg，介面規範參考）", "VST3 SDK (Steinberg, interface specification reference)", "VST3 SDK (Steinberg, référence de spécification)", "VST3 SDK (Steinberg, спецификация интерфейса)", "VST3 SDK (Steinberg, referencia de especificación)", "VST3 SDK（Steinberg、インターフェース仕様参照）", "VST3 SDK (Steinberg, 인터페이스 사양 참조)");
		Add("VST3 插件模拟宿主（自研，尚未加载真实插件）", "VST3 外掛模擬宿主（自研，尚未載入真實外掛）", "VST3 Plugin Mock Host (in-house, real plugins not yet loaded)", "Hôte simulé de plugin VST3 (interne, sans chargement de vrais plugins)", "Эмулятор хоста VST3-плагинов (собственная разработка)", "Host simulado de plugins VST3 (propio, sin carga real aún)", "VST3 プラグイン模擬ホスト（独自実装・実プラグイン未対応）", "VST3 플러그인 모의 호스트 (자체 구현, 실제 플러그인 미지원)");
		Add(
			"{0}（第 {1} 次迭代 · {2} 项新增功能 · {3} 项修复 · {4} 编译）",
			"{0}（第 {1} 次迭代 · {2} 項新增功能 · {3} 項修復 · {4} 編譯）",
			"{0} (Iteration {1} · {2} new features · {3} fixes · {4} build)",
			"{0} (Itération {1} · {2} nouveautés · {3} correctifs · build {4})",
			"{0} (Итерация {1} · {2} новых функций · {3} исправлений · сборка {4})",
			"{0} (Iteración {1} · {2} novedades · {3} correcciones · compilación {4})",
			"{0}（第 {1} 回イテレーション · 新機能 {2} 件 · 修正 {3} 件 · {4} ビルド）",
			"{0} ({1}차 이터레이션 · 새 기능 {2}개 · 수정 {3}건 · {4} 빌드)");

		// SettingsPage 静态 XBF 控件文本
		Add("软件解码（LibVLC / NAudio）", "軟體解碼（LibVLC / NAudio）", "Software Decode (LibVLC / NAudio)", "Décodage logiciel (LibVLC / NAudio)", "Программное декодирование (LibVLC / NAudio)", "Decodificación por software (LibVLC / NAudio)", "ソフトウェアデコード（LibVLC / NAudio）", "소프트웨어 디코딩 (LibVLC / NAudio)");
		Add("共享", "共用", "Shared", "Partagé", "Общий", "Compartido", "共有", "공유");
		Add("独占", "獨佔", "Exclusive", "Exclusif", "Монопольный", "Exclusivo", "排他", "단독");
		Add("设备", "裝置", "Device", "Périphérique", "Устройство", "Dispositivo", "デバイス", "장치");
		Add("缓冲大小（毫秒）", "緩衝大小（毫秒）", "Buffer Size (ms)", "Taille du tampon (ms)", "Размер буфера (мс)", "Tamaño de búfer (ms)", "バッファーサイズ（ms）", "버퍼 크기(ms)");
		Add("外观与主题", "外觀與主題", "Appearance & Theme", "Apparence et thème", "Внешний вид и тема", "Apariencia y tema", "外観とテーマ", "모양 및 테마");
		Add("主题", "主題", "Theme", "Thème", "Тема", "Tema", "テーマ", "테마");
		Add("材质效果", "材質效果", "Material Effect", "Effet de matériau", "Эффект материала", "Efecto de material", "マテリアル効果", "재질 효과");
		Add("壁纸来源", "桌布來源", "Wallpaper Source", "Source du fond d'écran", "Источник обоев", "Origen del fondo", "壁紙のソース", "배경 화면 소스");
		Add("每日壁纸", "每日桌布", "Daily Wallpaper", "Fond d'écran quotidien", "Обои дня", "Fondo diario", "日替わり壁紙", "오늘의 배경 화면");
		Add("选择本地图片…", "選擇本機圖片…", "Choose Local Image...", "Choisir une image locale...", "Выбрать локальное изображение...", "Elegir imagen local...", "ローカル画像を選択…", "로컬 이미지 선택…");
		Add("点击打开字体选择窗口", "點擊開啟字型選擇視窗", "Click to open font picker", "Cliquer pour ouvrir le sélecteur de police", "Нажмите для выбора шрифта", "Haz clic para elegir fuente", "クリックしてフォント選択を開く", "클릭하여 글꼴 선택 창 열기");
		Add("导入字体文件…", "匯入字型檔案…", "Import Font File...", "Importer une police...", "Импорт файла шрифта...", "Importar archivo de fuente...", "フォントファイルをインポート…", "글꼴 파일 가져오기…");
		Add("重置字体", "重設字型", "Reset Font", "Réinitialiser la police", "Сбросить шрифт", "Restablecer fuente", "フォントをリセット", "글꼴 재설정");
		Add("音乐库", "音樂庫", "Music Library", "Bibliothèque musicale", "Медиатека", "Biblioteca musical", "音楽ライブラリ", "음악 라이브러리");
		Add("添加音乐文件夹", "新增音樂資料夾", "Add Music Folder", "Ajouter un dossier musical", "Добавить папку с музыкой", "Añadir carpeta de música", "音楽フォルダーを追加", "음악 폴더 추가");
		Add("移除选中文件夹", "移除選取資料夾", "Remove Selected Folder", "Supprimer le dossier sélectionné", "Удалить выбранную папку", "Quitar carpeta seleccionada", "選択したフォルダーを削除", "선택한 폴더 제거");
		Add("立即扫描", "立即掃描", "Scan Now", "Analyser maintenant", "Сканировать сейчас", "Escanear ahora", "今すぐスキャン", "지금 스캔");
		Add("在线搜索缺失专辑封面（手动）", "線上搜尋缺失專輯封面（手動）", "Search Missing Album Art Online (Manual)", "Rechercher les pochettes manquantes (manuel)", "Поиск недостающих обложек онлайн (вручную)", "Buscar carátulas faltantes en línea (manual)", "不足しているジャケットをオンライン検索（手動）", "누락된 앨범 아트 온라인 검색 (수동)");
		Add("关于", "關於", "About", "À propos", "О программе", "Acerca de", "アプリについて", "정보");
		Add("全格式无损音乐播放器", "全格式無損音樂播放器", "All-Format Lossless Music Player", "Lecteur audio sans perte multiformat", "Всеформатный Hi-Fi аудиоплеер", "Reproductor de música sin pérdida multiformato", "全形式対応ロスレス音楽プレーヤー", "전 포맷 무손실 음악 플레이어");
		Add("开发商：Xeon Studio", "開發商：Xeon Studio", "Developer: Xeon Studio", "Développeur : Xeon Studio", "Разработчик: Xeon Studio", "Desarrollador: Xeon Studio", "開発元：Xeon Studio", "개발사: Xeon Studio");
		Add(
			"WinUI 3 · .NET 8 · LibVLC（原生均衡器）· NAudio（实时 DSP）· ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC（原生等化器）· NAudio（即時 DSP）· ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC (Native EQ) · NAudio (Real-time DSP) · ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC (Égaliseur natif) · NAudio (DSP temps réel) · ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC (нативный эквалайзер) · NAudio (DSP в реальном времени) · ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC (ecualizador nativo) · NAudio (DSP en tiempo real) · ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC（ネイティブ EQ）· NAudio（リアルタイム DSP）· ATL · SkiaSharp · FFmpeg",
			"WinUI 3 · .NET 8 · LibVLC (네이티브 EQ) · NAudio (실시간 DSP) · ATL · SkiaSharp · FFmpeg");
		Add(
			"均衡器由 LibVLC 原生实现；空间音频采用多频段去相关混响 + 慢速调制 + 立体声宽度（独立实现的公开 DSP 技术路线）；内置无损格式转换器、多线程曲库完整性补齐，以及支持 125ms 三态动图与三行自适应歌词气泡的芙宁娜桌面宠物。",
			"等化器由 LibVLC 原生實現；空間音訊採用多頻段去相關殘響 + 慢速調變 + 立體聲寬度（獨立實現的公開 DSP 技術路線）；內建無損格式轉換器、多執行緒曲庫完整性補齊，以及支援 125ms 三態動圖與三行自適應歌詞氣泡的芙宁娜桌面寵物。",
			"EQ is natively powered by LibVLC; Spatial Audio uses multi-band decorrelated reverb + slow modulation + stereo width (independent open DSP pipeline); includes lossless Format Converter, multi-threaded Library Integrity Repair, and Furina Desktop Pet with 125ms tri-state GIFs and 3-line adaptive lyric bubbles.",
			"L'égaliseur est géré nativement par LibVLC ; l'audio spatial utilise un pipeline DSP ouvert indépendant ; inclut le convertisseur sans perte, la réparation multithread de la bibliothèque et la mascotte Furina avec GIF 125 ms et bulles de paroles adaptatives sur 3 lignes.",
			"Эквалайзер реализован нативно через LibVLC; пространственный звук использует открытый DSP-конвейер; включает конвертер без потерь, многопоточное восстановление библиотеки и настольного маскота Фурины с 125 мс GIF и 3-строчными облачками текста.",
			"El ecualizador usa LibVLC de forma nativa; el audio espacial utiliza un pipeline DSP abierto independiente; incluye conversor sin pérdida, reparación multihilo de biblioteca y mascota de escritorio de Furina con GIF de 125 ms y burbujas de letras adaptables.",
			"EQ は LibVLC によりネイティブ実装され、空間オーディオは独自 DSP パイプラインを採用。ロスレス変換、マルチスレッド曲庫補完、125ms 3状態 GIF と 3行歌詞吹き出し対応のフリーナデスクトップマスコットを搭載。",
			"이퀄라이저는 LibVLC로 기본 구현되며, 공간 음향은 독립 DSP 파이프라인을 사용합니다. 무손실 포맷 변환기, 멀티스레드 라이브러리 무결성 복구, 125ms 3상태 GIF 및 3줄 가사 말풍선 푸리나 데스크톱 펫을 포함합니다.");
		Add(
			"QQ 音乐元数据与封面接口（非官方）",
			"QQ 音樂中繼資料與封面介面（非官方）",
			"QQ Music Metadata & Cover API (Unofficial)",
			"API Métadonnées & Pochettes QQ Music (Non officielle)",
			"API метаданных и обложек QQ Music (неофициальный)",
			"API de metadatos y portadas de QQ Music (no oficial)",
			"QQ Music メタデータ＆カバー API（非公式）",
			"QQ 뮤직 메타데이터 및 커버 API (비공식)");
		Add(
			"FFmpeg（无损音频格式转换）",
			"FFmpeg（無損音訊格式轉換）",
			"FFmpeg (Lossless Audio Format Conversion)",
			"FFmpeg (Conversion audio sans perte)",
			"FFmpeg (конвертация аудио без потерь)",
			"FFmpeg (conversión de audio sin pérdida)",
			"FFmpeg（ロスレス音声フォーマット変換）",
			"FFmpeg (무손실 오디오 포맷 변환)");
		Add(
			"WinUI 3 · .NET 8 · LibVLC（原生均衡器）· NAudio（实时 DSP）· ATL",
			"WinUI 3 · .NET 8 · LibVLC（原生等化器）· NAudio（即時 DSP）· ATL",
			"WinUI 3 · .NET 8 · LibVLC (Native EQ) · NAudio (Real-time DSP) · ATL",
			"WinUI 3 · .NET 8 · LibVLC (Égaliseur natif) · NAudio (DSP temps réel) · ATL",
			"WinUI 3 · .NET 8 · LibVLC (нативный эквалайзер) · NAudio (DSP в реальном времени) · ATL",
			"WinUI 3 · .NET 8 · LibVLC (ecualizador nativo) · NAudio (DSP en tiempo real) · ATL",
			"WinUI 3 · .NET 8 · LibVLC（ネイティブ EQ）· NAudio（リアルタイム DSP）· ATL",
			"WinUI 3 · .NET 8 · LibVLC (네이티브 EQ) · NAudio (실시간 DSP) · ATL");
		Add("开源库与许可：", "開源函式庫與授權：", "Open Source Libraries & Licenses:", "Bibliothèques open source et licences :", "Библиотеки с открытым исходным кодом и лицензии:", "Bibliotecas de código abierto y licencias:", "オープンソースライブラリとライセンス：", "오픈 소스 라이브러리 및 라이선스:");
		Add("打开崩溃日志文件夹", "開啟當機記錄資料夾", "Open Crash Logs Folder", "Ouvrir le dossier des journaux de plantage", "Открыть папку журналов сбоев", "Abrir carpeta de registros de fallos", "クラッシュログフォルダーを開く", "충돌 로그 폴더 열기");
		Add(
			"均衡器由 LibVLC 原生实现；空间音频采用多频段去相关混响 + 慢速调制 + 立体声宽度（独立实现的公开 DSP 技术路线，不包含第三方插件代码）",
			"等化器由 LibVLC 原生實現；空間音訊採用多頻段去相關殘響 + 慢速調變 + 立體聲寬度（獨立實現的公開 DSP 技術路線，不包含第三方外掛程式程式碼）",
			"The equalizer is natively powered by LibVLC; Spatial Audio uses multi-band decorrelated reverb + slow modulation + stereo width (an independent open DSP pipeline with no third-party plugin code).",
			"L'égaliseur est géré nativement par LibVLC ; l'audio spatial utilise une réverbération décorrélée multibande + modulation lente + largeur stéréo (pipeline DSP indépendant sans code tiers).",
			"Эквалайзер реализован нативно через LibVLC; пространственный звук использует многополосную декоррелированную реверберацию + медленную модуляцию + ширину стерео (независимый открытый DSP-конвейер).",
			"El ecualizador funciona de forma nativa con LibVLC; el audio espacial usa reverberación decorrelacionada multibanda + modulación lenta + amplitud estéreo (pipeline DSP abierto independiente).",
			"イコライザーは LibVLC によりネイティブ実装されており、空間オーディオはマルチバンド無相関リバーブ＋低速変調＋ステレオ幅（サードパーティ製プラグインを含まない独自 DSP 実装）を採用しています。",
			"이퀄라이저는 LibVLC로 기본 구현되었으며, 공간 음향은 다중 대역 비상관 리버브 + 저속 변조 + 스테레오 폭(서드파티 플러그인 코드가 없는 독립 공개 DSP 파이프라인)을 사용합니다.");

		// 桌面宠物（看板娘）
		Add(
			"桌面宠物（看板娘）",
			"桌面寵物（看板娘）",
			"Desktop Pet (Mascot)",
			"Compagnon de bureau (Mascotte)",
			"Настольный питомец (Маскот)",
			"Mascota de escritorio",
			"デスクトップマスコット",
			"데스크톱 펫 (마스코트)");
		Add(
			"看板娘歌词气泡",
			"看板娘歌詞氣泡",
			"Mascot Lyric Bubbles",
			"Bulles de paroles de la mascotte",
			"Облачка текста для маскота",
			"Burbujas de letras de la mascota",
			"マスコット歌詞吹き出し",
			"마스코트 가사 말풍선");
		Add(
			"GIF 播放速度",
			"GIF 播放速度",
			"GIF Playback Speed",
			"Vitesse de lecture GIF",
			"Скорость анимации GIF",
			"Velocidad de reproducción GIF",
			"GIF 再生速度",
			"GIF 재생 속도");
		Add(
			"歌词气泡大小",
			"歌詞氣泡大小",
			"Lyric Bubble Size",
			"Taille des bulles de paroles",
			"Размер облачков текста",
			"Tamaño de burbujas de letras",
			"歌詞吹き出しサイズ",
			"가사 말풍선 크기");
		Add(
			"在桌面悬浮展示芙宁娜动态看板娘，支持律动同步、拖拽定位与快捷播放控制。",
			"在桌面懸浮展示芙寧娜動態看板娘，支援律動同步、拖曳定位與快捷播放控制。",
			"Display an animated Furina mascot floating on your desktop with playback sync, drag positioning, and quick controls.",
			"Affiche une mascotte Furina animée sur votre bureau avec synchronisation de lecture, glisser-déposer et contrôles rapides.",
			"Показывает анимированного маскота Фурины поверх рабочего стола с синхронизацией ритма, перетаскиванием и быстрым управлением.",
			"Muestra una mascota animada de Furina flotando en el escritorio con sincronización de ritmo, arrastre y controles rápidos.",
			"デスクトップにアニメーションするフリーナのマスコットを表示し、再生連動・ドラッグ配置・クイック操作に対応します。",
			"바탕 화면에 움직이는 푸리나 마스코트를 띄워 재생 동기화, 드래그 위치 조정 및 빠른 재생 제어를 지원합니다.");
		Add("已显示", "已顯示", "Visible", "Affiché", "Показан", "Visible", "表示中", "표시됨");
		Add("已隐藏", "已隱藏", "Hidden", "Masqué", "Скрыт", "Oculto", "非表示", "숨겨짐");
		Add("显示看板娘", "顯示看板娘", "Show Mascot", "Afficher la mascotte", "Показать маскота", "Mostrar mascota", "マスコットを表示", "마스코트 표시");
		Add("隐藏看板娘", "隱藏看板娘", "Hide Mascot", "Masquer la mascotte", "Скрыть маскота", "Ocultar mascota", "マスコットを隠す", "마스코트 숨기기");
		Add(
			"显示/隐藏看板娘",
			"顯示/隱藏看板娘",
			"Show/Hide Mascot",
			"Afficher/Masquer la mascotte",
			"Показать/скрыть маскота",
			"Mostrar/ocultar mascota",
			"マスコットを表示/非表示",
			"마스코트 표시/숨기기");
		Add("重置位置", "重設位置", "Reset Position", "Réinitialiser la position", "Сбросить позицию", "Restablecer posición", "位置をリセット", "위치 재설정");
		Add("宠物大小", "寵物大小", "Pet Size", "Taille de la mascotte", "Размер питомца", "Tamaño de mascota", "マスコットサイズ", "펫 크기");
		Add(
			"自定义动画 (GIF)",
			"自訂動畫 (GIF)",
			"Custom Animations (GIF)",
			"Animations personnalisées (GIF)",
			"Свои анимации (GIF)",
			"Animaciones personalizadas (GIF)",
			"カスタムアニメーション (GIF)",
			"사용자 지정 애니메이션 (GIF)");
		Add("待机 GIF", "待機 GIF", "Idle GIF", "GIF inactif", "GIF ожидания", "GIF en reposo", "待機 GIF", "대기 GIF");
		Add("律动 GIF", "律動 GIF", "Groove GIF", "GIF rythme", "GIF ритма", "GIF de ritmo", "リズム GIF", "리듬 GIF");
		Add("点击 GIF", "點擊 GIF", "Click GIF", "GIF clic", "GIF клика", "GIF de clic", "クリック GIF", "클릭 GIF");
		Add("选择 GIF…", "選擇 GIF…", "Choose GIF...", "Choisir un GIF...", "Выбрать GIF...", "Elegir GIF...", "GIF を選択…", "GIF 선택…");
		Add("默认内置", "預設內建", "Built-in Default", "Par défaut intégré", "Встроенный по умолчанию", "Predeterminado integrado", "内蔵デフォルト", "기본 내장");
	}
}
