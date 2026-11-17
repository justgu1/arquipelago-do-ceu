namespace ArquipelagoDoCeu.Equipment;

// A mochila: quantos equipamentos a criança carrega fora do corpo, por Nível. O que está vestido não ocupa slot.
// O que passa do limite não se perde: fica no Baú do Lobby e volta à mochila quando abre espaço.
public sealed record BackpackRules(IReadOnlyList<int> SlotsByLevel)
{
	public static readonly BackpackRules Default = new([6, 9, 12]);

	public int Slots(int level) => SlotsByLevel[Math.Clamp(level, 1, SlotsByLevel.Count) - 1];
}

public sealed record BackpackContents(IReadOnlyList<string> InPack, IReadOnlyList<string> InChest, int Slots)
{
	public int Free => Math.Max(0, Slots - InPack.Count);
}

public static class Backpack
{
	const int SlotsLimit = 60;

	// `owned` vem na ordem em que a criança foi ganhando: os mais antigos têm vaga primeiro, e o novo que não cabe vai para o Baú.
	public static BackpackContents Fill(IEnumerable<string> owned, IReadOnlySet<string> worn, int slots)
	{
		var carried = owned.Where(id => !worn.Contains(id)).Distinct().ToList();
		return new BackpackContents(carried.Take(slots).ToList(), carried.Skip(slots).ToList(), slots);
	}

	internal static string? Check(BackpackRulesDto? dto)
	{
		if (dto is null) return null;
		if (dto.SlotsByLevel is not { Length: > 0 } slots) return "Regras inválidas: 'backpack.slotsByLevel' precisa de ao menos um valor.";
		if (slots.Any(s => s < 1 || s > SlotsLimit)) return $"Regras inválidas: cada valor de 'backpack.slotsByLevel' deve estar entre 1 e {SlotsLimit}.";
		return slots.Zip(slots.Skip(1)).Any(p => p.Second < p.First) ? "Regras inválidas: 'backpack.slotsByLevel' não pode diminuir de um Nível para o seguinte." : null;
	}

	internal static BackpackRules Build(BackpackRulesDto? dto) => dto?.SlotsByLevel is { } slots ? new BackpackRules([.. slots]) : BackpackRules.Default;
}

internal sealed record BackpackRulesDto(int[]? SlotsByLevel);
