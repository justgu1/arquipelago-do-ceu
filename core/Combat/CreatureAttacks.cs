using System.Numerics;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.Rules;

namespace ArquipelagoDoCeu.Combat;

// Investida: uma faixa reta até a criança. Pulo: um círculo onde a criança estava. Giro: um anel em volta da criatura.
public enum CreatureAttackKind { Lunge, Slam, Spin }

public enum CreatureAttackPhase { Waiting, Warning }

// O que aconteceu neste instante: o aviso começou, o golpe acertou a criança ou errou.
public enum CreatureAttackEvent { None, Warned, Hit, Missed }

// Os ataques básicos da criatura: ela escolhe um dos que estão prontos, avisa a área (a mira trava no aviso) e golpeia.
// Sair da área durante o aviso é desviar. Recarga, aviso e cansaço vêm de dados e não dependem do Nível: a ação nunca fica mais difícil.
public sealed class CreatureAttacks
{
	readonly CreatureRules rules;
	readonly Pcg32 rng;
	readonly float[] cooldown = new float[3];
	float gap;
	float clock;
	int last = -1;

	public CreatureAttacks(CreatureRules rules, Pcg32 rng)
	{
		this.rules = rules;
		this.rng = rng;
		Reset();
	}

	public CreatureRules Rules => rules;

	public CreatureAttackPhase Phase { get; private set; } = CreatureAttackPhase.Waiting;

	public CreatureAttackKind Current { get; private set; }

	// Onde a criança estava quando o aviso começou: o Pulo cai aqui e a Investida segue para cá.
	public Vector2 Aim { get; private set; }

	public float WarningProgress => Phase == CreatureAttackPhase.Warning ? Math.Min(1f, clock / rules.Attacks[(int)Current].WarningSeconds) : 0f;

	// `tiredness` (de 0 a 1) alonga a Recarga do ataque que acabou de sair; o aviso em andamento não muda.
	public CreatureAttackEvent Update(float seconds, Vector2 from, Vector2 player, float tiredness)
	{
		for (var i = 0; i < cooldown.Length; i++) cooldown[i] = Math.Max(0f, cooldown[i] - seconds);
		if (Phase == CreatureAttackPhase.Warning) return Warn(seconds, from, player, tiredness);

		gap -= seconds;
		if (gap > 0f || Pick() is not { } next) return CreatureAttackEvent.None;
		Current = (CreatureAttackKind)next;
		last = next;
		Phase = CreatureAttackPhase.Warning;
		clock = 0f;
		Aim = player;
		return CreatureAttackEvent.Warned;
	}

	// Uma luta nova começa sem aviso no ar e com o primeiro golpe só depois de um respiro.
	public void Reset()
	{
		Phase = CreatureAttackPhase.Waiting;
		clock = 0f;
		last = -1;
		gap = rules.MinGapSeconds;
		for (var i = 0; i < cooldown.Length; i++) cooldown[i] = rules.Attacks[i].CooldownSeconds * 0.5f * i;
	}

	// O caminho da Investida a partir da criatura.
	public Vector2 DirectionFrom(Vector2 from)
	{
		var way = Aim - from;
		return way.LengthSquared() < 0.0001f ? Vector2.UnitX : Vector2.Normalize(way);
	}

	public bool Covers(Vector2 from, Vector2 player) => Current switch
	{
		CreatureAttackKind.Lunge => DistanceToSegment(player, from, from + DirectionFrom(from) * rules.LungeRange) <= rules.LungeWidth / 2f,
		CreatureAttackKind.Slam => Vector2.Distance(player, Aim) <= rules.SlamRadius,
		_ => Vector2.Distance(player, from) <= rules.SpinRadius,
	};

	CreatureAttackEvent Warn(float seconds, Vector2 from, Vector2 player, float tiredness)
	{
		clock += seconds;
		if (clock < rules.Attacks[(int)Current].WarningSeconds) return CreatureAttackEvent.None;

		var hit = Covers(from, player);
		var slowdown = 1f + rules.MaxSlowdown * Math.Clamp(tiredness, 0f, 1f);
		cooldown[(int)Current] = rules.Attacks[(int)Current].CooldownSeconds * slowdown;
		gap = rules.MinGapSeconds * slowdown;
		Phase = CreatureAttackPhase.Waiting;
		return hit ? CreatureAttackEvent.Hit : CreatureAttackEvent.Missed;
	}

	// Entre os ataques prontos, escolhe ao acaso e evita repetir o anterior quando há outro.
	int? Pick()
	{
		var ready = Enumerable.Range(0, cooldown.Length).Where(i => cooldown[i] <= 0f).ToList();
		if (ready.Count > 1) ready.Remove(last);
		return ready.Count == 0 ? null : ready[rng.NextInt(ready.Count)];
	}

	static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
	{
		var ab = b - a;
		var t = Math.Clamp(Vector2.Dot(point - a, ab) / ab.LengthSquared(), 0f, 1f);
		return Vector2.Distance(point, a + ab * t);
	}
}
