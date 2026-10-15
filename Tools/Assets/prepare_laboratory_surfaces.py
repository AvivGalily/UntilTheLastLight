"""Prepare research PBR data maps, optical films and authored equipment markings.

The three imagegen colour sources remain unmodified. Normal/metal/AO/smoothness
maps are technical derivatives; optical films and labels are deterministic assets.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/_Game/Art/Textures'
RNG=np.random.default_rng(100915)

def noise(n,grid):
    a=RNG.random((grid,grid)).astype(np.float32)
    p=np.arange(n,dtype=np.float32)*grid/n;i=p.astype(int);t=p-i;t=t*t*(3-2*t)
    return ((a[i[:,None]%grid,i[None,:]%grid]*(1-t[:,None])+a[(i[:,None]+1)%grid,i[None,:]%grid]*t[:,None])*(1-t[None,:])
            +(a[i[:,None]%grid,(i[None,:]+1)%grid]*(1-t[:,None])+a[(i[:,None]+1)%grid,(i[None,:]+1)%grid]*t[:,None])*t[None,:])

def save(name,a):
    Image.fromarray(np.uint8(np.clip(a,0,1)*255)).save(OUT/(name+'.png'))

def maps(key,height,metal,smooth,ao=1,strength=1):
    dx=(np.roll(height,-1,1)-np.roll(height,1,1))*strength
    dy=(np.roll(height,-1,0)-np.roll(height,1,0))*strength
    length=np.sqrt(1+dx*dx+dy*dy)
    save(key+'_Normal',np.stack([.5-dx/length*.5,.5-dy/length*.5,.5+.5/length],-1))
    packed=np.zeros((*height.shape,4),np.float32)
    packed[...,0]=metal;packed[...,1]=ao;packed[...,3]=smooth
    save(key+'_Mask',packed)

for key in ('LabSteel','LabEnamel','LabRubber'):
    source=Image.open(OUT/(key+'_BaseColor.png')).convert('RGB')
    rgb=np.asarray(source,dtype=np.float32)/255;lum=rgb@np.array([.2126,.7152,.0722],np.float32)
    blurred=np.asarray(source.convert('L').filter(ImageFilter.GaussianBlur(4)),dtype=np.float32)/255
    relief=lum-blurred
    if key=='LabSteel':
        oxide=np.clip((rgb[...,0]-rgb[...,2]-.012)*10,0,1)
        maps(key,relief-oxide*.1,.9-oxide*.78,.68-oxide*.32+relief*.35,.99-oxide*.13,1.4)
    elif key=='LabEnamel':
        chip=np.clip((.53-lum)*9,0,1)
        maps(key,relief*.6-chip*.14,chip*.65,.43-chip*.13+relief*.2,.99-chip*.12,2.4)
    else:maps(key,relief,0,.22+np.clip(lum*1.5,0,.15),.96,2.4)

n=1024;y,x=np.mgrid[:n,:n];u=x/n;v=y/n
broad=noise(n,9);medium=noise(n,47);fine=noise(n,211)
# Sparse condensation/wipe film rather than a uniformly opaque tank wall.
streak=np.clip(np.sin(u*389+medium*2.1)-.75,0,1)*np.clip(broad-.3,0,1)
smudge=np.clip((noise(n,21)-.63)*2.7,0,1)
waterline=np.exp(-((v-.92)/.009)**2)*(.6+medium*.4)
droplets=np.clip((fine-.72)*3,0,1)**2
film=np.clip(.045+smudge*.12+streak*.3+waterline*.10+droplets*.10,.025,.24)
rgba=np.ones((n,n,4),np.float32);rgba[...,:3]=np.stack([.63+medium*.10,.77+medium*.08,.78+medium*.09],-1);rgba[...,3]=film
save('LabGlass_BaseColor',rgba);maps('LabGlass',smudge*.12+streak*.18+droplets*.12,0,.94-smudge*.3-waterline*.25,.99,1.1)

rgba[...,:3]=np.array([.12,.44,.59]);rgba[...,3]=.09+droplets*.04
save('LabFluid_BaseColor',rgba);maps('LabFluid',medium*.015,0,.85,1,.12)

cloud=noise(n,13);vein=np.clip(1-np.abs(np.sin(u*49+v*33+medium*8))*8,0,1)
reagent=np.stack([.10+cloud*.16,.17+cloud*.23,.07+cloud*.11],-1)*(1-vein[...,None]*.20)
save('LabReagent_BaseColor',reagent);maps('LabReagent',medium*.18+fine*.035,0,.54+cloud*.16,.94,1.5)

# Wet, mottled dermis retains the source specimen's overlapping scale pattern.
row=np.floor(v*43+medium*.28);sx=(u*36+(row%2)*.5+medium*.11)%1;sy=(v*43+medium*.28)%1
edge=np.sqrt(((sx-.5)/.53)**2+((sy-.43)/.65)**2)
scales=np.clip(1-edge,0,1)**.7
mottling=.52+noise(n,6)*.35+medium*.15
skin=np.stack([(.27+scales*.11)*mottling,(.34+scales*.16)*mottling,(.22+scales*.12)*mottling],-1)
spots=np.clip((noise(n,63)-.69)*3.5,0,1);skin*=1-spots[...,None]*.43
skin[...,0]+=vein*.014
save('LabSpecimenSkin_BaseColor',skin);maps('LabSpecimenSkin',scales*.40+fine*.025,0,.30+medium*.19,1-(1-scales)*.12,1.2)

# Smaller folds and pores on the broad ventral scutes, instead of a flat colour.
fold=np.sin(v*126+medium*2.5)*.5+.5
scute=(.79+cloud*.16+fine*.05)[...,None]*np.array([.46,.49,.32])
scute*=1-np.clip((.18-fold)*2,0,.18)[...,None]
save('LabScute_BaseColor',scute);maps('LabScute',fold*.035+fine*.018,0,.28+medium*.12,.98,.9)

paper=noise(n,233)*.04+noise(n,13)*.05
maps('LabPaper',paper,0,.14+medium*.06,.99,.55)

for key in ('LabAbyssSkin','LabPreservedTissue'):
    path=OUT/(key+'_BaseColor.png')
    if not path.exists():continue
    source=Image.open(path).convert('RGB');rgb=np.asarray(source,dtype=np.float32)/255
    lum=rgb@np.array([.2126,.7152,.0722],np.float32)
    blur=np.asarray(source.convert('L').filter(ImageFilter.GaussianBlur(5)),dtype=np.float32)/255
    detail=lum-blur
    maps(key,detail,0,np.clip(.36+detail*.8,.25,.56),np.clip(.97+detail*.2,.90,1),2.8 if key=='LabAbyssSkin' else 1.4)

n=512;y,x=np.mgrid[:n,:n];u=x/n-.5;v=y/n-.5
angle=np.arctan2(v,u);radius=np.sqrt(u*u+v*v)
fibre=(np.sin(angle*137+radius*49)+np.sin(angle*231-radius*21))*.5
iris=np.stack([.40+fibre*.12,.32+fibre*.10,.12+fibre*.05],-1)
iris*=np.clip((radius-.12)*17,0,1)[...,None]
iris*=np.clip((.51-radius)*18,0,1)[...,None]
save('LabIris_BaseColor',iris);maps('LabIris',fibre*.015,0,.72,1,.6)

# UV-authored markings are crisp and readable at a hand-sized physical scale.
def font(size,bold=False):
    return ImageFont.truetype('C:/Windows/Fonts/'+('consolab.ttf' if bold else 'consola.ttf'),size)

labels=Image.new('RGBA',(2048,512),(0,0,0,0));d=ImageDraw.Draw(labels)
texts=[('BIO-CONTAINMENT','R-09 / SEALED SYSTEM','AUTHORIZED PERSONNEL'),('BLACKPINE LAB','PRESSURE 1.8 BAR','INSPECTION 03 / 09'),('SAMPLE R-09','TISSUE EXTRACT','OBSERVATION / 07'),('CAUTION','BIOLOGICAL MATERIAL','KEEP CONTAINER SEALED')]
for i,(title,line,footer) in enumerate(texts):
    ox=i*512;colour=(195,182,123,255) if i in (0,3) else (203,208,190,255)
    d.rounded_rectangle((ox+8,8,ox+504,504),radius=15,fill=colour,outline=(33,43,40,255),width=12)
    d.rectangle((ox+25,28,ox+487,111),fill=(31,42,39,255))
    d.text((ox+37,48),title,font=font(36,True),fill=colour)
    d.text((ox+35,148),line,font=font(28,True),fill=(35,43,36,255))
    d.text((ox+35,201),footer,font=font(22),fill=(53,58,45,255))
    for j in range(88):
        bx=ox+40+j*4
        if (j*37)%11<6:d.rectangle((bx,283,bx+1+(j%2),373),fill=(40,45,37,255))
    d.text((ox+36,406),'BP / 09-03 / ARCHIVE',font=font(26),fill=(46,51,40,255))
    for j in range(140):
        px=ox+int(RNG.integers(20,490));py=int(RNG.integers(20,490));d.line((px,py,px+int(RNG.integers(2,14)),py),fill=(*colour[:3],210),width=1)
labels.save(OUT/'LabLabels_BaseColor.png')

# An instrument face with etched scale marks and fastener recesses.
panel=Image.new('RGB',(1280,736),(56,66,63));d=ImageDraw.Draw(panel)
for py in range(736):
    c=55+int(RNG.integers(0,5));d.line((0,py,1280,py),fill=(c,c+10,c+7))
d.rounded_rectangle((13,13,1267,723),radius=22,outline=(102,111,103),width=4)
d.text((45,27),'BLACKPINE / ANALOG BIO-MONITOR',font=font(33,True),fill=(190,192,161))
for yy,label in zip([164,285,406,527],['GAIN','FLOW','O2','TEMP']):
    d.text((1098,yy),label,font=font(26),fill=(193,194,165))
    for j in range(9):
        a=(j/8*.75+.125)*np.pi*2;cx=1010;cy=yy+35
        d.line((cx+np.cos(a)*47,cy+np.sin(a)*47,cx+np.cos(a)*55,cy+np.sin(a)*55),fill=(165,176,155),width=2)
for px in [32,1248]:
    for py in [32,704]:d.ellipse((px-10,py-10,px+10,py+10),fill=(25,31,29),outline=(126,135,123),width=2)
d.text((45,665),'MODEL 03 / CONTINUOUS MONITORING',font=font(25),fill=(164,172,148))
panel.save(OUT/'LabInstrumentPanel_BaseColor.png')

manifest={'mode':'built-in image_gen for the three retained colour sources; deterministic PBR data/optical maps and instrument markings',
          'generatedColourSources':['LabSteel','LabEnamel','LabRubber']+[key for key in ['LabAbyssSkin','LabPreservedTissue','LabAbyssDermisV2'] if (OUT/(key+'_BaseColor.png')).exists()],
          'maps':'Normal RGB; Mask R metallic, G ambient occlusion, A smoothness; colour alpha stores optical film opacity where present',
          'textureDirectory':'Assets/_Game/Art/Textures','tool':'Tools/Assets/prepare_laboratory_surfaces.py',
          'materials':sorted(p.name for p in OUT.glob('Lab*.png'))}
(ROOT/'ArtSource/Research/Materials/Manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
print('Research material maps, optical films and equipment markings prepared.')
