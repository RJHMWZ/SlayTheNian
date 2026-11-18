"""Original deterministic paper-cut/cel sprites. Preserves existing texture GUIDs and pivots.
Pillow is used for new procedural artwork, never for editing AI-generated art.
"""
from PIL import Image,ImageDraw,ImageFont
import pathlib,json,math,hashlib,random
ROOT=pathlib.Path(__file__).resolve().parents[2];RES=ROOT/'Assets/Sleigh/Resources'
INDEX=RES/'Sleigh/ArtIndex.json';bundle=json.loads(INDEX.read_text());frames=[f for f in bundle['frames'] if f['type']!='EventArt'];bundle['frames']=frames
DEFS=json.loads((RES/'Sleigh/Definitions.json').read_text(encoding='utf-8'))
# Supply dedicated frames where the reference used a generic attack/skill fallback.
for g in DEFS['gifts']:
 if g['type']!='friend' and not any(f['type']=='Gift' and f['anim']==g['id'] for f in frames):
  frames.append(dict(type='Gift',anim=g['id'],frame=0,path='Sleigh/Art/Gift__'+g['id']+'__0',width=66,height=88,pivotX=0,pivotY=0,eyes=[]))
INK='#211b24';RED='#ba3c35';BRIGHT='#ef6743';GOLD='#e6ba69';CREAM='#f6e7bc';JADE='#69a88c';TEAL='#324f50';PAPER='#dec89f'
class Paint:
 def __init__(self,w=128,h=128):self.im=Image.new('RGBA',(w,h));self.d=ImageDraw.Draw(self.im)
 def poly(self,p,c,edge=INK,width=3):self.d.polygon(p,fill=c);self.d.line(p+[p[0]],fill=edge,width=width,joint='curve') if edge else None
 def rect(self,b,c,edge=None,width=2):self.d.rectangle(b,fill=c,outline=edge,width=width)
 def oval(self,b,c,edge=INK,width=3):self.d.ellipse(b,fill=c,outline=edge,width=width)
 def line(self,p,c=GOLD,width=3):self.d.line(p,fill=c,width=width,joint='curve')
 def arc(self,b,a,z,c=GOLD,width=3):self.d.arc(b,a,z,fill=c,width=width)
 def paste(self,im,b):
  im=im.resize((int(b[2]),int(b[3])),Image.Resampling.LANCZOS);self.im.alpha_composite(im,(int(b[0]),int(b[1])))
def lantern(p,x=64,y=58,s=1,c=RED):
 def box(a,b,d,e):return (x+a*s,y+b*s,x+d*s,y+e*s)
 p.line([(x,y-39*s),(x,y-27*s)],GOLD,max(1,int(s*3)))
 p.oval(box(-23,-26,23,26),c)
 p.oval(box(-12,-25,12,25),None,GOLD,2);p.line([(x,y-24*s),(x,y+24*s)],GOLD,2)
 p.rect(box(-14,-29,14,-24),GOLD);p.rect(box(-14,24,14,29),GOLD)
 p.line([(x,y+28*s),(x,y+44*s)],BRIGHT,max(2,int(4*s)))
def sword(p):
 p.poly([(32,88),(81,20),(91,14),(91,29),(45,99)],CREAM)
 p.line([(40,88),(86,24)],GOLD,3);p.line([(30,77),(57,97)],RED,9)
 p.line([(22,110),(39,88)],TEAL,9);p.line([(25,113),(13,123)],RED,4)
def seal(p,kind=0,color=RED):
 p.rect((36,17,93,104),CREAM,INK,3);p.rect((39,20,90,34),color)
 p.line([(47,47),(80,47),(65,57),(76,66),(49,66),(65,76),(52,88),(79,88)],color,5)
 if kind%3==1:p.line([(42,42),(87,94)],GOLD,3)
 if kind%3==2:p.oval((50,42,78,68),None,color,4)
 p.rect((60,97,69,113),RED)
def coin(p,x=64,y=64,r=28):
 p.oval((x-r,y-r,x+r,y+r),GOLD);p.oval((x-r+5,y-r+5,x+r-5,y+r-5),None,'#a37535',2)
 p.rect((x-r*.25,y-r*.25,x+r*.25,y+r*.25),TEAL,INK,2)
def cracker(p,n=3):
 p.line([(39,11),(69,111)],GOLD,3)
 for i in range(n):
  x=29+(i%2)*37;y=18+i*24
  p.poly([(x,y+6),(x+12,y),(x+27,y+24),(x+15,y+31)],RED)
  p.line([(x+3,y+7),(x+15,y+2)],GOLD,4);p.line([(x+14,y+27),(x+25,y+22)],GOLD,4)
 p.line([(69,111),(81,108),(88,119)],BRIGHT,3)
def flame(p,x=64,y=63,s=1,c=BRIGHT):
 p.poly([(x-21*s,y+22*s),(x-25*s,y),(x-8*s,y-13*s),(x,y-39*s),(x+12*s,y-9*s),(x+25*s,y+10*s),(x+13*s,y+26*s)],c)
 p.poly([(x-7*s,y+17*s),(x-9*s,y+4*s),(x+3*s,y-12*s),(x+13*s,y+15*s)],GOLD,None)
