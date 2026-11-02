# FurinaPlayer 项目开发规定 (Development Regulations)

## 一、 子代理并发协作规定
1. **多代理并发上限**：
   在推进 FurinaPlayer 的设计、代码重构、DSP 移植、UI 优化与测试时，主代理可以同时开放**至多 10 个子代理模型会话（Subagents）**并行开展独立工作，以最大化提升整体开发效能。
2. **职责划分原则（至多 10 代理并行矩阵）**：
   - 子代理 1：音频引擎与流式解码控制（`SonicWave.Audio`、`SafeAudioFileReader`）；
   - 子代理 2：ECHO DSP 核心处理器移植（`EchoEqProcessor`、`EchoReplayGainProcessor` 等）；
   - 子代理 3：WinUI 3 页面重构与 XAML 组件布局（`DspPage`、`EditorPage`）；
   - 子代理 4：SkiaSharp 自定义图形绘制与实时可视化（`ParametricEqCurveControl`、`SpectrumAnalyzer`、`PhaseAnalyzerControl`）；
   - 子代理 5：MVVM 数据绑定与实时参数事件同步（`DspViewModel`、`EditorViewModel`）；
   - 子代理 6：Steam 版 ECHO 源码分析与交互体验逆向适配；
   - 子代理 7：曲库响度分析与 BS.1770 / EBU R128 背景任务集成；
   - 子代理 8：单元测试、数字信号数学校验与防爆音限幅测试（`EchoDspTests`）；
   - 子代理 9：UI/UX 设计走查、对齐图纸排版与高 DPI 适配；
   - 子代理 10：本地构建、哈希完整性校验与多环境部署验证。
3. **主代理统筹**：
   由主代理统一进行顶层架构设计、任务解构、代码合并、SHA-256 哈希比对与最终构建，确保代码版本一致性与无任何冲突。

## 二、 音频测试音量安全红线
1. **测试音量强制限制**：
   当对 FurinaPlayer 进行任何除“响度（Loudness/LUFS 测量）”以外的音频测试、预览播放或自动化回归时，**必须提前先将软件播放音量降至 1%（或 -40 dB）**。
2. **严禁修改系统音量**：
   在任何情况下，**严禁通过脚本或代码修改 Windows 系统主音量或声卡硬件音量**。必须仅在软件应用内部或解码器内部做安全音量衰减。

## 三、 代码发布与 GitHub 同步铁律
1. **未授权禁推**：
   严禁擅自向 GitHub 执行 `git push`，严禁擅自打包发布 release。
2. **指令触发**：
   仅当用户明确下达“构建代码请上传GitHub并打包发布release”指令时，方可执行 git 提交、推送与 release 打包。
3. **哈希校验一致性**：
   每次代码改动与本地构建后，必须运行全量同步脚本，比对工作区与 GitHub 仓库源码及二进制（.exe, .dll, .pri）的 SHA-256 哈希，确保 100% 吻合。
