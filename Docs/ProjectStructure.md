# 项目结构与维护约定

Unity 2022.3.62f2；保留现有命名空间 `Sleigh`、默认程序集、序列化字段、存档键和运行时资源入口。项目整理不修改玩法、数值或界面布局。

## 目录

```text
Assets/Sleigh/
  Scenes/                    游戏主场景
  Scripts/
    Core/                    GameRoot、舞台、同程序集的场景烘焙 partial
    Gameplay/                Run、事件规则
    Battle/                  战斗模型与结算规则
    Data/                    数据定义、资源索引、本地化、字体读取
    UI/                      界面、战斗视图、存档面板、输入和布局
    Presentation/            春节背景、动画、粒子、剧情和过场
    Audio/                   音乐调度、音效池、音频配置类型
    Utilities/               UI 对象池
    Development/             保留的 Test.cs 模板
  Editor/
    Authoring/               场景、预制体、菜单预览及导入设置
    Audio/                   声音资源绑定与离线合成
    Build/                   构建、显示设置
    Import/                  字体生成；Legacy/ 保留旧参考导入器
    Validation/              项目、规则、主题、画面、视口、转场检查
  Audio/
    BGM/                     MainMenu、NormalBattle、BossBattle、Exploration、Ending
    SFX/                     Victory、Defeat；Generated/ 为离线生成音效
    Legacy/                  保留旧版源音乐、烘焙音乐及未再绑定的胜负音效
  Art/Fonts/                 字体源文件及许可证
  Materials/                 已绑定的材质与 shader
  Resources/Sleigh/
    Art/                     Characters、Cards、Backgrounds、Icons、Effects、UI
    Prefabs/                 保留资源键的 GameRoot、ActorView、GiftView
    Audio/AudioBank.asset    保留运行时加载入口的音频配置
    Fonts/                   运行时点阵字体与授权文件
    Localization/            中文文本
    UI/                      已有菜单按钮贴图
    *.json                   定义和三个美术索引，保留原加载键
```

`Assets/Settings`、URP 全局设置、包和插件目录保持原样。未构建的 `Assets/Scenes/SampleScene.unity` 保留，不根据静态分析直接删除。

## 依赖与入口

- `GameRoot` 负责屏幕流转；通过现有 partial 文件调用 UI 和演出。
- `Run`、`Battle`、`EventRules` 依赖 `DB` 定义；UI 读取模型并播放提示，不改写规则。
- `DB` 以 `type/anim` 为稳定的逻辑键，三个 JSON 索引决定实际贴图路径。图集坐标使用原始像素，显示尺寸独立保存。
- `PixelUI` 使用对象池创建和回收控件；控件复用时解除旧回调。运行时闪白材质由 GameRoot 持有副本并在销毁时释放。
- `SleighAudio` 使用四个预建音乐 AudioSource 和音效声部池。`AudioBank.asset` 通过 GUID 引用实际音频，运行时不合成声音。
- `SleighSceneAuthoring.cs` 虽为 editor-only 功能，仍与 GameRoot 保持同程序集，不能直接搬到 Editor 目录。
- 场景路径由 `SleighProjectSetup.ScenePath` 和构建配置共同维护。不要因目录整洁引入 asmdef 或重命名序列化类。

## 资源移动和生成

1. 文件与 `.meta` 同步移动，保留原 GUID；不要重新导出图片、转码音频或重建字体来完成目录整理。
2. 图像目录变化时同步三个美术 JSON 索引和参考 CSV；逻辑动画键保持不变。资源 JSON 和 Resources prefab 根入口保持稳定。
3. 音乐和提供的胜负音效绑定集中于 `Editor/Audio/SleighSoundBaker.cs`。重新烘焙保留五首提供的音乐及两条提供的音效。
4. `FestivalSoundSynth.cs` 是离线音效算法；从现有菜单烘焙资源即可。未经需求不要修改已经确认的声音或导入质量。
5. `Editor/Import/BakeChineseFont.py` 从 `Art/Fonts/FusionPixel12.ttf` 生成运行时点阵字体；整理目录时不执行字体重烘焙。
6. `Import/Legacy/ImportReference.py` 为旧项目迁移工具，源数据当前不在仓库。它不是日常生成入口；不要用旧参考覆盖现有玩法配置。

## 验证

- `python Tools/audit_project.py`：检查 GUID、包资源引用、构建场景、Resources 字符串和索引。
- `python Tools/compile_project.py`：使用已有 Unity IDE 引用编译当前脚本；临时工程写入 Temp，不修改自动生成的 IDE 工程文件。
- Unity 菜单 `Sleigh/Verify Project Integrity`：实际打开全部项目场景、加载三个 Resources prefab、检查缺失脚本和对象引用、检查音频绑定。
- 原有规则、主题、春节画面、视口和转场检查继续保留。批处理入口 `Sleigh.Editor.SleighProjectChecks.ValidateBatch` 汇总执行。
- 涉及初始化、输入、脚本重载时，还要在隔离副本 `Temp/ProjectOrganizationAudit/UnityValidation` 运行 `Sleigh.Editor.SleighPlayModeChecks.StartBatch`，验证真实Play Mode按钮射线、选人、存档、剧情、地图和战斗。测试使用独立PlayerPrefs范围，不操作玩家存档。
- 编辑器运行中重载脚本会重新绑定单例、按钮和音频，并返回主菜单；可以从自动存档继续。该恢复逻辑不进入玩家构建。
- 有编辑器或未保存场景时，在独立项目副本上跑批处理；不关闭用户编辑器、不重烘焙或覆盖用户场景。
- `Tools/OrganizeProject.ps1` 是本次一次性迁移的受限执行器，必须配合审阅过的移动计划使用，不应在后续开发中盲目重复执行。

## 保留候选与限制

- 四组同内容文件属于字体许可证；为满足各字体目录的授权归档而保留。
- 旧音乐和两条旧胜负音效已移到 `Audio/Legacy`，没有删除或重新压缩。
- 旧版图标、教程和品牌贴图仍可能被原美术索引、烘焙视图或动态资源逻辑访问，保留现有内容。进一步删除需要独立验证资源加载与发行构建。
- `Test.cs`、样例场景、字体字符表及参考 CSV 保留；CSV 仅为原提取参考，当前运行时 JSON 为准。
- 没有调整第三方包、Unity 缓存、平台配置或 Android 构建依赖。移动后首次切回 Unity，编辑器可能需要刷新资源和重新生成 IDE 工程。
