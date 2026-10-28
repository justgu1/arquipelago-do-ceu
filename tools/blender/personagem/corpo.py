# Corpo base do Personagem (um só, neutro, chibi de cabeça grande): esqueleto, pele, rosto, roupa básica e cabelo padrão. Rode no Blender (MCP em modo seguro ou blender -b -P).
# >>> comum (copiado de tools/blender/comum.py por sincronizar.mjs; não edite aqui)
import bpy, bmesh, math
from mathutils import Vector, Matrix

# Qualidade dos modelos: 1.0 é a alta (referência de PC); o jogo é mobile primeiro e usa 0.4, que reduz segmentos e corta os micro-detalhes (guia de estilo, seção 8).
LOD = 0.4
SEM_DETALHE = ("rebite", "parafuso", "pino", "furo", "narina", "unha", "ilhos")

def reduzir(n, minimo=4):
    return max(minimo, round(n*LOD))

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
    conta_vermelha=mat("conta_vermelha", "#D9483B", 0.4), conta_amarela=mat("conta_amarela", "#F2C230", 0.4),
    conta_azul=mat("conta_azul", "#3E8FD8", 0.4), conta_verde=mat("conta_verde", "#3FA89B", 0.4),
)

def apagar(prefixo):
    for n in [o.name for o in bpy.data.objects if o.name.startswith(prefixo)]:
        if n in bpy.data.objects: bpy.data.objects.remove(bpy.data.objects[n], do_unlink=True)

def vazio(name, loc, parent, rot=(0, 0, 0), scale=(1, 1, 1)):
    e = bpy.data.objects.new(name, None); col.objects.link(e); e.location = loc; e.rotation_euler = rot; e.scale = scale
    e.parent = parent; e.empty_display_size = 0.04; return e

def objeto(name, bm, materiais, parent, smooth=False, loc=(0, 0, 0)):
    if LOD < 1 and any(t in name for t in SEM_DETALHE): bm.free(); return None
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); col.objects.link(o)
    for m in materiais: me.materials.append(m)
    for p in me.polygons: p.use_smooth = smooth
    o.parent = parent; o.location = loc; return o

def elip(name, half, loc, material, parent, smooth=True, seg=24, aneis=16, rot=(0, 0, 0)):
    miudo = max(half) < 0.05
    u, v = (5, 3) if miudo and LOD < 1 else (reduzir(seg, 6), reduzir(aneis, 4))
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*half[0], v.co.y*half[1], v.co.z*half[2]))
    o = objeto(name, bm, [material], parent, smooth, tuple(loc))
    if o is not None: o.rotation_euler = rot
    return o

def tubo(name, pts, r, material, parent, fechado=False):
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = r; cu.bevel_resolution = reduzir(3, 0); cu.use_fill_caps = True
    cu.resolution_u = 1 if len(pts) >= 12 else reduzir(12, 3)
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
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = 0.0062; cu.bevel_resolution = reduzir(3, 0); cu.use_fill_caps = True
    sp = cu.splines.new("POLY"); n = reduzir(50, 18); sp.points.add(n-1)
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
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=reduzir(24, 8), v_segments=reduzir(16, 6), radius=1.0)
        for v in bm.verts: v.co = Vector((v.co.x*A, v.co.y*B, v.co.z*C))
        bmesh.ops.bisect_plane(bm, geom=list(bm.verts)+list(bm.edges)+list(bm.faces), plane_co=(0, 0, corte), plane_no=(0, 0, 1), clear_inner=True)
        objeto(f"{pref}_palpebra{side}", bm, [mat_lid], piv, True)
        k = math.sqrt(max(0.0, 1-(corte/C)**2)); a = A*k; b = B*k
        tubo(f"{pref}_cilio{side}", [(a*math.cos(f), -b*math.sin(f)-0.004, corte) for f in [math.pi*i/12 for i in range(13)]], 0.0085, P["tinta"], piv)

PE_PALMADO = [(0.0, 0.07), (-0.07, 0.04), (-0.14, -0.05), (-0.19, -0.17), (-0.18, -0.25), (-0.14, -0.28), (-0.10, -0.24), (-0.07, -0.17),
              (-0.05, -0.26), (-0.02, -0.33), (0.02, -0.33), (0.05, -0.26), (0.07, -0.17), (0.10, -0.24), (0.14, -0.28), (0.18, -0.25),
              (0.19, -0.17), (0.14, -0.05), (0.07, 0.04)]

# ---- peças de máquina dos autômatos: engrenagens, canos, juntas, aros, escudo e chave de dar corda ----
def orientar(o, dire):
    d = Vector(dire).normalized(); o.rotation_mode = "QUATERNION"
    o.rotation_quaternion = d.to_track_quat('Z', 'X' if abs(d.y) > 0.9 else 'Y')

def cano(name, A, B, raio, material, parent, segs=14, raio_b=None):
    A = Vector(A); B = Vector(B); comp = (B-A).length
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=reduzir(segs, 6), radius1=raio, radius2=raio if raio_b is None else raio_b, depth=comp)
    bmesh.ops.translate(bm, vec=Vector((0, 0, comp/2)), verts=bm.verts)
    o = objeto(name, bm, [material], parent, True, tuple(A))
    if o is not None: orientar(o, B-A)
    return o

def junta(name, pos, raio, material, parent):
    return elip(name, (raio, raio, raio), pos, material, parent, True, 16, 10)

def aro(name, centro, normal, raio, r, material, parent):
    n = Vector(normal).normalized(); u = n.cross(Vector((0, 0, 1)))
    if u.length < 0.1: u = n.cross(Vector((1, 0, 0)))
    u.normalize(); v = n.cross(u)
    n_pts = reduzir(20, 12)
    pts = [tuple(Vector(centro) + (u*math.cos(2*math.pi*i/n_pts) + v*math.sin(2*math.pi*i/n_pts))*raio) for i in range(n_pts)]
    return tubo(name, pts, r, material, parent, True)

def engrenagem(name, centro, raio, dentes, esp, dire, material, parent):
    d = 2*math.pi/dentes; rin = raio*0.80; pts = []
    for i in range(dentes):
        for da, r in ((-0.30, rin), (-0.17, raio), (0.17, raio), (0.30, rin)):
            a = d*(i+da); pts.append((r*math.cos(a), r*math.sin(a), 0.0))
    o = folha(name, pts, (0, 0, 0), material, parent, esp); o.location = tuple(centro); orientar(o, dire)
    cano(name+"_cubo", (0, 0, -esp*0.9), (0, 0, esp*0.9), raio*0.24, P["latao_esc"], o)
    for k in range(4):
        a = math.pi/2*k + math.pi/4; x = raio*0.55*math.cos(a); y = raio*0.55*math.sin(a)
        cano(f"{name}_furo{k}", (x, y, -esp*0.55), (x, y, esp*0.55), raio*0.13, P["tinta"], o)
    return o

def escudo_liso(pref, parent, pos, normal, raio):
    # O escudo liso é uma face de relógio sem números dentro de uma engrenagem, sobre o corpo e nunca na frente do rosto: o jogo escreve o número nela.
    n = Vector(normal).normalized()
    engrenagem(pref+"_escudo_engrenagem", pos, raio, 16, 0.03, n, P["dourado"], parent)
    o = elip(pref+"_escudo", (raio*0.80, raio*0.80, 0.02), Vector(pos) + n*0.022, P["escudo"], parent); orientar(o, n)
    aro(pref+"_escudo_aro", Vector(pos) + n*0.03, n, raio*0.80, 0.012, P["latao_esc"], parent)
    return o

