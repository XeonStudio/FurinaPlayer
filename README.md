<div align="center">

<img src="docs/media/app_logo.png" width="128" height="128" alt="FurinaPlayer Logo" />

# FurinaPlayer

**基于 WinUI 3 与 .NET 8 的全格式无损音乐播放器 · 专业级实时 DSP · 音频编辑与工具箱 · 芙宁娜桌面看板娘**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20x64-0078D4?style=flat-square&logo=windows)](https://github.com/XeonStudio/FurinaPlayer)
[![Framework](https://img.shields.io/badge/.NET-8.0%20%7C%20WinUI%203-512BD4?style=flat-square&logo=dotnet)](https://github.com/XeonStudio/FurinaPlayer)
[![Version](https://img.shields.io/badge/Release-v1.30.12.0012.1125-38BDF8?style=flat-square)](https://github.com/XeonStudio/FurinaPlayer/releases)
[![License](https://img.shields.io/badge/License-Apache--2.0-22C55E?style=flat-square)](LICENSE)

</div>

---

## ✨ 近期更新亮点（v1.30.12.0012.1125 · 2026-11-25）

- **🎧 OPRA 600 耳机知识库 & Mix Assistant V2**
  - 按佩戴方式、发音单元、调音风格各收录 200 款热门与旗舰耳机参数；重构 Mix Assistant 智能混音平衡，增强曲风自动识别、耳机风格匹配与防削波保护。
- **🏠 ECHO 风格主页重构 & 独立曲库视图**
  - 首页拆分为独立「首页 / 专辑 / 歌曲 / 音乐收藏」一级菜单：首页支持实时同步正在播放封面/曲目/格式与双语歌词、听歌热力图与艺人排行；歌曲页支持“喜欢”收藏，“音乐收藏”支持鼠标拖拽自定义排序。
- **🎛️ 音频编辑器 & 实时 DSP 全面升级**
  - **WSOLA 无失真变速变调**：替换原有倍速实现，显著降低音乐倍速播放与编辑器变调失真；底部播控栏“更多”菜单新增倍速、变调滑块、睡眠定时器及独立音量滑块。
  - **空间声场工作台 & 32 种实时音效**：重构「效果器与 DSP 设置 → 虚拟声场」及编辑器「空间与变速」，实时预览即时生效；新增 32 种实时音效。
  - **Insight Pro Spectrum 重构 & 波形合并**：升级至 `8192` 点 Blackman-Harris FFT，支持 Pink `+3 dB/oct` / `Flat 0 dB/oct` 斜率切换与音高音名指示；合并淡入淡出与标准化至「波形编辑与响度」。
- **🧰 工具箱工作台 & 听力检测与黄金听感**
  - 原“格式转换”升级为「工具箱」，采用与 DSP 设置一致的亮暗自适应分组侧栏，集成「格式转换」「批量标签工作台」与纯音测听「听力检测与黄金听感」（自动生成听力图并联动 EQ 补偿曲线）。
- **🛠️ 检查与修复助手、设置与性能深度优化**
  - 新增「检查与修复」助手（播放状态检查、兼容性审计、排障向导、重置音频设置）；修复设置页打开卡顿、内存占用过高、LibVLC 延迟初始化、PowerPoint 歌词全屏铺满、桌宠全系统/自定义字体与动态壁纸（测试版）等功能。

---

## 🖼️ 看板娘与歌词气泡预览

<div align="center">

| 音乐暂停 / 待机 (`Idle`) | 音乐播放律动 (`Groove`) | 桌宠点击互动 (`Click`) |
| :---: | :---: | :---: |
| <img src="docs/media/furina_pet_idle.gif" width="160" alt="Idle" /> | <img src="docs/media/furina_pet_groove.gif" width="160" alt="Groove" /> | <img src="docs/media/furina_pet_click.gif" width="160" alt="Click" /> |

<br/>

**三行自适应歌词对话气泡效果**

<img src="docs/media/pet_bubble_preview.png" width="440" alt="Lyric Bubble Preview" />

</div>

---

## 🚀 核心功能特性

| 模块 | 核心能力 |
| :--- | :--- |
| **高保真播放引擎** | 基于 LibVLC + NAudio（WASAPI 共享/独占 / ASIO），畅播 FLAC、APE、WAV、DSD、ALAC、AAC、MP3、OGG 等全格式，支持记忆进度、无缝播放与播放顺序记忆 |
| **专业级 DSP 机架** | 10 段参量均衡器 + Mix Assistant V2、OPRA 耳机校准（700+ 厂商 / 6,000+ 型号）、空间声场工作台、32 种实时音效、压限器、多阶重采样与 PCM→DSD 调制 |
| **音频编辑与分析** | 实时波形编辑与响度（裁剪/淡入淡出/标准化）、8192 点 Insight Pro 频谱与相位分析、WSOLA 实时变速变调试听 |
| **工具箱与听力检测** | NCM / MGG / KGM 等加密音频解密与批量无损转码、批量标签与文件整理工作台、医院级多频点纯音听力检测与 EQ 补偿联动 |
| **曲库、流媒体与看板娘** | SQLite / EF Core 本地曲库、拖拽排序音乐收藏、多线程完整性补齐、动态壁纸、PowerPoint 全屏歌词主题、全系统/自定义字体芙宁娜桌面宠物 |

---

## 📦 下载与运行

1. 前往 [Releases](https://github.com/XeonStudio/FurinaPlayer/releases) 下载最新的 `FurinaPlayer-v1.30.12.0012.1125-win-x64.zip`。
2. 解压后直接双击运行 `FurinaPlayer.exe`（已包含自包含运行时，无需额外安装）。
3. **系统要求**：Windows 10 1809（Build 17763）或 Windows 11，64 位（x64）系统。

### 从源码构建

```powershell
# 编译 Release x64 主程序
dotnet build src\FurinaPlayer\FurinaPlayer.csproj -c Release -p:Platform=x64

# 运行全部单元测试（245 项）
dotnet run --project src\FurinaPlayerDspTests\FurinaPlayerDspTests.csproj -c Release -p:Platform=x64
```

---

## 🗂️ 工程结构

- `src/FurinaPlayer`：WinUI 3 主程序入口、窗口管理、系统托盘与芙宁娜桌面宠物（`FurinaPetWindow`）
- `src/FurinaPlayer.UI`：页面（首页/专辑/歌曲/音乐收藏、正在播放、音频编辑、DSP 机架、工具箱、设置）与 ViewModel
- `src/FurinaPlayer.Core`：曲库管理、元数据/封面/歌词服务、多语言本地化与设置持久化
- `src/FurinaPlayer.Audio`：NAudio / LibVLC 音频引擎与全套纯自研 DSP 处理器（含 WSOLA、Mix Assistant V2、空间声场、8192 点频谱）
- `src/FurinaPlayerDspTests`：DSP 与核心功能自动化测试套件（245 / 245 通过）

---

## 🧩 开源组件与接口致谢

| 组件 / 接口 | 用途 | 仓库 / 主页 |
| :--- | :--- | :--- |
| Windows App SDK / WinUI 3 | 桌面 UI 框架与系统集成 | [microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) |
| .NET 8 / EF Core (SQLite) | 运行时与曲库持久化 | [dotnet/dotnet](https://github.com/dotnet/dotnet) |
| CommunityToolkit.Mvvm & WinUI | MVVM 与界面扩展控件 | [CommunityToolkit/dotnet](https://github.com/CommunityToolkit/dotnet) |
| LibVLC & LibVLCSharp | 全格式解码与播放 | [videolan/libvlcsharp](https://github.com/videolan/libvlcsharp) |
| NAudio | WASAPI / ASIO 低延迟输出 | [naudio/NAudio](https://github.com/naudio/NAudio) |
| ATL (Audio Tools Library) | 音频标签、封面与内嵌歌词读写 | [mabdelouahab/atldotnet](https://github.com/mabdelouahab/atldotnet) |
| SkiaSharp | 桌面宠物透明渲染、歌词气泡与音频仪表绘制 | [mono/SkiaSharp](https://github.com/mono/SkiaSharp) |
| MathNet.Numerics & LiveChartsCore | 信号分析与频谱波形图表 | [mathnet/mathnet-numerics](https://github.com/mathnet/mathnet-numerics) |
| FFmpeg | 批量音频格式转换与动态壁纸解码 | [ffmpeg.org](https://ffmpeg.org/) |
| LRCLIB / 网易云 / QQ 音乐 / Apple Music | 在线歌词、元数据与高清封面检索 | [lrclib.net](https://lrclib.net/) |

---

## 📄 许可证

本项目基于 [Apache License 2.0](LICENSE) 开源。
