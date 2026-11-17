using ArquipelagoDoCeu.World;

namespace ArquipelagoDoCeu.Generation;

// Onde fica a mobília de uma Sala: pilares no meio dela e peças (baú, estante, barril) encostadas na parede.
// A escolha é por seed e nunca fecha um caminho: cada hexágono ocupado sai da rede de passagem e todo o resto continua alcançável do centro.
public enum FurnishingSlot { Pillar, Wall }

public sealed record Furnishing(HexCell Cell, FurnishingSlot Slot);

public static class RoomFurnishing
{
	// Uma Sala só ganha pilares a partir deste tamanho, e as peças de parede e os pilares seguem a proporção em hexágonos por peça.
	const int MinCellsForPillars = 30;
	const int CellsPerPillar = 16;
	const int CellsPerWallPiece = 8;
	const int ClearRadiusAroundCenter = 2;
	const int PillarSpacing = 3;
	const int WallSpacing = 2;

	// `room` são os hexágonos da Sala; `network` tudo o que é pisável em volta (a Sala, os corredores e as vizinhas);
	// `keepFree` são os que nunca ficam ocupados (portas, escada lateral).
	public static IReadOnlyList<Furnishing> Plan(IReadOnlySet<HexCell> room, IReadOnlySet<HexCell> network, IReadOnlySet<HexCell> keepFree, HexCell center, ulong seed)
	{
		var rng = new Pcg32(seed, 77);
		var taken = new HashSet<HexCell>();
		var chosen = new List<Furnishing>();

		bool Open(HexCell c) => room.Contains(c) && !keepFree.Contains(c) && c.DistanceTo(center) > ClearRadiusAroundCenter && !taken.Contains(c);
		bool Spaced(HexCell c, int spacing) => taken.All(t => t.DistanceTo(c) >= spacing);

		void Fill(FurnishingSlot slot, IEnumerable<HexCell> candidates, int limit, int spacing)
		{
			var pool = candidates.OrderBy(c => c.Q).ThenBy(c => c.R).ToList();
			rng.Shuffle(pool);
			foreach (var cell in pool)
			{
				if (chosen.Count(f => f.Slot == slot) >= limit) return;
				if (!Open(cell) || !Spaced(cell, spacing)) continue;
				taken.Add(cell);
				if (!StaysConnected(network, room, taken, center)) { taken.Remove(cell); continue; }
				chosen.Add(new Furnishing(cell, slot));
			}
		}

		if (room.Count >= MinCellsForPillars)
			Fill(FurnishingSlot.Pillar, room.Where(c => c.Neighbors().All(room.Contains)), room.Count / CellsPerPillar, PillarSpacing);
		Fill(FurnishingSlot.Wall, room.Where(c => c.Neighbors().Any(n => !network.Contains(n))), room.Count / CellsPerWallPiece, WallSpacing);
		return chosen;
	}

	// Todo hexágono da Sala que ficou livre ainda se alcança do centro, andando só pela rede.
	static bool StaysConnected(IReadOnlySet<HexCell> network, IReadOnlySet<HexCell> room, HashSet<HexCell> taken, HexCell center)
	{
		var seen = new HashSet<HexCell> { center };
		var queue = new Queue<HexCell>([center]);
		while (queue.Count > 0)
			foreach (var next in queue.Dequeue().Neighbors().Where(n => network.Contains(n) && !taken.Contains(n)))
				if (seen.Add(next)) queue.Enqueue(next);
		return room.All(c => taken.Contains(c) || seen.Contains(c)) && network.Where(c => !taken.Contains(c)).All(seen.Contains);
	}
}
