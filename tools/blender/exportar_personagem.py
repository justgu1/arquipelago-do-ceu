# Exporta o Personagem (esqueleto, corpo, kits, equipamentos, cosméticos e as três animações) num GLB só e mostra os triângulos de cada peça.
# Só roda sem janela, depois dos scripts de personagem/: blender -b --factory-startup --python personagem/corpo.py ... --python exportar_personagem.py -- <pasta de saída>
import bpy, sys, os

# Orçamento do guia de estilo (seção 8), em triângulos.
LIMITES = {"Corpo": 3500, "Cabelo_padrao": 600, "Roupa_basica": 1000, "Rosto_padrao": 600}
LIMITE_KIT = 1000
LIMITE_PECA = 400
KITS = ("Matematico_", "Escriba_")
destino = sys.argv[sys.argv.index("--") + 1]

arm = bpy.data.objects["Personagem"]

# Orçamento de triângulos depois da redução. O desenho é feito com folga e esta etapa corta o que a silhueta não mostra, mantendo pesos, UV e materiais.
ORCAMENTO = {"Corpo": 3400, "Cabelo_padrao": 2600, "Roupa_basica": 1100}
ORCAMENTO_KIT = 5500
ORCAMENTO_PECA = 600

def triangulos_da_peca(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def reduzir_para(o, alvo):
    n = triangulos_da_peca(o)
    if n <= alvo: return
    m = o.modifiers.new("reduzir", "DECIMATE"); m.decimate_type = "COLLAPSE"; m.ratio = max(0.02, alvo / n); m.use_collapse_triangulate = True
    with bpy.context.temp_override(object=o, active_object=o):
        bpy.ops.object.modifier_move_to_index(modifier="reduzir", index=0)
        bpy.ops.object.modifier_apply(modifier="reduzir")

def reduzir_tudo():
    kits = {}
    for o in arm.children:
        if o.type != "MESH": continue
        base = o.name.split("_")[0]
        if o.name in ORCAMENTO: reduzir_para(o, ORCAMENTO[o.name])
        elif base in ("Equip", "Cosm"): reduzir_para(o, ORCAMENTO_PECA)
        else: kits.setdefault(base, []).append(o)
    for pecas in kits.values():
        total = sum(triangulos_da_peca(o) for o in pecas)
        for o in pecas: reduzir_para(o, max(150, round(triangulos_da_peca(o) * min(1.0, ORCAMENTO_KIT / total))))

reduzir_tudo()
for o in bpy.data.objects: o.select_set(False)
arm.select_set(True)
for o in arm.children: o.select_set(True)
bpy.context.view_layer.objects.active = arm
arquivo = os.path.join(destino, "personagem.glb")
bpy.ops.export_scene.gltf(filepath=arquivo, export_format="GLB", use_selection=True, export_apply=False, export_yup=True,
                          export_image_format="AUTO", export_animation_mode="ACTIONS", export_skins=True, export_def_bones=False)

total = 0
base_z = [v.co.z for n in ("Corpo", "Cabelo_padrao") for v in bpy.data.objects[n].data.vertices]
print(f"ALTURA do corpo com cabelo: {max(base_z):.3f} m (o jogo reescala para 1,5)")
for o in sorted(arm.children, key=lambda x: x.name):
    n = sum(len(p.vertices) - 2 for p in o.data.polygons); total += n
    base = o.name.split("_")[0]
    limite = LIMITES.get(o.name) or (LIMITE_PECA if base in ("Equip", "Cosm") else None)
    if limite is not None: print(f"  {o.name}: {n} triângulos ({'dentro' if n <= limite else 'ACIMA'} do limite de {limite})")
for kit in KITS:
    soma = sum(len(p.vertices) - 2 for o in arm.children if o.name.startswith(kit) for p in o.data.polygons)
    print(f"  Kit {kit[:-1]}: {soma} triângulos ({'dentro' if soma <= LIMITE_KIT else 'ACIMA'} do limite de {LIMITE_KIT} do conjunto)")
print(f"EXPORTADO Personagem: {total} triângulos no total, {os.path.getsize(arquivo)//1024} KB, ações: {[a.name for a in bpy.data.actions]}")
