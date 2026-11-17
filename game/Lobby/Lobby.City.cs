using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// A cidade em volta da ilha central: as ilhas de bairro têm casas, moinho e poço; as ilhas das torres têm a praça de pedra e a mobília da própria torre
// (oficina nos Números, biblioteca nas Letras), a mesma das Dungeons. Os lugares saem dos dados e da forma das ilhas, sem sorteio.
public partial class Lobby
{
	const string DistrictPrefix = "bairro";
	const int MaxHousesPerDistrict = 7;

	sealed record Decor(string Model, float Height, HexCell Cell, float Body, bool Tall);

	static readonly HexCell[] Around = [new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)];

	IReadOnlyList<Decor>? cityDecor;

	IReadOnlyList<Decor> CityDecor => cityDecor ??= [.. Rules.Islands.SelectMany(DecorOf), .. Fountain()];

	IEnumerable<HexCell> PlazaCells => tiles.Where(t => t.Value == Kind.Plaza && map.Main.Contains(new HexCell(t.Key.X, t.Key.Y))).Select(t => new HexCell(t.Key.X, t.Key.Y));

	// A fonte no meio da praça, entre o Guia e o Álbum, no hexágono de praça livre mais perto do ponto médio.
	IEnumerable<Decor> Fountain()
	{
		var anchors = Rules.Objects.Where(o => o.Id is "guia" or "album").Select(o => new HexCell(o.Q, o.R)).ToList();
		if (anchors.Count < 2) yield break;
		var middle = HexGrid.FromWorld((HexGrid.ToWorld(anchors[0]) + HexGrid.ToWorld(anchors[1])) / 2f);
		var taken = Rules.Objects.Select(o => new HexCell(o.Q, o.R)).ToHashSet();
		var spot = PlazaCells.Where(c => !taken.Contains(c) && taken.All(t => t.DistanceTo(c) > 1)).OrderBy(c => c.DistanceTo(middle)).Cast<HexCell?>().FirstOrDefault();
		if (spot is { } cell) yield return new Decor("cenario/fonte-descanso", 1.5f, cell, 0.95f, false);
	}

	IEnumerable<Decor> DecorOf(ArquipelagoDoCeu.Rules.LobbyIsland island)
	{
		if (!map.Islands.TryGetValue(island.Id, out var ground)) return [];
		var center = new HexCell(island.Q, island.R);
		var bridge = map.Bridges.TryGetValue(island.Id, out var cells) ? cells : [];
		var free = Around.Select(d => center + new HexCell(d.Q * 2, d.R * 2)).Where(c => ground.Contains(c) && bridge.All(b => b.DistanceTo(c) > 2)).ToList();
		if (IsDistrict(island)) return District(island, ground, StreetOf(island));
		if (island.Id == "mercador") return Market(free);
		return island.Solid ? Tower(island.Id, free) : [];
	}

	static bool IsDistrict(ArquipelagoDoCeu.Rules.LobbyIsland island) => island.Id.StartsWith(DistrictPrefix, System.StringComparison.Ordinal);

	// A rua do bairro: da ponte até o centro, em linha reta. A cidade cresce em volta dela, com a praça do bairro no fim.
	IReadOnlyList<HexCell> StreetOf(ArquipelagoDoCeu.Rules.LobbyIsland island)
	{
		if (!map.Islands.TryGetValue(island.Id, out var ground) || !map.Bridges.TryGetValue(island.Id, out var bridge) || bridge.Count == 0) return [];
		return [.. LobbyMap.Line(bridge[^1], new HexCell(island.Q, island.R)).Where(ground.Contains)];
	}

	// Casas dos dois lados da rua, a um hexágono de folga dela (quintal), e no centro o que o bairro tem de público: moinho, poço ou canteiro.
	IEnumerable<Decor> District(ArquipelagoDoCeu.Rules.LobbyIsland island, IReadOnlySet<HexCell> ground, IReadOnlyList<HexCell> street)
	{
		var center = new HexCell(island.Q, island.R);
		var middle = (island.Id.Sum(c => c) % 3) switch { 0 => VillagePiece.Moinho, 1 => VillagePiece.Poco, _ => VillagePiece.Canteiro };
		var (name, height, body) = Pieces[middle];
		yield return new Decor(name, height, center, body, middle != VillagePiece.Canteiro);

		var square = center.Neighbors().Append(center).ToHashSet();
		var along = street.Select((cell, index) => (cell, index)).ToList();
		var lots = ground
			.Where(c => !square.Contains(c) && street.All(s => s.DistanceTo(c) >= 2) && along.Any(s => s.cell.DistanceTo(c) == 2))
			.OrderBy(c => along.Min(s => s.cell.DistanceTo(c) * 10 + s.index))
			.ThenBy(c => c.Q).ThenBy(c => c.R)
			.ToList();
		var houses = new List<HexCell>();
		foreach (var lot in lots.Where(l => houses.All(h => h.DistanceTo(l) >= 2)))
			houses.Add(lot);
		for (var k = 0; k < houses.Count && k < MaxHousesPerDistrict; k++)
		{
			var (model, h, b) = Pieces[(k + island.Id.Sum(c => c)) % 2 == 0 ? VillagePiece.CasaA : VillagePiece.CasaB];
			yield return new Decor(model, h, houses[k], b, true);
		}
	}

	// O estoque do Mercador em volta da barraca: barris, caixotes e um baú.
	static IEnumerable<Decor> Market(List<HexCell> free)
	{
		(string Model, float Height, float Body)[] props = [("cenario/barril", 0.75f, 0.35f), ("obstaculos/caixote-madeira", 1f, 0.5f), ("cenario/bau", 0.62f, 0.45f), ("cenario/barril", 0.75f, 0.35f)];
		for (var i = 0; i < props.Length && i < free.Count; i++)
			yield return new Decor(props[i].Model, props[i].Height, free[i], props[i].Body, false);
	}

	// A mobília da torre em volta da base, no estilo das Salas da Dungeon dela.
	static IEnumerable<Decor> Tower(string id, List<HexCell> free)
	{
		(string Model, float Height, float Body)[] props = id == "matematica"
			? [("cenario/coluna-braseiro", 1.85f, 0.35f), ("cenario/bancada", 0.95f, 0.6f), ("cenario/coluna-braseiro", 1.85f, 0.35f), ("obstaculos/pilha-de-engrenagens", 0.7f, 0.5f)]
			: [("cenario/coluna-dungeon", 1.5f, 0.3f), ("cenario/estante", 1.5f, 0.6f), ("cenario/coluna-dungeon", 1.5f, 0.3f), ("obstaculos/pilha-de-livros", 0.95f, 0.5f)];
		for (var i = 0; i < props.Length && i < free.Count; i++)
			yield return new Decor(props[i].Model, props[i].Height, free[i], props[i].Body, props[i].Height > 1f);
	}

	void AddCity()
	{
		foreach (var decor in CityDecor)
		{
			if (LoadModel(decor.Model) is not { } model) continue;
			AddChild(model);
			model.Rotation = new Vector3(0f, TowardCamera, 0f);
			FitToHeight(model, decor.Height, HexToWorld(decor.Cell.Q, decor.Cell.R));
			if (decor.Tall) occluders.Add(model);
			if (decor.Model == "vilarejo/casa-a") Chimneys.Add(model.ToGlobal(new Vector3(0.6f, 2.65f, -0.4f)));
			if (decor.Model == "vilarejo/casa-b") Chimneys.Add(model.ToGlobal(new Vector3(0.45f, 2.75f, -0.35f)));
			if (decor.Model == "vilarejo/moinho") AddSails(model);
		}
	}

	// O chão das ilhas das torres é de pedra, em volta do centro onde a torre está.
	void MarkTowerPaving()
	{
		foreach (var island in Rules.Islands.Where(IsDistrict))
		{
			var center = new HexCell(island.Q, island.R);
			foreach (var cell in StreetOf(island).Concat(center.Neighbors()).Append(center))
				if (map.Islands.TryGetValue(island.Id, out var ground) && ground.Contains(cell)) tiles[new Vector2I(cell.Q, cell.R)] = Kind.Path;
		}
		foreach (var island in Rules.Islands.Where(i => i.Solid && map.Islands.ContainsKey(i.Id)))
		{
			var center = new HexCell(island.Q, island.R);
			foreach (var cell in map.Islands[island.Id].Where(c => c.DistanceTo(center) <= 2))
				tiles[new Vector2I(cell.Q, cell.R)] = Kind.Plaza;
		}
	}
}
