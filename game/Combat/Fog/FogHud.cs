using System;
using System.Collections.Generic;
using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// A HUD da camada da Névoa (ADR 0009): a Etapa, a barra e o número da Névoa, a fraqueza da criatura por ícone e nome, e os três
// Ataques básicos do lado direito, oposto ao do movimento (teclas 1, 2 e 3). Também corre o relógio da luta, em tempo de jogo.
public partial class FogHud : Control
{
	static readonly BasicAttack[] Attacks = [BasicAttack.Precise, BasicAttack.Diagonal, BasicAttack.Charged];

	readonly Dictionary<BasicAttack, (Button Button, AttackIcon Icon)> buttons = [];
	readonly Label stage = BookStyle.Text("", 20);
	readonly Label fogText = BookStyle.Text("", 20);
	readonly Label weakness = BookStyle.Text("", 18);
	readonly Label warning = BookStyle.Text("", 20);
	readonly Label clock = BookStyle.Text("", 18);
	readonly ProgressBar fogBar = new() { ShowPercentage = false, CustomMinimumSize = new Vector2(320, 22) };
	readonly ProgressBar clockBar = new() { ShowPercentage = false, CustomMinimumSize = new Vector2(320, 14) };
	readonly AudioStreamPlayer sound = new();
	readonly Control weaknessSlot = new() { CustomMinimumSize = new Vector2(36, 36), MouseFilter = MouseFilterEnum.Ignore };
	BasicAttack? shownWeakness;
	Control attackColumn = null!;
	FogFight fight;
	bool warned;

	public FogHud(FogFight fight)
	{
		Name = "FogHud";
		this.fight = fight;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
	}

	public event Action<BasicAttack>? Attack;

	// O fim da janela do QTE já devolveu a Névoa no core; quem usa só precisa fechar a tela de resposta.
	public event Action? TimedOut;

	// O que a criatura diz em voz e em texto: a fraqueza vai mudar e mudou.
	public event Action<string>? Said;

	public void Bind(FogFight next)
	{
		fight = next;
		warned = false;
		Refresh();
	}

	public override void _Ready()
	{
		AddChild(sound);
		sound.Stream = Tones.Warning;
		foreach (var bar in new[] { fogBar, clockBar })
		{
			bar.AddThemeStyleboxOverride("background", BookStyle.Box(BookStyle.Disabled));
			bar.AddThemeStyleboxOverride("fill", BookStyle.Box(BookStyle.Border));
		}
		AddChild(BuildTop());
		attackColumn = BuildAttacks();
		AddChild(attackColumn);
		Refresh();
	}

