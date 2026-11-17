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

	// Uma peça com que a criança pode interagir: só curiosidade, sem prêmio (moedas e Itens vêm de criaturas, Salas e Andares).
	public sealed record Prop(string Id, System.Numerics.Vector2 Ground, string Prompt, string Line, Node3D Model);

	static (string Prompt, string Line) Describe(string model) => model switch
	{
		"cenario/bau" => ("Abrir o baú", "Você abriu o baú: só poeira e teias. Mas ele já foi de alguém que explorava a torre!"),
		"cenario/barril" => ("Olhar o barril", "Um barril de madeira, vazio e leve. Dá até para ouvir o eco."),
		"cenario/bancada" => ("Mexer na bancada", "Engrenagens, chaves e parafusos. Alguém consertava máquinas aqui."),
		"cenario/estante" => ("Ler a estante", "Livros de capas coloridas. Cada um guarda uma história esperando para ser lida."),
		"cenario/mesa-leitura" => ("Ver a mesa de leitura", "Um livro aberto e uma vela quase no fim. Alguém estava lendo."),
		"obstaculos/pilha-de-livros" => ("Olhar os livros", "Uma pilha torta de livros. Cuidado para não derrubar!"),
		"obstaculos/pilha-de-engrenagens" => ("Olhar as engrenagens", "Engrenagens de latão de vários tamanhos. Algumas ainda giram."),
		_ => ("Olhar o caixote", "Um caixote de madeira com uma engrenagem desenhada na tampa."),
	};

	// Modelo e altura de cada peça de parede.
	static readonly (string Model, float Height)[] Common = [("cenario/bau", 0.62f), ("cenario/barril", 0.75f)];
	static readonly (string Model, float Height)[] Workshop = [.. Common, ("cenario/bancada", 0.95f), ("obstaculos/pilha-de-engrenagens", 0.7f), ("obstaculos/caixote-madeira", 1f)];
	static readonly (string Model, float Height)[] Library = [.. Common, ("cenario/estante", 1.5f), ("cenario/mesa-leitura", 0.95f), ("obstaculos/pilha-de-livros", 0.95f), ("cenario/estante", 1.5f)];

	// `plan` já veio sem hexágonos que fechem caminho e `network` é tudo o que se pisa em volta (a parede é o que dá para fora dela); devolve as peças.
	public static IReadOnlyList<RoomWalls.Piece> Add(Node3D parent, IEnumerable<Furnishing> plan, IReadOnlySet<HexCell> network, IReadOnlyDictionary<HexCell, float> heights, DungeonTheme theme, bool library, IList<Prop> props)
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
			if (RoomWalls.Place(parent, pieces, theme, model, height, at, Mathf.Atan2(-toWall.X, -toWall.Y), themed: false) is { } placed)
			{
				var (prompt, line) = Describe(model);
				props.Add(new Prop($"prop:{props.Count}", new System.Numerics.Vector2(world.X, world.Y), prompt, line, placed.Root));
			}
		}
		return pieces;
	}

}
