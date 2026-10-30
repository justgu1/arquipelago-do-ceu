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