	Control BuildTop()
	{
		var weaknessRow = new HBoxContainer();
		weaknessRow.AddThemeConstantOverride("separation", 8);
		weaknessRow.AddChild(weaknessSlot);
		weakness.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		weaknessRow.AddChild(weakness);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 4);
		foreach (var child in new Control[] { stage, fogBar, fogText, weaknessRow, warning, clockBar, clock }) column.AddChild(child);
		var panel = BookStyle.Panel(column);
		panel.Name = "FogPanel";
		panel.CustomMinimumSize = new Vector2(380, 0);
		panel.MouseFilter = MouseFilterEnum.Ignore;
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop, LayoutPresetMode.Minsize);
		panel.OffsetTop = 12;
		panel.GrowHorizontal = GrowDirection.Both;
		return panel;
	}

	Control BuildAttacks()
	{
		var column = new VBoxContainer { Name = "Attacks", MouseFilter = MouseFilterEnum.Ignore };
		column.AddThemeConstantOverride("separation", 8);
		for (var i = 0; i < Attacks.Length; i++)
		{
			var attack = Attacks[i];
			var button = BookStyle.Button("", () => Attack?.Invoke(attack), $"Attack_{attack}", 64);
			button.CustomMinimumSize = new Vector2(270, 64);
			button.Alignment = HorizontalAlignment.Right;
			button.FocusMode = FocusModeEnum.None;
			button.TooltipText = $"Tecla {i + 1}";
			var icon = new AttackIcon(attack) { Position = new Vector2(10, 10) };
			button.AddChild(icon);
			column.AddChild(button);
			buttons[attack] = (button, icon);
		}
		column.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight, LayoutPresetMode.Minsize);
		column.OffsetRight = -16;
		column.OffsetBottom = -16;
		column.GrowHorizontal = GrowDirection.Begin;
		column.GrowVertical = GrowDirection.Begin;
		return column;
	}

	public override void _Process(double delta)
	{
		// Recarga e janela contam em segundos de relógio, mesmo com o mundo em câmera lenta; parado (cutscene), o relógio também para.
		var real = Engine.TimeScale > 0.0 ? (float)(delta / Engine.TimeScale) : 0f;
		switch (fight.Tick(real))
		{
			case FogEvent.ShiftApplied:
				Said?.Invoke($"A fraqueza mudou: agora é o ataque {Label(fight.Weakness)}!");
				break;
			case FogEvent.QteTimedOut:
				TimedOut?.Invoke();
				break;
		}
		if (fight.ShiftingTo is { } next && !warned)
		{
			warned = true;
			sound.Play();
			Said?.Invoke($"Atenção! A fraqueza vai mudar para o ataque {Label(next)}.");
		}
		Refresh();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key || Controls.TypingPanels > 0 || fight.Phase != FogPhase.Fog) return;
		var index = key.PhysicalKeycode switch { Key.Key1 or Key.Kp1 => 0, Key.Key2 or Key.Kp2 => 1, Key.Key3 or Key.Kp3 => 2, _ => -1 };
		if (index < 0) return;
		GetViewport().SetInputAsHandled();
		Attack?.Invoke(Attacks[index]);
	}

	void Refresh()
	{
		var inFog = fight.Phase == FogPhase.Fog;
		stage.Text = $"Etapa {fight.Stage} de {fight.Stages}";
		fogBar.MaxValue = fight.StageFog;
		fogBar.Value = fight.Fog;
		fogText.Text = inFog ? $"Névoa: {fight.Fog} de {fight.StageFog}" : "Névoa zerada! Hora do desafio.";
		ShowWeaknessIcon();
		weakness.Text = $"Fraqueza: ataque {Label(fight.Weakness)}";
		warning.Visible = fight.ShiftingTo is not null;
		warning.Text = fight.ShiftingTo is { } next ? $"Atenção! Vai mudar para: {Label(next)}" : "";
		RefreshClock();
		attackColumn.Visible = inFog;
		foreach (var (attack, (button, icon)) in buttons) RefreshAttack(attack, button, icon);
	}

	void RefreshClock()
	{
		var inQte = fight.Phase == FogPhase.Qte;
		clock.Visible = inQte;
		clockBar.Visible = inQte && fight.HasClock;
		if (!inQte) return;
		clock.Text = fight.HasClock ? $"Tempo: {Math.Ceiling(fight.QteLeft):0} s" : "Sem pressa: sem relógio.";
		clockBar.MaxValue = fight.QteSeconds;
		clockBar.Value = fight.QteLeft;
	}

	// Pronto ou recarregando se diz em palavra e número, e o anel fecha; a cor sozinha nunca é a pista.
	void RefreshAttack(BasicAttack attack, Button button, AttackIcon icon)
	{
		var weak = attack == fight.Weakness;
		var left = fight.RechargeLeft(attack);
		var state = left <= 0f ? "Pronto" : $"{left:0.0} s";
		button.Text = $"[{(int)attack + 1}] {Label(attack)}\n{state}{(weak ? " · ★ fraqueza" : "")}";
		button.Disabled = !fight.IsReady(attack);
		icon.SetProgress(fight.RechargeProgress(attack));
	}

	// O ícone da fraqueza só é refeito quando ela muda.
	void ShowWeaknessIcon()
	{
		if (shownWeakness == fight.Weakness) return;
		shownWeakness = fight.Weakness;
		foreach (var old in weaknessSlot.GetChildren()) old.QueueFree();
		weaknessSlot.AddChild(new AttackIcon(fight.Weakness, 36));
	}

	public static string Label(BasicAttack attack) => attack switch { BasicAttack.Precise => "Precisa", BasicAttack.Diagonal => "Diagonal", _ => "Carregado" };
}
