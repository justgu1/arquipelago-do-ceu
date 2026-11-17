using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ArquipelagoDoCeu;

// Árvores, torres e prédios ficam translúcidos quando escondem o Personagem da câmera, como as paredes da Dungeon (RoomWalls.Fade).
// O esmaecer usa a transparência pontilhada da própria malha, sem trocar os materiais, e volta ao normal quando inteira.
public partial class LobbyOcclusion : Node
{
	// Só esmaece um pouco, devagar: o que esconde a criança continua à vista, e não some como uma mancha.
	const float HiddenAlpha = 0.55f;
	const float Speed = 2.5f;
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
		public required List<MeshInstance3D> Meshes;
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

	static Fadeable Prepare(Node3D model) =>
		new() { Box = Lobby.WorldBox(model), Meshes = [.. model.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>()] };

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
		foreach (var mesh in item.Meshes) mesh.Transparency = 1f - item.Alpha;
	}
}
