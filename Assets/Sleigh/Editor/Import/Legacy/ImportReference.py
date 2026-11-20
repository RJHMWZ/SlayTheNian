"""Offline extraction only. The shipped Unity game contains no JavaScript interpreter."""
import csv, json, pathlib, re, shutil, math
from PIL import Image, ImageDraw

ROOT = next(p for p in pathlib.Path(__file__).resolve().parents if (p/'Assets').is_dir() and (p/'Packages').is_dir())
SRC = ROOT / 'Assets/Preference/SleighTheSpire_Extracted_Resources/SleighTheSpire_Extracted'
OUT = ROOT / 'Assets/Sleigh/Resources/Sleigh'
if not SRC.is_dir():
    raise SystemExit('Legacy reference source is not present. This migration tool is archived; do not regenerate current game definitions from it.')
OUT.mkdir(parents=True, exist_ok=True)

def split(s, delimiter=','):
    result=[]; start=0; stack=[]; quote=None; escaped=False; i=0
    while i<len(s):
        c=s[i]
        if quote:
            if escaped: escaped=False
            elif c=='\\': escaped=True
            elif c==quote: quote=None
        elif c in '\"\'`': quote=c
        elif s[i:i+2]=='//':
            j=s.find('\n',i); i=len(s) if j<0 else j; continue
        elif c in '([{': stack.append(c)
        elif c in ')]}':
            if stack: stack.pop()
        elif c==delimiter and not stack:
            result.append(s[start:i].strip()); start=i+1
        i+=1
    result.append(s[start:].strip())
    return result

def balanced(s, start, left='(', right=')'):
    depth=0; quote=None; escape=False
    for i in range(start,len(s)):
        c=s[i]
        if quote:
            if escape: escape=False
            elif c=='\\': escape=True
            elif c==quote: quote=None
        elif c in '\"\'`': quote=c
        elif c==left: depth+=1
        elif c==right:
            depth-=1
            if depth==0: return s[start+1:i],i+1
    raise ValueError(s[start:start+80])

def calls(s,name):
    for m in re.finditer(r'(?m)^'+name+r'\(',s):
        body,_=balanced(s,m.end()-1)
        yield split(body)

def string(s): return json.loads(s) if s.startswith('"') else s.strip("'")
def props(s):
    return {p[:p.index(':')].strip().strip('"'):p[p.index(':')+1:].strip() for p in split(s.strip()[1:-1]) if ':' in p}
def lit(s):
    s=s.strip()
    if s.startswith('['): return [lit(x) for x in split(s[1:-1]) if x]
    if s.startswith('{'): return {k:lit(v) for k,v in props(s).items()}
    if s.startswith(('"',"'")): return string(s)
    if s=='true': return True
    if s=='false': return False
    if s=='null': return None
    return int(s,0)

data=(SRC/'scripts/project/data.js').read_text(encoding='utf-8-sig')
gifts=[]
for a in calls(data,'gift'):
    v=lit(a[6]); cost=lit(a[4]); cost=cost if isinstance(cost,list) else [cost,cost]
    gifts.append(dict(id=string(a[0]),name=string(a[1]),type={'A':'atk','S':'skl','P':'pow','X':'curse'}[a[2]],rarity=int(a[3]),cost=cost,target=string(a[5]),values=[dict(key=k,base=x[0],up=x[1]) for k,x in v.items()],desc=string(a[7]),flags=string(a[9]) if len(a)>9 else '',steps=[]))
relics=[dict(id=string(a[0]),name=string(a[1]),rarity=int(a[2]),desc=string(a[3])) for a in calls(data,'relic')]
potions=[dict(id=string(a[0]),name=string(a[1]),target=string(a[3]),desc=string(a[4]),outside=len(a)>6) for a in calls(data,'potion')]

