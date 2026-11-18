"""Offline bitmap font bake; the Unity player reads only the saved atlas and metrics."""
import json,pathlib,math
from PIL import Image,ImageDraw,ImageFont
root=pathlib.Path(__file__).resolve().parents[3]
font=ImageFont.truetype(str(root/'Assets/Sleigh/Fonts/FusionPixel12.ttf'),12)
characters=set(chr(i) for i in range(32,127))
for high in range(0xA1,0xF8):
    for low in range(0xA1,0xFF):
        try: characters.update(bytes([high,low]).decode('gb2312'))
        except UnicodeDecodeError: pass
catalog=json.loads((root/'Assets/Sleigh/Resources/Sleigh/Localization/zh-CN.json').read_text(encoding='utf-8'))
for entry in catalog['entries']:characters.update(entry['text'])
characters.update('圣诞救援雪橇上的冒险廷瑟克劳斯夫人鲁道夫□×—…，。！？：；（）【】「」“”‘’')
for source in (root/'Assets/Sleigh').rglob('*.cs'):
    characters.update(source.read_text(encoding='utf-8-sig'))
characters.update((root/'Assets/Sleigh/Resources/Sleigh/Definitions.json').read_text(encoding='utf-8'))
characters.difference_update('\n\r\t');characters=sorted(characters)
cell=16;columns=64;width=cell*columns;height=2**math.ceil(math.log2(math.ceil(len(characters)/columns)*cell))
atlas=Image.new('RGBA',(width,height),(255,255,255,0));mask=Image.new('L',(width,height));draw=ImageDraw.Draw(mask);glyphs=[]
for i,c in enumerate(characters):
    draw.text(((i%columns)*cell,(i//columns)*cell-3),c,font=font,fill=255,stroke_width=0)
    glyphs.append({'code':ord(c),'advance':round(font.getlength(c))})
mask=mask.point(lambda x:255 if x>=128 else 0);atlas.putalpha(mask)
output=root/'Assets/Sleigh/Resources/Sleigh/Fonts';output.mkdir(parents=True,exist_ok=True)
atlas.save(output/'Chinese12.png')
(output/'Chinese12.json').write_text(json.dumps({'cell':cell,'columns':columns,'width':width,'height':height,'glyphs':glyphs},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
print(f'Baked {len(glyphs)} glyphs, {width}x{height}, point sampling, no runtime font generation.')
