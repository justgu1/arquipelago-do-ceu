using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// As paredes baixas e as colunas na borda das Salas, do kit de Dungeon feito no Blender, para a Dungeon parecer uma Dungeon.
// Baixas de propósito: a câmera isométrica vê a criança por cima delas. Uma coluna em cada sete é um braseiro aceso.
public static class RoomWalls
{
	const float WallHeight = 0.75f;
	const float PillarHeight = 1.5f;
	const float BrazierHeight = 1.85f;
	const float Edge = 1f;
	const float Apothem = 0.866f;
	const float HideRadius = 1.6f;

	// Uma peça do cenário que some quando esconde alguém. A transparência é do próprio modelo (dithering), então os materiais do tema não mudam.
	public sealed class Piece(Node3D root)
	{
		public Node3D Root { get; } = root;
		public IReadOnlyList<MeshInstance3D> Meshes { get; } = [.. root.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>()];
		public float Alpha { get; set; } = 1f;
	}

	// Uma parede em cada lado de hexágono do chão que dá para o vazio; os corredores e as portas ficam abertos.
	public static IReadOnlyList<Piece> Add(Node3D parent, IReadOnlyDictionary<HexCell, float> floor, IReadOnlySet<HexCell> open, DungeonTheme theme)
	{
		var pieces = new List<Piece>();
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
				Place(parent, pieces, theme, "cenario/parede-baixa", WallHeight, mid, angle);
				foreach (var end in new[] { mid + along * (Edge / 2f), mid - along * (Edge / 2f) })
				{
					var key = (Mathf.RoundToInt(end.X * 20f), Mathf.RoundToInt(end.Z * 20f));
					if (!pillars.Add(key)) continue;
					var lit = Mathf.PosMod(key.Item1 / 7 + key.Item2 / 5, 7) == 0;
					Place(parent, pieces, theme, lit ? "cenario/coluna-braseiro" : "cenario/coluna-dungeon", lit ? BrazierHeight : PillarHeight, end, 0f);
				}
			}
		}
		return pieces;
	}

	static void Place(Node3D parent, List<Piece> pieces, DungeonTheme theme, string model, float height, Vector3 at, float yaw)
	{
		if (RoomProps.Add(parent, model, height, at, yaw) is not { } node) return;
		theme.Tint(node);
		pieces.Add(new Piece(node));
	}

	// Só as peças que escondem alguém (na linha de visão da câmera, entre ela e a criança ou a criatura) somem; as outras voltam.
	public static void Fade(IReadOnlyList<Piece> pieces, Vector3 forward, IReadOnlyList<Vector3> seen, float delta)
	{
		foreach (var piece in pieces)
		{
			if (!GodotObject.IsInstanceValid(piece.Root)) continue;
			var hides = seen.Any(target => Hides(piece.Root.Position, target, forward));
			var alpha = Mathf.MoveToward(piece.Alpha, hides ? 0.05f : 1f, delta * 8f);
			if (Mathf.IsEqualApprox(alpha, piece.Alpha)) continue;
			piece.Alpha = alpha;
			foreach (var mesh in piece.Meshes) mesh.Transparency = 1f - alpha;
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
