# Exporta as criaturas da cena para GLB (uma por arquivo, com a raiz na origem) e mostra os triângulos de cada uma.
# Só roda sem janela, depois dos scripts de criaturas: blender -b --factory-startup --python criaturas/mapa.py ... --python exportar.py -- <pasta de saída>
import bpy, sys, os

CRIATURAS = ("Mapa", "Pato", "Sapo", "Tartaruga", "Besouro", "Coruja", "Boneco")
LIMITE = 2500
argumentos = sys.argv[sys.argv.index("--") + 1:]
destino = argumentos[0]
# Nomes depois da pasta limitam o que sai: o Boneco vai para modelos/, não para criaturas/.
escolhidas = argumentos[1:] or [n for n in CRIATURAS if n != "Boneco"]

def triangulos(objs):
    dg = bpy.context.evaluated_depsgraph_get(); total = 0
    for o in objs:
        if o.type not in ("MESH", "CURVE"): continue
        e = o.evaluated_get(dg); me = e.to_mesh()
        total += sum(len(p.vertices) - 2 for p in me.polygons); e.to_mesh_clear()
    return total

for nome in escolhidas:
    raiz = bpy.data.objects[nome]; raiz.location.x = 0.0
    bpy.context.view_layer.update()
    pecas = [raiz] + list(raiz.children_recursive)
    for o in bpy.data.objects: o.select_set(False)
    for o in pecas: o.select_set(True)
    bpy.context.view_layer.objects.active = raiz
    arquivo = os.path.join(destino, nome.lower() + ".glb")
    bpy.ops.export_scene.gltf(filepath=arquivo, export_format="GLB", use_selection=True, export_apply=True, export_yup=True, export_image_format="NONE")
    tris = triangulos(pecas)
    print(f"EXPORTADO {nome}: {tris} triângulos ({'dentro' if tris <= LIMITE else 'ACIMA'} do limite de {LIMITE}), {os.path.getsize(arquivo)//1024} KB")