def chave_de_corda(pref, parent, pos, dire, esc=1.0):
    h = vazio(pref+"_chave", pos, parent); orientar(h, dire)
    cano(pref+"_chave_haste", (0, 0, 0), (0, 0, 0.14*esc), 0.018*esc, P["dourado"], h)
    for s in (-1, 1): elip(f"{pref}_chave_asa{s}", (0.075*esc, 0.014*esc, 0.052*esc), (0.07*esc*s, 0, 0.16*esc), P["dourado"], h)
    junta(pref+"_chave_base", (0, 0, 0), 0.04*esc, P["latao_esc"], h)
    return h

def aro_olho(pref):
    # Aro de latão com quatro parafusos em volta de cada olho, para o olho parecer de máquina.
    for s in (-1, 1):
        piv = bpy.data.objects[f"{pref}_olho{s}"]
        n_pts = reduzir(20, 12)
        tubo(f"{pref}_olho_aro{s}", [(0.116*math.cos(2*math.pi*i/n_pts), -0.012, 0.130*math.sin(2*math.pi*i/n_pts)) for i in range(n_pts)], 0.016, P["dourado"], piv, True)
        for k in range(4):
            a = math.pi/2*k + math.pi/4
            elip(f"{pref}_olho_parafuso{s}_{k}", (0.012, 0.012, 0.012), (0.116*math.cos(a), -0.02, 0.130*math.sin(a)), P["latao_esc"], piv)

def normal_elip(c, h, p):
    return Vector(((p[0]-c[0])/h[0]**2, (p[1]-c[1])/h[1]**2, (p[2]-c[2])/h[2]**2)).normalized()

# ---- partes dos inimigos de objeto: punho fechado e tênis ----
def punho(name, parent, loc, rot, scale):
    bm = bmesh.new(); sk = bm.verts.layers.skin.verify()
    def v(co, rx, ry, root=False):
        x = bm.verts.new(co); x[sk].radius = (rx, ry)
        if root: x[sk].use_root = True
        return x
    palma = v((0, 0, 0.015), 0.058, 0.04, True); pulso = v((0, 0.005, -0.055), 0.04, 0.034); bm.edges.new((palma, pulso))
    for x, r in [(-0.045, 0.0205), (-0.015, 0.0215), (0.015, 0.0215), (0.045, 0.0205)]:
        b = v((x, 0.0, 0.052), r, r); j1 = v((x, -0.004, 0.088), r, r); j2 = v((x, -0.05, 0.082), r*0.97, r*0.97); pt = v((x, -0.062, 0.040), r*0.92, r*0.92)
        bm.edges.new((palma, b)); bm.edges.new((b, j1)); bm.edges.new((j1, j2)); bm.edges.new((j2, pt))
    t0 = v((0.056, -0.015, 0.0), 0.024, 0.024); t1 = v((0.05, -0.058, 0.012), 0.0225, 0.0225)
    t2 = v((0.012, -0.078, 0.026), 0.0215, 0.0215); t3 = v((-0.026, -0.078, 0.03), 0.02, 0.02)
    bm.edges.new((palma, t0)); bm.edges.new((t0, t1)); bm.edges.new((t1, t2)); bm.edges.new((t2, t3))
    o = objeto(name, bm, [P["branco"]], parent, False, loc); o.rotation_euler = rot; o.scale = scale
    s = o.modifiers.new("pele", "SKIN"); s.branch_smoothing = 0.7; s.use_smooth_shade = True
    sub = o.modifiers.new("lisa", "SUBSURF"); sub.levels = sub.render_levels = 2 if LOD >= 1 else 1
    cb = bmesh.new(); bmesh.ops.create_cone(cb, cap_ends=True, cap_tris=False, segments=18, radius1=0.078, radius2=0.088, depth=0.05)
    c = objeto(name+"_manga", cb, [P["branco"]], o, True, (0, 0.004, -0.062)); c.scale = (0.85, 0.68, 1.0)
    bv = c.modifiers.new("chanfro", "BEVEL"); bv.width = 0.016; bv.segments = 3

def tenis(name, parent, loc, rot):
    t = vazio(name, loc, parent, rot, (1.2, 1.2, 1.2))
    elip(name+"_sola", (0.1, 0.195, 0.028), (0, -0.02, 0.028), P["sola"], t)
    elip(name+"_corpo", (0.095, 0.135, 0.078), (0, 0.01, 0.085), P["tenis"], t)
    elip(name+"_cano", (0.082, 0.07, 0.058), (0, 0.078, 0.13), P["tenis"], t)
    elip(name+"_biqueira", (0.07, 0.0665, 0.0476), (0, -0.115, 0.062), P["sola"], t)
    tubo(name+"_costura_bico", [(-0.068, -0.074, 0.058), (0.0, -0.082, 0.102), (0.068, -0.074, 0.058)], 0.0045, P["tinta"], t)
    elip(name+"_interior", (0.055, 0.052, 0.01), (0, 0.078, 0.183), P["interior"], t)
    tubo(name+"_gola", [(0.062*math.cos(2*math.pi*i/18), 0.078+0.058*math.sin(2*math.pi*i/18), 0.187) for i in range(18)], 0.012, P["tenis_esc"], t, True)
    elip(name+"_lingua", (0.046, 0.015, 0.052), (0, -0.012, 0.168), P["tenis_esc"], t, rot=(0.45, 0, 0))
    elip(name+"_talao", (0.02, 0.01, 0.03), (0, 0.145, 0.14), P["tenis_esc"], t)
    for lado in (-1, 1):
        tubo(f"{name}_listra{lado}", [(lado*0.088, 0.07, 0.095), (lado*0.099, 0.0, 0.097), (lado*0.084, -0.06, 0.095)], 0.0075, P["branco"], t)
    for k, yy in enumerate([-0.055, -0.02, 0.015]):
        zt = 0.085 + 0.078*math.sqrt(max(0.0, 1-((yy-0.01)/0.135)**2)) + 0.004
        tubo(f"{name}_cadarco{k}", [(-0.052, yy, zt-0.016), (0.0, yy, zt+0.002), (0.052, yy, zt-0.016)], 0.0085, P["branco"], t)
        for lado in (-1, 1): elip(f"{name}_ilhos{k}_{lado}", (0.009, 0.009, 0.009), (lado*0.054, yy, zt-0.018), P["tinta"], t)
    elip(name+"_no", (0.012, 0.011, 0.011), (0, 0.042, 0.172), P["branco"], t)
    for lado in (-1, 1): elip(f"{name}_laco{lado}", (0.024, 0.011, 0.011), (lado*0.027, 0.042, 0.176), P["branco"], t, rot=(0, lado*0.5, 0))
    for k in range(4): elip(f"{name}_trava{k}", (0.016, 0.012, 0.006), (-0.054+0.036*k, 0, 0.004), P["tinta"], t)

# ---- props: caixa chanfrada, fusão de peças em uma malha ----
def congelar(o):
    # Deixa a peça como malha simples já na pose de repouso: aplica curva, modificadores e a posição.
    dg = bpy.context.evaluated_depsgraph_get(); e = o.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(e, depsgraph=dg); me.transform(o.matrix_world)
    nome = o.name; mats = [m for m in o.data.materials]
    if o.type == "MESH" and o.modifiers: mats = [m for m in me.materials]
    bpy.data.objects.remove(o, do_unlink=True)
    n = bpy.data.objects.new(nome, me); col.objects.link(n)
    if len(me.materials) == 0:
        for m in mats: me.materials.append(m)
    return n