def drum(p):
 p.poly([(30,51),(97,51),(90,96),(38,96)],RED);p.oval((25,29,103,68),CREAM)
 p.oval((34,36,94,61),None,GOLD,3);p.line([(21,15),(58,50)],INK,7);p.line([(111,14),(79,50)],INK,7)
 p.line([(30,95),(21,117)],GOLD,5);p.line([(95,95),(106,117)],GOLD,5)
def gate(p):
 p.rect((25,33,103,112),RED,INK,4);p.rect((36,44,92,112),TEAL)
 p.poly([(15,41),(29,27),(43,26),(64,13),(85,26),(101,27),(115,41)],INK,GOLD,3)
 p.line([(64,45),(64,111)],GOLD,3)
 for x in (47,78):p.oval((x-3,74,x+3,80),GOLD,None)
def bowl(p):
 p.poly([(21,57),(108,57),(96,91),(79,104),(49,104),(32,90)],RED)
 p.oval((21,44,108,70),CREAM);p.arc((30,49,100,84),0,175,GOLD,3)
 for x in (42,63,84):p.arc((x-6,45,x+6,63),180,355,BRIGHT,2)
 p.line([(43,35),(37,26),(44,17)],PAPER,3);p.line([(78,35),(73,24),(80,11)],PAPER,3)
def scroll(p):
 p.rect((24,31,105,100),PAPER,INK,3);p.rect((20,24,29,106),RED,INK);p.rect((100,24,109,106),RED,INK)
 for y in (44,58,72,86):p.line([(39,y),(78+(y%9),y)],RED,3)
def mask(p,c=RED,horns=True):
 if horns:
  p.poly([(32,40),(13,13),(22,52)],GOLD);p.poly([(96,40),(115,13),(107,52)],GOLD)
 p.poly([(24,37),(44,19),(83,19),(108,39),(101,87),(83,111),(45,111),(27,88)],c)
 p.oval((28,40,60,69),CREAM);p.oval((69,40,101,69),CREAM)
 p.oval((40,48,52,64),INK,None);p.oval((78,48,90,64),INK,None)
 p.poly([(55,65),(72,65),(79,79),(64,85),(49,79)],GOLD)
 p.arc((39,71,91,101),0,180,INK,4);p.line([(42,40),(56,35)],GOLD,5);p.line([(72,35),(88,40)],GOLD,5)
