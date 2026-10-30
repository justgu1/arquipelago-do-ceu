using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// A vida do vilarejo no céu: moradores que passeiam pelos caminhos e se juntam nos lampiões à noite, nuvens, pássaros, vaga-lumes e brilho nas torres.
// Tudo segue o tempo do jogo e o ciclo do dia; com a redução de animações fica parado, sem sumir.
public partial class LobbyLife : Node3D
{
	const float NightBelow = 0.3f;
	const int Villagers = 5;

	readonly Lobby lobby;
	readonly IReadOnlyList<HexCell> roads;
	readonly IReadOnlyList<HexCell> gathering;
	readonly List<Villager> villagers = new();
	readonly List<Node3D> clouds = new();
	readonly List<Node3D> birds = new();
	readonly List<(MeshInstance3D Mote, Vector3 Home, float Phase)> fireflies = new();
	readonly List<(MeshInstance3D Mote, Vector3 Center, float Radius, float Phase)> sparks = new();
	readonly List<(OmniLight3D Light, float Phase)> towerLights = new();
	readonly List<StandardMaterial3D> cloudMaterials = new();
	readonly List<(MeshInstance3D Puff, StandardMaterial3D Material, Vector3 Origin, float Phase)> smoke = new();
	readonly List<(Node3D Node, float BaseY, float Phase)> floaters = new();
	double clock;

	public LobbyLife(Lobby lobby, IReadOnlyList<HexCell> roads, IReadOnlyList<HexCell> gathering)
	{
		this.lobby = lobby;
		this.roads = roads;
		this.gathering = gathering;
		Name = "LobbyLife";
	}

	public override void _Ready()
	{
		var random = new RandomNumberGenerator { Seed = 20261028 };
		AddVillagers(random);
		AddClouds(random);
		AddBirds(random);
		AddFireflies(random);
		AddTowerSparks();
		AddSmoke();
	}

	void AddVillagers(RandomNumberGenerator random)
	{
		if (roads.Count == 0) return;
		string[] roles = ["matematica", "portugues", "", "portugues", "matematica"];
		string[] clothes = ["#C8553D", "#4C9F70", "#8E6BBF", "#E0A030", "#3F7FBF"];
		for (var i = 0; i < Villagers; i++)
		{
			var home = roads[random.RandiRange(0, roads.Count - 1)];
			var villager = new Villager(lobby.Grid, roads, gathering, home, roles[i % roles.Length], clothes[i % clothes.Length], random.Randi());
			villagers.Add(villager);
			AddChild(villager);
		}
	}

	void AddClouds(RandomNumberGenerator random)
	{
		for (var i = 0; i < 9; i++)
		{
			var cloud = new Node3D();
			var material = new StandardMaterial3D { AlbedoColor = Colors.White, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
			cloudMaterials.Add(material);
			for (var puff = 0; puff < 4; puff++)
			{
				var size = random.RandfRange(1.6f, 2.8f);
				cloud.AddChild(new MeshInstance3D
				{
					Mesh = new SphereMesh { Radius = 1f, Height = 2f, RadialSegments = 10, Rings = 5 },
					MaterialOverride = material,
					Scale = new Vector3(size, size * 0.45f, size * 0.8f),
					Position = new Vector3(puff * 1.7f - 2.5f, random.RandfRange(-0.2f, 0.3f), random.RandfRange(-0.4f, 0.4f)),
					CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
				});
			}
			var angle = random.Randf() * Mathf.Tau;
			var far = random.RandfRange(26f, 44f);
			cloud.Position = new Vector3(Mathf.Cos(angle) * far, i % 3 == 0 ? random.RandfRange(7f, 10f) : random.RandfRange(-6f, -3f), Mathf.Sin(angle) * far);
			AddChild(cloud);
			clouds.Add(cloud);
		}
	}

	void AddBirds(RandomNumberGenerator random)
	{
		var body = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#F4EFE4"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
		for (var i = 0; i < 4; i++)
		{
			var bird = new Node3D { Name = "Bird" };
			foreach (var side in new[] { -1f, 1f })
				bird.AddChild(new MeshInstance3D { Name = side < 0 ? "WingL" : "WingR", Mesh = new BoxMesh { Size = new Vector3(0.5f, 0.03f, 0.18f) }, MaterialOverride = body, Position = new Vector3(side * 0.25f, 0f, 0f) });
			bird.SetMeta("phase", random.Randf() * Mathf.Tau);
			bird.SetMeta("radius", random.RandfRange(9f, 17f));
			bird.SetMeta("height", random.RandfRange(6f, 9f));
			AddChild(bird);
			birds.Add(bird);
		}
	}

	void AddFireflies(RandomNumberGenerator random)
	{
		var glow = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#D8FF7A"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, EmissionEnabled = true, Emission = Color.FromHtml("#C8FF50"), EmissionEnergyMultiplier = 2.5f };
		for (var i = 0; i < 18; i++)
		{
			var home = new Vector3(random.RandfRange(-9f, 9f), random.RandfRange(0.5f, 2.2f), random.RandfRange(-9f, 9f));
			var mote = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f, RadialSegments = 6, Rings = 3 }, MaterialOverride = glow, Position = home, Visible = false, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
			AddChild(mote);
			fireflies.Add((mote, home, random.Randf() * Mathf.Tau));
		}
	}

