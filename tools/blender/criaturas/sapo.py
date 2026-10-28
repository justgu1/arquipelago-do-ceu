# Sapo (Torre das Letras): animal de origami, sentadinho, com patas e sem mãos. Rode no Blender (MCP em modo seguro ou blender -b -P).
# >>> comum (copiado de tools/blender/comum.py por sincronizar.mjs; não edite aqui)
import bpy, bmesh, math
from mathutils import Vector, Matrix

col = bpy.data.collections.get("Inimigos") or bpy.data.collections.new("Inimigos")
if col.name not in bpy.context.scene.collection.children:
    bpy.context.scene.collection.children.link(col)

def mat(name, hexcol, rough=0.8, metal=0.0):
    h = hexcol.lstrip('#'); c = [int(h[i:i+2], 16)/255 for i in (0, 2, 4)]
    l = [v/12.92 if v <= 0.04045 else ((v+0.055)/1.055)**2.4 for v in c]
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name); m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*l, 1); b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    m.diffuse_color = (*l, 1); return m

P = dict(
    papel=mat("papel", "#EBD196"), borda=mat("borda", "#C9A465"), vinco=mat("vinco", "#B08A50"), moldura=mat("moldura", "#6B4A2E"),
    terra=mat("terra", "#58AD3F"), costa=mat("costa", "#2F6E2A"), monte=mat("monte", "#8C6B43"), neve=mat("neve", "#F4EFE4"),
    mar=mat("mar", "#2F86D6"), onda=mat("onda", "#8CC7F2"), rota=mat("rota", "#D93A30"), tinta=mat("tinta", "#2E211B"),
    branco=mat("branco", "#F7F3EA", 0.45), pupila=mat("pupila", "#17131D", 0.25), tenis=mat("tenis", "#E8542A"), sola=mat("sola", "#F2EEE4"),
    tenis_esc=mat("tenis_esc", "#B83A1C", 0.7), interior=mat("interior", "#3A1E14", 0.9), metal=mat("metal", "#C4CAD2", 0.3, 0.6),
    pato_papel=mat("pato_papel", "#F6EFD9"), pato_azul=mat("pato_azul", "#5BB6E4"), pato_azul_esc=mat("pato_azul_esc", "#3C8FC4"),
    azul_claro=mat("azul_claro", "#8CD0F0"), bico=mat("bico", "#F59A2B"), bico_esc=mat("bico_esc", "#D97A18"),
    bochecha=mat("bochecha", "#F2A0A0"), boca=mat("boca", "#4A1E1A", 0.9), lingua=mat("lingua", "#E8707A", 0.7),
    mancha_sapo=mat("mancha_sapo", "#3FA89B"), pe_sapo=mat("pe_sapo", "#6CC7BA"), barriga_sapo=mat("barriga_sapo", "#F3E9C8"),
    latao=mat("latao", "#C9A24F", 0.38, 0.85), latao_esc=mat("latao_esc", "#8A6A28", 0.45, 0.8), dourado=mat("dourado", "#EBC24E", 0.30, 1.0),
    roxo_metal=mat("roxo_metal", "#8E4FA6", 0.42, 0.45), escudo=mat("escudo", "#EAD6A2", 0.55, 0.15),
)

def apagar(prefixo):
    for n in [o.name for o in bpy.data.objects if o.name.startswith(prefixo)]:
        if n in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[n], do_unlink=True)

def vazio(name, loc, parent, rot=(0, 0, 0), scale=(1, 1, 1)):
    e = bpy.data.objects.new(name, None); col.objects.link(e); e.location = loc; e.rotation_euler = rot; e.scale = scale
    e.parent = parent; e.empty_display_size = 0.04; return e

def objeto(name, bm, materiais, parent, smooth=False, loc=(0, 0, 0)):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); col.objects.link(o)
    for m in materiais: me.materials.append(m)
    for p in me.polygons: p.use_smooth = smooth
    o.parent = parent; o.location = loc; return o

def elip(name, half, loc, material, parent, smooth=True, seg=24, aneis=16, rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=aneis, radius=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*half[0], v.co.y*half[1], v.co.z*half[2]))
    o = objeto(name, bm, [material], parent, smooth, tuple(loc)); o.rotation_euler = rot; return o

def tubo(name, pts, r, material, parent, fechado=False):
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = r; cu.bevel_resolution = 3; cu.use_fill_caps = True
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts)-1)
    for bp, p in zip(sp.bezier_points, pts): bp.co = p; bp.handle_left_type = bp.handle_right_type = "AUTO"
    if fechado: sp.use_cyclic_u = True
    o = bpy.data.objects.new(name, cu); col.objects.link(o); cu.materials.append(material); o.parent = parent; return o

