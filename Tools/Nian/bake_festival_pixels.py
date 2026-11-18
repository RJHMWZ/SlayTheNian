"""Bright, code-drawn pixel art. Builds new presentation resources; no gameplay data changes.
Draw at native integer resolutions, use a shared limited palette and nearest-neighbour scaling.
Original painted assets stay intact so the art direction change is reversible.
"""
from PIL import Image, ImageDraw, ImageFont
import ast, hashlib, json, math, pathlib, random

ROOT = pathlib.Path(__file__).resolve().parents[2]
RES = ROOT / 'Assets/Sleigh/Resources/Sleigh'
OUT = RES / 'Festival'
OUT.mkdir(exist_ok=True)
P = dict(ink='#75463e', red='#df5141', lightred='#ff9370', gold='#f7bd53',
         cream='#fff0be', jade='#6cbfa5', blue='#90cbd0', sky='#78aaa9',
         brown='#b97c54', ivory='#fff8df', violet='#bd96c7', green='#80b976')
PALETTE = [tuple(bytes.fromhex(s[1:])) for s in P.values()]

# Reuse the existing hand-authored symbol recipes, without executing its asset bake.
source = ast.parse((ROOT / 'Tools/Nian/bake_art.py').read_text(encoding='utf-8'))
recipes = dict(Image=Image, ImageDraw=ImageDraw, random=random, math=math, hashlib=hashlib)
recipes.update(INK=P['ink'], RED=P['red'], BRIGHT=P['lightred'], GOLD=P['gold'],
               CREAM=P['cream'], JADE=P['jade'], TEAL=P['blue'], PAPER=P['cream'])
exec(compile(ast.Module(body=[n for n in source.body if isinstance(n, (ast.FunctionDef, ast.ClassDef))],
                        type_ignores=[]), 'festival-symbols', 'exec'), recipes)

