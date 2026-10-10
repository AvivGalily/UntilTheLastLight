import bpy
from pathlib import Path
root=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/Vendor/polyhaven/fir_tree_01/fir_tree_01.blend'))
o=bpy.data.objects['fir_tree_01_c_LOD2']
print('UVLAYERS',[(uv.name,uv.active_render) for uv in o.data.uv_layers])
print('ATTRIBUTES',[(a.name,a.domain,a.data_type,len(a.data)) for a in o.data.attributes])
for m in o.data.materials:
    if not m:continue
    print('MAT',m.name)
    for n in m.node_tree.nodes:
        if n.type in ('UVMAP','TEX_IMAGE','ATTRIBUTE'):print(n.type,n.name,getattr(n,'uv_map',''),getattr(n,'attribute_name',''),getattr(n,'image',None))
    for link in m.node_tree.links:
        if link.to_socket.name=='Vector':print('VECTOR LINK',link.from_node.type,link.from_node.name,link.from_socket.name,link.to_node.name)
    for n in m.node_tree.nodes:
        if n.type=='MAPPING':print('MAPPING',[(p.name,tuple(p.default_value)) for p in n.inputs if p.name!='Vector'])
