# Path2Ico

将矢量路径数据（SVG `d` 或 WPF Path mini-language）或完整 XAML 矢量片段渲染并导出为 **PNG / JPG / ICO** 图标文件的 Windows 桌面工具。

## 功能

- 粘贴 SVG path / WPF Path 语法，或完整的 XAML 矢量片段，实时预览渲染效果
- 自动识别输入类型：以 `<` 开头视为 XAML 片段，其余按路径数据解析
- 路径模式支持填充色、描边色、描边宽度、留白比例设置
- XAML 片段模式支持多图层、独立填充/描边/透明度/阴影等效果
- PNG / JPG 通过下拉选择输出尺寸（16 / 20 / 24 / 32 / 40 / 48 / 64 / 96 / 128 / 256）
- ICO 可勾选多个内嵌尺寸
- 通过「另存为」对话框导出 PNG（透明背景）/ JPG（白底）/ ICO，一次一种格式
- 自动将 SVG 圆弧命令等 WPF 不兼容语法进行归一化处理

## 环境要求

- Windows
- .NET Framework 4.8
- .NET SDK（用于 `dotnet build`，或使用 Visual Studio 2022）

## 构建与运行

```powershell
dotnet build -c Release
dotnet run
```

生成的可执行文件位于 `bin\Release\net48\Path2Ico.exe`。

## 使用说明

1. 在左侧文本框输入路径数据，例如：

   ```
   M 50,5 L 61,38 L 95,38 L 67,59 L 78,92 L 50,71 L 22,92 L 33,59 L 5,38 L 39,38 Z
   ```

   点击「示例」可填入内置的五角星路径。

   也可粘贴完整 XAML 矢量片段（以 `<` 开头），例如包含 `Viewbox`、`Canvas` 与多个 `Path` 的图标；若片段缺少命名空间会自动补全。此模式下「填充色/描边色/描边宽度/留白比例」不生效，样式由片段自身决定；背景色仍用于 PNG（透明）/ JPG（白底）。

2. 在「样式」中调整填充色、描边色、描边宽度与留白比例（仅路径模式）。
3. 在「输出尺寸（PNG / JPG）」下拉框选择 PNG / JPG 的输出尺寸；在「ICO 内嵌尺寸」勾选要内嵌的多个尺寸。
4. 点击「生成并保存」，在弹出的「另存为」对话框中输入文件名并选择格式（`.png` / `.jpg` / `.ico`），每次保存一种格式；默认不预填文件名、目录与扩展名。
5. 日志区会显示保存结果。

## 项目结构

```
Path2Ico/
├─ App.xaml(.cs)          应用入口
├─ MainWindow.xaml(.cs)   主界面与交互逻辑
├─ Services/
│  ├─ PathRenderer.cs     几何路径渲染为位图
│  ├─ XamlIconRenderer.cs XAML 矢量片段渲染为位图
│  ├─ SvgPathNormalizer.cs SVG path 语法归一化
│  ├─ ImageExporter.cs    PNG / JPG 导出
│  └─ IcoWriter.cs        ICO 多尺寸打包
└─ Path2Ico.csproj
```

## 许可

MIT
