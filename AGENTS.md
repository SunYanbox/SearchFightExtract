# 项目约定（AGENTS）

本文件供 AI 代理与协作者参考，说明本仓库的变更记录规范。

## 变更记录

### 代码与工作流

代码、工作流（`.github/` 等）的更改**默认写入 [CHANGELOG.md](CHANGELOG.md) 的 `[Unreleased]` 段落**。

### 数值机制

技能、天赋、数值、RPG、敌人等**数值机制的改动、增强和削弱**，默认以 **Diff 格式**写入 [BALANCE.md](BALANCE.md) 的 `[Unreleased]` 段落（可参考文件中已有的 ```diff 代码块写法）。

## 版本更新

**仅当用户明确要求更新版本时**才执行版本发布，不得自行提升版本号。

用户要求更新版本时，需同时完成以下三件事：

1. 更新 `SearchFightExtract.csproj` 中的 `<Version>` 版本号；
2. 将 [CHANGELOG.md](CHANGELOG.md) 与 [BALANCE.md](BALANCE.md) 的 `[Unreleased]` 段落内容移入新的版本标题（如 `## [0.1.2] - YYYY-MM-DD`）下；
3. 在文件顶部保留一个空的 `[Unreleased]` 段落。
