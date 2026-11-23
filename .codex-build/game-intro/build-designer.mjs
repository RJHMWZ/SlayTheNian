import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
const runtime='C:/Users/ivy/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const {Presentation,PresentationFile}=await import(pathToFileURL(runtime+'/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs'));
const root='D:/workspace/SlayTheNian', work=root+'/.codex-build/game-intro/designer', assets=root+'/.codex-build/game-intro/assets';
await fs.mkdir(work,{recursive:true});
const P=Presentation.create({slideSize:{width:1280,height:720}});
const C={bg:'#FFF5E5',ink:'#40252E',red:'#A52E38',muted:'#79625D',gold:'#E8B96A',dark:'#291C2A',cream:'#FFF0CD'};
function txt(s,t,x,y,w,h,size=28,color=C.ink,bold=false){
 const z=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 z.text=t;z.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,autoFit:'none',wrap:'square',insets:0,verticalAlignment:'top'}; return z;
}
async function img(s,file,x,y,w,h,fit='contain'){
 const b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:path.basename(file),position:{left:x,top:y,width:w,height:h},fit});
}
function slide(title,n,dark=false){const s=P.slides.add();s.background.fill=dark?C.dark:C.bg;if(title)txt(s,title,64,46,1150,70,44,dark?C.cream:C.ink,true);txt(s,String(n).padStart(2,'0'),1166,658,50,28,18,dark?C.gold:C.muted);return s;}
function para(s,label,text,y,x=64,w=740,dark=false){txt(s,label,x,y,w,40,27,dark?C.gold:C.red,true);txt(s,text,x,y+47,w,110,28,dark?C.cream:C.ink);}
function impl(s,t,dark=false){txt(s,'实现：'+t,64,622,1080,62,24,dark?C.cream:C.muted);}
function note(s,t,refs){s.speakerNotes.text=t+'\n\n项目依据：\n'+refs.join('\n')+'\n配图为项目现有素材或离线场景预览。';}
let s=slide('',1,true);
await img(s,root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalBattleBackground.png',0,160,1280,560,'cover');
txt(s,'爆竹驱年',64,52,850,110,82,C.cream,true);
txt(s,'一段关于守岁、送礼与结伴的卡牌冒险',70,174,1100,55,34,C.gold);
txt(s,'我的设计思路与实现取舍',70,239,1000,50,28,C.cream);
note(s,'大家好，今天分享我的游戏《爆竹驱年》。它是一款春节主题的回合制卡牌构筑游戏。我想讲的重点是：我希望玩家获得怎样的体验，以及每个机制为什么适合这段守岁的故事。整个介绍约5到8分钟。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Presentation/SpringFestivalMenu.cs','Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalBattleBackground.png']);

s=slide('设计出发点：让玩家参与“守岁”',2);
await img(s,root+'/Docs/Previews/春节开场.png',64,181,674,379);
para(s,'我想带来的体验','在策略冒险中，感受到送礼、\n结伴与守护灯火的意义',171,790,430);
para(s,'为什么选择春节','年兽提供共同的威胁\n年礼与生肖承接玩家的行动',377,790,430);
txt(s,'用卡牌构筑承载策略，用归乡旅程连接这些选择',64,586,1130,48,30,C.red,true);
impl(s,'三幕开场交代动机；年礼、年味与伙伴机制回应主题');
note(s,'我的设计出发点，是让玩家通过行动参与守岁。春节里有送礼、团圆、守护灯火这些具体的事情，适合变成游戏中的选择：用什么年礼、帮助谁、是否留下资源。年兽提供明确的冒险目标，卡牌构筑则承载每一回合的策略。开场先展示村落的生活，再引入威胁，是为了让玩家理解自己为什么出发。这里表达的是我希望带来的体验，是否成立还需要试玩反馈。',['Docs/十二生肖与年兽主题.md','Assets/Sleigh/Scripts/Presentation/FestivalStoryView.cs','Docs/Previews/春节开场.png']);

