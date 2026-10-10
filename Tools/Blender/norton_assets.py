"""CC0 Creomoto wreck and MakeHuman head/casualty, with original articulated legs and shotgun."""
import bpy,bmesh,math,sys
from pathlib import Path
from mathutils import Vector,Matrix,Euler
sys.path.insert(0,str(Path(__file__).parent))
import licensed_humans as H
from encounter_assets import clean,save,material,ellipsoid,tube

def uv(o):
    H.activate(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.02);bpy.ops.object.mode_set(mode='OBJECT')
def join(parts,name):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts:o.select_set(True)
    bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bpy.context.object.name=name
    return bpy.context.object
def weight(o,bone,rig):
    o.vertex_groups.new(name=bone).add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('Six-legged skeleton','ARMATURE');mod.object=rig;o.parent=rig

def head():
    bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/'hanging_survivor_01.blend'),load_ui=False,use_scripts=False)
    oldrig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');oldrig.animation_data_clear()
    for b in oldrig.pose.bones:b.matrix_basis.identity()
    for a in list(bpy.data.actions):bpy.data.actions.remove(a)
    bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();parts=[]
    origin=Vector((0,0,1.49));factor=2.65
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH' or any(any(s in m.name for s in ['Outfit','Boots','Hair','Brows']) for m in o.data.materials):continue
        mesh=bpy.data.meshes.new_from_object(o.evaluated_get(deps),preserve_all_data_layers=True,depsgraph=deps)
        mesh.transform(o.matrix_world);bm=bmesh.new();bm.from_mesh(mesh)
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if v.co.z<1.465],context='VERTS');bm.to_mesh(mesh);bm.free()
        if not mesh.polygons:continue
        for v in mesh.vertices:v.co=(v.co-origin)*factor+Vector((0,0,.51))
        if any(m and m.name in ['HumanEyes','CinderEyes'] for m in mesh.materials):
            for v in mesh.vertices:v.co.y+=.025
        if o.name=='Human':
            # Retain the anatomical UVs, with sunken sockets, wasted cheeks and
            # a slightly asymmetric sag instead of a neutral living face.
            for v in mesh.vertices:
                x,y,z=v.co
                if y<-.22:
                    socket=sum(math.exp(-((x-s*.12)/.065)**2-((z-.835)/.055)**2) for s in [-1,1])
                    cheek=math.exp(-((abs(x)-.17)/.09)**2-((z-.73)/.08)**2)
                    v.co.y+=socket*.029+cheek*.034
                    v.co.z-=cheek*(.022 if x>0 else .012)
                if z>1.0:v.co.x*=.95
            # The neutral MakeHuman lips share a sealed surface. Give the bite
            # a real aperture instead of stretching that surface over the teeth.
            bm=bmesh.new();bm.from_mesh(mesh)
            opening=[f for f in bm.faces if (lambda c:c.y<-.36 and (c.x/.087)**2+((c.z-.635)/.018)**2<1)(f.calc_center_median())]
            bmesh.ops.delete(bm,geom=opening,context='FACES');bm.to_mesh(mesh);bm.free()
        copy=bpy.data.objects.new('Cadaver '+o.name,mesh);bpy.context.collection.objects.link(copy);parts.append(copy)
        for v in mesh.vertices:
            g=copy.vertex_groups # Original evaluated mesh keeps indices but not object group names.
        for vg in o.vertex_groups:copy.vertex_groups.new(name=vg.name)
        for mat in list(mesh.materials):
            if mat and mat.name=='HumanSkin0':mat.name='CinderSkin'
            elif mat and mat.name=='HumanEyes':mat.name='CinderEyes'
    for o in list(bpy.context.scene.objects):
        if o not in parts:bpy.data.objects.remove(o,do_unlink=True)
    data=bpy.data.armatures.new('CinderHeadSkeleton');rig=bpy.data.objects.new('CinderHeadRig',data);bpy.context.collection.objects.link(rig);H.activate(rig);bpy.ops.object.mode_set(mode='EDIT')
    def bone(name,a,b,parent='Root'):
        e=data.edit_bones.new(name);e.head=a;e.tail=b
        if name!='Root':e.parent=data.edit_bones[parent]
    bone('Root',(0,0,.49),(0,0,.59),None);bone('Head',(0,0,.54),(0,0,1.05));bone('Jaw',(0,-.148,.744),(0,-.297,.514),'Head')
    legs=[]
    for side in [-1,1]:
        for n,y in enumerate([-.42,0,.45]):
            points=[(side*.19,y*.38,.60),(side*.67,y,.79),(side*.92,y+.08,.23),(side*1.02,y-.13,.025)]
            names=[f'Leg_{side}_{n}_{k}' for k in range(3)]
            for k in range(3):bone(names[k],points[k],points[k+1],'Root' if k==0 else names[k-1])
            legs.append((side,n,points,names))
    # Six tendons provide secondary deformation and bring the rig to 28 bones.
    for i in range(7):bone('Tendon_'+str(i),(0,0,.53),(0,.02,.60),'Head')
    bpy.ops.object.mode_set(mode='OBJECT')
    for o in parts:
        oldgroups={g.index:g.name for g in o.vertex_groups}
        values=[]
        for v in o.data.vertices:
            jaw=sum(g.weight for g in v.groups if oldgroups.get(g.group,'') in ['jaw','lowerlip01.L','lowerlip01.R','lowerlip02.L','lowerlip02.R'])
            # Keep the upper lip on the skull and the lower lip on the jaw.
            if v.co.y<-.32:
                if v.co.z>.644:jaw=0
                elif v.co.z<.628:jaw=max(jaw,.9)
            values.append(min(1,jaw))
        o.vertex_groups.clear();headgroup=o.vertex_groups.new(name='Head');jawgroup=o.vertex_groups.new(name='Jaw')
        for v,j in zip(o.data.vertices,values):
            headgroup.add([v.index],1-j,'REPLACE');jawgroup.add([v.index],j,'REPLACE')
        m=o.modifiers.new('Head and jaw','ARMATURE');m.object=rig;o.parent=rig
        for p in o.data.polygons:p.use_smooth=True
        if 'Human' in o.name and not any(x in o.name for x in ['short','high-poly','eyebrow']):
            H.activate(o);sub=o.modifiers.new('Facial contours','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
    skin=material('CinderLimb',(.29,.24,.22));gums=material('CinderGums',(.24,.025,.035));teeth=material('CinderTeeth',(.55,.45,.27));throat=material('CinderThroat',(.03,.006,.01));bone_mat=material('CinderBone',(.5,.44,.31))
    for side,n,points,names in legs:
        pts=[];radii=[];segments=[]
        for k in range(3):
            a,b=Vector(points[k]),Vector(points[k+1]);start=[.094,.058,.030][k];end=[.058,.030,.009][k]
            for step in range(10):
                t=step/10;pts.append(a.lerp(b,t));radii.append((start*(1-t)+end*t)*(1+.25*math.sin(t*math.pi)**2));segments.append((k,t))
        pts.append(Vector(points[-1]));radii.append(.003);segments.append((2,1))
        ob=tube('Continuous fleshy leg',pts,radii,skin,24)
        groups=[ob.vertex_groups.new(name=name) for name in names]
        for ring,(k,t) in enumerate(segments):
            blend=max(0,(t-.78)/.22) if k<2 else 0
            for vertex in range(ring*24,(ring+1)*24):
                groups[k].add([vertex],1-blend,'REPLACE')
                if blend>0:groups[k+1].add([vertex],blend,'REPLACE')
        m=ob.modifiers.new('Continuous limb weights','ARMATURE');m.object=rig;ob.parent=rig
        H.activate(ob);sub=ob.modifiers.new('Organic limb transitions','SUBSURF');sub.levels=1;bpy.ops.object.modifier_apply(modifier=sub.name)
        for k in range(2):
            a,b=Vector(points[k]),Vector(points[k+1]);offset=Vector((0,-.043,.022))
            ridge=tube('Raised stretched tendon',[a.lerp(b,t)+offset for t in [.18,.35,.6,.83]],[.005,.009,.007,.003],skin,10);weight(ridge,names[k],rig)
        claw=tube('Exposed hooked claw',[Vector(points[-1])+Vector((0,.025,.03)),Vector(points[-1]),Vector(points[-1])+Vector((side*.07,-.08,0))],[.014,.01,.0015],bone_mat,12);weight(claw,names[-1],rig)
    mouth=ellipsoid('Torn mouth interior',(0,-.36,.608),(.085,.035,.12),throat);weight(mouth,'Head',rig)
    for row in [0,1]:
        lippts=[(x,-.411+2*x*x,(.654 if row==0 else .613)+(.014 if row==0 else -.012)*math.cos(x*math.pi/.19)) for x in [-.091,-.073,-.05,-.025,0,.025,.05,.073,.091]]
        lip=tube('Torn necrotic lip',lippts,[.011,.014,.013,.014,.013,.012,.014,.011,.008],gums,12);weight(lip,'Head' if row==0 else 'Jaw',rig)
        count=13 if row==0 else 11
        for i in range(count):
            x=(i-(count-1)/2)*.0135;y=-.431+2*x*x;z=.652 if row==0 else .608;sgn=-1 if row==0 else 1
            length=.026+.008*math.sin(i*2.7)**2
            ob=tube('Crooked pointed rotten tooth',[(x,y,z),(x+.002*math.sin(i*3),y-.005,z+sgn*length*.7),(x+.003*math.sin(i),y-.007,z+sgn*length)],[.0057,.0042,.001],teeth,12);weight(ob,'Head' if row==0 else 'Jaw',rig)
    stump=tube('Severed neck',[(0,0,.51),(0,.04,.47),(0,.05,.41)],[.19,.15,.10],gums,24);weight(stump,'Root',rig)
    # One weighted surface avoids a draw call for every tooth and joint.
    join([o for o in bpy.context.scene.objects if o.type=='MESH'],'CinderHeadSurface')
    rig.animation_data_create();bpy.context.scene.render.fps=30
    for name,length in [('Head_Idle',60),('Head_Run',30),('Head_Bite',36),('Head_Spit',48),('Head_Death',96)]:
        act=bpy.data.actions.new(name);rig.animation_data.action=act
        for frame in range(1,length+2):
            t=(frame-1)/30
            for b in rig.pose.bones:b.matrix_basis.identity();b.rotation_mode='XYZ'
            if name=='Head_Death':
                q=min(1,t/1.2);q=q*q*(3-2*q)
                # Absolute armature-space matrices account for Blender bones'
                # local Y axis. Keep the folded feet above ground while the head
                # rolls onto its cheek; key all translations and scales.
                root=rig.pose.bones['Root'];rest=root.bone.matrix_local
                root.matrix=Matrix.LocRotScale(rest.translation+Vector((0,0,-.30*q)),Euler((0,0,-.10*q)).to_quaternion()@rest.to_quaternion(),Vector((1,1,1)))
                bpy.context.view_layer.update()
                hb=rig.pose.bones['Head'];rest=hb.bone.matrix_local
                hb.matrix=Matrix.LocRotScale(rest.translation.lerp(Vector((.12,-.14,.36)),q),Euler((.25*q,1.3*q,-.14*q)).to_quaternion()@rest.to_quaternion(),Vector((1,1,1)))
                bpy.context.view_layer.update()
            else:
                rig.pose.bones['Root'].location.z=math.sin(t*math.tau*(1 if name=='Head_Idle' else 2))*.012
                rig.pose.bones['Head'].rotation_euler.z+=math.sin(t*3)*.045
            jaw=rig.pose.bones['Jaw']
            jaw.rotation_euler.x=.14+math.sin(t*2)*.035 if name in ['Head_Idle','Head_Run'] else .72*max(0,math.sin(math.pi*min(1,t/1.2))) if name=='Head_Bite' else .8*max(0,math.sin(math.pi*min(1,t/1.6))) if name=='Head_Spit' else .36
            for side,n,points,names in legs:
                phase=t*math.tau+(0 if (n+(side>0))%2==0 else math.pi)
                for k,nameb in enumerate(names):
                    b=rig.pose.bones[nameb]
                    if name=='Head_Run':b.rotation_euler.z=side*math.sin(phase)*(.27 if k==0 else .08);b.rotation_euler.x=max(0,math.sin(phase))*(.28 if k==1 else -.22)
                    elif name=='Head_Death':
                        y=[-.42,0,.45][n];endpoints=[Vector((side*.19,y*.38,.28)),Vector((side*.46,y+.04,.20)),Vector((side*.74,y-.12,.085)),Vector((side*(.97+n*.035),y-.18,.033))]
                        a=Vector(points[k]).lerp(endpoints[k],q);c=Vector(points[k+1]).lerp(endpoints[k+1],q)
                        twitch=math.sin(t*22+n)*.012*max(0,1-t/2.2)
                        if k==1:a.z+=twitch
                        direction=c-a;original=Vector(points[k+1])-Vector(points[k])
                        b.matrix=Matrix.LocRotScale(a,original.rotation_difference(direction)@b.bone.matrix_local.to_quaternion(),Vector((1,direction.length/original.length,1)))
                        bpy.context.view_layer.update()
                    elif name=='Head_Bite' and n==0:b.rotation_euler.x=-.23*max(0,math.sin(t*math.pi/1.2))
            for b in rig.pose.bones:
                b.keyframe_insert('rotation_euler',frame=frame);b.keyframe_insert('location',frame=frame);b.keyframe_insert('scale',frame=frame)
        act.use_fake_user=True
    rig.animation_data.action=bpy.data.actions['Head_Idle'];bpy.context.scene.frame_set(1);bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=61
    save('cinder_head_01',True)

def casualty():
    bpy.ops.wm.open_mainfile(filepath=str(H.SOURCE/'player_body_01.blend'),load_ui=False,use_scripts=False)
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True);continue
        # The standing player source also carries a crouch morph. A casualty
        # is a fixed pose; remove that morph before opening the abdomen.
        if o.data.shape_keys:o.shape_key_clear()
        bm=bmesh.new();bm.from_mesh(o.data)
        # Open the front of the work suit and the abdominal surface together.
        cut=[v for v in bm.verts if ((v.co.x/.135)**2+((v.co.z-1.08)/.18)**2)<1 and v.co.y<-.006]
        bmesh.ops.delete(bm,geom=cut,context='VERTS');bm.to_mesh(o.data);bm.free()
    back=max(v.co.y for o in bpy.context.scene.objects if o.type=='MESH' for v in o.data.vertices)
    flesh=material('Viscera',(.31,.018,.03));dark=material('CrawlerThroat',(.035,.002,.006))
    cavity=ellipsoid('Abdominal cavity',(0,-.05,1.08),(.14,.033,.185),dark)
    points=[(.141*math.cos(a),-.095,1.08+.181*math.sin(a)) for a in [n*math.tau/64 for n in range(65)]]
    tube('Torn abdominal rim',points,[.024]*len(points),flesh,12)
    # A continuous intestine coils through the wound and onto the ground.
    pts=[]
    for i in range(160):
        t=i/159;fall=max(0,(t-.65)/.35);y=(-.105-.035*math.sin(t*math.pi*5))*(1-fall)+(back-.008)*fall
        pts.append((.105*math.sin(t*math.pi*9)+.27*max(0,(t-.62)/.38),y,1.20-.26*t-.17*max(0,(t-.6)/.4)))
    tube('Exposed continuous intestines',pts,[.025+.005*math.sin(i*.8) for i in range(len(pts))],flesh,12)
    for n in range(4):ellipsoid('Torn tissue',(n*.034-.05,-.10,1.12),(.043,.028,.073),flesh)
    ob=join([o for o in bpy.context.scene.objects if o.type=='MESH'],'eviscerated_casualty_01')
    ob.data.transform(Matrix.Rotation(-math.pi/2,4,'X'));low=min(v.co.z for v in ob.data.vertices)
    ob.data.transform(Matrix.Translation((0,0,.025-low)));save('eviscerated_casualty_01')

