namespace ArquipelagoDoCeu.Rules;

// Os ataques básicos da criatura: o golpe tem Recarga própria e um aviso antes (a mira trava no aviso).
public sealed record CreatureAttackRules(float CooldownSeconds, float WarningSeconds);

// A criatura também tem três ataques básicos (Investida, Pulo e Giro) e escolhe entre os que estão prontos. `MinGapSeconds` separa um golpe do outro.
// O cansaço sobe a cada golpe que ela leva e alonga a Recarga dela em até `MaxSlowdown` vezes.
public sealed record CreatureRules(
	IReadOnlyList<CreatureAttackRules> Attacks, float LungeWidth, float LungeRange, float SlamRadius, float SpinRadius,
	float MinGapSeconds, float TirednessPerStrike, float TirednessDecayPerSecond, float MaxSlowdown)
{
	public static CreatureRules Default { get; } = new([new(6f, 1.5f), new(8f, 1.8f), new(10f, 2f)], 1.6f, 9f, 1.6f, 3f, 3f, 0.12f, 0.02f, 1.5f);
}

// O combate em duas camadas (ADR 0009), em dados: Etapas, Névoa, dano e Recarga de cada Ataque básico (Precisa, Diagonal, Carregado),
// multiplicadores da fraqueza, fração devolvida, janela do QTE, limiares do prêmio (fração do tempo que sobra) e os ataques da criatura.
public sealed record FogRules(
	int Stages, int StageFog, IReadOnlyList<int> Damage, IReadOnlyList<float> RechargeSeconds, float WeakMultiplier, float OtherMultiplier,
	float ReturnFraction, float QteSeconds, float CalmRechargeSeconds, float ShiftWarningSeconds, float PerfectTimeLeft, float CloseTimeLeft, CreatureRules Creature)
{
	public static FogRules Default { get; } = new(2, 40, [3, 4, 6], [0.8f, 1.2f, 1.8f], 2f, 0.5f, 0.5f, 25f, 0.6f, 2.5f, 0.3f, 0.1f, CreatureRules.Default);
}

sealed record CreatureAttackDto(float? CooldownSeconds, float? WarningSeconds);

sealed record CreatureDto(
	CreatureAttackDto[]? Attacks, float? LungeWidth, float? LungeRange, float? SlamRadius, float? SpinRadius,
	float? MinGapSeconds, float? TirednessPerStrike, float? TirednessDecayPerSecond, float? MaxSlowdown);

sealed record FogDto(
	int? Stages, int? StageFog, int[]? Damage, float[]? RechargeSeconds, float? WeakMultiplier, float? OtherMultiplier,
	float? ReturnFraction, float? QteSeconds, float? CalmRechargeSeconds, float? ShiftWarningSeconds, float? PerfectTimeLeft, float? CloseTimeLeft, CreatureDto? Creature);

static class FogChecks
{
	// O bloco é opcional: sem ele valem os padrões.
	public static string? Check(FogDto? d)
	{
		if (d is null) return null;
		var numbers = new (string Name, float? Value, float Min, float Max)[]
		{
			("stages", d.Stages, 1f, 4f), ("stageFog", d.StageFog, 3f, 200f), ("weakMultiplier", d.WeakMultiplier, 1f, 5f),
			("otherMultiplier", d.OtherMultiplier, 0.1f, 1f), ("returnFraction", d.ReturnFraction, 0.1f, 1f), ("qteSeconds", d.QteSeconds, 5f, 120f),
			("calmRechargeSeconds", d.CalmRechargeSeconds, 0f, 5f), ("shiftWarningSeconds", d.ShiftWarningSeconds, 1f, 10f),
			("perfectTimeLeft", d.PerfectTimeLeft, 0f, 1f), ("closeTimeLeft", d.CloseTimeLeft, 0f, 0.5f),
		};
		if (CheckNumbers("combat.fog", numbers) is { } problem) return problem;
		if (d.Damage is not { Length: 3 } damage || damage.Any(x => x is < 1 or > 50)) return "Regras inválidas: 'combat.fog.damage' precisa de 3 valores entre 1 e 50 (Precisa, Diagonal, Carregado).";
		if (d.RechargeSeconds is not { Length: 3 } recharge || recharge.Any(x => x is < 0.3f or > 10f)) return "Regras inválidas: 'combat.fog.rechargeSeconds' precisa de 3 valores entre 0,3 e 10.";
		return d.CloseTimeLeft >= d.PerfectTimeLeft ? "Regras inválidas: 'combat.fog.closeTimeLeft' precisa ser menor que 'perfectTimeLeft'." : CheckCreature(d.Creature);
	}

