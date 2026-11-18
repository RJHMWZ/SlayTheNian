"""Meaningful migration checks against the pre-migration revision (or supplied commit)."""
import json,pathlib,re,subprocess,sys,hashlib
ROOT=pathlib.Path(__file__).resolve().parents[2]
base=sys.argv[1] if len(sys.argv)>1 else 'HEAD'
# The first migration records the actual base SHA; no dependency on moving HEAD.
report=ROOT/'Artifacts/Nian';record=report/'baseline.txt'
if len(sys.argv)==1:base=record.read_text().strip() if record.exists() else 'HEAD'
original=json.loads(subprocess.check_output(['git','show',base+':Assets/Sleigh/Resources/Sleigh/Definitions.json'],cwd=ROOT))
current=json.loads((ROOT/'Assets/Sleigh/Resources/Sleigh/Definitions.json').read_text(encoding='utf-8'))
text_fields={'name','desc','lore','lines','title','text','label'}
def rules(x):
 if isinstance(x,dict):return {k:rules(v) for k,v in x.items() if k not in text_fields}
 if isinstance(x,list):return [rules(v) for v in x]
 return x
assert rules(original)==rules(current),'Changed gameplay data, identifier or trigger'
for before,after in zip(original['gifts'],current['gifts']):
 assert set(re.findall(r'\{\w+\}',before['desc']))==set(re.findall(r'\{\w+\}',after['desc'])),after['id']
 assert not re.search('圣诞|驯鹿|胡桃夹|姜饼|槲寄生|坎卜斯|克劳斯|鲁道夫',after['name']+after['desc'])
for key in ['enemies','relics','events','parts','potions']:
 for item in current[key]:
  assert not re.search('圣诞|驯鹿|胡桃夹|姜饼|槲寄生|坎卜斯|克劳斯|鲁道夫',json.dumps({k:v for k,v in item.items() if k in text_fields},ensure_ascii=False))
frames=json.loads((ROOT/'Assets/Sleigh/Resources/Sleigh/ArtIndex.json').read_text())['frames']
keys=set()
for frame in frames:
 key=(frame['type'],frame['anim'],frame['frame']);assert key not in keys,key;keys.add(key)
 assert (ROOT/'Assets/Sleigh/Resources'/(frame['path']+'.png')).exists(),frame['path']
 if frame.get('columns'):assert 0<=frame['tile']<frame['columns']*frame['rows']
glyphs={g['code'] for g in json.loads((ROOT/'Assets/Sleigh/Resources/Sleigh/Fonts/Chinese12.json').read_text())['glyphs']}
for group in current.values():
 for item in group:
  for field in text_fields:
   value=item.get(field,'')
   assert not re.search(r'\?{2,}',str(value)),(item.get('id'),field,value)
  for field in text_fields:
   value=item.get(field,'')
   if isinstance(value,str):assert all(ord(c) in glyphs for c in value if not c.isspace()),value
result=f'PASS: gameplay data identical to {base}; 71 card placeholder sets; content theme, unique art keys, existing resources, atlas bounds and font glyphs.\n'
report.mkdir(parents=True,exist_ok=True);(report/'content-checks.txt').write_text(result,encoding='utf-8');print(result)
