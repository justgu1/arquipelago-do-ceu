using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// O chão hexagonal de uma Sala da Dungeon: lajes de pedra do kit de Dungeon (duas variações) na cor do tema da torre, com a faixa de ouro tingida pela
// cor da função da Sala (a cor nunca é a única pista: a Sala tem nome e forma no mapa). Sem o kit, vale o chão de prismas num só mesh.
public static class RoomMeshes
{
	const float TileGap = 0.98f;
	const float TileThickness = 0.5f;

	// Cada hexágono vem com a altura do topo (o desnível sobe alguns).
	public static void AddFloor(Node3D parent, IEnumerable<(HexCell Cell, float Height)> tiles, Color baseColor, bool cleared, DungeonTheme? theme = null)
	{
		var list = tiles.ToList();
		if (theme is not null && AddKitFloor(parent, list, baseColor, cleared, theme)) return;
		tiles = list;
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		foreach (var (cell, height) in tiles)
		{
			var world = HexGrid.ToWorld(cell);
			var center = new Vector3(world.X, height, world.Y);
			var tint = (cell.Q + cell.R) % 2 == 0 ? 0.85f : 1f;
			var top = baseColor.Lerp(Colors.White, 0.35f) * tint;
			top = cleared ? top.Lerp(Color.FromHtml("#8BC34A"), 0.35f) : top;
			top.A = 1f;
			var corners = Enumerable.Range(0, 6).Select(i => center + new Vector3(Mathf.Cos(Mathf.DegToRad(60f * i)), 0f, Mathf.Sin(Mathf.DegToRad(60f * i))) * TileGap).ToArray();
			var down = new Vector3(0f, -TileThickness - height, 0f);
			var side = top.Darkened(0.4f);
			for (var i = 0; i < 6; i++)
			{
				var a = corners[i];
				var b = corners[(i + 1) % 6];
				Triangle(st, center, a, b, Vector3.Up, top);
				var outward = ((a + b) / 2f - center).Normalized();
				Triangle(st, a, a + down, b, outward, side);
				Triangle(st, b, a + down, b + down, outward, side);
			}
		}
		parent.AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f } });
	}

	static readonly string[] KitTiles = ["cenario/piso-dungeon-a", "cenario/piso-dungeon-b"];

	// A mesma Sala sempre sai com as mesmas lajes: a variação vem das coordenadas, sem sorteio.
	static int Variation(HexCell cell) => (unchecked(cell.Q * 73856093 ^ cell.R * 19349663) >> 4) & 1;

	// A laje do kit no lugar de cada prisma. Devolve falso se o modelo não carregou, e aí vale o chão de reserva.
	static bool AddKitFloor(Node3D parent, List<(HexCell Cell, float Height)> tiles, Color baseColor, bool cleared, DungeonTheme theme)
	{
		if (!ResourceLoader.Exists($"res://assets/modelos/{KitTiles[0]}.glb")) return false;
		var trim = cleared ? baseColor.Lerp(Color.FromHtml("#8BC34A"), 0.45f) : baseColor;
		foreach (var (cell, height) in tiles)
		{
			if (Lobby.LoadModel(KitTiles[Variation(cell)]) is not { } tile) return false;
			var world = HexGrid.ToWorld(cell);
			parent.AddChild(tile);
			tile.Position = new Vector3(world.X, height, world.Y);
			theme.Tint(tile, trim);
		}
		return true;
	}

	static void Triangle(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color color)
	{
		foreach (var point in new[] { a, b, c })
		{
			st.SetNormal(normal);
			st.SetColor(color);
			st.AddVertex(point);
		}
	}
}
