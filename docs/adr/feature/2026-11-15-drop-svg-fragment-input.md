---
status: accepted
date: 2026-11-15
---

# 富效果图标改由 XAML 片段承载，不再内置 SVG 片段渲染

## 背景

内置的 SVG 子集转换器只覆盖基础形状与纯色填充，遇到 `linearGradient`、`filter`、`clipPath` 时静默丢弃，导致同一份图标在浏览器与本工具中呈现完全不同。补齐这些特性要么持续手写解析器，要么引入第三方 SVG 引擎。

## 结论

移除 `<svg>` 片段输入，输入收敛为 Path 数据与 XAML 矢量片段。XAML 由 WPF 自身解析与渲染，渐变、阴影、`StaticResource` 等能力与 WPF 完全一致，无需维护第二套渲染管线。

## 取舍

- 放弃「粘贴 SVG 就能用」，换取渲染结果与解析语义只有一套。
- 检测到 `<svg>` 时明确提示而不是静默留空，避免用户误判为程序故障。

## Considered Options

- **手写渐变与投影支持**：只能覆盖 `linearGradient`/`radialGradient` 和近似的 `feDropShadow`，`clipPath`/`mask`/`use` 仍需逐项补，长期维护成本持续增长，且永远无法与浏览器完全对齐。
- **引入 SharpVectors 等 SVG 引擎**：覆盖度高，但给零依赖的自包含小工具增加运行时体积与 net472 兼容风险，且与现有 `PathRenderer` 的坐标系、尺寸策略需要重新对接。
- **WebView 渲染后截图**：与浏览器一致性最好，但小尺寸 ICO 的采样质量差，还要引入浏览器运行时。