	static string? CheckCreature(CreatureDto? c)
	{
		if (c is null) return "Regras inválidas: o campo 'combat.fog.creature' é obrigatório.";
		var numbers = new (string Name, float? Value, float Min, float Max)[]
		{
			("lungeWidth", c.LungeWidth, 0.4f, 4f), ("lungeRange", c.LungeRange, 3f, 30f), ("slamRadius", c.SlamRadius, 0.5f, 5f), ("spinRadius", c.SpinRadius, 1f, 8f),
			("minGapSeconds", c.MinGapSeconds, 0.5f, 30f), ("tirednessPerStrike", c.TirednessPerStrike, 0f, 1f),
			("tirednessDecayPerSecond", c.TirednessDecayPerSecond, 0f, 1f), ("maxSlowdown", c.MaxSlowdown, 0f, 5f),
		};
		if (CheckNumbers("combat.fog.creature", numbers) is { } problem) return problem;
		if (c.Attacks is not { Length: 3 } attacks) return "Regras inválidas: 'combat.fog.creature.attacks' precisa de 3 ataques (Investida, Pulo, Giro).";
		foreach (var attack in attacks)
		{
			if (attack?.CooldownSeconds is not { } cooldown || attack.WarningSeconds is not { } warning)
				return "Regras inválidas: cada ataque de 'combat.fog.creature.attacks' precisa de 'cooldownSeconds' e 'warningSeconds'.";
			if (cooldown is < 1f or > 60f) return "Regras inválidas: 'combat.fog.creature.attacks[].cooldownSeconds' deve estar entre 1 e 60.";
			if (warning is < 0.5f or > 5f) return "Regras inválidas: 'combat.fog.creature.attacks[].warningSeconds' deve estar entre 0,5 e 5.";
			if (warning >= cooldown) return "Regras inválidas: o aviso de um ataque da criatura precisa ser menor que a Recarga dele.";
		}
		return null;
	}

	static string? CheckNumbers(string path, (string Name, float? Value, float Min, float Max)[] numbers)
	{
		foreach (var (name, value, min, max) in numbers)
		{
			if (value is null) return $"Regras inválidas: o campo '{path}.{name}' é obrigatório.";
			if (value < min || value > max) return $"Regras inválidas: '{path}.{name}' deve estar entre {min} e {max} (veio {value}).";
		}
		return null;
	}

	public static FogRules Build(FogDto? d)
	{
		if (d is null) return FogRules.Default;
		var c = d.Creature!;
		var creature = new CreatureRules([.. c.Attacks!.Select(a => new CreatureAttackRules(a.CooldownSeconds!.Value, a.WarningSeconds!.Value))],
			c.LungeWidth!.Value, c.LungeRange!.Value, c.SlamRadius!.Value, c.SpinRadius!.Value,
			c.MinGapSeconds!.Value, c.TirednessPerStrike!.Value, c.TirednessDecayPerSecond!.Value, c.MaxSlowdown!.Value);
		return new FogRules(d.Stages!.Value, d.StageFog!.Value, [.. d.Damage!], [.. d.RechargeSeconds!], d.WeakMultiplier!.Value, d.OtherMultiplier!.Value,
			d.ReturnFraction!.Value, d.QteSeconds!.Value, d.CalmRechargeSeconds!.Value, d.ShiftWarningSeconds!.Value, d.PerfectTimeLeft!.Value, d.CloseTimeLeft!.Value, creature);
	}
}