def motif(id):
 p=Paint();seed=int(hashlib.sha256(id.encode()).hexdigest()[:8],16);rng=random.Random(seed)
 if id=='doublewrap':
  for x in (2,60):q=Paint();gate(q);p.paste(q.im,(x,20,66,95))
  p.line([(64,32),(64,112)],GOLD,3)
 elif id=='globe':
  p.oval((13,13,115,115),GOLD);p.oval((24,24,104,104),TEAL);p.oval((34,34,94,94),JADE)
  p.line([(40,83),(86,38)],CREAM,4);p.line([(57,87),(90,54)],CREAM,2)
  for k in range(8):a=k*math.tau/8;p.oval((61+43*math.cos(a),61+43*math.sin(a),67+43*math.cos(a),67+43*math.sin(a)),RED,None)
 elif id=='fireplace':
  p.rect((9,26,119,119),'#805043',INK,3);p.rect((29,47,99,112),INK,GOLD,3)
  for y in (34,44,112):p.line([(12,y),(116,y)],GOLD,2)
  flame(p,64,81,.7);p.line([(36,111),(92,106)],'#9a6c44',6)
 elif id=='checktwice':
  q=Paint();seal(q);p.paste(q.im,(3,3,83,112));p.paste(q.im,(43,15,83,112));p.line([(35,61),(87,75)],GOLD,4)
 elif id=='elfhelp':
  p.poly([(37,40),(90,40),(108,99),(81,88),(65,111),(49,89),(19,99)],CREAM)
  p.oval((40,16,88,58),PAPER);p.oval((51,33,56,40),INK,None);p.oval((72,33,77,40),INK,None)
  p.poly([(48,17),(64,2),(82,17)],RED);q=Paint();seal(q);p.paste(q.im,(7,63,46,61))
 elif id=='spiked':
  p.poly([(29,40),(99,40),(88,85),(72,96),(55,96),(39,85)],GOLD);p.oval((28,29,100,53),CREAM);p.oval((36,33,93,48),BRIGHT)
  p.line([(64,96),(64,115)],GOLD,6);p.line([(43,117),(86,117)],GOLD,5);flame(p,69,20,.38)
 elif id=='tinselarmor':
  p.poly([(38,21),(52,14),(78,14),(92,21),(114,46),(96,61),(88,50),(94,116),(31,116),(40,50),(29,61),(12,46)],RED)
  for y in range(42,112,13):
   for x in range(43,86,12):p.poly([(x,y),(x+8,y),(x+8,y+7),(x+4,y+11),(x,y+7)],GOLD,INK,1)
  p.line([(40,29),(64,43),(88,29)],GOLD,4)
 elif id=='hollywreath':
  p.oval((19,19,109,109),None,JADE,13)
  for k in range(9):a=k*math.tau/9;x=64+46*math.cos(a);y=64+46*math.sin(a);p.poly([(x-4,y-4),(x+12*math.cos(a),y+12*math.sin(a)),(x+4,y+5)],GOLD)
  p.line([(47,103),(64,111),(83,102)],RED,8)
 elif id=='candles':
  for x,y in [(31,78),(65,60),(98,78)]:
   p.rect((x-5,y,x+5,118),RED,INK,2);p.oval((x-14,y-2,x+14,y+8),GOLD);flame(p,x,y-16,.36)
 elif id=='rhythm':
  scroll(p);p.oval((11,80,66,118),TEAL,GOLD);p.oval((22,87,56,107),INK)
  p.line([(89,13),(56,90)],'#946841',8);p.poly([(52,84),(64,90),(49,108)],INK,GOLD,1)
 elif id=='starfall':
  for x,y,s in [(27,25,.45),(77,16,.35),(103,57,.4),(57,78,.6)]:lantern(p,x,y,s)
  p.line([(9,70),(22,89)],GOLD,2);p.line([(85,93),(98,115)],GOLD,2)
 elif id=='birch':
  p.poly([(16,101),(38,83),(49,98),(60,80),(77,101),(107,112),(78,125),(33,119)],TEAL)
  sword(p);p.line([(17,70),(107,103)],GOLD,4)
 elif id=='bigbow':
  gate(p);p.oval((42,46,88,95),GOLD);p.poly([(54,56),(74,56),(69,74),(78,84),(50,84),(59,74)],RED)
  p.line([(15,19),(6,42)],GOLD,4);p.line([(113,19),(122,42)],GOLD,4)
 elif id=='miracle':
  lantern(p,64,63,.9);flame(p,65,53,.65)
  for k in range(8):a=k*math.tau/8;p.line([(64+42*math.cos(a),64+42*math.sin(a)),(64+59*math.cos(a),64+59*math.sin(a))],GOLD,3)
 elif id=='milkcookies':
  bowl(p)
  for x,y in [(42,55),(65,53),(87,55)]:p.oval((x-9,y-8,x+9,y+8),CREAM,BRIGHT,1)
  p.line([(21,94),(10,103)],GOLD,4);p.line([(103,94),(119,103)],GOLD,4)
 elif id=='guidingstar':
  p.line([(64,4),(64,120)],GOLD,4);lantern(p,64,49,.72)
  p.poly([(10,116),(37,92),(47,105),(63,88),(81,105),(95,97),(118,116)],TEAL,GOLD,2)
 elif id=='choir':
  for x,y,c in [(0,28,JADE),(67,28,GOLD),(34,0,RED)]:q=Paint();mask(q,c);p.paste(q.im,(x,y,65,90))
  p.line([(9,124),(117,124)],GOLD,3)
 elif id=='coal':
  p.poly([(12,82),(32,45),(61,30),(98,48),(117,91),(85,117),(39,110)],'#3f3443',GOLD,2)
  p.line([(32,45),(50,71),(61,30),(74,78),(98,48)],'#766276',4);p.line([(50,71),(39,110)],'#766276',3)
 elif id=='soot':
  for i in range(14):x=rng.randrange(16,97);y=rng.randrange(18,95);p.oval((x,y,x+22,y+20),'#655563',None)
  for i in range(16):x=rng.randrange(11,118);y=rng.randrange(90,120);p.rect((x,y,x+2,y+3),GOLD)
 elif id=='naughty':
  seal(p,2,'#70456f');p.line([(35,17),(94,104)],INK,7);p.line([(91,18),(37,104)],'#714c7e',5)
 elif id in ['sock','baubles','snowball','midnight','crumbs','st_soggy','node_battle','cracker','spring','st_candles','yulelog']:cracker(p,{'sock':1,'snowball':2,'midnight':4,'crumbs':2}.get(id,3))
 elif id in ['canejab','birch','sword','nutjaw','intent_attack']:sword(p)
 elif id in ['mitten','snowfort','bigbow','doublewrap','bellybump','warmsocks','shield','intent_defend','node_boss']:gate(p)
 elif id in ['offkey','bellslam','choir','squeaker','st_choir','st_carol','churchbell','jingle','rhythm']:drum(p)
 elif id in ['milkcookies','cocoasip','fireplace','hotcocoa','milk','wishbone','cocoapot','cookietin','musicbox','st_cocoa','kiss']:bowl(p)
 elif id in ['stuffer','wrapspree','checktwice','globe','frostbreath','icicle','flurry','soot','naughty','coal','wishlist','ledger','gifttag','elfcoupon','workbench','intent_debuff']:seal(p,seed%3,JADE if id in ['frostbreath','icicle','flurry'] else RED)
 elif id in ['nutchomp','gingercharm','hollywreath','intent_unknown','treeangel']:mask(p,RED if id!='treeangel' else JADE)
 elif id in ['coin','reindeerfeed','snowglobe','pocketwatch','st_brittle']:
  coin(p)
  if id=='reindeerfeed':p.poly([(17,62),(34,79),(64,88),(94,79),(112,62),(99,99),(30,99)],GOLD)
  if id=='pocketwatch':p.line([(64,64),(64,45)],INK,4);p.line([(64,64),(78,70)],INK,3)
 elif id in ['starfall','guidingstar','rednose','treestar','tree','orb','orb_empty','cost','windup','candles','st_star','st_jolly','miracle','eggshot','battery','peppermint','elixir']:lantern(p,c=TEAL if id=='orb_empty' else RED)
 elif id in ['sack','toolbelt','sackpatch']:
  p.poly([(30,40),(98,40),(109,104),(89,119),(37,119),(21,104)],TEAL);p.line([(31,47),(98,47)],RED,9);p.oval((45,61,83,98),GOLD);p.line([(41,36),(54,16),(76,16),(87,37)],GOLD,5)
 elif id in ['candybowl','stocking','gift']:
  p.rect((24,20,104,112),RED,INK,4);p.poly([(25,21),(64,57),(103,21)],BRIGHT,GOLD,3);coin(p,64,71,15)
 elif id in ['heart','HeartEye']:
  p.poly([(64,106),(17,58),(16,33),(33,18),(52,22),(64,35),(77,22),(96,18),(113,33),(112,58)],RED);p.line([(28,43),(31,33),(45,33)],CREAM,4)
 elif id in ['scarf','tinselwhip','tinselarmor','tinselgarland','ribbon','st_ribbon','st_tinsel']:
  p.line([(27,18),(93,35),(39,64),(92,83),(30,114)],RED,15);p.line([(30,18),(94,34),(39,64),(92,83),(30,114)],GOLD,2)
 elif id in ['jinglebell','reindeerbell','bell']:
  p.poly([(34,81),(40,43),(64,28),(88,43),(95,81)],GOLD);p.oval((30,72,99,94),GOLD);p.oval((54,85,74,105),BRIGHT);p.line([(64,25),(64,9)],RED,5)
 elif id in ['node_shop']:scroll(p);lantern(p,99,34,.35)
 elif id in ['node_event','node_treasure']:scroll(p);p.oval((50,45,77,72),RED);p.line([(64,72),(64,91)],RED,5)
 elif id in ['node_rest','st_holly','hollysprig']:flame(p);p.line([(20,111),(106,99)],TEAL,7)
 elif id in ['arrow','new_arrow','pin','check','intent_escape','path_dot','node']:
  p.poly([(26,57),(61,57),(61,25),(109,64),(61,104),(61,74),(26,74)],GOLD)
 elif id in ['st_snowfall','snowfall','blizzard','frostvial','st_chilled','st_frostbite']:
  for a in range(0,360,60):
   r=math.radians(a);p.line([(64,64),(64+44*math.cos(r),64+44*math.sin(r))],JADE,7)
  seal(p,1,TEAL)
 elif id in ['elfhelp','elfdust']:scroll(p);flame(p,64,62,.65)
 elif id in ['spiked','eggnog','cider','ginger','punch','st_might','st_nimble','st_workshop','st_drawnext','intent_buff']:seal(p,seed%3,RED);flame(p,80,84,.45)
 elif id in ['coal','coalscuttle']:p.oval((22,50,109,110),TEAL);flame(p,64,41,.7)
 elif id in ['snowshovel','iceskates','lostmitten']:sword(p);p.line([(24,88),(76,110),(113,83)],JADE,8)
 else:
  # Other consumables remain individually colored ceramic flasks with different wax seals.
  colors=[RED,JADE,BRIGHT,TEAL,'#9d76b8'];c=colors[seed%len(colors)]
  p.poly([(49,23),(80,23),(80,48),(99,64),(101,97),(86,112),(40,112),(26,97),(29,65),(49,48)],c)
  p.rect((45,14,83,29),GOLD,INK,3);coin(p,64,79,18)
  p.line([(38,70),(38,92)],CREAM,3)
 # Unique embossed small insignia; no two objects are merely duplicate files.
 for j in range(1+seed%4):p.rect((9+j*6,118,12+j*6,121),GOLD)
 return p.im
