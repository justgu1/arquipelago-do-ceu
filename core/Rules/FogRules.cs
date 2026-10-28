namespace ArquipelagoDoCeu.Rules;

// O combate em duas camadas (ADR 0009), em dados: Etapas, Névoa, dano e Recarga de cada Ataque básico (Precisa, Diagonal, Carregado),
// multiplicadores da fraqueza, fração devolvida, janela do QTE e limiares do prêmio (fração do tempo que sobra).
public sealed record FogRules(
	int Stages, int StageFog, IReadOnlyList<int> Damage, IReadOnlyList<float> RechargeSeconds, float WeakMultiplier, float OtherMultiplier,
	float ReturnFraction, float QteSeconds, float CalmRechargeSeconds, float ShiftWarningSeconds, float PerfectTimeLeft, float CloseTimeLeft)
{
	public static FogRules Default { get; } = new(2, 30, [2, 3, 4], [2f, 3f, 4f], 2f, 0.5f, 0.5f, 25f, 1f, 2.5f, 0.3f, 0.1f);
}

sealed record FogDto(
	int? Stages, int? StageFog, int[]? Damage, float[]? RechargeSeconds, float? WeakMultiplier, float? OtherMultiplier,
	float? ReturnFraction, float? QteSeconds, float? CalmRechargeSeconds, float? ShiftWarningSeconds, float? PerfectTimeLeft, float? CloseTimeLeft);

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
		foreach (var (name, value, min, max) in numbers)
		{
			if (value is null) return $"Regras inválidas: o campo 'combat.fog.{name}' é obrigatório.";
			if (value < min || value > max) return $"Regras inválidas: 'combat.fog.{name}' deve estar entre {min} e {max} (veio {value}).";
		}
		if (d.Damage is not { Length: 3 } damage || damage.Any(x => x is < 1 or > 50)) return "Regras inválidas: 'combat.fog.damage' precisa de 3 valores entre 1 e 50 (Precisa, Diagonal, Carregado).";
		if (d.RechargeSeconds is not { Length: 3 } recharge || recharge.Any(x => x is < 0.5f or > 10f)) return "Regras inválidas: 'combat.fog.rechargeSeconds' precisa de 3 valores entre 0,5 e 10.";
		return d.CloseTimeLeft >= d.PerfectTimeLeft ? "Regras inválidas: 'combat.fog.closeTimeLeft' precisa ser menor que 'perfectTimeLeft'." : null;
	}

	public static FogRules Build(FogDto? d) =>
		d is null
			? FogRules.Default
			: new FogRules(d.Stages!.Value, d.StageFog!.Value, [.. d.Damage!], [.. d.RechargeSeconds!], d.WeakMultiplier!.Value, d.OtherMultiplier!.Value,
				d.ReturnFraction!.Value, d.QteSeconds!.Value, d.CalmRechargeSeconds!.Value, d.ShiftWarningSeconds!.Value, d.PerfectTimeLeft!.Value, d.CloseTimeLeft!.Value);
}
