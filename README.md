# Path2Ico

将矢量路径数据（SVG `d` 或 WPF Path mini-language）渲染并导出为 **PNG / JPG / ICO** 图标文件的 Windows 桌面工具。

## 功能

- 粘贴 SVG path 或 WPF Path 语法，实时预览渲染效果
- 支持填充色、描边色、描边宽度、留白比例设置
- 任意勾选输出尺寸（16 / 20 / 24 / 32 / 40 / 48 / 64 / 96 / 128 / 256）
- 多格式导出：PNG（透明背景）、JPG（白底）、ICO（可内嵌多尺寸）
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

2. 在「样式」中调整填充色、描边色、描边宽度与留白比例。
3. 在「输出设置」中选择输出目录与文件名。
4. 勾选需要的「输出尺寸」「输出格式」及「ICO 内嵌尺寸」。
5. 点击「生成并保存」，日志区会显示生成结果。

## 项目结构

```
Path2Ico/
├─ App.xaml(.cs)          应用入口
├─ MainWindow.xaml(.cs)   主界面与交互逻辑
├─ Services/
│  ├─ PathRenderer.cs     几何路径渲染为位图
│  ├─ SvgPathNormalizer.cs SVG path 语法归一化
│  ├─ ImageExporter.cs    PNG / JPG 导出
│  └─ IcoWriter.cs        ICO 多尺寸打包
└─ Path2Ico.csproj
```

## 许可

MIT
