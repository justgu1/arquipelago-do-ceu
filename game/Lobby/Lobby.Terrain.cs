using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

public partial class Lobby
{
	// Uma malha só para todos os tiles. O shader desenha grama, flores, água e os caminhos,
	// e cada tile sabe quais vizinhos também são caminho, para os braços se encontrarem na aresta.
	void AddTerrain()
	{
		var st = new SurfaceTool();
		st.Begin(Mesh.PrimitiveType.Triangles);
		foreach (var (cell, kind) in tiles)
		{
			var center = HexToWorld(cell.X, cell.Y);
			if (kind == Kind.Plank) continue;
			if (kind is Kind.Plaza or Kind.Path) { AddPlazaTile(center, ToneOf(cell), kind == Kind.Path, (int)(Hash01(cell.X * 37 + cell.Y * 91) * 3f) % 3); continue; }
			if (kind != Kind.Water)
			{
				AddLote02Tile(center, ToneOf(cell));
				AddSpill(center, cell, kind);
				continue;
			}
			center.Y = -.24f;
			var info = new Color((int)kind / 3f, PathMask(cell) / 63f, Hash01(cell.X * 7 + cell.Y * 13), 1f);
			var corners = new Vector3[6];
			for (var i = 0; i < 6; i++)
			{
				var angle = Mathf.DegToRad(60f * i);
				corners[i] = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * TileSize * TileGap;
			}
			for (var i = 0; i < 6; i++)
			{
				var a = corners[i];
				var b = corners[(i + 1) % 6];
				Vertex(st, center, Vector3.Up, info, Vector2.Zero);
				Vertex(st, a, Vector3.Up, info, new Vector2(a.X - center.X, a.Z - center.Z) / (TileSize * TileGap));
				Vertex(st, b, Vector3.Up, info, new Vector2(b.X - center.X, b.Z - center.Z) / (TileSize * TileGap));

				var side = new Color(info.R, info.G, info.B, 0f);
				var outward = ((a + b) / 2f - center).Normalized();
				var down = new Vector3(0f, -TileThickness, 0f);
				Vertex(st, a, outward, side, new Vector2(0f, 0f));
				Vertex(st, a + down, outward, side, new Vector2(0f, 1f));
				Vertex(st, b, outward, side, new Vector2(0f, 0f));
				Vertex(st, b, outward, side, new Vector2(0f, 0f));
				Vertex(st, a + down, outward, side, new Vector2(0f, 1f));
				Vertex(st, b + down, outward, side, new Vector2(0f, 1f));
			}
		}
		var shader = GD.Load<Shader>("res://Lobby/terrain.gdshader");
		AddChild(new MeshInstance3D { Mesh = st.Commit(), MaterialOverride = new ShaderMaterial { Shader = shader } });
	}

	// A laje de pedra clara da praça, a mesma do piso das Dungeons, mas quente como o resto do Lobby.
	void AddPlazaTile(Vector3 center, int tone, bool road, int variation = 0)
	{
		if (LoadModel(road ? $"lobby/tijolo-hex-{"abc"[variation]}" : "lobby/pedra-hex") is not { } tile)
		{
			AddLote02Tile(center, tone);
			return;
		}
		AddChild(tile);
		lote02Roots.Add(tile);
		tile.Position = center;
		foreach (var mesh in tile.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
			for (var s = 0; s < mesh.Mesh.GetSurfaceCount(); s++)
				if (mesh.Mesh.SurfaceGetMaterial(s) is StandardMaterial3D source)
					mesh.SetSurfaceOverrideMaterial(s, PlazaMaterial(source.ResourceName, tone, road));
	}

	static readonly System.Collections.Generic.Dictionary<string, Material> PlazaMaterials = [];

	static Material PlazaMaterial(string name, int tone, bool road)
	{
		var key = $"{name}{tone}{road}";
		if (PlazaMaterials.TryGetValue(key, out var cached) && IsInstanceValid(cached)) return cached;
		var stone = name switch
		{
			"argamassa" => "#8F7F63",
			"tijolo_a" => "#BCA173",
			"tijolo_b" => "#A98E62",
			_ => road ? "#B39A6C" : "#CBB991",
		};
		var tint = Color.FromHtml(stone) * Colors.White.Lerp(GrassTones[tone], 0.35f);
		var material = new StandardMaterial3D { AlbedoColor = new Color(tint.R, tint.G, tint.B), Roughness = 0.9f };
		PlazaMaterials[key] = material;
		return material;
	}

	// O caminho vaza para a grama: nas lajes de grama encostadas nele, uns tijolos soltos na beirada, em volta do jeito e no ângulo de cada laje.
	void AddSpill(Vector3 center, Vector2I cell, Kind kind)
	{
		if (kind is not (Kind.Grass or Kind.Flowers)) return;
		var toward = Neighbors.Where(n => tiles.TryGetValue(cell + n, out var other) && other == Kind.Path).Select(n => (Vector2I?)n).FirstOrDefault();
		if (toward is not { } direction || Hash01(cell.X * 53 + cell.Y * 29) > 0.8f) return;
		var edge = HexToWorld(cell.X + direction.X, cell.Y + direction.Y) - center;
		if (LoadModel("lobby/tijolos-soltos") is not { } spill) return;
		AddChild(spill);
		lote02Roots.Add(spill);
		spill.Position = center + edge.Normalized() * 0.62f;
		spill.Rotation = new Vector3(0f, Mathf.Tau * Hash01(cell.X * 17 + cell.Y * 61), 0f);
		foreach (var mesh in spill.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
			for (var s = 0; s < mesh.Mesh.GetSurfaceCount(); s++)
				if (mesh.Mesh.SurfaceGetMaterial(s) is StandardMaterial3D source)
					mesh.SetSurfaceOverrideMaterial(s, PlazaMaterial(source.ResourceName, ToneOf(cell), true));
	}

	static void Vertex(SurfaceTool st, Vector3 position, Vector3 normal, Color color, Vector2 uv)
	{
		st.SetNormal(normal);
		st.SetColor(color);
		st.SetUV(uv);
		st.AddVertex(position);
	}

	int PathMask(Vector2I cell)
	{
		if (tiles[cell] != Kind.Path) return 0;
		var mask = 0;
		for (var i = 0; i < Neighbors.Length; i++)
			if (tiles.TryGetValue(cell + Neighbors[i], out var other) && other == Kind.Path) mask |= 1 << i;
		return mask;
	}
}
