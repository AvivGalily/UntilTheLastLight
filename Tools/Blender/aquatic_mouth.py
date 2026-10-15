"""R-09 oral anatomy: a tapered rostrum, open mandible and recessed buccal sac.

All coordinates are game metres. Oral surfaces are authored surfaces, not balls
covering the face; the cheek lining blends between the head and hinged jaw.
"""
import math
import random
import bpy


def mesh(F, name, vertices, faces, material):
    data = bpy.data.meshes.new(name)
    # F.C changes handedness. Preserve the authored outward normals before
    # boolean operations, not only in the final FBX normals cleanup.
    data.from_pydata([F.C(p) for p in vertices], [], [tuple(reversed(f)) for f in faces])
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    return F.track(obj, name, material)


def loft(F, name, sections, material, sides=48):
    """Closed, softly squared cross sections: z, half-width, top, bottom."""
    vertices = []
    faces = []
    for z, width, top, bottom in sections:
        for j in range(sides):
            a = j * math.tau / sides
            x = math.copysign(abs(math.cos(a))**.78, math.cos(a)) * width
            y = (top+bottom)/2 + math.copysign(abs(math.sin(a))**.72, math.sin(a)) * (top-bottom)/2
            vertices.append((x, y, z))
    for k in range(len(sections)-1):
        for j in range(sides):
            a = k*sides+j; b = k*sides+(j+1)%sides
            faces.append((a, b, b+sides, a+sides))
    faces.extend([tuple(range(sides-1,-1,-1)), tuple((len(sections)-1)*sides+j for j in range(sides))])
    obj=mesh(F, name, vertices, faces, material)
    # Twisted loft quads must be resolved before the boolean solver sees them.
    bpy.context.view_layer.objects.active=obj
    triangulate=obj.modifiers.new('Explicit loft topology','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=triangulate.name)
    return obj


def subtract(skin,cutter,label):
    # Exact CSG can silently return an empty mesh at a near-coplanar contact.
    # Preserve the body and retry the floating-point solver in that case.
    original=skin.data.copy();count=len(original.vertices)
    solvers=bpy.types.BooleanModifier.bl_rna.properties['solver'].enum_items.keys()
    try:
        for solver in ('EXACT','FLOAT' if 'FLOAT' in solvers else 'FAST'):
            bpy.context.view_layer.objects.active=skin
            cut=skin.modifiers.new(label,'BOOLEAN');cut.operation='DIFFERENCE';cut.solver=solver;cut.object=cutter
            bpy.ops.object.modifier_apply(modifier=cut.name)
            if len(skin.data.vertices)>=count*.75:
                return
            broken=skin.data;skin.data=original.copy();bpy.data.meshes.remove(broken)
            print('R09 boolean retry:',label,solver,flush=True)
        raise RuntimeError('Oral/gill subtraction removed the creature body: '+label)
    finally:
        bpy.data.meshes.remove(original)


def membrane(obj, thickness=.0015):
    # A closed lining has a real inner and outer face. This also keeps the oral
    # cavity opaque under Unity's back-face culling in a fully opened pose.
    bpy.context.view_layer.objects.active=obj
    solid=obj.modifiers.new('Living membrane thickness','SOLIDIFY')
    solid.thickness=thickness;solid.offset=0
    bpy.ops.object.modifier_apply(modifier=solid.name)


def upper_face(F):
    # A bony wedge tapers towards the nose instead of an inflated oval muzzle.
    loft(F, 'Tapered upper maxilla', [
        (.045,.139,1.982,1.870), (.105,.143,1.975,1.872),
        (.185,.130,1.961,1.893), (.255,.112,1.948,1.902),
        (.296,.079,1.936,1.906), (.311,.034,1.923,1.908),
    ], 'AbyssSkin')


def carve(F, skin):
    # Remove actual flesh behind the lips, including the old rounded chin.
    cutter = loft(F, 'Temporary oral recess', [
        (-.008,.046,1.871,1.794), (.055,.104,1.873,1.784),
        (.115,.142,1.879,1.782), (.210,.132,1.897,1.785),
        (.330,.120,1.909,1.789), (.420,.110,1.909,1.789),
    ], 'AbyssThroat')
    subtract(skin,cutter,'Open oral cavity behind the dental arch')
    F.PARTS.remove(cutter); bpy.data.objects.remove(cutter, do_unlink=True)


def arch(a, lower=False):
    c = max(0, math.cos(a))
    width = .136 if lower else .145
    return (math.sin(a)*width,
            1.864 + (-.043 if lower else .043)*c**.8,
            .087 + (.208 if lower else .218)*c**.85)


def finish(F, tube):
    # Open U-shaped mandibular bone, with a thin ventral keel and broad rami.
    # Its cross-section is flattened and tapers into the jaw joint at the ears.
    vertices=[]; faces=[]; steps=64; sides=20
    for i in range(steps+1):
        a=(i/steps-.5)*math.pi; x,y,z=arch(a,True); c=math.cos(a)
        outward=(math.sin(a), math.cos(a))
        for j in range(sides):
            b=j*math.tau/sides
            radial=(.011+.011*c)*math.copysign(abs(math.cos(b))**.7,math.cos(b))
            height=(.011+.006*c)*math.copysign(abs(math.sin(b))**.65,math.sin(b))
            vertices.append((x+outward[0]*radial,y-.010+height,z+outward[1]*radial))
        if i:
            for j in range(sides):
                p=(i-1)*sides+j; q=(i-1)*sides+(j+1)%sides
                faces.append((p,q,q+sides,p+sides))
    faces.extend([tuple(range(sides-1,-1,-1)),tuple(steps*sides+j for j in range(sides))])
    mesh(F,'Lower tapered mandibular arch',vertices,faces,'AbyssSkin')
    for side in (-1,1):
        tube('Lower mandibular tendon',[(side*.109,1.889,.025),(side*.137,1.862,.080),(side*.133,1.839,.157)], [.019,.014,.011])

    # Continuous concave lining, from the tooth roots to a narrow recessed
    # pharynx. The surface faces into the cavity, never bulges out of the face.
    vertices=[]; faces=[]; influences=[]; around=96; depth=20
    for k in range(depth+1):
        v=k/depth
        for j in range(around):
            angle=j*math.tau/around; s=math.sin(angle); c=math.cos(angle)
            a=math.asin(max(-1,min(1,c))); lower=s<0
            x,y,z=arch(a,lower)
            x-=.006*c; z-=.006*abs(s)
            end=(.031*c,1.852+.017*s,.045)
            point=tuple(p*(1-v)+q*v for p,q in zip((x,y,z),end))
            vertices.append(point)
            influences.append((1-s)*.5*(1-v*.68))
        if k:
            for j in range(around):
                a=(k-1)*around+j; b=(k-1)*around+(j+1)%around
                faces.append((a,a+around,b+around,b))
    faces.append(tuple(depth*around+j for j in range(around-1,-1,-1)))
    lining=mesh(F,'Recessed buccal lining',vertices,faces,'AbyssThroat')
    weights=lining.data.attributes.new(name='oral_jaw_weight',type='FLOAT',domain='POINT')
    for item,weight in zip(weights.data,influences):item.value=weight
    membrane(lining)

    # A low tongue with a median groove sits on the floor, behind the teeth.
    vertices=[];faces=[]
    for k in range(17):
        v=k/16; z=.098+.160*v; width=.034*math.sin(math.pi*(.12+.83*v))
        for j in range(17):
            u=j/8-1
            y=1.811+.009*math.sin(math.pi*v)+.004*(1-u*u)-.0025*math.exp(-u*u*45)
            vertices.append((u*width,y,z))
            if k and j:
                n=k*17+j;faces.append((n-18,n-17,n,n-1))
    tongue=mesh(F,'Lower grooved tongue',vertices,faces,'AbyssGums')
    membrane(tongue,.002)

    rng=random.Random(1409)
    for lower,count in [(False,17),(True,15)]:
        prefix='Lower' if lower else 'Upper'
        # Thin lip borders merge into skin; gum is recessed inside that edge.
        # Elliptical cross sections avoid the old circular hose-like lip bead.
        for gum in (False,True):
            vertices=[];faces=[];n=64;rings=12
            for i in range(n+1):
                a=(i/n-.5)*math.pi; x,y,z=arch(a,lower); c=math.cos(a)
                for j in range(rings):
                    b=j*math.tau/rings
                    radial=(.0035 if gum else .007)*( .55+.45*c)*math.cos(b)
                    height=(.0035 if gum else .0045)*math.sin(b)
                    vertices.append((x+math.sin(a)*radial,y+height+(.001 if lower else -.001),z+math.cos(a)*radial-(.007 if gum else 0)))
                if i:
                    for j in range(rings):
                        p=(i-1)*rings+j;q=(i-1)*rings+(j+1)%rings;faces.append((p,q,q+rings,p+rings))
            faces.extend([tuple(range(rings-1,-1,-1)),tuple(n*rings+j for j in range(rings))])
            mesh(F,prefix+(' recessed alveolar ridge' if gum else ' thin labial border'),vertices,faces,'AbyssGums' if gum else 'AbyssSkin')
        for i in range(count):
            a=(i/(count-1)-.5)*2.82
            x,y,z=arch(a,lower); x*=.96;z-=.006
            length=(.020+rng.random()*.016)*( .80 if lower else 1)
            # The lateral canine pairs are longer; the incisors vary in wear.
            length*=1.3 if abs(abs(a)-.88)<.15 else 1
            direction=1 if lower else -1; root=.0038+rng.random()*.0015
            tube(prefix+' rooted irregular tooth',[(x,y,z),(x*.98,y+direction*length*.56,z-.001),(x*.92,y+direction*length,z-.007)], [root,root*.60,.00025],'AbyssTeeth',12)

