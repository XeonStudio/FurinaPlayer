# FurinaPlayer

<p align="center"><b>全格式无损音乐播放器 · Lossless music player with editing & VST3 support</b></p>

FurinaPlayer 是一款基于 WinUI 3 的全格式无损音乐播放器，集音乐播放、音频编辑、DSP 效果处理与 VST3 插件支持于一体。播放核心基于 LibVLC，可流畅播放绝大多数音频格式；内置均衡器、频谱分析、相位分析、波形编辑与空间音频等工具，并支持加载 VST3 插件进行独占渲染。

## 功能特性

- 全格式无损播放（基于 LibVLC，支持 FLAC / APE / WAV / DSD / MP3 / AAC 等常见格式）
- 音乐库管理（本地扫描、搜索、最近播放，基于 SQLite / EF Core）
- 歌词面板与迷你播放器
- 10 段均衡器与多种音色预设
- 空间音频 / 立体声展宽 / 标准化等 DSP 效果
- 波形、频谱、相位分析器
- 音频编辑器：选区裁剪、淡入淡出、标准化
- VST3 插件扫描、加载与独占渲染
- 液态玻璃（Liquid Glass）界面主题

## 系统要求

- Windows 10 1809（10.0.17763）或更高版本
- 64 位（x64）系统
- 免安装运行：发布目录为自包含部署，已内置 .NET 8 与 Windows App SDK 运行时

## 运行方式

下载最新 [Release](https://github.com/XeonStudio/FurinaPlayer/releases) 中的压缩包，解压后直接运行 `FurinaPlayer.exe` 即可，无需安装。

## 当前版本

- 版本号：`1.21.08.0013.0810`

## 许可证

Apache License 2.0
