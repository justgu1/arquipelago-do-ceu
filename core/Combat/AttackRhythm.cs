using ArquipelagoDoCeu.Rules;

namespace ArquipelagoDoCeu.Combat;

// A recarga do jogador entre uma resposta e a próxima, em tempo real. Demorar não pune: o golpe só espera.
public sealed class AttackRhythm
{
	readonly CombatRules rules;
	float recharge;

	public AttackRhythm(CombatRules rules) => this.rules = rules;

	public bool PlayerReady => recharge <= 0f;

	// O quanto da recarga já passou, de 0 a 1, para a tela mostrar a espera.
	public float RechargeProgress => rules.Rhythm.PlayerRechargeSeconds <= 0f ? 1f : Math.Clamp(1f - recharge / rules.Rhythm.PlayerRechargeSeconds, 0f, 1f);

	public void PlayerAnswered() => recharge = rules.Rhythm.PlayerRechargeSeconds;

	public void Reset() => recharge = 0f;

	public void Tick(float realSeconds) => recharge = Math.Max(0f, recharge - realSeconds);
}