es=(SRC/'scripts/project/enemies.js').read_text(encoding='utf-8-sig')
pref_start=es.index('{',es.index('const PREFS'))
prefbody,_=balanced(es,pref_start,'{','}')
prefs=props('{'+prefbody+'}')
enemies=[]
for a in calls(es,'enemy'):
    id=string(a[0]); p=props(a[1]); pref=props(prefs.get(id,'{}'))
    moves=[]
    for key,raw in props(p['moves']).items():
        mv=props(raw); acts=lit(mv['a'])
        moves.append(dict(id=key,name=lit(mv['n']),actions=[dict(op=x[0],key=x[1] if len(x)>1 and isinstance(x[1],str) else '',n=x[1] if len(x)>1 and isinstance(x[1],int) else (x[2] if len(x)>2 and isinstance(x[2],int) else 0),hits=x[2] if x[0]=='atk' and len(x)>2 else 1,pile=x[3] if len(x)>3 else '') for x in acts]))
    ai=p['ai']; seq=re.findall(r'"([a-z]+)"',ai)
    weights=[]
    for key,weight in re.findall(r'(\w+)\s*:\s*(\d+)',ai): weights.append(dict(key=key,n=int(weight)))
    enemies.append(dict(id=id,name=lit(p['name']),tier=lit(p['tier']),hp=lit(p['hp']),fly=p.get('fly')=='true',scale=float(p.get('scale','1')),noCharm=p.get('noCharm',pref.get('noCharm'))=='true',pips=int(pref.get('pips',{'weak':2,'normal':3,'elite':5,'boss':8}[lit(p['tier'])])),love=lit(pref['love']) if 'love' in pref else '',hate=lit(pref['hate']) if 'hate' in pref else '',lore=lit(pref.get('lore','""')),lines=lit(p['lines']),moves=moves,sequence=seq,weights=weights))

enc_start=es.index('{',es.index('export const ENC'))
encbody,_=balanced(es,enc_start,'{','}')
encs=[dict(tier=k,groups=[dict(ids=g) for g in lit(v)]) for k,v in props('{'+encbody+'}').items()]

events=[]
for a in calls((SRC/'scripts/project/events.js').read_text(encoding='utf-8-sig'),'ev'):
    choices=[]
    for raw in split(a[3][1:-1]):
        if not raw: continue
        p=props(raw); label=lit(p['l']); req=p.get('req',''); money=re.search(r'cookies\s*>=\s*(\d+)',req)
        choices.append(dict(label=label,price=int(money.group(1)) if money else 0,needsPotion='potions' in req))
    events.append(dict(id=lit(a[0]),title=lit(a[1]),text=lit(a[2]),choices=choices))

ps=(SRC/'scripts/project/parts.js').read_text(encoding='utf-8-sig')
partbody,_=balanced(ps,ps.index('{',ps.index('export const PARTS')),'{','}')
parts=[]
for id,raw in props('{'+partbody+'}').items():
    p=props(raw); parts.append(dict(id=id,name=lit(p['name']),rarity=int(p['r']),desc=lit(p['desc'])))

# Build Friend definitions with exactly the original bestMove scoring and multipliers.
def score(actions):
    total=0
    for a in actions:
        op=a['op']; n=a['n']
        total+= n*a['hits'] if op=='atk' else n*.8 if op=='def' else min(n,4)*3 if op in ('buff','buffAll','debuff') else n*.6 if op in ('heal','healAll') else 2 if op in ('steal','pocket') else 0
    return total
for e in enemies:
    if e['noCharm'] or e['tier']=='boss': continue
    use={'atk','def','buff','buffAll','debuff','heal','healAll','steal','pocket'}
    candidates=[dict(m,actions=[a for a in m['actions'] if a['op'] in use]) for m in e['moves']]
    m=max(candidates,key=lambda m:score(m['actions']))
    values=[]; steps=[]; desc=[]; aim=False
    for i,a in enumerate(m['actions']):
        op=a['op']; key=('d' if op=='atk' else 'b' if op=='def' else 'k'+str(i)); n=a['n']
        if op in ('buff','buffAll','debuff'): b=min(n,3); u=b+1
        elif op in ('steal','pocket'): b=min(n or 10,12); u=math.floor(b*1.5+.5)
        else: b=max(1,math.floor(n*1.25+.5)); u=max(1,math.floor(n*1.6+.5))
        values.append(dict(key=key,base=b,up=u)); t={'buffAll':'buff','healAll':'heal','steal':'cookies','pocket':'cookies'}.get(op,op)
        steps.append(dict(op=t,key=key,status=a['key'],hits=a['hits']))
        if t=='atk': desc.append('Deal {'+key+'} damage'+(' x'+str(a['hits']) if a['hits']>1 else '')+'.'); aim=True
        elif t=='def': desc.append('Gain {'+key+'} Wrap.')
        elif t=='buff': desc.append('Gain {'+key+'} '+a['key'].title()+'.')
        elif t=='debuff': desc.append('Apply {'+key+'} '+a['key'].title()+'.'); aim=True
        elif t=='heal': desc.append('Heal {'+key+'} HP.')
        else: desc.append('Gain {'+key+'} Cookies.')
    cost=2 if e['tier']=='elite' else 1
    gifts.append(dict(id='f_'+e['id'],name=e['name'],type='friend',rarity=4,cost=[cost,cost],target='e' if aim else 'n',values=values,desc=m['name']+'! '+' '.join(desc)+' Consume.',flags='x',steps=steps,enemy=e['id']))