def cubo(name, centro, tam, material, parent=None, chanfro=0.012, rot=(0, 0, 0), smooth=True):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*tam[0], v.co.y*tam[1], v.co.z*tam[2]))
    if chanfro > 0: bmesh.ops.bevel(bm, geom=list(bm.edges), offset=chanfro, segments=2, affect="EDGES")
    o = objeto(name, bm, [material], parent, smooth, tuple(centro)); o.rotation_euler = rot; return o

def fundir(nome, pecas):
    # Junta as peças de um prop numa malha só, com um material por cor, na origem do mundo: curvas e modificadores viram malha antes.
    todas = []
    for p in pecas:
        if p is None: continue
        for q in [p] + list(p.children_recursive):
            if q not in todas and q.type in ("MESH", "CURVE"): todas.append(q)
    pecas = todas
    feitas = [congelar(p) if (p.type == "CURVE" or (p.type == "MESH" and p.modifiers)) else p for p in pecas]
    for x in bpy.context.view_layer.objects: x.select_set(False)
    base = bpy.data.objects.new(nome + "_base", bpy.data.meshes.new(nome + "_base")); col.objects.link(base)
    for q in feitas:
        if q.parent is not None:
            m = q.matrix_world.copy(); q.parent = None; q.matrix_world = m
        q.select_set(True)
    base.select_set(True); bpy.context.view_layer.objects.active = base
    with bpy.context.temp_override(active_object=base, selected_editable_objects=feitas + [base], selected_objects=feitas + [base]):
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active; o.name = nome; o.data.name = nome; return o

from mathutils.bvhtree import BVHTree

PERSONAGEM_COL = bpy.data.collections.get("Personagem") or bpy.data.collections.new("Personagem")
if PERSONAGEM_COL.name not in bpy.context.scene.collection.children:
    bpy.context.scene.collection.children.link(PERSONAGEM_COL)
col = PERSONAGEM_COL

# Cores do Personagem. "pele", "cabelo" e "roupa" são tingidas pelo jogo, pelo nome do material.
P.update(
    pele=mat("pele", "#F2B88F", 0.7), pele_sombra=mat("pele_sombra", "#D9946E", 0.7), cabelo=mat("cabelo", "#8A4A26", 0.6), roupa=mat("roupa", "#B9AD98", 0.85),
    iris=mat("iris", "#7A4A24", 0.3), bochecha_p=mat("bochecha_p", "#EE8F7E", 0.8), sobrancelha=mat("sobrancelha", "#5C3319", 0.7),
    meia=mat("meia", "#D8CFBE", 0.9),
    jaqueta=mat("jaqueta", "#2F9C8E", 0.75), ouro_tecido=mat("ouro_tecido", "#E3B23A", 0.55, 0.2), shorts=mat("shorts", "#7A4B2C", 0.8),
    couro=mat("couro", "#7A4326", 0.65), couro_cl=mat("couro_cl", "#9C5A32", 0.65), lente=mat("lente", "#59606B", 0.15, 0.1),
    cachecol=mat("cachecol", "#E3B23A", 0.95), tenis_lar=mat("tenis_lar", "#F28C38", 0.7), sola_br=mat("sola_br", "#F4F0E6", 0.6),
    cadarco=mat("cadarco", "#F2EEE6", 0.8), peca_ouro=mat("peca_ouro", "#F2C94C", 0.45),
    tunica=mat("tunica", "#9B5DC9", 0.8), capa_cl=mat("capa_cl", "#EFE3C6", 0.85), capa_borda=mat("capa_borda", "#B07AA0", 0.8),
    calca_az=mat("calca_az", "#4A5693", 0.8), bota=mat("bota", "#7B4A2E", 0.7), boina=mat("boina", "#7A3FB5", 0.85),
    pena_br=mat("pena_br", "#F4F1EA", 0.6), pena_ponta=mat("pena_ponta", "#2B2630", 0.6), saco=mat("saco", "#E5D3B0", 0.9),
    cubo_madeira=mat("cubo_madeira", "#C98A55", 0.7), cristal=mat("cristal", "#6FD3F2", 0.2, 0.1), aco=mat("aco", "#C7CDD6", 0.35, 0.7),
    jaqueta_esc=mat("jaqueta_esc", "#1E7167", 0.8), couro_esc=mat("couro_esc", "#4E2A16", 0.7), cachecol_esc=mat("cachecol_esc", "#C9962A", 0.95),
    equip_pano=mat("equip_pano", "#8A6AC0", 0.85), equip_metal=mat("equip_metal", "#C7CDD6", 0.38, 0.5), equip_detalhe=mat("equip_detalhe", "#F2C94C", 0.4, 0.5),
    cosmetico=mat("cosmetico", "#E8E8E8", 0.8), vidro_luz=mat("vidro_luz", "#FFE8A0", 0.2), papel_azul=mat("papel_azul", "#8CD0F0", 0.8),
)

# ---- o corpo em números: quatro cabeças, o Personagem olha para -Y e a esquerda dele é +X; tudo em metros, pés em z = 0 ----
BRACO_DIR = Vector((0.78, 0.0, -0.62)).normalized()
# (z, meia-largura, meia-profundidade) do tronco, do quadril ao ombro.
TRONCO = [(0.54, 0.130, 0.098), (0.62, 0.150, 0.106), (0.70, 0.124, 0.090), (0.80, 0.132, 0.096), (0.90, 0.155, 0.102), (0.96, 0.160, 0.098), (1.00, 0.100, 0.072)]
# (z, meia-largura, meia-profundidade, deslocamento y do centro) da cabeça, do queixo ao alto.
CABECA = [(1.03, 0.050, 0.050, 0.0), (1.06, 0.100, 0.100, -0.010), (1.10, 0.148, 0.138, -0.012), (1.17, 0.188, 0.168, 0.0), (1.25, 0.202, 0.182, 0.0),
          (1.32, 0.198, 0.182, 0.004), (1.38, 0.158, 0.150, 0.010), (1.42, 0.090, 0.090, 0.010), (1.44, 0.020, 0.020, 0.010)]
# A cabeça cresce em volta do queixo, como no desenho: tudo o que prende na cabeça passa por rigido(..., "Head") e cresce junto.
ESCALA_CABECA = 1.5
PIVO_CABECA = Vector((0, 0, 1.03))
JOELHO_T = 0.42
COTOVELO_T = 0.52

def interp(tab, z):
    if z <= tab[0][0]: return tab[0][1:]
    if z >= tab[-1][0]: return tab[-1][1:]
    for i in range(len(tab)-1):
        if tab[i][0] <= z <= tab[i+1][0]:
            f = (z-tab[i][0])/(tab[i+1][0]-tab[i][0])
            return tuple(tab[i][k]+(tab[i+1][k]-tab[i][k])*f for k in range(1, len(tab[i])))
    return tab[-1][1:]

def superficie_cabeca(x, z, folga=0.0):
    # Y da frente da cabeça no ponto x, z: onde o rosto, os olhos e os óculos se apoiam.
    rx, ry, cy = interp(CABECA, z)
    return cy - ry*math.sqrt(max(0.0, 1.0 - (x/rx)**2)) - folga

def frente_tronco(z, margem=0.0):
    return -(interp(TRONCO, z)[1] + margem)

