# 源码阅读整理记录

2026-11-26。本轮面向源码阅读与复刻教程准备，整理界面脚本职责，清除两处可确认的冗余，并补充源码导航和课程目录。

## 代码整理

原 `Assets/Sleigh/Scripts/UI/SleighViews.cs` 集中了地图、卡牌工具、奖励、事件、商店和结局。本轮拆成六个同程序集的 `GameRoot` partial 文件，方法和相关状态放在一起：

| 文件 | 内容 |
| --- | --- |
| `GameRoot.Map.cs` | 地图内容、滚动状态、地图构建与移动 |
| `GameRoot.Cards.cs` | 卡牌构建、提示、背包与选牌、配件、补给菜单和当前界面刷新 |
| `GameRoot.Rewards.cs` | 奖励状态、奖励生成与领取、节点后送礼 |
| `GameRoot.Events.cs` | 事件状态和选择、休息状态、宝箱界面 |
| `GameRoot.Shop.cs` | 商品类型、商品列表、价格、商店生成与购买 |
| `GameRoot.Progression.cs` | 图鉴与结局、统计和难度解锁 |

原脚本的 `.meta` 移至 `GameRoot.Map.cs.meta`，保留 GUID；另外五个文件各有独立元数据。方法提取逐段核对，除了下面的无效参数清理，没有改写原方法逻辑。

清理的代码：

- 删除 `GameRoot.tipContent`：只有声明和赋值，没有读取，实际显示使用 `tipText`。
- 删除 `TiledBackground` 的 `up` 参数：方法体不读取它；同步清理战斗、送礼和编辑器烘焙中的对应实参。

本轮没有删除完整功能脚本。`Development/Test.cs` 是维护约定明确保留的模板；字体许可证、示例场景及现有素材继续保留。没有执行历史资源删除工具。

## 文档入口

- [README](../README.md)：项目入口与常用检查。
- [源码阅读指南](CodeGuide.md)：当前目录、逐脚本职责、三条调用链、阅读顺序和问题定位表。
- [复刻教程详细目录](ReproductionTutorial.md)：8 篇、34 章，包含每章小节、源码位置、验收目标和阶段里程碑；章节正文待后续编写。
- [工程结构与维护约定](ProjectStructure.md)：补充拆分映射，修正与当前工作树不一致的历史目录描述。旧整理报告保留为历史记录。

## 验证结果

- `python Tools/compile_project.py`：0 警告、0 错误。
- `python Tools/audit_project.py`：无错误和警告，233 条美术索引记录有效，无无法解析的序列化 GUID；重复内容仅为保留的字体许可证组。
- 在 `Temp/ProjectOrganizationAudit/UnityValidation` 隔离副本运行 `Sleigh.Editor.SleighProjectChecks.ValidateBatch`：通过；3 个场景、3 个运行时预制体、6931 个对象，无缺失脚本或引用。
- 原生检查包含 3076 项主题断言、1741 项规则断言、107 项春节表现断言，以及材质所有权、视口、转场、交互和战斗特效检查，均通过。
- 隔离副本运行 `Sleigh.Editor.SleighPlayModeChecks.StartBatch`：退出码 0，`PLAY_SMOKE_PASS`。覆盖标题打开时脚本重载、按钮射线、选人、存档槽、开场、地图、普通战、出牌特效、结束回合和首领启动。
- 六个文件的方法拼接与原脚本对照，确认除无效背景实参外内容一致；检查原脚本 GUID 保留、源码目录覆盖、教程章节连续性及文档链接。
- `git diff --check` 通过。

验证日志位于隔离副本的 `Integrity.log`、`PlayMode.log` 与 `Logs`，属于本地临时结果，不进入教程源码。本轮未做发行打包、真机验证或全路线人工通关。

## 保留的用户工作

开始整理时，`ProjectSettings/PackageManagerSettings.asset`、`ProjectSettings/ProjectSettings.asset` 和 `ProjectSettings/ProjectVersion.txt` 已有修改。本轮未编辑这些工作区文件，也未暂存或提交任何改动。Unity 批处理只操作隔离副本，不关闭用户编辑器或保存其工作场景。
