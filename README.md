<div align="center">

<img src="docs/media/app_logo.png" width="128" height="128" alt="FurinaPlayer Logo" />

# FurinaPlayer

**基于 WinUI 3 与 .NET 8 的全格式无损音乐播放器 · 专业级实时 DSP · 芙宁娜桌面看板娘**

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11%20x64-0078D4?style=flat-square&logo=windows)](https://github.com/XeonStudio/FurinaPlayer)
[![Framework](https://img.shields.io/badge/.NET-8.0%20%7C%20WinUI%203-512BD4?style=flat-square&logo=dotnet)](https://github.com/XeonStudio/FurinaPlayer)
[![Version](https://img.shields.io/badge/Release-v1.29.06.0008.1121-38BDF8?style=flat-square)](https://github.com/XeonStudio/FurinaPlayer/releases)
[![License](https://img.shields.io/badge/License-Apache--2.0-22C55E?style=flat-square)](LICENSE)

</div>

---

## ✨ 近期更新亮点（v1.29.06.0008.1121 · 2026-11-21）

- **🎭 芙宁娜桌面宠物（看板娘）**
  - 基于 Win32 `WS_EX_LAYERED` + `SkiaSharp` 实现逐像素透明置顶悬浮窗，支持 **待机 / 播放律动 / 点击互动** 三态 `125ms` 像素动图。
  - 内置悬停半透明迷你播控栏（新增 **2.5s** 防误触隐藏冷却）、右键快捷菜单、系统托盘联动、`60%–180%` 宠物缩放、`50%–200%` GIF 播放速度调节与三态自定义 `.gif` 替换。
- **💬 看板娘三行自适应歌词气泡**
  - 看板娘上方可同时展示 **当前 / 上一行 / 上上行** 三行歌词对话气泡，历史第 4 个气泡平滑上滑并渐隐消失；气泡随歌词长短自动伸缩、支持双语换行与 `60%–180%` 气泡比例调节，并采用 `HTTRANSPARENT` 实现鼠标完全穿透。
- **🔍 曲库完整性多线程扫描与自动补齐**
  - 在「设置 → 音乐库」中一键多线程并行检查本地曲目元数据、内嵌歌词与专辑封面完整性，按 **网易云音乐 → QQ 音乐 → Apple Music** 优先级自动补齐并显示实时处理进度。
- **🔄 音频格式转换器 & 歌词体验升级**
  - 新增批量无损音频格式转换器；支持网易云逐字 YRC + 译文双语融合、LRCLIB 在线兜底及主页专辑网格/模糊搜索去重。
- **🎨 主题视觉统一**
  - 全新芙宁娜主题图标已统一应用至主程序、桌面快捷方式、标题栏与首页欢迎卡片。

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
| **高保真播放引擎** | 基于 LibVLC + NAudio（WASAPI / ASIO / DirectSound），畅播 FLAC、APE、WAV、DSD、ALAC、AAC、MP3 等全格式 |
| **专业级 DSP 机架** | 独立实现的 10 段参量均衡器、OPRA 耳机校准库（700+ 厂商 / 6,000+ 型号）、空间音频、压限器、采样率重采样、PCM→DSD 调制与电平标准化 |
| **音频编辑与分析** | 实时波形 / FFT 频谱 / 相位分析器，支持选区裁剪、淡入淡出、标准化与批量无损格式转换 |
| **曲库与流媒体** | SQLite / EF Core 本地曲库、多关键字/拼音首字母模糊搜索、专辑封面网格、在线流媒体直链与曲库完整性多线程补齐 |
| **桌面宠物与外观** | 液态玻璃（Liquid Glass）与动态壁纸主题、8 国语言实时切换、芙宁娜三态透明看板娘与自适应歌词对话气泡 |

---

## 📦 下载与运行

1. 前往 [Releases](https://github.com/XeonStudio/FurinaPlayer/releases) 下载最新的 `FurinaPlayer-v*-win-x64.zip`。
2. 解压后直接双击运行 `FurinaPlayer.exe`（已包含自包含运行时，无需额外安装）。
3. **系统要求**：Windows 10 1809（Build 17763）或 Windows 11，64 位（x64）系统。

### 从源码构建

```powershell
# 编译 Release x64 主程序
dotnet build src\FurinaPlayer\FurinaPlayer.csproj -c Release -p:Platform=x64

# 运行全部单元测试（159 项）
dotnet run --project src\FurinaPlayerDspTests\FurinaPlayerDspTests.csproj -c Release
```

---

## 🗂️ 工程结构

- `src/FurinaPlayer`：WinUI 3 主程序入口、窗口管理、系统托盘与芙宁娜桌面宠物（`FurinaPetWindow`）
- `src/FurinaPlayer.UI`：页面（主页、正在播放、音频编辑、DSP 机架、格式转换、设置）与 ViewModel
- `src/FurinaPlayer.Core`：曲库管理、元数据/封面/歌词服务、多语言本地化与设置持久化
- `src/FurinaPlayer.Audio`：NAudio / LibVLC 音频引擎与全套纯自研 DSP 处理器
- `src/FurinaPlayerDspTests`：DSP 与核心功能自动化测试套件（159 / 159 通过）

---

## 🧩 开源组件与接口致谢

| 组件 / 接口 | 用途 | 仓库 / 主页 |
| :--- | :--- | :--- |
| Windows App SDK / WinUI 3 | 桌面 UI 框架与系统集成 | [microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) |
| .NET 8 / EF Core (SQLite) | 运行时与曲库持久化 | [dotnet/dotnet](https://github.com/dotnet/dotnet) |
| CommunityToolkit.Mvvm & WinUI | MVVM 与界面扩展控件 | [CommunityToolkit/dotnet](https://github.com/CommunityToolkit/dotnet) |
| LibVLC & LibVLCSharp | 全格式解码与原生均衡器 | [videolan/libvlcsharp](https://github.com/videolan/libvlcsharp) |
| NAudio | WASAPI / ASIO 低延迟输出 | [naudio/NAudio](https://github.com/naudio/NAudio) |
| ATL (Audio Tools Library) | 音频标签、封面与内嵌歌词读写 | [mabdelouahab/atldotnet](https://github.com/mabdelouahab/atldotnet) |
| SkiaSharp | 桌面宠物透明渲染与歌词气泡绘制 | [mono/SkiaSharp](https://github.com/mono/SkiaSharp) |
| MathNet.Numerics & LiveChartsCore | 信号分析与频谱波形图表 | [mathnet/mathnet-numerics](https://github.com/mathnet/mathnet-numerics) |
| FFmpeg | 批量音频格式转换 | [ffmpeg.org](https://ffmpeg.org/) |
| LRCLIB / 网易云 / QQ 音乐 / Apple Music | 在线歌词、元数据与高清封面检索 | [lrclib.net](https://lrclib.net/) |

---

## 📄 许可证

本项目基于 [Apache License 2.0](LICENSE) 开源。
