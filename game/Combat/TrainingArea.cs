using System;
using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Dungeon;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.Missions;
using ArquipelagoDoCeu.Rules;
using ArquipelagoDoCeu.Settings;
using ArquipelagoDoCeu.Voice;
using Godot;
using Vector2 = System.Numerics.Vector2;

namespace ArquipelagoDoCeu;

// A Área de treino e as lutas das Salas: liga criatura, Personagem, telas e voz à sessão de combate do core.
// Nada aqui grava no save: o treino não conta para o progresso.
public partial class TrainingArea : Node
{
	const float ReactionSeconds = 3.0f;

	readonly GameRules rules;
	readonly ICombatStage stage;
	readonly Player player;
	readonly TrainingDummy dummy;
	readonly VoiceService voice;
	readonly SettingsService settings;
	readonly Control uiRoot;
	readonly Func<int> unlockedLevel;
	readonly Func<bool> canAct;
	readonly Action<OverlayScreen> showOverlay;
	readonly Action<string> notify;
	readonly CombatTime time;
	readonly AttackRhythm rhythm;
	readonly CreatureMotion motion;
	readonly CreatureDirector creatureAttacks;
	readonly AnswerReactions reactions;
	readonly IReadOnlyList<Mission> missions;
	readonly IRandomSource random;
	readonly TowerCatalog towers;
	CombatSession? session;
	OverlayScreen? panel;
	EnergyView? energyView;
	bool reacting;
	// Na Expedição a luta é de uma Sala: vencer o Desafio chama fightWon e zerar a Energia chama fightLost.
	// A Energia mostrada é a da Expedição, que a Dungeon desenha, e não a da Área de treino.
	Action? fightWon;
	Action? fightLost;
	bool ownsEnergyView = true;
	// Cada erro e cada fim de treino mudam a época: um temporizador antigo não faz mais nada.
	int epoch;

	public TrainingArea(GameRules rules, ICombatStage stage, Player player, TrainingDummy dummy, VoiceService voice, SettingsService settings, Control uiRoot,
		Func<int> unlockedLevel, Func<bool> canAct, Action<OverlayScreen> showOverlay, Action<string> notify, IRandomSource random, TowerCatalog towers)
	{
		this.towers = towers;
		this.random = random;
		this.rules = rules;
		this.stage = stage;
		this.player = player;
		this.dummy = dummy;
		this.voice = voice;
		this.settings = settings;
		this.uiRoot = uiRoot;
		this.unlockedLevel = unlockedLevel;
		this.canAct = canAct;
		this.showOverlay = showOverlay;
		this.notify = notify;
		time = new CombatTime(rules.Combat);
		rhythm = new AttackRhythm(rules.Combat);
		creatureAttacks = new CreatureDirector(new CreatureAttacks(rules.Combat.Fog.Creature, random.NewStream()), time, () => motion!.Ground, dummy, player, voice);
		reactions = new AnswerReactions(this, dummy, player);
		missions = MissionSource.Load().Missions;

		var (ground, top) = stage.CreatureSpot;
		dummy.Place(ground, top);
		dummy.Idle();
		var stream = random.NewStream();
		motion = new CreatureMotion(rules.Combat.Movement, stage, dummy, () => stream.NextInt(10_000) / 10_000.0);
	}

	// Dentro de uma Dungeon só se luta com a criatura da Sala; não há escolha de treino.
	public bool InDungeon { get; set; }

	// As Mecânicas de luta que o jogo conhece (a de dinheiro entra quando o catálogo do Real carrega).
	public BattleMechanics Mechanics { get; set; } = BattleMechanics.Default;

	// A luta de Névoa e QTE (ADR 0009) vale para toda criatura. No Puzzle a porta não ataca e no Atalho de Nível o primeiro erro encerra a tentativa:
	// nos dois só se responde ao Desafio.
	public bool UsesFog { get; set; } = true;

	// Quantas Etapas tem a luta de Sala; sem valor valem as dos dados. O Desafio épico e o Guardião têm uma a mais.
	public int? FogStages { get; set; }

	// O Desafio épico e o Guardião têm uma Etapa a mais que a luta comum, qualquer que seja o valor dos dados.
	public int BossStages => rules.Combat.Fog.Stages + 1;

	// O piloto de testes liga isto para a criatura não tirar Energia no meio de um roteiro; só o roteiro dos ataques o desliga.
	internal static bool PilotPeace { get; set; }

	// No Atalho de Nível o primeiro erro encerra a tentativa, sem tirar Energia.
	public Action? StopOnMistake { get; set; }

	// Como o jogo chama a criatura nas frases e nos painéis.
	public CreatureWords Creature { get; set; } = CreatureWords.Dummy;

	// Onde a barra de Energia fica; sem valor, o Lobby a posiciona abaixo da interface dele.
	public int? EnergyTop { get; set; }

	public bool HasTarget => panel is not null;
	public Vector2 CreatureGround => motion.Ground;

