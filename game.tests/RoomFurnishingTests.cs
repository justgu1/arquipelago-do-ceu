using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.World;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class RoomFurnishingTests
{
	static readonly HexCell Center = new(0, 0);

	static HashSet<HexCell> Hall(int radius) => HexGrid.Island(radius, []).Cells.ToHashSet();

	static IReadOnlyList<Furnishing> Plan(HashSet<HexCell> room, ulong seed = 1, HashSet<HexCell>? keepFree = null, HashSet<HexCell>? network = null) =>
		RoomFurnishing.Plan(room, network ?? room, keepFree ?? [], Center, seed);

	[Fact]
	public void A_mesma_seed_poe_a_mobilia_nos_mesmos_lugares()
	{
		var a = Plan(Hall(4), 9);
		var b = Plan(Hall(4), 9);

		Assert.Equal(a, b);
		Assert.NotEmpty(a);
	}

	[Fact]
	public void Seeds_diferentes_arrumam_a_Sala_de_outro_jeito()
	{
		var layouts = Enumerable.Range(1, 12).Select(seed => string.Join(";", Plan(Hall(4), (ulong)seed).Select(f => f.Cell))).Distinct().Count();

		Assert.True(layouts > 4);
	}

	[Fact]
	public void Nunca_ocupa_o_centro_a_volta_dele_nem_o_que_deve_ficar_livre()
	{
		var door = new HexCell(4, 0);
		var plan = Plan(Hall(4), 3, [door, .. door.Neighbors()]);

		Assert.All(plan, f => Assert.True(f.Cell.DistanceTo(Center) > 2));
		Assert.DoesNotContain(plan, f => f.Cell == door || door.Neighbors().Contains(f.Cell));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void O_que_sobra_da_Sala_continua_alcancavel_do_centro(int seed)
	{
		var room = Hall(5);
		var plan = Plan(room, (ulong)seed);
		var taken = plan.Select(f => f.Cell).ToHashSet();
		var seen = new HashSet<HexCell> { Center };
		var queue = new Queue<HexCell>([Center]);
		while (queue.Count > 0)
			foreach (var next in queue.Dequeue().Neighbors().Where(n => room.Contains(n) && !taken.Contains(n)))
				if (seen.Add(next)) queue.Enqueue(next);

		Assert.Equal(room.Count - taken.Count, seen.Count);
	}

	[Fact]
	public void Pilares_ficam_no_meio_da_Sala_e_as_pecas_de_parede_na_borda()
	{
		var room = Hall(5);
		var plan = Plan(room, 2);

		Assert.All(plan.Where(f => f.Slot == FurnishingSlot.Pillar), f => Assert.All(f.Cell.Neighbors(), n => Assert.Contains(n, room)));
		Assert.All(plan.Where(f => f.Slot == FurnishingSlot.Wall), f => Assert.Contains(f.Cell.Neighbors(), n => !room.Contains(n)));
		Assert.Contains(plan, f => f.Slot == FurnishingSlot.Pillar);
		Assert.Contains(plan, f => f.Slot == FurnishingSlot.Wall);
	}

	[Fact]
	public void Sala_pequena_nao_ganha_pilares_e_as_pecas_nao_se_encostam()
	{
		var small = Plan(Hall(2), 5);
		var plan = Plan(Hall(5), 5);

		Assert.DoesNotContain(small, f => f.Slot == FurnishingSlot.Pillar);
		foreach (var a in plan)
			foreach (var b in plan.Where(b => b.Cell != a.Cell))
				Assert.True(a.Cell.DistanceTo(b.Cell) >= 2);
	}

	[Fact]
	public void Sala_minuscula_de_passagem_nao_ganha_peca_nenhuma()
	{
		var bridge = new HashSet<HexCell> { new(-1, 0), Center, new(1, 0), new(2, 0), new(3, 0) };

		var plan = Plan(bridge, 4);

		Assert.Empty(plan);
	}
}
