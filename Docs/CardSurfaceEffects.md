# 卡牌溶解和周期扫光

效果参考 [Cyanilux Cards](https://github.com/Cyanilux/Cards)，阅读版本为 `2a2d33f8d74748082b9f2695fadb81bc99eb1171`。原项目的 `Card.cs` 在用牌时推进溶解参数，Shader Graph 中的 Dissolve 使用噪声，Shine 使用时间和周期函数生成亮带。原项目面向 3D 卡牌；本项目按既有 UGUI 图片和点阵文字结构重新实现，无需导入原场景、模型、渲染器配置或升级包。

原项目许可归档于 [CyaniluxCardsLicense.txt](CyaniluxCardsLicense.txt)。

## 当前表现

- 战斗用牌后，保留原有约 0.14 秒的飞行动作，再用约 0.34 秒播放不规则溶解，边缘带暖金色。
- 卡框、插图、图标和点阵文字共享同一个卡牌局部坐标，沿同一条溶解边界消失。
- 静置卡牌约每 4.8 秒出现一次斜向白光；亮带扫过约占周期的 26%，其余时间保持正常。卡牌相位错开，不使用局内随机数。
- 开启减少动态效果时关闭周期扫光，用牌后的消失改为短暂淡出。
- 卡牌回收和重新配置时归零溶解进度，保持现有点击、拖拽、费用、牌堆与胜负规则。

## 实现位置

| 文件 | 内容 |
| --- | --- |
| `Scripts/Presentation/CardSurfaceEffects.cs` | 配置相位、绑定多层图形，维护溶解进度与减少动态效果 |
| `Scripts/Presentation/CardSurfaceMesh.cs` | 计算子图形到卡牌根节点的坐标变换，把局部坐标、进度和相位写入 UV1 |
| `Materials/CardSurface.shader` | 像素噪声溶解、暖金边缘、斜向白色扫光及 UGUI 遮罩兼容 |
| `Resources/Sleigh/Materials/CardSurface.mat` | Shader 的打包引用及外观参数 |
| `Scripts/UI/GameRoot.Cards.cs` | 所有通过 `Card` 工厂生成的运行时卡牌自动接入效果 |
| `Scripts/UI/SleighBattleView.cs` | `FlyPlayedGift` 在用牌飞行后推进溶解并回收 |
| `Editor/Validation/SleighCardSurfaceChecks.cs` | GPU 渲染检查与静态预览输出 |

以上路径相对于 `Assets/Sleigh`。材质资产只读使用；每张牌的变化通过顶点数据传入，不逐帧修改共享材质，也不需要每个文字和图片克隆材质。`CardSurfaceMesh` 停用时恢复原材质，复用时重新绑定；UV0 保留原图片或字体图集坐标，UV1 独立保存整张卡的坐标。

## 外观调整

在 `CardSurface.mat` 上调整：

| 参数 | 默认值 | 含义 |
| --- | --- | --- |
| Shine Period Seconds | 4.8 | 扫光周期，秒 |
| Shine Width | 0.14 | 亮带宽度 |
| Shine Strength | 0.48 | 亮带向白色混合的强度 |
| Dissolve Edge | 暖金色 | 溶解边缘颜色 |

溶解时长在 `FlyPlayedGift` 的 `Tween(.34f, …)`；Shader 中的 `.26` 决定亮带运动占周期的比例。隐藏的 `_PreviewTime` 默认为 -1，运行时使用正常时间；验证工具只在临时材质副本上固定时间，便于对照周期。

## 验证和预览

Unity 菜单 `Sleigh/Verify Card Surfaces and Render Previews` 会在预览场景中生成实际卡牌，检查溶解各阶段的像素覆盖、终点文字完全消失、扫光周期、减少动态效果和复用。输出写入执行工程的 `Logs/CardSurfacePreviews`，不修改工作场景。

有图形设备的 `SleighProjectChecks.ValidateBatch` 包含该检查；无图形设备时跳过 GPU 部分。Play Mode 检查另外确认实际用牌协程经过中间溶解状态，并继续验证菜单重载、射线、战斗出牌和结束回合。
