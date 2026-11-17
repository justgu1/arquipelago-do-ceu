using System;
using ArquipelagoDoCeu.Dungeon;
using Godot;

namespace ArquipelagoDoCeu;

// O mapa em tela cheia: a cena escurece e o Personagem abre a folha e a segura pelos cantos. Pausa o jogo enquanto está aberto.
// A legenda tem ícone e texto, e segurar uma Sala (ou o botão) fala em voz alta. Abre por toque, mouse ou tecla M.
public partial class MapScreen : OverlayScreen
{
	const float RiseSeconds = 0.35f;
	const float RiseDistance = 140f;

	readonly Expedition expedition;
	readonly string title;
	readonly bool reduceMotion;
	readonly bool floorDone;
	readonly Action<int> speakRoom;
	readonly Action speakMap;
	PanelContainer sheet = null!;
	CenterContainer holder = null!;
	bool unpaused;
	readonly System.Numerics.Vector2 playerAt;

	public MapScreen(Expedition expedition, string title, bool floorDone, bool reduceMotion, Action<int> speakRoom, Action speakMap, System.Numerics.Vector2 playerAt)
	{
		this.playerAt = playerAt;
		this.expedition = expedition;
		this.title = title;
		this.floorDone = floorDone;
		this.reduceMotion = reduceMotion;
		this.speakRoom = speakRoom;
		this.speakMap = speakMap;
		Name = "MapaEmTelaCheia";
		ProcessMode = ProcessModeEnum.Always;
	}

	// A Sala (ou o botão) que a criança segura fala; a tela não anda sozinha enquanto isso.
	public bool Paused => GetTree().Paused;

