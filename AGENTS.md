# AGENTS.md

EnigmaNet 仓库级维护说明。

## 适用范围
- 本文件作用于当前目录为根的整个仓库。
- 只要框架目标、依赖版本、构建或打包策略发生变化，就需要同步更新本文件。

## 双目标基线（.NET 8 与 .NET 10）
- 当前基线是 23 个项目全部多目标：
  - `TargetFrameworks` = `net8.0;net10.0`
- 目标是保持单一代码库、单一 NuGet 包线，并确保两个 TFM 的公共 API 兼容。

## EF Core 版本映射规则
- 涉及 EF 的项目必须按目标框架使用条件 `PackageReference`。
- 必须满足如下映射：
  - net8.0 -> EF Core 8 + 对应主版本的 Provider
  - net10.0 -> EF Core 10 + 对应主版本的 Provider
- 当前已落地在：
  - `EnigmaNet.EntityFramework/EnigmaNet.EntityFramework.csproj`
  - `EnigmaNet.ChinaDivisions.Pg/EnigmaNet.ChinaDivisions.Pg.csproj`

## EF 内部 API 兼容说明
- `EnigmaNet.EntityFramework/DatabaseForeignKeyMigrationsModelDiffer.cs` 使用了 EF 内部 API 路径。
- 在 net10.0 下，`MigrationsModelDiffer` 构造函数需要 `IRelationalAnnotationProvider`。
- 除非整体实现重构，否则应保留条件编译（`#if NET10_0_OR_GREATER`）。

## 依赖策略
- 以兼容性与稳定性优先，避免无关的大范围升级。
- `Microsoft.Extensions.*` 版本应保持一致，避免 NU1605 包降级问题。
- 已知冲突模式：
  - `Microsoft.Extensions.Http 8.0.1` 依赖 `Microsoft.Extensions.Logging.Abstractions >= 8.0.2`。
- 对于 `Web SDK` 项目，框架已自动提供的包不要重复显式引用。

## 打包策略
- 预期结果：每个项目产出的包都同时包含 `lib/net8.0` 与 `lib/net10.0` 资产。
- 需要打包的 `Web SDK` 类库应显式设置 `IsPackable=true`。
- 需要核验 `.nuspec` 中两个目标框架的依赖组是否齐全。

## 必做验证命令
每次涉及框架或依赖变更后，都应执行以下验证：

1. 还原
   - `dotnet restore EnigmaNet.sln`
2. 项目级双框架构建矩阵
   - 每个 `.csproj` 分别执行 `-f net8.0` 与 `-f net10.0`
3. 解决方案级双框架构建矩阵
   - `dotnet build EnigmaNet.sln -c Release -p:TargetFramework=net8.0`
   - `dotnet build EnigmaNet.sln -c Release -p:TargetFramework=net10.0`
4. 打包验证
   - `dotnet pack <project> -c Release`
   - 确认包内含双 TFM 资产，且依赖组正确
5. 消费验证
   - 创建临时 net8/net10 消费项目并引用本地 nupkg 验证

## 当前测试基线
- 当前解决方案没有测试项目。
- 仍应执行 `dotnet test EnigmaNet.sln`，用于命令链路验证和后续测试接入预留。

## 已知非阻断告警
- `EnigmaNet.QCloud.Cls` 中的部分告警（例如 CS8981、SYSLIB0014）为历史遗留，不是本次双目标改造引入。
- 除非变为阻断问题，否则作为独立清理项处理。

## 维护规则
- 后续若修改框架目标、EF/Provider 版本或打包策略，必须在同一 PR 中同步更新本 AGENTS.md。