	// Clicar na criatura a escolhe: o raio do clique passa perto do centro do Boneco.
	public bool HitsDummy(Godot.Vector2 screenPoint) => dummy.IsPickedBy(stage.Camera, screenPoint);

	public bool InTargetRange() => Vector2.Distance(player.GroundPosition, motion.Ground) <= rules.Combat.TargetRange;

	public void ToggleTarget()
	{
		if (panel is not null) DropTarget();
		else Select();
	}

	// Solta o alvo sem responder; o treino continua (com a criatura e a Energia) até a criança sair ou a Energia zerar.
	public void DropTarget() => AnswerPanels.Abandon(panel);

	// Se a criança se afasta demais, o alvo se perde.
	public void CheckRange()
	{
		if (panel is null || InTargetRange()) return;
		notify($"{Creature.Start} ficou longe.");
		DropTarget();
	}

	// Trocar de Perfil ou fechar o jogo acaba o treino sem nenhuma mensagem.
	public void End()
	{
		DropTarget();
		if (session is null) return;
		if (fightWon is not null)
		{
			// Uma luta de fora (o Desafio mestre) precisa saber que acabou, para não ficar presa em andamento.
			EndFight();
			FightAborted?.Invoke();
		}
		else Finish("");
	}

	public event Action? FightAborted;

	// Toda batalha, do Treino ou da Dungeon, termina com um resultado. O Treino o emite mas nada o conta.
	public event Action<BattleResult>? BattleEnded;

	// A Missão que acabou de ser vencida numa luta de Sala, antes de a Sala valer como vencida. O treino não dispara isto.
	public event Action<Mission>? ChallengeSolved;

	internal CombatSession? Session => session;

	// A Missão em jogo.
	public Mission? CurrentMission => session?.Mission;

	// Começa a luta de uma Sala com a Energia da Expedição. A criança escolhe a criatura (clique ou F) para atacar.
	public void BeginFight(CombatSession fight, EnergyView energy, Action won, Action lost)
	{
		epoch++;
		session = fight;
		energyView = energy;
		ownsEnergyView = false;
		fightWon = won;
		fightLost = lost;
		reacting = false;
		creatureAttacks.Reset();
		rhythm.Reset();
		StartTime();
		UpdateEnergy();
		dummy.ShowChallenge(session);
		if (UsesFog) StartFogFight();
	}

	// O Modo calmo deixa o tempo lento a luta toda.
	void StartTime()
	{
		time.Calm(settings.Current.CalmMode);
		Engine.TimeScale = time.Scale;
	}

	// Termina a luta sem mexer na Energia nem na tela dela; vale tanto para vitória quanto para sair da Sala.
	public void EndFight()
	{
		epoch++;
		DropTarget();
		session = null;
		reacting = false;
		fightWon = null;
		fightLost = null;
		energyView = null;
		ownsEnergyView = true;
		EndFog();
		creatureAttacks.Clear();
		time.End();
		time.Calm(false);
		Engine.TimeScale = time.Scale;
		if (!InDungeon) dummy.Idle();
	}

	void Select()
	{
		if (!canAct()) return;
		if (!InTargetRange())
		{
			notify($"Chegue mais perto {Creature.Of} para escolhê-{Creature.Pronoun}.");
			return;
		}
		if (session is null && InDungeon) return;
		if (session is null) showOverlay(new TrainingSetupScreen(towers.All.Select(t => (t.Id, t.Role)).ToList(), TrainingSetup.Levels(unlockedLevel(), towers.MaxLevel), TryStart));
		else if (fog is null || fog.Phase == FogPhase.Qte) OpenPanel();
	}

	// O treino é de um Papel; o equipamento dele é o que vale na luta.
	public event Action<string>? RoleChosen;

	string? TryStart(TrainingSetup setup)
	{
		var bonus = Math.Clamp(EnergyBonusFor?.Invoke(setup.Discipline) ?? extraEnergy, 0, 2);
		var started = CombatSession.Start(rules.Combat, new Energy(rules.MaxEnergy + bonus), unlockedLevel(), setup, missions, random.NewStream(), mechanics: Mechanics);
		if (!started.IsOk) return started.Error;

		RoleChosen?.Invoke(setup.Discipline);
		session = started.Value!;
		creatureAttacks.Reset();
		rhythm.Reset();
		StartTime();
		energyView = new EnergyView();
		energyView.LeaveRequested += () => Finish("Treino encerrado. Quando quiser, é só escolher o Boneco de novo.");
		uiRoot.AddChild(energyView);
		UpdateEnergy();
		dummy.ShowChallenge(session);
		StartFogFight();
		return null;
	}

