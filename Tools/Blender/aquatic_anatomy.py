"""Continuous, metre-scale R-09 anatomy. No mechanical parts on the animal."""
import math
import bpy
from mathutils import Vector
import aquatic_mouth


def build(F, fuse):
    S = F.sphere

    def tube(name, points, radii, material='AbyssSkin', sides=24):
        # Interpolate the centreline as well as the radius: joints and tail should
        # not retain the straight-sided profile of the construction volumes.
        samples = []
        sizes = []
        vectors = [Vector(p) for p in points]
        for k in range(len(points) - 1):
            p0, p1, p2, p3 = [vectors[max(0, min(len(points) - 1, j))] for j in (k - 1, k, k + 1, k + 2)]
            for step in range(6):
                t = step / 6
                samples.append(.5 * ((2*p1) + (-p0+p2)*t + (2*p0-5*p1+4*p2-p3)*t*t + (-p0+3*p1-3*p2+p3)*t*t*t))
                sizes.append(radii[k] + (radii[k+1] - radii[k]) * t)
        return F.tube(name, samples + [vectors[-1]], sizes + [radii[-1]], material, sides)

    S('Pelvic girdle', (0,.90,-.025), (.25,.235,.18), 'AbyssSkin', 48, 32)
    S('Tapered abdominal wall', (0,1.18,-.025), (.224,.34,.17), 'AbyssSkin', 48, 32)
    S('Thoracic cage', (0,1.47,-.035), (.325,.265,.20), 'AbyssSkin', 48, 32)
    tube('Laryngeal column', [(0,1.57,-.01),(0,1.72,.018),(0,1.87,.025)], [.138,.112,.131])
    S('Asymmetric cranium', (.006,1.972,.012), (.184,.205,.166), 'AbyssSkin', 56, 36)
    S('Occipital mass', (0,1.99,-.065), (.168,.165,.128), 'AbyssSkin', 48, 32)
    aquatic_mouth.upper_face(F)
    for side in (-1,1):
        S('Pectoral attachment', (side*.135,1.47,.119), (.153,.12,.047), 'AbyssSkin')
        S('Scapular contour', (side*.178,1.48,-.15), (.12,.17,.065), 'AbyssSkin')
        tube('Clavicular ridge', [(side*.025,1.605,.112),(side*.135,1.59,.119),(side*.273,1.55,.018)], [.017,.025,.014])
        S('Deltoid', (side*.288,1.498,-.014), (.115,.127,.118), 'AbyssSkin')
        S('Recessed cheek plane', (side*.119,1.925,.10), (.078,.084,.075), 'AbyssSkin')
        brow=S('Orbital rim', (side*.142,2.038,.119), (.079,.026,.067), 'AbyssSkin')
        brow.rotation_euler.y=side*math.radians(-12)
        tube('Sternomastoid', [(side*.093,1.59,.08),(side*.084,1.72,.107),(side*.105,1.845,.073)], [.018,.014,.010])
        tube('Upper and lower arm', [(side*.28,1.515,-.016),(side*.388,1.335,.007),(side*.477,1.135,.10),(side*.566,1.004,.179)], [.112,.101,.065,.044])
        S('Forearm extensor', (side*.50,1.12,.105), (.064,.13,.065), 'AbyssSkin')
        S('Metacarpal pad', (side*.566,.984,.188), (.074,.09,.042), 'AbyssSkin')
        for finger in range(4):
            x=side*(.514+finger*.035)
            end=.795+abs(finger-1.5)*.021
            tube('Flexed digit', [(x,1,.194),(x+side*.020,.902,.229),(x+side*.020,end,.275)], [.019,.014,.008], sides=18)
            S('Finger knuckle', (x+side*.012,.927,.224), (.022,.026,.021), 'AbyssSkin', 20, 12)
            tube('Keratin finger claw', [(x+side*.020,end,.275),(x+side*.012,end-.018,.312),(x-side*.006,end-.003,.328)], [.009,.005,.0005], 'AbyssTeeth', 14)
        tube('Opposed thumb', [(side*.514,1.005,.17),(side*.466,.946,.218),(side*.467,.898,.269)], [.024,.017,.009], sides=18)
        tube('Thumb claw', [(side*.467,.898,.269),(side*.479,.887,.302)], [.010,.0006], 'AbyssTeeth', 14)
        tube('Thigh calf and hock', [(side*.17,.9,-.014),(side*.225,.57,.042),(side*.217,.31,-.032),(side*.25,.103,.15)], [.143,.112,.067,.045], sides=28)
        S('Quadriceps', (side*.192,.749,.015), (.127,.165,.107), 'AbyssSkin')
        S('Patellar contour', (side*.23,.556,.083), (.078,.074,.058), 'AbyssSkin')
        S('Heel and foot', (side*.25,.091,.19), (.088,.058,.147), 'AbyssSkin')
        for toe in range(3):
            x=side*.25+(toe-1)*.061
            tube('Webbed toe', [(x,.113,.185),(x,.073,.291),(x,.061,.345)], [.025,.019,.008], sides=18)
            tube('Toe claw', [(x,.061,.34),(x,.049,.393)], [.012,.0007], 'AbyssTeeth', 14)
        # Short upper eyelids give the eye a socket instead of a surface bead.
        S('Orbital soft tissue', (side*.125,1.997,.122), (.050,.041,.038), 'AbyssSkin')
        for upper in (True,False):
            points=[]
            for j in range(17):
                a=j*math.pi/16+(0 if upper else math.pi)
                points.append((side*.125+math.cos(a)*.027,1.997+math.sin(a)*.022,.151+math.sin(a)*.003))
            tube('Upper eyelid' if upper else 'Lower eyelid', points, [.0055 if upper else .004]*len(points), sides=12)
    tube('Continuous muscular tail', [(0,.91,-.14),(0,.69,-.30),(.09,.44,-.46),(.29,.25,-.50),(.46,.20,-.38),(.53,.26,-.20),(.51,.38,-.10)], [.135,.113,.081,.055,.036,.018,.001], sides=30)
    skin=[o for o in list(bpy.context.scene.objects) if o.type=='MESH' and o.data.materials[0].name=='AbyssSkin' and not o.name.startswith('Continuous muscular tail')]
    skin=fuse(skin,'Continuous living dermis',.0055)
    aquatic_mouth.carve(F,skin)
    # Cut real orbital recesses into the fused head. A black sphere placed on top
    # of the skin reads as a second exposed eyeball rather than an eye socket.
    for side in (-1,1):
        cutter=S('Temporary orbital cutter', (side*.125,1.997,.159), (.025,.021,.022), 'AbyssThroat', 40, 28)
        bpy.context.view_layer.objects.active=skin
        cut=skin.modifiers.new('Recessed eye socket','BOOLEAN');cut.operation='DIFFERENCE';cut.solver='EXACT';cut.object=cutter
        bpy.ops.object.modifier_apply(modifier=cut.name)
        F.PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)
        for row in range(3):
            cutter=S('Temporary gill recess', (side*.115,1.70+row*.052,.018), (.019,.007,.083), 'AbyssThroat', 32, 16)
            bpy.context.view_layer.objects.active=skin
            cut=skin.modifiers.new('Recessed organic gill slit','BOOLEAN');cut.operation='DIFFERENCE';cut.solver='EXACT';cut.object=cutter
            bpy.ops.object.modifier_apply(modifier=cut.name)
            F.PARTS.remove(cutter);bpy.data.objects.remove(cutter,do_unlink=True)
    skin.data.calc_loop_triangles()
    if len(skin.data.loop_triangles)>78000:
        bpy.context.view_layer.objects.active=skin
        budget=skin.modifiers.new('Continuous dermis runtime budget','DECIMATE');budget.ratio=78000/len(skin.data.loop_triangles)
        bpy.ops.object.modifier_apply(modifier=budget.name)
    # Gentle anatomical asymmetry breaks bilateral construction without adding
    # noisy geometric pores or destroying the smooth weighted joints.
    for vert in skin.data.vertices:
        p=vert.co
        wave=math.sin(p.x*19+p.z*7)*math.sin(p.y*23-p.z*13)
        vert.co += vert.normal * (.0015*wave)
    aquatic_mouth.finish(F,tube)
    for side in (-1,1):
        S('Recessed amber eye', (side*.125,1.997,.148), (.019,.018,.018), 'LabIris', 32, 20)
        S('Slit pupil', (side*.125,1.997,.165), (.004,.013,.0025), 'LabPupil', 24, 16)
        S('Nasal recess', (side*.037,1.938,.277), (.009,.004,.003), 'AbyssThroat', 24, 16)
        for row in range(3):
            S('Recessed gill tissue', (side*.105,1.70+row*.052,.018), (.009,.003,.067), 'AbyssGums', 32, 16)
    # A continuous ragged dorsal membrane, with small variations in its edge.
    vertices=[];faces=[]
    for row in range(19):
        y=.95+row*.045
        for edge in (0,1):
            depth=-.175-edge*(.035+.025*math.sin(row*.48)**2)
            vertices.append(F.C((.003*math.sin(row*.7),y,depth)))
        if row:
            k=row*2;faces.extend([(k-2,k-1,k),(k-1,k+1,k)])
    mesh=bpy.data.meshes.new('Dorsal dermal fin');mesh.from_pydata(vertices,[],faces);mesh.update()
    fin=bpy.data.objects.new('Continuous dorsal membrane',mesh);bpy.context.collection.objects.link(fin);F.track(fin,fin.name,'AbyssRidge')
    solid=fin.modifiers.new('Dorsal membrane thickness','SOLIDIFY');solid.thickness=.004
    bpy.context.view_layer.objects.active=fin;bpy.ops.object.modifier_apply(modifier=solid.name)

    for obj in list(bpy.context.scene.objects):
        if obj.type!='MESH':continue
        if not obj.data.uv_layers:
            bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
            bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.cube_project(cube_size=1);bpy.ops.object.mode_set(mode='OBJECT')
