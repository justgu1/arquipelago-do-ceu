# Exporta o Guia (esqueleto, peças e as ações Repouso, Bocejo, Olhar e Alongar) num GLB só e mostra os triângulos de cada peça.
# Só roda sem janela, depois de personagem/guia.py: blender -b --factory-startup --python personagem/guia.py --python exportar_guia.py -- <pasta de saída>
import bpy, sys, os

# O rosto fica de fora da redução: o decimate mexe nas UVs da grade plana do decalque.
ORCAMENTO = {"Guia_pele": 1400, "Guia_manto": 1800, "Guia_barba": 900, "Guia_gorro": 450, "Guia_oculos": 350, "Guia_pes": 300, "Guia_lanterna": 400}
LIMITE = 3000
destino = sys.argv[sys.argv.index("--") + 1]
arm = bpy.data.objects["Personagem"]

def triangulos(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

for o in arm.children:
    alvo = ORCAMENTO.get(o.name)
    if alvo is None or triangulos(o) <= alvo: continue
    m = o.modifiers.new("reduzir", "DECIMATE"); m.decimate_type = "COLLAPSE"; m.ratio = max(0.02, alvo / triangulos(o)); m.use_collapse_triangulate = True
    with bpy.context.temp_override(object=o, active_object=o):
        bpy.ops.object.modifier_move_to_index(modifier="reduzir", index=0)
        bpy.ops.object.modifier_apply(modifier="reduzir")

for o in bpy.data.objects: o.select_set(False)
arm.select_set(True)
for o in arm.children: o.select_set(True)
bpy.context.view_layer.objects.active = arm
arquivo = os.path.join(destino, "guia.glb")
bpy.ops.export_scene.gltf(filepath=arquivo, export_format="GLB", use_selection=True, export_apply=False, export_yup=True,
                          export_image_format="AUTO", export_animation_mode="ACTIONS", export_skins=True, export_def_bones=False)
total = 0
for o in sorted(arm.children, key=lambda x: x.name):
    n = triangulos(o); total += n; print(f"  {o.name}: {n} triângulos")
print(f"EXPORTADO Guia: {total} triângulos ({'dentro' if total <= LIMITE else 'ACIMA'} do limite de {LIMITE}), {os.path.getsize(arquivo)//1024} KB, ações: {[a.name for a in bpy.data.actions]}")
