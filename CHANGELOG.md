# Changelog

## v1.0.0

### 新增

- 支持 SVG path、完整 SVG 片段和 XAML 矢量片段实时预览。
- 支持导出 PNG / JPG / ICO，并可选择输出尺寸与 ICO 内嵌尺寸。
- 保存图像时自动生成同名 SVG 伴随文件。
- 新增应用图标，以及 GitHub Actions 自动构建和标签发布流程。

### 改进

- 重构为 `src/` 单项目布局，补充 LICENSE、EditorConfig、Issue/PR 模板。
- 界面精简为左右两栏，移除顶部标题区和日志区。
- 统一按钮、输入框、勾选框和下拉框的视觉样式。

### 修复

- 修正 Release 打包时 README / LICENSE 的复制参数。
