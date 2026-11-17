Sleigh the Spire - HAR资源提取包

提取来源：用户上传的 Chrome Network HAR
目标游戏：Sleigh the Spire
游戏资源路径：html-classic.itch.zone/html/20473636/

本包内容：
- scripts/：HAR 中实际捕获到的游戏脚本与 Construct 运行时脚本
- data.json：Construct 导出运行时数据，包含对象/动画/精灵图集帧区域等信息
- images/：原始 WebP 精灵图集
- icons/：PNG 图标
- media/：捕获到的 WebM 音频
- Unity_Ready/Images/：由 WebP 转换的 PNG 图集，可直接导入 Unity 2022
- Unity_Ready/Audio/：由 WebM 转换的 OGG 音频，可直接导入 Unity
- Resource_Index.csv：提取文件清单与来源 URL

本次提取统计：
- HAR 请求记录：181
- 提取文件：62
- 游戏脚本文件：26
- 原始 WebP 图集：19
- PNG 图标：7
- WebM 音频：4
- PNG 转换图集：19
- OGG 转换音频：4

Unity 使用建议：
1. 先将 Unity_Ready/Images 中的 PNG 图集导入 Unity。
2. 保留原始 images/ 与 data.json；data.json 中的帧坐标可用于后续拆分 Sprite。
3. Unity Sprite Editor 中可按 data.json 中的帧矩形切片；不要直接把整张图集当成单个 Sprite。
4. 将 Unity_Ready/Audio 中的 OGG 导入 Unity，按 BGM/SFX 再分类。
5. scripts/project/ 下的 JS 可用于理解原玩法、卡牌、战斗、敌人、场景和 UI 逻辑；不能直接作为 Unity C# 脚本运行，需要按机制重写。

重要限制：
- HAR 只包含浏览器本次运行时实际请求并成功记录响应体的文件，不保证覆盖所有隐藏/未触发关卡资源。
- 这不是原始 Construct 编辑器工程（.c3p），data.json 是运行时导出数据。
- HAR 中未捕获响应体、缓存未命中或仅有空响应的资源无法从这份文件恢复。
- 此包适合个人学习、分析和获得授权的复刻用途；公开发布或商业使用素材前，请确认权利人许可。
