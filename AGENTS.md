# AGENTS.md

## 发布流程

- 发版前读取上一个 tag 到当前 HEAD 的提交，按「新增 / 改进 / 修复」更新 `CHANGELOG.md` 的当前版本段落。
- 将更新后的日志内容交给用户确认；确认后才创建并推送版本标签。
- 标签格式为 `v<version>`。推送 `v*` 标签后，release workflow 自动提取 `CHANGELOG.md` 中对应版本段落作为 GitHub Release 正文。
- `CHANGELOG.md` 是 Release 日志的唯一来源；不要直接从 commit 自动生成 Release 正文。
- 未更新对应版本的 `CHANGELOG.md` 时，不得直接打 tag。

## 提交与推送

- 人工验收通过前禁止提交。
- `git commit` 与 `git push` 都必须获得用户明确许可；许可 commit 不等于许可 push。

## 构建与验证

- 构建命令：`dotnet build Path2Ico.sln -c Release`
- 源码位于 `src/`，Release 产物位于 `src/bin/Release/net472/Path2Ico.exe`。
