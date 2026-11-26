# 爆竹驱年源码阅读指南
这份指南对应当前工程，帮助你从整体流程进入具体实现，并为录制复刻教程定位源码。先理解数据和规则，再理解界面如何把操作送进规则、如何播放规则产生的结果。

## 项目入口和命名
- Unity 版本：2022.3.62f2；URP 版本由 `Packages/manifest.json` 固定为 14.0.12。
- 可玩场景：`Assets/Sleigh/Scenes/SleighTheSpire.unity`。
- 游戏控制入口：`Scripts/Core/GameRoot.cs` 中的 `Awake`、`Start`、`Update`。
- 运行流程：标题 → 角色 → 存档槽 → 开场 → 地图 → 节点 → 奖励与送礼 → 下一夜 → 胜利或失败。
- `Sleigh` 是现有命名空间和资源前缀。文件名、角色 ID 和部分英文文本保留了早期主题。中文显示主要通过本地化映射实现，不能把旧名称直接当成无用代码删掉。
- `mrs`、`rudolph` 是当前选人列表使用的 ID；`tinsel` 仍被规则和存档校验支持，不应据此认定其分支无用。

## 实际目录
以下列出游戏自有代码及关键资源，省略 `.meta` 和贴图、音频等逐个素材文件。
```text
SlayTheNian/
├─ README.md                         阅读入口
├─ AGENTS.md                         工程维护约束
├─ Assets/
│  ├─ Sleigh/                        游戏自有内容
│  │  ├─ Scenes/SleighTheSpire.unity 主场景
│  │  ├─ Scripts/
│  │  │  ├─ Core/
│  │  │  │  ├─ GameRoot.cs
│  │  │  │  ├─ SleighStage.cs
│  │  │  │  └─ SleighSceneAuthoring.cs
│  │  │  ├─ Data/
│  │  │  │  ├─ SleighData.cs
│  │  │  │  ├─ Chinese.cs
│  │  │  │  └─ ChinesePixelFont.cs
│  │  │  ├─ Gameplay/
│  │  │  │  ├─ SleighRun.cs
│  │  │  │  └─ SleighEvents.cs
│  │  │  ├─ Battle/SleighBattle.cs
│  │  │  ├─ UI/
│  │  │  │  ├─ GameRoot.Map.cs
│  │  │  │  ├─ GameRoot.Cards.cs
│  │  │  │  ├─ GameRoot.Rewards.cs
│  │  │  │  ├─ GameRoot.Events.cs
│  │  │  │  ├─ GameRoot.Shop.cs
│  │  │  │  ├─ GameRoot.Progression.cs
│  │  │  │  ├─ GameRoot.SaveSlots.cs
│  │  │  │  ├─ SleighBattleView.cs
│  │  │  │  ├─ SleighPixelUI.cs
│  │  │  │  ├─ PixelLabel.cs
│  │  │  │  ├─ Hotspot.cs
│  │  │  │  ├─ MapDrag.cs
│  │  │  │  ├─ SleighViewportLayout.cs
│  │  │  │  ├─ SpringFestivalTitleView.cs
│  │  │  │  └─ FirecrackerButtonBackground.cs
│  │  │  ├─ Presentation/
│  │  │  │  ├─ CardSurfaceEffects.cs
│  │  │  │  ├─ CardSurfaceMesh.cs
│  │  │  │  ├─ SleighPresentation.cs
│  │  │  │  ├─ SleighMotion.cs
│  │  │  │  ├─ SleighInterlude.cs
│  │  │  │  ├─ FestivalStoryView.cs
│  │  │  │  ├─ FestivalBattleEffects.cs
│  │  │  │  ├─ GameRoot.BattleEffects.cs
│  │  │  │  ├─ PixelParticleField.cs
│  │  │  │  ├─ PixelIrisTransition.cs
│  │  │  │  ├─ SpringFestivalMenu.cs
│  │  │  │  ├─ SpringFestivalBackdrop.cs
│  │  │  │  └─ SpringFestivalProp.cs
│  │  │  ├─ Audio/
│  │  │  │  ├─ SleighAudio.cs
│  │  │  │  ├─ SleighAudioBank.cs
│  │  │  │  └─ SleighAudioCompressor.cs
│  │  │  ├─ Utilities/
│  │  │  │  ├─ PixelPoolBucket.cs
│  │  │  │  └─ PixelPoolElement.cs
│  │  │  └─ Development/Test.cs      保留模板
│  │  ├─ Editor/
│  │  │  ├─ Authoring/               场景预制与导入
│  │  │  ├─ Audio/                   音效离线生成与绑定
│  │  │  ├─ Build/                   预览与 Android 构建
│  │  │  ├─ Import/                  字体烘焙
│  │  │  └─ Validation/              原生检查和诊断
│  │  ├─ Resources/Sleigh/
│  │  │  ├─ Definitions.json         玩法定义
│  │  │  ├─ ArtIndex.json            基础美术索引
│  │  │  ├─ FestivalHudArt.json      春节界面美术覆盖索引
│  │  │  ├─ FestivalMapArt.json      春节地图美术覆盖索引
│  │  │  ├─ Localization/zh-CN.json 中文文本
│  │  │  ├─ Fonts/                  点阵图集与字形数据
│  │  │  ├─ Art/                    背景、角色、特效、图标、春节图集
│  │  │  ├─ Prefabs/                GameRoot、ActorView、GiftView
│  │  │  ├─ Audio/AudioBank.asset    音乐音效配置
│  │  │  └─ Materials/CardSurface.mat 卡牌溶解和扫光材质
│  │  ├─ Audio/BGM/                 提供的音乐
│  │  ├─ Audio/SFX/                 提供的音效和 Generated 生成音效
│  │  ├─ Art/AppIcons/              应用图标候选
│  │  ├─ Art/Fonts/                 字体源文件和许可证
│  │  └─ Materials/                 材质与 Shader
│  ├─ Settings/                     Unity 渲染模板设置
│  └─ Scenes/SampleScene.unity       保留示例场景
├─ Packages/                         包清单与锁文件
├─ ProjectSettings/                  Unity 工程设置
├─ Tools/                            Python 和 PowerShell 维护工具
└─ Docs/                             阅读、教程和主题资料
```

