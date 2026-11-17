using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// A mobília da Sala que o `RoomFurnishing` (no core) sorteou: pilares no meio e peças de cada torre encostadas na parede, viradas para o centro.
// Os Números têm oficina (bancada, engrenagens, caixotes); as Letras, biblioteca (estantes, mesa de leitura, livros). Baú e barril são de todas.
public static class RoomFurniture
{
	const float PillarHeight = 2.3f;
	const float TowardWall = 0.42f;

	// Modelo e altura de cada peça de parede.
	static readonly (string Model, float Height)[] Common = [("cenario/bau", 0.62f), ("cenario/barril", 0.75f)];
	static readonly (string Model, float Height)[] Workshop = [.. Common, ("cenario/bancada", 0.95f), ("obstaculos/pilha-de-engrenagens", 0.7f), ("obstaculos/caixote-madeira", 1f)];
	static readonly (string Model, float Height)[] Library = [.. Common, ("cenario/estante", 1.5f), ("cenario/mesa-leitura", 0.95f), ("obstaculos/pilha-de-livros", 0.95f), ("cenario/estante", 1.5f)];

	// `plan` já veio sem hexágonos que fechem caminho e `network` é tudo o que se pisa em volta (a parede é o que dá para fora dela); devolve as peças.
	public static IReadOnlyList<RoomWalls.Piece> Add(Node3D parent, IEnumerable<Furnishing> plan, IReadOnlySet<HexCell> network, IReadOnlyDictionary<HexCell, float> heights, DungeonTheme theme, bool library)
	{
		var pieces = new List<RoomWalls.Piece>();
		var catalog = library ? Library : Workshop;
		foreach (var item in plan)
		{
			var world = HexGrid.ToWorld(item.Cell);
			var floor = heights.TryGetValue(item.Cell, out var h) ? h : 0f;
			if (item.Slot == FurnishingSlot.Pillar)
			{
				RoomWalls.Place(parent, pieces, theme, "cenario/coluna-dungeon", PillarHeight, new Vector3(world.X, floor, world.Y), 0f);
				continue;
			}
			var toWall = item.Cell.Neighbors().Where(n => !network.Contains(n)).Select(n => System.Numerics.Vector2.Normalize(HexGrid.ToWorld(n) - world)).FirstOrDefault();
			var (model, height) = catalog[Mathf.Abs(item.Cell.Q * 31 + item.Cell.R * 17) % catalog.Length];
			var at = new Vector3(world.X + toWall.X * TowardWall, floor, world.Y + toWall.Y * TowardWall);
			RoomWalls.Place(parent, pieces, theme, model, height, at, Mathf.Atan2(-toWall.X, -toWall.Y), themed: false);
		}
		return pieces;
	}

}
