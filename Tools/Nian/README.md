# 《逐年：除夕之战》项目改造记录

当前美术方向已更新为明亮、喜庆的程序化像素风：`bake_festival_pixels.py` 生成 `Resources/Sleigh/Festival/` 内的像素场景、28 种敌人、三名角色、法牌插画、遗物／UI 图标和事件图。当前 ArtIndex 使用这套资源，原先绘画风格源图保留作历史素材。像素纹理使用 Point 采样，无 mipmap，无压缩。开始菜单按钮字号为 1；移动灯笼队列已移除，改为独立背景层的四种间歇烟花，支持“减少动态效果”。

重生成当前美术请运行 `python Tools/Nian/bake_festival_pixels.py`，然后刷新 Unity、清空脚本域缓存，并运行 `Sleigh/Bake Scene and Presentation`。下面关于原 AI 素材和过渡素材的描述记录上一版美术，不代表当前画面的风格。

直接修改原生 Unity 项目，场景入口仍为 `Assets/Scenes/SleighTheSpire.unity`。没有使用 WebView，也没有创建平行游戏框架。

## 已落地

- 47 张基础法牌、24 张伙伴牌、28 种敌人、32 件遗物、20 种药汤、10 种符印、26 个分支事件全部完成文本映射；开场、三名角色、教程、地图、商店、战后点灯、结局同步改写。
- 24 关按进度显示腊月古镇、雪封山道、灯火集市、岁门禁地。保留原有节点模板、遭遇与随机选择机制。
- 怨气／息怒／结盟保留 Grumpy／Softened／Charm 机制；岁火保留 Spirit 的抽牌、能量和最终首领强度影响。
- 普通路线：年兽初现 → 噬岁狂年 → 半血后的岁火决战表现。隐藏路线：与年兽结盟 → 伙伴支援 → 岁门魇。半血表现只添加动画反馈，不修改战斗规则。
- 新卡框具有品质边框和升级印记。图集由 Unity 原生 Sprite 切片使用。所有新资源均有实际消费入口。
- 五段原创合成配乐：菜单、探索、战斗、首领、黎明。声音使用程序合成，无外部音乐采样。原始参考 OGG 未接入新音频银行。
- 原有呼吸、攻击位移、受击闪白、死亡缩放、收服、援助、首领变身及粒子反馈继续使用。

## 上一版绘画素材记录（当前已停用）

已接入八张成功生成的原创源图：村镇场景、年兽初现、噬岁形态、岁门魇、两组九宫格敌人图集、25 格卡牌图集、36 格遗物／图标图集。

图像服务的其余请求多次网络失败。因此以下内容使用独立程序化绘制的过渡素材，**不是全部正式美术已精修完成**：

- 灶糖童子、雪魈、灯笼鬼、灶台鼠、烛火精、机括小狮、纸甲门卒、纸人弩手、爆竹精。
- 三名玩家角色。
- 后 22 张基础法牌及对应的上述伙伴牌插画。
- 地图／休息／商店／事件插画及部分 UI 图标。

全部已替换原主题并接入游戏，未用空白图替代。后续可直接替换对应 PNG 或加入同名图集，不改存档 ID 或战斗代码。

## 兼容性与检查

保留 `Sleigh.Run`、`Sleigh.Ledger`、原字符／卡牌／物品／敌人 ID、序列化字段和公司／产品标识。完整旧存档仍从原路径读取。验证前的存档备份位于忽略提交的 `Temp/Nian/`。

`Artifacts/Nian/baseline.txt` 记录改造前 SHA。`verify_content.py` 逐字段对比费用、数值、升级、行动、遭遇、事件价格和条件，并校验描述占位符、图片引用、图集边界、字体和编码。

Unity 菜单：

- `Sleigh/Verify Migrated Rules`：既有战斗／事件／双路线规则检查。
- `Nian/Verify Theme and Compatibility`：主题、资源、字体、音频与内存存档往返检查，不写玩家存档。
- `Sleigh/Bake Scene and Presentation`：重新烘焙原生场景和预制体。请先退出运行模式。

重生成程序化资源时运行 `python Tools/Nian/bake_art.py`；音乐使用 `bake_music.py`；位图字体使用 `Assets/Sleigh/Editor/BakeChineseFont.py`。新增图集后运行 `import_atlases.py`，在 Unity 刷新后重载脚本域以清空旧 Sprite 缓存，再烘焙场景。图集 PNG 位于 `Assets/Sleigh/Resources/Sleigh/Nian/`，Boss 源图沿用原资源路径以保留 GUID。

`NianVisualReview.Show` 提供编辑模式的隔离画面预览，不写 PlayerPrefs。运行与截图检查通过本地 Unity MCP 完成，未使用 computer use。自动检查不能替代完整人工通关和平衡验收。