`Library`、`Temp`、`Logs`、IDE 自动生成的 `.csproj/.sln` 不作为教程源码。`Builds`、`Deliverables`、`Recordings` 按实际交付和录制需求使用，不能仅凭名称认定其中内容可删除。

## 核心与规则脚本

路径均相对于 `Assets/Sleigh/Scripts`。文件名和类名不总是一致，例如 `SleighRun.cs` 中的核心类名是 `Run`。

| 文件 | 职责 | 优先阅读位置 |
| --- | --- | --- |
| `Core/GameRoot.cs` | 绑定舞台、生命周期、输入分发、界面切换、HUD、标题、选人、节点路由、设置 | `Awake` → `Start` → `NewScreen` → `ShowTitle` → `EnterNode` |
| `Core/SleighStage.cs` | 已烘焙界面层的引用，画面震动、闪光、转场调度 | `View`、`Show`、`Navigate`、`TickIris` |
| `Core/SleighSceneAuthoring.cs` | 编辑器中调用真实界面代码构造静态预览 | `BakeViewsInEditor`、`AuthoringRun` |
| `Data/SleighData.cs` | 配置 DTO、`DB` 索引与 Sprite 缓存、`Gift` 实例、图鉴记录 | `DefinitionBundle` → `DB.Load` → `Gift` → `Ledger` |
| `Data/Chinese.cs` | 本地化加载、文本替换和定义中文化 | `Text`、`Localize` |
| `Data/ChinesePixelFont.cs` | 点阵字体资源加载、字宽与 UV 查询 | `Load`、`Advance`、`UV` |
| `Gameplay/SleighRun.cs` | 一局游戏状态、24 夜路线、种子随机、背包、遗物、送礼与存档 | `New`、`Random`、`PickWish`、`LeaveGift`、`Save/Load` |
| `Gameplay/SleighEvents.cs` | 事件选项的规则结算，返回结果及后续操作要求 | `EventOutcome`、`EventRules.Resolve` |
| `Battle/SleighBattle.cs` | 战斗单位、状态、抽牌、卡牌结算、敌人意图、净化与胜负 | `Unit`、`BattleCue`、构造函数、`StartTurn`、`Play`、`EndTurn` |

`Run`、`Battle`、`EventRules` 不继承 `MonoBehaviour`，但现有实现会使用 `Mathf`、`JsonUtility`、`PlayerPrefs` 等 Unity API；不要把它们描述成已完全脱离 Unity 的独立库。

## 界面脚本

