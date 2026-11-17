namespace ArquipelagoDoCeu.Rules;

// O ritmo das respostas, em dados: a recarga curta do jogador entre uma resposta e a próxima. O ritmo da criatura está em `combat.fog.creature`.
public sealed record RhythmRules(float PlayerRechargeSeconds);

sealed record PlayerRhythmDto(float? RechargeSeconds);

sealed record RhythmDto(PlayerRhythmDto? Player);

static class RhythmChecks
{
	const float MaxRecharge = 5f;

	public static string? Check(RhythmDto? rhythm)
	{
		if (rhythm is null) return "Regras inválidas: o campo 'combat.rhythm' é obrigatório.";
		if (rhythm.Player?.RechargeSeconds is not { } recharge) return "Regras inválidas: o campo 'combat.rhythm.player.rechargeSeconds' é obrigatório.";
		return recharge < 0f || recharge > MaxRecharge ? $"Regras inválidas: 'combat.rhythm.player.rechargeSeconds' deve estar entre 0 e {MaxRecharge} (veio {recharge})." : null;
	}

	public static RhythmRules Build(RhythmDto rhythm) => new(rhythm.Player!.RechargeSeconds!.Value);
}