def enemy(id):
 p=Paint();c=RED
 if id in ['santa','krampus','alpha','penguin','deer']:
  colors={'santa':RED,'krampus':'#493242','alpha':JADE,'penguin':GOLD,'deer':TEAL};c=colors[id]
  # Four paws, long tail, protective mane and curling horns distinguish a Nian from a deer.
  p.line([(75,68),(111,54),(117,29),(104,21)],c,11)
  p.oval((33,47,107,96),c);p.poly([(41,76),(40,116),(24,116),(28,87)],c);p.poly([(91,80),(104,117),(87,117),(77,90)],c)
  p.poly([(34,18),(59,22),(76,47),(68,82),(38,94),(8,72),(7,41)],BRIGHT if id=='santa' else c)
  head=Paint();mask(head,c);p.paste(head.im,(0,16,74,78))
  p.line([(12,51),(4,38),(12,13),(27,11)],GOLD,5);p.line([(53,41),(66,25),(61,7)],GOLD,5)
  p.line([(26,88),(58,86)],RED,6);p.line([(43,88),(50,110),(62,103)],RED,5)
  for x in (57,72,87):p.arc((x,50,x+13,66),0,180,GOLD,2)
  if id=='krampus':flame(p,89,50,.6);p.line([(33,102),(77,110)],BRIGHT,4)
 elif id in ['naughtylist','naughtylistbig']:
  p.poly([(18,23),(32,12),(97,12),(111,23),(108,110),(91,121),(36,121),(20,111)],TEAL)
  p.poly([(24,35),(7,21),(9,5),(41,33)],RED);p.poly([(99,35),(122,17),(118,3),(88,32)],RED)
  p.rect((32,32,98,109),INK,GOLD,4)
  p.oval((38,49,61,64),BRIGHT);p.oval((72,49,95,64),BRIGHT);p.poly([(43,86),(88,86),(79,103),(51,103)],CREAM)
  for x in (39,56,79,94):p.line([(x,112),(x-8,126)],GOLD,3)
 elif id in ['ornament','lights','jack']:
  if id=='ornament':lantern(p,64,55,1.1)
  elif id=='lights':flame(p,64,51,1.1);p.oval((21,86,107,107),RED)
  else:cracker(p,4);p.poly([(32,85),(91,85),(97,116),(25,116)],GOLD)
  p.oval((43,44,55,57),CREAM);p.oval((72,44,84,57),CREAM);p.line([(51,71),(76,71)],INK,5)
 elif id in ['nutgrunt','toysoldier','general','rogueelf','witch','gingerling','snowman','jackfrost','krampimp']:
  c={'nutgrunt':PAPER,'toysoldier':JADE,'general':RED,'rogueelf':TEAL,'witch':'#8a5d8d','gingerling':'#b7783f','snowman':'#afcfc8','jackfrost':JADE,'krampimp':'#6b4659'}[id]
  p.poly([(44,71),(36,114),(55,114),(65,89),(77,114),(96,114),(85,71)],INK)
  p.poly([(38,52),(89,52),(105,85),(91,94),(81,72),(79,101),(44,101),(46,72),(32,94),(19,85)],c)
  p.oval((36,15,91,67),c);p.line([(45,43),(55,40)],INK,4);p.line([(71,40),(81,43)],INK,4)
  p.poly([(57,56),(69,56),(63,63)],BRIGHT);p.line([(41,78),(85,78)],GOLD,7)
  if id in ['general','nutgrunt','toysoldier']:
   p.poly([(32,24),(39,10),(62,3),(89,12),(96,24)],INK,GOLD,3);p.line([(107,24),(107,122)],GOLD,5)
   for y in (62,70,88):p.line([(47,y),(80,y)],GOLD,2)
   if id=='general':p.line([(48,11),(38,0)],RED,6)
  elif id=='witch':p.poly([(22,29),(40,16),(60,0),(87,17),(108,29)],INK,GOLD);p.line([(110,51),(110,121)],GOLD,4)
  elif id=='rogueelf':p.poly([(30,44),(37,15),(64,4),(93,19),(98,47),(81,34),(47,34)],TEAL);p.line([(48,54),(82,54)],RED,7)
  elif id in ['snowman','jackfrost']:p.poly([(32,20),(47,3),(53,19),(70,1),(85,22)],CREAM);p.line([(40,69),(90,69),(100,101)],RED,6)
  elif id=='gingerling':p.rect((55,26,71,38),RED);p.oval((56,76,69,90),GOLD)
  elif id=='krampimp':p.poly([(35,27),(25,4),(51,20)],GOLD);p.poly([(79,20),(103,4),(92,31)],GOLD)
 elif id in ['yeti','coalgolem','coalbaron','grumpkin']:
  c={'yeti':'#abc4c4','coalgolem':'#626268','coalbaron':'#73624d','grumpkin':GOLD}[id]
  p.poly([(32,53),(19,60),(4,93),(19,105),(32,87),(34,119),(54,119),(63,95),(75,119),(98,119),(98,84),(114,101),(125,91),(108,56),(92,48)],c)
  head=Paint();mask(head,c);p.paste(head.im,(27,6,77,75))
  if id=='coalbaron':p.poly([(30,26),(44,8),(89,8),(101,26)],INK,GOLD);coin(p,66,91,18)
  elif id=='grumpkin':coin(p,66,91,18);p.line([(94,49),(116,36),(119,17)],c,8)
  elif id=='yeti':p.line([(34,84),(48,89),(60,80),(77,90),(91,81)],CREAM,6)
  else:flame(p,64,82,.55)
 elif id in ['viper','fruitcake','creeper']:
  c=RED if id=='viper' else GOLD if id=='fruitcake' else JADE
  p.line([(96,116),(38,111),(24,94),(35,80),(83,78),(100,59),(86,34)],INK,23);p.line([(96,116),(38,111),(24,94),(35,80),(83,78),(100,59),(86,34)],c,17)
  p.poly([(56,29),(74,13),(103,17),(117,35),(101,56),(70,50)],c);p.oval((83,25,94,36),CREAM);p.line([(108,42),(122,48)],RED,3)
  for x,y in [(37,99),(62,84),(88,70)]:p.line([(x,y),(x+8,y-5)],GOLD,3)
  if id=='creeper':
   for x,y in [(33,88),(76,76),(90,55)]:p.poly([(x,y),(x-18,y-24),(x+7,y-13)],JADE)
 elif id in ['angel','bat']:
  c=CREAM if id=='angel' else TEAL
  p.poly([(62,55),(31,24),(4,15),(12,78),(32,67),(46,90),(65,72),(83,90),(99,67),(119,78),(125,15),(95,24)],c)
  p.oval((44,44,86,95),RED);p.poly([(46,47),(46,28),(60,43)],GOLD);p.poly([(71,43),(86,28),(85,50)],GOLD)
  p.line([(4,15),(32,67),(62,55),(99,67),(125,15)],GOLD,3);p.oval((51,58,60,67),CREAM);p.oval((72,58,81,67),CREAM)
 elif id=='rat':
  p.line([(80,93),(113,94),(120,77),(108,69)],RED,6);p.oval((30,55,100,115),TEAL);p.oval((14,57,57,94),TEAL)
  p.oval((24,36,52,68),RED);p.oval((33,65,41,73),GOLD);p.poly([(4,86),(26,79),(26,93)],PAPER)
 elif id=='slime':
  p.poly([(10,112),(21,78),(43,70),(53,40),(80,44),(90,77),(109,83),(120,113)],CREAM)
  p.oval((39,78,49,91),INK);p.oval((76,78,86,91),INK);p.arc((50,90,77,106),0,180,RED,4);p.line([(31,111),(95,111)],GOLD,5)
 else:return enemy('gingerling')
 return p.im
