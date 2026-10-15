"""Split the original imagegen mouth atlas into native material swatches."""
import hashlib,json
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[2];source=root/'ArtSource/Textures/CrawlerMouth_Imagegen.png'
image=Image.open(source).convert('RGB');width,height=image.size
out=root/'Assets/_Game/Art/Textures';out.mkdir(parents=True,exist_ok=True)
records=[]
for index,name in enumerate(('CrawlerTeeth','CrawlerGums','CrawlerThroat')):
    bounds=(0,index*height//3,width,(index+1)*height//3)
    target=out/(name+'_BaseColor.png');image.crop(bounds).save(target)
    records.append({'material':name,'crop':bounds,'runtime':target.relative_to(root).as_posix()})
(root/'ArtSource/Textures/CrawlerMouth_Imagegen.source.json').write_text(json.dumps({'tool':'built-in image_gen','source':source.relative_to(root).as_posix(),'sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'nativeSize':[width,height],'prompt':'ArtSource/Textures/CrawlerMouth_Imagegen.prompt.txt','materials':records},indent=2)+'\n',encoding='utf-8')
print('Prepared three native mouth material crops:',width,'x',height//3)
