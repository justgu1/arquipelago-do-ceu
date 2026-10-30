using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArquipelagoDoCeu;

// Árvores, torres e prédios ficam translúcidos quando escondem o Personagem da câmera, como as paredes da Dungeon (RoomWalls.Fade).
// Cada instância ganha cópias dos próprios materiais; só vira translúcida enquanto some, e volta ao material original quando inteira.
public partial class LobbyOcclusion : Node
{
	const float HiddenAlpha = 0.2f;
	const float Speed = 8f;
	// Altura do peito do Personagem: o raio parte dali, para um galho baixo ou a base da torre também contarem.
	const float ChestHeight = 0.8f;

	readonly Lobby lobby;
	readonly IReadOnlyList<Node3D> models;
	readonly Func<bool> reduceMotion;
	readonly List<Fadeable> fadeables = new();
	Player? player;

	sealed class Fadeable
	{
		public required Aabb Box;
		public required List<(StandardMaterial3D Material, BaseMaterial3D.TransparencyEnum Original, Color Color)> Materials;
		public float Alpha = 1f;
	}

	public LobbyOcclusion(Lobby lobby, IReadOnlyList<Node3D> models, Func<bool> reduceMotion)
	{
		this.lobby = lobby;
		this.models = models;
		this.reduceMotion = reduceMotion;
		Name = "LobbyOcclusion";
	}

	public int FadedNow => fadeables.Count(f => f.Alpha < 0.99f);

	public override void _Ready()
	{
		foreach (var model in models) fadeables.Add(Prepare(model));
	}

	static Fadeable Prepare(Node3D model)
	{
		var materials = new List<(StandardMaterial3D, BaseMaterial3D.TransparencyEnum, Color)>();
		foreach (var mesh in model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
		{
			if (mesh.MaterialOverride is StandardMaterial3D shared)
			{
				var copy = (StandardMaterial3D)shared.Duplicate();
				mesh.MaterialOverride = copy;
				materials.Add((copy, copy.Transparency, copy.AlbedoColor));
				continue;
			}
			for (var s = 0; s < mesh.GetSurfaceOverrideMaterialCount(); s++)
			{
				if (mesh.GetActiveMaterial(s) is not StandardMaterial3D source) continue;
				var copy = (StandardMaterial3D)source.Duplicate();
				mesh.SetSurfaceOverrideMaterial(s, copy);
				materials.Add((copy, copy.Transparency, copy.AlbedoColor));
			}
		}
		return new Fadeable { Box = Lobby.WorldBox(model), Materials = materials };
	}

	public override void _Process(double delta)
	{
		player ??= GetTree().CurrentScene?.GetNodeOrNull<Player>("Player");
		if (player is not { Visible: true } || lobby.Camera is not { } camera) return;
		// A câmera é ortogonal: todos os raios seguem a mesma direção, e o que está no caminho entre o peito e a câmera esconde o Personagem.
		var from = player.GlobalPosition + Vector3.Up * ChestHeight;
		var toward = camera.GlobalTransform.Basis.Z;
		var step = reduceMotion() ? 1f : (float)delta * Speed;
		foreach (var item in fadeables)
		{
			var hides = item.Box.IntersectsSegment(from, from + toward * 80f);
			item.Alpha = Mathf.MoveToward(item.Alpha, hides ? HiddenAlpha : 1f, step);
			Apply(item);
		}
	}

	static void Apply(Fadeable item)
	{
		var whole = item.Alpha >= 0.999f;
		foreach (var (material, original, color) in item.Materials)
		{
			material.Transparency = whole ? original : BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
			material.AlbedoColor = new Color(color, color.A * item.Alpha);
		}
	}
}
