using System;
using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Voice;
using Godot;
using Vector2 = System.Numerics.Vector2;

namespace ArquipelagoDoCeu;

// Os ataques da criatura na cena: avisa a área pela forma, pelo texto e pela voz, anima o golpe e diz se a criança foi alcançada.
// Quem tira a Energia é a luta; aqui só se mostra e se conta o acerto.
public sealed class CreatureDirector
{
	// Um quadro longo (janela parada) não vale como tempo de jogo.
	const float MaxFrameSeconds = 0.1f;

	readonly CreatureAttacks attacks;
	readonly CombatTime time;
	readonly Func<Vector2> creatureGround;
	readonly TrainingDummy dummy;
	readonly Player player;
	readonly VoiceService voice;

	public CreatureDirector(CreatureAttacks attacks, CombatTime time, Func<Vector2> creatureGround, TrainingDummy dummy, Player player, VoiceService voice)
	{
		this.attacks = attacks;
		this.time = time;
		this.creatureGround = creatureGround;
		this.dummy = dummy;
		this.player = player;
		this.voice = voice;
	}

	public void Reset() => attacks.Reset();

	// Apaga o aviso da cena e recomeça o relógio dos ataques.
	public void Clear()
	{
		attacks.Reset();
		dummy.HideWarning();
	}

	// Devolve verdadeiro no quadro em que o golpe alcança a criança. O relógio dos ataques corre na escala de CombatTime (lenta ao
	// desenhar), então desfaz a escala que o Godot já aplicou a `processDelta`. Parada (Modo calmo, QTE, Puzzle), a criatura não ataca.
	public bool Tick(double processDelta, bool stopped, float tiredness)
	{
		if (stopped)
		{
			if (attacks.Phase != CreatureAttackPhase.Waiting) Clear();
			return false;
		}

		var scale = (float)Engine.TimeScale;
		var realSeconds = scale > 0f ? Math.Min((float)processDelta / scale, MaxFrameSeconds) : 0f;
		var origin = creatureGround();
		var result = attacks.Update(realSeconds * time.Scale, origin, player.GroundPosition, tiredness);
		if (result == CreatureAttackEvent.Warned) voice.Say(Warning(attacks.Current), interrupt: false);
		if (attacks.Phase == CreatureAttackPhase.Warning) ShowWarning(origin);
		if (result is not (CreatureAttackEvent.Hit or CreatureAttackEvent.Missed)) return false;

		dummy.HideWarning();
		var way = attacks.DirectionFrom(origin);
		dummy.Strike(attacks.Current, new Vector3(way.X, 0f, way.Y), attacks.Rules.LungeRange);
		if (result != CreatureAttackEvent.Hit) return false;
		dummy.Hurt();
		return true;
	}

	void ShowWarning(Vector2 origin)
	{
		var rules = attacks.Rules;
		var at = new Vector3(origin.X, 0f, origin.Y);
		switch (attacks.Current)
		{
			case CreatureAttackKind.Lunge:
				var way = attacks.DirectionFrom(origin);
				dummy.ShowStrip(at, new Vector3(way.X, 0f, way.Y), rules.LungeRange, rules.LungeWidth, attacks.WarningProgress);
				break;
			case CreatureAttackKind.Slam:
				dummy.ShowDisc(new Vector3(attacks.Aim.X, 0f, attacks.Aim.Y), rules.SlamRadius, attacks.WarningProgress);
				break;
			default:
				dummy.ShowRing(at, rules.SpinRadius);
				break;
		}
	}

	static string Warning(CreatureAttackKind kind) => kind switch
	{
		CreatureAttackKind.Lunge => "Cuidado! Investida vindo. Saia da faixa.",
		CreatureAttackKind.Slam => "Cuidado! Pulo vindo. Saia do círculo.",
		_ => "Cuidado! Giro vindo. Afaste-se da criatura.",
	};
}