| 文件 | 职责 | 主要入口 |
| --- | --- | --- |
| `UI/GameRoot.Map.cs` | 地图连线、节点、房屋、提示与滚动目标 | `ShowMap`、`MoveMap` |
| `UI/GameRoot.Cards.cs` | 卡牌外观、详情、背包、分页选牌、配件和补给菜单，当前界面刷新 | `Card`、`GiftTip`、`OpenGiftPicker`、`PotionMenu` |
| `UI/GameRoot.Rewards.cs` | 战利品生成、领取标记、选择送礼或压岁钱 | `ShowReward`、`RenderReward`、`ShowLeave` |
| `UI/GameRoot.Events.cs` | 事件选项与结果、事件选牌、休息和宝箱 | `ShowEvent`、`RenderEvent`、`EventPicker`、`ShowRest`、`ShowTreasure` |
| `UI/GameRoot.Shop.cs` | 商品生成、价格、购买、强化与捐赠 | `ShowShop`、`Buy` |
| `UI/GameRoot.Progression.cs` | 敌人图鉴、结局统计、难度解锁 | `OpenLedger`、`ShowEnding` |
| `UI/GameRoot.SaveSlots.cs` | 五个存档槽的显示、新开局覆盖确认和继续 | `ShowSaveSlots`、`StartRunInSlot` |
| `UI/SleighBattleView.cs` | 模型到角色和手牌视图的绑定、选牌与瞄准、战斗反馈队列播放、材质持有 | `StartBattle`、`RefreshBattleUI`、`PlayGift`、`AnimateBattle` |
| `UI/SleighPixelUI.cs` | 静态类 `PixelUI`，创建文字、图片、面板、热点，统一复用和回收 | `Acquire`、`Label`、`Button`、`Clear/Release` |
| `UI/PixelLabel.cs` | 自绘文字网格、测宽、换行和文本更新 | `Lines`、`Set`、`OnPopulateMesh` |
| `UI/Hotspot.cs` | 点击、悬停、按下、拖拽的委托分发 | 各 `OnPointer…`、`ClearBindings` |
| `UI/MapDrag.cs` | 地图拖动、滚轮输入转成移动量 | `OnDrag`、`OnScroll` |
| `UI/SleighViewportLayout.cs` | `SleighStage` 的视口与安全区适配部分 | `FitViewport`、`CoverScreen` |
| `UI/SpringFestivalTitleView.cs` | 绑定烘焙标题按钮，复用时清理交互状态 | `SetAuthoredButtons`、`Bind` |
| `UI/FirecrackerButtonBackground.cs` | 爆竹按钮背景的自绘与悬停、按压表现 | `SetHovered`、`SetPressed`、`OnPopulateMesh` |

`GameRoot` 的多个 `partial` 文件在编译后是同一个类，互相访问私有字段，不是多个可分别挂载的组件。本轮将原 `SleighViews.cs` 拆成了六个文件；查旧教程或历史提交时注意名称变化。`SleighSceneAuthoring.cs` 的编辑器代码也属于这个类，必须留在同一运行时程序集。

## 演出音频和复用

| 文件 | 作用 |
| --- | --- |
| `Presentation/SleighPresentation.cs` | 每帧维护 HUD 显示数字、手牌排列、瞄准提示、地图惯性及环境表现 |
| `Presentation/CardSurfaceEffects.cs` | 卡牌溶解进度、扫光相位、减少动态效果与整张卡的多层绑定 |
| `Presentation/CardSurfaceMesh.cs` | 把各图片和文字的顶点转换到同一张卡的局部坐标，通过 UV1 传给 Shader |
| `Presentation/SleighMotion.cs` | 漂浮、摇摆等通用 UI 动作，复用前重置动作状态 |
| `Presentation/SleighInterlude.cs` | 宝箱开启、遗物揭示和送礼过场协程 |
| `Presentation/FestivalStoryView.cs` | 分页开场和结局剧情，以及 `GameRoot` 中的剧情播放部分 |
| `Presentation/FestivalBattleEffects.cs` | 卡牌风格映射、飞行轨迹与命中特效的自绘网格 |
| `Presentation/GameRoot.BattleEffects.cs` | 将战斗单位位置和规则反馈接入特效组件 |
| `Presentation/PixelParticleField.cs` | 固定容量粒子与纸屑的更新和绘制 |
| `Presentation/PixelIrisTransition.cs` | 纸艺风格开合遮罩网格 |
| `Presentation/SpringFestivalMenu.cs` | 春节标题的构建、装饰绘制与动画 |
| `Presentation/SpringFestivalBackdrop.cs` | 村庄、集市、炉边等程序绘制背景 |
| `Presentation/SpringFestivalProp.cs` | 房屋、福袋等场景小物件 |
| `Audio/SleighAudio.cs` | 四路音乐声部、淡入淡出、压低音乐、音效池和音量设置 |
| `Audio/SleighAudioBank.cs` | `ScriptableObject` 音频资源配置 |
| `Audio/SleighAudioCompressor.cs` | 音频输出动态压缩，控制峰值 |
| `Utilities/PixelPoolBucket.cs` | 每个容器的对象索引、获取和批量回收 |
| `Utilities/PixelPoolElement.cs` | 单个 UI 对象的复用状态、代次和释放 |
| `Development/Test.cs` | Unity 空模板，按工程约定保留，不参与主要功能讲解 |

