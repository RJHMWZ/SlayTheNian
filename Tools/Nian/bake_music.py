"""Original pentatonic compositions and physical-model-inspired synthesis. No samples."""
import numpy as np, wave,pathlib,hashlib
ROOT=pathlib.Path(__file__).resolve().parents[2];OUT=ROOT/'Assets/Sleigh/Resources/Sleigh/Audio/Music';OUT.mkdir(exist_ok=True)
SR=24000
def save(path,a):
 a=np.tanh(a*.85);pcm=(a*26000).astype('<i2')
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(SR);w.writeframes(pcm.tobytes())
def compose(name,bpm,root,melody,measures,drive):
 beat=60/bpm;dur=measures*4*beat;a=np.zeros(round(SR*dur));rng=np.random.default_rng(int(hashlib.sha256(name.encode()).hexdigest()[:8],16))
 def add(t,x):
  start=round(t*SR);n=min(len(x),len(a)-start)
  if n>0:a[start:start+n]+=x[:n]
 def pluck(midi,t,d=.9,g=.15):
  tt=np.arange(round(SR*d))/SR;freq=440*2**((midi-69)/12)
  s=sum(np.sin(2*np.pi*freq*k*tt)*np.exp(-tt*(2+k*.8))/k**1.3 for k in range(1,7))
  add(t,s*(1-np.exp(-tt*220))*g)
 def flute(midi,t,d=1,g=.07):
  tt=np.arange(round(SR*d))/SR;f=440*2**((midi-69)/12);env=np.minimum(tt/.08,1)*np.minimum((d-tt)/.18,1)
  add(t,(np.sin(2*np.pi*f*tt+.016*np.sin(2*np.pi*5*tt))+.13*np.sin(4*np.pi*f*tt))*env*g)
 def drum(t,strong=False):
  tt=np.arange(int(SR*.44))/SR;phase=2*np.pi*(46*tt+38*.045*(1-np.exp(-tt/.045)))
  add(t,np.sin(phase)*np.exp(-tt*12)*(.35 if strong else .18)+rng.normal(0,1,len(tt))*np.exp(-tt*70)*.03)
 def gong(t,g=.1):
  tt=np.arange(int(SR*1.8))/SR;s=sum(np.sin(2*np.pi*f*tt)*np.exp(-tt*(2+i*.4))/(i+1) for i,f in enumerate([181,294,417,643,899]))
  add(t,s*g*(1-np.exp(-tt*180)))
 # Original melodies use gong/shang/jue/zhi/yu rather than quotations of traditional songs.
 for b in range(measures*4):
  chord=[0,7,9,5][(b//4)%4];bass=root-24+chord
  pluck(bass,b*beat,1.5,.12)
  note=root+melody[b%len(melody)];pluck(note,b*beat,beat*1.5,.15)
  if b%2==0:flute(note+12,b*beat,beat*1.75,.055)
  if drive:
   drum(b*beat,b%4==0)
   if drive>1:drum((b+.5)*beat,False)
  elif b%4==0:drum(b*beat,False)
  if b%8==0:gong(b*beat,.08 if name!='boss' else .16)
  if b%2==1:pluck(root+melody[(b+3)%len(melody)]+12,(b+.5)*beat,beat,.06)
 # Fade at musical boundary to prevent clicks, intro resolves into loop tonic.
 a[:240]*=np.linspace(0,1,240);a[-480:]*=np.linspace(1,0,480)
 return a
tracks=[('calm',84,62,[0,2,4,7,9,7,4,2,0,4,7,9,12,9,7,2],8,0),('map',76,57,[0,7,4,2,0,-3,2,4,7,9,7,4,2,0,2,-3],8,0),('battle',116,62,[0,7,4,9,7,4,2,0,2,4,7,12,9,7,4,2],8,1),('boss',132,50,[0,0,7,2,4,2,0,-3,0,7,9,7,4,2,0,-5],8,2),('end',104,62,[0,4,7,9,12,9,7,4,2,4,7,12,14,12,9,7],8,1)]
for name,bpm,root,melody,m,d in tracks:
 loop=compose(name,bpm,root,melody,m,d);intro=compose(name+'intro',bpm,root,melody[:8],2,d)
 save(OUT/(name+'_intro.wav'),intro);save(OUT/(name+'_loop.wav'),loop)
print('Baked five original pentatonic scores (menu, exploration, battle, boss, dawn).')
