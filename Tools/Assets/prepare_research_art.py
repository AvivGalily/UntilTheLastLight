"""Compose the retained Blender study renders and original reptilian scale maps."""
from pathlib import Path
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT=Path(__file__).resolve().parents[2]
out=ROOT/'Assets/_Game/Art/Textures'
folder=ROOT/'ArtSource/Research'
im=Image.new('RGB',(2400,1600),'#d8d8c3');d=ImageDraw.Draw(im)
def font(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/'+('consolab.ttf' if bold else 'consola.ttf'),size)
ink='#263c3b';faint='#9da99b'
for x in range(48,2354,40):d.line((x,172,x,1470),fill='#c6cbb7',width=1)
for y in range(172,1470,40):d.line((48,y,2352,y),fill='#c6cbb7',width=1)
d.rectangle((32,32,2368,1568),outline=ink,width=5)
d.rectangle((48,48,2352,152),fill=ink)
d.text((76,67),'BLACKPINE FIELD RESEARCH  /  RESTRICTED ARCHIVE',font=font(47,True),fill='#dedec9')
d.text((76,119),'MORPHOLOGY DIVISION     |     PLATE 08     |     SUBJECT: WALKER',font=font(22),fill='#dedec9')
views=[('front','A / ANTERIOR',76,204),('side','B / LATERAL',1250,204),('dorsal','C / DORSAL',76,836),('oblique','D / THREE-QUARTER',1250,836)]
for key,title,x,y in views:
    d.text((x,y),title,font=font(30,True),fill=ink)
    d.line((x,y+42,x+1070,y+42),fill=ink,width=2)
    img=Image.open(folder/('walker_'+key+'.png')).convert('RGBA')
    box=img.getbbox();img=img.crop(box);img.thumbnail((970,455),Image.Resampling.LANCZOS)
    # Restrained monochrome anatomy keeps the real silhouette, joints and teeth.
    gray=img.convert('L');tint=Image.merge('RGB',(gray.point(lambda a:int(a*.73)),gray.point(lambda a:int(a*.80)),gray.point(lambda a:int(a*.72))))
    im.paste(tint,(x+(1070-img.width)//2,y+66),img.getchannel('A'))
    d=ImageDraw.Draw(im)
    d.line((x+80,y+544,x+990,y+544),fill=ink,width=2)
    for tick in range(11):d.line((x+80+91*tick,y+536,x+80+91*tick,y+552),fill=ink,width=2)
    d.text((x+422,y+558),'0             1 m',font=font(20),fill=ink)
    d.line((x+70,y+220,x+248,y+220,x+340,y+290),fill=ink,width=2)
    d.ellipse((x+334,y+284,x+346,y+296),outline=ink,width=2)
    d.text((x+76,y+190),'0'+str(views.index((key,title,x,y))+1),font=font(21,True),fill=ink)
d.rectangle((48,1478,2352,1546),fill=ink)
d.text((72,1495),'01 MANDIBLE   02 LIMB ARTICULATION   03 DORSAL MASS   04 LOCOMOTION  |  LIGHT AVERSION: DELAYED RESPONSE',font=font(24),fill='#dedec9')
im.save(out/'WalkerResearch_BaseColor.png')

n=1024;y,x=np.mgrid[:n,:n];u=x/n;v=y/n
row=np.floor(v*46);sx=(u*38+(row%2)*.5)%1;sy=(v*46)%1
edge=np.sqrt(((sx-.5)/.51)**2+((sy-.42)/.62)**2)
height=np.clip(1-edge,0,1)**.55
rng=np.random.default_rng(8472);grain=rng.random((n,n))
shade=.56+.34*height+.07*grain+.08*np.sin(u*21)*np.cos(v*14)
rgb=np.stack([shade*.53,shade*.68,shade*.51],axis=-1)
Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).save(out/'SpecimenSkin_BaseColor.png')
dx=(np.roll(height,-1,1)-np.roll(height,1,1))*.8;dy=(np.roll(height,-1,0)-np.roll(height,1,0))*.8
length=np.sqrt(1+dx*dx+dy*dy)
normal=np.stack([.5-dx/length*.5,.5-dy/length*.5,.5+.5/length],axis=-1)
Image.fromarray(np.uint8(normal*255)).save(out/'SpecimenSkin_Normal.png')
mask=np.zeros((n,n,4),dtype=np.uint8);mask[...,1]=240;mask[...,3]=np.uint8((.30+.20*height)*255)
Image.fromarray(mask).save(out/'SpecimenSkin_Mask.png')
print('Research poster and original scale albedo/normal/mask written.')
