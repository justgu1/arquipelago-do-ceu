using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Equipment;
using ArquipelagoDoCeu.Saves;
using Godot;

namespace ArquipelagoDoCeu;

// O corpo do Personagem: um GLB só, feito no Blender por script (tools/blender/personagem), com o esqueleto e todas as peças como malhas
// separadas no mesmo esqueleto. Aqui cada peça liga e desliga pelo nome e a cor entra pelo nome do material.
public partial class CharacterModel : Node3D
{
	public const string ModelPath = "res://assets/modelos/personagem/personagem.glb";
	const float Height = 1.5f;
	const float ModelHeight = 1.84f;
	// Velocidade de apoio dos pés em cada ciclo, já na escala do jogo: a animação acelera ou desacelera para os pés não deslizarem.
	const float WalkStrideSpeed = 1.5f;
	const float RunStrideSpeed = 3.6f;

	static readonly string[] Base = ["Corpo", "Rosto_padrao", "Cabelo_padrao"];
	static readonly Dictionary<string, string> KitOfRole = new() { ["matematica"] = "Matematico_", ["portugues"] = "Escriba_" };

	readonly Dictionary<string, MeshInstance3D> parts = new();
	readonly Dictionary<string, Color> tints = new();
	AnimationPlayer? animations;

	public bool IdleOnly { get; init; }
	public bool ReduceMotion { get; set; }
	public int VisiblePartCount => parts.Values.Count(p => p.Visible);
	public bool IsWalking => animations is { } a && a.IsPlaying() && a.CurrentAnimation != "parado";
	public bool Loaded => animations is not null;

	public override void _Ready()
	{
		if (GD.Load<PackedScene>(ModelPath) is not { } scene)
		{
			GD.PushError($"Não foi possível carregar o modelo do Personagem em {ModelPath}.");
			return;
		}
		var model = scene.Instantiate<Node3D>();
		model.Scale = Vector3.One * (Height / ModelHeight);
		AddChild(model);
		foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()) parts[mesh.Name] = mesh;
		animations = model.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().FirstOrDefault();
		foreach (var name in animations?.GetAnimationList() ?? [])
			animations!.GetAnimation(name).LoopMode = Animation.LoopModeEnum.Linear;
		animations?.Play("parado");
		Show(new EquipmentLoadout());
	}

	// Veste o que o Perfil escolheu: o kit do Papel (ou a roupa básica), o equipamento e os Cosméticos. Com "alone" mostra só o equipamento, sem corpo (a prévia de um item).
	public void Show(EquipmentLoadout loadout, bool alone = false)
	{
		var visible = new HashSet<string>();
		if (!alone)
		{
			visible.UnionWith(Base);
			var kit = KitOfRole.GetValueOrDefault(loadout.Role);
			if (kit is null) visible.Add("Roupa_basica");
			else visible.UnionWith(parts.Keys.Where(n => n.StartsWith(kit)));
		}
		foreach (var id in new[] { loadout.Armor, loadout.Helmet, loadout.Accessory })
			if (id is not ("original" or "none")) visible.Add("Equip_" + id);
		var cosmetics = alone ? [] : (loadout.Cosmetics ?? []).ToList();
		foreach (var look in cosmetics) visible.Add("Cosm_" + look.Kind);
		foreach (var (name, mesh) in parts) mesh.Visible = visible.Contains(name);
		Paint(cosmetics);
		// As cores do equipamento do conjunto (pano, metal e detalhe) tingem as peças de equipamento pelos nomes dos materiais.
		SetTint("equip_pano", Color.FromHtml(loadout.ClothColor));
		SetTint("equip_metal", Color.FromHtml(loadout.MetalColor));
		SetTint("equip_detalhe", Color.FromHtml(loadout.AccentColor));
	}

	// Tom de pele, cor da roupa básica e do cabelo: o material "pele", "roupa" ou "cabelo" de todas as peças vira a cor escolhida.
	public void SetTint(string material, Color color)
	{
		tints[material] = color;
		foreach (var mesh in parts.Values) Recolor(mesh, material, color);
	}

	void Paint(IReadOnlyList<CosmeticLook> cosmetics)
	{
		foreach (var look in cosmetics)
			if (parts.TryGetValue("Cosm_" + look.Kind, out var mesh)) Recolor(mesh, "cosmetico", Color.FromHtml(look.Color));
	}

	static void Recolor(MeshInstance3D mesh, string material, Color color)
	{
		for (var i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
		{
			if (mesh.Mesh?.SurfaceGetMaterial(i) is not StandardMaterial3D original || original.ResourceName != material) continue;
			var tinted = (StandardMaterial3D)original.Duplicate();
			tinted.AlbedoColor = color;
			mesh.SetSurfaceOverrideMaterial(i, tinted);
		}
	}

	public void SetLocomotion(float speed, bool running)
	{
		if (animations is null) return;
		var moving = speed > .015f && !ReduceMotion && !IdleOnly;
		var desired = moving ? (running ? "correndo" : "andando") : "parado";
		animations.SpeedScale = moving ? speed / (running ? RunStrideSpeed : WalkStrideSpeed) : 1f;
		if (animations.CurrentAnimation != desired) animations.Play(desired, .1);
		if (ReduceMotion) { animations.Seek(0, true); animations.Pause(); }
		else if (!animations.IsPlaying()) animations.Play(desired);
	}
}