def ombro_de(s):
    return Vector((0.15*s, 0.0, 0.94))

def pontos_braco(s):
    d = Vector((BRACO_DIR.x*s, 0, BRACO_DIR.z)); o = ombro_de(s)
    return [o, o + d*0.20, o + d*0.38]

def pontos_perna(s):
    return [Vector((0.07*s, 0, 0.60)), Vector((0.07*s, 0, 0.35)), Vector((0.07*s, 0, 0.08))]

def ossos_do_personagem():
    t = [("Hips", None, (0, 0, 0.60), (0, 0, 0.68)), ("Spine", "Hips", (0, 0, 0.68), (0, 0, 0.78)),
         ("Spine01", "Spine", (0, 0, 0.78), (0, 0, 0.88)), ("Spine02", "Spine01", (0, 0, 0.88), (0, 0, 0.98)),
         ("neck", "Spine02", (0, 0, 0.98), (0, 0, 1.05)), ("Head", "neck", (0, 0, 1.05), (0, 0, 1.05 + 0.39*ESCALA_CABECA))]
    for lado, s in (("Left", 1), ("Right", -1)):
        b = pontos_braco(s); d = Vector((BRACO_DIR.x*s, 0, BRACO_DIR.z)); fp = pontos_perna(s)
        t += [(lado+"Shoulder", "Spine02", (0.03*s, 0, 0.97), tuple(b[0])), (lado+"Arm", lado+"Shoulder", tuple(b[0]), tuple(b[1])),
              (lado+"ForeArm", lado+"Arm", tuple(b[1]), tuple(b[2])), (lado+"Hand", lado+"ForeArm", tuple(b[2]), tuple(b[2]+d*0.09)),
              (lado+"UpLeg", "Hips", tuple(fp[0]), tuple(fp[1])), (lado+"Leg", lado+"UpLeg", tuple(fp[1]), tuple(fp[2])),
              (lado+"Foot", lado+"Leg", tuple(fp[2]), (0.07*s, -0.09, 0.03)), (lado+"ToeBase", lado+"Foot", (0.07*s, -0.09, 0.03), (0.07*s, -0.16, 0.03))]
    return t

def esqueleto_do_personagem():
    apagar("Personagem")
    arm = bpy.data.armatures.new("Personagem"); o = bpy.data.objects.new("Personagem", arm); col.objects.link(o)
    for x in bpy.context.view_layer.objects: x.select_set(False)
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    for nome, pai, cabeca, ponta in ossos_do_personagem():
        b = arm.edit_bones.new(nome); b.head = cabeca; b.tail = ponta
        if pai is not None: b.parent = arm.edit_bones[pai]
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.display_type = "STICK"; o.show_in_front = True
    return o

def esqueleto():
    return bpy.data.objects["Personagem"]

# ---- peças simples ----
def esfera(name, half, loc, material, seg=16, aneis=10, rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=aneis, radius=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*half[0], v.co.y*half[1], v.co.z*half[2]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    me.materials.append(material); o = bpy.data.objects.new(name, me); col.objects.link(o); o.location = loc; o.rotation_euler = rot; return o

def caixa(name, centro, tam, material, chanfro=0.01, rot=(0, 0, 0), seg=2, liso=True):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts: v.co = Vector((v.co.x*tam[0], v.co.y*tam[1], v.co.z*tam[2]))
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=chanfro, segments=seg, affect="EDGES")
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = liso
    me.materials.append(material); o = bpy.data.objects.new(name, me); col.objects.link(o); o.location = centro; o.rotation_euler = rot; return o

def tronco(name, A, B, raio_a, raio_b, material, seg=14, tampas=True):
    # Cano cônico de A até B, macio.
    A = Vector(A); B = Vector(B); comp = (B-A).length
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=tampas, cap_tris=False, segments=seg, radius1=raio_a, radius2=raio_b, depth=comp)
    bmesh.ops.translate(bm, vec=Vector((0, 0, comp/2)), verts=bm.verts)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    me.materials.append(material); o = bpy.data.objects.new(name, me); col.objects.link(o); o.location = tuple(A); orientar(o, B-A); return o

def fio(name, pts, raio, material, seg=6, afinar=True, fechado=False):
    # Fio liso e redondo por uma lista de pontos (curva com espessura): sobrancelha, boca, friso, alça. Afina nas pontas quando "afinar".
    cu = bpy.data.curves.new(name, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = raio; cu.bevel_resolution = 2; cu.use_fill_caps = True; cu.resolution_u = 2 if len(pts) >= 12 else 4
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts)-1); n = len(pts); sp.use_cyclic_u = fechado
    for i, bp in enumerate(sp.bezier_points):
        bp.co = pts[i]; bp.handle_left_type = bp.handle_right_type = "AUTO"
        bp.radius = 0.3 + 0.7*math.sin(math.pi*i/(n-1))**0.5 if afinar and n > 2 and not fechado else 1.0
    o = bpy.data.objects.new(name, cu); col.objects.link(o); cu.materials.append(material)
    return [o]

# ---- loft: superfície que passa por seções elípticas; é o que faz o corpo e a roupa seguirem a forma em vez de serem balões ----
def virar_para_fora(o, meio):
    # Confere pela soma das normais em relação ao meio da peça: se a malha ficou do avesso, inverte.
    me = o.data; soma = 0.0
    for p in me.polygons: soma += p.normal.dot(p.center - meio)
    if soma < 0:
        for p in me.polygons: p.flip()
        me.update()

def loft(name, secoes, material, seg=16, tampa_a=True, tampa_b=True):
    # secoes: [(centro, eixo_u, eixo_v, raio_u, raio_v)], na ordem do caminho.
    bm = bmesh.new(); aneis = []
    for c, u, v, ru, rv in secoes:
        c = Vector(c); u = Vector(u).normalized(); v = Vector(v).normalized()
        aneis.append([bm.verts.new(c + u*(ru*math.cos(2*math.pi*i/seg)) + v*(rv*math.sin(2*math.pi*i/seg))) for i in range(seg)])
    for a in range(len(aneis)-1):
        for i in range(seg):
            j = (i+1) % seg
            bm.faces.new((aneis[a][i], aneis[a][j], aneis[a+1][j], aneis[a+1][i]))
    if tampa_a: bm.faces.new(aneis[0])
    if tampa_b: bm.faces.new(aneis[-1][::-1])
    if tampa_a and tampa_b: bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    me.materials.append(material); o = bpy.data.objects.new(name, me); col.objects.link(o)
    meio = Vector((0, 0, 0))
    for s in secoes: meio += Vector(s[0])
    if not (tampa_a and tampa_b): virar_para_fora(o, meio/len(secoes))
    return o

def secoes_z(tab, z0, z1, n, margem=0.0, desloca=0.0, uso_cy=False):
    # Seções horizontais de uma tabela (tronco ou cabeça) entre z0 e z1, com uma margem para fora: a roupa que veste o corpo.
    s = []
    for i in range(n+1):
        z = z0 + (z1-z0)*i/n; v = interp(tab, z); cy = v[2] if uso_cy else 0.0
        s.append((Vector((0, cy + desloca, z)), (1, 0, 0), (0, 1, 0), v[0]+margem, v[1]+margem))
    return s