	void OpenPanel()
	{
		if (session is null || panel is not null) return;
		var opened = AnswerPanels.Create(session, rules, voice, InTargetRange, Creature, rhythm, new PanelEvents(
			reactions.Attacked, reactions.NameChecked, reactions.WordChecked, reactions.ChangeChecked, OnComposing, player.WalkTo, text => dummy.Say(text, true), dummy.SetRequest,
			Hint: () => { if (session is not null) { fog?.NoteHint(); Tell($"Dica: {session.Hint()}"); } },
			Read: () => { if (session is not null) Tell(session.Mission.Prompt); }));

		panel = opened;
		UpdateEnergyDock();
		uiRoot.AddChild(opened);
		dummy.SetSelected(true);
		// Fecha também se a tela sair da cena por outro caminho: o tempo nunca pode ficar preso.
		void Release()
		{
			if (panel != opened) return;
			panel = null;
			UpdateEnergyDock();
			dummy.SetSelected(false);
			dummy.SetRequest(null);
			time.End();
			Engine.TimeScale = time.Scale;
		}
		opened.Closed += () =>
		{
			Release();
			opened.QueueFree();
		};
		opened.TreeExited += Release;
		Tell(session.Mission.Prompt);
	}

	void OnComposing(bool composing)
	{
		if (composing) time.Begin();
		else time.End();
		Engine.TimeScale = time.Scale;
	}

	internal void Tell(string text)
	{
		if (!AnswerPanels.Tell(panel, text)) voice.Say(text, interrupt: true);
	}

	// --- Erro, travessura, dica

	internal void Mistake(string headline, string lead)
	{
		if (StopOnMistake is { } stop) { EndFight(); stop(); return; }
		session!.RevealHidden();
		epoch++;
		var outcome = session.Mistake();
		UpdateEnergy();
		if (outcome.Empty)
		{
			Finish("A névoa ficou forte demais desta vez. Vamos recarregar a energia?");
			return;
		}

		dummy.Prank();
		var prank = AnswerPanels.PrankText(panel, Creature, outcome.Prank, rules.Combat.PrankSeconds);
		dummy.ShowChallenge(session);
		AnswerPanels.Refresh(panel);

		var text = $"{lead} {prank} Novo Desafio: {session.Mission.Prompt}";
		if (outcome.HintOffered) text += $" Dica: {session.Hint()}";
		Say($"{headline} Travessura!", text, good: false);

		if (QteFailed() || outcome.Prank != Prank.Hidden) return;
		var mine = epoch;
		// A peça escondida volta sozinha; o tempo é real, mesmo com o jogo em câmera lenta.
		GetTree().CreateTimer(rules.Combat.PrankSeconds, true, false, true).Timeout += () =>
		{
			if (epoch != mine || session is null) return;
			session.RevealHidden();
			AnswerPanels.Refresh(panel);
		};
	}

	// Mostra na cena e no painel; o painel também fala. Sem painel aberto, só o Boneco e a voz.
	internal void Say(string headline, string text, bool good = true)
	{
		dummy.Say(headline, good);
		if (panel is null) voice.Say(text, interrupt: true);
		else Tell(text);
	}

	internal void AttackAnimation(BasicAttack? attack = null) => player.Attack(motion.Ground, attack);

	// Energia zerada, "Sair do treino" ou troca de Perfil: a criança só sai da Área de treino, sem perder nada.
	void Finish(string message)
	{
		if (session is { IsOver: true }) BattleEnded?.Invoke(session.Conclude(calmed: false));
		if (fightLost is not null)
		{
			var lost = fightLost;
			EndFight();
			lost();
			return;
		}
		epoch++;
		DropTarget();
		session = null;
		reacting = false;
		creatureAttacks.Clear();
		if (ownsEnergyView) energyView?.QueueFree();
		energyView = null;
		EndFog();
		dummy.Idle();
		time.End();
		time.Calm(false);
		Engine.TimeScale = time.Scale;
		if (message.Length == 0) return;
		notify(message);
		dummy.Say("Fim do treino por hoje.", good: false);
	}

	// --- Quadro a quadro

	// Um quadro longo (janela parada) não pode recarregar tudo de uma vez, como o CreatureDirector também limita.
	const float MaxFrameSeconds = 0.1f;

	public override void _Process(double delta)
	{
		if (canAct()) motion.Tick((float)delta, player.GroundPosition, Creature.Moves, panel is not null, settings.Current.CalmMode);
		if (session is null || reacting || !canAct()) return;
		rhythm.Tick(Engine.TimeScale > 0.0 ? Math.Min((float)(delta / Engine.TimeScale), MaxFrameSeconds) : 0f);
		if (fog is null) return;
		dummy.SetTired(fog.Tiredness);
		// Modo calmo, QTE e pausas entre Etapas: a criatura só espera. Fora deles ela escolhe um ataque quando a Recarga deixa.
		var stopped = settings.Current.CalmMode || fog.Stunned || fogBusy || PilotPeace;
		if (!creatureAttacks.Tick(delta, stopped, fog.Tiredness)) return;
		session.CreatureHit();
		UpdateEnergy();
		if (session.IsOver) Finish("A névoa ficou forte demais desta vez. Vamos recarregar a energia?");
	}

	public override void _ExitTree() => Engine.TimeScale = 1.0;
}