bundle=dict(gifts=gifts,relics=relics,potions=potions,enemies=enemies,encounters=encs,events=events,parts=parts)
(OUT/'Definitions.json').write_text(json.dumps(bundle,ensure_ascii=False,indent=2),encoding='utf-8')

types=json.loads((SRC/'data.json').read_text(encoding='utf-8-sig'))['project'][3]
(OUT/'FontChars.txt').write_text('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.,;-+/!?:\'>^*%@()"&<',encoding='utf-8')
art=OUT/'Art'; art.mkdir(exist_ok=True)
frames=[]; images={}
for t in types:
    animations=t[7] or [['default',0,False,0,0,False,0,[t[6]]]]
    for anim in animations:
        for i,f in enumerate(anim[7]):
            if not f: continue
            sheet=pathlib.Path(f[0]).stem+'.png'
            if sheet not in images: images[sheet]=Image.open(SRC/'Unity_Ready/Images'/sheet).convert('RGBA')
            x,y,w,h=f[2:6]
            # Construct's rotated atlas entries store logical (unrotated) dimensions.
            im=images[sheet].crop((x,y,x+(h if f[6] else w),y+(w if f[6] else h)))
            if f[6]: im=im.transpose(Image.Transpose.ROTATE_90)
            name=t[0]+'__'+re.sub(r'[^\w-]','_',anim[0])+'__'+str(i)
            im.save(art/(name+'.png'))
            frames.append(dict(type=t[0],anim=anim[0],frame=i,path='Sleigh/Art/'+name,width=w,height=h,pivotX=f[8] if len(f)>8 else 0,pivotY=f[9] if len(f)>9 else 0,sheet=sheet,x=x,y=y,eyes=[dict(x=p[1],y=p[2]) for p in f[10]] if len(f)>10 else []))
# Keep authored player replacements when extracting the reference again.
for animation, filename, pivot_x in [('idle', 'yierIdle0.png', .54), ('rudolph_idle', 'bubu1.png', .5)]:
    player_art = art/filename
    if player_art.exists():
        with Image.open(player_art) as player_image:
            player_width, player_height = player_image.size
        for frame in frames:
            if frame['type'] == 'Claus' and frame['anim'] == animation and frame['frame'] == 0:
                frame.update(path='Sleigh/Art/'+player_art.stem, width=player_width, height=player_height,
                             pivotX=pivot_x, pivotY=1, sheet=player_art.name, x=0, y=0, eyes=[])
(OUT/'ArtIndex.json').write_text(json.dumps(dict(frames=frames),indent=2),encoding='utf-8')
with (OUT/'Sprite_Frame_Index.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=list(frames[0])); writer.writeheader(); writer.writerows(frames)
audio=OUT/'Audio'; audio.mkdir(exist_ok=True)
for f in (SRC/'Unity_Ready/Audio').glob('*.ogg'): shutil.copyfile(f,audio/f.name)
# A contact sheet is a transient inspection aid, outside Assets.
sheet=Image.new('RGB',(1024,math.ceil(len(frames)/10)*82),(18,24,42)); draw=ImageDraw.Draw(sheet)
for i,f in enumerate(frames):
    im=Image.open(art/(pathlib.Path(f['path']).name+'.png')); im.thumbnail((94,58),Image.Resampling.NEAREST)
    x=(i%10)*102; y=(i//10)*82; sheet.paste(im,(x,y),im); draw.text((x,y+59),(f['type']+':'+f['anim'])[:17],fill='white')
(ROOT/'Temp/Sleigh').mkdir(exist_ok=True)
sheet.save(ROOT/'Temp/Sleigh/contact.png')
print(f'Extracted {len(frames)} frames from {len(images)} atlases; {len(gifts)} gifts ({len(gifts)-sum(g["type"]=="friend" for g in gifts)} base), {len(enemies)} enemies, {len(relics)} relics, {len(potions)} potions, {len(events)} events, {len(parts)} parts.')