def hero(id):
 p=Paint();c={'idle':RED,'tinsel_idle':JADE,'rudolph_idle':TEAL}[id]
 # Layered hanfu/padded coats, tied hair, boots, belt fittings and profession-specific equipment.
 p.line([(42,89),(41,113)],'#40343c',13);p.line([(70,87),(78,113)],'#40343c',13)
 p.poly([(33,108),(47,108),(53,120),(31,120)],INK,GOLD,1)
 p.poly([(71,107),(83,109),(94,120),(70,120)],INK,GOLD,1)
 if id=='idle':
  p.poly([(38,36),(26,58),(23,91),(39,80),(52,49)],INK)
  p.line([(28,61),(18,89),(34,96)],BRIGHT,5)
 elif id=='rudolph_idle':
  p.poly([(30,41),(20,53),(15,100),(33,94),(49,58)],'#213e42')
 p.poly([(41,43),(67,43),(83,60),(86,96),(64,107),(33,99),(33,66)],c)
 p.poly([(36,71),(49,77),(45,95),(34,99)],'#294740' if id=='tinsel_idle' else '#742c32',None)
 p.poly([(55,45),(69,49),(52,69),(38,62)],PAPER,INK,1)
 p.line([(62,47),(53,67),(55,95)],GOLD,2)
 p.line([(36,76),(77,77)],INK,7);p.line([(38,75),(76,76)],GOLD,2);p.oval((53,72,63,81),GOLD,INK,1)
 for y in (86,94):p.arc((58,y-5,76,y+4),0,170,GOLD,1)
 # Back arm and bent leading arm make the idle pose directional.
 p.poly([(37,48),(25,52),(21,76),(33,84),(39,73)],c)
 p.oval((28,75,40,86),PAPER,INK,1)
 p.poly([(68,50),(80,53),(88,68),(103,64),(105,74),(84,81),(71,67)],c)
 p.oval((97,62,110,73),PAPER,INK,1)
 # Ear, jaw and offset eyes create a three-quarter portrait.
 p.oval((45,13,76,49),PAPER,INK,2);p.oval((41,29,49,38),PAPER,INK,1)
 p.poly([(43,30),(40,19),(47,9),(62,5),(77,13),(80,29),(70,17),(62,25),(50,20)],INK)
 p.line([(49,32),(56,31)],INK,2);p.line([(67,30),(74,29)],INK,2)
 p.oval((52,32,55,36),INK,None);p.oval((69,31,72,35),INK,None)
 p.line([(63,35),(65,39),(62,40)],'#b88765',1);p.arc((58,38,72,45),10,140,RED,1)
 p.line([(47,27),(55,27)],'#53404a',1);p.line([(67,25),(73,26)],'#53404a',1)
 if id=='tinsel_idle':
  p.oval((34,3,54,20),INK);p.line([(36,10),(56,13)],GOLD,3)
  p.poly([(42,79),(68,79),(76,102),(42,100)],'#865b43',INK,1)
  for x in (47,53,60):p.line([(x,87),(x-3,96)],GOLD,2)
  p.line([(105,49),(104,83)],GOLD,3);p.poly([(101,47),(108,44),(106,33)],INK)
  q=Paint();seal(q);p.paste(q.im,(7,77,26,35))
 else:
  p.line([(106,13),(106,119)],'#916848',4)
  if id=='rudolph_idle':p.poly([(102,19),(106,2),(111,19)],CREAM,INK,1)
  lantern(p,103,48,.35)
  p.line([(37,45),(57,47),(66,51)],BRIGHT,6)
  p.poly([(37,45),(28,60),(18,63),(26,47)],RED,GOLD,1)
  if id=='idle':p.oval((48,8,55,14),RED,GOLD,1)
 return p.im