def perfil_em(perfil, f):
    if f <= perfil[0][0]: return perfil[0][1], perfil[0][2]
    if f >= perfil[-1][0]: return perfil[-1][1], perfil[-1][2]
    for i in range(len(perfil)-1):
        if perfil[i][0] <= f <= perfil[i+1][0]:
            t = (f-perfil[i][0])/(perfil[i+1][0]-perfil[i][0])
            return perfil[i][1]+(perfil[i+1][1]-perfil[i][1])*t, perfil[i][2]+(perfil[i+1][2]-perfil[i][2])*t
    return perfil[-1][1], perfil[-1][2]

def ponto_em(pts, f):
    # Posição e direção a uma fração f do comprimento de uma linha de pontos.
    total = sum((pts[i+1]-pts[i]).length for i in range(len(pts)-1)); alvo = max(0.0, min(1.0, f))*total; acc = 0.0
    for i in range(len(pts)-1):
        seg = (pts[i+1]-pts[i]).length
        if acc + seg >= alvo - 1e-9:
            t = (alvo-acc)/seg if seg > 0 else 0.0
            return pts[i] + (pts[i+1]-pts[i])*t, (pts[i+1]-pts[i]).normalized()
        acc += seg
    return pts[-1], (pts[-1]-pts[-2]).normalized()

def secoes_membro(pts, perfil, de=0.0, ate=1.0, margem=0.0):
    # perfil: [(fração, raio_lado, raio_frente)] ao longo do braço ou da perna; entram as frações dentro de de..ate e as pontas.
    fr = [de] + [p[0] for p in perfil if de < p[0] < ate] + [ate]
    s = []
    for f in fr:
        c, d = ponto_em(pts, f); u = d.cross(Vector((0, 1, 0))).normalized()
        a, b = perfil_em(perfil, f); s.append((c, u, (0, 1, 0), a+margem, b+margem))
    return s

PERFIL_BRACO = [(0.0, 0.060, 0.058), (0.25, 0.054, 0.052), (0.52, 0.046, 0.046), (0.75, 0.042, 0.042), (1.0, 0.038, 0.038)]
PERFIL_PERNA = [(0.0, 0.090, 0.092), (0.2, 0.083, 0.085), (0.42, 0.064, 0.066), (0.62, 0.068, 0.070), (0.85, 0.050, 0.050), (1.0, 0.046, 0.046)]

# ---- formas macias: subdivisão, mechas e curvas ----
def suavizar(o, n=1):
    # Subdivisão: poucas seções viram uma superfície redonda e cheia, como num boneco de vinil.
    m = o.modifiers.new("suave", "SUBSURF"); m.levels = n; m.render_levels = n
    return o

def curva3(a, b, c, n):
    a = Vector(a); b = Vector(b); c = Vector(c)
    return [a*(1-i/n)**2 + b*2*(i/n)*(1-i/n) + c*(i/n)**2 for i in range(n+1)]

def mecha(name, a, b, c, base, material, seg=8, n=6, ponta=0.003, ref=None):
    # Mecha de cabelo (ou pluma, cauda, ponta de cachecol): afina do volume da base até uma ponta, ao longo de uma curva de três pontos.
    pts = curva3(a, b, c, n); secs = []
    for i in range(n+1):
        d = (pts[min(i+1, n)] - pts[max(i-1, 0)]).normalized()
        ref_i = Vector(ref) if ref is not None else (Vector((0, 1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0)))
        u = d.cross(ref_i).normalized(); v = d.cross(u).normalized(); k = max(0.0, 1.0 - (i/n)**1.15)
        secs.append((pts[i], u, v, ponta + (base[0]-ponta)*k, ponta + (base[1]-ponta)*k))
    return suavizar(loft(name, secs, material, seg, True, True), 1)

# ---- pesos e fusão ----
def pesos_em(t, chaves):
    if t <= chaves[0][1]: return {chaves[0][0]: 1.0}
    if t >= chaves[-1][1]: return {chaves[-1][0]: 1.0}
    for i in range(len(chaves)-1):
        a = chaves[i][0]; ta = chaves[i][1]; b = chaves[i+1][0]; tb = chaves[i+1][1]
        if ta <= t <= tb:
            f = (t-ta)/(tb-ta) if tb > ta else 1.0
            r = {}; r[a] = r.get(a, 0.0) + (1.0-f); r[b] = r.get(b, 0.0) + f; return r
    return {chaves[-1][0]: 1.0}

def ligar(o, chaves, p0=(0, 0, 0), p1=(0, 0, 1)):
    # chaves: [(osso, t)], t de 0 a 1 ao longo de p0->p1; uma só chave prende a peça inteira ao osso.
    o = congelar(o); arm = esqueleto(); d = Vector(p1) - Vector(p0); l2 = max(d.length_squared, 1e-9)
    nomes = []
    for c in chaves:
        if c[0] not in nomes: nomes.append(c[0])
    grupos = {n: o.vertex_groups.new(name=n) for n in nomes}
    for v in o.data.vertices:
        t = (v.co - Vector(p0)).dot(d)/l2 if len(chaves) > 1 else 0.0
        for n, w in pesos_em(t, chaves).items():
            if w > 0.001: grupos[n].add([v.index], w, "REPLACE")
    m = o.modifiers.new("Armature", "ARMATURE"); m.object = arm; o.parent = arm
    return o

def rigido(o, osso):
    if osso == "Head":
        m = Matrix.Translation(PIVO_CABECA) @ Matrix.Scale(ESCALA_CABECA, 4) @ Matrix.Translation(-PIVO_CABECA)
        o.matrix_basis = m @ o.matrix_basis
    return ligar(o, [(osso, 0.0)])

def ligar_tronco(o):
    # Peça do tronco: o peso vai do quadril à coluna alta conforme a altura (0,54 a 1,0 m).
    return ligar(o, [("Hips", 0.0), ("Spine", 0.2), ("Spine01", 0.55), ("Spine02", 1.0)], (0, 0, 0.54), (0, 0, 1.0))

def ligar_braco(o, s):
    lado = "Left" if s > 0 else "Right"; b = pontos_braco(s)
    return ligar(o, [(lado+"Arm", 0.0), (lado+"Arm", COTOVELO_T-0.08), (lado+"ForeArm", COTOVELO_T+0.08), (lado+"ForeArm", 1.0)], b[0], b[2])

def ligar_perna(o, s):
    lado = "Left" if s > 0 else "Right"; p = pontos_perna(s)
    return ligar(o, [(lado+"UpLeg", 0.0), (lado+"UpLeg", JOELHO_T-0.1), (lado+"Leg", JOELHO_T+0.1), (lado+"Leg", 1.0)], p[0], p[2])

def manga(name, s, de, ate, margem, material, seg=14):
    # Manga ou luva: o mesmo perfil do braço, um pouco maior, de "de" a "ate" (fração do braço).
    return ligar_braco(loft(name, secoes_membro(pontos_braco(s), PERFIL_BRACO, de, ate, margem), material, seg, de <= 0.001, False), s)

def calca(name, s, de, ate, margem, material, seg=14):
    # Perna de calça, bota ou meia: o perfil da perna, um pouco maior, de "de" a "ate" (fração da perna).
    return ligar_perna(loft(name, secoes_membro(pontos_perna(s), PERFIL_PERNA, de, ate, margem), material, seg, de <= 0.001, False), s)

def ombreira(name, s, raio, material):
    # Bola macia no encaixe do braço com o tronco: fecha o vão do ombro, na pele ou na roupa.
    lado = "Left" if s > 0 else "Right"; o = ombro_de(s)
    return rigido(esfera(name, (raio, raio*0.95, raio), (o.x, o.y, o.z - 0.004), material, 14, 10), lado + "Arm")

