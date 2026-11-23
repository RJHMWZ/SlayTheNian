import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
const runtime='C:/Users/ivy/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const {Presentation,PresentationFile}=await import(pathToFileURL(runtime+'/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs'));
const sharp=createRequire(import.meta.url)(runtime+'/node/node_modules/sharp');
const root='D:/workspace/SlayTheNian', work=root+'/.codex-build/game-intro', assets=work+'/assets';
await fs.mkdir(assets,{recursive:true});
const art=JSON.parse(await fs.readFile(root+'/Assets/Sleigh/Resources/Sleigh/ArtIndex.json','utf8')).frames;
const hud=JSON.parse(await fs.readFile(root+'/Assets/Sleigh/Resources/Sleigh/FestivalHudArt.json','utf8')).frames;
async function crop(type,anim,out){
 const f=[...art,...hud].find(f=>f.type===type&&f.anim===anim&&f.frame===0);
 if(!f)throw Error(type+':'+anim);
 const src=root+'/Assets/Sleigh/Resources/'+f.path+'.png';
 let im=sharp(src);
 if(f.sourceWidth) im=im.extract({left:f.x,top:f.y,width:f.sourceWidth,height:f.sourceHeight});
 await im.trim().resize({height:type==='Enemy'?320:180,kernel:'nearest'}).png().toFile(assets+'/'+out+'.png');
}
await crop('Claus','idle','hero1'); await crop('Claus','tinsel_idle','hero2');
for(const a of ['rat','coalgolem','yeti','penguin','alpha','santa','krampus'])await crop('Enemy',a,a);
for(const a of ['sock','mitten','snowball'])await crop('Gift',a,a);
for(const a of ['heart','coin'])await crop('Icon',a,a);
const P=Presentation.create({slideSize:{width:1280,height:720}});
const C={bg:'#FFF5E5',ink:'#40252E',red:'#A52E38',muted:'#79625D',gold:'#E8B96A',dark:'#291C2A',cream:'#FFF0CD'};
const font='Microsoft YaHei';
function txt(s,t,x,y,w,h,size=28,color=C.ink,bold=false){
 const z=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 z.text=t;z.text.style={typeface:font,fontSize:size,color,bold,autoFit:'none',wrap:'square',insets:0,verticalAlignment:'top'}; return z;
}
async function img(s,file,x,y,w,h,fit='contain'){
 const b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:path.basename(file),position:{left:x,top:y,width:w,height:h},fit});
}
function slide(title,n,dark=false){let s=P.slides.add();s.background.fill=dark?C.dark:C.bg;if(title)txt(s,title,64,46,1150,70,44,dark?C.cream:C.ink,true);txt(s,String(n).padStart(2,'0'),1166,658,50,28,18,dark?C.gold:C.muted);return s;}
function para(s,label,text,y,x=64,w=520,dark=false){txt(s,label,x,y,w,40,27,dark?C.gold:C.red,true);txt(s,text,x,y+48,w,94,27,dark?C.cream:C.ink);}
function note(s,t,refs){s.speakerNotes.text=t+'\n\n项目依据：\n'+refs.join('\n')+'\n配图来自本项目现有美术或离线预览，不代表完整运行截图。';}
let s=slide('',1,true);
await img(s,root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalBattleBackground.png',0,160,1280,560,'cover');
txt(s,'爆竹驱年',64,52,850,110,82,C.cream,true);
txt(s,'春节主题 · 卡牌构筑冒险',70,174,950,55,32,C.gold);
txt(s,'游戏介绍与功能实现',70,239,900,50,27,C.cream);
await img(s,assets+'/santa.png',840,348,340,278);
note(s,'大家好，今天分享我的游戏《爆竹驱年》。它把春节、年礼和生肖伙伴放进卡牌冒险中。下面先简单介绍玩法，再按模块讲功能和实现。建议用时20秒。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Presentation/SpringFestivalMenu.cs','Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalBattleBackground.png','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);
s=slide('游戏概览',2);
txt(s,'守护村落灯火，踏上归乡路',64,144,1130,58,36,C.red,true);
await img(s,root+'/Docs/Previews/春节开场.png',64,231,720,405);
para(s,'游戏类型','单人、回合制卡牌构筑冒险',242,836,370);
para(s,'核心体验','挑选路线与年礼\n在战斗中击败或安抚敌人',380,836,370);
txt(s,'选角色 → 探索节点 → 战斗与成长 → 年兽决战',836,552,370,100,26,C.red,true);
note(s,'故事发生在岁末，年兽与祟气威胁村落。玩家选一名角色，沿24站旅程战斗、做事件、调整卡组，最后迎战年兽。除了造成伤害，也可以用合适的年礼安抚敌人。建议用时40秒。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs','Docs/Previews/春节开场.png']);
s=slide('整体模块划分',3);
txt(s,'Unity 2022.3 LTS  /  C#',64,132,1000,44,27,C.red,true);
const rows=[['Core','负责主菜单、剧情、地图和战斗之间的流转'],['Gameplay / Battle','管理冒险进度、事件选择与回合结算'],['Data','读取玩法定义、本地化文本和美术索引'],['UI / Presentation','显示界面、处理输入并播放动画反馈'],['Audio / Utilities','调度背景音乐与音效，复用 UI 对象']];
for(let i=0;i<rows.length;i++){let y=220+i*77;txt(s,rows[i][0],64,y,350,46,28,C.red,true);txt(s,rows[i][1],430,y,780,52,28);}
note(s,'项目把流程、规则和画面分开。GameRoot调度屏幕，Run保存冒险状态，Battle负责战斗结算，UI读取状态后显示，Presentation播放对应演出。这样改画面时更容易避免影响规则。建议用时40秒。',['Docs/ProjectStructure.md','ProjectSettings/ProjectVersion.txt','Assets/Sleigh/Scripts/Core/GameRoot.cs','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs','Assets/Sleigh/Scripts/Battle/SleighBattle.cs']);
s=slide('角色与专属能力',4);
txt(s,'三位角色，三种战斗思路',64,140,1100,50,34,C.red,true);
para(s,'克劳斯夫人 · 安抚','降低祟气、发现喜好，帮助招募伙伴',228,64,740);
para(s,'廷瑟 · 改装','为手中的年礼安装配件，调整效果',367,64,740);
para(s,'鲁道夫 · 助阵','通过驯鹿队列与充能释放专属能力',506,64,740);
await img(s,assets+'/hero1.png',861,207,205,205);await img(s,assets+'/hero2.png',994,399,205,205);
txt(s,'实现：角色 ID 决定初始配置、充能规则与技能逻辑',64,650,1020,35,23,C.muted);
note(s,'三个角色不是只换外观：夫人偏向安抚，廷瑟偏向配件改装，鲁道夫偏向驯鹿助阵。代码通过角色ID选择初始生命、遗物、充能方式和专属技能。当前主角贴图以项目现有资源为准。建议用时40秒。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: Run.New','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: Charge / Power相关逻辑','Assets/Sleigh/Resources/Sleigh/Art/Characters/yierIdle0.png','Assets/Sleigh/Resources/Sleigh/Art/Characters/bubu1.png']);
s=slide('地图探索与事件',5);
await img(s,root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalMapBackground.png',64,162,700,394);
txt(s,'地图场景素材',64,572,700,32,20,C.muted);
para(s,'功能','24 站旅程，包含战斗、事件、\n精英、休息、商店与宝藏',177,820,395);
para(s,'实现','Run 记录节点与冒险状态\nEventRules 结算事件选项',383,820,395);
txt(s,'节点模板控制旅程结构，随机种子影响遭遇与奖励',64,633,1080,40,26,C.red,true);
note(s,'地图总共24站，但不是完全随机生成连线地图，而是在既有节点模板上组织选择，随机种子参与遭遇与奖励。事件由选项驱动，结算后修改生命、货币、年味或卡组，再返回旅程。建议用时40秒。',['Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: Template / New / Random / Encounter','Assets/Sleigh/Scripts/Gameplay/SleighEvents.cs: EventRules.Resolve','Assets/Sleigh/Scripts/UI/SleighViews.cs','Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalMapBackground.png']);
s=slide('卡牌与回合战斗',6);
para(s,'玩家能做什么','消耗喜气打出年礼，选择目标\n组合攻击、防御、状态与配件效果',162,64,700);
para(s,'回合如何运转','抽牌 → 出牌 → 结束回合\n敌人按意图行动，再进入下一回合',330,64,700);
txt(s,'实现：Battle 维护牌堆与数值，BattleCue 驱动演出',64,530,1100,42,28,C.red,true);
txt(s,'抽牌堆 / 手牌 / 弃牌堆 / 消耗区',64,591,1050,44,27,C.muted);
await img(s,assets+'/sock.png',874,198,145,145);await img(s,assets+'/mitten.png',1042,290,145,145);await img(s,assets+'/snowball.png',861,410,145,145);
note(s,'战斗核心是能量管理和卡牌组合。Battle统一维护手牌、抽牌堆、弃牌堆和消耗区，结算伤害、格挡与状态。每次结算还会产生BattleCue，界面用这些提示播放动作、数字和音效，让规则与演出保持同步。建议用时50秒。',['Assets/Sleigh/Scripts/Battle/SleighBattle.cs: Battle / BattleCue / DamageCalc / BlockCalc','Assets/Sleigh/Scripts/UI/SleighBattleView.cs','Assets/Sleigh/Scripts/Presentation/GameRoot.BattleEffects.cs','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalItemsAtlas.png']);
s=slide('生肖伙伴与安抚机制',7,true);
txt(s,'送出喜欢的年礼，让对手成为伙伴',64,143,1130,55,35,C.gold,true);
const zo=[['rat','子鼠'],['coalgolem','丑牛'],['yeti','寅虎'],['penguin','卯兔']];
for(let i=0;i<zo.length;i++){await img(s,assets+'/'+zo[i][0]+'.png',98+i*295,232,200,190);txt(s,zo[i][1],114+i*295,434,210,45,28,C.cream,true);}
txt(s,'了解喜好 → 降低祟气 → 安定后不再伤害 → 招募伙伴',64,528,1150,52,30,C.gold,true);
txt(s,'实现：喜好与祟气状态判定 + 伙伴卡奖励 + 图鉴记录',64,606,1130,42,26,C.cream);
note(s,'这是游戏最有辨识度的机制。敌人有喜欢和讨厌的年礼，送对礼物能降低祟气。进入安定状态后不再伤害它，就有机会结为伙伴，获得对应伙伴卡，图鉴也会记录。这里只展示四位生肖素材；游戏并不要求集齐十二生肖才能通关。建议用时50秒。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: ChangePips / Charm / SoftHurt','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: AddFriend','Assets/Sleigh/Scripts/Data/SleighData.cs: Ledger','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);
s=slide('成长系统与存档',8);
await img(s,root+'/Docs/Previews/围炉守岁.png',64,170,674,379);
txt(s,'围炉场景预览',64,565,674,32,20,C.muted);
para(s,'成长功能','年礼升级、配件、遗物与药水\n用压岁钱消费，管理生命与年味',173,787,425);
para(s,'存档功能','5 个独立存档位\n支持继续冒险、覆盖与删除确认',368,787,425);
txt(s,'实现：Run 保存进度；JSON 序列化后写入 PlayerPrefs',64,628,1130,44,27,C.red,true);
note(s,'成长围绕卡组和资源展开，休息、商店与奖励让玩家调整构筑。存档方面有五个槽位，Run会把冒险数据转成JSON保存到PlayerPrefs，包含当前旅程和随机状态，能够继续冒险。界面在覆盖和删除前会确认。建议用时40秒。',['Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: SaveSlotCount / Save / Load / rngState','Assets/Sleigh/Scripts/UI/GameRoot.SaveSlots.cs','Assets/Sleigh/Scripts/UI/SleighViews.cs','Docs/Previews/围炉守岁.png']);
s=slide('画面、交互与音频',9);
para(s,'春节像素表现','灯笼、烟花、春节场景与剧情翻页\n提供“减少动态效果”设置',160,64,810);
para(s,'界面与资源','JSON 定义玩法与中文文本\n美术索引连接图集；UI 对象池复用控件',322,64,810);
para(s,'音乐与反馈','按场景切换音乐，播放操作和战斗音效\nAudioBank 绑定资源，音效通过声部池播放',487,64,900);
await img(s,assets+'/alpha.png',972,179,233,253);await img(s,assets+'/coin.png',981,500,107,107);await img(s,assets+'/heart.png',1110,493,101,114);
note(s,'表现层使用春节场景、粒子、转场和战斗反馈；减少动态效果可以降低装饰动画。数据层用Definitions和中文本地化文件管理内容，图集通过JSON索引读取。UI使用对象池减少反复创建，复用时清除旧回调。音频由SleighAudio调度，声音已经作为资源准备，运行时不合成。建议用时50秒。',['Docs/ProjectStructure.md','Assets/Sleigh/Scripts/Presentation/SpringFestivalBackdrop.cs','Assets/Sleigh/Scripts/Presentation/PixelIrisTransition.cs','Assets/Sleigh/Scripts/Data/SleighData.cs','Assets/Sleigh/Scripts/Data/Chinese.cs','Assets/Sleigh/Scripts/Utilities/PixelPoolBucket.cs','Assets/Sleigh/Scripts/Audio/SleighAudio.cs','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png','Assets/Sleigh/Resources/Sleigh/Art/Icons/FestivalHudAtlas.png']);
s=slide('现场演示与后续方向',10,true);
txt(s,'演示顺序',64,171,700,50,34,C.gold,true);
txt(s,'主菜单 → 选角色与存档 → 开场剧情\n地图选节点 → 一场卡牌战斗 → 展示安抚',64,248,840,120,31,C.cream);
txt(s,'后续可继续打磨',64,429,700,50,34,C.gold,true);
txt(s,'角色与卡组平衡、引导体验、主题演出',64,500,860,65,29,C.cream);
await img(s,assets+'/krampus.png',942,224,277,318);
txt(s,'谢谢大家，欢迎体验与交流',64,615,1060,50,35,C.gold,true);
note(s,'如果时间允许，现场演示从主菜单开始，选择角色和存档，快速带过开场，在地图进入一场战斗。重点展示一次出牌，再说明安抚如何获得伙伴。最后邀请大家反馈操作理解和构筑体验。后续方向是可讨论的计划，不代表已经完成的新功能。建议用时30秒，现场演示另计。',['Assets/Sleigh/Scripts/UI/GameRoot.SaveSlots.cs','Assets/Sleigh/Editor/Validation/SleighPlayModeChecks.cs','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);
await (await PresentationFile.exportPptx(P)).save(work+'/candidate.pptx');
for(let i=0;i<P.slides.items.length;i++){
 const sl=P.slides.items[i];
 try{const png=await P.export({slide:sl,format:'png',scale:1});await fs.writeFile(work+'/slide-'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await png.arrayBuffer()));}catch(e){console.log('RENDER_ERROR',i+1,String(e));}
}
console.log('EXPORTED',work+'/candidate.pptx');
await fs.writeFile(work+'/deck.json',JSON.stringify(P.toProto()));