def background(kind,w=640,h=360):
 p=Paint(w,h);d=p.d
 for y in range(h):
  t=y/h;d.line((0,y,w,y),fill=(int(21+22*t),int(24+10*t),int(32+4*t),255))
 rng=random.Random(sum(map(ord,kind)))
 # Layered paper-cut mountains and slate street.
 for layer,col in enumerate(['#354247','#2d383c','#242e32']):
  pts=[(0,h)]+[(x,h*.46+math.sin(x*.017+layer)*h*.09+layer*18-rng.randrange(20)) for x in range(0,w+32,32)]+[(w,h)]
  p.poly(pts,col,None)
 p.oval((w*.79,20,w*.79+42,62),GOLD,None)
 p.rect((0,h*.69,w,h),'#33282a')
 for y in range(int(h*.72),h,21):p.line([(0,y),(w,y)],'#463438',1)
 def house(x,y,size,lit=True):
  width=size;height=size*.68
  p.rect((x,y,x+width,y+height),'#6c3932',INK,2)
  p.poly([(x-9,y+7),(x+width*.15,y-9),(x+width*.5,y-23),(x+width*.85,y-9),(x+width+9,y+7)],'#233237',GOLD,2)
  for j in range(3):
   xx=x+10+j*(width-20)/3;p.rect((xx,y+17,xx+width*.18,y+height*.72),BRIGHT if lit else '#253235',INK,2)
  p.rect((x+width*.42,y+height*.36,x+width*.64,y+height),RED,INK)
  for xx in (x+6,x+width-6):
   q=Paint();lantern(q);p.paste(q.im,(xx-9,y+4,18,25))
 if kind in ['shop','rest','treasure']:
  p.rect((0,0,w*.23,h),'#3f2629');p.rect((0,35,w,44),RED);p.line([(0,44),(w,44)],GOLD,3)
  for x in range(32,w,100):
   q=Paint();lantern(q);p.paste(q.im,(x,15,32,52))
  if kind=='rest':
   p.rect((120,190,420,300),'#322a29',GOLD,3);q=Paint();flame(q);p.paste(q.im,(250,170,120,120));p.line([(120,300),(420,300)],RED,9)
  elif kind=='shop':
   for yy in (190,263):p.rect((168,yy,w-10,yy+7),'#7b4a33',GOLD,1)
   q=hero('tinsel_idle');p.paste(q,(35,151,90,150))
  else:house(50,165,150);house(460,165,150)
 else:
  for x,size in [(-18,133),(145,66),(430,68),(532,130)]:house(x,int(h*.71-size*.68),size)
 # Plum branch, no fir trees.
 p.line([(0,37),(63,62),(109,41),(161,55)],'#594336',6)
 for x,y in [(41,50),(64,62),(103,43),(132,49),(153,54)]:
  for k in range(5):a=k*math.tau/5;p.oval((x+math.cos(a)*4-3,y+math.sin(a)*4-3,x+math.cos(a)*4+3,y+math.sin(a)*4+3),RED,None)
 return p.im