## 编辑器脚本和工具

以下编辑器路径相对于 `Assets/Sleigh/Editor`。录制教程时应先教运行时闭环，再介绍这些生产工具。

| 文件 | 用途 |
| --- | --- |
| `Authoring/SleighProjectSetup.cs` | `SleighProjectSetup` 维护主场景路径和打开菜单；同文件的 `SleighAssetImport` 设置像素贴图、字体和生成音效的导入参数 |
| `Authoring/SleighSceneBaker.cs` | 创建场景、视图层和预制体并烘焙表现；会写入资源，不是只读检查 |
| `Authoring/SpringFestivalMenuPreview.cs` | 标题菜单预览 |
| `Audio/SleighSoundBaker.cs` | 离线生成音效，绑定提供的音乐与音效到 AudioBank |
| `Audio/FestivalSoundSynth.cs` | 离线 PCM 合成算法，不在玩家运行时合成声音 |
| `Build/SleighDisplaySetup.cs` | 设置 1920×1080 编辑器预览 |
| `Build/SleighAndroidBuild.cs` | Android 横屏配置与测试 APK 构建 |
| `Import/BakeChineseFont.py` | 从源字体生成运行时点阵字形数据与图集 |
| `Validation/SleighProjectChecks.cs` | 场景、预制体、缺失引用、音频及材质所有权检查；`ValidateBatch` 汇总原生检查 |
| `Validation/SleighRuleChecks.cs` | 春节主题和已迁移玩法规则检查 |
| `Validation/SleighFestivalChecks.cs` | 春节剧情和美术表现检查、预览图输出 |
| `Validation/SleighViewportChecks.cs` | 不同屏幕比例与安全区检查 |
| `Validation/SleighTransitionChecks.cs` | 转场绘制和流程检查 |
| `Validation/SleighPolishChecks.cs` | 交互和表现检查 |
| `Validation/SleighBattleEffectsChecks.cs` | 战斗特效映射与表现检查 |
| `Validation/SleighCardSurfaceChecks.cs` | 实际 GPU 渲染卡牌，验证溶解覆盖、文字同步消失、周期扫光、减少动态效果及复用，并输出预览图 |
| `Validation/SleighPlayModeChecks.cs` | 在隔离工程里实际运行标题、重载、选人、存档、剧情、地图、战斗流程 |
| `Validation/SleighRuntimeDiagnostics.cs` | 输出编辑器、场景和按钮绑定诊断 |

| 项目根目录下的工具 | 用途 |
| --- | --- |
| `Tools/audit_project.py` | 只读检查 GUID、资源索引、资源键、构建场景和重复内容 |
| `Tools/compile_project.py` | 依据已生成的 Unity IDE 引用编译当前源文件，不覆盖 `.csproj` |
| `Tools/BuildFestivalArt.py` | 对准备好的图像做裁切、像素缩放、调色板处理和图集打包；使用 Pillow |
| `Tools/CleanFestivalAssets.py` | 有删除行为的历史素材清理工具，不作为日常验证命令运行 |
| `Tools/OrganizeProject.ps1` | 历史一次性目录迁移器，需要审阅后的移动清单，不应重复执行来整理本轮代码 |

## 三条源码追踪路线

### 从点击新游戏到地图

