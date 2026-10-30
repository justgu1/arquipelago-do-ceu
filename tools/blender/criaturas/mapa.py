# Mapa (Torre das Letras): objeto com olhos, punhos fechados e tênis. Rode no Blender (MCP em modo seguro ou blender -b -P).
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
# <<< comum

ORIGEM_X = 0.0

def construir_mapa(ox):
    apagar("Mapa")
    raiz = vazio("Mapa", (ox, 0, 0), None)
    xs = [-0.48, -0.16, 0.16, 0.48]; ys = [0.17, 0.0, 0.0, 0.17]; z0, z1 = 0.40, 1.20
    lens = [math.hypot(xs[i+1]-xs[i], ys[i+1]-ys[i]) for i in range(3)]; total = sum(lens)
    cum = [0.0]
    for l in lens: cum.append(cum[-1] + l/total)
    bm = bmesh.new()
    bot = [bm.verts.new((x, y, z0)) for x, y in zip(xs, ys)]; top = [bm.verts.new((x, y, z1)) for x, y in zip(xs, ys)]
    for i in range(3): bm.faces.new((bot[i], bot[i+1], top[i+1], top[i]))
    corpo = objeto("Mapa_corpo", bm, [P["papel"]], raiz, False, (0, 0, -0.1))
    s = corpo.modifiers.new("espessura", "SOLIDIFY"); s.thickness = 0.06; s.offset = 0
    b = corpo.modifiers.new("chanfro", "BEVEL"); b.width = 0.018; b.segments = 2
    corpo.rotation_euler = (0, 0.05, 0)
    # As marcas do mapa são folhas coladas nos três painéis; (s, v) vai de 0 a 1 pelo mapa inteiro, atravessando as dobras.
    def locate(s):
        for i in range(3):
            if s <= cum[i+1]+1e-9: return i, (s-cum[i])/(cum[i+1]-cum[i])
        return 2, 1.0
    def frame(i, u, v, lift):
        a = Vector((xs[i], ys[i], 0)); c = Vector((xs[i+1], ys[i+1], 0)); n = (c-a).normalized()
        p = a.lerp(c, u); normal = Vector((n.y, -n.x, 0))
        return Vector((p.x, p.y, z0+(z1-z0)*v)) + normal*lift, math.atan2(n.y, n.x)
    def flat(name, s, v, sx, sz, material, rot=0.0, lift=0.034, segs=24, shape=None):
        i, u = locate(s); pos, ang = frame(i, u, v, lift); bm = bmesh.new()
        if shape is None: bmesh.ops.create_circle(bm, cap_ends=True, segments=segs, radius=0.5)
        else: bm.faces.new([bm.verts.new((x, y, 0)) for x, y in shape])
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.pi/2, 3, 'X'))
        bmesh.ops.scale(bm, vec=Vector((sx, 1, sz)), verts=bm.verts)
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(rot, 3, 'Y'))
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(ang, 3, 'Z'))
        bmesh.ops.translate(bm, vec=pos, verts=bm.verts)
        return objeto("Mapa_"+name, bm, [material], corpo, False)
    quad = [(-0.5, -0.5), (0.5, -0.5), (0.5, 0.5), (-0.5, 0.5)]; tri = [(-0.5, -0.5), (0.5, -0.5), (0, 0.5)]
    for i in range(3):
        sm = (cum[i]+cum[i+1])/2
        flat(f"mold_sup{i}", sm, 0.95, lens[i], 0.013, P["moldura"], shape=quad)
        flat(f"mold_inf{i}", sm, 0.05, lens[i], 0.013, P["moldura"], shape=quad)
        flat(f"vinco_h{i}", sm, 0.5, lens[i], 0.008, P["vinco"], lift=0.033, shape=quad)
    flat("mold_e", 0.035*cum[1], 0.5, 0.013, 0.73, P["moldura"], shape=quad)
    flat("mold_d", cum[2]+0.965*(cum[3]-cum[2]), 0.5, 0.013, 0.73, P["moldura"], shape=quad)
    for i in range(3): flat(f"mar{i}", (cum[i]+cum[i+1])/2, 0.5, lens[i]*0.9, 0.7, P["mar"], shape=quad)
    def ilha(nome, s, v, sx, sz, rot):
        flat(f"raso_{nome}", s, v, sx*1.42, sz*1.34, P["onda"], rot, 0.0355, 28)
        flat(f"costa_{nome}", s, v, sx*1.16, sz*1.12, P["costa"], rot, 0.037, 28)
        flat(f"ilha_{nome}", s, v, sx, sz, P["terra"], rot, 0.0385, 28)
    ilha("partida", 0.17, 0.30, 0.13, 0.17, 0.4); ilha("meio", 0.50, 0.16, 0.15, 0.10, -0.2); ilha("tesouro", 0.80, 0.52, 0.24, 0.30, 0.25)
    flat("marca_anel", 0.17, 0.30, 0.075, 0.075, P["branco"], lift=0.040)
    flat("marca_miolo", 0.17, 0.30, 0.045, 0.045, P["rota"], lift=0.0415)
    X0 = 0.80*total; Z0 = 0.52*(z1-z0)
    for k, (dx, dz, w, h) in enumerate([(0.045, -0.005, 0.06, 0.06), (0.030, 0.078, 0.045, 0.045), (0.078, 0.050, 0.04, 0.04)]):
        sm = (X0+dx)/total; v = (Z0+dz)/(z1-z0)
        flat(f"monte{k}", sm, v, w, h, P["monte"], lift=0.041, shape=tri)
        flat(f"neve{k}", sm, v+h*0.325/(z1-z0), w*0.36, h*0.36, P["neve"], lift=0.0425, shape=tri)
    onda = [(-0.5, 0), (-0.25, 0.5), (0, 0), (0.25, 0.5), (0.5, 0), (0.5, -0.3), (0.25, 0.2), (0, -0.3), (-0.25, 0.2), (-0.5, -0.3)]
    for k, (s_, v) in zip([0, 1, 2, 5, 7, 8], [(0.06, 0.62), (0.12, 0.84), (0.26, 0.74), (0.62, 0.82), (0.93, 0.20), (0.72, 0.16)]):
        flat(f"onda{k}", s_, v, 0.07, 0.016, P["onda"], lift=0.0365, shape=onda)
    star = [(math.cos(a)*(0.5 if k % 2 == 0 else 0.15), math.sin(a)*(0.5 if k % 2 == 0 else 0.15)) for k, a in enumerate([j*math.pi/4 for j in range(8)])]
    flat("rosa_aro", 0.905, 0.86, 0.085, 0.085, P["borda"], lift=0.038)
    flat("rosa", 0.905, 0.86, 0.12, 0.12, P["tinta"], lift=0.0395, shape=star)
    # Rota: curva suave (Catmull-Rom) em metros, porque o traço precisa seguir o caminho mesmo passando pelas dobras.
    ctrl = [(0.17, 0.30), (0.24, 0.25), (0.32, 0.19), (0.41, 0.155), (0.50, 0.16), (0.59, 0.19), (0.66, 0.28), (0.72, 0.40), (0.755, 0.49), (0.772, 0.52)]
    pm = [Vector((s_*total, v*(z1-z0))) for s_, v in ctrl]
    def cr(p0, p1, p2, p3, t): return 0.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
    pts = []; Q = [pm[0]] + pm + [pm[-1]]
    for k in range(1, len(Q)-2):
        for j in range(24): pts.append(cr(Q[k-1], Q[k], Q[k+1], Q[k+2], j/24))
    pts.append(pm[-1]); acc = [0.0]
    for a, b in zip(pts[:-1], pts[1:]): acc.append(acc[-1] + (b-a).length)
    fronteiras = [cum[1]*total, cum[2]*total]; feito = 0
    for k in range(1, int(acc[-1]/0.052)):
        d = k*0.052; j = min(max(x for x in range(len(acc)) if acc[x] <= d), len(pts)-2)
        t = (d-acc[j])/max(1e-6, acc[j+1]-acc[j]); p = pts[j].lerp(pts[j+1], t); tg = (pts[j+1]-pts[j]).normalized()
        if min(abs(p.x-f) for f in fronteiras) < 0.024: continue
        flat(f"rota{feito}", p.x/total, p.y/(z1-z0), 0.04, 0.021, P["rota"], rot=-math.atan2(tg.y, tg.x), lift=0.0445, segs=14); feito += 1
    i, u = locate(0.772); base, _ = frame(i, u, 0.52, 0.036)
    nrm = Vector((0.4692, -0.8832, 0)); tang = Vector((0.8832, 0.4692, 0))
    dire = (nrm*0.45 + Vector((0, 0, 0.85)) - tang*0.3).normalized()
    alf = vazio("Mapa_alfinete", base, corpo); alf.rotation_mode = "QUATERNION"; alf.rotation_quaternion = dire.to_track_quat('Z', 'Y')
    L = 0.085
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=10, radius1=0.0065, radius2=0.0055, depth=L)
    bmesh.ops.translate(bm, vec=Vector((0, 0, L/2-0.012)), verts=bm.verts); objeto("Mapa_alfinete_agulha", bm, [P["metal"]], alf, True)
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=14, radius1=0.022, radius2=0.011, depth=0.016)
    bmesh.ops.translate(bm, vec=Vector((0, 0, L-0.014)), verts=bm.verts); objeto("Mapa_alfinete_colarinho", bm, [P["metal"]], alf, True)
    elip("Mapa_alfinete_cabeca", (0.038, 0.038, 0.036), (0, 0, L+0.012), P["rota"], alf, seg=20, aneis=14)
    elip("Mapa_alfinete_brilho", (0.010, 0.007, 0.013), (-0.014, -0.022, L+0.026), P["branco"], alf, seg=12, aneis=8)
    tubo("Mapa_canto", [(0.40, 0.12, 1.205), (0.47, 0.13, 1.19), (0.5, 0.17, 1.15)], 0.028, P["papel"], corpo)
    kit_olhos("Mapa", corpo, 0.0, -0.05, 0.93, 0.118, 1.0, P["papel"])
    YB = -0.072; EZ = 0.93
    tubo("Mapa_sobrancelha-1", [(-0.205, YB, EZ+0.178), (-0.13, YB, EZ+0.153), (-0.05, YB, EZ+0.116)], 0.018, P["tinta"], corpo)
    tubo("Mapa_sobrancelha1", [(0.05, YB, EZ+0.125), (0.125, YB, EZ+0.17), (0.205, YB, EZ+0.205)], 0.018, P["tinta"], corpo)
    tubo("Mapa_boca", [(-0.06, -0.044, 0.765), (-0.025, -0.049, 0.775), (0.012, -0.049, 0.752), (0.05, -0.044, 0.768)], 0.012, P["tinta"], corpo)
    punho("Mapa_mao_e", raiz, (-0.62, -0.05, 0.56), (0.2, -0.4, -0.45), (-1, 1, 1))
    punho("Mapa_mao_d", raiz, (0.66, -0.05, 0.98), (0.15, 0.2, -0.3), (1, 1, 1))
    tenis("Mapa_pe_e", raiz, (-0.2, -0.1, 0.0), (0, 0, 0.18)); tenis("Mapa_pe_d", raiz, (0.2, -0.1, 0.0), (0, 0, -0.18))

construir_mapa(ORIGEM_X)
