using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// Os corredores da Dungeon desenhados como vãos de pedra: cada trecho reto vira uma laje contínua com uma parede reta de cada lado,
// e não uma fila de hexágonos com parede serrilhada. A passagem (a grade) segue sendo a dos hexágonos.
public partial class DungeonWorld
{
	const float HalfWidth = 1.05f;
	const float SlabThickness = 0.5f;
	const float WallPiece = 1f;

	void AddCorridor(IReadOnlyList<HexCell> cells, List<RoomWalls.Piece> pieces)
	{
		var points = cells.Select(c => GroundOf(c)).ToList();
		var runs = Runs(points);
		foreach (var (from, to) in runs)
		{
			AddSlab(points[from], points[to]);
			AddSide(pieces, points[from], points[to], 1f);
			AddSide(pieces, points[from], points[to], -1f);
		}
		foreach (var end in new[] { runs[0].From, runs[^1].To }.Distinct())
			AddJoint(pieces, points[end], points[end == runs[0].From ? runs[0].To : runs[^1].From]);
		foreach (var turn in runs.Skip(1).Select(r => r.From))
			PlacePillar(pieces, points[turn], 1f);
	}

	// Cada trecho reto de `From` até `To` (índices): uma curva abre outro trecho.
	static List<(int From, int To)> Runs(List<Vector3> points)
	{
		var runs = new List<(int, int)>();
		var start = 0;
		for (var i = 2; i < points.Count; i++)
			if ((points[i] - points[i - 1]).Normalized().DistanceTo((points[i - 1] - points[i - 2]).Normalized()) > 0.01f)
			{
				runs.Add((start, i - 1));
				start = i - 1;
			}
		runs.Add((start, points.Count - 1));
		return runs;
	}

	void AddSlab(Vector3 a, Vector3 b)
	{
		var length = a.DistanceTo(b) + 2f * HalfWidth;
		var mid = (a + b) / 2f;
		var along = b - a;
		var slab = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = new Vector3(2f * HalfWidth, SlabThickness, Mathf.Max(length, 2f * HalfWidth)) },
			MaterialOverride = Theme.StoneMaterial(),
			Position = mid + new Vector3(0f, -SlabThickness / 2f - 0.001f, 0f),
			Rotation = new Vector3(0f, Mathf.Atan2(along.X, along.Z), 0f),
		};
		room.AddChild(slab);
	}

	// Uma parede reta ao lado do trecho, em peças de um hexágono de comprimento; a de trás é alta e a da frente é baixa.
	void AddSide(List<RoomWalls.Piece> pieces, Vector3 a, Vector3 b, float side)
	{
		var along = (b - a).Normalized();
		var normal = new Vector3(-along.Z, 0f, along.X) * side;
		var front = (normal.X + normal.Z) / 1.4142f > 0.35f;
		var count = Mathf.Max(1, Mathf.RoundToInt(a.DistanceTo(b)));
		var piece = a.DistanceTo(b) / count;
		var yaw = Mathf.Atan2(along.X, along.Z) - Mathf.Pi / 2f;
		for (var i = 0; i < count; i++)
		{
			var at = a + along * (piece * (i + 0.5f)) + normal * HalfWidth;
			if (RoomWalls.Place(room, pieces, Theme, front ? "cenario/parede-baixa" : "cenario/parede-alta", front ? 0.75f : 2f, at, yaw) is { } wall)
				wall.Root.Scale = new Vector3(wall.Root.Scale.X * piece / WallPiece, wall.Root.Scale.Y, wall.Root.Scale.Z);
		}
		PlacePillar(pieces, a + normal * HalfWidth, front ? 1f : 2.2f);
		PlacePillar(pieces, b + normal * HalfWidth, front ? 1f : 2.2f);
	}

	void PlacePillar(List<RoomWalls.Piece> pieces, Vector3 at, float height) =>
		RoomWalls.Place(room, pieces, Theme, "cenario/coluna-dungeon", height, at, 0f);

	// A ponta do corredor na porta: um pilar de cada lado fecha a quina entre a parede da Sala e a do corredor.
	void AddJoint(List<RoomWalls.Piece> pieces, Vector3 end, Vector3 other)
	{
		var along = (other - end).Normalized();
		var normal = new Vector3(-along.Z, 0f, along.X);
		PlacePillar(pieces, end + normal * HalfWidth, 2.2f);
		PlacePillar(pieces, end - normal * HalfWidth, 2.2f);
	}
}