1. `GameRoot.Start` 调用 `ShowTitle`，`SpringFestivalTitleView.Bind` 绑定按钮。
2. `ShowCharacters` 显示角色；`BeginRun` 进入槽位选择或启动指定空槽。
3. `GameRoot.SaveSlots.StartRunInSlot` 创建 `Run`、保存后进入 `ShowIntro`。
4. `FestivalStoryView` 播放开场，完成后导航到 `ShowMap`。
5. `ShowMap` 创建节点热点；点击可达节点后调用 `EnterNode`。

### 从打一张牌到播放结果

1. `SleighBattleView.SelectGift/ClickUnit` 或拖拽流程选择牌与目标。
2. `PlayGift` 调用 `Battle.Play`，由规则检查费用、目标并结算效果。
3. `Battle.Cue` 记录演出信息和状态快照。
4. `AnimateBattle` 逐条消费反馈；`ApplyCueFrame` 更新显示快照，调用文字、动作、音效和特效。
5. 反馈结束后刷新界面，并按战斗结果进入奖励或失败结局。

规则可能先算完，画面随后逐步播放，所以 `battle` 的最终数值和 `displayedUnits`、`viewHp` 等显示快照不一定同时变化。讲解扣血、变身和连击时要区分这两者。

### 从领取奖励到下一夜

1. `ShowReward` 生成一次奖励，`RenderReward` 重画已领取状态。
2. `ShowLeave` 让玩家选年礼或花压岁钱；`Run.LeaveGift/LeaveCookies` 修改局内状态。
3. `SleighInterlude` 播放送礼，之后 `GameRoot.FinishNode` 调用 `Run.FinishNode`。
4. `Run.FinishNode` 标记节点、推进夜晚并保存，控制层返回地图或进入结局。

## 推荐阅读顺序和练习

| 轮次 | 阅读范围 | 读完应能解释 |
| --- | --- | --- |
| 第一轮 | 本指南、主题设定、运行一次主场景 | 玩家一局的完整流程、术语和主要界面 |
| 第二轮 | `SleighData`、`Definitions.json`、`Chinese` | 静态定义与运行实例的区别，ID 如何连接中文与图片 |
| 第三轮 | `SleighRun`、`SleighEvents` | 同种子随机、地图、资源收支和存档 |
| 第四轮 | `SleighBattle` | 一次出牌、一轮回合、净化与胜负的结算顺序 |
| 第五轮 | `GameRoot`、Stage、UI 六个拆分文件、SaveSlots | 屏幕流转、共享弹窗、节点规则如何被界面调用 |
| 第六轮 | `SleighBattleView`、Presentation | 输入、规则结果与动画之间如何协作 |
| 第七轮 | PixelUI、字体、对象池、视口、音频 | 复用为何必须清回调，如何避免旧协程影响新对象 |
| 第八轮 | Editor、Tools、Validation | 从编辑器生产资源，到检查、构建与复现问题 |

先在断点中追踪一张 `sock` 的 `Battle.Play`，再追踪一个普通战节点的进入和完成。第一次阅读不用从复杂的自绘背景开始，也不用逐行背诵所有卡牌分支。

## 按问题找文件

| 想理解或修改的内容 | 优先定位 |
| --- | --- |
| 卡牌费用、稀有度、描述参数 | `Definitions.json`、`GiftDef`、`Gift.Values/Description` |
| 卡牌实际效果与敌人回合 | `Battle.ResolveGift`、`ExecuteMove`、`Relics` |
| 开局卡组与地图模板 | `Run.New`、`Template` |
| 存档槽和旧存档迁移 | `Run.SlotKey/ReadSave/MigrateLegacySave`、`GameRoot.SaveSlots.cs` |
| 商店、事件、休息奖励 | 对应 UI 文件；事件实际结算同时查 `EventRules` |
| 中文显示不对 | `Chinese.Text`、`zh-CN.json`；缺字再查字体图集 |
| 图片不对 | `DB.Frame/Sprite`、三个索引的覆盖顺序和实际 Resources 路径 |
| 按钮可见但不响应 | `Hotspot`、池化清理、TitleView 绑定、GameRoot 编辑器重载恢复 |
| 卡牌动作与数字不同步 | `BattleCue`、`ApplyCueFrame`、`AnimateBattle` |
| 声音绑定与音乐切换 | `SleighAudioBank`、`SleighSoundBaker`、`SleighAudio` |

详细实现教程的章节规划见 [复刻教程详细目录](ReproductionTutorial.md)。

卡牌溶解和周期扫光的参数、实现及预览入口见 [卡牌视觉效果说明](CardSurfaceEffects.md)。
