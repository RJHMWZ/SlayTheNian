# 爆竹驱年项目导航

Unity 2022.3.62f2 项目。游戏主场景为 `Assets/Sleigh/Scenes/SleighTheSpire.unity`，也可从 Unity 菜单 `Sleigh/Open Game Scene` 打开。

准备阅读代码或制作复刻教程时，从以下文档开始：

- [源码阅读指南](Docs/CodeGuide.md)：实际文件目录、每个脚本的职责、主要调用链和阅读顺序。
- [复刻教程详细目录](Docs/ReproductionTutorial.md)：从空项目到完整游戏的分章实现路线、对应源码和验收目标。
- [工程结构与维护约定](Docs/ProjectStructure.md)：资源路径、GUID、程序集、对象池和验证约束。
- [本轮整理记录](Docs/CodeOrganizationReport.md)：这次拆分和清理的范围与验证记录。
- [卡牌溶解和周期扫光](Docs/CardSurfaceEffects.md)：视觉效果实现、参数、参考来源与预览方法。
- [春节主题设定](Docs/十二生肖与年兽主题.md)：主题和玩法词汇的对照。

初次阅读建议：`SleighData.cs` → `SleighRun.cs` → `SleighBattle.cs` → `GameRoot.cs` → 各界面及演出文件。`Scripts/Development/Test.cs` 是保留的 Unity 模板，不是游戏入口。

本地基础检查：

```powershell
python Tools/audit_project.py
python Tools/compile_project.py
git diff --check
```

编译工具需要 Unity 已生成 IDE 工程并安装现有包。Unity 原生检查与隔离副本验证方式见维护文档。