def canvas(w, h, color=None):
    im = Image.new('RGBA', (w, h), color or (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)

def pixel_symbol(name, size=32):
    # New recipe output, not a filter applied to the previous generated paintings.
    im = recipes['motif'](name).resize((size, size), Image.Resampling.NEAREST)
    return limited(im)

def limited(im):
    pixels = []
    for r, g, b, a in im.convert('RGBA').get_flattened_data():
        if a < 128: pixels.append((0, 0, 0, 0)); continue
        c = min(PALETTE, key=lambda c: (r-c[0])**2+(g-c[1])**2+(b-c[2])**2)
        pixels.append((*c, 255))
    out = Image.new('RGBA', im.size); out.putdata(pixels)
    return out

def outlined(d, shape, box, color):
    getattr(d, shape)(box, fill=color, outline=P['ink'], width=1)

def face(d, x, y, spacing=8, happy=True):
    for xx in (x-spacing//2, x+spacing//2):
        d.rectangle((xx-1, y-1, xx+1, y+1), fill=P['ivory'])
        d.rectangle((xx, y, xx+1, y+1), fill=P['ink'])
    d.point((x-spacing//2-3, y+3), fill=P['lightred'])
    d.point((x+spacing//2+3, y+3), fill=P['lightred'])
    if happy: d.line([(x-2, y+4), (x, y+5), (x+2, y+4)], fill=P['ink'])

def enemy(name):
    im, d = canvas(48, 56); ink=P['ink']; gold=P['gold']; red=P['red']; cream=P['cream']
    if name in ['santa', 'krampus', 'alpha', 'penguin', 'deer']:
        c = P['lightred'] if name in ['santa','krampus'] else P['jade'] if name in ['alpha','deer'] else gold
        d.line([(35,36),(43,30),(44,20),(39,17)], fill=ink, width=4)
        d.line([(35,36),(43,30),(44,20),(39,17)], fill=c, width=2)
        outlined(d,'ellipse',(12,25,41,46),c)
        for x in (12,31):
            outlined(d,'rectangle',(x,39,x+6,52),c);d.rectangle((x,49,x+6,52),fill=cream)
        for x,y in [(10,19),(18,17),(23,23),(7,27),(24,32),(9,39),(17,41)]:
            outlined(d,'ellipse',(x-4,y-4,x+5,y+5),gold if name!='krampus' else red)
        outlined(d,'ellipse',(4,19,25,40),cream);face(d,14,28,10)
        d.rectangle((6,31,13,33),fill=c);d.rectangle((5,33,9,34),fill=ink)
        for x in (8,21):
            d.line([(x,22),(x-2,15),(x,12),(x+2,12)],fill=ink,width=3)
            d.line([(x,21),(x-2,15),(x,12),(x+2,12)],fill=gold,width=1)
        d.line([(14,42),(25,41),(31,48)],fill=red,width=3);d.rectangle((20,42,23,45),fill=gold)
        for x in (28,34):d.arc((x,29,x+5,35),0,180,fill=cream)
        if name=='penguin':
            outlined(d,'ellipse',(29,32,38,41),cream);d.line([(33,34),(33,39)],fill=ink)
        if name=='krampus':
            for x,y in [(29,20),(35,15),(39,28),(5,46)]:
                d.polygon([(x-2,y+5),(x-3,y),(x,y-5),(x+3,y+2),(x+1,y+5)],fill=gold)
                d.point((x,y+1),fill=cream)
    elif name in ['naughtylist','naughtylistbig']:
        outlined(d,'rectangle',(7,17,41,50),red)
        d.polygon([(2,19),(9,14),(17,13),(24,8),(31,13),(39,14),(46,19)],fill=P['jade'],outline=ink)
        d.line([(3,19),(45,19)],fill=gold,width=2)
        outlined(d,'rectangle',(13,23,35,48),P['violet']);face(d,24,31,12)
        d.rectangle((7,22,10,44),fill=gold);d.rectangle((38,22,40,44),fill=gold)
        d.line([(15,45),(24,43),(33,45)],fill=cream)
        for x in (11,37):
            outlined(d,'ellipse',(x-3,5,x+3,12),P['lightred']);d.line([(x,12),(x,15)],fill=gold)
        d.rectangle((5,50,43,53),fill=gold)
    elif name in ['ornament','lights','jack']:
        if name=='ornament':
            outlined(d,'ellipse',(8,13,39,41),red)
            d.arc((16,13,31,41),90,270,fill=gold);d.arc((16,13,31,41),270,450,fill=gold)
            d.rectangle((15,11,32,14),fill=gold);d.rectangle((15,40,32,43),fill=gold)
            d.line([(24,2),(24,11)],fill=ink);d.line([(24,43),(24,52)],fill=red,width=3)
            face(d,24,25,12)
        elif name=='lights':
            d.polygon([(7,42),(11,30),(17,23),(22,8),(28,23),(38,32),(41,42),(34,48),(14,48)],fill=P['lightred'],outline=ink)
            d.polygon([(16,43),(18,34),(24,23),(31,40),(28,47)],fill=gold);face(d,24,35,10)
            outlined(d,'ellipse',(9,47,39,52),red)
        else:
            for x,y in [(12,18),(26,24),(19,34)]:
                outlined(d,'rectangle',(x,y,x+10,y+17),red)
                d.line([(x+1,y+2),(x+9,y+2)],fill=gold);d.line([(x+1,y+14),(x+9,y+14)],fill=gold)
            d.line([(22,19),(22,10),(30,5)],fill=gold);d.rectangle((28,3,31,6),fill=P['lightred'])
            face(d,24,32,8);d.line([(15,50),(12,54),(17,54)],fill=ink);d.line([(29,50),(32,54),(36,54)],fill=ink)
    elif name in ['rat','slime','viper','fruitcake','creeper','angel','bat']:
        if name=='rat':
            d.line([(33,42),(44,43),(45,33),(40,31)],fill=red,width=2)
            outlined(d,'ellipse',(8,29,37,50),P['blue']);outlined(d,'ellipse',(5,19,21,35),P['jade'])
            outlined(d,'ellipse',(7,13,18,25),P['lightred']);face(d,13,32,8)
            d.rectangle((3,36,8,38),fill=red);d.line([(12,46),(10,53),(17,53)],fill=ink)
        elif name=='slime':
            d.polygon([(4,50),(7,34),(15,30),(20,19),(28,18),(34,32),(41,37),(44,50)],fill=cream,outline=ink)
            face(d,24,38,14);d.line([(10,49),(38,49)],fill=gold,width=2)
        elif name in ['viper','fruitcake','creeper']:
            c=red if name=='viper' else gold if name=='fruitcake' else P['jade']
            d.line([(37,51),(14,48),(10,42),(17,36),(34,34),(37,25),(29,17)],fill=ink,width=10)
            d.line([(37,51),(14,48),(10,42),(17,36),(34,34),(37,25),(29,17)],fill=c,width=8)
            outlined(d,'ellipse',(14,9,37,26),c);face(d,25,17,9)
            if name=='fruitcake':
                for x,y in [(16,41),(28,37),(32,27)]:d.line([(x,y-2),(x,y+2)],fill=cream,width=2)
            elif name=='creeper':
                for x,y in [(13,40),(33,30),(20,11)]:d.polygon([(x,y),(x-7,y-5),(x-3,y+2)],fill=P['green'])
                d.rectangle((16,7,20,11),fill=P['lightred'])
            else:
                for x,y in [(14,43),(26,39),(33,32)]:d.rectangle((x,y,x+2,y+2),fill=cream)
        else:
            c=cream if name=='angel' else P['blue']
            d.polygon([(23,25),(11,13),(2,9),(4,36),(14,32),(21,42),(27,42),(34,32),(44,36),(46,9),(34,13)],fill=c,outline=ink)
            d.line([(3,10),(14,32),(24,25),(34,32),(45,10)],fill=gold)
            outlined(d,'ellipse',(17,25,31,45),red);face(d,24,32,6)
            if name=='angel':d.line([(24,43),(24,53)],fill=red,width=2)
    else:
        # Upright folk figures share proportions, with distinct hats, weapons and body materials.
        c={'gingerling':gold,'snowman':P['blue'],'nutgrunt':cream,'toysoldier':P['jade'],
           'general':red,'rogueelf':P['jade'],'yeti':cream,'coalgolem':P['blue'],
           'coalbaron':P['violet'],'grumpkin':gold,'witch':P['violet'],
           'jackfrost':P['blue'],'krampimp':P['violet']}.get(name,red)
        big=name in ['yeti','coalgolem','coalbaron','grumpkin']
        left,right=(9,38) if big else (15,32)
        outlined(d,'rectangle',(left+4,40,left+10,52),c);outlined(d,'rectangle',(right-9,40,right-3,52),c)
        outlined(d,'ellipse',(left-2,24,right+2,47),c)
        outlined(d,'ellipse',(left-7,27,left+1,42),c);outlined(d,'ellipse',(right-1,27,right+7,42),c)
        outlined(d,'ellipse',(12 if big else 14,9,36 if big else 34,31),c);face(d,24,20,10)
        d.line([(left,35),(right,35)],fill=red,width=3);d.rectangle((22,33,26,37),fill=gold)
        if name in ['general','nutgrunt','toysoldier']:
            d.polygon([(12,13),(15,6),(23,4),(33,6),(36,13)],fill=red if name=='general' else P['blue'],outline=ink)
            d.line([(15,13),(33,13)],fill=gold,width=2)
            for y in (28,32,40):d.line([(18,y),(30,y)],fill=gold)
            if name=='toysoldier':
                d.line([(35,28),(44,39)],fill=ink,width=3);d.line([(34,37),(46,27)],fill=P['brown'],width=2)
                d.line([(35,27),(44,39)],fill=gold)
            else:
                d.line([(41,15),(41,53)],fill=P['brown'],width=2)
                d.polygon([(41,14),(45,18),(45,26),(41,29)],fill=gold,outline=ink)
            if name=='nutgrunt':outlined(d,'rectangle',(5,30,16,44),red);d.rectangle((9,34,12,39),fill=gold)
            if name=='general':d.line([(19,5),(17,1),(22,1)],fill=P['lightred'],width=2)
        elif name in ['snowman','yeti','jackfrost']:
            d.polygon([(13,13),(15,3),(21,11),(27,3),(34,13)],fill=cream,outline=ink)
            d.line([(15,30),(34,30),(38,43)],fill=red,width=3)
            if name=='jackfrost':
                for x,y in [(6,20),(39,18),(36,46)]:d.polygon([(x,y-3),(x+2,y),(x,y+3),(x-2,y)],fill=cream)
            if name=='yeti':d.line([(13,40),(18,44),(23,41),(28,44),(34,40)],fill=P['blue'],width=2)
        elif name=='witch':
            d.polygon([(9,13),(17,7),(24,2),(31,7),(40,13)],fill=P['brown'],outline=ink)
            d.line([(41,23),(41,53)],fill=gold,width=2);outlined(d,'ellipse',(38,17,45,25),P['lightred'])
            d.line([(17,30),(14,38),(14,44)],fill=cream)
        elif name=='rogueelf':
            d.arc((10,5,37,35),180,360,fill=red,width=5);d.line([(15,27),(32,27)],fill=red,width=3)
            outlined(d,'ellipse',(33,32,46,48),gold);d.rectangle((38,37,41,40),fill=ink)
        elif name=='grumpkin':
            d.polygon([(14,13),(13,3),(22,10),(29,10),(36,3),(35,15)],fill=gold,outline=ink)
            outlined(d,'ellipse',(16,36,32,48),gold);d.rectangle((22,40,26,44),fill=P['brown'])
        elif name=='coalbaron':
            d.rectangle((14,6,33,12),fill=red);d.rectangle((18,2,29,8),fill=ink)
            d.polygon([(10,38),(18,42),(30,42),(38,38),(33,48),(15,48)],fill=gold,outline=ink)
        elif name=='coalgolem':
            for x,y in [(15,32),(28,40),(34,29)]:d.line([(x,y),(x+3,y+4),(x,y+6)],fill=cream)
            d.rectangle((21,38,27,44),fill=gold);d.point((24,40),fill=P['lightred'])
        elif name=='krampimp':
            d.line([(15,12),(12,5),(16,7)],fill=gold,width=2);d.line([(32,12),(36,5),(33,7)],fill=gold,width=2)
            d.line([(7,35),(3,22),(6,15)],fill=P['green'],width=2);d.line([(40,35),(45,23),(42,15)],fill=P['green'],width=2)
        elif name=='gingerling':
            d.rectangle((21,10,27,14),fill=red)
            for y in (30,41):d.rectangle((22,y,25,y+2),fill=cream)
    return im

def hero(name):
    # The three playable roles face right, using the same native pixel density as the enemies.
    im,d=canvas(40,60);ink=P['ink'];red=P['red'];gold=P['gold']
    c=red if name=='idle' else P['jade'] if name=='tinsel_idle' else P['blue']
    d.rectangle((12,43,17,56),fill=ink);d.rectangle((22,43,27,56),fill=ink)
    d.rectangle((10,55,18,58),fill=P['brown']);d.rectangle((22,55,31,58),fill=P['brown'])
    d.polygon([(12,24),(25,24),(30,46),(23,50),(10,47),(8,33)],fill=c,outline=ink)
    d.line([(20,25),(16,33),(20,46)],fill=gold);d.line([(11,38),(27,38)],fill=P['brown'],width=3)
    d.rectangle((17,37,20,40),fill=gold)
    outlined(d,'ellipse',(12,7,28,26),P['cream'])
    d.polygon([(11,18),(9,10),(13,4),(23,3),(29,8),(29,15),(22,9),(19,13),(15,10)],fill=ink)
    d.rectangle((23,15,25,17),fill=ink);d.point((27,19),fill=P['lightred']);d.line([(23,22),(26,21)],fill=ink)
    d.line([(24,28),(29,36),(35,33)],fill=c,width=4);d.rectangle((32,31,36,34),fill=P['cream'])
    d.line([(10,28),(7,39),(11,41)],fill=c,width=4)
    if name=='tinsel_idle':
        d.ellipse((8,1,17,9),fill=ink);d.line([(9,5),(19,7)],fill=gold,width=2)
        d.rectangle((12,39,25,49),fill=P['brown']);d.line([(15,42),(14,47)],fill=gold)
        d.line([(35,21),(34,39)],fill=P['brown'],width=2);d.rectangle((34,20,36,24),fill=red)
        outlined(d,'rectangle',(3,40,11,51),P['cream']);d.line([(6,43),(9,44),(6,47),(9,48)],fill=red)
    else:
        d.line([(10,25),(24,27),(29,34)],fill=P['lightred'],width=3)
        d.line([(9,26),(4,33),(1,32)],fill=red,width=3)
        d.line([(35,7),(35,56)],fill=P['brown'],width=2)
        outlined(d,'ellipse',(30,19,39,31),red);d.line([(31,19),(38,19)],fill=gold);d.line([(31,30),(38,30)],fill=gold)
        d.line([(35,22),(35,27)],fill=gold)
        if name=='rudolph_idle':d.polygon([(33,8),(35,1),(38,8)],fill=gold,outline=ink)
        else:d.rectangle((12,5,15,8),fill=red)
    return im

def backdrop(kind):
    im,d=canvas(320,180,P['sky']);ink=P['ink'];gold=P['gold'];red=P['red']
    # Flat color bands and stepped clouds are intentional pixel art, not a scaled painting.
    d.rectangle((0,53,319,108),fill='#9ac6ba');d.rectangle((0,109,319,179),fill='#e9bd80')
    for x,y,w in [(12,23,32),(67,38,23),(247,21,32),(196,48,20)]:
        d.rectangle((x,y,x+w,y+3),fill=P['cream']);d.rectangle((x+5,y-3,x+w-5,y+5),fill=P['cream'])
    d.ellipse((270,12,293,35),fill=P['cream'])
    d.polygon([(32,108),(74,64),(86,80),(105,59),(148,108)],fill='#79ad9e')
    d.polygon([(174,108),(202,75),(216,85),(242,59),(280,108)],fill='#83b6a4')
    def lamp(x,y,s=1):
        d.line([(x,y-5),(x,y)],fill=ink)
        outlined(d,'ellipse',(x-4*s,y,x+4*s,y+8*s),red)
        d.line([(x-3*s,y),(x+3*s,y)],fill=gold);d.line([(x-3*s,y+8*s),(x+3*s,y+8*s)],fill=gold)
        d.line([(x,y+2*s),(x,y+6*s)],fill=P['lightred']);d.line([(x,y+9*s),(x,y+12*s)],fill=red,width=s)
    def house(x,y,w,h):
        outlined(d,'rectangle',(x,y,x+w,y+h),'#e8c28f')
        d.polygon([(x-7,y+2),(x+2,y-6),(x+9,y-5),(x+w//2,y-14),(x+w-8,y-5),(x+w+1,y-6),(x+w+7,y+2)],fill=P['jade'],outline=ink)
        for yy in (y-3,y):d.line([(x+2,yy),(x+w-2,yy)],fill='#b3dfbc')
        outlined(d,'rectangle',(x+w//2-7,y+10,x+w//2+7,y+h),red)
        d.line([(x+w//2,y+11),(x+w//2,y+h)],fill=gold)
        for xx in (x+3,x+w-10):
            outlined(d,'rectangle',(xx,y+10,xx+7,y+22),gold)
            d.line([(xx+3,y+11),(xx+3,y+21)],fill=P['brown'])
        lamp(x+4,y+3);lamp(x+w-3,y+3)
        for xx in (x+w//2-11,x+w//2+9):
            d.rectangle((xx,y+10,xx+2,y+h-2),fill=red)
            for yy in range(y+13,y+h-3,5):d.point((xx+1,yy),fill=gold)
    for x,w in [(63,31),(100,25),(198,24),(232,28)]:house(x,100,w,28)
    house(-8,75,63,68);house(270,75,56,68)
    # Lantern street, stone paving and bright paper-cut decorations.
    d.line([(34,63),(76,72),(116,77),(160,79),(204,77),(244,72),(286,63)],fill=P['brown'])
    for x,y in [(52,68),(88,74),(232,74),(267,68)]:lamp(x,y)
    for y in (145,156,170):
        d.line([(0,y),(319,y)],fill='#cd986a')
        for x in range((y%3)*20,320,42):d.line([(x,y),(x-5,y+10)],fill='#cd986a')
    # Plum blossom branch and orange trees, clear of the title and menu column.
    d.line([(0,28),(20,24),(37,12),(48,5)],fill=ink,width=3)
    for x,y in [(8,26),(19,24),(32,16),(43,9)]:
        for dx,dy in [(0,0),(-2,0),(2,0),(0,-2),(0,2)]:d.rectangle((x+dx,y+dy,x+dx+1,y+dy+1),fill=P['lightred'])
        d.point((x,y),fill=gold)
    for x in (18,302):
        d.rectangle((x-4,158,x+4,169),fill=red);d.line([(x,158),(x,141)],fill=P['brown'],width=2)
        d.ellipse((x-10,134,x+10,157),fill=P['green'])
        for dx,dy in [(-5,141),(3,145),(-2,151)]:d.rectangle((x+dx,dy,x+dx+2,dy+2),fill=gold)
    if kind=='shop':
        d.rectangle((73,74,319,84),fill=red);d.line([(73,84),(319,84)],fill=gold,width=2)
        for x in range(80,310,20):d.rectangle((x,74,x+8,83),fill=P['lightred'])
        for y in (98,130):d.rectangle((85,y,312,y+4),fill=P['brown']);d.line([(85,y),(312,y)],fill=gold)
        im.alpha_composite(hero('tinsel_idle'),(15,95))
    elif kind=='rest':
        outlined(d,'rectangle',(90,105,204,149),P['brown']);outlined(d,'rectangle',(114,115,180,148),P['red'])
        d.polygon([(126,146),(124,132),(135,120),(139,108),(146,124),(164,141),(157,147)],fill=gold)
        d.rectangle((105,148,194,153),fill=ink)
    elif kind=='treasure':
        for x in (105,206):lamp(x,104,2)
    elif kind=='mountain':
        d.polygon([(43,103),(82,54),(101,82),(139,36),(185,102)],fill=P['blue'])
        d.polygon([(112,70),(139,36),(160,67),(145,61),(137,57),(128,66)],fill=P['cream'])
    elif kind=='gate':
        d.rectangle((122,86,197,131),fill=red);d.rectangle((136,98,184,131),fill=P['jade'])
        d.polygon([(115,87),(131,77),(161,66),(187,77),(205,87)],fill=gold,outline=ink)
    return im

index_path=RES/'ArtIndex.json'
bundle=json.loads(index_path.read_text(encoding='utf-8'))
defs=json.loads((RES/'Definitions.json').read_text(encoding='utf-8'))
gift_by_id={g['id']:g for g in defs['gifts']}
enemy_ids={e['id'] for e in defs['enemies']}|{'deer','Animation 1','naughtylistbig'}
written={}
def save(f, im, key):
    name=''.join(c if c.isalnum() or c in '_-' else '_' for c in key)
    path=OUT/(name+'.png')
    if name not in written:im.save(path);written[name]=im
    f.update(path='Sleigh/Festival/'+name,columns=0,rows=0,tile=0,pixel=True,authored=True)

for f in bundle['frames']:
    t,a=f['type'],f['anim']
    if t=='Enemy':
        save(f,enemy(a),'Enemy_'+a);f['flipX']=False
        f.update(width=144 if a in ['santa','krampus'] else 80 if a in ['naughtylist','naughtylistbig'] else 96,
                 height=168 if a in ['santa','krampus'] else 94 if a in ['naughtylist','naughtylistbig'] else 112)
    elif t=='Claus':save(f,hero(a),'Hero_'+a);f.update(width=80,height=120)
    elif t=='GiftIllustration':
        g=gift_by_id[a];art=enemy(g['enemy']) if g['type']=='friend' else pixel_symbol(a)
        save(f,art,'Illustration_'+a)
    elif t=='Gift':
        im,d=canvas(66,88,P['ink']);d.rectangle((1,1,64,86),fill=P['gold'])
        d.rectangle((3,3,62,84),fill=P['red']);d.rectangle((5,23,60,68),fill=P['cream'])
        d.line([(5,21),(60,21)],fill=P['lightred']);d.line([(5,70),(60,70)],fill=P['gold'])
        if not any(x['type']=='GiftIllustration' and x['anim']==a for x in bundle['frames']):
            art=enemy(a) if a in enemy_ids else pixel_symbol(a)
            # Integer-scaled, nearest-neighbour symbols; no smoothing at import/runtime.
            art=art.resize((32,40),Image.Resampling.NEAREST);im.alpha_composite(art,(17,26))
        save(f,im,'Card_'+a)
    elif t=='Icon':
        name=a.split('_',1)[1] if a.startswith(('relic_','potion_','part_','st_')) else a
        save(f,pixel_symbol(name,24),'Icon_'+a+'_'+str(f['frame']))
    elif t in ['NianTitle','BG','IntroBG','Background','ChimneyBG','ShopBG','StockingBG']:
        kind={'ChimneyBG':'rest','ShopBG':'shop','StockingBG':'treasure'}.get(t,'town')
        save(f,backdrop(kind),'Background_'+kind)
    elif t=='EventArt':
        im,d=canvas(64,80,P['cream']);d.rectangle((0,0,63,79),outline=P['gold'],width=2)
        i=next(i for i,e in enumerate(defs['events']) if e['id']==a)
        motifs=['gift','checktwice','choir','coin','cookietin','globe','gingercharm','snowglobe','naughty','rhythm','milkcookies','rednose','gift','kiss','workbench','snowfall','wishlist','hotcocoa','fruitcake','offkey','stocking','candles','treestar','elfhelp','scarf','pocketwatch']
        im.alpha_composite(pixel_symbol(motifs[i],48),(8,14));save(f,im,'Event_'+a)
    elif t=='BGImage' and a in ['house','house_lit']:
        im,d=canvas(46,31,P['cream']);d.polygon([(0,10),(7,5),(23,0),(39,5),(45,10)],fill=P['jade'],outline=P['ink'])
        d.rectangle((5,11,40,30),fill=P['brown']);d.rectangle((18,15,28,30),fill=P['red'])
        for x in (8,31):d.rectangle((x,16,x+6,22),fill=P['gold'] if a=='house_lit' else P['cream'])
        save(f,im,'House_'+a)
    elif t=='BGImage' and a=='moon':
        im,d=canvas(24,24);d.ellipse((1,1,22,22),fill=P['cream']);save(f,im,'Moon')
    elif t=='BGImage' and a in ['tree','Animation 1']:
        im,d=canvas(48,72);d.line([(21,70),(24,18),(39,8)],fill=P['brown'],width=3)
        for x,y in [(13,20),(23,12),(37,10),(31,29),(12,41),(27,45)]:d.ellipse((x-6,y-5,x+6,y+5),fill=P['green']);d.rectangle((x,y,x+2,y+2),fill=P['gold'])
        save(f,im,'SpringTree_'+a)
    elif t=='HeartEye':save(f,pixel_symbol('heart',16),'FriendshipHeart')

# Region backgrounds retain the same map/combat rules; only artwork is selected by progress.
for a,kind in [('town','town'),('mountain','mountain'),('market','shop'),('gate','gate')]:
    f=dict(type='FestivalRegion',anim=a,frame=0,width=640,height=360,pivotX=0,pivotY=0,eyes=[])
    save(f,backdrop(kind),'Region_'+a)
    previous=next((x for x in bundle['frames'] if x['type']==f['type'] and x['anim']==a),None)
    if previous:previous.update(f)
    else:bundle['frames'].append(f)
index_path.write_text(json.dumps(bundle,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
sheet,_=canvas(8*64,4*80,P['cream'])
for i,e in enumerate(defs['enemies']):sheet.alpha_composite(enemy(e['id']),(i%8*64+8,i//8*80+8))
(ROOT/'Artifacts/Nian').mkdir(exist_ok=True,parents=True)
sheet.save(ROOT/'Artifacts/Nian/festival-enemies.png')
backdrop('town').resize((960,540),Image.Resampling.NEAREST).save(ROOT/'Artifacts/Nian/festival-background.png')
print('Baked',len(written),'original bright pixel resources; preserved definitions and gameplay IDs.')