	public override void _Ready()
	{
		base._Ready();
		GetTree().Paused = true;
		TreeExiting += Resume;
		var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.62f), MouseFilter = MouseFilterEnum.Stop };
		dim.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(dim);

		holder = new CenterContainer();
		holder.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(holder);

		sheet = new PanelContainer { Name = "Folha" };
		sheet.AddThemeStyleboxOverride("panel", UiKit.Surface());
		holder.AddChild(sheet);
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		sheet.AddChild(box);

		var heading = UiKit.Heading(title);
		box.AddChild(heading);

		var map = new FloorMap(expedition.Floor, new Vector2(780f, 380f), labeled: true) { Source = expedition, Here = expedition.CurrentRoom, Cleared = new System.Collections.Generic.HashSet<int>(expedition.Cleared), Complete = floorDone };
		map.SetPlayer(playerAt);
		map.RoomPointed += room => speakRoom(room);
		box.AddChild(map);

		box.AddChild(RoomLegend());
		box.AddChild(Legend());
		var footer = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		footer.AddThemeConstantOverride("separation", 12);
		footer.AddChild(UiKit.BigButton("Ouvir o mapa", () => speakMap(), 200));
		var back = UiKit.BigButton("Voltar", Close, 200);
		footer.AddChild(back);
		box.AddChild(footer);
		back.GrabFocus();

		AddChild(new Hands(sheet));
		Rise();
		speakMap();
	}

	// A folha sobe e as mãos a seguram; com "reduzir animações" ela aparece direto.
	void Rise()
	{
		if (reduceMotion) return;
		holder.Modulate = new Color(1f, 1f, 1f, 0f);
		holder.Position += new Vector2(0f, RiseDistance);
		var tween = CreateTween().SetParallel();
		tween.TweenProperty(holder, "modulate:a", 1f, RiseSeconds);
		tween.TweenProperty(holder, "position:y", 0f, RiseSeconds).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed(Controls.Map))
		{
			GetViewport().SetInputAsHandled();
			Close();
			return;
		}
		base._UnhandledInput(@event);
	}

	// Cada tipo de Sala com a mesma marca do mapa (forma, cor e letra) e o nome por extenso.
	static Control RoomLegend()
	{
		var row = new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("h_separation", 16);
		foreach (var kind in Generation.RoomTypes.Default.All)
		{
			var item = new HBoxContainer();
			item.AddThemeConstantOverride("separation", 5);
			item.AddChild(new RoomIcon(kind));
			var label = new Label { Text = kind.Name };
			label.AddThemeFontSizeOverride("font_size", 18);
			label.AddThemeColorOverride("font_color", UiKit.TextColor);
			item.AddChild(label);
			row.AddChild(item);
		}
		return row;
	}

	static Control Legend()
	{
		var row = new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("h_separation", 22);
		foreach (var (icon, text) in new[] { (LegendIcon.Mark.You, "Você"), (LegendIcon.Mark.Done, "Vencida"), (LegendIcon.Mark.Portal, "Portal"), (LegendIcon.Mark.Unseen, "Não vista") })
		{
			var item = new HBoxContainer();
			item.AddThemeConstantOverride("separation", 6);
			item.AddChild(new LegendIcon(icon));
			var label = new Label { Text = text };
			label.AddThemeFontSizeOverride("font_size", 20);
			label.AddThemeColorOverride("font_color", UiKit.TextColor);
			item.AddChild(label);
			row.AddChild(item);
		}
		return row;
	}

	void Resume()
	{
		if (unpaused) return;
		unpaused = true;
		if (GetTree() is { } tree) tree.Paused = false;
	}

	sealed partial class RoomIcon : Control
	{
		readonly Generation.RoomType kind;

		public RoomIcon(Generation.RoomType kind)
		{
			this.kind = kind;
			CustomMinimumSize = new Vector2(30f, 30f);
		}

		public override void _Draw()
		{
			var c = new Vector2(15f, 15f);
			FloorMap.Badge(this, kind, c, 13f, Colors.Black);
			FloorMap.Badge(this, kind, c, 11f, FloorMap.ColorOf(kind));
			DrawString(ThemeDB.FallbackFont, c + new Vector2(-6f, 6f), kind.Letter, HorizontalAlignment.Center, 12, 17, Colors.Black);
		}
	}

	// Cada marca do mapa tem um ícone desenhado, e nunca só uma cor.
	sealed partial class LegendIcon : Control
	{
		public enum Mark { You, Done, Portal, Unseen }

		readonly Mark mark;

		public LegendIcon(Mark mark)
		{
			this.mark = mark;
			CustomMinimumSize = new Vector2(30f, 30f);
		}

		public override void _Draw()
		{
			var c = new Vector2(15f, 15f);
			switch (mark)
			{
				case Mark.You:
					DrawCircle(c + new Vector2(0f, -6f), 5f, Colors.Black);
					DrawRect(new Rect2(c + new Vector2(-5f, -1f), new Vector2(10f, 11f)), Colors.Black);
					DrawCircle(c + new Vector2(0f, -6f), 3.5f, Colors.White);
					DrawRect(new Rect2(c + new Vector2(-3.5f, 0f), new Vector2(7f, 9f)), Colors.White);
					break;
				case Mark.Done:
					Vector2[] check = [c + new Vector2(-8f, 0f), c + new Vector2(-2f, 6f), c + new Vector2(9f, -7f)];
					DrawPolyline(check, Colors.Black, 7f, true);
					DrawPolyline(check, Color.FromHtml("#7CFC8A"), 4f, true);
					break;
				case Mark.Portal:
					DrawLine(c + new Vector2(-9f, 0f), c + new Vector2(9f, 0f), Colors.Black, 7f);
					DrawLine(c + new Vector2(0f, -9f), c + new Vector2(0f, 9f), Colors.Black, 7f);
					DrawLine(c + new Vector2(-9f, 0f), c + new Vector2(9f, 0f), Colors.White, 3.5f);
					DrawLine(c + new Vector2(0f, -9f), c + new Vector2(0f, 9f), Colors.White, 3.5f);
					break;
				default:
					DrawCircle(c, 12f, Color.FromHtml("#8A8399"));
					DrawArc(c, 12f, 0f, Mathf.Tau, 20, Color.FromHtml("#E6E0F2"), 2f, true);
					DrawString(ThemeDB.FallbackFont, c + new Vector2(-5f, 7f), "?", HorizontalAlignment.Left, -1, 20, Colors.Black);
					break;
			}
		}
	}

	// As mãos do Personagem seguram os cantos de cima da folha: o braço sobe pelas laterais, a palma fica por fora da borda,
	// os quatro dedos se fecham por cima dela e o polegar apoia no alto.
	sealed partial class Hands : Control
	{
		static readonly Color Skin = Color.FromHtml("#F0BE94");
		static readonly Color SkinShade = Color.FromHtml("#D99A70");
		static readonly Color Sleeve = Color.FromHtml("#7A5AA8");
		static readonly Color Cuff = Color.FromHtml("#F4EBD6");

		readonly Control sheet;

		public Hands(Control sheet)
		{
			this.sheet = sheet;
			MouseFilter = MouseFilterEnum.Ignore;
			SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		}

		public override void _Process(double delta) => QueueRedraw();

		public override void _Draw()
		{
			var rect = sheet.GetGlobalRect();
			foreach (var left in new[] { true, false })
			{
				var inward = left ? 1f : -1f;
				var corner = new Vector2(left ? rect.Position.X + 2f : rect.End.X - 2f, rect.Position.Y + 10f);
				DrawArm(corner, inward);
				DrawHand(corner, inward);
			}
		}

		void DrawArm(Vector2 corner, float inward)
		{
			var wrist = corner + new Vector2(-inward * 20f, 52f);
			var foot = new Vector2(wrist.X - inward * 60f, Size.Y + 30f);
			Capsule(foot, wrist, 46f, Colors.Black);
			Capsule(foot, wrist, 38f, Sleeve);
			var toward = (foot - wrist).Normalized();
			Capsule(wrist + toward * 12f, wrist + toward * 22f, 48f, Colors.Black);
			Capsule(wrist + toward * 12f, wrist + toward * 22f, 40f, Cuff);
		}

		void DrawHand(Vector2 corner, float inward)
		{
			var palm = corner + new Vector2(-inward * 12f, 26f);
			DrawCircle(palm, 27f, Colors.Black);
			DrawCircle(palm, 24f, Skin);
			for (var i = 0; i < 4; i++)
			{
				var start = corner + new Vector2(-inward * 8f, 8f + i * 12f);
				var end = corner + new Vector2(inward * (18f - (i == 3 ? 5f : 0f)), 8f + i * 12f);
				Capsule(start, end, 15f, Colors.Black);
				Capsule(start, end, 11f, Skin);
				DrawLine(start + new Vector2(inward * 12f, 5f), end + new Vector2(0f, 5f), SkinShade, 1.5f);
			}
			var thumbBase = palm + new Vector2(inward * 4f, -14f);
			var thumbTip = corner + new Vector2(inward * 12f, -14f);
			Capsule(thumbBase, thumbTip, 17f, Colors.Black);
			Capsule(thumbBase, thumbTip, 13f, Skin);
		}

		void Capsule(Vector2 from, Vector2 to, float width, Color color)
		{
			DrawLine(from, to, color, width, true);
			DrawCircle(from, width / 2f, color);
			DrawCircle(to, width / 2f, color);
		}
	}
}