s=slide('卡牌战斗：让每次出牌都有取舍',3);
para(s,'为什么用回合制卡牌','让玩家有时间判断局面\n用有限喜气决定攻击、防御和出牌顺序',161);
para(s,'功能怎样支撑这个目标','手牌带来变化，敌人意图提供判断依据\n升级和配件延伸同一张年礼的用法',342);
txt(s,'设计取舍：保留组合空间，同时要让效果和结算容易理解',64,545,1130,60,29,C.red,true);
await img(s,assets+'/sock.png',921,176,148,148);await img(s,assets+'/mitten.png',1055,297,148,148);await img(s,assets+'/snowball.png',895,414,148,148);
impl(s,'Battle 处理牌堆与结算；BattleCue 把结算交给界面演出');
note(s,'我选择回合制卡牌，是希望玩家把注意力放在判断上。喜气有限，所以这一回合先挡伤害、先打输出，还是留出配合空间，都需要取舍。抽牌带来变化，敌人意图给玩家可参考的信息。代价是组合越多，规则越需要讲清楚，所以结算由Battle统一处理，再通过BattleCue展示反馈。这里的目标是让玩家能理解选择与结果的联系，不预先声称平衡已经完成。',['Assets/Sleigh/Scripts/Battle/SleighBattle.cs: Battle / ChooseIntent / BattleCue','Assets/Sleigh/Scripts/UI/SleighBattleView.cs','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalItemsAtlas.png']);

s=slide('角色设计：提供不同的解题方式',4);
txt(s,'我希望换一个角色，就会重新考虑卡牌与资源的价值',64,142,1120,60,32,C.red,true);
para(s,'克劳斯夫人 · 理解对手','发现喜好、降低祟气，帮助玩家走安抚路线',241,64,795);
para(s,'廷瑟 · 调整工具','给年礼安装配件，把成长集中到卡牌组合上',367,64,795);
para(s,'鲁道夫 · 等待时机','通过充能与驯鹿助阵，安排专属能力的使用',493,64,795);
await img(s,assets+'/hero1.png',898,222,205,192);await img(s,assets+'/hero2.png',1018,420,205,192);
impl(s,'保留原有三位主角；角色 ID 区分初始遗物、技能与充能规则');
note(s,'角色差异承担的是不同的解题方向。夫人的能力引导玩家留意祟气和喜好，廷瑟让玩家关注配件与卡牌效果，鲁道夫强调充能和助阵时机。这样，玩家使用同一套基础战斗规则，也可以形成不同侧重点。项目中保留了原有三位角色的身份和能力，生肖作为旅途中可结交的伙伴出现。这也控制了主题改造的范围，不需要为了换主题重新扩展职业系统。',['Docs/十二生肖与年兽主题.md: 可选角色','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: New','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: Charge / Power','Assets/Sleigh/Resources/Sleigh/Art/Characters/yierIdle0.png','Assets/Sleigh/Resources/Sleigh/Art/Characters/bubu1.png']);