def folha(name, pts, centro, material, parent, espessura):
    bm = bmesh.new(); c = bm.verts.new(centro); vs = [bm.verts.new(p) for p in pts]
    for i in range(len(vs)): bm.faces.new((c, vs[i], vs[(i+1) % len(vs)]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = objeto(name, bm, [material], parent); sd = o.modifiers.new("papel", "SOLIDIFY"); sd.thickness = espessura; sd.offset = 0; return o

def pena3d(name, O, U, V, a, b, push, material, parent, esp=0.014):
    U = Vector(U).normalized(); V = Vector(V).normalized(); Wn = U.cross(V).normalized(); pts = []
    for k in range(16):
        th = 2*math.pi*k/16; pts.append(tuple(Vector(O) + U*(a*math.cos(th)) + V*(b*math.sin(th)*(1+0.3*math.cos(th)))))
    return folha(name, pts, tuple(Vector(O) + Wn*push), material, parent, esp)

def espiral(name, cx, cz, rmax, voltas, sentido, parent):
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = 0.0062; cu.bevel_resolution = 3; cu.use_fill_caps = True
    sp = cu.splines.new("POLY"); n = 70; sp.points.add(n-1)
    for k in range(n):
        f = k/(n-1); a = sentido*f*voltas*2*math.pi; r = 0.004 + (rmax-0.004)*f
        x = cx + r*math.cos(a); z = cz + r*math.sin(a)
        sp.points[k].co = (x, -0.055*math.sqrt(max(0.0, 1-(x/0.1)**2-(z/0.115)**2))-0.003, z, 1)
    o = bpy.data.objects.new(name, cu); col.objects.link(o); cu.materials.append(P["pupila"]); o.parent = parent; return o

def kit_olhos(pref, parent, cx, cy, cz, sep, esc, mat_lid):
    # Olhos de todo inimigo: espirais em sentidos opostos e pálpebra inclinada para o meio (bravo e confuso).
    for side, corte, sx, sz, sent in [(-1, 0.052, 0.010, -0.014, 1), (1, 0.046, -0.008, -0.016, -1)]:
        piv = vazio(f"{pref}_olho{side}", (cx+sep*side, cy, cz), parent, (0, -0.20*side, 0), (esc, esc, esc))
        elip(f"{pref}_olho_bola{side}", (0.1, 0.055, 0.115), (0, 0, 0), P["branco"], piv)
        espiral(f"{pref}_pupila{side}", sx, sz, 0.05, 2.6, sent, piv)
        A, B, C = 0.1045, 0.0616, 0.1202
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=1.0)
        for v in bm.verts: v.co = Vector((v.co.x*A, v.co.y*B, v.co.z*C))
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces), plane_co=(0, 0, corte), plane_no=(0, 0, 1), clear_inner=True)
        objeto(f"{pref}_palpebra{side}", bm, [mat_lid], piv, True)
        k = math.sqrt(max(0.0, 1-(corte/C)**2)); a = A*k; b = B*k
        tubo(f"{pref}_cilio{side}", [(a*math.cos(f), -b*math.sin(f)-0.004, corte) for f in [math.pi*i/12 for i in range(13)]], 0.0085, P["tinta"], piv)

PE_PALMADO = [(0.0, 0.07), (-0.07, 0.04), (-0.14, -0.05), (-0.19, -0.17), (-0.18, -0.25), (-0.14, -0.28), (-0.10, -0.24), (-0.07, -0.17),
              (-0.05, -0.26), (-0.02, -0.33), (0.02, -0.33), (0.05, -0.26), (0.07, -0.17), (0.10, -0.24), (0.14, -0.28), (0.18, -0.25),
              (0.19, -0.17), (0.14, -0.05), (0.07, 0.04)]
# <<< comum

ORIGEM_X = 6.4

