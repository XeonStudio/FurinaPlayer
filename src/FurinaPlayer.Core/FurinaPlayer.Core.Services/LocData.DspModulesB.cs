namespace FurinaPlayer.Core.Services;

public static partial class Loc
{
	static partial void RegisterDspModulesB()
	{
		// Desktop Pet Bubble Styles
		Add("歌词气泡样式", "歌詞氣泡樣式", "Lyric Bubble Style", "Style des bulles de paroles", "Стиль пузырей текста", "Estilo de burbujas", "歌詞バブルスタイル", "가사 말풍선 스타일");
		Add("切换气泡样式", "切換氣泡樣式", "Switch Bubble Style", "Changer le style de bulle", "Сменить стиль пузыря", "Cambiar estilo de burbuja", "バブルスタイル切替", "말풍선 스타일 전환");
		Add("经典芙宁娜（深蓝细框）", "經典芙寧娜（深藍細框）", "Classic Furina (Navy Frame)", "Furina Classique (Cadre bleu)", "Классическая Фурина", "Furina Clásica", "クラシック・フリーナ", "클래식 푸리나");
		Add("皇冠小鱼寝（柔蓝圆框）", "皇冠小魚寢（柔藍圓框）", "Crown & Sleeping Kitty (Soft Blue)", "Couronne & Chaton (Bleu doux)", "Корона и спящий котёнок", "Corona y Gatito Durmiente", "王冠とおやすみ子猫", "왕관과 잠자는 아기고양이");
		Add("冰雪晶语（漫威晶冰）", "冰雪晶語（漫威晶冰）", "Frost Crystal Comic (Ice Dialogue)", "Cristal de Givre (BD)", "Ледяной кристалл", "Cristal de Hielo", "氷雪クリスタル", "얼음 결정 대화풍선");
		Add("阳伞水母礼帽（圆润蓝边）", "陽傘水母禮帽（圓潤藍邊）", "Parasol & Jellyfish Hat", "Ombrelle & Méduse", "Зонтик и медуза", "Parasol y Medusa", "日傘とクラゲのシルクハット", "양산과 해파리 모자");
		Add("天使小猫互动（双层蓝框）", "天使小貓互動（雙層藍框）", "Angel Kitty & Social Icons", "Chaton Ange & Icônes", "Ангел-котёнок", "Gatito Ángel", "エンジェル子猫チャット", "천사 아기고양이 채팅");

		// Multiband Compressor
		Add("三频段压缩", "三頻段壓縮", "Multiband Compressor", "Compresseur multibande", "Многополосный компрессор", "Compresor multibanda", "マルチバンドコンプレッサー", "멀티밴드 컴프레서");
		Add("多段压缩旁路", "多段壓縮旁路", "Multiband Bypassed", "Multibande contourné", "Байпас многополосного", "Multibanda en bypass", "マルチバンドバイパス", "멀티밴드 바이패스");
		Add("多段压缩已启用", "多段壓縮已啟用", "Multiband Active", "Multibande actif", "Многополосный активен", "Multibanda activo", "マルチバンド有効", "멀티밴드 활성");
		Add("恢复默认", "恢復預設", "Reset Defaults", "Réinitialiser", "Сбросить", "Restablecer", "デフォルトに戻す", "기본값 복원");
		Add("低频", "低頻", "Low", "Basses", "Низкие", "Graves", "低域", "저음");
		Add("中频", "中頻", "Mid", "Médiums", "Средние", "Medios", "中域", "중음");
		Add("高频", "高頻", "High", "Aigus", "Высокие", "Agudos", "高域", "고음");
		Add("快捷试听", "快捷試聽", "Quick Audition", "Écoute rapide", "Быстрое прослушивание", "Audición rápida", "クイック試聴", "빠른 미리듣기");
		Add("轻柔均衡", "輕柔均衡", "Gentle Balance", "Équilibre doux", "Мягкий баланс", "Equilibrio suave", "ジェントルバランス", "부드러운 밸런스");
		Add("低频收紧", "低頻收緊", "Tight Bass", "Basses serrées", "Плотный бас", "Graves firmes", "タイトベース", "단단한 저음");
		Add("明显压缩", "明顯壓縮", "Punchy", "Compression marquée", "Выраженная компрессия", "Compresión marcada", "強めの圧縮", "강한 컴프레션");
		Add("听不出变化？试试“明显压缩”，再用上方开关对比原声。点击预设会开启压缩并替换本模块参数。",
			"聽不出變化？試試「明顯壓縮」，再用上方開關對比原聲。點擊預設會開啟壓縮並替換本模組參數。",
			"Can't hear a difference? Try \"Punchy\" and toggle the power button above to A/B. Clicking a preset enables compression and replaces module parameters.",
			"Vous n'entendez pas de différence ? Essayez « Compression marquée » puis utilisez l'interrupteur ci-dessus pour comparer. Cliquer sur un préréglage active la compression et remplace les paramètres.",
			"Не слышите разницы? Попробуйте «Выраженная компрессия» и сравните с оригиналом кнопкой сверху. Выбор пресета включает компрессор и заменяет текущие параметры.",
			"¿No notas la diferencia? Prueba «Compresión marcada» y usa el interruptor superior para comparar. Al pulsar un ajuste se activa la compresión y se reemplazan los parámetros.",
			"違いが分かりませんか？「強めの圧縮」を試し、上のスイッチで原音と比較してください。プリセットを選ぶと圧縮がオンになりパラメータが置き換わります。",
			"차이가 잘 안 들리나요? '강한 컴프레션'을 선택하고 위 스위치로 원음과 비교해 보세요. 프리셋을 누르면 컴프레서가 켜지고 현재 설정이 바뀝니다.");
		Add("最大增益衰减：当前没有压缩", "最大增益衰減：目前沒有壓縮", "Max Gain Reduction: No compression active", "Réduction max : aucune compression", "Макс. подавление: нет компрессии", "Reducción máx.: sin compresión", "最大ゲインリダクション：圧縮なし", "최대 게인 리덕션: 압축 없음");
		Add("已旁路", "已旁路", "Bypassed", "Contourné", "Байпас", "En bypass", "バイパス中", "바이패스됨");
		Add("已启用", "已啟用", "Enabled", "Activé", "Включено", "Activado", "有効", "활성화됨");
		Add("三段都直接通过。打开之后，只有超过各自阈值的频段会被压缩。",
			"三段都直接通過。開啟之後，只有超過各自閾值的頻段會被壓縮。",
			"All 3 bands pass through untouched. When enabled, only bands exceeding their threshold are compressed.",
			"Les 3 bandes passent directement. Une fois activé, seules les bandes dépassant leur seuil sont compressées.",
			"Все 3 полосы проходят напрямую. После включения сжимаются только полосы выше порога.",
			"Las 3 bandas pasan sin cambios. Al activarlo, solo se comprimen las bandas que superen su umbral.",
			"3バンドすべてスルーしています。オンにすると、しきい値を超えた帯域のみ圧縮されます。",
			"3개 대역 모두 그대로 통과합니다. 켜면 각 임계값을 넘는 대역만 압축됩니다.");
		Add("三段压缩正全速运作中。超过各自阈值的频段正被动态规整。",
			"三段壓縮正全速運作中。超過各自閾值的頻段正被動態規整。",
			"All 3 compressor bands are active. Bands exceeding their threshold are dynamically controlled.",
			"Les 3 bandes de compression sont actives. Les bandes dépassant leur seuil sont régulées dynamiquement.",
			"Все 3 полосы компрессии активны. Полосы выше порога динамически выравниваются.",
			"Las 3 bandas de compresión están activas. Las bandas que superan su umbral se controlan dinámicamente.",
			"3バンドコンプレッサーが稼働中です。しきい値を超えた帯域がダイナミックに制御されています。",
			"3밴드 컴프레서가 작동 중입니다. 임계값을 초과한 대역이 다이내믹하게 제어됩니다.");
		Add("拖动分频线改变分频点，点击频段编辑", "拖曳分頻線改變分頻點，點擊頻段編輯", "Drag crossover lines to adjust, click a band to edit", "Glissez les lignes de coupure pour ajuster, cliquez sur une bande pour éditer", "Перетащите линии кроссовера, нажмите на полосу для правки", "Arrastra las líneas de cruce para ajustar, pulsa una banda para editar", "クロスオーバー線をドラッグして調整、帯域をクリックして編集", "크로스오버 선을 드래그해 조정하고 대역을 클릭해 편집");
		Add("整机", "整機", "Master", "Global", "Общие", "General", "マスター", "마스터");
		Add("分频点把频谱切开", "分頻點把頻譜切開", "Crossovers split the spectrum", "Les fréquences de coupure divisent le spectre", "Кроссоверы делят спектр", "Los cruces dividen el espectro", "クロスオーバーで帯域を分割", "크로스오버로 스펙트럼 분할");
		Add("低频分频", "低頻分頻", "Low Crossover", "Coupure basse", "НЧ кроссовер", "Cruce grave", "低域クロスオーバー", "저음 크로스오버");
		Add("高频分频", "高頻分頻", "High Crossover", "Coupure haute", "ВЧ кроссовер", "Cruce agudo", "高域クロスオーバー", "고음 크로스오버");
		Add("湿声", "濕聲", "Wet Mix", "Mix humide", "Подмес (Wet)", "Mezcla Wet", "ウェット", "웻 믹스");
		Add("中频启用", "中頻啟用", "Mid Band Active", "Bande médium active", "Средняя полоса вкл.", "Banda media activa", "中域を有効", "중음 대역 활성");
		Add("开", "開", "On", "Activé", "Вкл", "Sí", "オン", "켜짐");
		Add("关", "關", "Off", "Désactivé", "Выкл", "No", "オフ", "꺼짐");
		Add("阈值", "閾值", "Threshold", "Seuil", "Порог", "Umbral", "スレッショルド", "스레시홀드");
		Add("比率", "比率", "Ratio", "Ratio", "Соотношение", "Relación", "レシオ", "비율");
		Add("快慢", "快慢", "Timing", "Vitesse", "Скорость", "Velocidad", "タイミング", "타이밍");
		Add("只作用于中频", "只作用於中頻", "Applies to Mid only", "S'applique aux médiums uniquement", "Только для средних", "Solo para medios", "中域のみに適用", "중음에만 적용");
		Add("只作用于{0}", "只作用於{0}", "Applies to {0} only", "S'applique à {0} uniquement", "Только для: {0}", "Solo para {0}", "{0}のみに適用", "{0}에만 적용");
		Add("启动", "啟動", "Attack", "Attaque", "Атака", "Ataque", "アタック", "어택");
		Add("释放", "釋放", "Release", "Relâchement", "Спад", "Liberación", "リリース", "릴리즈");

		// Room Correction
		Add("FIR / 房间校正", "FIR / 房間校正", "FIR / Room Correction", "FIR / Correction acoustique", "FIR / Коррекция комнаты", "FIR / Corrección de sala", "FIR / ルーム補正", "FIR / 룸 보정");
		Add("空间处理 / IR", "空間處理 / IR", "Spatial / IR", "Spatial / IR", "Пространство / IR", "Espacial / IR", "空間処理 / IR", "공간 처리 / IR");
		Add("房间与音箱校正", "房間與音箱校正", "Room & Speaker Correction", "Correction de pièce et d'enceintes", "Коррекция помещения и АС", "Corrección de sala y altavoces", "ルーム＆スピーカー補正", "룸 및 스피커 보정");
		Add("导入 REW 或其他测量软件生成的校正滤波器。",
			"匯入 REW 或其他測量軟體產生的校正濾波器。",
			"Import correction filters generated by REW or other measurement software.",
			"Importez les filtres de correction générés par REW ou un autre logiciel de mesure.",
			"Импортируйте корректирующие фильтры из REW или других программ измерения.",
			"Importa filtros de corrección generados con REW u otro software de medición.",
			"REWや他の測定ソフトで生成した補正フィルターを読み込みます。",
			"REW 또는 다른 측정 소프트웨어에서 생성한 보정 필터를 가져옵니다.");
		Add("准备音频校正", "準備音訊校正", "Ready for Correction", "Prêt pour la correction", "Готово к коррекции", "Listo para corrección", "補正の準備完了", "보정 준비 완료");
		Add("校正正在应用", "校正正在套用", "Correction Active", "Correction active", "Коррекция активна", "Corrección activa", "補正適用中", "보정 적용 중");
		Add("滤波器已准备", "濾波器已就緒", "Filter Loaded", "Filtre chargé", "Фильтр загружен", "Filtro cargado", "フィルター読込済み", "필터 로드됨");
		Add("WAV · FLAC · AIFF · ALAC(.m4a) · ZIP, ZIP 支持 Convolver .cfg 及其引用的 .pcm / .dbl, 按播放采样率与声道自动匹配。",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP，ZIP 支援 Convolver .cfg 及其引用的 .pcm / .dbl，依播放取樣率與聲道自動匹配。",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP (supports Convolver .cfg and referenced .pcm / .dbl), auto-matched by playback sample rate and channels.",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP (prend en charge Convolver .cfg et .pcm / .dbl référencés), adapté automatiquement à la fréquence et aux canaux.",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP (поддержка Convolver .cfg и .pcm / .dbl), автоподбор по частоте дискретизации и каналам.",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP (admite Convolver .cfg y .pcm / .dbl), ajustado automáticamente a la frecuencia y canales.",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP（Convolver .cfg および参照先 .pcm / .dbl 対応）、再生サンプルレート・チャンネルに自動一致。",
			"WAV · FLAC · AIFF · ALAC(.m4a) · ZIP(Convolver .cfg 및 참조 .pcm / .dbl 지원), 재생 샘플레이트와 채널에 자동 매칭됩니다.");
		Add("实际信号路径", "實際訊號路徑", "Actual Signal Path", "Chemin du signal réel", "Фактический путь сигнала", "Ruta real de señal", "実際の信号経路", "실제 신호 경로");
		Add("等待宿主确认", "等待宿主確認", "Awaiting Host", "En attente de l'hôte", "Ожидание хоста", "Esperando al host", "ホスト確認待ち", "호스트 확인 대기 중");
		Add("来自音频宿主", "來自音訊宿主", "From Audio Host", "Depuis l'hôte audio", "От аудиохоста", "Desde el host de audio", "オーディオホストから", "오디오 호스트에서");
		Add("音频输入", "音訊輸入", "Audio Input", "Entrée audio", "Аудиовход", "Entrada de audio", "オーディオ入力", "오디오 입력");
		Add("内置 DSP", "內建 DSP", "Built-in DSP", "DSP intégré", "Встроенный DSP", "DSP integrado", "内蔵 DSP", "내장 DSP");
		Add("卷积插槽", "卷積插槽", "Convolver Slot", "Slot de convolution", "Слот конвольвера", "Ranura de convolución", "コンボルバースロット", "컨볼버 슬롯");
		Add("设备输出", "裝置輸出", "Device Output", "Sortie périphérique", "Выход устройства", "Salida de dispositivo", "デバイス出力", "장치 출력");
		Add("已载入 IR 脉冲文件", "已載入 IR 脈衝檔案", "IR Impulse File Loaded", "Fichier IR chargé", "Файл IR загружен", "Archivo IR cargado", "IR インパルス読込済み", "IR 임펄스 파일 로드됨");
		Add("48 kHz · 立体声 · 65,536 Taps · 1,365 ms", "48 kHz · 立體聲 · 65,536 Taps · 1,365 ms", "48 kHz · Stereo · 65,536 Taps · 1,365 ms", "48 kHz · Stéréo · 65 536 Taps · 1 365 ms", "48 кГц · Стерео · 65 536 отсчётов · 1 365 мс", "48 kHz · Estéreo · 65,536 Taps · 1,365 ms", "48 kHz · ステレオ · 65,536 Taps · 1,365 ms", "48 kHz · 스테레오 · 65,536 Taps · 1,365 ms");
		Add("{0:F1} kHz · 立体声 · {1:N0} Taps · {2:F1} ms", "{0:F1} kHz · 立體聲 · {1:N0} Taps · {2:F1} ms", "{0:F1} kHz · Stereo · {1:N0} Taps · {2:F1} ms", "{0:F1} kHz · Stéréo · {1:N0} Taps · {2:F1} ms", "{0:F1} кГц · Стерео · {1:N0} отсч. · {2:F1} мс", "{0:F1} kHz · Estéreo · {1:N0} Taps · {2:F1} ms", "{0:F1} kHz · ステレオ · {1:N0} Taps · {2:F1} ms", "{0:F1} kHz · 스테레오 · {1:N0} Taps · {2:F1} ms");
		Add("重新导入", "重新匯入", "Re-import", "Réimporter", "Повторный импорт", "Reimportar", "再読み込み", "다시 가져오기");
		Add("清除", "清除", "Clear", "Effacer", "Очистить", "Limpiar", "クリア", "지우기");
		Add("校正增益 (Correction Trim)", "校正增益 (Correction Trim)", "Correction Trim", "Gain de correction (Trim)", "Усиление коррекции (Trim)", "Ajuste de corrección (Trim)", "補正トリム (Correction Trim)", "보정 트림 (Correction Trim)");
		Add("快捷增益:", "快捷增益:", "Quick Trim:", "Trim rapide :", "Быстрый Trim:", "Trim rápido:", "クイックトリム:", "빠른 트림:");
		Add("检测到卷积输出存在削波风险！建议将校正增益衰减至 -6 dB 以下，并在 Headroom 页面配置数字余量。",
			"偵測到卷積輸出存在削波風險！建議將校正增益衰減至 -6 dB 以下，並在 Headroom 頁面配置數位餘量。",
			"Clipping risk detected on convolver output! Consider attenuating trim below -6 dB and configuring digital headroom.",
			"Risque d'écrêtage détecté sur la sortie de convolution ! Atténuez le gain sous -6 dB et configurez la marge (Headroom).",
			"Обнаружен риск клиппинга на выходе конвольвера! Снизьте усиление ниже -6 дБ и настройте запас (Headroom).",
			"¡Riesgo de recorte en la salida del convolver! Atenúa la ganancia por debajo de -6 dB y configura el margen (Headroom).",
			"コンボルバー出力にクリッピングの恐れがあります。トリムを -6 dB 以下に下げ、Headroom ページでヘッドルームを設定してください。",
			"컨볼버 출력에 클리핑 위험이 감지되었습니다! 트림을 -6 dB 이하로 낮추고 Headroom 페이지에서 디지털 헤드룸을 설정하세요.");
		Add("扬声器补偿", "揚聲器補償", "Speaker Compensation", "Compensation d'enceintes", "Компенсация АС", "Compensación de altavoces", "スピーカー補正", "스피커 보상");
		Add("左右增益、延迟与反相", "左右增益、延遲與反相", "L/R gain, delay & polarity", "Gain G/D, délai et polarité", "Усиление Л/П, задержка и фаза", "Ganancia I/D, retardo y polaridad", "左右ゲイン・遅延・極性", "좌우 게인, 지연 및 위상 반전");
		Add("余量与削波", "餘量與削波", "Headroom & Clipping", "Marge et écrêtage", "Запас и клиппинг", "Margen y recorte", "ヘッドルーム＆クリッピング", "헤드룸 및 클리핑");
		Add("检查实际输出峰值", "檢查實際輸出峰值", "Check actual output peaks", "Vérifier les crêtes de sortie", "Проверка пиков выхода", "Comprobar picos de salida", "実際の出力ピークを確認", "실제 출력 피크 확인");
		Add("保存校正场景", "儲存校正場景", "Save Correction Scene", "Enregistrer la scène", "Сохранить сцену коррекции", "Guardar escena", "補正シーンを保存", "보정 장면 저장");
		Add("绑定输出设备与对比试听", "綁定輸出裝置與對比試聽", "Bind output device & A/B audition", "Lier le périphérique et comparer", "Привязка устройства и A/B тест", "Vincular dispositivo y prueba A/B", "出力デバイスの紐付けとA/B試聴", "출력 장치 연결 및 비교 청취");
		Add("用 REW 制作音箱校正", "用 REW 製作音箱校正", "Create Speaker Correction with REW", "Créer une correction avec REW", "Создание коррекции в REW", "Crear corrección con REW", "REW でスピーカー補正を作成する", "REW로 스피커 보정 만들기");
		Add("在 REW 中测量与生成滤波器，再由 FurinaPlayer 应用于播放。需要带校准文件的测量麦克风；本页不会录音或自动测量。",
			"在 REW 中測量與產生濾波器，再由 FurinaPlayer 套用於播放。需要帶校準檔的測量麥克風；本頁不會錄音或自動測量。",
			"Measure and generate filters in REW, then apply them in FurinaPlayer. Requires a calibrated measurement mic; this page does not record or auto-measure.",
			"Mesurez et générez les filtres dans REW, puis appliquez-les dans FurinaPlayer. Nécessite un micro calibré ; cette page n'enregistre pas.",
			"Измерьте и создайте фильтры в REW, затем примените их в FurinaPlayer. Требуется калиброванный микрофон; эта страница не ведёт запись.",
			"Mide y genera filtros en REW y aplícalos en FurinaPlayer. Requiere micrófono calibrado; esta página no graba ni mide automáticamente.",
			"REW で測定・フィルター生成を行い、FurinaPlayer で適用します。校正ファイル付き測定マイクが必要です。このページでは録音や自動測定は行いません。",
			"REW에서 측정 및 필터를 생성한 뒤 FurinaPlayer에서 재생에 적용합니다. 보정 파일이 있는 측정 마이크가 필요하며, 이 페이지에서는 녹음이나 자동 측정을 하지 않습니다.");
		Add("测量左右音箱", "測量左右音箱", "Measure Left & Right Speakers", "Mesurer les enceintes G et D", "Измерение левой и правой АС", "Medir altavoces I y D", "左右スピーカーを測定", "좌우 스피커 측정");
		Add("在 REW 中载入麦克风校准文件，将麦克风放在听音位置，与耳朵同高，检查输入和输出电平，从较低音量开始，分别测量左、右音箱并保存。",
			"在 REW 中載入麥克風校準檔，將麥克風放在聽音位置，與耳朵同高，檢查輸入和輸出電平，從較低音量開始，分別測量左、右音箱並儲存。",
			"Load the mic calibration file in REW, place the mic at ear height in the listening position, check I/O levels, start at low volume, and measure L/R speakers separately.",
			"Chargez la calibration du micro dans REW, placez-le à hauteur d'oreille, vérifiez les niveaux, commencez à faible volume et mesurez G et D séparément.",
			"Загрузите калибровку микрофона в REW, установите его на уровне ушей в точке прослушивания, проверьте уровни и измерьте Л/П каналы отдельно.",
			"Carga la calibración del micro en REW, colócalo a la altura del oído, revisa niveles, empieza a volumen bajo y mide I y D por separado.",
			"REW にマイク校正ファイルを読み込み、耳の高さの視聴位置に配置して入出力レベルを確認し、小音量から左右別々に測定して保存します。",
			"REW에 마이크 보정 파일을 불러오고 청취 위치의 귀 높이에 마이크를 둔 뒤 입출력 레벨을 확인하고 낮은 볼륨부터 좌/우 스피커를 각각 측정해 저장합니다.");
		Add("生成校正", "產生校正", "Generate Correction", "Générer la correction", "Генерация коррекции", "Generar corrección", "補正を生成", "보정 필터 생성");
		Add("在 REW 的 EQ 窗口设置目标曲线和校正频段，优先处理低频峰值，限制提升量，保留音箱自然低频衰减，不强行填平深谷。左右声道分别生成滤波器。",
			"在 REW 的 EQ 視窗設定目標曲線與校正頻段，優先處理低頻峰值，限制提升量，保留音箱自然低頻衰減，不強行填平深谷。左右聲道分別產生濾波器。",
			"Set target curve and EQ range in REW's EQ window. Prioritize bass peaks, limit boost, preserve natural roll-off, and avoid filling deep nulls. Generate L/R filters separately.",
			"Définissez la courbe cible dans la fenêtre EQ de REW. Traitez d'abord les pics graves, limitez le boost et ne comblez pas les creux profonds. Générez G et D séparément.",
			"Настройте целевую кривую в окне EQ в REW. Уберите пики НЧ, ограничьте подъём, сохраните естественный спад и не заполняйте глубокие провалы. Создайте фильтры для Л и П.",
			"Configura la curva objetivo en la ventana EQ de REW. Prioriza picos graves, limita el realce, respeta la caída natural y no rellenes valles profundos. Genera I y D por separado.",
			"REW の EQ ウィンドウでターゲットカーブと補正帯域を設定し、低域ピークの抑制を優先、ブースト量を制限して自然なロールオフを残します。左右別々に生成してください。",
			"REW의 EQ 창에서 목표 곡선과 보정 대역을 설정하고 저역 피크 억제를 우선하며 부스트량을 제한해 자연스러운 롤오프를 유지하세요. 좌우 채널 각각 필터를 생성합니다.");
		Add("导出立体声滤波器", "匯出立體聲濾波器", "Export Stereo Filter", "Exporter le filtre stéréo", "Экспорт стереофильтра", "Exportar filtro estéreo", "ステレオフィルターをエクスポート", "스테레오 필터 내보내기");
		Add("选择下方导出菜单，使用 Stereo, 32-bit Float，左、右分别对应刚才的测量，关闭峰值归一化以保留校正增益关系。导出校正滤波器，不要导出原始房间响应。",
			"選擇下方匯出選單，使用 Stereo, 32-bit Float，左、右分別對應剛才的測量，關閉峰值歸一化以保留校正增益關係。匯出校正濾波器，不要匯出原始房間響應。",
			"Use the export menu below with Stereo, 32-bit Float matching your L/R measurements. Disable peak normalization to preserve gain alignment. Export the correction filter, not raw room response.",
			"Utilisez le menu d'export ci-dessous en Stereo, 32-bit Float pour G/D. Désactivez la normalisation de crête. Exportez le filtre de correction, pas la réponse brute.",
			"Выберите экспорт ниже: Stereo, 32-bit Float для Л/П измерений. Отключите нормализацию пиков. Экспортируйте именно фильтр коррекции, а не сырой отклик.",
			"Usa el menú inferior en Stereo, 32-bit Float para I/D. Desactiva la normalización de pico. Exporta el filtro de corrección, no la respuesta bruta.",
			"下のエクスポートメニューから Stereo, 32-bit Float を選び、左右の測定に対応させてピーク正規化をオフにして出力してください（生の室内応答ではなく補正フィルターを出力）。",
			"아래 내보내기 메뉴에서 Stereo, 32-bit Float를 선택해 좌우 측정을 매핑하고 피크 정규화를 꺼서 보정 게인 관계를 유지한 채 보정 필터를 내보내세요.");
		Add("每声道最多 131,072 个采样点，总计不超过 262,144。为实际使用的采样率分别导出（如 44.1 / 48 / 96 kHz），可将 WAV 打成 ZIP，每种采样率只放一份立体声滤波器；分开的左右 IR 可用 Convolver .cfg 明确对应，并连同 IR 一起打包。",
			"每聲道最多 131,072 個取樣點，總計不超過 262,144。為實際使用的取樣率分別匯出（如 44.1 / 48 / 96 kHz），可將 WAV 打成 ZIP，每種取樣率只放一份立體聲濾波器；分開的左右 IR 可用 Convolver .cfg 明確對應，並連同 IR 一起打包。",
			"Up to 131,072 samples/channel (262,144 total). Export per target rate (e.g. 44.1 / 48 / 96 kHz) and optionally zip them (one stereo filter per rate); separate L/R IRs can be mapped via Convolver .cfg inside the ZIP.",
			"Jusqu'à 131 072 échantillons/canal (262 144 au total). Exportez par fréquence (44,1 / 48 / 96 kHz) dans un ZIP (un filtre stéréo par fréquence) ou mappez G/D via Convolver .cfg.",
			"До 131 072 отсчётов на канал (262 144 всего). Экспортируйте для нужных частот (44.1 / 48 / 96 кГц) в ZIP (один стереофильтр на частоту) или укажите раздельные Л/П через Convolver .cfg.",
			"Hasta 131 072 muestras/canal (262 144 en total). Exporta por frecuencia (44.1 / 48 / 96 kHz) en un ZIP (un filtro estéreo por frecuencia) o mapea I/D con Convolver .cfg.",
			"1チャンネル最大 131,072 サンプル（合計 262,144 まで）。使用するサンプルレート別（44.1 / 48 / 96 kHz 等）に出力し ZIP にまとめられます。左右別 IR は Convolver .cfg で対応付けて同梱可能です。",
			"채널당 최대 131,072샘플(총 262,144개 이하). 사용하는 샘플레이트별(44.1 / 48 / 96 kHz 등)로 내보내 ZIP으로 묶을 수 있으며, 분리된 좌/우 IR은 Convolver .cfg로 매핑해 함께 압축할 수 있습니다.");
		Add("启用、试听与复测", "啟用、試聽與複測", "Enable, Audition & Re-measure", "Activer, écouter et remesurer", "Включение, прослушивание и проверка", "Activar, probar y remedir", "有効化・試聴・再測定", "활성화, 청취 및 재측정");
		Add("导入后使用“安全启用 FIR”，低音量试听并观察削波；预留 6 dB 只是起点，比较前后时匹配听感音量。复测信号须经过 FurinaPlayer：可用 REW 导出的测量扫频文件在 FurinaPlayer 播放，再按 REW 离线测量流程录制、分析；直接由 REW 输出的扫频不会经过这里的校正。",
			"匯入後使用「安全啟用 FIR」，低音量試聽並觀察削波；預留 6 dB 只是起點，比較前後時匹配聽感音量。複測訊號須經過 FurinaPlayer：可用 REW 匯出的測量掃頻檔在 FurinaPlayer 播放，再按 REW 離線測量流程錄製、分析；直接由 REW 輸出的掃頻不會經過這裡的校正。",
			"After import, use \"Safe Enable FIR\", audition at low volume, and watch for clipping; 6 dB headroom is a starting point—level-match when A/B comparing. Re-measurement sweeps must play through FurinaPlayer (export sweep from REW) so the correction is active.",
			"Après l'import, utilisez « Activer FIR (Sûr) », écoutez à faible volume et surveillez l'écrêtage ; égalisez le volume pour comparer. Les balayages de contrôle doivent passer par FurinaPlayer.",
			"После импорта нажмите «Безопасное вкл. FIR», слушайте на малой громкости, следите за клиппингом и выравнивайте громкость при сравнении. Контрольный свип нужно воспроизводить через FurinaPlayer.",
			"Tras importar, usa «Activar FIR seguro», prueba a volumen bajo y vigila el recorte; iguala el volumen al comparar. El barrido de verificación debe reproducirse a través de FurinaPlayer.",
			"読込後は「安全に FIR を有効化」を使い、小音量で試聴してクリッピングを確認してください。再測定のスイープ信号は FurinaPlayer 経由で再生する必要があります。",
			"가져온 뒤 'FIR 안전 활성화'를 사용해 낮은 볼륨으로 청취하며 클리핑을 확인하세요. 재측정 스윕 신호는 반드시 FurinaPlayer를 통해 재생해야 보정이 적용됩니다.");
		Add("已有校正滤波器？使用上方“导入 IR”。", "已有校正濾波器？使用上方「匯入 IR」。", "Already have a correction filter? Use \"Import IR\" above.", "Vous avez déjà un filtre ? Utilisez « Importer IR » ci-dessus.", "Уже есть фильтр коррекции? Нажмите «Импорт IR» выше.", "¿Ya tienes un filtro? Usa «Importar IR» arriba.", "すでに補正フィルターをお持ちですか？上の「IR をインポート」を使用してください。", "이미 보정 필터가 있나요? 위의 'IR 가져오기'를 사용하세요.");
		Add("已启用校正。实际改善需要试听与复测确认；更换音箱、摆位或听音位置后应重新测量。",
			"已啟用校正。實際改善需要試聽與複測確認；更換音箱、擺位或聽音位置後應重新測量。",
			"Correction enabled. Verify improvement by listening and re-measuring; re-measure if speakers, placement, or listening position change.",
			"Correction activée. Vérifiez l'amélioration à l'écoute et par mesure ; remesurez en cas de changement d'enceintes ou de placement.",
			"Коррекция включена. Оцените результат на слух и повторным замером; перемерьте при смене АС или их положения.",
			"Corrección activada. Verifica la mejora escuchando y remidiendo; vuelve a medir si cambias altavoces o posición.",
			"補正が有効です。試聴と再測定で効果を確認してください。スピーカーや配置、視聴位置を変えた場合は再測定が必要です。",
			"보정이 활성화되었습니다. 청취 및 재측정으로 개선 여부를 확인하세요. 스피커나 배치, 청취 위치가 바뀌면 다시 측정해야 합니다.");
		Add("滤波器已载入。确认输出仍是测量时的音箱与摆位，再启用。",
			"濾波器已載入。確認輸出仍是測量時的音箱與擺位，再啟用。",
			"Filter loaded. Confirm the output still matches the measured speakers and placement before enabling.",
			"Filtre chargé. Vérifiez que la sortie correspond aux enceintes et au placement mesurés avant d'activer.",
			"Фильтр загружен. Перед включением убедитесь, что используются те же АС и расстановка.",
			"Filtro cargado. Confirma que la salida coincide con los altavoces y ubicación medidos antes de activar.",
			"フィルターが読み込まれました。測定時と同じスピーカー・配置であることを確認してから有効にしてください。",
			"필터가 로드되었습니다. 출력이 측정 당시의 스피커 및 배치와 일치하는지 확인한 후 활성화하세요.");
		Add("导入 IR", "匯入 IR", "Import IR", "Importer IR", "Импорт IR", "Importar IR", "IR をインポート", "IR 가져오기");
		Add("停用 FIR", "停用 FIR", "Disable FIR", "Désactiver FIR", "Откл. FIR", "Desactivar FIR", "FIR を無効化", "FIR 비활성화");
		Add("安全启用 FIR", "安全啟用 FIR", "Safe Enable FIR", "Activer FIR (Sûr)", "Безоп. вкл. FIR", "Activar FIR seguro", "安全に FIR を有効化", "FIR 안전 활성화");
		Add("启用 FIR", "啟用 FIR", "Enable FIR", "Activer FIR", "Вкл. FIR", "Activar FIR", "FIR を有効化", "FIR 활성화");
		Add("无 IR", "無 IR", "No IR", "Aucun IR", "Нет IR", "Sin IR", "IR なし", "IR 없음");
		Add("已载入", "已載入", "Loaded", "Chargé", "Загружен", "Cargado", "読込済み", "로드됨");

		// SDM / DSD
		Add("DSP 模块", "DSP 模組", "DSP Module", "Module DSP", "Модуль DSP", "Módulo DSP", "DSP モジュール", "DSP 모듈");
		Add("FurinaPlayer SDM / DSD (测试版)", "FurinaPlayer SDM / DSD (測試版)", "FurinaPlayer SDM / DSD (Beta)", "FurinaPlayer SDM / DSD (Bêta)", "FurinaPlayer SDM / DSD (Бета)", "FurinaPlayer SDM / DSD (Beta)", "FurinaPlayer SDM / DSD (ベータ)", "FurinaPlayer SDM / DSD (베타)");
		Add("当前不是 DSD 源", "目前不是 DSD 訊源", "Not a DSD Source", "Source non DSD", "Не DSD источник", "No es fuente DSD", "DSD ソースではありません", "DSD 소스가 아님");
		Add("真实信号路径", "真實訊號路徑", "Actual Signal Path", "Chemin réel du signal", "Реальный путь сигнала", "Ruta real de señal", "実際の信号経路", "실제 신호 경로");
		Add("Requested: 关闭 SDM", "Requested: 關閉 SDM", "Requested: SDM Off", "Demandé : SDM désactivé", "Запрос: SDM выкл.", "Solicitado: SDM desactivado", "要求: SDM オフ", "요청됨: SDM 끔");
		Add("高级", "進階", "Advanced", "Avancé", "Дополнительно", "Avanzado", "詳細", "고급");
		Add("收起", "收合", "Collapse", "Réduire", "Свернуть", "Contraer", "折りたたむ", "접기");
		Add("SDM 模式", "SDM 模式", "SDM Mode", "Mode SDM", "Режим SDM", "Modo SDM", "SDM モード", "SDM 모드");
		Add("关闭 SDM", "關閉 SDM", "SDM Off", "SDM désactivé", "SDM выкл.", "SDM desactivado", "SDM オフ", "SDM 끔");
		Add("PCM 和 FurinaPlayer SRC 保持独立；不会启动 DSD/SDM 输出。",
			"PCM 與 FurinaPlayer SRC 保持獨立；不會啟動 DSD/SDM 輸出。",
			"PCM and FurinaPlayer SRC remain independent; DSD/SDM output is not started.",
			"PCM et FurinaPlayer SRC restent indépendants ; la sortie DSD/SDM n'est pas lancée.",
			"PCM и FurinaPlayer SRC работают независимо; вывод DSD/SDM не запускается.",
			"PCM y FurinaPlayer SRC permanecen independientes; no se inicia salida DSD/SDM.",
			"PCM と FurinaPlayer SRC は独立したまま、DSD/SDM 出力は開始されません。",
			"PCM과 FurinaPlayer SRC는 독립적으로 유지되며 DSD/SDM 출력을 시작하지 않습니다.");
		Add("常用实时目标；PCM -> SDM 由 FurinaPlayer SDM 独立编排实际超频。",
			"常用即時目標；PCM -> SDM 由 FurinaPlayer SDM 獨立編排實際超頻。",
			"Common real-time target; PCM -> SDM oversampling is orchestrated independently by FurinaPlayer SDM.",
			"Cible temps réel courante ; PCM -> SDM est orchestré indépendamment par FurinaPlayer SDM.",
			"Типовая цель реального времени; PCM -> SDM независимо обрабатывается модулем FurinaPlayer SDM.",
			"Objetivo habitual en tiempo real; PCM -> SDM es gestionado de forma independiente por FurinaPlayer SDM.",
			"標準のリアルタイムターゲット。PCM -> SDM は FurinaPlayer SDM が独立して制御します。",
			"표준 실시간 타깃입니다. PCM -> SDM은 FurinaPlayer SDM이 독립적으로 처리합니다.");
		Add("调制器 / 稳定性", "調變器 / 穩定性", "Modulator / Stability", "Modulateur / Stabilité", "Модулятор / Стабильность", "Modulador / Estabilidad", "モジュレーター / 安定性", "모듈레이터 / 안정성");
		Add("3 阶异常 NTF，固定 6 dB 调制安全余量；稳定性优先。",
			"3 階異常 NTF，固定 6 dB 調變安全餘量；穩定性優先。",
			"3rd-order NTF with fixed 6 dB modulation headroom; stability first.",
			"NTF d'ordre 3 avec marge de modulation fixe de 6 dB ; priorité à la stabilité.",
			"NTF 3-го порядка с запасом модуляции 6 дБ; приоритет стабильности.",
			"NTF de 3.er orden con margen de modulación fijo de 6 dB; prioridad estabilidad.",
			"3次 NTF、固定 6 dB 変調ヘッドルーム；安定性優先。",
			"3차 NTF, 고정 6 dB 변조 헤드룸; 안정성 우선.");
		Add("量化策略", "量化策略", "Quantizer Strategy", "Stratégie de quantification", "Стратегия квантования", "Estrategia de cuantización", "量子化ストラテジー", "양자화 전략");
		Add("未启用量化，负载与当前 Quality 相同。",
			"未啟用量化，負載與目前 Quality 相同。",
			"Quantization disabled; CPU load matches current Quality setting.",
			"Quantification désactivée ; la charge correspond à la qualité actuelle.",
			"Квантование отключено; нагрузка соответствует текущему качеству.",
			"Cuantización desactivada; la carga equivale a la calidad actual.",
			"量子化は無効です。負荷は現在の Quality 設定と同じです。",
			"양자화가 비활성화되어 부하가 현재 Quality 설정과 동일합니다.");
		Add("SDM 声音风格", "SDM 聲音風格", "SDM Sound Profile", "Profil sonore SDM", "Звуковой профиль SDM", "Perfil de sonido SDM", "SDM サウンドプロファイル", "SDM 사운드 프로필");
		Add("性能相位、频响完全收敛；保留平滑顺畅和甜蜜厚度，适合作为中性基准。",
			"效能相位、頻響完全收斂；保留平滑順暢與甜美厚度，適合作為中性基準。",
			"Phase and frequency response fully converged; smooth, warm, and suitable as a neutral reference.",
			"Phase et réponse en fréquence convergées ; fluide et chaleureux, idéal comme référence neutre.",
			"Фаза и АЧХ полностью сошлись; гладкое и тёплое звучание, подходит как нейтральный эталон.",
			"Fase y respuesta convergidas; suave y cálido, ideal como referencia neutra.",
			"位相と周波数特性が完全に収束。滑らかで温かみがあり、ニュートラルな基準に最適です。",
			"위상과 주파수 응답이 완전히 수렴되었으며 부드럽고 따뜻한 중립 레퍼런스로 적합합니다.");
		Add("FIR 升频档", "FIR 升頻檔", "FIR Oversampling Tier", "Palier de suréchantillonnage FIR", "Ступень передискретизации FIR", "Nivel de sobremuestreo FIR", "FIR オーバーサンプリング", "FIR 업샘플링 단계");
		Add("使用声学频域滤波插头；默认快。", "使用聲學頻域濾波外掛；預設快。", "Uses acoustic frequency-domain filtering; fast by default.", "Utilise le filtrage fréquentiel acoustique ; rapide par défaut.", "Использует частотную фильтрацию; быстрый режим по умолчанию.", "Usa filtrado acústico en frecuencia; rápido por defecto.", "音響周波数領域フィルタリングを使用；デフォルトで高速。", "음향 주파수 영역 필터링을 사용하며 기본값이 빠릅니다.");
		Add("GPU 加速 (若支持)", "GPU 加速 (若支援)", "GPU Acceleration (If Supported)", "Accélération GPU (si prise en charge)", "Ускорение GPU (если доступно)", "Aceleración GPU (si es compatible)", "GPU アクセラレーション (対応時)", "GPU 가속 (지원 시)");
		Add("CPU 实时编排已进入第一版 PCM -> SDM DoP。",
			"CPU 即時編排已進入第一版 PCM -> SDM DoP。",
			"Real-time CPU orchestration active for PCM -> SDM DoP.",
			"L'orchestration CPU temps réel est active pour PCM -> SDM DoP.",
			"Оркестрация CPU в реальном времени активна для PCM -> SDM DoP.",
			"Orquestación de CPU en tiempo real activa para PCM -> SDM DoP.",
			"PCM -> SDM DoP の CPU リアルタイム処理が有効です。",
			"PCM -> SDM DoP용 CPU 실시간 오케스트레이션이 활성화되었습니다.");
		Add("DoP passthrough / 预调制", "DoP passthrough / 預調變", "DoP Passthrough / Pre-modulation", "DoP passthrough / Pré-modulation", "DoP passthrough / Пре-модуляция", "DoP passthrough / Premodulación", "DoP パススルー / プリモジュレーション", "DoP 패스스루 / 프리 모듈레이션");
		Add("DSD512 预调制", "DSD512 預調變", "DSD512 Pre-modulation", "Pré-modulation DSD512", "Пре-модуляция DSD512", "Premodulación DSD512", "DSD512 プリモジュレーション", "DSD512 프리 모듈레이션");
		Add("实验：DSD512 预调制 opt-in！提升单端调制限制；默认关，开启后需按切动进入准入。",
			"實驗：DSD512 預調變 opt-in！提升單端調變限制；預設關，開啟後需按切換進入准入。",
			"Experimental: DSD512 pre-modulation opt-in! Raises modulation limit; off by default.",
			"Expérimental : pré-modulation DSD512 optionnelle ! Augmente la limite de modulation ; désactivée par défaut.",
			"Эксперимент: пре-модуляция DSD512 (opt-in)! Повышает предел модуляции; по умолчанию выкл.",
			"Experimental: ¡premodulación DSD512 opcional! Amplía el límite de modulación; desactivada por defecto.",
			"実験的：DSD512 プリモジュレーション（オプトイン）！変調上限を引き上げます（デフォルトオフ）。",
			"실험적: DSD512 프리 모듈레이션(opt-in)! 변조 한계를 높입니다. 기본값은 끔입니다.");
		Add("环路积分稳定性与防溢出保护 (Guard Active)",
			"環路積分穩定性與防溢位保護 (Guard Active)",
			"Loop Integrator Stability & Overflow Guard (Guard Active)",
			"Stabilité de l'intégrateur et protection anti-débordement (Guard Active)",
			"Стабильность интегратора и защита от переполнения (Guard Active)",
			"Estabilidad del integrador y protección contra desbordamiento (Guard Active)",
			"ループ積分器安定性＆オーバーフロー保護 (Guard Active)",
			"루프 적분기 안정성 및 오버플로 보호 (Guard Active)");
		Add("动态监测 5 阶环路积分器状态，当反馈超过 ±1.55 时瞬间触发硬件自恢复，消除直流漂移与高频爆音风险。",
			"動態監測 5 階環路積分器狀態，當回饋超過 ±1.55 時瞬間觸發硬體自恢復，消除直流漂移與高頻爆音風險。",
			"Dynamically monitors 5th-order loop integrator state and triggers instant recovery above ±1.55 feedback to prevent DC drift and HF pops.",
			"Surveille l'intégrateur d'ordre 5 et déclenche une récupération instantanée au-delà de ±1,55 pour éviter la dérive DC et les clics HF.",
			"Отслеживает интегратор 5-го порядка и мгновенно восстанавливает состояние при превышении ±1.55, устраняя дрейф DC и ВЧ-щелчки.",
			"Supervisa el integrador de 5.º orden y recupera al instante si la realimentación supera ±1.55, evitando deriva DC y chasquidos.",
			"5次ループ積分器を常時監視し、フィードバックが ±1.55 を超えると即座にリセットして DC ドリフトや高域ノイズを防止します。",
			"5차 루프 적분기 상태를 동적으로 감시해 피드백이 ±1.55를 초과하면 즉시 복구하여 DC 드리프트와 고주파 팝 노이즈를 방지합니다.");
		Add("3 阶异常 NTF，固定 6 dB 调制安全余量：稳定性优先。SDM 与 PCM/FurinaPlayer SRC 分开保存；只有当音频模块满足预定条件时才会实时接管 PCM。",
			"3 階異常 NTF，固定 6 dB 調變安全餘量：穩定性優先。SDM 與 PCM/FurinaPlayer SRC 分開儲存；只有當音訊模組滿足預定條件時才會即時接管 PCM。",
			"3rd-order NTF with fixed 6 dB headroom: stability first. SDM is stored separately from PCM/FurinaPlayer SRC and takes over PCM only when conditions are met.",
			"NTF d'ordre 3, marge fixe de 6 dB : stabilité prioritaire. SDM est distinct de PCM/FurinaPlayer SRC et ne prend le relais que si les conditions sont réunies.",
			"NTF 3-го порядка, запас 6 дБ: приоритет стабильности. SDM хранится отдельно от PCM/FurinaPlayer SRC и перехватывает поток только при соблюдении условий.",
			"NTF de 3.er orden, margen fijo de 6 dB: estabilidad primero. SDM se guarda aparte de PCM/FurinaPlayer SRC y solo asume el PCM si se cumplen las condiciones.",
			"3次 NTF・固定 6 dB ヘッドルーム：安定性優先。SDM は PCM/FurinaPlayer SRC とは別に保存され、条件を満たした場合のみリアルタイムで PCM を引き継ぎます。",
			"3차 NTF, 고정 6 dB 헤드룸: 안정성 우선. SDM은 PCM/FurinaPlayer SRC와 별도로 저장되며 조건이 충족될 때만 실시간으로 PCM을 인계받습니다.");
		Add("阶数", "階數", "Order", "Ordre", "Порядок", "Orden", "次数", "차수");
		Add("当前调校收敛到 5阶 SDM", "目前調校收斂到 5 階 SDM", "Tuned and converged to 5th-order SDM", "Convergé vers SDM d'ordre 5", "Настроено на SDM 5-го порядка", "Convergido a SDM de 5.º orden", "5次 SDM に収束", "5차 SDM으로 수렴됨");
		Add("衰减系数", "衰減係數", "Decay Factor", "Facteur d'atténuation", "Коэфф. затухания", "Factor de atenuación", "減衰係数", "감쇠 계수");
		Add("稳定性限幅 ±1.55", "穩定性限幅 ±1.55", "Stability clamp ±1.55", "Limitation de stabilité ±1,55", "Предел стабильности ±1.55", "Límite de estabilidad ±1.55", "安定性クランプ ±1.55", "안정성 클램프 ±1.55");
		Add("自由调制 --", "自由調變 --", "Free mod --", "Mod. libre --", "Своб. мод. --", "Mod. libre --", "自由変調 --", "자유 변조 --");
		Add("摆动余量", "擺動餘量", "Swing Headroom", "Marge d'excursion", "Запас размаха", "Margen de oscilación", "スイングヘッドルーム", "스윙 헤드룸");
		Add("PCM -> SDM 会自动预置调频余量 headroom，并对低频信号导进入 DSD idle 区压低噪声。",
			"PCM -> SDM 會自動預置調頻餘量 headroom，並對低頻訊號導入 DSD idle 區壓低噪訊。",
			"PCM -> SDM automatically reserves modulation headroom and guides low-level signals into the DSD idle zone to reduce noise.",
			"PCM -> SDM réserve automatiquement la marge de modulation et guide les signaux faibles vers la zone de repos DSD.",
			"PCM -> SDM автоматически резервирует запас модуляции и уводит слабые сигналы в тихую зону DSD.",
			"PCM -> SDM reserva margen de modulación automáticamente y dirige señales débiles a la zona inactiva DSD.",
			"PCM -> SDM は変調ヘッドルームを自動確保し、低レベル信号を DSD アイドル帯域へ逃がしてノイズを抑えます。",
			"PCM -> SDM은 변조 헤드룸을 자동 확보하고 저레벨 신호를 DSD 유휴 영역으로 유도해 노이즈를 낮춥니다.");
		Add("反馈环路系数 (FEEDBACK COEFFICIENTS)", "回饋環路係數 (FEEDBACK COEFFICIENTS)", "FEEDBACK COEFFICIENTS", "COEFFICIENTS DE RÉTROACTION", "КОЭФФИЦИЕНТЫ ОБРАТНОЙ СВЯЗИ", "COEFICIENTES DE REALIMENTACIÓN", "フィードバック係数 (FEEDBACK COEFFICIENTS)", "피드백 루프 계수 (FEEDBACK COEFFICIENTS)");
		Add("专业遥测与硬件宿主 (PROFESSIONAL TELEMETRY)", "專業遙測與硬體宿主 (PROFESSIONAL TELEMETRY)", "PROFESSIONAL TELEMETRY", "TÉLÉMÉTRIE PROFESSIONNELLE", "ПРОФЕССИОНАЛЬНАЯ ТЕЛЕМЕТРИЯ", "TELEMETRÍA PROFESIONAL", "プロフェッショナルテレメトリ (PROFESSIONAL TELEMETRY)", "전문 텔레메트리 및 하드웨어 호스트");
		Add("信号链 (Signal Chain)", "訊號鏈 (Signal Chain)", "Signal Chain", "Chaîne de signal", "Тракт сигнала", "Cadena de señal", "シグナルチェーン (Signal Chain)", "신호 체인 (Signal Chain)");
		Add("计算引擎 (Compute)", "計算引擎 (Compute)", "Compute Engine", "Moteur de calcul", "Вычислитель (Compute)", "Motor de cálculo", "計算エンジン (Compute)", "연산 엔진 (Compute)");
		Add("未启用", "未啟用", "Disabled", "Désactivé", "Отключено", "Desactivado", "無効", "비활성");
		Add("可选 GPU 流水线加速", "可選 GPU 流水線加速", "Optional GPU pipeline", "Pipeline GPU optionnel", "Опциональный конвейер GPU", "Pipeline GPU opcional", "オプションの GPU パイプライン", "선택적 GPU 파이프라인 가속");
		Add("实时预算 (Budget)", "即時預算 (Budget)", "Realtime Budget", "Budget temps réel", "Бюджет реального времени", "Presupuesto en tiempo real", "リアルタイムバジェット (Budget)", "실시간 예산 (Budget)");
		Add("87.6% 充沛冗余 · 稳定无下溢", "87.6% 充沛冗餘 · 穩定無下溢", "87.6% headroom · Stable, no underruns", "87,6 % de marge · Stable sans sous-débit", "87.6% запаса · Стабильно, без сбоев", "87.6% de margen · Estable sin cortes", "87.6% の余裕 · アンダーランなし", "87.6% 여유 · 언더런 없이 안정적");
		Add("FurinaPlayer SDM/DSD 引擎为高性能母带级架构，支持 Native DSD 与 DoP (DSD over PCM) 输出标准；完全兼容 DSD64/128/256/512 与 PCM 实时高保真重调制。",
			"FurinaPlayer SDM/DSD 引擎為高效能母帶級架構，支援 Native DSD 與 DoP (DSD over PCM) 輸出標準；完全相容 DSD64/128/256/512 與 PCM 即時高保真重調變。",
			"The FurinaPlayer SDM/DSD engine is a mastering-grade architecture supporting Native DSD and DoP (DSD over PCM), fully compatible with DSD64/128/256/512 and real-time PCM re-modulation.",
			"Le moteur FurinaPlayer SDM/DSD est une architecture de qualité mastering prenant en charge Native DSD et DoP, compatible DSD64/128/256/512 et remodulation PCM temps réel.",
			"Движок FurinaPlayer SDM/DSD — архитектура мастеринг-класса с поддержкой Native DSD и DoP, совместимая с DSD64/128/256/512 и передискретизацией PCM в реальном времени.",
			"El motor FurinaPlayer SDM/DSD ofrece arquitectura de masterización compatible con Native DSD y DoP, DSD64/128/256/512 y remodulación PCM en tiempo real.",
			"FurinaPlayer SDM/DSD エンジンは Native DSD と DoP に対応したマスタリング級アーキテクチャで、DSD64/128/256/512 と PCM リアルタイム再変調に完全対応します。",
			"FurinaPlayer SDM/DSD 엔진은 Native DSD와 DoP를 지원하는 마스터링급 아키텍처로 DSD64/128/256/512 및 PCM 실시간 재변조를 완벽 지원합니다.");
		Add("直通硬件 DSD 解码通道；DoP 封装或 Native ASIO 直通输出。",
			"直通硬體 DSD 解碼通道；DoP 封裝或 Native ASIO 直通輸出。",
			"Direct hardware DSD path; DoP encapsulation or Native ASIO passthrough.",
			"Chemin DSD matériel direct ; encapsulation DoP ou passthrough ASIO natif.",
			"Прямой аппаратный тракт DSD; упаковка DoP или прямой вывод Native ASIO.",
			"Ruta directa de DSD por hardware; encapsulación DoP o salida Native ASIO.",
			"ハードウェア DSD へダイレクト出力（DoP カプセル化または Native ASIO）。",
			"하드웨어 DSD 디코딩 경로로 직접 전달(DoP 캡슐화 또는 Native ASIO 패스스루).");
		Add("开启高精度 5 阶 Δ-Σ 实时过采样与调制引擎，将 PCM 转为 DSD 位流。",
			"開啟高精度 5 階 Δ-Σ 即時過取樣與調變引擎，將 PCM 轉為 DSD 位元流。",
			"Enables high-precision 5th-order Δ-Σ real-time oversampling and modulation from PCM to DSD.",
			"Active le moteur Δ-Σ temps réel d'ordre 5 haute précision pour convertir le PCM en flux DSD.",
			"Включает высокоточный Δ-Σ модулятор 5-го порядка для преобразования PCM в DSD в реальном времени.",
			"Activa el motor Δ-Σ en tiempo real de 5.º orden para convertir PCM a flujo DSD.",
			"高精度 5次 Δ-Σ リアルタイムオーバーサンプリング＆変調エンジンで PCM を DSD ビットストリームへ変換します。",
			"고정밀 5차 Δ-Σ 실시간 오버샘플링 및 변조 엔진을 활성화해 PCM을 DSD 비트스트림으로 변환합니다.");

		// SRC / Upsampling
		Add("基于 FurinaPlayer 原生 AVX2 / SIMD 多级联多相 FIR 引擎，全面兼容 WASAPI 共享、ASIO 独占与 WaveOut，支持硬件采样率自动协商与无断音切换。",
			"基於 FurinaPlayer 原生 AVX2 / SIMD 多級聯多相 FIR 引擎，全面相容 WASAPI 共用、ASIO 獨占與 WaveOut，支援硬體取樣率自動協商與無斷音切換。",
			"Powered by FurinaPlayer's native AVX2/SIMD multi-stage polyphase FIR engine across WASAPI Shared, ASIO, and WaveOut with automatic DAC rate negotiation.",
			"Propulsé par le moteur FIR polyphase multi-étages AVX2/SIMD natif de FurinaPlayer sur WASAPI partagé, ASIO et WaveOut avec négociation automatique du DAC.",
			"Работает на нативном многокаскадном полифазном FIR-движке FurinaPlayer AVX2/SIMD для WASAPI Shared, ASIO и WaveOut с автосогласованием частоты ЦАП.",
			"Impulsado por el motor FIR polifásico multietapa AVX2/SIMD nativo de FurinaPlayer en WASAPI compartido, ASIO y WaveOut con negociación automática de DAC.",
			"FurinaPlayer ネイティブ AVX2/SIMD マルチステージ・ポリフェーズ FIR エンジンにより、WASAPI 共有・ASIO・WaveOut で自動レートネゴシエーション動作します。",
			"FurinaPlayer 네이티브 AVX2/SIMD 다단 다상 FIR 엔진으로 WASAPI 공유, ASIO 및 WaveOut에서 자동 DAC 샘플레이트 협상을 지원합니다.");
		Add("FurinaPlayer SRC / 升频", "FurinaPlayer SRC / 升頻", "FurinaPlayer SRC / Upsampling", "FurinaPlayer SRC / Suréchantillonnage", "FurinaPlayer SRC / Апсемплинг", "FurinaPlayer SRC / Sobremuestreo", "FurinaPlayer SRC / アップサンプリング", "FurinaPlayer SRC / 업샘플링");
		Add("原生直通", "原生直通", "Native Direct", "Direct natif", "Нативный прямой", "Directo nativo", "ネイティブダイレクト", "네이티브 다이렉트");
		Add("升频激活", "升頻啟用", "Upsampling Active", "Suréchantillonnage actif", "Апсемплинг активен", "Sobremuestreo activo", "アップサンプリング有効", "업샘플링 활성");
		Add("A/B 实时盲听对比", "A/B 即時盲聽對比", "A/B Live Blind Compare", "Comparatif A/B en direct", "Сравнение A/B в реальном времени", "Comparativa A/B en vivo", "A/B リアルタイム比較", "A/B 실시간 블라인드 비교");
		Add("幅频响应 (dB)", "幅頻響應 (dB)", "Magnitude (dB)", "Réponse (dB)", "АЧХ (дБ)", "Magnitud (dB)", "振幅特性 (dB)", "진폭 응답 (dB)");
		Add("脉冲响应 (Impulse)", "脈衝響應 (Impulse)", "Impulse Response", "Réponse impulsionnelle", "Импульсная характеристика", "Respuesta al impulso", "インパルス応答", "임펄스 응답");
		Add("路径", "路徑", "Route", "Chemin", "Маршрут", "Ruta", "ルート", "경로");
		Add("源采样率", "源取樣率", "Source Rate", "Fréquence source", "Исходная частота", "Frecuencia origen", "ソースレート", "원본 샘플레이트");
		Add("目标采样率", "目標取樣率", "Target Rate", "Fréquence cible", "Целевая частота", "Frecuencia destino", "ターゲットレート", "목표 샘플레이트");
		Add("01 FurinaPlayer SRC 控制模式", "01 FurinaPlayer SRC 控制模式", "01 FurinaPlayer SRC Mode", "01 Mode FurinaPlayer SRC", "01 Режим FurinaPlayer SRC", "01 Modo FurinaPlayer SRC", "01 FurinaPlayer SRC モード", "01 FurinaPlayer SRC 모드");
		Add("02 质量策略", "02 品質策略", "02 Quality", "02 Qualité", "02 Качество", "02 Calidad", "02 品質", "02 품질 정책");
		Add("03 倍率", "03 倍率", "03 Ratio", "03 Facteur", "03 Кратность", "03 Factor", "03 倍率", "03 배율");
		Add("关闭", "關閉", "Off", "Désactivé", "Выкл", "Desactivado", "オフ", "끔");
		Add("CPU 实时路径,未请求 CUDA。", "CPU 即時路徑，未請求 CUDA。", "CPU real-time path, CUDA not requested.", "Chemin CPU temps réel, CUDA non demandé.", "Путь CPU реального времени, CUDA не запрошена.", "Ruta CPU en tiempo real, sin CUDA.", "CPU リアルタイムパス（CUDA 未使用）。", "CPU 실시간 경로, CUDA 요청 안 함.");
		Add("精度", "精度", "Precision", "Précision", "Точность", "Precisión", "精度", "정밀도");
		Add("过滤器插槽", "濾波器插槽", "Filter Slot", "Emplacement filtre", "Слот фильтра", "Ranura de filtro", "フィルタースロット", "필터 슬롯");
		Add("FurinaPlayer SRC 控制模式", "FurinaPlayer SRC 控制模式", "FurinaPlayer SRC Control Mode", "Mode de contrôle FurinaPlayer SRC", "Режим управления FurinaPlayer SRC", "Modo de control FurinaPlayer SRC", "FurinaPlayer SRC 制御モード", "FurinaPlayer SRC 제어 모드");
		Add("48 kHz 兼容", "48 kHz 相容", "48 kHz Compat", "Compat. 48 kHz", "Совм. 48 кГц", "Compat. 48 kHz", "48 kHz 互換", "48 kHz 호환");
		Add("保持源采样率，Bit-perfect 条件不受 FurinaPlayer SRC 影响。",
			"保持源取樣率，Bit-perfect 條件不受 FurinaPlayer SRC 影響。",
			"Keeps source sample rate; bit-perfect playback is unaffected by FurinaPlayer SRC.",
			"Conserve la fréquence source ; la lecture bit-perfect n'est pas affectée.",
			"Сохраняет исходную частоту; режим Bit-perfect не затрагивается.",
			"Mantiene la frecuencia original; el modo bit-perfect no se ve afectado.",
			"ソースサンプルレートを維持し、Bit-perfect 再生に影響しません。",
			"원본 샘플레이트를 유지하며 Bit-perfect 재생에 영향을 주지 않습니다.");
		Add("质量策略", "品質策略", "Quality Profile", "Profil de qualité", "Профиль качества", "Perfil de calidad", "品質プロファイル", "품질 정책");
		Add("全精度多相级联参考档，优先极低通带波纹、零染色与极限阻带抑制。",
			"最高精度 Furina FIR，優先透明度與截斷失真。",
			"Highest precision Furina FIR, prioritizing transparency and minimal truncation distortion.",
			"Furina FIR haute précision, privilégiant la transparence et une distorsion minimale.",
			"Максимальная точность Furina FIR с приоритетом прозрачности и минимума искажений.",
			"Máxima precisión Furina FIR, priorizando transparencia y mínima distorsión.",
			"最高精度 Furina FIR。透明感と打ち切り歪みの低減を優先します。",
			"최고 정밀도 Furina FIR로 투명도와 절단 왜곡 최소화를 우선합니다.");
		Add("过滤器 1x", "濾波器 1x", "Filter 1x", "Filtre 1x", "Фильтр 1x", "Filtro 1x", "フィルター 1x", "필터 1x");
		Add("高阶量 sinc 滤波，听感更柔和，兼顾瞬态。",
			"高階數 sinc 濾波，聽感更柔和，兼顧瞬態。",
			"High-order sinc filter with smooth tonality and balanced transients.",
			"Filtre sinc d'ordre élevé, sonorité douce et transitoires équilibrées.",
			"Фильтр sinc высокого порядка с мягким звучанием и точными переходными процессами.",
			"Filtro sinc de alto orden, sonido suave y transitorios equilibrados.",
			"高次 sinc フィルター。滑らかな音調とトランジェントを両立します。",
			"고차 sinc 필터로 부드러운 음색과 과도 응답을 조화롭게 유지합니다.");
		Add("过滤器 Nx", "濾波器 Nx", "Filter Nx", "Filtre Nx", "Фильтр Nx", "Filtro Nx", "フィルター Nx", "필터 Nx");
		Add("半带 sinc 滤波，适合 2x/4x 先倒频定实时性。",
			"半帶 sinc 濾波，適合 2x/4x 即時升頻。",
			"Half-band sinc filter, ideal for real-time 2x/4x oversampling.",
			"Filtre sinc demi-bande, idéal pour le suréchantillonnage 2x/4x temps réel.",
			"Полуполосовой sinc-фильтр, оптимален для 2x/4x в реальном времени.",
			"Filtro sinc de media banda, ideal para 2x/4x en tiempo real.",
			"ハーフバンド sinc フィルター。リアルタイム 2x/4x 処理に最適です。",
			"하프밴드 sinc 필터로 실시간 2x/4x 업샘플링에 적합합니다.");
		Add("CPU 多线程", "CPU 多執行緒", "CPU Multi-Thread", "CPU multithread", "CPU многопоточный", "CPU multihilo", "CPU マルチスレッド", "CPU 멀티스레드");
		Add("默认实时路径：极优，无降采样风险，并保证随机可用。",
			"預設即時路徑：極佳效能，無降取樣風險，確保隨時可用。",
			"Default real-time path: optimal, zero downsampling risk, always available.",
			"Chemin temps réel par défaut : optimal, sans risque de sous-échantillonnage.",
			"Путь по умолчанию: оптимальный, без риска понижения частоты и всегда доступный.",
			"Ruta por defecto en tiempo real: óptima, sin riesgo y siempre disponible.",
			"標準リアルタイムパス：最適でダウンサンプリングの恐れなく常に利用可能です。",
			"기본 실시간 경로: 최적화되어 있으며 다운샘플링 위험 없이 항상 사용 가능합니다.");
		Add("保持 Float32 PCM 链原图标：默认关闭，避免无意义加噪。",
			"保持 Float32 PCM 鏈原貌：預設關閉，避免無意義加噪。",
			"Preserves Float32 PCM chain as-is; off by default to avoid unnecessary noise.",
			"Préserve la chaîne Float32 PCM intacte ; désactivé par défaut.",
			"Сохраняет тракт Float32 PCM без изменений; по умолчанию выключено.",
			"Preserva la cadena Float32 PCM intacta; desactivado por defecto.",
			"Float32 PCM チェーンをそのまま維持します（不要なノイズを避けるためデフォルトオフ）。",
			"Float32 PCM 체인을 그대로 유지합니다(불필요한 노이즈 방지를 위해 기본값 끔).");
		Add("快速套用进阶调谐梯度", "快速套用進階調校梯度", "Quick-apply advanced tuning tiers", "Appliquer rapidement les paliers avancés", "Быстрый выбор продвинутых профилей", "Aplicar niveles avanzados rápidamente", "高度なチューニング段階をすばやく適用", "고급 튜닝 단계 빠르게 적용");
		Add("低延迟 / CPU", "低延遲 / CPU", "Low Latency / CPU", "Faible latence / CPU", "Низкая задержка / CPU", "Baja latencia / CPU", "低遅延 / CPU", "저지연 / CPU");
		Add("中等延迟 / CPU", "中等延遲 / CPU", "Medium Latency / CPU", "Latence moyenne / CPU", "Средняя задержка / CPU", "Latencia media / CPU", "中遅延 / CPU", "중간 지연 / CPU");
		Add("高延迟 / CPU", "高延遲 / CPU", "High Latency / CPU", "Latence élevée / CPU", "Высокая задержка / CPU", "Alta latencia / CPU", "高遅延 / CPU", "고지연 / CPU");
		Add("超高延迟 / CPU", "超高延遲 / CPU", "Ultra Latency / CPU", "Très haute latence / CPU", "Макс. задержка / CPU", "Latencia ultra / CPU", "超高遅延 / CPU", "초고지연 / CPU");
		Add("高阶量 sinc 滤波，听感更柔和，兼顾瞬态。44.1/48 kHz 音源的差异主要集中在最高频段，相位与瞬态响应：透明 SRC 本身就不应产生明显染色。",
			"高階數 sinc 濾波，聽感更柔和，兼顧瞬態。44.1/48 kHz 訊源的差異主要集中在極高頻段、相位與瞬態響應：透明 SRC 本身就不應產生明顯染色。",
			"High-order sinc filter with smooth sound and transients. Differences on 44.1/48 kHz sources lie in upper treble, phase, and transient response—transparent SRC should add no coloration.",
			"Filtre sinc d'ordre élevé. Les différences en 44,1/48 kHz concernent le très haut du spectre, la phase et les transitoires : un SRC transparent ne doit pas colorer le son.",
			"Фильтр sinc высокого порядка. Разница на 44.1/48 кГц проявляется на краю ВЧ, в фазе и переходных процессах: прозрачный SRC не должен окрашивать звук.",
			"Filtro sinc de alto orden. Las diferencias en 44.1/48 kHz están en los agudos extremos, fase y transitorios: un SRC transparente no debe colorear.",
			"高次 sinc フィルター。44.1/48 kHz 音源での違いは超高域・位相・トランジェントに現れます。透明な SRC は余計な色付けをしないのが理想です。",
			"고차 sinc 필터입니다. 44.1/48 kHz 음원에서의 차이는 초고역, 위상, 과도 응답에 집중되며 투명한 SRC는 착색이 없어야 합니다.");
		Add("· 激活", "· 啟用", "· Active", "· Actif", "· Активен", "· Activo", "· 有効", "· 활성");
		Add("将 PCM 固定转换到 48 kHz，适合只接受 48 kHz 的独占耳机或声卡；默认关闭。",
			"將 PCM 固定轉換到 48 kHz，適合只接受 48 kHz 的獨占耳機或音效卡；預設關閉。",
			"Resamples PCM to fixed 48 kHz for 48 kHz-only exclusive DACs/headsets; off by default.",
			"Convertit le PCM à 48 kHz fixe pour les DAC/casques exclusifs 48 kHz ; désactivé par défaut.",
			"Фиксированный ресемплинг PCM в 48 кГц для устройств, принимающих только 48 кГц; по умолчанию выкл.",
			"Convierte PCM a 48 kHz fijos para DAC o auriculares exclusivos de 48 kHz; desactivado por defecto.",
			"48 kHz 専用機器向けに PCM を 48 kHz 固定へ変換します（デフォルトオフ）。",
			"48 kHz 전용 DAC/헤드셋을 위해 PCM을 48 kHz로 고정 변환합니다(기본값 끔).");
		Add("44.1k 家族升到 88.2k，48k 家族升到 96k。", "44.1k 家族升到 88.2k，48k 家族升到 96k。", "44.1k family -> 88.2k, 48k family -> 96k.", "Famille 44,1k -> 88,2k, famille 48k -> 96k.", "Семейство 44.1k -> 88.2k, 48k -> 96k.", "Familia 44.1k -> 88.2k, 48k -> 96k.", "44.1k 系を 88.2k へ、48k 系を 96k へアップサンプリング。", "44.1k 계열은 88.2k로, 48k 계열은 96k로 업샘플링합니다.");
		Add("44.1k 家族升到 176.4k，48k 家族升到 192k。", "44.1k 家族升到 176.4k，48k 家族升到 192k。", "44.1k family -> 176.4k, 48k family -> 192k.", "Famille 44,1k -> 176,4k, famille 48k -> 192k.", "Семейство 44.1k -> 176.4k, 48k -> 192k.", "Familia 44.1k -> 176.4k, 48k -> 192k.", "44.1k 系を 176.4k へ、48k 系を 192k へアップサンプリング。", "44.1k 계열은 176.4k로, 48k 계열은 192k로 업샘플링합니다.");
		Add("实验档：44.1k 家族升到 352.8k，48k 家族升到 384k。", "實驗檔：44.1k 家族升到 352.8k，48k 家族升到 384k。", "Experimental: 44.1k family -> 352.8k, 48k family -> 384k.", "Expérimental : 44,1k -> 352,8k, 48k -> 384k.", "Экспериментально: 44.1k -> 352.8k, 48k -> 384k.", "Experimental: 44.1k -> 352.8k, 48k -> 384k.", "実験的：44.1k 系を 352.8k へ、48k 系を 384k へ。", "실험적: 44.1k 계열은 352.8k로, 48k 계열은 384k로 업샘플링합니다.");
		Add("均衡级多相 FIR 档位，兼顾低延迟、温和 CPU 负载与高阻带抑制。", "保持原有 Furina FIR 檔位，兼顧穩定與開銷。", "Balanced Furina FIR tier for stability and efficiency.", "Palier Furina FIR équilibré entre stabilité et charge.", "Сбалансированный профиль Furina FIR для стабильности.", "Nivel Furina FIR equilibrado entre estabilidad y consumo.", "安定性と負荷のバランスを取った Furina FIR 設定。", "안정성과 부하의 균형을 맞춘 Furina FIR 단계입니다.");
		Add("降低 SRC 开销，适合低延迟输出。", "降低 SRC 開銷，適合低延遲輸出。", "Lower SRC overhead for low-latency output.", "Charge SRC réduite pour faible latence.", "Сниженная нагрузка SRC для малой задержки.", "Menor carga SRC para baja latencia.", "SRC 負荷を抑え、低遅延出力に適しています。", "SRC 부하를 줄여 저지연 출력에 적합합니다.");
		Add("CPU 多线程实时路径。", "CPU 多執行緒即時路徑。", "Multi-threaded CPU real-time path.", "Chemin CPU multithread temps réel.", "Многопоточный путь CPU реального времени.", "Ruta CPU multihilo en tiempo real.", "CPU マルチスレッドリアルタイムパス。", "CPU 멀티스레드 실시간 경로입니다.");
		Add("CUDA FIR 准备中，自动兼顾回落。", "CUDA FIR 準備中，自動兼顧回落。", "CUDA FIR preparing with automatic CPU fallback.", "CUDA FIR en préparation, repli CPU automatique.", "Подготовка CUDA FIR с автооткатом на CPU.", "CUDA FIR preparándose con respaldo automático.", "CUDA FIR 準備中（自動フォールバック対応）。", "CUDA FIR 준비 중(자동 폴백 지원).");
		Add("低阶高斯窗 sinc 滤波，瞬态响应快，听感温暖。", "低階高斯窗 sinc 濾波，瞬態響應快，聽感溫暖。", "Low-order Gaussian-windowed sinc filter; fast transients, warm tone.", "Filtre sinc gaussien d'ordre court ; transitoires rapides, son chaud.", "Короткий sinc-фильтр с окном Гаусса; быстрые переходные процессы, тёплое звучание.", "Filtro sinc gaussiano corto; transitorios rápidos y tono cálido.", "低次ガウス窓 sinc フィルター。高速な過渡応答と温かみのある音色。", "저차 가우시안 윈도우 sinc 필터로 과도 응답이 빠르고 따뜻한 음색을 냅니다.");
		Add("对称 sinc 滤波，群延迟恒定，零相位失真。", "對稱 sinc 濾波，群延遲恆定，零相位失真。", "Symmetric linear-phase sinc filter; constant group delay, zero phase distortion.", "Filtre sinc symétrique à phase linéaire ; délai de groupe constant.",
			"Симметричный sinc-фильтр с линейной фазой и постоянной групповой задержкой.", "Filtro sinc simétrico de fase lineal; retardo de grupo constante.", "対称リニアフェーズ sinc フィルター。群遅延一定・位相歪みゼロ。", "대칭 선형 위상 sinc 필터로 군지연이 일정하고 위상 왜곡이 없습니다.");
		Add("最小相位滤波，零前振铃，能量集中于瞬态起音。", "最小相位濾波，零前振鈴，能量集中於瞬態起音。", "Minimum-phase filter; zero pre-ringing, energy focused on transient attack.", "Filtre à phase minimale ; sans pré-oscillation, attaque précise.", "Минимально-фазовый фильтр; без предзвона, фокус на атаке.", "Filtro de fase mínima; sin pre-oscilación, ataque concentrado.", "ミニマムフェーズフィルター。プリリンギングなしで立ち上がりにエネルギーを集中。", "최소 위상 필터로 프리 링잉 없이 과도 어택에 에너지를 집중합니다.");
		Add("短阶陡峭滤波，截止带陡峭，适合实时处理。", "短階陡峭濾波，截止帶陡峭，適合即時處理。", "Short steep filter; sharp roll-off suited for real-time processing.", "Filtre raide court ; coupure nette adaptée au temps réel.", "Короткий крутой фильтр для обработки в реальном времени.", "Filtro corto abrupto; corte pronunciado para tiempo real.", "短タップ急峻フィルター。急峻な遮断特性でリアルタイム処理に最適。", "짧은 탭 급준 필터로 차단 대역이 가파르며 실시간 처리에 적합합니다.");
		Add("长阶陡峭滤波，>100dB 阻带衰减，母带级透明度。", "長階陡峭濾波，>100dB 阻帶衰減，母帶級透明度。", "Long steep filter; >100 dB stopband attenuation, mastering-grade transparency.", "Filtre raide long ; atténuation >100 dB, transparence mastering.", "Длинный крутой фильтр; подавление >100 дБ, прозрачность мастеринга.", "Filtro largo abrupto; atenuación >100 dB, transparencia de masterización.", "長タップ急峻フィルター。100dB 超の阻止域減衰によるマスタリング級透明感。", "긴 탭 급준 필터로 100dB 이상의 저지대역 감쇠와 마스터링급 투명도를 제공합니다.");
		Add("多相 sinc 滤波，频响平直，极低互调失真。", "多相 sinc 濾波，頻響平直，極低互調失真。", "Polyphase sinc filter; flat frequency response and ultra-low IMD.", "Filtre sinc polyphase ; réponse plate et très faible IMD.", "Полифазный sinc-фильтр; ровная АЧХ и сверхнизкие интермодуляционные искажения.", "Filtro sinc polifásico; respuesta plana y mínima IMD.", "ポリフェーズ sinc フィルター。平坦な周波数特性と極小の相互変調歪み。", "다상 sinc 필터로 주파수 응답이 평탄하고 상호 변조 왜곡이 극히 낮습니다.");
		Add("陡峭过渡带滤波，阻带深度抑制镜像混叠。", "陡峭過渡帶濾波，阻帶深度抑制鏡像混疊。", "Steep transition filter; deep stopband suppression of imaging.", "Filtre à transition raide ; forte suppression du repliement.", "Фильтр с крутым переходом; глубокое подавление зеркальных спектров.", "Filtro de transición abrupta; profunda supresión de alias.", "急峻な遷移帯フィルター。イメージングを深く抑制します。", "가파른 전이 대역 필터로 이미지 에일리어싱을 깊이 억제합니다.");
		Add("极限 3071 阶陡峭滤波，用于测量级对照。", "極限 3071 階陡峭濾波，用於測量級對照。", "Extreme 3071-tap steep filter for measurement-grade reference.", "Filtre extrême à 3071 coefficients pour référence de mesure.", "Экстремальный фильтр 3071 порядка для измерительного эталона.", "Filtro extremo de 3071 taps para referencia de medición.", "測定級リファレンス向けの極限 3071 タップ急峻フィルター。", "측정급 레퍼런스를 위한 극한 3071탭 급준 필터입니다.");
		Add("利用多核心并发执行块滤波，降低单核负载与发热。", "利用多核心並發執行區塊濾波，降低單核負載與發熱。", "Runs block filtering across multiple cores to reduce single-core load and heat.", "Exécute le filtrage en parallèle sur plusieurs cœurs pour réduire la chauffe.",
			"Распределяет блочную фильтрацию по ядрам, снижая нагрузку на одно ядро.", "Ejecuta filtrado por bloques en varios núcleos para reducir carga y calor.", "マルチコア並列ブロック処理により、単一コア負荷と発熱を低減します。", "멀티코어 병렬 블록 필터링으로 단일 코어 부하와 발열을 줄입니다.");
		Add("实验性 CUDA FIR 路径；需 NVIDIA 显卡驱动支持。", "實驗性 CUDA FIR 路徑；需 NVIDIA 顯示卡驅動程式支援。", "Experimental CUDA FIR path; requires NVIDIA GPU driver support.", "Chemin CUDA FIR expérimental ; nécessite un pilote NVIDIA.", "Экспериментальный путь CUDA FIR; требуется драйвер NVIDIA.", "Ruta CUDA FIR experimental; requiere controlador NVIDIA.", "実験的 CUDA FIR パス。NVIDIA GPU ドライバーが必要です。", "실험적 CUDA FIR 경로입니다. NVIDIA 그래픽 드라이버 지원이 필요합니다.");
		Add("标准三角形概率密度抖动，消除 16/24-bit 量化相关失真。", "標準三角形機率密度抖動，消除 16/24-bit 量化相關失真。", "Standard Triangular PDF dither; eliminates 16/24-bit quantization distortion.", "Dither TPDF standard ; élimine la distorsion de quantification 16/24 bits.", "Стандартный треугольный дитеринг; устраняет искажения квантования 16/24 бит.", "Dither TPDF estándar; elimina distorsión de cuantización de 16/24 bits.", "標準 TPDF ディザ。16/24-bit 量子化歪みを除去します。", "표준 TPDF 디더로 16/24-bit 양자화 왜곡을 제거합니다.");
		Add("高通噪声整形：将量化噪声推向 18kHz 以上高频不敏感区。", "高通噪訊整形：將量化噪訊推向 18kHz 以上高頻不敏感區。", "High-pass noise shaping: pushes quantization noise above 18 kHz.", "Mise en forme passe-haut : repousse le bruit de quantification au-dessus de 18 kHz.", "ВЧ-шейпинг шума: сдвигает шум квантования выше 18 кГц.", "Conformado de ruido paso alto: desplaza el ruido por encima de 18 kHz.", "ハイパスノイズシェーピング：量子化ノイズを 18kHz 以上の高域へ押し上げます。", "고역 통과 노이즈 셰이핑: 양자화 노이즈를 18kHz 이상 고주파 영역으로 밀어냅니다.");

		// Stereo Field
		Add("FurinaPlayer Spatial Workbench · Mid / Side 声像重塑", "FurinaPlayer Spatial Workbench · Mid / Side 聲像重塑", "FurinaPlayer Spatial Workbench · Mid / Side Imaging", "FurinaPlayer Spatial Workbench · Sculpture Mid / Side", "FurinaPlayer Spatial Workbench · Mid / Side сцена", "FurinaPlayer Spatial Workbench · Imagen Mid / Side", "FurinaPlayer Spatial Workbench · Mid / Side 音像再構築", "FurinaPlayer Spatial Workbench · Mid / Side 음상 재구성");
		Add("立体声场 (Stereo Field)", "立體聲場 (Stereo Field)", "Stereo Field", "Champ stéréo (Stereo Field)", "Стереополе (Stereo Field)", "Campo estéreo (Stereo Field)", "ステレオ音場 (Stereo Field)", "스테레오 음장 (Stereo Field)");
		Add("纯单声道", "純單聲道", "Pure Mono", "Mono pur", "Чистое моно", "Mono puro", "ピュアモノ", "순수 모노");
		Add("0% · 纯中置", "0% · 純中置", "0% · Center Only", "0 % · Centre pur", "0% · Только центр", "0% · Solo centro", "0% · センターのみ", "0% · 센터 전용");
		Add("聚焦中置", "聚焦中置", "Focus Center", "Focus centre", "Фокус на центр", "Enfoque central", "センターフォーカス", "센터 집중");
		Add("70% · 人声突出", "70% · 人聲突出", "70% · Vocal Focus", "70 % · Voix en avant", "70% · Выделение вокала", "70% · Voz destacada", "70% · ボーカル強調", "70% · 보컬 강조");
		Add("原声平衡", "原聲平衡", "Original Balance", "Équilibre original", "Исходный баланс", "Balance original", "オリジナルバランス", "원음 밸런스");
		Add("100% · 原始立体声", "100% · 原始立體聲", "100% · Original Stereo", "100 % · Stéréo d'origine", "100% · Исходное стерео", "100% · Estéreo original", "100% · 原音ステレオ", "100% · 원본 스테레오");
		Add("宽阔声场", "寬闊聲場", "Wide Stage", "Scène large", "Широкая сцена", "Escena amplia", "ワイドステージ", "넓은 음장");
		Add("130% · 空间延展", "130% · 空間延展", "130% · Spatial Expansion", "130 % · Extension spatiale", "130% · Расширение сцены", "130% · Expansión espacial", "130% · 空間拡張", "130% · 공간 확장");
		Add("超宽环绕", "超寬環繞", "Super Wide", "Ultra large", "Сверхширокое", "Súper amplio", "スーパーワイド", "초광폭 서라운드");
		Add("175% · 环绕沉浸", "175% · 環繞沉浸", "175% · Immersive", "175 % · Immersion", "175% · Погружение", "175% · Inmersivo", "175% · イマーシブ", "175% · 입체 몰입");
		Add("立体声场已旁路", "立體聲場已旁路", "Stereo Field Bypassed", "Champ stéréo contourné", "Стереополе в байпасе", "Campo estéreo en bypass", "ステレオ音場バイパス中", "스테레오 음장 바이패스됨");
		Add("立体声场已开启", "立體聲場已開啟", "Stereo Field Active", "Champ stéréo actif", "Стереополе активно", "Campo estéreo activo", "ステレオ音場有効", "스테레오 음장 켜짐");
		Add("应用参数", "套用參數", "Apply", "Appliquer", "Применить", "Aplicar", "適用", "적용");
		Add("未应用修改", "未套用修改", "Unapplied Changes", "Modifications non appliquées", "Есть неприменённые изменения", "Cambios sin aplicar", "未適用の変更", "적용되지 않은 변경 사항");
		Add("在 Mid / Side 域调节立体声场宽度、中央人声专注度与侧向空间感。0% 为完全单声道，100% 为原始立体声，200% 为超宽空间。",
			"在 Mid / Side 域調節立體聲場寬度、中央人聲專注度與側向空間感。0% 為完全單聲道，100% 為原始立體聲，200% 為超寬空間。",
			"Adjust stereo width, center vocal focus, and side ambience in the Mid / Side domain. 0% is full mono, 100% is original stereo, 200% is ultra-wide.",
			"Ajustez la largeur stéréo, la présence vocale centrale et l'ampleur latérale en Mid / Side. 0 % = mono, 100 % = stéréo d'origine, 200 % = ultra-large.",
			"Настройте ширину стерео, фокус центрального вокала и боковой объём в домене Mid / Side. 0% — моно, 100% — оригинал, 200% — сверхшироко.",
			"Ajusta el ancho estéreo, el enfoque vocal central y el ambiente lateral en el dominio Mid / Side. 0% es mono, 100% estéreo original, 200% súper amplio.",
			"Mid / Side 領域でステレオ幅、センターボーカルの定位、サイドの空間感を調整します。0% でモノラル、100% で原音、200% で超ワイド。",
			"Mid / Side 영역에서 스테레오 폭, 중앙 보컬 집중도, 측면 공간감을 조절합니다. 0%는 모노, 100%는 원음 스테레오, 200%는 초광폭입니다.");
		Add("M/S 声像仪", "M/S 聲像儀", "M/S Vectorscope", "Analyseur M/S", "M/S вектороскоп", "Analizador M/S", "M/S ベクトルスコープ", "M/S 음상 분석기");
		Add("由宽度与增益推导的能量分布椭圆；虚线圆为 0dB 参考。",
			"由寬度與增益推導的能量分布橢圓；虛線圓為 0dB 參考。",
			"Energy distribution ellipse derived from width & gain; dashed circle marks 0 dB reference.",
			"Ellipse d'énergie dérivée de la largeur et du gain ; le cercle pointillé indique la référence 0 dB.",
			"Эллипс распределения энергии по ширине и усилению; пунктир — уровень 0 дБ.",
			"Elipse de energía derivada del ancho y ganancia; el círculo discontinuo marca 0 dB.",
			"幅とゲインから算出されたエネルギー分布楕円。破線の円が 0 dB 基準です。",
			"폭과 게인에서 도출된 에너지 분포 타원이며 점선 원은 0 dB 기준입니다.");
		Add("可拖动 · 调整场宽", "可拖曳 · 調整場寬", "Drag · Adjust Width", "Glisser · Ajuster largeur", "Тяните · Ширина", "Arrastrar · Ajustar ancho", "ドラッグで幅を調整", "드래그하여 폭 조절");
		Add("中置能量: M × 1.00 (0.0 dB)", "中置能量: M × 1.00 (0.0 dB)", "Mid Energy: M × 1.00 (0.0 dB)", "Énergie Mid : M × 1,00 (0,0 dB)", "Энергия Mid: M × 1.00 (0.0 дБ)", "Energía Mid: M × 1.00 (0.0 dB)", "Mid エネルギー: M × 1.00 (0.0 dB)", "Mid 에너지: M × 1.00 (0.0 dB)");
		Add("中置能量: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "中置能量: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "Mid Energy: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "Énergie Mid : M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "Энергия Mid: M × {0:0.00} ({1:+0.0;-0.0;0.0} дБ)", "Energía Mid: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "Mid エネルギー: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)", "Mid 에너지: M × {0:0.00} ({1:+0.0;-0.0;0.0} dB)");
		Add("侧向能量: S × 1.00 (0.0 dB)", "側向能量: S × 1.00 (0.0 dB)", "Side Energy: S × 1.00 (0.0 dB)", "Énergie Side : S × 1,00 (0,0 dB)", "Энергия Side: S × 1.00 (0.0 дБ)", "Energía Side: S × 1.00 (0.0 dB)", "Side エネルギー: S × 1.00 (0.0 dB)", "Side 에너지: S × 1.00 (0.0 dB)");
		Add("侧向能量: S × {0:0.00} ({1})", "側向能量: S × {0:0.00} ({1})", "Side Energy: S × {0:0.00} ({1})", "Énergie Side : S × {0:0.00} ({1})", "Энергия Side: S × {0:0.00} ({1})", "Energía Side: S × {0:0.00} ({1})", "Side エネルギー: S × {0:0.00} ({1})", "Side 에너지: S × {0:0.00} ({1})");
		Add("声像相关性", "聲像相關性", "Phase Correlation", "Corrélation de phase", "Фазовая корреляция", "Correlación de fase", "位相相関", "위상 상관도");
		Add("+1.00 (聚焦中置)", "+1.00 (聚焦中置)", "+1.00 (Center Focused)", "+1,00 (Centré)", "+1.00 (Фокус в центре)", "+1.00 (Centrado)", "+1.00 (センター集中)", "+1.00 (센터 집중)");
		Add("快速预设 (PRESETS)", "快速預設 (PRESETS)", "QUICK PRESETS", "PRÉRÉGLAGES RAPIDES", "БЫСТРЫЕ ПРЕСЕТЫ", "AJUSTES RÁPIDOS", "クイックプリセット (PRESETS)", "빠른 프리셋 (PRESETS)");
		Add("一键套用经典声像比例", "一鍵套用經典聲像比例", "One-click classic imaging ratios", "Proportions classiques en un clic", "Классические профили в один клик", "Proporciones clásicas en un clic", "定番の音像バランスをワンクリックで適用", "원클릭으로 클래식 음상 비율 적용");
		Add("参数控制 (CONTROLS)", "參數控制 (CONTROLS)", "CONTROLS", "CONTRÔLES", "ПАРАМЕТРЫ", "CONTROLES", "コントロール (CONTROLS)", "파라미터 제어 (CONTROLS)");
		Add("双击滑块可恢复默认", "雙擊滑塊可恢復預設", "Double-click slider to reset", "Double-cliquez pour réinitialiser", "Двойной клик по ползунку — сброс", "Doble clic en el deslizador para restablecer", "スライダーをダブルクリックでリセット", "슬라이더를 더블클릭하면 기본값으로 복원");
		Add("场宽 (Stereo Width)", "場寬 (Stereo Width)", "Stereo Width", "Largeur stéréo (Width)", "Ширина стерео (Width)", "Ancho estéreo (Width)", "ステレオ幅 (Stereo Width)", "스테레오 폭 (Stereo Width)");
		Add("0% (单声道) 至 200% (超宽)", "0% (單聲道) 至 200% (超寬)", "0% (Mono) to 200% (Ultra-wide)", "0 % (Mono) à 200 % (Ultra-large)", "от 0% (моно) до 200% (сверхшироко)", "0% (Mono) a 200% (Súper amplio)", "0% (モノラル) ～ 200% (超ワイド)", "0% (모노) ~ 200% (초광폭)");
		Add("中置增益 (Mid Gain)", "中置增益 (Mid Gain)", "Mid Gain", "Gain central (Mid)", "Усиление центра (Mid)", "Ganancia central (Mid)", "Mid ゲイン (Mid Gain)", "Mid 게인 (Mid Gain)");
		Add("调整居中人声与主乐器音量权重", "調整居中人聲與主樂器音量權重", "Adjust level of center vocals and lead instruments", "Ajuste le niveau des voix et instruments centraux", "Регулирует громкость вокала и центральных инструментов", "Ajusta el nivel de voces e instrumentos centrales", "中央のボーカルや主旋律の音量を調整", "중앙 보컬과 메인 악기의 음량 비중 조절");
		Add("侧向增益 (Side Gain)", "側向增益 (Side Gain)", "Side Gain", "Gain latéral (Side)", "Усиление сторон (Side)", "Ganancia lateral (Side)", "Side ゲイン (Side Gain)", "Side 게인 (Side Gain)");
		Add("增强或抑制声场两侧空间氛围", "增強或抑制聲場兩側空間氛圍", "Boost or attenuate side ambience and width", "Accentue ou atténue l'ambiance latérale", "Усиливает или ослабляет боковую атмосферу", "Realza o atenúa el ambiente lateral", "左右の空間感や広がりを強調または抑制", "음장 양측의 공간감과 잔향을 강화 또는 억제");
		Add("单声道低频保留 (Mono Bass)", "單聲道低頻保留 (Mono Bass)", "Mono Bass", "Basses mono (Mono Bass)", "Моно-бас (Mono Bass)", "Graves en mono (Mono Bass)", "モノベース (Mono Bass)", "모노 베이스 (Mono Bass)");
		Add("将设定频点以下合并为单声道，保障低音扎实下潜",
			"將設定頻點以下合併為單聲道，保障低音紮實下潛",
			"Sums frequencies below cutoff to mono for tight, solid bass",
			"Passe en mono sous la fréquence cible pour des graves solides",
			"Суммирует частоты ниже порога в моно для плотного баса",
			"Suma a mono por debajo del corte para unos graves firmes",
			"指定周波数以下をモノラル化してタイトな低音を確保",
			"설정 주파수 이하를 모노로 합쳐 단단하고 깊은 저음 유지");
		Add("立体声场宽", "立體聲場寬", "Stereo Width", "Largeur stéréo", "Ширина стерео", "Ancho estéreo", "ステレオ幅", "스테레오 폭");
		Add("中置偏置 (Mid)", "中置偏置 (Mid)", "Mid Bias", "Biais Mid", "Смещение Mid", "Sesgo Mid", "Mid バイアス", "Mid 편차");
		Add("中央人声基准", "中央人聲基準", "Neutral Center Vocals", "Voix centrales neutres", "Нейтральный центр", "Voces centrales neutras", "センターボーカル基準", "중앙 보컬 기준");
		Add("侧向增益 (Side)", "側向增益 (Side)", "Side Gain", "Gain Side", "Усиление Side", "Ganancia Side", "Side ゲイン", "Side 게인");
		Add("空间氛围自然", "空間氛圍自然", "Natural Ambience", "Ambiance naturelle", "Естественный объём", "Ambiente natural", "自然な空間感", "자연스러운 공간감");
		Add("双击滑块可恢复默认值。高宽度的侧向声场建议配合低频保留，以保持低音下潜扎实。",
			"雙擊滑塊可恢復預設值。高寬度的側向聲場建議配合低頻保留，以保持低音下潛紮實。",
			"Double-click sliders to reset. Pair wide stereo settings with Mono Bass to keep low-end punch.",
			"Double-cliquez pour réinitialiser. Associez une grande largeur à Mono Bass pour garder des graves fermes.",
			"Двойной клик сбрасывает ползунки. При широкой сцене используйте Mono Bass для сохранения глубины баса.",
			"Doble clic para restablecer. Combina anchura alta con Mono Bass para mantener graves firmes.",
			"スライダーをダブルクリックで初期値に戻せます。広めの設定では Mono Bass 併用で低音の芯を保つのがおすすめです。",
			"슬라이더를 더블클릭하면 초기화됩니다. 폭을 넓게 설정할 때는 Mono Bass를 함께 사용해 단단한 저음을 유지하세요.");
		Add("纯同相 · 聚焦中置", "純同相 · 聚焦中置", "In-Phase · Center Focused", "En phase · Centré", "В фазе · Фокус на центр", "En fase · Centrado", "同相 · センター集中", "동위상 · 센터 집중");
		Add("同相 · 原声立体声", "同相 · 原聲立體聲", "In-Phase · Natural Stereo", "En phase · Stéréo naturelle", "В фазе · Обычное стерео", "En fase · Estéreo natural", "同相 · 原音ステレオ", "동위상 · 원음 스테레오");
		Add("开阔 · 空间漫射", "開闊 · 空間漫射", "Open · Spatial Diffusion", "Ouvert · Diffusion spatiale", "Открыто · Диффузия", "Abierto · Difusión espacial", "開放的 · 空間拡散", "개방감 · 공간 확산");
		Add("边缘 · 宽体临界", "邊緣 · 寬體臨界", "Edge · Wide Threshold", "Limite · Seuil large", "Граница · Предел ширины", "Borde · Umbral amplio", "境界 · ワイド限界", "경계 · 광폭 임계");
		Add("反相风险 · 宽体抵消", "反相風險 · 寬體抵消", "Phase Risk · Cancellation", "Risque hors phase", "Риск противофазы", "Riesgo de contrafase", "逆相リスク · 打ち消し注意", "역위상 위험 · 상쇄 주의");
		Add("人声增强强化", "人聲增強強化", "Vocals Boosted", "Voix renforcées", "Вокал усилен", "Voces realzadas", "ボーカル強化", "보컬 강화");
		Add("弱化中心伴奏", "弱化中心伴奏", "Center Attenuated", "Centre atténué", "Центр ослаблен", "Centro atenuado", "センター抑制", "센터 약화");
		Add("空间氛围扩展", "空間氛圍擴展", "Ambience Expanded", "Ambiance étendue", "Объём расширен", "Ambiente expandido", "空間感拡張", "공간감 확장");
		Add("收敛侧声道混响", "收斂側聲道混響", "Side Reverb Tightened", "Réverbération latérale réduite", "Боковая реверберация сжата", "Reverberación lateral contenida", "サイド残響を抑制", "측면 잔향 억제");
		Add("自然空间感", "自然空間感", "Natural Space", "Espace naturel", "Естественное пространство", "Espacio natural", "自然な空間感", "자연스러운 공간감");
		Add("收拢场宽", "收攏場寬", "Narrowed Stage", "Scène resserrée", "Суженная сцена", "Escena estrecha", "狭い音場", "좁아진 음장");

		// Virtual Stage
		Add("虚拟声场", "虛擬聲場", "Virtual Soundstage", "Scène virtuelle", "Виртуальная сцена", "Escenario virtual", "バーチャル音場", "가상 음장");
		Add("已旁路 · 开始试听", "已旁路 · 開始試聽", "Bypassed · Start Audition", "Contourné · Écouter", "Байпас · Начать тест", "En bypass · Probar", "バイパス中 · 試聴を開始", "바이패스됨 · 미리듣기 시작");
		Add("用两耳时间差、频率衰减和短反射模拟音箱空间感，保留中置直达声的位置；适合耳机，开启后会关闭交叉馈送；听感因耳机和个人而异。",
			"用兩耳時間差、頻率衰減和短反射模擬音箱空間感，保留中置直達聲的位置；適合耳機，開啟後會關閉交叉饋送；聽感因耳機和個人而異。",
			"Simulates speaker soundstage using ITD, head-shadowing roll-off, and early reflections while keeping center direct sound intact. Ideal for headphones; disables crossfeed when enabled.",
			"Simule une scène d'enceintes par ITD, atténuation spectrale et réflexions courtes tout en gardant le centre intact. Idéal au casque ; désactive le crossfeed.",
			"Имитирует сцену АС через межушную задержку, частотное затухание и ранние отражения, сохраняя центр. Для наушников; отключает кроссфид.",
			"Simula altavoces mediante ITD, atenuación y reflexiones cortas manteniendo intacto el centro. Ideal para auriculares; desactiva el crossfeed.",
			"両耳時間差・周波数減衰・初期反射でスピーカーの音場感を再現し、センター定位を保ちます。ヘッドホン向け（オン時はクロスフィード無効）。",
			"양귀 시간차, 주파수 감쇠, 초기 반사로 스피커 공간감을 재현하며 중앙 직접음을 유지합니다. 헤드폰에 적합하며 켜면 크로스피드가 꺼집니다.");
		Add("已旁路，保留原始立体声，不添加空间处理。",
			"已旁路，保留原始立體聲，不添加空間處理。",
			"Bypassed; original stereo preserved without spatial processing.",
			"Contourné ; stéréo d'origine conservée sans traitement spatial.",
			"Байпас; оригинальное стерео без пространственной обработки.",
			"En bypass; estéreo original sin procesamiento espacial.",
			"バイパス中。空間処理なしで原音ステレオを維持しています。",
			"바이패스됨. 공간 처리 없이 원본 스테레오를 유지합니다.");
		Add("参数实时同步。点击预设开始试听，使用电源按钮对比原声。",
			"參數即時同步。點擊預設開始試聽，使用電源按鈕對比原聲。",
			"Parameters sync in real time. Click a preset to audition and use the power button to A/B.",
			"Paramètres synchronisés en temps réel. Cliquez sur un préréglage pour écouter.",
			"Параметры применяются мгновенно. Нажмите пресет для теста и сравнивайте кнопкой питания.",
			"Parámetros sincronizados en tiempo real. Pulsa un ajuste para probar.",
			"パラメータはリアルタイム同期されます。プリセットで試聴し、電源ボタンで原音と比較できます。",
			"파라미터가 실시간 동기화됩니다. 프리셋을 눌러 청취하고 전원 버튼으로 원음과 비교하세요.");
		Add("中置", "中置", "Center", "Centre", "Центр", "Centro", "センター", "센터");
		Add("左", "左", "L", "G", "Л", "I", "左", "좌");
		Add("右", "右", "R", "D", "П", "D", "右", "우");
		Add("模拟 ±30°", "模擬 ±30°", "Sim ±30°", "Sim ±30°", "Сим. ±30°", "Sim ±30°", "仮想 ±30°", "가상 ±30°");
		Add("模拟 ±{0}°", "模擬 ±{0}°", "Sim ±{0}°", "Sim ±{0}°", "Сим. ±{0}°", "Sim ±{0}°", "仮想 ±{0}°", "가상 ±{0}°");
		Add("模拟角度", "模擬角度", "Virtual Angle", "Angle simulé", "Угол сцены", "Ángulo simulado", "仮想角度", "가상 각도");
		Add("反射量", "反射量", "Reflections", "Réflexions", "Отражения", "Reflexiones", "反射量", "반사량");
		Add("直达声居中", "直達聲居中", "Direct Center", "Son direct centré", "Прямой центр", "Centro directo", "直接音センター", "직접음 중앙");
		Add("HRTF 声场", "HRTF 聲場", "HRTF Soundstage", "Scène HRTF", "HRTF сцена", "Escena HRTF", "HRTF 音場", "HRTF 음장");
		Add("导入 SOFA", "匯入 SOFA", "Import SOFA", "Importer SOFA", "Импорт SOFA", "Importar SOFA", "SOFA インポート", "SOFA 가져오기");
		Add("方案", "方案", "Profile", "Profil", "Профиль", "Perfil", "プロファイル", "프로필");
		Add("SADIE II · 标准", "SADIE II · 標準", "SADIE II · Standard", "SADIE II · Standard", "SADIE II · Стандарт", "SADIE II · Estándar", "SADIE II · 標準", "SADIE II · 표준");
		Add("KEMAR · 扩散场", "KEMAR · 擴散場", "KEMAR · Diffuse Field", "KEMAR · Champ diffus", "KEMAR · Диффузное поле", "KEMAR · Campo difuso", "KEMAR · 拡散音場", "KEMAR · 확산음장");
		Add("IRCAM · 监听", "IRCAM · 監聽", "IRCAM · Monitor", "IRCAM · Moniteur", "IRCAM · Монитор", "IRCAM · Monitor", "IRCAM · モニター", "IRCAM · 모니터");
		Add("Furina 3D 沉浸", "Furina 3D 沉浸", "Furina 3D Immersive", "Furina 3D Immersif", "Furina 3D Погружение", "Furina 3D Inmersivo", "Furina 3D イマーシブ", "Furina 3D 몰입");
		Add("试听匹配", "試聽匹配", "Audition Match", "Test comparatif", "Подбор на слух", "Prueba comparativa", "試聴マッチング", "청취 매칭");
		Add("用当前音乐切换比较，选择声像与音色最自然的一项。",
			"用目前音樂切換比較，選擇聲像與音色最自然的一項。",
			"Switch between profiles with your current track and choose the most natural imaging and timbre.",
			"Comparez les profils avec votre musique et choisissez le plus naturel.",
			"Переключайте профили на текущей музыке и выберите наиболее естественный.",
			"Alterna perfiles con tu música actual y elige el más natural.",
			"再生中の音楽で切り替えて、最も自然な音像と音色を選んでください。",
			"현재 재생 중인 음악으로 전환하며 가장 자연스러운 음상과 음색을 선택하세요.");
		Add("快速预设", "快速預設", "Quick Presets", "Préréglages rapides", "Быстрые пресеты", "Ajustes rápidos", "クイックプリセット", "빠른 프리셋");
		Add("轻度", "輕度", "Light", "Léger", "Лёгкий", "Suave", "軽め", "약하게");
		Add("轻度：强度 25% · 角度 ±20° · 反射 40%", "輕度：強度 25% · 角度 ±20° · 反射 40%", "Light: Strength 25% · Angle ±20° · Reflections 40%", "Léger : Force 25 % · Angle ±20° · Réflexions 40 %", "Лёгкий: Сила 25% · Угол ±20° · Отраж. 40%", "Suave: Fuerza 25% · Ángulo ±20° · Refl. 40%", "軽め：強度 25% · 角度 ±20° · 反射 40%", "약하게: 강도 25% · 각도 ±20° · 반사 40%");
		Add("自然", "自然", "Natural", "Naturel", "Естеств.", "Natural", "自然", "자연스럽게");
		Add("自然：强度 45% · 角度 ±30° · 反射 100%", "自然：強度 45% · 角度 ±30° · 反射 100%", "Natural: Strength 45% · Angle ±30° · Reflections 100%", "Naturel : Force 45 % · Angle ±30° · Réflexions 100 %", "Естеств.: Сила 45% · Угол ±30° · Отраж. 100%", "Natural: Fuerza 45% · Ángulo ±30° · Refl. 100%", "自然：強度 45% · 角度 ±30° · 反射 100%", "자연스럽게: 강도 45% · 각도 ±30° · 반사 100%");
		Add("强烈", "強烈", "Strong", "Intense", "Сильный", "Intenso", "強め", "강하게");
		Add("强烈：强度 70% · 角度 ±45° · 反射 100%", "強烈：強度 70% · 角度 ±45° · 反射 100%", "Strong: Strength 70% · Angle ±45° · Reflections 100%", "Intense : Force 70 % · Angle ±45° · Réflexions 100 %", "Сильный: Сила 70% · Угол ±45° · Отраж. 100%", "Intenso: Fuerza 70% · Ángulo ±45° · Refl. 100%", "強め：強度 70% · 角度 ±45° · 反射 100%", "강하게: 강도 70% · 각도 ±45° · 반사 100%");
		Add("参数控制", "參數控制", "Parameters", "Paramètres", "Параметры", "Parámetros", "パラメータ", "파라미터 제어");
		Add("强度", "強度", "Strength", "Intensité", "Сила", "Intensidad", "強度", "강도");
		Add("已开启", "已開啟", "Enabled", "Activé", "Включено", "Activado", "オン", "켜짐");
		Add("强度为 0%，平滑过渡后恢复原始立体声。提高强度可加入空间线索。",
			"強度為 0%，平滑過渡後恢復原始立體聲。提高強度可加入空間線索。",
			"Strength is 0%; smoothly fades back to original stereo. Increase strength to add spatial cues.",
			"Intensité à 0 % ; retour fluide à la stéréo d'origine.",
			"Сила 0%; плавный возврат к исходному стерео.",
			"Intensidad al 0%; vuelve suavemente al estéreo original.",
			"強度 0%。滑らかに原音ステレオへ戻ります。強度を上げると空間手がかりが加わります。",
			"강도가 0%여서 원본 스테레오로 부드럽게 복귀합니다. 강도를 높이면 공간감이 추가됩니다.");
		Add("已开启 · 虚拟空间声场渲染中（HRTF: {0} · 模拟角度 ±{1}°）",
			"已開啟 · 虛擬空間聲場渲染中（HRTF: {0} · 模擬角度 ±{1}°）",
			"Active · Rendering virtual soundstage (HRTF: {0} · Angle ±{1}°)",
			"Actif · Rendu de scène virtuelle (HRTF : {0} · Angle ±{1}°)",
			"Активно · Виртуальная сцена (HRTF: {0} · Угол ±{1}°)",
			"Activo · Renderizando escena virtual (HRTF: {0} · Ángulo ±{1}°)",
			"有効 · バーチャル音場レンダリング中（HRTF: {0} · 角度 ±{1}°）",
			"켜짐 · 가상 음장 렌더링 중 (HRTF: {0} · 각도 ±{1}°)");
		Add("已开启 · 虚拟空间声场渲染中", "已開啟 · 虛擬空間聲場渲染中", "Active · Rendering virtual soundstage", "Actif · Rendu de scène virtuelle", "Активно · Рендеринг виртуальной сцены", "Activo · Renderizando escena virtual", "有効 · バーチャル音場レンダリング中", "켜짐 · 가상 음장 렌더링 중");

		// Volume Equalization (ReplayGain)
		Add("音量均衡", "音量均衡", "Volume Equalization", "Égalisation du volume", "Выравнивание громкости", "Nivelación de volumen", "音量ノーマライズ", "음량 균일화");
		Add("曲库响度", "曲庫響度", "Library Loudness", "Sonie de la bibliothèque", "Громкость библиотеки", "Sonoridad de biblioteca", "ライブラリラウドネス", "라이브러리 라우드니스");
		Add("先补齐缺失的曲库响度，再按目标均衡。分析会等待暂停或停止播放。",
			"先補齊缺失的曲庫響度，再依目標均衡。分析會等待暫停或停止播放。",
			"Scan missing library loudness first, then level to target. Analysis waits until playback pauses or stops.",
			"Analysez d'abord la sonie manquante, puis égalisez selon la cible. L'analyse attend la pause ou l'arrêt.",
			"Сначала проанализируйте недостающую громкость, затем выравнивайте по цели. Анализ ждёт паузы или остановки.",
			"Analiza primero la sonoridad faltante y luego nivela al objetivo. El análisis espera pausa o detención.",
			"まず不足しているライブラリのラウドネスを補完してから目標値で均等化します。解析は一時停止または停止時に行われます。",
			"누락된 라이브러리 라우드니스를 먼저 채운 뒤 목표값에 맞춰 균일화합니다. 분석은 일시정지 또는 정지 시 진행됩니다.");
		Add("正在排队分析曲目响度...", "正在排隊分析曲目響度...", "Queuing track loudness analysis...", "Mise en file d'attente de l'analyse...", "Очередь анализа громкости треков...", "Encolando análisis de sonoridad...", "トラックラウドネス解析を待機中...", "트랙 라우드니스 분석 대기 중...");
		Add("查看响度数据", "查看響度資料", "View Loudness Data", "Voir les données", "Данные громкости", "Ver datos de sonoridad", "ラウドネスデータを見る", "라우드니스 데이터 보기");
		Add("补齐曲库响度", "補齊曲庫響度", "Scan Library Loudness", "Analyser la bibliothèque", "Анализ библиотеки", "Analizar biblioteca", "ライブラリを解析", "라이브러리 라우드니스 분석");
		Add("取消分析", "取消分析", "Cancel Analysis", "Annuler l'analyse", "Отменить анализ", "Cancelar análisis", "解析をキャンセル", "분석 취소");
		Add("正在扫描曲库响度... (0%)", "正在掃描曲庫響度... (0%)", "Scanning library loudness... (0%)", "Analyse de la bibliothèque... (0 %)", "Сканирование громкости... (0%)", "Analizando biblioteca... (0%)", "ライブラリラウドネスをスキャン中... (0%)", "라이브러리 라우드니스 스캔 중... (0%)");
		Add("正在测量曲库响度... {0}% ({1}/10 专辑已校验)", "正在測量曲庫響度... {0}% ({1}/10 專輯已校驗)", "Measuring library loudness... {0}% ({1}/10 albums verified)", "Mesure de la sonie... {0} % ({1}/10 albums vérifiés)", "Измерение громкости... {0}% ({1}/10 альбомов)", "Midiendo sonoridad... {0}% ({1}/10 álbumes)", "ラウドネス計測中... {0}% ({1}/10 アルバム確認済み)", "라우드니스 측정 중... {0}% ({1}/10 앨범 확인됨)");
		Add("曲库响度分析完成，已全部对齐。", "曲庫響度分析完成，已全部對齊。", "Library loudness analysis complete; all tracks aligned.", "Analyse terminée ; tous les titres sont alignés.", "Анализ завершён; все треки выровнены.", "Análisis completado; todo alineado.", "ライブラリのラウドネス解析が完了し、すべて揃いました。", "라이브러리 라우드니스 분석이 완료되어 모두 정렬되었습니다.");
		Add("已取消分析。", "已取消分析。", "Analysis cancelled.", "Analyse annulée.", "Анализ отменён.", "Análisis cancelado.", "解析がキャンセルされました。", "분석이 취소되었습니다.");
		Add("分析已取消。", "分析已取消。", "Analysis cancelled.", "Analyse annulée.", "Анализ отменён.", "Análisis cancelado.", "解析はキャンセルされました。", "분석이 취소되었습니다.");
		Add("目标响度", "目標響度", "Target Loudness", "Sonie cible", "Целевая громкость", "Sonoridad objetivo", "目標ラウドネス", "목표 라우드니스");
		Add("未开启", "未開啟", "Off", "Désactivé", "Выкл", "Desactivado", "オフ", "꺼짐");
		Add("让不同歌曲的音量更接近，减少切歌时的忽大忽小。不改写音乐文件。",
			"讓不同歌曲的音量更接近，減少切歌時的忽大忽小。不改寫音樂檔案。",
			"Keeps volume consistent across tracks and reduces sudden jumps without modifying audio files.",
			"Uniformise le volume entre les morceaux sans modifier les fichiers audio.",
			"Выравнивает громкость разных треков без изменения исходных файлов.",
			"Iguala el volumen entre canciones sin modificar los archivos de audio.",
			"曲ごとの音量差を抑え、再生切り替え時の急な大小を防ぎます。音楽ファイルは書き換えません。",
			"곡 간 음량 차이를 줄여 트랙 전환 시 급격한 볼륨 변화를 방지합니다. 음악 파일은 수정하지 않습니다.");
		Add("综合响度", "綜合響度", "Integrated", "Sonie intégrée", "Интегральная", "Integrada", "統合ラウドネス", "통합 라우드니스");
		Add("日常聆听 · 推荐", "日常聆聽 · 推薦", "Daily · Recommended", "Quotidien · Recommandé", "Повседневный · Реком.", "Diario · Recomendado", "日常リスニング · 推奨", "일상 청취 · 권장");
		Add("安静", "安靜", "Quiet", "Calme", "Тихий", "Suave", "静か", "조용하게");
		Add("自定义目标", "自訂目標", "Custom Target", "Cible personnalisée", "Своя цель", "Objetivo personalizado", "カスタム目標", "사용자 지정 목표");
		Add("模式", "模式", "Mode", "Mode", "Режим", "Modo", "モード", "모드");
		Add("自动", "自動", "Auto", "Auto", "Авто", "Auto", "自動", "자동");
		Add("连续播放同一专辑用专辑增益；混合播放用单曲增益。",
			"連續播放同一專輯用專輯增益；混合播放用單曲增益。",
			"Uses Album gain for consecutive album tracks; Track gain for mixed playlists.",
			"Gain d'album pour un album complet ; gain de piste en lecture mixte.",
			"Усиление альбома при подряд идущих треках альбома; трека — в миксе.",
			"Usa ganancia de álbum en álbumes seguidos y de pista en listas mixtas.",
			"同一アルバムの連続再生時はアルバムゲイン、シャッフル等ではトラックゲインを使用。",
			"같은 앨범 연속 재생 시 앨범 게인, 혼합 재생 시 트랙 게인을 사용합니다.");
		Add("单曲", "單曲", "Track", "Piste", "Трек", "Pista", "トラック", "트랙");
		Add("适合随机播放，让每首歌的音量更接近。", "適合隨機播放，讓每首歌的音量更接近。", "Ideal for shuffle; matches loudness per track.", "Idéal en aléatoire ; égalise chaque piste.", "Для случайного порядка; выравнивает каждый трек.", "Ideal para aleatorio; iguala cada canción.", "シャッフル再生に最適。曲ごとに音量を揃えます。", "셔플 재생에 적합하며 각 곡의 음량을 비슷하게 맞춥니다.");
		Add("专辑", "專輯", "Album", "Album", "Альбом", "Álbum", "アルバム", "앨범");
		Add("适合整张专辑，保留曲目之间原有的强弱。", "適合整張專輯，保留曲目之間原有的強弱。", "Ideal for full albums; preserves intended track-to-track dynamics.", "Idéal pour un album ; préserve les écarts d'origine.", "Для целых альбомов; сохраняет задуманную динамику.", "Para álbumes completos; preserva la dinámica original.", "アルバム通しての再生に最適。曲間の強弱バランスを保ちます。", "앨범 전체 감상에 적합하며 트랙 간 원래 강약 차이를 유지합니다.");
		Add("前级增益", "前級增益", "Preamp", "Préampli", "Предусиление", "Preamplificador", "プリアンプゲイン", "프리앰프 게인");
		Add("当前状态", "目前狀態", "Current Status", "État actuel", "Текущее состояние", "Estado actual", "現在の状態", "현재 상태");
		Add("刷新", "重新整理", "Refresh", "Actualiser", "Обновить", "Actualizar", "更新", "새로고침");
		Add("当前应用  {0:+0.0;-0.0;0.0} dB", "目前套用  {0:+0.0;-0.0;0.0} dB", "Applied  {0:+0.0;-0.0;0.0} dB", "Appliqué  {0:+0.0;-0.0;0.0} dB", "Применено  {0:+0.0;-0.0;0.0} дБ", "Aplicado  {0:+0.0;-0.0;0.0} dB", "適用中  {0:+0.0;-0.0;0.0} dB", "현재 적용  {0:+0.0;-0.0;0.0} dB");
		Add("当前应用  --", "目前套用  --", "Applied  --", "Appliqué  --", "Применено  --", "Aplicado  --", "適用中  --", "현재 적용  --");
		Add("音量均衡已生效。系统将自动按目标响度调节各曲目播放增益，保障听感一致。",
			"音量均衡已生效。系統將自動依目標響度調節各曲目播放增益，保障聽感一致。",
			"Volume equalization is active. Playback gain is automatically adjusted to your target loudness.",
			"L'égalisation est active. Le gain est ajusté automatiquement selon la sonie cible.",
			"Выравнивание активно. Усиление автоматически подстраивается под целевую громкость.",
			"Nivelación activa. La ganancia se ajusta automáticamente a la sonoridad objetivo.",
			"音量ノーマライズが有効です。目標ラウドネスに合わせて各曲のゲインを自動調整します。",
			"음량 균일화가 적용 중입니다. 목표 라우드니스에 맞춰 각 곡의 재생 게인을 자동 조절합니다.");
		Add("请先补齐曲库响度。分析结果会保存在曲库中，下次播放直接使用。",
			"請先補齊曲庫響度。分析結果會儲存在曲庫中，下次播放直接使用。",
			"Scan library loudness first. Results are saved in your library for instant use next time.",
			"Analysez d'abord la bibliothèque. Les résultats sont enregistrés pour les écoutes suivantes.",
			"Сначала проанализируйте библиотеку. Результаты сохраняются для последующих воспроизведений.",
			"Analiza primero la biblioteca. Los resultados se guardan para futuras reproducciones.",
			"先にライブラリのラウドネスを解析してください。結果は保存され次回から即利用できます。",
			"먼저 라이브러리 라우드니스를 분석하세요. 분석 결과는 라이브러리에 저장되어 다음 재생 시 바로 사용됩니다.");
		Add("高级设置", "進階設定", "Advanced Settings", "Paramètres avancés", "Дополнительные настройки", "Ajustes avanzados", "詳細設定", "고급 설정");
		Add("选用增益", "選用增益", "Selected Gain", "Gain choisi", "Выбранное усиление", "Ganancia elegida", "選択ゲイン", "선택된 게인");
		Add("音频峰值", "音訊峰值", "Audio Peak", "Crête audio", "Пик аудио", "Pico de audio", "オーディオピーク", "오디오 피크");
		Add("曲目增益", "曲目增益", "Track Gain", "Gain piste", "Усиление трека", "Ganancia pista", "トラックゲイン", "트랙 게인");
		Add("专辑增益", "專輯增益", "Album Gain", "Gain album", "Усиление альбома", "Ganancia álbum", "アルバムゲイン", "앨범 게인");
		Add("数据来源", "資料來源", "Data Source", "Source", "Источник", "Fuente", "データソース", "데이터 출처");
		Add("当前模式", "目前模式", "Active Mode", "Mode actif", "Текущий режим", "Modo actual", "現在のモード", "현재 모드");
		Add("防削波限幅", "防削波限幅", "Clip Limiting", "Limiteur d'écrêtage", "Защита от клиппинга", "Límite de recorte", "クリッピング制限", "클리핑 제한");
		Add("响度未知时", "響度未知時", "When Loudness Unknown", "Si sonie inconnue", "Если громкость неизвестна", "Si se desconoce sonoridad", "ラウドネス不明時", "라우드니스 미확인 시");
		Add("默认衰减 5 dB。0 dB 关闭保护；前置增益不作用于未知响度。",
			"預設衰減 5 dB。0 dB 關閉保護；前置增益不作用於未知響度。",
			"Defaults to -5 dB attenuation. Set 0 dB to disable; preamp does not apply to unknown tracks.",
			"Atténuation par défaut de 5 dB (0 dB pour désactiver) ; le préampli ne s'applique pas.",
			"По умолчанию ослабление 5 дБ (0 дБ отключает); предусиление не применяется.",
			"Atenuación por defecto de 5 dB (0 dB desactiva); el preamplificador no se aplica.",
			"デフォルト -5 dB 減衰。0 dB で保護オフ；不明な曲にはプリアンプは適用されません。",
			"기본값 5 dB 감쇠. 0 dB로 보호 끄기; 알 수 없는 곡에는 프리앰프가 적용되지 않습니다.");
		Add("防爆音 / 防削波保护", "防爆音 / 防削波保護", "Anti-Clipping Protection", "Protection anti-écrêtage", "Защита от клиппинга", "Protección anticorte", "クリッピング防止保護", "클리핑 방지 보호");
		Add("当增益导致峰值超过 0 dBFS 时自动下调，避免削波失真。",
			"當增益導致峰值超過 0 dBFS 時自動下調，避免削波失真。",
			"Automatically attenuates gain if peak would exceed 0 dBFS to prevent clipping.",
			"Réduit automatiquement le gain si la crête dépasse 0 dBFS.",
			"Автоматически снижает усиление при риске превышения 0 dBFS.",
			"Reduce la ganancia automáticamente si el pico supera 0 dBFS.",
			"ピークが 0 dBFS を超える場合に自動でゲインを下げ、歪みを防ぎます。",
			"게인으로 인해 피크가 0 dBFS를 넘을 경우 자동으로 낮춰 클리핑 왜곡을 방지합니다.");
		Add("扫描后分析", "掃描後分析", "Analyze on Scan", "Analyser après scan", "Анализ после сканирования", "Analizar al escanear", "スキャン後に解析", "스캔 후 분석");
		Add("开启真峰值扫描 (True Peak)", "開啟真峰值掃描 (True Peak)", "Enable True Peak Scanning", "Activer le scan True Peak", "Включить сканирование True Peak", "Activar escaneo True Peak", "トゥルーピークスキャンを有効化", "트루 피크 스캔 활성화");
		Add("响度分析协议", "響度分析協定", "Loudness Standard", "Norme d'analyse", "Протокол анализа", "Protocolo de análisis", "ラウドネス解析規格", "라우드니스 분석 규격");
		Add("恢复推荐设置", "恢復推薦設定", "Restore Recommended", "Rétablir recommandés", "Сбросить к рекомендуемым", "Restaurar recomendados", "推奨設定に戻す", "권장 설정 복원");
		Add("自动 (单曲/专辑)", "自動 (單曲/專輯)", "Auto (Track/Album)", "Auto (Piste/Album)", "Авто (Трек/Альбом)", "Auto (Pista/Álbum)", "自動 (トラック/アルバム)", "자동 (트랙/앨범)");
		Add("单曲 (Track)", "單曲 (Track)", "Track", "Piste (Track)", "Трек (Track)", "Pista (Track)", "トラック (Track)", "트랙 (Track)");
		Add("专辑 (Album)", "專輯 (Album)", "Album", "Album", "Альбом (Album)", "Álbum (Album)", "アルバム (Album)", "앨범 (Album)");
		Add("已限幅", "已限幅", "Limited", "Limité", "Ограничено", "Limitado", "制限あり", "제한됨");
		Add("未限幅", "未限幅", "Not Limited", "Non limité", "Без ограничений", "Sin límite", "制限なし", "제한 안 됨");
		Add("曲库已实测 100% 音频文件。所有曲目已具备 ITU-R BS.1770-4 综合响度与真峰值数据。",
			"曲庫已實測 100% 音訊檔案。所有曲目已具備 ITU-R BS.1770-4 綜合響度與真峰值資料。",
			"100% of library audio files measured. All tracks have ITU-R BS.1770-4 integrated loudness and true peak data.",
			"100 % de la bibliothèque mesurée selon ITU-R BS.1770-4 (sonie intégrée et crête réelle).",
			"Измерено 100% файлов библиотеки. Все треки имеют данные ITU-R BS.1770-4 и True Peak.",
			"100% de la biblioteca medida con datos ITU-R BS.1770-4 y True Peak.",
			"ライブラリ内の 100% の音声ファイルを測定済みです（ITU-R BS.1770-4 統合ラウドネス・トゥルーピーク完備）。",
			"라이브러리 오디오 파일 100% 측정 완료. 모든 트랙에 ITU-R BS.1770-4 통합 라우드니스 및 트루 피크 데이터가 있습니다.");
		Add("分析协议：", "分析協定：", "Standard:", "Norme :", "Протокол:", "Protocolo:", "解析規格：", "분석 규격:");
		Add("参考目标响度：", "參考目標響度：", "Target Loudness:", "Sonie cible :", "Целевая громкость:", "Sonoridad objetivo:", "基準目標ラウドネス：", "기준 목표 라우드니스:");
		Add("综合响度范围 (LRA)：", "綜合響度範圍 (LRA)：", "Loudness Range (LRA):", "Plage dynamique (LRA) :", "Диапазон (LRA):", "Rango (LRA):", "ラウドネスレンジ (LRA)：", "라우드니스 범위 (LRA):");
		Add("True Peak 过采样：", "True Peak 過取樣：", "True Peak Oversampling:", "Suréchantillonnage True Peak :", "Передискр. True Peak:", "Sobremuestreo True Peak:", "True Peak オーバーサンプリング：", "True Peak 오버샘플링:");
		Add("削波保护门限：", "削波保護門檻：", "Clipping Ceiling:", "Seuil de protection :", "Порог клиппинга:", "Límite de recorte:", "クリッピング保護しきい値：", "클리핑 보호 한계:");
		Add("曲库响度分析数据", "曲庫響度分析資料", "Library Loudness Data", "Données de sonie", "Данные громкости библиотеки", "Datos de sonoridad", "ライブラリラウドネスデータ", "라이브러리 라우드니스 데이터");
		Add("确定", "確定", "OK", "OK", "ОК", "Aceptar", "OK", "확인");
		Add("测量 LUFS", "測量 LUFS", "Measured LUFS", "LUFS mesuré", "Измерено LUFS", "LUFS medido", "実測 LUFS", "측정 LUFS");

		// VST3 Rack
		Add("VST3 机架 / 插件管理器", "VST3 機架 / 外掛程式管理器", "VST3 Rack / Plugin Manager", "Rack VST3 / Gestionnaire", "Стойка VST3 / Менеджер", "Rack VST3 / Gestor de plugins", "VST3 ラック / プラグイン管理", "VST3 랙 / 플러그인 관리자");
		Add("Steinberg VST3 64位原生宿主 · 支持实时音频流宿主渲染与原生插件 GUI 窗口交互",
			"Steinberg VST3 64 位元原生宿主 · 支援即時音訊流宿主算圖與原生外掛 GUI 視窗互動",
			"Steinberg VST3 64-bit native host · Real-time audio processing & native plugin GUI windows",
			"Hôte natif Steinberg VST3 64 bits · Traitement temps réel et fenêtres GUI natives",
			"Нативный 64-бит хост Steinberg VST3 · Обработка в реальном времени и нативные окна GUI",
			"Host nativo Steinberg VST3 64 bits · Audio en tiempo real y ventanas GUI nativas",
			"Steinberg VST3 64bit ネイティブホスト · リアルタイム音声処理＆ネイティブプラグイン GUI 対応",
			"Steinberg VST3 64비트 네이티브 호스트 · 실시간 오디오 처리 및 네이티브 플러그인 GUI 창 지원");
		Add("🔍 扫描目录", "🔍 掃描目錄", "🔍 Scan Folders", "🔍 Scanner les dossiers", "🔍 Сканировать папки", "🔍 Escanear carpetas", "🔍 フォルダをスキャン", "🔍 폴더 스캔");
		Add("➕ 加载 .vst3 文件", "➕ 載入 .vst3 檔案", "➕ Load .vst3 File", "➕ Charger un .vst3", "➕ Загрузить .vst3", "➕ Cargar archivo .vst3", "➕ .vst3 を読み込む", "➕ .vst3 파일 불러오기");
		Add("实时 VST3 宿主信号链路 (Active VST3 Signal Chain)",
			"即時 VST3 宿主訊號鏈路 (Active VST3 Signal Chain)",
			"Active VST3 Signal Chain",
			"Chaîne de signal VST3 active",
			"Активная цепь сигнала VST3",
			"Cadena de señal VST3 activa",
			"アクティブ VST3 シグナルチェーン",
			"실시간 VST3 호스트 신호 체인 (Active VST3 Signal Chain)");
		Add("宿主就绪", "宿主就緒", "Host Ready", "Hôte prêt", "Хост готов", "Host listo", "ホスト準備完了", "호스트 준비됨");
		Add("音频解码输入", "音訊解碼輸入", "Decoded Audio In", "Entrée audio décodée", "Вход декодированного аудио", "Entrada de audio decodificado", "デコード済み音声入力", "오디오 디코딩 입력");
		Add("FurinaPlayer DSP 前级链路", "FurinaPlayer DSP 前級鏈路", "FurinaPlayer DSP Pre-Chain", "Pré-chaîne DSP FurinaPlayer", "Пре-цепь DSP FurinaPlayer", "Pre-cadena DSP FurinaPlayer", "FurinaPlayer DSP 前段チェーン", "FurinaPlayer DSP 전단 체인");
		Add("EQ / 压缩 / 空间处理", "EQ / 壓縮 / 空間處理", "EQ / Dynamics / Spatial", "EQ / Compresseur / Spatial", "EQ / Компрессор / Пространство", "EQ / Compresor / Espacial", "EQ / コンプ / 空間処理", "EQ / 컴프레서 / 공간 처리");
		Add("VST3 原生宿主插槽", "VST3 原生宿主插槽", "VST3 Native Host Slot", "Slot hôte natif VST3", "Нативный слот VST3", "Ranura host nativa VST3", "VST3 ネイティブホストスロット", "VST3 네이티브 호스트 슬롯");
		Add("Active Slot (待命)", "Active Slot (待命)", "Active Slot (Standby)", "Active Slot (En veille)", "Active Slot (Ожидание)", "Active Slot (En espera)", "Active Slot (待機中)", "Active Slot (대기)");
		Add("原生 GUI 编辑器", "原生 GUI 編輯器", "Native GUI Editor", "Éditeur GUI natif", "Нативный GUI редактор", "Editor GUI nativo", "ネイティブ GUI エディタ", "네이티브 GUI 편집기");
		Add("IPlugView 窗口", "IPlugView 視窗", "IPlugView Window", "Fenêtre IPlugView", "Окно IPlugView", "Ventana IPlugView", "IPlugView ウィンドウ", "IPlugView 창");
		Add("安全限幅输出", "安全限幅輸出", "Safe Limiter Out", "Sortie limiteur de sécurité", "Выход с лимитером", "Salida con limitador", "安全リミッター出力", "안전 리미터 출력");
		Add("防削波直通音频设备", "防削波直通音訊裝置", "Clip-safe direct to device", "Vers périphérique sans écrêtage", "На устройство без клиппинга", "Directo al dispositivo sin recorte", "クリップ保護付きデバイス出力", "클리핑 방지 후 오디오 장치 출력");
		Add("当前挂载插件：", "目前掛載外掛：", "Active Plugin:", "Plugin actif :", "Активный плагин:", "Plugin activo:", "現在のプラグイン：", "현재 장착된 플러그인:");
		Add("未启用插件 (直通模式)", "未啟用外掛 (直通模式)", "No Plugin Active (Bypass)", "Aucun plugin actif (Direct)", "Нет активного плагина (Байпас)", "Sin plugin activo (Directo)", "プラグイン未有効 (バイパス)", "플러그인 미사용 (바이패스 모드)");
		Add("待命 (直通)", "待命 (直通)", "Standby (Bypass)", "En veille (Direct)", "Ожидание (Байпас)", "En espera (Directo)", "待機中 (バイパス)", "대기 (바이패스)");
		Add("厂商 (Vendor)", "廠商 (Vendor)", "Vendor", "Éditeur (Vendor)", "Издатель (Vendor)", "Fabricante (Vendor)", "ベンダー (Vendor)", "제조사 (Vendor)");
		Add("版本 (Version)", "版本 (Version)", "Version", "Version", "Версия (Version)", "Versión (Version)", "バージョン (Version)", "버전 (Version)");
		Add("宿主状态 (Host)", "宿主狀態 (Host)", "Host Status", "État hôte (Host)", "Статус хоста (Host)", "Estado del host (Host)", "ホスト状態 (Host)", "호스트 상태 (Host)");
		Add("采样率与声道", "取樣率與聲道", "Sample Rate & Channels", "Fréquence et canaux", "Частота и каналы", "Frecuencia y canales", "サンプルレートとチャンネル", "샘플레이트 및 채널");
		Add("插件路径：可在下方机架列表中启用任意 VST3 插件，或直接点击“🎛️ 界面”一键挂载并打开原生窗口。",
			"外掛路徑：可在下方機架清單中啟用任意 VST3 外掛，或直接點擊「🎛️ 介面」一鍵掛載並開啟原生視窗。",
			"Plugin path: Enable any VST3 plugin below, or click \"🎛️ UI\" to mount and open its native window.",
			"Chemin : activez un plugin VST3 ci-dessous ou cliquez sur « 🎛️ Interface » pour ouvrir sa fenêtre native.",
			"Путь: включите любой VST3-плагин ниже или нажмите «🎛️ Интерфейс» для открытия нативного окна.",
			"Ruta: activa cualquier plugin VST3 abajo o pulsa «🎛️ Interfaz» para abrir su ventana nativa.",
			"プラグインパス：下のラックから VST3 プラグインを有効化するか、「🎛️ 画面」でネイティブウィンドウを開けます。",
			"플러그인 경로: 아래 랙 목록에서 VST3 플러그인을 활성화하거나 '🎛️ 창'을 클릭해 네이티브 창을 여세요.");
		Add("插件路径：{0}", "外掛路徑：{0}", "Plugin path: {0}", "Chemin : {0}", "Путь плагина: {0}", "Ruta del plugin: {0}", "プラグインパス：{0}", "플러그인 경로: {0}");
		Add("🎛️ 打开选中/活动插件原生界面", "🎛️ 開啟選中/活動外掛原生介面", "🎛️ Open Active Plugin UI", "🎛️ Ouvrir l'interface native", "🎛️ Открыть окно плагина", "🎛️ Abrir interfaz nativa", "🎛️ プラグインネイティブ画面を開く", "🎛️ 활성 플러그인 네이티브 창 열기");
		Add("⏸ 旁通当前插件", "⏸ 旁通目前外掛", "⏸ Bypass Active Plugin", "⏸ Contourner le plugin", "⏸ Байпас плагина", "⏸ Bypass del plugin", "⏸ 現在のプラグインをバイパス", "⏸ 현재 플러그인 바이패스");
		Add("▶ 恢复插件处理", "▶ 恢復外掛處理", "▶ Resume Plugin", "▶ Reprendre le plugin", "▶ Возобновить плагин", "▶ Reanudar plugin", "▶ プラグイン処理を再開", "▶ 플러그인 처리 재개");
		Add("⏹ 全部停用 (卸载宿主)", "⏹ 全部停用 (卸載宿主)", "⏹ Disable All (Unload Host)", "⏹ Tout désactiver (Décharger)", "⏹ Отключить все (Выгрузить)", "⏹ Desactivar todo (Descargar)", "⏹ すべて無効化 (ホスト解放)", "⏹ 모두 비활성화 (호스트 언로드)");
		Add("扫描目录管理", "掃描目錄管理", "Scan Folders", "Dossiers de scan", "Папки сканирования", "Carpetas de escaneo", "スキャンフォルダ管理", "스캔 폴더 관리");
		Add("管理 VST3 插件搜索目录（无高度截断）", "管理 VST3 外掛搜尋目錄（無高度截斷）", "Manage VST3 plugin search directories", "Gérer les dossiers de recherche VST3", "Управление папками поиска VST3", "Gestionar carpetas de búsqueda VST3", "VST3 プラグイン検索フォルダを管理", "VST3 플러그인 검색 폴더 관리");
		Add("0 个目录", "0 個目錄", "0 folders", "0 dossier", "0 папок", "0 carpetas", "0 フォルダ", "0개 폴더");
		Add("{0} 个目录", "{0} 個目錄", "{0} folders", "{0} dossiers", "Папок: {0}", "{0} carpetas", "{0} フォルダ", "{0}개 폴더");
		Add("⚡ 立即扫描全部目录", "⚡ 立即掃描全部目錄", "⚡ Scan All Folders Now", "⚡ Scanner tout maintenant", "⚡ Сканировать все папки", "⚡ Escanear todo ahora", "⚡ すべてのフォルダをスキャン", "⚡ 모든 폴더 즉시 스캔");
		Add("📁 添加扫描目录", "📁 新增掃描目錄", "📁 Add Scan Folder", "📁 Ajouter un dossier", "📁 Добавить папку", "📁 Añadir carpeta", "📁 フォルダを追加", "📁 스캔 폴더 추가");
		Add("恢复默认目录", "恢復預設目錄", "Restore Defaults", "Dossiers par défaut", "Папки по умолчанию", "Restaurar carpetas", "デフォルトに戻す", "기본 폴더 복원");
		Add("搜索插件名称、厂商 (如 Insight, Melodyne, FabFilter, iZotope)...",
			"搜尋外掛名稱、廠商 (如 Insight, Melodyne, FabFilter, iZotope)...",
			"Search plugin name or vendor (e.g. Insight, Melodyne, FabFilter, iZotope)...",
			"Rechercher un plugin ou éditeur (ex. Insight, FabFilter, iZotope)...",
			"Поиск по названию или автору (Insight, FabFilter, iZotope)...",
			"Buscar plugin o fabricante (ej. Insight, FabFilter, iZotope)...",
			"プラグイン名・ベンダーを検索 (例: Insight, Melodyne, FabFilter, iZotope)...",
			"플러그인 이름, 제조사 검색 (예: Insight, Melodyne, FabFilter, iZotope)...");
		Add("全部插件 (0)", "全部外掛 (0)", "All Plugins (0)", "Tous (0)", "Все плагины (0)", "Todos (0)", "すべて (0)", "전체 플러그인 (0)");
		Add("全部插件 ({0})", "全部外掛 ({0})", "All Plugins ({0})", "Tous ({0})", "Все плагины ({0})", "Todos ({0})", "すべて ({0})", "전체 플러그인 ({0})");
		Add("已启用 (0)", "已啟用 (0)", "Enabled (0)", "Activés (0)", "Включено (0)", "Activados (0)", "有効 (0)", "활성화됨 (0)");
		Add("已启用 ({0})", "已啟用 ({0})", "Enabled ({0})", "Activés ({0})", "Включено ({0})", "Activados ({0})", "有効 ({0})", "활성화됨 ({0})");
		Add("共发现 0 个 VST3 插件", "共發現 0 個 VST3 外掛", "0 VST3 plugins found", "0 plugin VST3 trouvé", "Найдено 0 плагинов VST3", "0 plugins VST3 encontrados", "0 個の VST3 プラグインが見つかりました", "총 0개의 VST3 플러그인 발견");
		Add("共发现 {0} 个 VST3 插件", "共發現 {0} 個 VST3 外掛", "{0} VST3 plugins found", "{0} plugins VST3 trouvés", "Найдено плагинов VST3: {0}", "{0} plugins VST3 encontrados", "{0} 個の VST3 プラグインが見つかりました", "총 {0}개의 VST3 플러그인 발견");
		Add("{0} (已旁通)", "{0} (已旁通)", "{0} (Bypassed)", "{0} (Contourné)", "{0} (Байпас)", "{0} (Bypass)", "{0} (バイパス)", "{0} (바이패스됨)");
		Add("Active Slot (直通待命)", "Active Slot (直通待命)", "Active Slot (Bypass Standby)", "Active Slot (En veille)", "Active Slot (Байпас)", "Active Slot (En espera)", "Active Slot (バイパス待機)", "Active Slot (바이패스 대기)");
		Add("原生窗口已打开 (Active)", "原生視窗已開啟 (Active)", "Native Window Open (Active)", "Fenêtre native ouverte (Active)", "Нативное окно открыто (Active)", "Ventana nativa abierta (Active)", "ネイティブウィンドウ表示中 (Active)", "네이티브 창 열림 (Active)");
		Add("点击 [🎛️ 界面] 随时唤起", "點擊 [🎛️ 介面] 隨時喚起", "Click [🎛️ UI] anytime to open", "Cliquez sur [🎛️ Interface] pour ouvrir", "Нажмите [🎛️ Окно] чтобы открыть", "Pulsa [🎛️ Interfaz] para abrir", "[🎛️ 画面] でいつでも開けます", "[🎛️ 창]을 눌러 언제든 열 수 있습니다");
		Add("实时运行中", "即時運行中", "Running Live", "Actif en direct", "Работает", "En ejecución", "リアルタイム動作中", "실시간 실행 중");
		Add("已旁通 (Bypass)", "已旁通 (Bypass)", "Bypassed", "Contourné (Bypass)", "Байпас (Bypass)", "En bypass (Bypass)", "バイパス中 (Bypass)", "바이패스됨 (Bypass)");
		Add("原生宿主已挂载 (Native Active)", "原生宿主已掛載 (Native Active)", "Native Host Mounted (Active)", "Hôte natif monté (Active)", "Нативный хост активен (Native Active)", "Host nativo montado (Active)", "ネイティブホスト接続済み (Native Active)", "네이티브 호스트 장착됨 (Native Active)");
		Add("暂未配置扫描目录。可点击“恢复默认目录”或“📁 添加扫描目录”。",
			"暫未配置掃描目錄。可點擊「恢復預設目錄」或「📁 新增掃描目錄」。",
			"No scan folders configured. Click \"Restore Defaults\" or \"📁 Add Scan Folder\".",
			"Aucun dossier configuré. Cliquez sur « Dossiers par défaut » ou « 📁 Ajouter un dossier ».",
			"Папки не настроены. Нажмите «Папки по умолчанию» или «📁 Добавить папку».",
			"Sin carpetas configuradas. Pulsa «Restaurar carpetas» o «📁 Añadir carpeta».",
			"スキャンフォルダが未設定です。「デフォルトに戻す」または「📁 フォルダを追加」を押してください。",
			"스캔 폴더가 설정되지 않았습니다. '기본 폴더 복원' 또는 '📁 스캔 폴더 추가'를 클릭하세요.");
		Add("打开文件夹", "開啟資料夾", "Open Folder", "Ouvrir le dossier", "Открыть папку", "Abrir carpeta", "フォルダを開く", "폴더 열기");
		Add("该目录当前不存在：{0}", "該目錄目前不存在：{0}", "Directory does not exist: {0}", "Le dossier n'existe pas : {0}", "Папка не существует: {0}", "La carpeta no existe: {0}", "フォルダが存在しません：{0}", "해당 폴더가 존재하지 않습니다: {0}");
		Add("移除", "移除", "Remove", "Retirer", "Удалить", "Quitar", "削除", "제거");
		Add("尚未发现任何 VST3 插件", "尚未發現任何 VST3 外掛", "No VST3 plugins found yet", "Aucun plugin VST3 trouvé", "Плагины VST3 пока не найдены", "No se encontraron plugins VST3", "VST3 プラグインがまだ見つかっていません", "아직 발견된 VST3 플러그인이 없습니다");
		Add("没有符合当前筛选或搜索条件的插件", "沒有符合目前篩選或搜尋條件的外掛", "No plugins match current filter or search", "Aucun plugin ne correspond à votre recherche", "Нет плагинов по текущему фильтру", "Ningún plugin coincide con la búsqueda", "条件に一致するプラグインがありません", "현재 필터 또는 검색 조건에 맞는 플러그인이 없습니다");
		Add("点击左侧“⚡ 立即扫描全部目录”自动发现系统中的 Steinberg VST3 插件，或点击顶部“➕ 加载 .vst3 文件”直接载入。",
			"點擊左側「⚡ 立即掃描全部目錄」自動探索系統中的 Steinberg VST3 外掛，或點擊頂部「➕ 載入 .vst3 檔案」直接載入。",
			"Click \"⚡ Scan All Folders Now\" on the left to discover Steinberg VST3 plugins, or click \"➕ Load .vst3 File\" above.",
			"Cliquez sur « ⚡ Scanner tout maintenant » à gauche ou sur « ➕ Charger un .vst3 » en haut.",
			"Нажмите «⚡ Сканировать все папки» слева или «➕ Загрузить .vst3» вверху.",
			"Pulsa «⚡ Escanear todo ahora» a la izquierda o «➕ Cargar archivo .vst3» arriba.",
			"左の「⚡ すべてのフォルダをスキャン」で自動検出するか、上の「➕ .vst3 を読み込む」で直接読み込みます。",
			"왼쪽의 '⚡ 모든 폴더 즉시 스캔'을 눌러 VST3 플러그인을 찾거나 상단의 '➕ .vst3 파일 불러오기'를 클릭하세요.");
		Add("请尝试清空搜索关键词或切换回“全部插件”视图。",
			"請嘗試清空搜尋關鍵字或切換回「全部外掛」檢視。",
			"Try clearing the search box or switching back to \"All Plugins\".",
			"Essayez d'effacer la recherche ou de revenir à « Tous ».",
			"Очистите строку поиска или переключитесь на «Все плагины».",
			"Prueba a limpiar la búsqueda o volver a «Todos».",
			"検索キーワードを消去するか「すべて」表示に切り替えてください。",
			"검색어를 지우거나 '전체 플러그인' 보기로 전환해 보세요.");
		Add("处理中 (Active)", "處理中 (Active)", "Processing (Active)", "Traitement (Active)", "Обработка (Active)", "Procesando (Active)", "処理中 (Active)", "처리 중 (Active)");
		Add("已启用但旁通 (Bypassed)", "已啟用但旁通 (Bypassed)", "Enabled but Bypassed", "Activé mais contourné", "Включён, но в байпасе", "Activado pero en bypass", "有効 (バイパス中)", "활성화되었으나 바이패스됨");
		Add("运行中 · 界面已开", "運行中 · 介面已開", "Running · UI Open", "Actif · Interface ouverte", "Работает · Окно открыто", "Activo · Interfaz abierta", "動作中 · 画面表示", "실행 중 · 창 열림");
		Add("运行中", "運行中", "Running", "Actif", "Работает", "Activo", "動作中", "실행 중");
		Add("已旁通", "已旁通", "Bypassed", "Contourné", "Байпас", "Bypass", "バイパス", "바이패스");
		Add("● 已启用", "● 已啟用", "● Enabled", "● Activé", "● Включён", "● Activado", "● 有効", "● 활성화됨");
		Add("○ 启用", "○ 啟用", "○ Enable", "○ Activer", "○ Включить", "○ Activar", "○ 有効化", "○ 활성화");
		Add("旁通", "旁通", "Bypass", "Bypass", "Байпас", "Bypass", "バイパス", "바이패스");
		Add("已旁通插件：{0}", "已旁通外掛：{0}", "Bypassed plugin: {0}", "Plugin contourné : {0}", "Байпас плагина: {0}", "Bypass del plugin: {0}", "プラグインをバイパス：{0}", "플러그인 바이패스됨: {0}");
		Add("已恢复插件处理：{0}", "已恢復外掛處理：{0}", "Resumed plugin: {0}", "Plugin repris : {0}", "Возобновлён плагин: {0}", "Plugin reanudado: {0}", "プラグイン処理を再開：{0}", "플러그인 처리 재개됨: {0}");
		Add("🎛️ 界面", "🎛️ 介面", "🎛️ UI", "🎛️ Interface", "🎛️ Окно", "🎛️ Interfaz", "🎛️ 画面", "🎛️ 창");
		Add("已启用 VST3 插件：{0} ({1})", "已啟用 VST3 外掛：{0} ({1})", "Enabled VST3 plugin: {0} ({1})", "Plugin VST3 activé : {0} ({1})", "Включён VST3 плагин: {0} ({1})", "Plugin VST3 activado: {0} ({1})", "VST3 プラグインを有効化：{0} ({1})", "VST3 플러그인 활성화됨: {0} ({1})");
		Add("已停用 VST3 插件：{0}", "已停用 VST3 外掛：{0}", "Disabled VST3 plugin: {0}", "Plugin VST3 désactivé : {0}", "Отключён VST3 плагин: {0}", "Plugin VST3 desactivado: {0}", "VST3 プラグインを無効化：{0}", "VST3 플러그인 비활성화됨: {0}");
		Add("打开插件界面失败：{0}", "開啟外掛介面失敗：{0}", "Failed to open plugin UI: {0}", "Échec d'ouverture de l'interface : {0}", "Не удалось открыть окно плагина: {0}", "Error al abrir interfaz: {0}", "プラグイン画面を開けませんでした：{0}", "플러그인 창 열기 실패: {0}");
		Add("已打开插件原生界面：{0}，实时处理当前音频流", "已開啟外掛原生介面：{0}，即時處理目前音訊流", "Opened native UI for {0}, processing audio live", "Interface native ouverte pour {0}, traitement en direct", "Открыто окно {0}, обработка в реальном времени", "Interfaz nativa abierta para {0}, procesando en vivo", "{0} のネイティブ画面を開きました（リアルタイム処理中）", "{0} 네이티브 창을 열었습니다(실시간 오디오 처리 중)");
		Add("打开插件界面异常：{0}", "開啟外掛介面異常：{0}", "Error opening plugin UI: {0}", "Erreur d'ouverture de l'interface : {0}", "Ошибка открытия окна плагина: {0}", "Excepción al abrir interfaz: {0}", "プラグイン画面を開く際にエラーが発生しました：{0}", "플러그인 창 열기 오류: {0}");
		Add("当前列表中暂无可用插件，请先扫描目录或加载 .vst3 文件",
			"目前清單中暫無可用外掛，請先掃描目錄或載入 .vst3 檔案",
			"No plugins in list; scan folders or load a .vst3 file first",
			"Aucun plugin dans la liste ; scannez ou chargez un .vst3",
			"Нет плагинов в списке; сначала просканируйте папки или загрузите .vst3",
			"Sin plugins en la lista; escanea carpetas o carga un .vst3",
			"利用可能なプラグインがありません。フォルダをスキャンするか .vst3 を読み込んでください",
			"목록에 사용 가능한 플러그인이 없습니다. 먼저 폴더를 스캔하거나 .vst3 파일을 불러오세요");
		Add("当前没有已启用的插件可供旁通", "目前沒有已啟用的外掛可供旁通", "No active plugin to bypass", "Aucun plugin actif à contourner", "Нет активного плагина для байпаса", "No hay plugin activo para bypass", "バイパスできる有効なプラグインがありません", "바이패스할 활성 플러그인이 없습니다");
		Add("已旁通当前插件：{0}", "已旁通目前外掛：{0}", "Bypassed active plugin: {0}", "Plugin actif contourné : {0}", "Байпас текущего плагина: {0}", "Bypass del plugin actual: {0}", "現在のプラグインをバイパス：{0}", "현재 플러그인 바이패스됨: {0}");
		Add("已恢复当前插件处理：{0}", "已恢復目前外掛處理：{0}", "Resumed active plugin: {0}", "Plugin actif repris : {0}", "Возобновлён текущий плагин: {0}", "Plugin actual reanudado: {0}", "現在のプラグイン処理を再開：{0}", "현재 플러그인 처리 재개됨: {0}");
		Add("已全部停用并卸载 VST3 原生宿主，音频恢复直通",
			"已全部停用並卸載 VST3 原生宿主，音訊恢復直通",
			"Disabled all plugins and unloaded VST3 native host; audio bypassed",
			"Tous les plugins désactivés et hôte VST3 déchargé ; audio direct",
			"Все плагины отключены, нативный хост VST3 выгружен; прямой звук",
			"Todos los plugins desactivados y host VST3 descargado; audio directo",
			"すべてのプラグインを無効化し VST3 ホストを解放しました（バイパス復帰）",
			"모든 플러그인을 비활성화하고 VST3 네이티브 호스트를 언로드했습니다(바이패스 복귀)");
		Add("添加目录失败：{0}", "新增目錄失敗：{0}", "Failed to add folder: {0}", "Échec d'ajout du dossier : {0}", "Не удалось добавить папку: {0}", "Error al añadir carpeta: {0}", "フォルダの追加に失敗：{0}", "폴더 추가 실패: {0}");
		Add("加载 .vst3 文件失败：{0}", "載入 .vst3 檔案失敗：{0}", "Failed to load .vst3 file: {0}", "Échec du chargement du .vst3 : {0}", "Не удалось загрузить .vst3: {0}", "Error al cargar .vst3: {0}", ".vst3 ファイルの読み込みに失敗：{0}", ".vst3 파일 불러오기 실패: {0}");
		Add("已启用 · {0}", "已啟用 · {0}", "Enabled · {0}", "Activé · {0}", "Включён · {0}", "Activado · {0}", "有効 · {0}", "활성화됨 · {0}");
		Add("已就绪 · {0} 个插件", "已就緒 · {0} 個外掛", "Ready · {0} plugins", "Prêt · {0} plugins", "Готово · {0} плаг.", "Listo · {0} plugins", "準備完了 · {0} プラグイン", "준비됨 · {0}개 플러그인");
		Add("原生插件宿主", "原生外掛宿主", "Native Plugin Host", "Hôte de plugins natif", "Нативный хост плагинов", "Host nativo de plugins", "ネイティブプラグインホスト", "네이티브 플러그인 호스트");
	}
}