s=slide('地图与事件：安排冒险的张弛',5);
await img(s,root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalMapBackground.png',64,170,674,379);
txt(s,'地图背景素材',64,561,674,31,20,C.muted);
para(s,'为什么采用固定旅程','24 站给冒险一个清楚的终点\n节点模板帮助我安排压力与补给',167,789,430);
para(s,'为什么穿插多种节点','战斗检验构筑，事件提出取舍\n休息和商店提供调整机会',370,789,430);
impl(s,'Run 管进度和随机状态，EventRules 结算选项；随机遭遇增加变化');
note(s,'我用24站的旅程把一局组织成一个有限的过程。这里的24站是游戏进度，不对应真实农历日期。固定节点模板便于安排精英、休息、商店和最终Boss的顺序，遭遇与奖励再引入变化。这样我能同时考虑局部随机性和整体节奏。事件则让生命、货币和卡组进入同一个决策过程。代价是路线结构的变化有限，因此每一站的选择质量更重要。',['Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: Template / Random / Encounter','Assets/Sleigh/Scripts/Gameplay/SleighEvents.cs','Docs/十二生肖与年兽主题.md: 故事与旅程','Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalMapBackground.png']);

s=slide('安抚与伙伴：让玩家关心对手的状态',6,true);
txt(s,'我希望玩家同时观察血量、喜好与祟气',64,142,1130,56,34,C.gold,true);
const zo=[['rat','子鼠'],['coalgolem','丑牛'],['yeti','寅虎'],['penguin','卯兔']];
for(let i=0;i<zo.length;i++){await img(s,assets+'/'+zo[i][0]+'.png',98+i*295,224,200,178);txt(s,zo[i][1],114+i*295,419,210,45,28,C.cream,true);}
txt(s,'送对年礼 → 降低祟气 → 安定后不再伤害 → 招募伙伴',64,498,1150,55,29,C.gold,true);
txt(s,'关键取舍：继续造成伤害，还是停手保留结交的机会？',64,560,1130,48,28,C.cream);
impl(s,'喜好与安定状态参与招募判定；伙伴卡与图鉴保留结交结果',true);
note(s,'安抚机制是我最想强调的设计。春节主题里，送礼可以承担与对手建立关系的功能。玩家要观察喜好，用合适的年礼降低祟气；进入安定状态后，还需要避免继续伤害该目标，才有机会招募它。这让“能否打出更多伤害”之外，多了一项战术考虑。获得伙伴卡后，这次结交还会进入后续卡组。设计上需要留意的是：喜好是否容易发现，安定后不能继续伤害的条件是否清楚；这些是试玩要检查的问题。',['Docs/十二生肖与年兽主题.md: 十二生肖灵身','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: ChangePips / SoftHurt / Charm','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: AddFriend','Assets/Sleigh/Scripts/Data/SleighData.cs: Ledger','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);

s=slide('成长与送礼：让付出产生后续影响',7);
await img(s,root+'/Docs/Previews/围炉守岁.png',64,176,674,379);
txt(s,'围炉场景预览',64,568,674,31,20,C.muted);
para(s,'成长带来长期决策','升级、配件、遗物与药水\n让路线和奖励影响后续战斗',170,790,430);
para(s,'送礼也有真实成本','送出的年礼会离开卡组\n换来年味，并点亮村落灯火',366,790,430);
impl(s,'LeaveGift 移除卡牌并更新年味；年味档位参与战斗与首领结算');
note(s,'成长系统让前面的选择延续到后面的战斗。这里还有一个很适合春节主题的取舍：给村民留下年礼，会把那张牌从卡组移除，同时增加年味、点亮灯火。于是送礼既是故事行动，也会改变构筑；玩家需要判断这张牌是否值得留下，移除它又会怎样影响自己的卡组。年味的档位还参与能量、抽牌和Boss相关规则。这个机制并不保证每次送礼都是损失或收益，它的价值取决于当前卡组和资源。',['Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: Joy / LeaveGift / SpiritTier','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: StartTurn / Transform','Assets/Sleigh/Scripts/UI/SleighViews.cs: ShowLeaveGift相关逻辑','Docs/Previews/围炉守岁.png']);

s=slide('结局设计：回应玩家解决问题的方式',8,true);
txt(s,'我希望最后的结果能呼应旅途中学会的安抚与结伴',64,141,1130,58,32,C.gold,true);
para(s,'普通路线','击败年兽，守护村民迎春\n给正面战斗提供完整的结局',226,64,740,true);
para(s,'隐藏路线','安抚首阶段首领，揭露岁祟之源\n让伙伴关系参与最后的对抗',403,64,740,true);
await img(s,assets+'/santa.png',948,210,235,191);await img(s,assets+'/krampus.png',948,407,235,204);
txt(s,'设计取舍：保留不同的完成方式；不要求集齐十二生肖',64,566,1130,48,28,C.gold,true);
impl(s,'首领状态触发不同路线；已结交的伙伴参与隐藏路线助战',true);
note(s,'双结局把前面学到的机制带到终点。普通路线中，玩家击败年兽，村民平安迎春；隐藏路线中，通过安抚首阶段Boss揭露岁祟之源，伙伴也会参与助战。这样，安抚和结交就与最后的问题产生联系。同时，普通战斗路线仍然有完整结局，游戏没有要求集齐十二生肖才能通关。我要保留的是完成方式的选择，而不是额外加一项必须收集的任务。',['Docs/十二生肖与年兽主题.md: 年兽与结局','Assets/Sleigh/Scripts/Battle/SleighBattle.cs: Charm / SecretBoss / Transform','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);

s=slide('模块实现：让设计方便调整、反馈容易理解',9);
txt(s,'Unity 2022.3 LTS / C#，按职责组织功能',64,133,1130,45,28,C.red,true);
const rows=[
 ['Core / Gameplay / Battle','流程与规则分开，便于调整界面和战斗表现'],
 ['Data','JSON 定义和美术索引集中管理内容，方便迭代'],
 ['UI / Presentation','让动画对应结算；减少动态效果保留操作反馈'],
 ['Audio / Utilities','按场景调度音频，用对象池复用 UI 与音效声部'],
 ['存档与继续冒险','5 个存档位保存进度，让一次旅程可以分段完成']
];
for(let i=0;i<rows.length;i++){let y=222+i*77;txt(s,rows[i][0],64,y,365,48,26,C.red,true);txt(s,rows[i][1],448,y,768,57,27);}
note(s,'实现上，我关心的是设计能否被持续调整，以及玩家能否看懂反馈。Core组织屏幕流转，Run和EventRules处理冒险，Battle负责结算；UI和Presentation再表达这些结果。玩法定义、本地化和图集索引集中管理，方便调整内容。UI对象池和音效声部池复用对象，音频按场景调度。减少动态效果设置照顾不同的动态偏好。五个存档位用JSON和PlayerPrefs保存进度，让玩家可以分段完成旅程。代价是模块之间的状态和资源引用需要保持一致，所以项目保留了对应的规则与资源检查。',['Docs/ProjectStructure.md','Assets/Sleigh/Scripts/Core/GameRoot.cs','Assets/Sleigh/Scripts/Data/SleighData.cs','Assets/Sleigh/Scripts/Presentation/SleighPresentation.cs','Assets/Sleigh/Scripts/Audio/SleighAudio.cs','Assets/Sleigh/Scripts/Utilities/PixelPoolBucket.cs','Assets/Sleigh/Scripts/UI/GameRoot.SaveSlots.cs','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: Save / Load']);

s=slide('我希望大家重点体验什么',10,true);
txt(s,'演示时，我会重点展示一次“做选择”的过程',64,151,1130,58,34,C.gold,true);
para(s,'先看一场战斗','在有限喜气下选择出牌顺序\n再展示安抚后是否继续伤害的判断',246,64,830,true);
para(s,'再看一次送礼','把年礼留在卡组，还是送给村民？\n观察构筑变化与年味反馈',425,64,830,true);
await img(s,assets+'/alpha.png',990,249,218,233);
txt(s,'我想听到的反馈：选择是否看得懂，付出是否有意义',64,612,1130,58,29,C.gold,true);
note(s,'最后我想用实际游戏展示这些设计是否讲得通。先选角色和存档，带过开场与地图，进入一场战斗，展示喜气限制下的出牌，以及安抚后的停手判断。再展示一次给村民送礼，说明卡组与年味如何变化。我希望大家反馈两件事：能不能理解选择的后果，以及做出这些选择时是否感到有意义。这些是接下来可以通过试玩验证和继续打磨的方向，暂时不把它们当成已经证明的体验结论。谢谢大家。',['Assets/Sleigh/Scripts/Battle/SleighBattle.cs','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: LeaveGift','Assets/Sleigh/Scripts/UI/GameRoot.SaveSlots.cs','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);

await (await PresentationFile.exportPptx(P)).save(work+'/candidate.pptx');
for(let i=0;i<P.slides.items.length;i++){
 const png=await P.export({slide:P.slides.items[i],format:'png',scale:1});
 await fs.writeFile(work+'/slide-'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await png.arrayBuffer()));
}
await fs.writeFile(work+'/deck.json',JSON.stringify(P.toProto()));
console.log('EXPORTED',work+'/candidate.pptx');
