using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.Rules;

namespace ArquipelagoDoCeu.Combat;

public enum BasicAttack { Precise, Diagonal, Charged }

// Névoa: os Ataques básicos tiram a Névoa da Etapa. Qte: ela zerou e a criança responde ao Desafio. Won: o golpe final passou.
public enum FogPhase { Fog, Qte, Won }

public enum FogEvent { None, ShiftApplied, QteTimedOut }

// Os três níveis de prêmio mudam espetáculo e Energia, nunca o progresso.
public enum Prize { Hit, Perfect, Close }

public sealed record StrikeResult(int Damage, bool Weak, bool StageCleared);

public sealed record QtePass(Prize Prize, bool FinalBlow);

// A luta em duas camadas (ADR 0009): Ataques básicos tiram a Névoa da Etapa e, ao zerar, abre o QTE. Passar encerra a Etapa;
// falhar (erro, fim da janela ou dúvida) devolve uma fração da Névoa. O custo do erro é da sessão de combate, nunca daqui.
public sealed class FogFight
{
	readonly FogRules rules;
	readonly Pcg32 rng;
	readonly float[] cooldown = new float[3];
	bool shifted;
	float shiftIn;
	int attempts;
	bool hinted;

	public FogFight(FogRules rules, bool calm, Pcg32 rng)
	{
		this.rules = rules;
		this.rng = rng;
		Calm = calm;
		Weakness = (BasicAttack)rng.NextInt(3);
		Fog = rules.StageFog;
	}

	// No Modo calmo não há relógio no QTE e a Recarga cai para cerca de 1 s.
	public bool Calm { get; }

	public FogPhase Phase { get; private set; } = FogPhase.Fog;

	public int Stage { get; private set; } = 1;

	public int Stages => rules.Stages;

	public int Fog { get; private set; }

	public int StageFog => rules.StageFog;

	public BasicAttack Weakness { get; private set; }

	// A fraqueza que vem aí, durante o aviso; nula fora dele.
	public BasicAttack? ShiftingTo { get; private set; }

	// Quanto falta da janela do QTE, em segundos. Sem relógio no Modo calmo.
	public float QteLeft { get; private set; }

	public bool HasClock => !Calm;

	public float QteSeconds => rules.QteSeconds;

	public bool IsReady(BasicAttack attack) => Phase == FogPhase.Fog && cooldown[(int)attack] <= 0f;

	public float RechargeLeft(BasicAttack attack) => Math.Max(0f, cooldown[(int)attack]);

	// De 0 a 1: o anel que enche no botão.
	public float RechargeProgress(BasicAttack attack)
	{
		var total = RechargeTotal(attack);
		return total <= 0f ? 1f : Math.Clamp(1f - cooldown[(int)attack] / total, 0f, 1f);
	}

	// O ataque só sai pronto e na fase da Névoa. O certo tira mais, o errado menos, e nunca zero.
	public StrikeResult? Strike(BasicAttack attack)
	{
		if (!IsReady(attack)) return null;
		var weak = attack == Weakness;
		var damage = Math.Max(1, (int)Math.Round(rules.Damage[(int)attack] * (weak ? rules.WeakMultiplier : rules.OtherMultiplier), MidpointRounding.AwayFromZero));
		Fog = Math.Max(0, Fog - damage);
		cooldown[(int)attack] = RechargeTotal(attack);
		if (Fog > 0) return new StrikeResult(damage, weak, false);

		Phase = FogPhase.Qte;
		attempts++;
		hinted = false;
		QteLeft = rules.QteSeconds;
		return new StrikeResult(damage, weak, true);
	}

	// Pedir dica nunca custa; só rebaixa o prêmio daquela tentativa.
	public void NoteHint() => hinted = true;

	// Passou: a Etapa acaba de vez e a Névoa dela não volta. A última Etapa é o golpe final.
	public QtePass? QtePassed()
	{
		if (Phase != FogPhase.Qte) return null;
		var left = Calm ? 1f : QteLeft / rules.QteSeconds;
		var prize = hinted || left < rules.CloseTimeLeft ? Prize.Close : attempts == 1 && left >= rules.PerfectTimeLeft ? Prize.Perfect : Prize.Hit;
		if (Stage >= rules.Stages)
		{
			Phase = FogPhase.Won;
			return new QtePass(prize, true);
		}

		Stage++;
		attempts = 0;
		Fog = rules.StageFog;
		Phase = FogPhase.Fog;
		Array.Clear(cooldown);
		if (!shifted) ScheduleShift();
		return new QtePass(prize, false);
	}

	// Falhou (resposta errada, fim da janela ou dúvida): devolve a fração da Névoa da Etapa e a criança precisa tirá-la de novo.
	public void QteFailed()
	{
		if (Phase != FogPhase.Qte) return;
		Fog = Math.Max(1, (int)Math.Ceiling(rules.StageFog * rules.ReturnFraction));
		Phase = FogPhase.Fog;
		Array.Clear(cooldown);
	}

	// O tempo real do quadro: a Recarga corre na Névoa, a janela corre no QTE (menos no Modo calmo) e o aviso da fraqueza vira troca.
	public FogEvent Tick(float seconds)
	{
		if (Phase == FogPhase.Fog)
		{
			for (var i = 0; i < cooldown.Length; i++) cooldown[i] = Math.Max(0f, cooldown[i] - seconds);
			return TickShift(seconds);
		}
		if (Phase != FogPhase.Qte || Calm) return FogEvent.None;

		QteLeft = Math.Max(0f, QteLeft - seconds);
		if (QteLeft > 0f) return FogEvent.None;
		QteFailed();
		return FogEvent.QteTimedOut;
	}

	// A Energia que o prêmio dá de volta, dos dados da Energia do combate.
	public static int EnergyFor(Prize prize, EnergyRules energy) => prize switch { Prize.Perfect => energy.Perfect, Prize.Hit => energy.Hit, _ => 0 };

	float RechargeTotal(BasicAttack attack) => Calm ? rules.CalmRechargeSeconds : rules.RechargeSeconds[(int)attack];

	void ScheduleShift()
	{
		shifted = true;
		var others = Enum.GetValues<BasicAttack>().Where(a => a != Weakness).ToList();
		ShiftingTo = others[rng.NextInt(others.Count)];
		shiftIn = rules.ShiftWarningSeconds;
	}

	FogEvent TickShift(float seconds)
	{
		if (ShiftingTo is not { } next) return FogEvent.None;
		shiftIn -= seconds;
		if (shiftIn > 0f) return FogEvent.None;
		Weakness = next;
		ShiftingTo = null;
		return FogEvent.ShiftApplied;
	}
}
