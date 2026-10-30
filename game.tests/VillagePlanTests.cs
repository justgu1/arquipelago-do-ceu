using ArquipelagoDoCeu.Rules;
using ArquipelagoDoCeu.World;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

// O vilarejo do Lobby: peças na grama, perto de caminho, longe dos objetos e sempre nos mesmos lugares.
public class VillagePlanTests
{
	static readonly LobbyRules Rules =
		GameRules.Parse(File.ReadAllText(Path.Combine(MissionFiles.RepositoryRoot(), "game", "data", "rules.json"))).Value!.Lobby;

	static readonly LobbyMap Map = LobbyMap.Build(Rules).Value!;
	static readonly LobbyPlan Plan = LobbyPlan.Build(Rules, Map).Value!;

	static IReadOnlyList<VillageSpot> Build()
	{
		var land = Map.Main.Where(c => !Plan.Lake.Contains(c));
		var near = Plan.Trees.Concat(Rules.Islands.Select(i => new HexCell(i.FromQ, i.FromR)));
		return VillagePlan.Build(land, Plan.Path, near, Rules.Objects.Select(o => new HexCell(o.Q, o.R)));
	}

	[Fact]
	public void Os_dados_do_Lobby_ganham_o_vilarejo()
	{
		var spots = Build();

		Assert.True(spots.Count >= 5);
		Assert.Contains(spots, s => s.Piece == VillagePiece.Moinho);
		Assert.Contains(spots, s => s.Piece == VillagePiece.CasaA);
		Assert.Contains(spots, s => s.Piece == VillagePiece.CasaB);
	}

	[Fact]
	public void As_pecas_ficam_na_grama_longe_dos_objetos_e_das_arvores()
	{
		foreach (var spot in Build())
		{
			Assert.Contains(spot.Cell, Map.Main);
			Assert.DoesNotContain(spot.Cell, Plan.Lake);
			Assert.DoesNotContain(spot.Cell, Plan.Path);
			Assert.All(Rules.Objects, o => Assert.True(new HexCell(o.Q, o.R).DistanceTo(spot.Cell) >= 3));
			Assert.All(Plan.Trees, tree => Assert.True(tree.DistanceTo(spot.Cell) >= 2));
		}
	}

	[Fact]
	public void As_pecas_ficam_separadas_entre_si()
	{
		var spots = Build();

		foreach (var a in spots)
			foreach (var b in spots.Where(b => b != a))
				Assert.True(a.Cell.DistanceTo(b.Cell) >= 3);
	}

	[Fact]
	public void Os_mesmos_dados_dao_sempre_os_mesmos_lugares()
	{
		Assert.Equal(Build(), Build());
	}

	[Fact]
	public void Sem_espaco_a_ilha_fica_sem_pecas_em_vez_de_falhar()
	{
		var tiny = new[] { new HexCell(3, 0) };

		Assert.Empty(VillagePlan.Build(tiny, new HashSet<HexCell>(), [], [], 3, 7));
	}
}
