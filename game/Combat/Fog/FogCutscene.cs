using System;
using Godot;

namespace ArquipelagoDoCeu;

// A cutscene do golpe final (ADR 0009): raios de luz, sem ferir nada. Dura `seconds` de relógio, com o jogo parado, e um toque,
// clique, Esc, Espaço ou Enter a pula. Quem usa decide a duração: longa na primeira vez e curta nas seguintes.
public partial class FogCutscene : Control
{
	static readonly Color Gold = Color.FromHtml("#FFD76A");
	const int Rays = 14;
	readonly float seconds;
	readonly string text;
	float progress;
	bool done;

	public FogCutscene(float seconds, string text)
	{
		Name = "FogCutscene";
		this.seconds = seconds;
		this.text = text;
		MouseFilter = MouseFilterEnum.Stop;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		ProcessMode = ProcessModeEnum.Always;
	}

	public event Action? Finished;

	public override void _Ready()
	{
		var label = Centered(text, 48, VerticalAlignment.Center);
		var hint = Centered("Toque para pular", 18, VerticalAlignment.Bottom);
		hint.OffsetBottom = -24;
		AddChild(label);
		AddChild(hint);
		// Com o jogo parado (TimeScale 0) só um Tween que ignora a escala continua andando.
		var tween = CreateTween().SetIgnoreTimeScale();
		tween.TweenMethod(Callable.From<float>(value => { progress = value; QueueRedraw(); }), 0f, 1f, seconds);
		tween.TweenCallback(Callable.From(Finish));
	}

	// Texto claro sobre o fundo escuro, na largura da tela: o rótulo do livro quebraria letra por letra sem largura.
	static Label Centered(string line, int size, VerticalAlignment vertical)
	{
		var label = BookStyle.Text(line, size);
		label.AddThemeColorOverride("font_color", Colors.White);
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = vertical;
		label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		return label;
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true }) Finish();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is not InputEventKey { Pressed: true, Echo: false } key || key.PhysicalKeycode is not (Key.Escape or Key.Space or Key.Enter or Key.KpEnter)) return;
		GetViewport().SetInputAsHandled();
		Finish();
	}

	void Finish()
	{
		if (done) return;
		done = true;
		Finished?.Invoke();
		QueueFree();
	}

	// Os raios saem do centro e crescem até o fim.
	public override void _Draw()
	{
		var shade = new Color(0.12f, 0.08f, 0.2f, Mathf.Min(0.7f, progress * 4f));
		DrawRect(new Rect2(Vector2.Zero, Size), shade);
		var center = Size / 2f;
		var reach = Mathf.Max(Size.X, Size.Y) * progress;
		for (var i = 0; i < Rays; i++)
		{
			var angle = Mathf.Tau * i / Rays + progress * 0.6f;
			var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			DrawLine(center + direction * reach * 0.15f, center + direction * reach * (i % 2 == 0 ? 0.6f : 0.4f), Gold with { A = 1f - progress * 0.6f }, i % 2 == 0 ? 6f : 3f);
		}
	}
}
