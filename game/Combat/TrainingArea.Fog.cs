using System;
using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// O combate em duas camadas (ADR 0009), em protótipo: só no treino do Escriba por sílabas, com F10 ligada (FogMode).
// A Névoa zerada abre a HUD de resposta de sempre como QTE; fora dessa condição nada aqui roda.
public partial class TrainingArea
{
	const float FinalBlowSeconds = 3f;
	const float FinalBlowRepeatSeconds = 1.2f;
	const float StageReactionSeconds = 1.4f;
	FogFight? fog;
	FogHud? fogHud;
	FogCutscene? cutscene;
	bool fogBusy;
	int energyAtQte;
	int finalBlows;

	// A luta da Névoa em andamento; nula no combate de sempre. O piloto de testes a lê.
	internal FogFight? Fight => fog;

	// No treino de sempre vem logo o próximo Desafio; no combate novo vencer o Desafio é passar no QTE.
	void TrainingWon()
	{
		if (fog is null) NextChallengeSoon();
		else QtePassed();
	}

	bool BeginFog()
	{
		if (!FogMode.On || InDungeon || session?.Name is not { Kind: NameKind.Syllables }) return false;
		StartFogFight();
		return true;
	}

	void StartFogFight()
	{
		fog = new FogFight(rules.Combat.Fog, settings.Current.CalmMode, random.NewStream());
		if (fogHud is null)
		{
			fogHud = new FogHud(fog);
			fogHud.Attack += FogAttack;
			fogHud.TimedOut += QteTimedOut;
			fogHud.Said += text => { dummy.Say(text, true); voice.Say(text, interrupt: true); };
			uiRoot.AddChild(fogHud);
		}
		else fogHud.Bind(fog);
		BackToFog();
		voice.Say("Tire a Névoa com os ataques. Cada criatura tem uma fraqueza.", interrupt: true);
	}

	// O Boneco sem criatura: só a Névoa da Etapa, cheia ou no que sobrou depois de uma falha.
	void BackToFog()
	{
		if (fog is null) return;
		dummy.ShowCreature(null);
		dummy.Reset(fog.StageFog, "Névoa");
		if (fog.Fog < fog.StageFog) dummy.Reduced(fog.Fog);
	}

	// Atacar não pede estar perto: o alvo anda, e perder o ataque por isso seria demora que não ensina nada.
	void FogAttack(BasicAttack attack)
	{
		if (fog is null || fogBusy || !canAct() || fog.Strike(attack) is not { } hit) return;
		AttackAnimation();
		dummy.Ray(player.GlobalPosition + Vector3.Up, hit.Weak);
		dummy.Reduced(fog.Fog);
		dummy.Say(hit.Weak ? $"Ponto fraco! −{hit.Damage} de Névoa" : $"−{hit.Damage} de Névoa", hit.Weak);
		if (hit.StageCleared) EnterQte();
	}

	void EnterQte()
	{
		energyAtQte = session!.Energy.Current;
		dummy.ShowChallenge(session);
		OpenPanel();
		if (panel is CombatHud hud) hud.Unclear += QteUnclear;
		dummy.Say("A Névoa zerou! Agora o desafio.", true);
	}

	// A Energia do prêmio vale a partir da que a criança tinha ao abrir o QTE, e não do acerto que a luta de sempre já somou.
	void QtePassed()
	{
		if (fog?.QtePassed() is not { } pass || session is null) return;
		DropTarget();
		var target = Math.Min(session.Energy.Max, energyAtQte + FogFight.EnergyFor(pass.Prize, rules.Combat.Energy));
		var diff = target - session.Energy.Current;
		if (diff > 0) session.Energy.Recover(diff);
		else if (diff < 0) session.Energy.Lose(-diff);
		UpdateEnergy();

		var note = pass.Prize switch { Prize.Perfect => "Golpe perfeito!", Prize.Close => "Passou! Sem pressa também vale.", _ => "Acertou!" };
		if (pass.FinalBlow) FinalBlow(note);
		else NextStage(note);
	}

	// A Etapa acabou de vez: um respiro para a criança ver o que fez, e a próxima Névoa vem cheia, com outra palavra esperando no QTE.
	void NextStage(string note)
	{
		fogBusy = true;
		dummy.Say(note, true);
		session!.NextChallenge();
		var mine = ++epoch;
		GetTree().CreateTimer(StageReactionSeconds, true, false, true).Timeout += () =>
		{
			if (epoch != mine || fog is null) return;
			fogBusy = false;
			BackToFog();
		};
	}

	// A cutscene longa para o jogo; a primeira vez dura mais, e depois fica curta para não cansar a repetição.
	void FinalBlow(string note)
	{
		fogBusy = true;
		dummy.Say(note, true);
		cutscene = new FogCutscene(finalBlows++ == 0 ? FinalBlowSeconds : FinalBlowRepeatSeconds, "Golpe final!");
		cutscene.Finished += FreeCreature;
		Engine.TimeScale = 0f;
		uiRoot.AddChild(cutscene);
	}

	void FreeCreature()
	{
		cutscene = null;
		Engine.TimeScale = time.Scale;
		if (session is null || fog is null) return;
		session.NextChallenge();
		StartFogFight();
		fogBusy = false;
		dummy.Say("Criatura libertada!", true);
	}

	// Falhar devolve a fração da Névoa e nada mais. Devolve verdadeiro quando a luta nova tratou a falha.
	bool QteFailed()
	{
		if (fog is null) return false;
		fog.QteFailed();
		DropTarget();
		BackToFog();
		dummy.Say("A Névoa voltou um pouco. Tire de novo!", false);
		return true;
	}

	// Dúvida na leitura e fim da janela são falhas sem custo: a criatura traz outra palavra do mesmo Conceito.
	void QteUnclear()
	{
		if (session is null) return;
		session.NextChallenge();
		QteFailed();
	}

	void QteTimedOut()
	{
		if (session is null) return;
		voice.Say("O tempo acabou, sem custo nenhum. Vamos de novo.", interrupt: true);
		QteUnclear();
	}

	void EndFog()
	{
		fog = null;
		fogBusy = false;
		if (cutscene is not null && IsInstanceValid(cutscene)) cutscene.QueueFree();
		cutscene = null;
		fogHud?.QueueFree();
		fogHud = null;
	}
}
