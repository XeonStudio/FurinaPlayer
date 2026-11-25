namespace FurinaPlayer.Core.Services;

public static partial class Loc
{
	static partial void RegisterCheckRepair()
	{
		Add(
			"检查与修复",
			"檢查與修復",
			"Check & Repair",
			"Vérification et réparation",
			"Проверка и исправление",
			"Comprobar y reparar",
			"チェックと修復",
			"검사 및 복구");
		Add(
			"实时检查音频播放链路、排查硬件兼容隐患、逐步引导无声修复并安全重置音频引擎，全程展示当前检查进度与原因。",
			"即時檢查音訊播放鏈路、排查硬體相容隱患、逐步引導無聲修復並安全重設音訊引擎，全程顯示目前檢查進度與原因。",
			"Inspect live playback telemetry, scan hardware compatibility, walk through step-by-step no-sound repair, and safely reset the audio engine with real-time progress and reasons.",
			"Inspectez l'état audio en direct, analysez la compatibilité matérielle, suivez l'assistant de dépannage étape par étape et réinitialisez le moteur audio avec progression et raisons en temps réel.",
			"Проверяйте тракт воспроизведения в реальном времени, совместимость устройств, пошагово устраняйте отсутствие звука и сбрасывайте аудиодвижок с отображением прогресса и причин.",
			"Supervise el estado en tiempo real, analice compatibilidad de hardware, repare paso a paso sin sonido y restablezca el motor de audio con progreso y causas en vivo.",
			"再生状態をリアルタイム監視し、ハードウェア互換性診断・無音トラブル解決ウィザード・安全なオーディオエンジンリセットを進捗と理由付きで実行します。",
			"실시간 오디오 재생 상태를 점검하고 하드웨어 호환성 검사, 무음 문제 해결 마법사, 안전한 엔진 재설정을 진행률과 원인 안내와 함께 수행합니다.");
		Add(
			"1. 音频播放状态检查助手",
			"1. 音訊播放狀態檢查助手",
			"1. Playback Status Inspector",
			"1. Inspecteur d'état de lecture",
			"1. Инспектор состояния воспроизведения",
			"1. Inspector de estado de reproducción",
			"1. 再生状態チェックアシスタント",
			"1. 재생 상태 검사 도우미");
		Add(
			"2. 音频播放兼容性助手",
			"2. 音訊播放相容性助手",
			"2. Playback Compatibility Assistant",
			"2. Assistant de compatibilité audio",
			"2. Помощник совместимости аудио",
			"2. Asistente de compatibilidad de audio",
			"2. 再生互換性アシスタント",
			"2. 재생 호환성 도우미");
		Add(
			"3. 音频播放排障助手（无声与点播失败向导）",
			"3. 音訊播放排障助手（無聲與點播失敗精靈）",
			"3. Interactive Playback Troubleshooter",
			"3. Assistant de dépannage audio",
			"3. Мастер устранения неполадок звука",
			"3. Asistente de solución de audio",
			"3. 再生トラブルシューティングアシスタント",
			"3. 오디오 문제 해결 도우미");
		Add(
			"4. 重置音频设置助手",
			"4. 重設音訊設定助手",
			"4. Audio Reset Assistant",
			"4. Assistant de réinitialisation audio",
			"4. Помощник сброса настроек аудио",
			"4. Asistente de restablecimiento de audio",
			"4. オーディオ設定リセットアシスタント",
			"4. 오디오 설정 초기화 도우미");
		Add("刷新检查", "重新檢查", "Refresh", "Actualiser", "Обновить", "Actualizar", "再スキャン", "새로 고침");
		Add("复制诊断信息", "複製診斷資訊", "Copy Diagnostics", "Copier le diagnostic", "Копировать отчёт", "Copiar diagnóstico", "診断情報をコピー", "진단 정보 복사");
		Add("已复制诊断信息", "已複製診斷資訊", "Copied", "Copié", "Скопировано", "Copiado", "コピーしました", "복사됨");
		Add("尝试播放", "嘗試播放", "Try Play", "Lire", "Воспроизвести", "Reproducir", "再生を試す", "재생 시도");
		Add("开始全面兼容扫描", "開始全面相容掃描", "Run Full Compatibility Scan", "Lancer l'analyse de compatibilité", "Запустить полную проверку", "Iniciar análisis completo", "互換性スキャンを開始", "전체 호환성 검사 시작");
		Add("热重置音频引擎", "熱重設音訊引擎", "Soft Reset Audio Engine", "Réinitialiser le moteur audio", "Мягкий сброс аудиодвижка", "Restablecer motor de audio", "オーディオエンジンを再起動", "오디오 엔진 소프트 재설정");
		Add("重置音频引擎", "重設音訊引擎", "Reset Audio Engine", "Réinitialiser le moteur", "Сбросить движок", "Reiniciar motor", "エンジンをリセット", "엔진 재설정");
		Add("重绑默认设备", "重綁預設裝置", "Rebind Default Device", "Réassocier le périphérique par défaut", "Перепривязать устройство", "Reasignar dispositivo", "既定デバイスを再紐付け", "기본 장치 다시 연결");
		Add("重置所有音频设置", "重設所有音訊設定", "Reset All Audio Settings", "Tout réinitialiser", "Сбросить все настройки звука", "Restablecer ajustes de audio", "すべてのオーディオ設定をリセット", "모든 오디오 설정 초기화");
		Add("实时监控开", "即時監控開", "Live Monitor On", "Suivi actif", "Монитор вкл", "Monitor en vivo", "リアルタイム監視オン", "실시간 모니터 켜짐");
		Add("实时监控关", "即時監控關", "Live Monitor Off", "Suivi inactif", "Монитор выкл", "Monitor apagado", "リアルタイム監視オフ", "실시간 모니터 꺼짐");
		Add("待检测", "待檢測", "Pending", "En attente", "Ожидание", "Pendiente", "待機中", "대기 중");
	}
}
