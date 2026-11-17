# Exporta os modelos do Lobby (torres, portal, atril, árvore, monumentos, chão e pontes), um GLB por modelo, e mostra os triângulos de cada um.
# Só roda sem janela, depois dos scripts de lobby/: blender -b --factory-startup --python lobby/portal.py ... --python exportar_lobby.py -- <pasta de saída>
import bpy, sys, os

# Arquivo (relativo à pasta de saída) -> peças que entram nele, com o orçamento de triângulos de cada uma (None não reduz).
MODELOS = {
    "torre-numeros": (("TorreNumeros", 6000),),
    "torre-letras": (("TorreLetras", 6000),),
    "portal": (("Portal", None),),
    "atril": (("Atril", 1800),),
    "arvores": (("Arvore", 700),),
    "landmarks/ciencias": (("Ciencias_estrutura", 3200), ("Ciencias_Foliage", 1400)),
    "landmarks/historia": (("Historia", 3500),),
    "landmarks/geografia": (("Geografia", 3500),),
    "vilarejo/casa-a": (("CasaA", None),),
    "vilarejo/casa-b": (("CasaB", None),),
    "vilarejo/poco": (("Poco", None),),
    "vilarejo/canteiro": (("Canteiro", None),),
    "vilarejo/moinho": (("Moinho", None),),
    "vilarejo/moinho-pas": (("MoinhoPas", None),),
    "mercado": (("Mercado", 2200),),
    "portal-mestre": (("PortalMestre", None),),
    "torre-engrenagem": (("TorreEngrenagem", None),),
    "torre-livro": (("TorreLivro", None),),
    "lote02/hex-base-v02": (("Hex", None),),
    "lobby/grama-hex": (("GramaHex", None),),
    "lobby/pedra-hex": (("PedraHex", None),),
    "lobby/tijolo-hex-a": (("TijoloHexA", None),),
    "lobby/tijolo-hex-b": (("TijoloHexB", None),),
    "lobby/tijolo-hex-c": (("TijoloHexC", None),),
    "lobby/tijolos-soltos": (("TijolosSoltos", None),),
    "lote02/margem-reta": (("MargemReta", None),),
    "lote02/margem-convexa": (("MargemConvexa", None),),
    "lote02/margem-ponta-d": (("MargemPontaD", None),),
    "lote02/margem-ponta-e": (("MargemPontaE", None),),
    "lote02/rocha-inferior-avaliacao": (("Rocha", None),),
    "lote02/grama-tres-pontas-v01": (("Tufo", None),),
    "cenario/porta-arco": (("PortaArco", 2500),),
    "cenario/fonte-descanso": (("FonteDescanso", 2500),),
    "cenario/escada-pedra": (("EscadaPedra", 2500),),
    "cenario/pilar-torre": (("PilarTorre", 1500),),
    "cenario/parede-baixa": (("ParedeBaixa", 1500),),
    "cenario/piso-dungeon-a": (("PisoA", 1200),),
    "cenario/coluna-dungeon": (("ColunaDungeon", 800),),
    "cenario/coluna-braseiro": (("ColunaBraseiro", 1000),),
    "cenario/parede-alta": (("ParedeAlta", 1500),),
    "cenario/parede-tocha": (("ParedeTocha", 1800),),
    "cenario/bau": (("Bau", 900),),
    "cenario/estante": (("Estante", 1500),),
    "cenario/barril": (("Barril", 700),),
    "cenario/bancada": (("Bancada", 1500),),
    "cenario/mesa-leitura": (("MesaLeitura", 1200),),
    "cenario/pedestal-mestre": (("PedestalMestre", 2500),),
    "obstaculos/caixote-madeira": (("CaixoteMadeira", 1500),),
    "obstaculos/pilha-de-livros": (("PilhaLivros", 2000),),
    "obstaculos/pilha-de-engrenagens": (("PilhaEngrenagens", 2500),),
    "itens/orbe-de-numero": (("OrbeNumero", 1500),),
    "itens/medalhao-engrenagem": (("MedalhaoEngrenagem", 1500),),
    "itens/placa-de-letra": (("PlacaLetra", 1500),),
    "lote02/ponte-piso": (("Piso", None),),
    "lote02/ponte-guarda-corpo": (("Guarda", None),),
    "lote02/ponte-cabeceira": (("Cabeceira", None),),
}
destino = sys.argv[sys.argv.index("--") + 1]

def triangulos(o):
    return sum(len(p.vertices) - 2 for p in o.data.polygons)

def reduzir(o, alvo):
    if alvo is None or triangulos(o) <= alvo: return
    m = o.modifiers.new("reduzir", "DECIMATE"); m.decimate_type = "COLLAPSE"; m.ratio = max(0.02, alvo / triangulos(o)); m.use_collapse_triangulate = True
    with bpy.context.temp_override(object=o, active_object=o):
        bpy.ops.object.modifier_apply(modifier="reduzir")

for arquivo, pecas in MODELOS.items():
    objs = [bpy.data.objects.get(nome) for nome, _ in pecas]
    if any(o is None for o in objs): continue
    for o, (_, alvo) in zip(objs, pecas): reduzir(o, alvo)
    for o in bpy.data.objects: o.select_set(False)
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    caminho = os.path.join(destino, arquivo + ".glb"); os.makedirs(os.path.dirname(caminho), exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=caminho, export_format="GLB", use_selection=True, export_apply=True, export_yup=True, export_image_format="AUTO")
    print(f"EXPORTADO {arquivo}: {sum(triangulos(o) for o in objs)} triângulos, {os.path.getsize(caminho)//1024} KB")