def construir_sapo(ox):
    apagar("Sapo")
    raiz = vazio("Sapo", (ox, 0, 0), None); T = vazio("Sapo_tronco", (0, 0, 0.03), raiz)
    CC = (0.0, 0.05, 0.34); CA = (0.48, 0.46, 0.32)
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=9, radius=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*CA[0], v.co.y*CA[1], v.co.z*CA[2]))
    bm.normal_update()
    for f in bm.faces:
        if f.calc_center_median().z < -0.10 and f.normal.z < -0.1: f.material_index = 1
    corpo = objeto("Sapo_corpo", bm, [P["pato_papel"], P["barriga_sapo"]], T, False, CC)
    HC = (0.0, -0.22, 0.50); HA = (0.42, 0.36, 0.27)
    cab = elip("Sapo_cabeca", HA, HC, P["pato_papel"], T, False, 14, 9)
    def sup(x, z, fora=0.016):
        inside = 1-(x/HA[0])**2-((z-HC[2])/HA[2])**2
        return (x, HC[1]-HA[1]*math.sqrt(max(0.0, inside))-fora, z)
    tubo("Sapo_boca", [sup(-0.29, 0.425), sup(-0.15, 0.455), sup(0.0, 0.465), sup(0.15, 0.455), sup(0.29, 0.425)], 0.013, P["tinta"], T)
    for s in (-1, 1): elip(f"Sapo_narina{s}", (0.014, 0.018, 0.008), (0.09*s, -0.545, 0.60), P["tinta"], T, seg=12, aneis=8)
    # As manchas de papel são malhas densas coladas (Shrinkwrap) na superfície faceteada do corpo e da cabeça.
    def elp(c, h, az, el):
        return Vector((c[0]+h[0]*math.cos(el)*math.cos(az), c[1]+h[1]*math.cos(el)*math.sin(az), c[2]+h[2]*math.sin(el)))
    def mancha(nome, alvo, c, h, az, el, ra, re):
        a0, e0 = math.radians(az), math.radians(el); bm = bmesh.new(); centro = bm.verts.new(elp(c, h, a0, e0)); an = []
        for r in range(1, 5):
            f = r/4; an.append([bm.verts.new(elp(c, h, a0+ra*f*math.cos(2*math.pi*k/16), e0+re*f*math.sin(2*math.pi*k/16))) for k in range(16)])
        for k in range(16): bm.faces.new((centro, an[0][k], an[0][(k+1) % 16]))
        for i in range(3):
            for k in range(16): bm.faces.new((an[i][k], an[i+1][k], an[i+1][(k+1) % 16], an[i][(k+1) % 16]))
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        o = objeto(nome, bm, [P["mancha_sapo"]], T, False)
        md = o.modifiers.new("cola", "SHRINKWRAP"); md.target = alvo; md.wrap_method = "NEAREST_SURFACEPOINT"; md.offset = 0.006
    for k, (az, el, ra, re) in enumerate([(90, 55, 0.30, 0.20), (55, 42, 0.22, 0.16), (125, 42, 0.24, 0.17), (90, 28, 0.18, 0.12), (25, 30, 0.20, 0.14), (155, 30, 0.22, 0.15), (70, 70, 0.14, 0.10)]):
        mancha(f"Sapo_mancha{k}", corpo, CC, CA, az, el, ra, re)
    for k, (az, el, ra, re) in enumerate([(0, 18, 0.18, 0.14), (180, 18, 0.18, 0.14)]):
        mancha(f"Sapo_mancha_cab{k}", cab, HC, HA, az, el, ra, re)
    def pe_palmado(name, loc, rot, escala):
        e = vazio(name, loc, raiz, rot)
        o = folha(name+"_pe", [(px*escala, py*escala, 0.014) for px, py in PE_PALMADO], (0.0, -0.10*escala, 0.055*escala*1.4), P["pe_sapo"], e, 0.026); o.modifiers["papel"].offset = -1
    # Pose sentada: pernas de trás abertas e para trás, para o pé de trás não cobrir o da frente do mesmo lado.
    for s in (-1, 1):
        elip(f"Sapo_coxa{s}", (0.17, 0.27, 0.19), (0.42*s, 0.20, 0.20), P["pato_papel"], T, False, 10, 7)
        elip(f"Sapo_perna{s}", (0.075, 0.22, 0.075), (0.52*s, 0.16, 0.10), P["pato_papel"], T, False, 10, 7, (0.35, 0, 0))
        pe_palmado(f"Sapo_pe_tras{s}", (0.56*s, 0.05, 0.0), (0, 0, -0.55*s), 0.95)
        elip(f"Sapo_braco{s}", (0.07, 0.075, 0.20), (0.405*s, -0.26, 0.17), P["pato_papel"], T, False, 10, 7, (0.0, 0.10*s, 0))
        pe_palmado(f"Sapo_pe_frente{s}", (0.415*s, -0.30, 0.0), (0, 0, -0.20*s), 0.55)
        elip(f"Sapo_olho_base{s}", (0.16, 0.14, 0.13), (0.24*s, -0.30, 0.68), P["pato_papel"], T, False, 10, 7)
    kit_olhos("Sapo", T, 0.0, -0.405, 0.70, 0.24, 1.25, P["pato_papel"])

construir_sapo(ORIGEM_X)
