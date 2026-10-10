"""Continuous face closure with rigid tooth motion, avoiding folded cheek triangles."""
import bpy,math
from pathlib import Path
from mathutils import Vector

def add_bite_shape(skin,tooth_groups=None):
    keys=skin.data.shape_keys.key_blocks
    old=keys.get('Jaw_Bite')
    if old:skin.shape_key_remove(old)
    closed=skin.shape_key_add(name='Jaw_Bite',from_mix=False);basis=skin.data.shape_keys.key_blocks['Basis']
    def smooth(a,b,x):
        t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
    def close(p):
        mask=smooth(.69,.88,p.z)*smooth(1.06,1.23,p.y)*(1-smooth(.21,.34,abs(p.x)))
        gap=max(0,1.67-p.y)
        return Vector((p.x,p.y+gap*.60*mask,p.z+.026*mask*min(1,gap/.4)))
    changed=0
    for i,v in enumerate(basis.data):
        p=Vector((-v.co.x,v.co.z,-v.co.y))
        q=close(p);closed.data[i].co=Vector((-q.x,-q.z,q.y));changed+=int((q-p).length>.00001)
    # Teeth move as solid pieces, retaining their pointed silhouette during closure.
    if tooth_groups is None:
        slot=next(i for i,m in enumerate(skin.data.materials) if m.name.split('.')[0]=='CrawlerTeeth')
        faces=[p for p in skin.data.polygons if p.material_index==slot];adj={i:set() for p in faces for i in p.vertices}
        for f in faces:
            for i in f.vertices:adj[i].update(f.vertices)
        remaining=set(adj);tooth_groups=[]
        while remaining:
            stack=[remaining.pop()];group=set(stack)
            while stack:
                for i in adj[stack.pop()]:
                    if i in remaining:remaining.remove(i);group.add(i);stack.append(i)
            tooth_groups.append(group)
    for group in tooth_groups:
        center=sum((basis.data[i].co for i in group),Vector())/len(group);p=Vector((-center.x,center.z,-center.y));q=close(p)
        delta=Vector((-q.x,-q.z,q.y))-center
        for i in group:closed.data[i].co=basis.data[i].co+delta
    closed.value=0;print('Jaw_Bite continuous closure vertices:',changed,'rigid teeth:',len(tooth_groups),flush=True)

if __name__=='__main__':
    root=Path(__file__).resolve().parents[2];file=root/'ArtSource/Blender/walker_01.blend'
    bpy.ops.wm.open_mainfile(filepath=str(file),load_ui=False,use_scripts=False)
    skin=next(o for o in bpy.context.scene.objects if o.type=='MESH' and o.data.shape_keys)
    add_bite_shape(skin);bpy.ops.wm.save_as_mainfile(filepath=str(file),compress=True)