def unir(nome, pecas):
    # Junta as peças de um item numa malha só, com um material por cor.
    pecas = [p for p in pecas if p is not None]
    for x in bpy.context.view_layer.objects: x.select_set(False)
    for p in pecas: p.select_set(True)
    bpy.context.view_layer.objects.active = pecas[0]
    with bpy.context.temp_override(active_object=pecas[0], selected_editable_objects=pecas, selected_objects=pecas):
        bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active; o.name = nome; o.data.name = nome; return o

def mao(name, s, material, osso):
    # Mão com palma, dedos juntos e polegar, na direção do braço: lê como mão mesmo pequena.
    b = pontos_braco(s); d = Vector((BRACO_DIR.x*s, 0, BRACO_DIR.z)); ang = math.atan2(d.x, d.z)
    palma = b[2] + d*0.048; dedos = b[2] + d*0.098; pol = b[2] + d*0.05 + Vector((0, -0.034, 0.0))
    p = [esfera(name+"_palma", (0.036, 0.02, 0.05), tuple(palma), material, 10, 7, (0, ang, 0)),
         esfera(name+"_dedos", (0.034, 0.018, 0.04), tuple(dedos), material, 10, 7, (0.25, ang, 0)),
         esfera(name+"_polegar", (0.013, 0.013, 0.03), tuple(pol), material, 8, 6, (0.5, ang, 0.5*s))]
    return [rigido(o, osso) for o in p]

def sapato(name, s, material_cima, material_sola):
    # Pé de tênis ou bota, grosso e arredondado: casco do calcanhar à ponta e sola alta.
    x = 0.07*s; lado = "Left" if s > 0 else "Right"
    secs = [(Vector((x, 0.085, 0.085)), (1, 0, 0), (0, 0, 1), 0.060, 0.074), (Vector((x, 0.02, 0.092)), (1, 0, 0), (0, 0, 1), 0.076, 0.09),
            (Vector((x, -0.06, 0.074)), (1, 0, 0), (0, 0, 1), 0.082, 0.072), (Vector((x, -0.15, 0.058)), (1, 0, 0), (0, 0, 1), 0.074, 0.056),
            (Vector((x, -0.215, 0.048)), (1, 0, 0), (0, 0, 1), 0.048, 0.04)]
    return [rigido(suavizar(loft(name+"_casco", secs, material_cima, 12, True, True), 1), lado+"Foot"),
            rigido(suavizar(esfera(name+"_sola", (0.086, 0.17, 0.024), (x, -0.062, 0.024), material_sola, 14, 6), 0), lado+"Foot")]

def arco_pts(cx, cy, z, raio, a0, a1, n=10, sobe=0.0):
    # Pontos de um arco em volta da cabeça (a0 e a1 em graus, 0 = frente); "sobe" levanta o meio, como numa tiara.
    return [(cx + raio*math.sin(math.radians(a0+(a1-a0)*i/n)), cy - raio*math.cos(math.radians(a0+(a1-a0)*i/n)), z + sobe*math.sin(math.pi*i/n)) for i in range(n+1)]

def pano(name, z0, z1, th0, th1, material, folga0, flare, n_th=12, n_z=6):
    # Pano que cai das costas: segue o tronco com uma folga que cresce para baixo (a capa), com espessura de tecido.
    bm = bmesh.new(); linhas = []
    for j in range(n_z+1):
        z = z0 + (z1-z0)*j/n_z; rx, ry = interp(TRONCO, z); m = folga0 + (0.99-z)*flare; linha = []; dobra = (0.99-z)*0.03
        for i in range(n_th+1):
            th = math.radians(th0 + (th1-th0)*i/n_th); linha.append(bm.verts.new((math.sin(th)*(rx+m+dobra*math.sin(th*4.0)), math.cos(th)*(ry+m+dobra*math.sin(th*4.0)), z)))
        linhas.append(linha)
    for j in range(n_z):
        for i in range(n_th): bm.faces.new((linhas[j][i], linhas[j][i+1], linhas[j+1][i+1], linhas[j+1][i]))
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for q in me.polygons: q.use_smooth = True
    me.materials.append(material); o = bpy.data.objects.new(name, me); col.objects.link(o)
    sd = o.modifiers.new("tecido", "SOLIDIFY"); sd.thickness = 0.014; sd.offset = 0
    return o

def anel_liso(name, centro, normal, raio, r, material, n=20):
    # Aro redondo e liso (óculos, fecho): curva fechada com espessura.
    nv = Vector(normal).normalized(); u = nv.cross(Vector((0, 0, 1)))
    if u.length < 0.1: u = nv.cross(Vector((1, 0, 0)))
    u.normalize(); v = nv.cross(u)
    return fio(name, [tuple(Vector(centro) + (u*math.cos(2*math.pi*i/n) + v*math.sin(2*math.pi*i/n))*raio) for i in range(n)], r, material, 6, False, True)[0]

def engrenagem_fio(name, centro, raio, dentes, r, material):
    # Contorno de engrenagem em curva fechada, de frente (plano XZ): emblema bordado, sem letra nem número.
    cx, cy, cz = centro; pts = []
    for i in range(dentes*4):
        a = 2*math.pi*i/(dentes*4); k = raio if i % 4 in (1, 2) else raio*0.72
        pts.append((cx + k*math.cos(a), cy, cz + k*math.sin(a)))
    return fio(name, pts, r, material, 6, False, True)

def arco_cabeca(z, a0, a1, n=10, margem=0.02, sobe=0.0):
    # Pontos de um arco que acompanha a cabeça na altura z (a0 e a1 em graus, 0 = frente); "sobe" levanta o meio, como numa tiara.
    rx, ry, cy = interp(CABECA, z); pts = []
    for i in range(n+1):
        a = math.radians(a0 + (a1-a0)*i/n); pts.append(((rx+margem)*math.sin(a), cy - (ry+margem)*math.cos(a), z + sobe*math.sin(math.pi*i/n)))
    return pts

# ---- animação: um ciclo por ação; as ações saem juntas no GLB ----
def gira(nome, rx=0.0, ry=0.0, rz=0.0):
    # Gira o osso em graus sobre os eixos do mundo (X é o balanço de frente para trás), seja qual for o roll dele.
    pb = esqueleto().pose.bones[nome]; r = pb.bone.matrix_local.to_3x3()
    w = Matrix.Rotation(math.radians(rz), 3, "Z") @ Matrix.Rotation(math.radians(ry), 3, "Y") @ Matrix.Rotation(math.radians(rx), 3, "X")
    pb.rotation_mode = "XYZ"; pb.rotation_euler = (r.inverted() @ w @ r).to_euler("XYZ")

def sobe(nome, dz):
    pb = esqueleto().pose.bones[nome]; r = pb.bone.matrix_local.to_3x3(); pb.location = r.inverted() @ Vector((0, 0, dz))

def zerar_pose():
    for pb in esqueleto().pose.bones:
        pb.rotation_mode = "XYZ"; pb.rotation_euler = (0, 0, 0); pb.location = (0, 0, 0)

def quadro(frame, giros, quadril_z=0.0):
    # giros: {osso: (rx, ry, rz)} em graus; o quadril sobe e desce em quadril_z (metros).
    zerar_pose()
    for n, g in giros.items(): gira(n, g[0], g[1], g[2])
    sobe("Hips", quadril_z)
    for pb in esqueleto().pose.bones:
        pb.keyframe_insert("rotation_euler", frame=frame)
        if pb.name == "Hips": pb.keyframe_insert("location", frame=frame)