	// Faíscas que giram no alto de cada torre e uma luz que pulsa; a cor de cada uma segue a do Papel.
	void AddTowerSparks()
	{
		foreach (var place in lobby.Rules.Places.Where(p => p.Id is "matematica" or "portugues"))
		{
			var color = Color.FromHtml(place.Id == "matematica" ? "#FFD27A" : "#8CD2FF");
			var material = new StandardMaterial3D { AlbedoColor = color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, EmissionEnabled = true, Emission = color, EmissionEnergyMultiplier = 2f };
			var top = new Vector3(place.X, 4.6f, place.Z);
			for (var i = 0; i < 6; i++)
			{
				var spark = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.07f, Height = 0.14f, RadialSegments = 6, Rings = 3 }, MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
				AddChild(spark);
				sparks.Add((spark, top, 1.25f + 0.12f * (i % 3), i * Mathf.Tau / 6f));
			}
			var light = new OmniLight3D { Position = top, LightColor = color, OmniRange = 5f, LightEnergy = 0.4f };
			AddChild(light);
			towerLights.Add((light, place.X));
			AddAdornment(place.Id == "matematica" ? "torre-engrenagem" : "torre-livro", new Vector3(place.X, 6.3f, place.Z));
		}
		if (lobby.Rules.Objects.Any(o => o.Id == "mestre"))
		{
			var (ground, top) = lobby.FootprintOf("mestre");
			var glow = new OmniLight3D { Position = ground + new Vector3(0f, top * 0.45f, 0f), LightColor = Color.FromHtml("#B58CFF"), OmniRange = 4.5f, LightEnergy = 0.6f };
			AddChild(glow);
			towerLights.Add((glow, ground.X));
		}
	}

	// Uma peça que gira devagar e flutua sobre a torre, no mesmo eixo vertical.
	void AddAdornment(string model, Vector3 at)
	{
		if (Lobby.LoadModel(model) is not { } adornment) return;
		var holder = new Node3D { Position = at };
		holder.AddChild(adornment);
		adornment.Scale = Vector3.One * 0.9f;
		AddChild(holder);
		floaters.Add((holder, at.Y, at.X));
	}

	void AddSmoke()
	{
		foreach (var origin in lobby.Chimneys)
			for (var i = 0; i < 4; i++)
			{
				var material = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.9f, 0.92f, 0.5f), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
				var puff = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.13f, Height = 0.26f, RadialSegments = 8, Rings = 4 }, MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
				AddChild(puff);
				smoke.Add((puff, material, origin, i / 4f));
			}
	}

	public override void _Process(double delta)
	{
		var reduced = lobby.ReduceMotion;
		if (!reduced) clock += delta;
		var night = lobby.Light < NightBelow;
		foreach (var villager in villagers) villager.Tick((float)delta, night, reduced);
		TickClouds((float)delta, reduced);
		TickBirds(reduced);
		TickFireflies(night, reduced);
		TickSparks(night);
		TickSmoke(reduced);
		TickSpinners((float)delta, reduced);
	}

	// Cada tufo sobe, cresce e some; com a redução de animações fica parado a meio caminho.
	void TickSmoke(bool reduced)
	{
		var tint = new Color(0.55f, 0.58f, 0.7f).Lerp(Colors.White, lobby.Light);
		foreach (var (puff, material, origin, phase) in smoke)
		{
			var t = reduced ? 0.3f + phase * 0.5f : Mathf.PosMod((float)clock * 0.22f + phase, 1f);
			puff.Position = origin + new Vector3(0.3f * t, 1.2f * t, 0f);
			puff.Scale = Vector3.One * (0.6f + 1.4f * t);
			material.AlbedoColor = new Color(tint, 0.55f * (1f - t));
		}
	}

	void TickSpinners(float delta, bool reduced)
	{
		if (reduced) return;
		foreach (var (node, axis, speed) in lobby.Spinners) node.Rotate(axis, speed * delta);
		foreach (var (node, baseY, phase) in floaters)
		{
			node.Rotate(Vector3.Up, 0.6f * delta);
			node.Position = new Vector3(node.Position.X, baseY + 0.25f * Mathf.Sin((float)clock * 1.3f + phase), node.Position.Z);
		}
	}

	void TickClouds(float delta, bool reduced)
	{
		var tint = new Color(0.55f, 0.6f, 0.8f).Lerp(Colors.White, lobby.Light);
		foreach (var material in cloudMaterials) material.AlbedoColor = new Color(tint, 0.92f);
		if (reduced) return;
		foreach (var cloud in clouds)
		{
			cloud.Position += new Vector3(0.45f, 0f, -0.15f) * delta;
			if (cloud.Position.X > 48f) cloud.Position = new Vector3(-48f, cloud.Position.Y, cloud.Position.Z);
		}
	}

	void TickBirds(bool reduced)
	{
		foreach (var bird in birds)
		{
			bird.Visible = lobby.Light > 0.35f;
			var phase = (float)bird.GetMeta("phase") + (reduced ? 0f : (float)clock * 0.35f);
			var radius = (float)bird.GetMeta("radius");
			bird.Position = new Vector3(Mathf.Cos(phase) * radius, (float)bird.GetMeta("height") + Mathf.Sin(phase * 2f) * 0.4f, Mathf.Sin(phase) * radius);
			bird.Rotation = new Vector3(0f, -phase, 0f);
			var flap = reduced ? 0f : Mathf.Sin((float)clock * 9f + phase * 3f) * 0.6f;
			bird.GetNode<MeshInstance3D>("WingL").Rotation = new Vector3(0f, 0f, -flap);
			bird.GetNode<MeshInstance3D>("WingR").Rotation = new Vector3(0f, 0f, flap);
		}
	}

	void TickFireflies(bool night, bool reduced)
	{
		foreach (var (mote, home, phase) in fireflies)
		{
			mote.Visible = night;
			if (reduced) continue;
			var t = (float)clock;
			mote.Position = home + new Vector3(Mathf.Sin(t * 0.7f + phase) * 0.9f, Mathf.Sin(t * 1.3f + phase * 2f) * 0.35f, Mathf.Cos(t * 0.6f + phase) * 0.9f);
		}
	}

	void TickSparks(bool night)
	{
		var t = (float)clock;
		foreach (var (mote, center, radius, phase) in sparks)
			mote.Position = center + new Vector3(Mathf.Cos(t * 0.9f + phase) * radius, Mathf.Sin(t * 1.7f + phase * 2f) * 0.5f, Mathf.Sin(t * 0.9f + phase) * radius);
		foreach (var (light, phase) in towerLights)
			light.LightEnergy = (night ? 1.1f : 0.35f) + 0.25f * Mathf.Sin(t * 2f + phase);
	}

	public Godot.Collections.Dictionary Diagnostics() => new()
	{
		["moradores"] = villagers.Count,
		["moradores_andando"] = villagers.Count(v => v.Walking),
		["nuvens"] = clouds.Count,
		["passaros"] = birds.Count,
		["vagalumes_visiveis"] = fireflies.Count(f => f.Mote.Visible),
	};
}
