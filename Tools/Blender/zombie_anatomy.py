"""Original half-skull fitted to the CC0 NewDLC wolf, weighted to its head/jaw."""
import bpy,bmesh,math
import numpy as np
from mathutils import Vector
from pathlib import Path
from wolf_bone_material import apply_bone_surface

def add_half_skull(mesh,rig,material,save_image):
    # Remove the actual fur/eye geometry from one side, retaining original UVs.
    skull_groups={g.index for g in mesh.vertex_groups if g.name in {'b_Head','b_Jaw','b_LeftEye','b_RightEye'}}
    weights={v.index:sum(g.weight for g in v.groups if g.group in skull_groups) for v in mesh.data.vertices}
    bm=bmesh.new();bm.from_mesh(mesh.data);bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table()
    removed=[f for f in bm.faces if f.calc_center_median().x>.001 and sum(weights[v.index] for v in f.verts)/len(f.verts)>.64]
    bmesh.ops.delete(bm,geom=removed,context='FACES');bm.to_mesh(mesh.data);bm.free();mesh.data.update()
    bone=material('WolfBone',(.48,.46,.39,1),.73);gum=material('WolfTissue',(.15,.016,.022,1),.53);dark=material('WolfSocket',(.003,.001,.002,1),1)
    size=1024;rng=np.random.default_rng(2318);y,x=np.mgrid[0:size,0:size]/size
    mottled=.055*np.sin(x*77+np.sin(y*43))+.035*np.sin(y*121+x*64)+rng.normal(0,.014,(size,size))
    pixels=np.ones((size,size,4),np.float32);pixels[:,:,:3]=np.clip(np.array([.48,.46,.39])+mottled[:,:,None],0,1)
    albedo=save_image('WolfBone_BaseColor',pixels)
    normal=np.ones_like(pixels);normal[:,:,:3]=(.5,.5,1);save_image('WolfBone_Normal',normal)
    mask=np.ones_like(pixels);mask[:,:,0]=0;mask[:,:,2]=.5;mask[:,:,3]=.19;save_image('WolfBone_Mask',mask)
    t=bone.node_tree.nodes.new('ShaderNodeTexImage');t.image=albedo;bone.node_tree.links.new(t.outputs['Color'],bone.node_tree.nodes.get('Principled BSDF').inputs['Base Color'])
    apply_bone_surface(bone,Path(__file__).resolve().parents[2])
    def sphere(name,center,scale,mat):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=32,ring_count=20,location=center);o=bpy.context.object;o.name=name;o.scale=scale
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat);return o
    def activate(o):
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    def join(objects,name):
        bpy.ops.object.select_all(action='DESELECT')
        for o in objects:o.select_set(True)
        bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name=name;return o
    def boolean(o,cutter):
        activate(o);m=o.modifiers.new('Anatomical opening','BOOLEAN');m.operation='DIFFERENCE';m.solver='EXACT';m.object=cutter
        bpy.ops.object.modifier_apply(modifier=m.name);bpy.data.objects.remove(cutter,do_unlink=True)
    def weight(o,bone_name):
        activate(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        for f in o.data.polygons:f.use_smooth=True
        if o.name=='Exposed canine cranium':
            for v in o.data.vertices:
                p=v.co;v.co+=v.normal*(.0003*math.sin(p.x*622+p.y*171)*math.sin(p.z*339)+.0001*math.sin(p.y*913))
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
        # Joined meshes match UV layers by name. Use the body's shader channel,
        # otherwise the appended skull has zero UVs in Unity's primary channel.
        o.data.uv_layers.active.name=mesh.data.uv_layers[0].name
        o.data.uv_layers.active.active_render=True
        o.vertex_groups.new(name=bone_name).add(list(range(len(o.data.vertices))),1,'REPLACE')
        mod=o.modifiers.new('Wolf anatomy skeleton','ARMATURE');mod.object=rig
        return o
    upper=join([sphere('Cranium',(0,-.493,.952),(.110,.135,.115),bone),sphere('Maxilla',(0,-.617,.868),(.078,.113,.055),bone),sphere('Nasal bridge',(0,-.589,.930),(.060,.113,.063),bone)],'Exposed canine cranium')
    activate(upper);m=upper.modifiers.new('Continuous bone','REMESH');m.mode='VOXEL';m.voxel_size=.006;bpy.ops.object.modifier_apply(modifier=m.name)
    m=upper.modifiers.new('Bone contour','SMOOTH');m.factor=.65;m.iterations=3;bpy.ops.object.modifier_apply(modifier=m.name)
    boolean(upper,sphere('Orbit',( .10,-.555,.975),(.065,.075,.061),bone))
    boolean(upper,sphere('Nasal cavity',(0,-.731,.881),(.045,.055,.029),bone))
    bm=bmesh.new();bm.from_mesh(upper.data)
    # Half skull, rather than a helmet covering the surviving side.
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.0001,plane_co=upper.matrix_world.inverted()@Vector((-.004,0,0)),plane_no=(1,0,0),clear_inner=True)
    bm.to_mesh(upper.data);bm.free();upper=weight(upper,'b_Head')
    def tube(name,points,radii,mat,sides=12):
        vertices=[];faces=[]
        for i,p in enumerate(points):
            direction=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
            direction.normalize();axis=direction.cross(Vector((1,0,0)))
            if axis.length<.01:axis=direction.cross(Vector((0,0,1)))
            axis.normalize();second=direction.cross(axis).normalized()
            for j in range(sides):vertices.append(Vector(p)+radii[i]*(axis*math.cos(j*math.tau/sides)+second*math.sin(j*math.tau/sides)))
        for i in range(len(points)-1):
            for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
        faces.extend([tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))])
        data=bpy.data.meshes.new(name);data.from_pydata(vertices,[],faces);o=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(o);data.materials.append(mat);return o
    # Canine orbital arch, elongated mandible and carnassial/canine teeth.
    parts=[upper]
    parts.append(weight(tube('Zygomatic arch',[(.080,-.467,.987),(.126,-.51,.916),(.109,-.583,.914),(.076,-.618,.895)],[.014,.015,.014,.012],bone),'b_Head'))
    parts.append(weight(sphere('Recessed orbital cavity',(.054,-.54,.965),(.006,.044,.039),dark),'b_Head'))
    parts.append(weight(tube('Exposed mandible',[(.080,-.477,.913),(.107,-.501,.818),(.080,-.613,.798),(.042,-.717,.822),(.002,-.739,.824)],[.020,.020,.017,.010,.008],bone),'b_Jaw'))
    for row,b in [('upper','b_Head'),('lower','b_Jaw')]:
        for n in range(9):
            t=n/8;y=-.72+t*.195;x=.018+.062*math.sin(t*math.pi*.7);z=.860 if row=='upper' else .824
            length=.045 if n==2 else .016+(n%3)*.004;direction=-1 if row=='upper' else 1
            parts.append(weight(tube('Exposed '+row+' canine tooth',[(x,y,z),(x*.98,y-.002,z+direction*length*.65),(x*.98,y-.004,z+direction*length)],[.008,.005,.0008],bone,10),b))
    # Ragged living tissue along the midline and behind the exposed cheek.
    points=[(.001+math.sin(n*2)*.006,-.73+n*.022,.89+.16*math.sin(n/12*math.pi)) for n in range(13)]
    parts.append(weight(tube('Continuous ragged tissue margin',points,[.005+.0015*math.sin(n*3) for n in range(13)],gum,10),'b_Head'))
    parts.append(weight(tube('Torn cheek tendon',[(.112,-.487,.928),(.118,-.53,.847),(.081,-.60,.825)],[.009,.006,.004],gum),'b_Jaw'))
    # Joining retains vertex groups and original fur UVs, one weighted renderer.
    result=join([mesh]+parts,'Undead wolf half skull');activate(result)
    for mod in list(result.modifiers):
        if mod.type=='ARMATURE':result.modifiers.remove(mod)
    mod=result.modifiers.new('Weighted wolf anatomy','ARMATURE');mod.object=rig
    bm=bmesh.new();bm.from_mesh(result.data);bmesh.ops.triangulate(bm,faces=list(bm.faces));bm.to_mesh(result.data);bm.free();result.data.update()
    print('HALF SKULL: removed fur faces',len(removed),'weighted vertices',len(result.data.vertices),flush=True)
    return result