def nova_acao(nome, quadros):
    arm = esqueleto(); zerar_pose()
    if arm.animation_data is None: arm.animation_data_create()
    antiga = bpy.data.actions.get(nome)
    if antiga is not None: bpy.data.actions.remove(antiga)
    a = bpy.data.actions.new(nome); arm.animation_data.action = a
    arm.pose.bones["Hips"].rotation_mode = "XYZ"
    bpy.context.scene.frame_start = 1; bpy.context.scene.frame_end = quadros
    return a

# ---- rosto pintado: textura com transparência colada num remendo sobre a cabeça ----


# A textura cobre este retângulo da frente da cabeça, em metros (antes de a cabeça crescer): largura de X0 a X1 e altura de Z0 a Z1.
X0 = -0.22
X1 = 0.22
Z0 = 1.02
Z1 = 1.46
TAMANHO = 1024

def cor_plana(nome, hexcol):
    return mat("rosto_" + nome, hexcol, 1.0)

def disco(cena, nome, cx, cz, rx, rz, cor, camada, rot=0.0):
    # Elipse chapada no plano XZ; camada maior fica mais perto da câmera (que olha de frente).
    bm = bmesh.new(); bmesh.ops.create_circle(bm, cap_ends=True, radius=1.0, segments=40)
    for v in bm.verts:
        x = v.co.x*rx; z = v.co.y*rz
        v.co = Vector((cx + x*math.cos(rot) - z*math.sin(rot), -0.001*camada, cz + x*math.sin(rot) + z*math.cos(rot)))
    me = bpy.data.meshes.new(nome); bm.to_mesh(me); bm.free(); me.materials.append(cor)
    o = bpy.data.objects.new(nome, me); cena.collection.objects.link(o); return o

def traco(cena, nome, pts, raio, cor, camada, afinar=True):
    # Traço de espessura variável (a pincelada): afina nas pontas.
    cu = bpy.data.curves.new(nome, "CURVE"); cu.dimensions = "3D"; cu.bevel_depth = raio; cu.bevel_resolution = 4; cu.use_fill_caps = True; cu.resolution_u = 12
    sp = cu.splines.new("BEZIER"); sp.bezier_points.add(len(pts)-1); n = len(pts)
    for i, bp in enumerate(sp.bezier_points):
        bp.co = (pts[i][0], -0.001*camada, pts[i][1]); bp.handle_left_type = bp.handle_right_type = "AUTO"
        bp.radius = 0.25 + 0.75*math.sin(math.pi*i/(n-1))**0.6 if afinar else 1.0
    cu.materials.append(cor); o = bpy.data.objects.new(nome, cu); cena.collection.objects.link(o); return o

def pintar_rosto(caminho, quem):
    cena = bpy.data.scenes.new("rosto_tex")
    cena.render.engine = "BLENDER_WORKBENCH"; cena.display.shading.light = "FLAT"; cena.display.shading.color_type = "MATERIAL"
    cena.render.film_transparent = True; cena.view_settings.view_transform = "Standard"; cena.view_settings.look = "None"; cena.display.render_aa = "32"; cena.render.resolution_x = TAMANHO; cena.render.resolution_y = TAMANHO; cena.render.resolution_percentage = 100
    cena.render.image_settings.file_format = "PNG"; cena.render.image_settings.color_mode = "RGBA"
    cam = bpy.data.objects.new("cam_rosto", bpy.data.cameras.new("cam_rosto")); cam.data.type = "ORTHO"; cam.data.ortho_scale = X1 - X0
    cam.location = (0.0, -2.0, (Z0 + Z1)/2); cam.rotation_euler = (math.pi/2, 0, 0); cena.collection.objects.link(cam); cena.camera = cam
    if quem == "guia": desenhar_rosto_guia(cena)
    else: desenhar_rosto_padrao(cena)
    cena.render.filepath = caminho
    bpy.ops.render.render(write_still=True, scene=cena.name)
    for o in list(cena.collection.objects): bpy.data.objects.remove(o, do_unlink=True)
    bpy.data.scenes.remove(cena)

def malha_da_cabeca():
    # A cabeça já suavizada, só para medir onde está a superfície de verdade (a subdivisão a encolhe) e apoiar o decalque nela.
    o = suavizar(loft("tmp_cabeca", secoes_z(CABECA, 1.03, 1.44, 12, 0.0, 0.0, True), P["pele"], 18), 1)
    dg = bpy.context.evaluated_depsgraph_get(); arv = BVHTree.FromObject(o, dg)
    return o, arv

def remendo(imagem, quem):
    # Grade sobre a frente da cabeça, apoiada na superfície medida, com UV de 0 a 1 em cima do retângulo da textura.
    cab, arv = malha_da_cabeca(); nx = 20; nz = 20
    bm = bmesh.new(); uv = bm.loops.layers.uv.new("UVMap"); linhas = []
    for j in range(nz+1):
        linha = []
        for i in range(nx+1):
            u = i/nx; v = j/nz; x = X0 + (X1-X0)*u; z = Z0 + (Z1-Z0)*v
            rx = interp(CABECA, z)[0]; xc = max(-rx*0.97, min(rx*0.97, x))
            hit = arv.ray_cast(Vector((xc, -1.0, z)), Vector((0, 1, 0)))
            p = hit[0] if hit[0] is not None else Vector((xc, -0.1, z))
            linha.append((bm.verts.new((p.x, p.y - 0.004, p.z)), u, v))
        linhas.append(linha)
    for j in range(nz):
        for i in range(nx):
            f = bm.faces.new((linhas[j][i][0], linhas[j][i+1][0], linhas[j+1][i+1][0], linhas[j+1][i][0]))
            for lp, (vv, u, v) in zip(f.loops, (linhas[j][i], linhas[j][i+1], linhas[j+1][i+1], linhas[j+1][i])): lp[uv].uv = (u, v)
    me = bpy.data.meshes.new("Rosto_remendo"); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    m = bpy.data.materials.get("rosto_" + quem) or bpy.data.materials.new("rosto_" + quem); m.use_nodes = True
    nos = m.node_tree.nodes; lig = m.node_tree.links; bsdf = next(n for n in nos if n.type == "BSDF_PRINCIPLED")
    tex = nos.new("ShaderNodeTexImage"); tex.image = imagem; tex.interpolation = "Linear"
    lig.new(tex.outputs["Color"], bsdf.inputs["Base Color"]); lig.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.55
    m.surface_render_method = "BLENDED"
    me.materials.append(m); o = bpy.data.objects.new("Rosto_remendo", me); col.objects.link(o)
    virar_para_fora(o, Vector((0, 0.0, 1.24)))
    bpy.data.objects.remove(cab, do_unlink=True)
    return o

def construir_rosto(quem="padrao"):
    apagar("Rosto_" + quem)
    caminho = bpy.app.tempdir + "/rosto_" + quem + ".png"
    pintar_rosto(caminho, quem)
    img = bpy.data.images.load(caminho); img.name = "rosto_" + quem; img.pack()
    o = rigido(remendo(img, quem), "Head"); o.name = "Rosto_" + quem; o.data.name = "Rosto_" + quem
# <<< comum

def sup(x, z):
    # Frente da cabeça um pouco para dentro, porque a subdivisão encolhe a superfície: o rosto se apoia nela sem afundar.
    return superficie_cabeca(x, z) - 0.004

