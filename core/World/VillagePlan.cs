namespace ArquipelagoDoCeu.World;

public enum VillagePiece { Moinho, CasaA, CasaB, Poco, Canteiro }

public sealed record VillageSpot(VillagePiece Piece, HexCell Cell);

// Onde ficam as casas, o moinho, o poço e os canteiros da ilha principal: na grama, perto dos caminhos e longe dos objetos de interação,
// sempre os mesmos lugares para os mesmos dados (a ordem vem de um embaralhamento fixo, sem sorteio).
public static class VillagePlan
{
	static readonly VillagePiece[] Order =
		[VillagePiece.Moinho, VillagePiece.CasaA, VillagePiece.CasaB, VillagePiece.Poco, VillagePiece.CasaA, VillagePiece.Canteiro, VillagePiece.CasaB, VillagePiece.Canteiro];

	// `land` é a grama da ilha (sem lagoa) e `path` os caminhos. `keepClear` (árvores, lampiões, pontes) pede um hexágono de folga e `wideClear`
	// (objetos de interação, de modelo grande) pede dois.
	public static IReadOnlyList<VillageSpot> Build(IEnumerable<HexCell> land, IReadOnlySet<HexCell> path, IEnumerable<HexCell> keepClear, IEnumerable<HexCell> wideClear,
		int innerRing = 3, int outerRing = 7, int spacing = 3)
	{
		var near = keepClear.ToList();
		var wide = wideClear.ToList();
		var origin = new HexCell(0, 0);
		var candidates = land
			.Where(c => !path.Contains(c) && near.All(k => k.DistanceTo(c) > 1) && wide.All(k => k.DistanceTo(c) > 2) && c.DistanceTo(origin) >= innerRing && c.DistanceTo(origin) <= outerRing)
			.Where(c => c.Neighbors().Any(path.Contains))
			.OrderBy(Shuffle)
			.ThenBy(c => c.Q)
			.ThenBy(c => c.R)
			.ToList();

		var spots = new List<VillageSpot>();
		foreach (var piece in Order)
		{
			var cell = candidates.Cast<HexCell?>().FirstOrDefault(c => spots.All(s => s.Cell.DistanceTo(c!.Value) >= spacing));
			if (cell is { } chosen) spots.Add(new VillageSpot(piece, chosen));
		}
		return spots;
	}

	static int Shuffle(HexCell cell) => unchecked((cell.Q * 73856093) ^ (cell.R * 19349663)) & 0x7fffffff;
}
