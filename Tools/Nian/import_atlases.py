"""Connect authored atlases to the existing resource-driven UI without rekeying gameplay data."""
import json,pathlib
ROOT=pathlib.Path(__file__).resolve().parents[2];RES=ROOT/'Assets/Sleigh/Resources/Sleigh'
p=RES/'ArtIndex.json';bundle=json.loads(p.read_text());frames=bundle['frames'];defs=json.loads((RES/'Definitions.json').read_text(encoding='utf-8'))
def add(f):
 old=next((x for x in frames if (x['type'],x['anim'],x['frame'])==(f['type'],f['anim'],f['frame'])),None)
 if old:old.update(f)
 else:frames.append(f)
def tile(type,anim,pack,index,n,w,h,pivot=.5):
 return dict(type=type,anim=anim,frame=0,path='Sleigh/Nian/'+pack,width=w,height=h,pivotX=pivot,pivotY=1 if type in ['Enemy','Claus'] else 0,eyes=[dict(x=.42,y=.35),dict(x=.58,y=.35)],columns=n,rows=n,tile=index,authored=True)
enemy_groups={
'FolkA':['gingerling','snowman','ornament','rat','lights','penguin','nutgrunt','toysoldier','jack'],
'FolkB':['fruitcake','angel','slime','bat','grumpkin','rogueelf','coalgolem','viper','creeper'],
'FolkC':['krampimp','general','yeti','coalbaron','alpha','witch','jackfrost','deer','Animation 1']}
for pack,ids in enemy_groups.items():
 if not (RES/'Nian'/(pack+'.png')).exists():continue
 for i,id in enumerate(ids):add(tile('Enemy',id,pack,i,3,95 if id not in ['yeti','alpha'] else 108,100))
for pack,start,end in [('CardsA',0,25),('CardsB',25,47)]:
 if not (RES/'Nian'/(pack+'.png')).exists():continue
 for i,g in enumerate(defs['gifts'][start:end]):add(tile('GiftIllustration',g['id'],pack,i,5,66,66,0))
for g in defs['gifts']:
 if g['type']=='friend':
  e=next(f for f in frames if f['type']=='Enemy' and f['anim']==g['enemy'])
  f=dict(e);f.update(type='GiftIllustration',anim=g['id'],width=66,height=66,pivotX=0,pivotY=0,authored=True);add(f)
if (RES/'Nian/Relics.png').exists():
 for i,r in enumerate(defs['relics']):add(tile('Icon','relic_'+r['id'],'Relics',i,6,18,18))
if (RES/'Nian/Heroes.png').exists():
 for i,id in enumerate(['idle','tinsel_idle','rudolph_idle']):
  f=tile('Claus',id,'Heroes',i,3,64,105);f['rows']=1;add(f)
for f in frames:
 if f['type']=='Enemy' and f['anim'] in ['santa','krampus']:f['flipX']=True
p.write_text(json.dumps(bundle,ensure_ascii=False,indent=2),encoding='utf-8')
print('Connected',len(frames),'frames; atlas-backed frames:',sum(bool(f.get('columns')) for f in frames))
