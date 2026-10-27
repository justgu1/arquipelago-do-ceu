# Reduz os triângulos de um modelo GLB estático (sem esqueleto) até um alvo, mantendo a textura e o mapeamento.
# Só roda sem janela: blender -b --factory-startup --python tools/blender/reduzir_modelo.py -- <entrada.glb> <saida.glb> <triângulos>
import bpy, sys

entrada, saida, alvo = sys.argv[sys.argv.index("--") + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=entrada)
malhas = [o for o in bpy.data.objects if o.type == "MESH"]
antes = sum(len(p.vertices) - 2 for o in malhas for p in o.data.polygons)
# A mesma razão em todas as malhas mantém as proporções entre as partes do modelo.
razao = min(1.0, int(alvo) / antes)
for o in malhas:
    md = o.modifiers.new("reduzir", "DECIMATE")
    md.decimate_type = "COLLAPSE"
    md.ratio = razao
    md.use_collapse_triangulate = True
bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB", export_apply=True, export_yup=True, export_image_format="AUTO")
dg = bpy.context.evaluated_depsgraph_get()
depois = 0
for o in malhas:
    e = o.evaluated_get(dg); me = e.to_mesh(); depois += sum(len(p.vertices) - 2 for p in me.polygons); e.to_mesh_clear()
print(f"REDUZIDO {entrada}: {antes} -> {depois} triângulos")
