using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// A ambientação da Sala: as cores do tema da torre (fundo, luz), o brilho das brasas e a poeira que flutua no ar.
public partial class DungeonWorld
{
	readonly DirectionalLight3D sun = new() { LightEnergy = 0.6f, ShadowEnabled = true, RotationDegrees = new Vector3(-50f, -30f, 0f) };
	DungeonTheme theme = DungeonTheme.Numbers;
	readonly List<RoomFurniture.Prop> props = [];
	CpuParticles3D? dust;

	public DungeonTheme Theme
	{
		get => theme;
		set
		{
			theme = value;
			if (IsInsideTree()) ApplyTheme();
		}
	}

	void ApplyTheme()
	{
		sun.LightColor = Color.FromHtml(theme.Sun);
		if (Camera.Environment is { } environment)
		{
			environment.BackgroundColor = Color.FromHtml(theme.Void);
			environment.AmbientLightColor = Color.FromHtml(theme.Ambient);
			environment.AmbientLightEnergy = 0.5f;
			// O brilho das brasas e do anel de ouro: leve, para não cansar a vista de quem tem sensibilidade à luz.
			environment.GlowEnabled = !ReduceMotion;
			environment.GlowIntensity = 0.5f;
			environment.GlowBloom = 0.05f;
		}
		AddDust();
	}

	// Sorteia a mobília da Sala sem fechar nenhum caminho: o que ela ocupa sai da grade de passagem. As portas, o centro e a escada lateral ficam livres.
	IReadOnlyList<RoomWalls.Piece> Furnish(PlacedRoom here, int floorNumber, HashSet<HexCell> walkable, HashSet<HexCell> blocked, Dictionary<HexCell, float> heights)
	{
		var cells = here.Cells.Select(Local).ToHashSet();
		var keep = doors.Select(d => HexGrid.FromWorld(new System.Numerics.Vector2(d.Ground.X, d.Ground.Z))).ToHashSet();
		keep.UnionWith(keep.SelectMany(c => c.Neighbors()).ToList());
		keep.Add(Local(here.SideSpot));
		var seed = Seed.Mix((ulong)(here.Origin.Q * 7919 + here.Origin.R), here.Form.Id, floorNumber, currentId, 1);
		var plan = RoomFurnishing.Plan(cells, walkable, keep, Local(here.Spot), seed);
		blocked.UnionWith(plan.Select(f => f.Cell));
		props.Clear();
		return RoomFurniture.Add(room, plan, walkable, heights, theme, theme == DungeonTheme.Letters, props);
	}

	public IReadOnlyList<RoomFurniture.Prop> Props => props;

	public string PropPrompt(string id) => props.FirstOrDefault(p => p.Id == id)?.Prompt + " [E]";

	// A peça dá um pulinho e a criança ouve o que ela é; sem "reduzir animações" só a frase.
	public string? UseProp(string id)
	{
		if (props.FirstOrDefault(p => p.Id == id) is not { } prop) return null;
		if (!ReduceMotion && GodotObject.IsInstanceValid(prop.Model))
		{
			var scale = prop.Model.Scale;
			var bounce = CreateTween();
			bounce.TweenProperty(prop.Model, "scale", scale * 1.12f, 0.12);
			bounce.TweenProperty(prop.Model, "scale", scale, 0.18);
		}
		return prop.Line;
	}

	// Poucos grãos de luz subindo devagar em volta da Sala; sem eles com "reduzir animações".
	void AddDust()
	{
		dust?.QueueFree();
		dust = null;
		if (ReduceMotion) return;
		var mote = new QuadMesh { Size = new Vector2(0.07f, 0.07f) };
		mote.Material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			AlbedoColor = Color.FromHtml(theme.Dust) with { A = 0.55f },
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};
		dust = new CpuParticles3D
		{
			Name = "Poeira",
			Amount = 36,
			Lifetime = 9.0,
			Preprocess = 9.0,
			Mesh = mote,
			EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(7f, 0.2f, 7f),
			Direction = Vector3.Up,
			Spread = 20f,
			Gravity = Vector3.Zero,
			InitialVelocityMin = 0.1f,
			InitialVelocityMax = 0.35f,
			Position = new Vector3(0f, 0.3f, 0f),
		};
		AddChild(dust);
	}
}
