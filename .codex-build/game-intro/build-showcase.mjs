import fs from 'node:fs/promises';
import path from 'node:path';
import {pathToFileURL} from 'node:url';
const runtime='C:/Users/ivy/.cache/codex-runtimes/codex-primary-runtime/dependencies';
const {Presentation,PresentationFile}=await import(pathToFileURL(runtime+'/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs'));
const root='D:/workspace/SlayTheNian',work=root+'/.codex-build/game-intro/showcase',assets=root+'/.codex-build/game-intro/assets';
await fs.mkdir(work,{recursive:true});
const P=Presentation.create({slideSize:{width:1280,height:720}});
const dark='#291C2A',cream='#FFF0CD',gold='#E8B96A';
function txt(s,t,x,y,w,h,size,color=cream,bold=false){
 const z=s.shapes.add({geometry:'textbox',position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
 z.text=t;z.text.style={typeface:'Microsoft YaHei',fontSize:size,color,bold,autoFit:'none',wrap:'square',insets:0};return z;
}
async function img(s,file,x,y,w,h,fit='contain'){
 const b=await fs.readFile(file);s.images.add({blob:b.buffer.slice(b.byteOffset,b.byteOffset+b.byteLength),contentType:'image/png',alt:path.basename(file),position:{left:x,top:y,width:w,height:h},fit});
}
const bg=root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalBattleBackground.png';
const map=root+'/Assets/Sleigh/Resources/Sleigh/Art/Backgrounds/FestivalMapBackground.png';
function slide(){const s=P.slides.add();s.background.fill=dark;return s;}
function heading(s,title,sub){txt(s,title,72,77,1136,110,72,cream,true);txt(s,sub,77,203,1110,62,31,gold);}
function note(s,t,sources=[]){s.speakerNotes.text=t+'\n\n依据：'+sources.join('；')+'\n本页是模块过场页，配图来自项目现有素材；当前未嵌入视频。';}
let s=slide();await img(s,bg,0,160,1280,560,'cover');heading(s,'爆竹驱年','游戏实机展示');
note(s,'开场约10秒：大家好，今天我用几段实际画面带大家看看《爆竹驱年》。接下来从主菜单开始，依次看图鉴、设置和一段完整的冒险过程。\n播放方式：每个模块先展示过场页，再播放对应实机视频；也可以在正式放映前，把视频作为全屏内容插入对应模块之后。\n建议录制为16:9，保留游戏声音；以下各页备注列出镜头顺序。\n全程视频建议约4分钟，另加口头过渡，总分享约5分钟。',['Docs/十二生肖与年兽主题.md']);

s=slide();await img(s,bg,0,160,1280,560,'cover');heading(s,'主菜单','爆竹声中，迎春除岁');await img(s,assets+'/hero1.png',943,384,230,230);
note(s,'对应片段建议命名：01_主菜单.mp4\n建议时长：15秒。\n镜头：主菜单停留3秒，保留灯笼与氛围动画；鼠标依次经过开始、继续、图鉴或设置入口，最后打开图鉴。\n一句过渡：先从游戏的主菜单开始。\n剪辑：去掉启动等待，避免快速晃动鼠标。',['Assets/Sleigh/Scripts/Core/GameRoot.cs: ShowTitle','Assets/Sleigh/Scripts/Presentation/SpringFestivalMenu.cs']);

s=slide();heading(s,'图鉴','记下旅途中每一次相遇');
for(const [i,name]of ['rat','coalgolem','yeti','penguin'].entries())await img(s,assets+'/'+name+'.png',86+i*296,370,212,218);
note(s,'对应片段建议命名：02_图鉴.mp4\n建议时长：20秒。\n镜头：打开图鉴；选一个已经遇见的生肖条目，停留展示立绘和资料；切换另一条目，再演示一次翻页。\n一句过渡：这些是旅途中会遇见的伙伴与对手。\n录制准备：使用已有图鉴记录的演示存档；未发现内容会按实际状态显示，不需要修改玩家记录。',['Assets/Sleigh/Scripts/UI/SleighViews.cs: OpenLedger','Assets/Sleigh/Resources/Sleigh/Art/Festival/FestivalEnemiesAtlas.png']);

s=slide();heading(s,'设置面板','按自己的习惯开始冒险');await img(s,assets+'/coin.png',874,360,170,170);await img(s,assets+'/heart.png',1048,430,147,164);
note(s,'对应片段建议命名：03_设置面板.mp4\n建议时长：20秒。\n镜头：打开设置，停留2秒显示完整面板；调节一次音乐或音效音量；演示一个动态或回合相关开关，随后返回。\n一句过渡：这里可以调整声音和游玩习惯。\n录制准备：记住原有设置，演示结束后恢复；不需要逐条讲解开关。',['Assets/Sleigh/Scripts/Core/GameRoot.cs: ShowSettings']);

s=slide();heading(s,'角色与存档','选择这次旅程的起点');await img(s,assets+'/hero2.png',913,351,259,259);
note(s,'对应片段建议命名：04_角色与存档.mp4\n建议时长：25秒。\n镜头：进入角色选择，切换一次角色并停留；进入存档选择，选择准备好的演示槽位；进入冒险。\n一句过渡：选择角色，开始这次守岁旅程。\n录制准备：使用空槽或独立演示存档；避免覆盖原有游戏进度。',['Assets/Sleigh/Scripts/Core/GameRoot.cs: ShowCharacters / BeginRun','Assets/Sleigh/Scripts/UI/GameRoot.SaveSlots.cs']);

s=slide();await img(s,map,0,180,1280,540,'cover');heading(s,'剧情与地图','从岁安镇出发');
note(s,'对应片段建议命名：05_剧情与地图.mp4\n建议时长：30秒。\n镜头：保留一幕开场的主要画面和翻页动作，切到地图；拖动一段路线，停留在节点提示，选择一个战斗节点。\n一句过渡：故事从岁安镇开始，旅程也从这里展开。\n剪辑：剧情只保留一小段，不用把三幕文字全部播完。',['Assets/Sleigh/Scripts/Presentation/FestivalStoryView.cs','Assets/Sleigh/Scripts/UI/SleighViews.cs: ShowMap']);

s=slide();await img(s,bg,0,180,1280,540,'cover');heading(s,'战斗场景','年礼出手，守岁驱祟');await img(s,assets+'/alpha.png',947,350,246,262);
note(s,'对应片段建议命名：06_战斗场景.mp4\n建议时长：60至80秒，本次分享的重点片段。\n镜头：战斗全景停留2秒；悬停一张牌；展示一次选牌与选目标、一次拖拽出牌；播放敌人回合；若条件合适，再演示专属能力或安抚招募；以胜利或回合结束收尾。\n一句过渡：接下来看一段实际战斗。\n剪辑：保留动作、数值和音效之间的连续性；删去长时间思考和等待，不必解释每一张牌。',['Assets/Sleigh/Scripts/UI/SleighBattleView.cs','Assets/Sleigh/Scripts/Battle/SleighBattle.cs']);

s=slide();await img(s,root+'/Docs/Previews/围炉守岁.png',0,290,1280,430,'cover');heading(s,'年货铺与围炉','为下一程做准备');
note(s,'对应片段建议命名：07_年货铺与围炉.mp4\n建议时长：25秒，可由两小段画面剪接。\n镜头：年货铺全景，查看一件物品并购买一次；切到围炉场景，完成一次休息或年礼升级；返回地图。\n一句过渡：战斗之间，也会经过年货铺和围炉歇脚处。\n录制准备：使用已到达相应节点的演示存档，不用在片段里播放赶路过程。',['Assets/Sleigh/Scripts/UI/SleighViews.cs: ShowShop / ShowRest','Docs/Previews/围炉守岁.png']);

s=slide();heading(s,'奖励与送礼','把年礼留给等待的人');await img(s,assets+'/sock.png',878,343,164,164);await img(s,assets+'/mitten.png',1040,449,154,154);await img(s,assets+'/snowball.png',740,482,146,146);
note(s,'对应片段建议命名：08_奖励与送礼.mp4\n建议时长：25秒。\n镜头：战斗奖励界面停留；选择一项奖励；进入送礼界面，查看愿望并送出一份年礼；展示结果和地图灯火变化。\n一句过渡：拿到奖励之后，还能把一份年礼留给村民。\n剪辑：把操作和结果接在一起，让画面说明过程。',['Assets/Sleigh/Scripts/UI/SleighViews.cs: ShowReward / ShowLeave','Assets/Sleigh/Scripts/Gameplay/SleighRun.cs: LeaveGift']);

s=slide();await img(s,bg,0,180,1280,540,'cover');heading(s,'结束画面','灯火仍在，新春将至');await img(s,assets+'/santa.png',909,357,284,231);
note(s,'对应片段建议命名：09_结束画面.mp4\n建议时长：15秒；若希望保留结局悬念，可以省略这段视频，直接以本页结束。\n镜头：展示一段首领或胜利演出；停留结束画面；最后返回主菜单，或定格在结尾。\n结束语：这就是《爆竹驱年》的实机展示，谢谢大家。\n剪辑：可保留结尾音乐，让最后一帧多停留2至3秒。',['Assets/Sleigh/Scripts/UI/SleighViews.cs: ShowEnding']);

await(await PresentationFile.exportPptx(P)).save(work+'/candidate.pptx');
for(let i=0;i<P.slides.items.length;i++){
const png=await P.export({slide:P.slides.items[i],format:'png',scale:1});await fs.writeFile(work+'/slide-'+String(i+1).padStart(2,'0')+'.png',new Uint8Array(await png.arrayBuffer()));
}
console.log('EXPORTED',work+'/candidate.pptx');