def construir_pele():
    p = [rigido(suavizar(loft("Pele_cabeca", secoes_z(CABECA, 1.03, 1.44, 12, 0.0, 0.0, True), P["pele"], 18), 1), "Head")]
    p.append(rigido(esfera("Pele_nariz", (0.018, 0.017, 0.02), (0, sup(0, 1.12) - 0.002, 1.12), P["pele"], 8, 6), "Head"))
    for s in (-1, 1):
        p.append(rigido(suavizar(esfera(f"Pele_orelha{s}", (0.026, 0.03, 0.05), (0.176*s, 0.02, 1.17), P["pele"], 12, 8, (0, 0, 0.2*s)), 1), "Head"))
        p.append(rigido(esfera(f"Pele_orelha_in{s}", (0.012, 0.016, 0.03), (0.196*s, 0.012, 1.17), P["pele_sombra"], 8, 6, (0, 0, 0.2*s)), "Head"))
        p.append(ligar_braco(suavizar(loft(f"Pele_braco{s}", secoes_membro(pontos_braco(s), PERFIL_BRACO), P["pele"], 10, False, False), 1), s))
        p += mao(f"Pele_mao{s}", s, P["pele"], ("Left" if s > 0 else "Right") + "Hand")
        p.append(ombreira(f"Pele_ombro{s}", s, 0.066, P["pele"]))
        p.append(ligar_perna(suavizar(loft(f"Pele_perna{s}", secoes_membro(pontos_perna(s), PERFIL_PERNA), P["pele"], 10, False, False), 1), s))
    p.append(rigido(loft("Pele_pescoco", [(Vector((0, 0, 0.95)), (1, 0, 0), (0, 1, 0), 0.075, 0.07), (Vector((0, 0, 1.08)), (1, 0, 0), (0, 1, 0), 0.066, 0.062)], P["pele"], 12, False, False), "neck"))
    return unir("Corpo", p)

def construir_roupa_basica():
    # Camiseta, shorts e meias: tingidas pelo jogo ("roupa"); os kits dos Papéis vestem por cima e o jogo esconde esta.
    p = [ligar_tronco(suavizar(loft("Roupa_camiseta", secoes_z(TRONCO, 0.6, 0.99, 8, 0.008), P["roupa"], 16, False, False), 1))]
    p.append(ligar_tronco(loft("Roupa_shorts", secoes_z(TRONCO, 0.54, 0.64, 3, 0.014), P["roupa"], 18, False, False)))
    for s in (-1, 1):
        p.append(manga(f"Roupa_manga{s}", s, 0.0, 0.3, 0.008, P["roupa"], 12))
        p.append(ombreira(f"Roupa_ombro{s}", s, 0.082, P["roupa"]))
        p.append(calca(f"Roupa_calcao{s}", s, 0.0, 0.3, 0.014, P["roupa"], 12))
        p.append(calca(f"Roupa_meia{s}", s, 0.8, 1.0, 0.006, P["meia"], 12))
        p += sapato(f"Roupa_pe{s}", s, P["meia"], P["meia"])
    return unir("Roupa_basica", p)

def suave(t):
    t = max(0.0, min(1.0, t))
    return t*t*(3 - 2*t)

def linha_do_cabelo(x, y):
    # Altura da borda de baixo do cabelo em cada ponto da cabeça: curva na testa, desce nas têmporas, deixa a orelha livre e chega à nuca.
    ax = abs(x); frente = 1.33 - 0.36*max(0.0, ax - 0.09)
    t = suave((y + 0.06)/0.16); h = frente*(1 - t) + 1.12*t
    e = suave((ax - 0.15)/0.03)*suave((y + 0.11)/0.05)*suave((0.1 - y)/0.05)
    return max(h, h + e*(1.25 - h))

def casca_do_cabelo():
    # Uma casca só em volta do crânio: cada ponto sobe ou desce até a linha do cabelo, sem recorte e sem buraco.
    m = 0.03; z0 = 1.1; ztop = 1.44
    o = loft("Cabelo_casca", secoes_z(CABECA, z0, ztop, 14, m, 0.0, True), P["cabelo"], 28, False, True)
    for v in o.data.vertices:
        c = v.co; rx0, ry0, cy0 = interp(CABECA, c.z); h = linha_do_cabelo(c.x, c.y)
        z = h + (c.z - z0)*(ztop - h)/(ztop - z0); rx1, ry1, cy1 = interp(CABECA, z)
        v.co = Vector((c.x*(rx1 + m)/(rx0 + m), cy1 + (c.y - cy0)*(ry1 + m)/(ry0 + m), z))
    return suavizar(o, 1)

def mecha_na_cabeca(nome, theta, z0, z1, larg, esp, saida=0.03):
    # Mecha que desce abraçando a cabeça (theta: 0 = frente, 90 = lado, 180 = nuca) e solta a ponta um pouco para fora.
    th = math.radians(theta); pts = []
    for z in (z0, (z0 + z1)/2, z1):
        rx, ry, cy = interp(CABECA, z); pts.append(Vector(((rx + 0.04)*math.sin(th), cy - (ry + 0.04)*math.cos(th), z)))
    pts[2] = pts[2] + Vector((math.sin(th), -math.cos(th), 0))*saida
    return rigido(mecha(nome, pts[0], pts[1], pts[2], (larg, esp), P["cabelo"], 12, 8, 0.016, Vector((math.sin(th), -math.cos(th), 0))), "Head")

def construir_cabelo():
    # Cabelo padrão: casca única com linha do cabelo, franja varrida, mechas nas têmporas e na nuca que abraçam a cabeça e um redemoinho no alto.
    p = [rigido(casca_do_cabelo(), "Head")]
    mechas = [(-52, 1.46, 1.30, 0.075), (-26, 1.47, 1.275, 0.08), (0, 1.47, 1.265, 0.08), (26, 1.47, 1.285, 0.08), (52, 1.46, 1.31, 0.075),
              (-78, 1.42, 1.25, 0.07), (78, 1.42, 1.25, 0.07), (-118, 1.4, 1.1, 0.07), (118, 1.4, 1.1, 0.07),
              (-148, 1.38, 1.06, 0.075), (-175, 1.38, 1.03, 0.08), (175, 1.38, 1.03, 0.08), (148, 1.38, 1.06, 0.075)]
    for i, (theta, z0, z1, larg) in enumerate(mechas):
        p.append(mecha_na_cabeca(f"Cabelo_mecha{i}", theta, z0, z1 - 0.03*math.sin(i*1.9), larg*1.05, 0.03))
    topo = [((-0.03, -0.03, 1.49), (-0.07, 0.0, 1.53), (-0.13, 0.04, 1.5)), ((0.04, -0.03, 1.49), (0.08, 0.0, 1.53), (0.15, 0.05, 1.49)), ((0.0, 0.05, 1.49), (0.0, 0.09, 1.53), (-0.02, 0.15, 1.53))]
    for i, (a, b, c) in enumerate(topo):
        p.append(rigido(mecha(f"Cabelo_topo{i}", a, b, c, (0.085, 0.02), P["cabelo"], 12, 8, 0.016), "Head"))
    return unir("Cabelo_padrao", p)

def construir_corpo():
    for prefixo in ("Corpo", "Roupa_", "Cabelo_", "Pele_"): apagar(prefixo)
    esqueleto_do_personagem()
    construir_pele(); construir_roupa_basica(); construir_cabelo()

construir_corpo()