def wreck():
    bpy.ops.wm.open_mainfile(filepath=str(H.ROOT/'ArtSource/Vendor/creomoto/car.blend'),load_ui=False,use_scripts=False)
    body=material('BurnedSteel',(.052,.049,.044));rubber=material('BurnedRubber',(.015,.014,.013));glass=material('BurnedGlass',(.029,.035,.033))
    for o in list(bpy.context.scene.objects):
        if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True);continue
        H.activate(o);bpy.ops.object.convert(target='MESH');bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
        o.data.materials.clear();o.data.materials.append(rubber if o.name.startswith('Cylinder') else body)
        for v in o.data.vertices:
            v.co*=.68
            # Crumpled nose and collapsed cabin without changing wheel anatomy.
            if not o.name.startswith('Cylinder') and v.co.z>.65:v.co.z-=.12*max(0,1-abs(v.co.y)/2);v.co.x+=.025*math.sin(v.co.y*13+v.co.z*7)
        uv(o)
    ob=join([o for o in bpy.context.scene.objects if o.type=='MESH'],'burning_car_01');low=min(v.co.z for v in ob.data.vertices);ob.data.transform(Matrix.Translation((0,0,-low+.02)))
    save('burning_car_01')

def shotgun():
    clean();wood=material('ShotgunWood',(.23,.11,.047));steel=material('ShotgunSteel',(.08,.085,.085));dark=material('BurnedRubber',(.01,.01,.01))
    # Blender -Y is the muzzle direction and exports as Unity +Z.
    for x in [-.018,.018]:tube('Double barrel',[(x,-.16,0),(x,-.86,0)],[.020,.018],steel,20)
    tube('Breech',[(0,.03,0),(0,-.21,0)],[.050,.043],steel,18)
    tube('Walnut stock',[(0,.43,-.025),(0,.35,-.026),(0,.18,-.026),(0,.02,-.005)],[.047,.052,.04,.026],wood,18)
    tube('Fore-end',[(0,-.22,-.04),(0,-.41,-.04),(0,-.55,-.04)],[.033,.034,.025],wood,18)
    ring=[(0,.047-.056*math.cos(a),-.055-.035*math.sin(a)) for a in [n*math.tau/30 for n in range(31)]]
    tube('Trigger guard',ring,[.004]*len(ring),steel,8);tube('Trigger',[(0,.04,-.03),(0,.06,-.065)],[.004,.003],steel,8)
    for x in [-.018,.018]:ellipsoid('Dark bore',(x,-.865,0),(.013,.001,.013),dark)
    join([o for o in bpy.context.scene.objects if o.type=='MESH'],'norton_shotgun_01');save('norton_shotgun_01')

def gunhand():
    import player_presence
    player_presence.grip(trigger=True)

if __name__=='__main__':
    task=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'all'
    if task not in ['head','casualty','wreck','shotgun','gunhand']:task='all'
    for name,fn in [('head',head),('casualty',casualty),('wreck',wreck),('shotgun',shotgun),('gunhand',gunhand)]:
        if task in ['all',name]:fn();print('NORTON ASSET',name,flush=True)