def write_frame(f,im):
 path=RES/(f['path']+'.png');im.resize((f['width'],f['height']),Image.Resampling.LANCZOS).save(path)
enemy_ids={e['id'] for e in DEFS['enemies']}|{'deer','naughtylistbig','Animation 1'}
for f in frames:
 if f.get('authored') or f['type']=='NianTitle':continue
 t,a=f['type'],f['anim'];w,h=f['width'],f['height']
 if t=='Enemy':im=enemy(a);f['eyes']=[{'x':.39,'y':.42},{'x':.62,'y':.42}]
 elif t=='Claus':im=hero(a);f['eyes']=[{'x':.47,'y':.28},{'x':.59,'y':.28}]
 elif t=='Gift':
  p=Paint(132,176);rarity=next((g['rarity'] for g in DEFS['gifts'] if g['id']==a),4 if a in enemy_ids else 1)
  c={0:'#a58b65',1:'#d6c3a0',2:'#ac88c6',3:GOLD,4:JADE,9:'#81727d'}[rarity]
  p.rect((0,0,131,175),INK);p.rect((2,2,129,173),c);p.rect((5,5,126,170),'#32242a');p.rect((8,29,123,138),'#614039' if rarity in [0,1] else '#3c3d42')
  subject=enemy(a) if a in enemy_ids else motif(a);
  if not any(x["type"]=="GiftIllustration" and x["anim"]==a for x in frames):p.paste(subject,(16,30,100,108))
  p.line([(9,140),(122,140)],c,2);p.line([(9,26),(122,26)],c,2)
  for x in (9,122):p.poly([(x,15),(x+4,20),(x,25),(x-4,20)],GOLD,None)
  im=p.im
 elif t=='Icon':
  id=a.split('_',1)[1] if a.startswith(('relic_','potion_','part_','st_')) else a
  im=motif(id)
  if a.startswith('node_'):
   p=Paint();p.oval((2,2,126,126),INK,GOLD,4);p.paste(im,(13,13,102,102));im=p.im
 elif t in ['BG','IntroBG','Background','ChimneyBG','ShopBG','StockingBG','TutorialImage']:
  im=background('rest' if t=='ChimneyBG' else 'shop' if t=='ShopBG' else 'treasure' if t=='StockingBG' else t,640,360)
  if t in ['BG','IntroBG']:f['width']=640;f['height']=360
 elif t=='BGImage':
  if a in ['house','house_lit']:
   p=Paint();p.rect((19,50,110,120),RED,INK);p.poly([(5,54),(27,36),(64,14),(101,36),(123,54)],TEAL,GOLD)
   p.rect((50,73,80,120),GOLD if a=='house_lit' else INK);p.rect((25,70,42,91),BRIGHT if a=='house_lit' else TEAL);im=p.im
  elif a=='moon':p=Paint();p.oval((3,3,125,125),GOLD,None);p.arc((27,27,105,105),30,200,PAPER,3);im=p.im
  elif a=='sleighs':
   p=Paint(456,120)
   for i in range(6):q=Paint();lantern(q);p.paste(q.im,(i*73,4+(i%2)*20,58,80))
   im=p.im
  else:
   p=Paint();p.line([(18,128),(43,80),(52,26),(84,13),(117,29)],TEAL,9)
   for x,y in [(43,80),(52,32),(84,14),(110,24)]:p.oval((x-6,y-6,x+6,y+6),RED,GOLD,1)
   im=p.im
 elif t in ['FeatureKreep','FeatureKreepLogo','GameLogo','Txt','Txt3']:
  p=Paint(w,h);p.rect((1,1,w-2,h-2),INK,GOLD,2);font=ImageFont.truetype(str(ROOT/'Assets/Sleigh/Fonts/FusionPixel12.ttf'),min(24,max(8,h//3)))
  label='逐年' if t=='GameLogo' else '守岁 · 百工';p.d.text((w/2,h/2),label,font=font,fill=GOLD,anchor='mm');im=p.im
 elif t in ['Board','EnemyList','ButtonPanel','PowerFill','SliderBar','Toggle']:
  p=Paint(w,h);p.rect((0,0,w-1,h-1),RED if t in ['ButtonPanel','PowerFill'] else INK,GOLD,1);im=p.im
 elif t=='Particle':
  p=Paint(4,4);p.poly([(0,2),(2,0),(3,2),(2,3)],GOLD,None);im=p.im
 elif t=='HeartEye':im=motif('heart')
 else:im=motif('gift')
 write_frame(f,im)
# Individual event vignettes, actually consumed by RenderEvent.
for i,e in enumerate(DEFS['events']):
 p=Paint(128,160);p.rect((0,0,127,159),INK,GOLD,2)
 subject=['checktwice','ledger','choir','coin','cookietin','globe','gingercharm','snowglobe','naughty','rhythm','milkcookies','rednose','gift','kiss','workbench','snowfall','wishlist','hotcocoa','fruitcake','offkey','stocking','candles','treestar','elfhelp','scarf','pocketwatch'][i]
 p.paste(motif(subject),(4,14,120,125));p.rect((12,145,16+(i%20)*4,148),RED)
 f={'type':'EventArt','anim':e['id'],'frame':0,'path':'Sleigh/Art/Event__'+e['id'],'width':128,'height':160,'pivotX':0,'pivotY':0,'eyes':[]};p.im.save(RES/(f['path']+'.png'));frames.append(f)
INDEX.write_text(json.dumps(bundle,ensure_ascii=False,indent=2),encoding='utf-8')
# A contact sheet is a QA artifact outside Resources.
sheet=Image.new('RGB',(800,math.ceil(len(DEFS['enemies'])/8)*152),'#241e25');draw=ImageDraw.Draw(sheet);font=ImageFont.truetype(str(ROOT/'Assets/Sleigh/Fonts/FusionPixel12.ttf'),12)
for i,e in enumerate(DEFS['enemies']):
 x=i%8*100;y=i//8*152;sheet.paste(enemy(e['id']).resize((96,120)),(x,y),enemy(e['id']).resize((96,120)));draw.text((x+4,y+126),e['name'],font=font,fill=GOLD)
(ROOT/'Artifacts/Nian').mkdir(parents=True,exist_ok=True);sheet.save(ROOT/'Artifacts/Nian/enemy-contact.png')
print('Baked',len(frames),'frames; preserved original resource paths and GUIDs.')
