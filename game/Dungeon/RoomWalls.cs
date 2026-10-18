using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// As paredes baixas e os pilares na borda das Salas, para a Dungeon parecer uma Dungeon.
// Baixas de propósito: a câmera isométrica vê a criança por cima delas.
public static class RoomWalls
{
	const float WallHeight = 0.75f;
	const float PillarHeight = 1.5f;
	const float Edge = 1f;
	const float Apothem = 0.866f;
	const float HideRadius = 1.1f;

	// Uma parede em cada lado de hexágono do chão que dá para o vazio; os corredores e as portas ficam abertos.
	public static IReadOnlyList<MeshInstance3D> Add(Node3D parent, IReadOnlyDictionary<HexCell, float> floor, IReadOnlySet<HexCell> open)
	{
		var pieces = new List<MeshInstance3D>();
		var stone = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#7A7390"), Roughness = 0.95f };
		var pillars = new HashSet<(int, int)>();
		foreach (var (cell, height) in floor)
		{
			var center = HexGrid.ToWorld(cell);
			foreach (var next in cell.Neighbors().Where(n => !floor.ContainsKey(n) && !open.Contains(n)))
			{
				var way = System.Numerics.Vector2.Normalize(HexGrid.ToWorld(next) - center);
				var mid = new Vector3(center.X + way.X * Apothem, height, center.Y + way.Y * Apothem);
				var along = new Vector3(-way.Y, 0f, way.X);
				var angle = Mathf.Atan2(along.X, along.Z) - Mathf.Pi / 2f;
				pieces.Add(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(Edge, WallHeight, 0.2f) }, MaterialOverride = stone, Position = mid + new Vector3(0f, WallHeight / 2f, 0f), Rotation = new Vector3(0f, angle, 0f) });
				foreach (var end in new[] { mid + along * (Edge / 2f), mid - along * (Edge / 2f) })
					if (pillars.Add((Mathf.RoundToInt(end.X * 20f), Mathf.RoundToInt(end.Z * 20f))))
						pieces.Add(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.34f, PillarHeight, 0.34f) }, MaterialOverride = stone, Position = end + new Vector3(0f, PillarHeight / 2f, 0f) });
			}
		}
		foreach (var piece in pieces) parent.AddChild(piece);
		return pieces;
	}

	// Só as peças que escondem alguém (na linha de visão da câmera, entre ela e a criança ou a criatura) somem; as outras voltam.
	public static void Fade(IReadOnlyList<MeshInstance3D> pieces, Vector3 forward, IReadOnlyList<Vector3> seen, float delta)
	{
		foreach (var piece in pieces)
		{
			if (!GodotObject.IsInstanceValid(piece)) continue;
			var hides = seen.Any(target => Hides(piece.Position, target, forward));
			piece.Transparency = Mathf.MoveToward(piece.Transparency, hides ? 0.9f : 0f, delta * 8f);
		}
	}

	// A câmera é ortogonal: todos os raios seguem `forward`, então a peça esconde quem está atrás dela a menos de um raio de distância da linha.
	static bool Hides(Vector3 piece, Vector3 target, Vector3 forward)
	{
		var between = piece - target;
		var along = between.Dot(forward);
		return along < 0f && (between - forward * along).Length() < HideRadius;
	}
}
