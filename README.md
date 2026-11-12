<p align="center"><img src="src/Assets/Path2Ico.png" width="96" alt="Path2Ico" /></p>

# Path2Ico

将矢量路径数据（SVG `d` 或 WPF Path mini-language）或完整 XAML / SVG 矢量片段渲染并导出为 **PNG / JPG / ICO** 图标文件的 Windows 桌面工具。

[![build](https://github.com/lengyuefenghua/Path2Ico/actions/workflows/build.yml/badge.svg)](https://github.com/lengyuefenghua/Path2Ico/actions/workflows/build.yml)
[![license](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## 功能

- 粘贴 SVG path / WPF Path 语法、完整 SVG 片段，或完整的 XAML 矢量片段，实时预览渲染效果
- 自动识别输入类型：根元素为 `svg` 时按 SVG 片段处理，其余以 `<` 开头视为 XAML 片段，否则按路径数据解析
- 路径模式支持填充色、描边色、描边宽度、留白比例设置
- XAML 片段模式支持多图层、独立填充/描边/透明度/阴影等效果
- PNG / JPG 通过下拉选择输出尺寸（16 / 20 / 24 / 32 / 40 / 48 / 64 / 96 / 128 / 256）
- ICO 可勾选多个内嵌尺寸
- 保存 PNG / JPG / ICO 时，同目录生成同名 SVG；路径模式按当前样式与留白重建，SVG 输入原样保存
- 通过「另存为」对话框导出 PNG（透明背景）/ JPG（白底）/ ICO，一次一种格式
- 自动将 SVG 圆弧命令等 WPF 不兼容语法进行归一化处理
- 支持常见 SVG 片段：`svg` / `g` / `rect` / `circle` / `ellipse` / `line` / `polyline` / `polygon` / `path`，以及基础填充、描边、透明度与 `translate` / `scale` / `rotate` / `matrix` / `skew` 变换

## 支持范围与已知限制

- SVG 片段仅支持上述元素和基础表现属性；暂不支持渐变、`use`、裁剪、遮罩与滤镜。
- XAML / SVG 片段模式会忽略「填充色 / 描边色 / 描边宽度 / 留白比例」，样式由片段自身决定。
- XAML 片段暂不生成 SVG 伴随文件，保存时会提示并仅输出图像。
- 每次保存选择一种图像格式，并生成同名 SVG（路径 / SVG 输入）。
- 仅支持 Windows。

## 环境要求

- Windows 10 / 11
- .NET Framework 4.7.2 或更高版本运行时
- 构建需要 .NET SDK 8.0+（命令行）或 Visual Studio 2022

## 构建与运行

```powershell
dotnet build Path2Ico.sln -c Release
dotnet run --project src/Path2Ico.csproj
```

Release 产物位于 `src/bin/Release/net472/Path2Ico.exe`。`dotnet run` 默认使用 Debug 配置。

## 使用说明

1. 在左侧文本框输入路径数据，例如：

   ```text
   M 50,5 L 61,38 L 95,38 L 67,59 L 78,92 L 50,71 L 22,92 L 33,59 L 5,38 L 39,38 Z
   ```

   点击「载入示例」可填入内置的五角星路径。

   也可粘贴完整 SVG 片段（根元素为 `<svg>`）或完整 XAML 矢量片段（以 `<` 开头），例如包含 `Viewbox`、`Canvas` 与多个 `Path` 的图标；若片段缺少命名空间会自动补全。

2. 在「样式」中调整填充色、描边色、描边宽度与留白比例（仅路径模式）。
3. 在「输出」中选择 PNG / JPG 的输出尺寸，并勾选 ICO 要内嵌的多个尺寸。
4. 点击「生成并保存」，在弹出的「另存为」对话框中输入文件名并选择格式（`.png` / `.jpg` / `.ico`）；默认不预填文件名、目录与扩展名。
5. 每次保存图像后，会在同目录写入同名 `.svg`；XAML 片段会跳过 SVG 并弹出提示；成功保存后不会额外弹窗。

## 项目结构

```text
Path2Ico/
├─ .github/
│  ├─ workflows/build.yml       GitHub Actions 构建
│  ├─ ISSUE_TEMPLATE/           Issue 模板
│  └─ PULL_REQUEST_TEMPLATE.md  PR 模板
├─ src/
│  ├─ App.xaml(.cs)             应用入口
│  ├─ MainWindow.xaml(.cs)      主界面与交互逻辑
│  ├─ Assets/                   应用图标
│  ├─ Services/
│  │  ├─ PathRenderer.cs        几何路径渲染为位图
│  │  ├─ XamlIconRenderer.cs    XAML 矢量片段渲染为位图
│  │  ├─ SvgToXamlConverter.cs  SVG 片段转换为 XAML
│  │  ├─ SvgPathNormalizer.cs   SVG path 语法归一化
│  │  ├─ ImageExporter.cs       PNG / JPG 导出
│  │  ├─ SvgExporter.cs         SVG 伴随文件生成
│  │  └─ IcoWriter.cs           ICO 多尺寸打包
│  └─ Path2Ico.csproj
├─ Path2Ico.sln
├─ LICENSE
└─ README.md
```

## 贡献

欢迎通过 Issue 反馈问题或提出建议，也可以提交 Pull Request。提交前请先运行：

```powershell
dotnet build Path2Ico.sln -c Release
```

## 许可

本项目采用 [MIT License](LICENSE)。
