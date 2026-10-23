using System;
using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Rules;
using Godot;

namespace ArquipelagoDoCeu;

// O desenho livre da HUD de combate: a criança escreve em qualquer lugar da tela, o jogo lê e a luta reage.
// Ambiguidade e rabisco nunca custam Energia; só uma resposta errada reconhecida com segurança custa 1.
public partial class CombatHud
{
	FreeDrawLayer? layer;
	// As peças da mochila que a criança escolheu e ainda vai contornar no pontilhado, na ordem: (índice, o que desenhar).
	readonly List<(int Piece, string Drawing)> pending = [];
	const int ButtonSize = 56;
	bool hintShown;
	bool writing;
	Button? writeButton;

	// O que a criatura diz em voz e em texto (o balão dela) e o pedido que ela mostra, ou nulo para voltar ao normal.
	public event Action<string>? Said;

	public event Action<string?>? Requested;

	// Um toque curto no vazio não é traço: o Personagem anda até ali.
	public event Action<Vector2>? Tapped;

	void BuildLayer(CombatRules rules)
	{
		layer = new FreeDrawLayer(rules.FreeDraw, rules.Tracing, TrailSource.Shared, () => presenter.DrawAllowed);
		layer.Read += OnRead;
		layer.Tapped += point => Tapped?.Invoke(point);
		layer.ActiveChanged += _ => NotifyComposing();
	}

	// A mochila fica sempre à vista, numa fileira baixa embaixo, com os botões pequenos ao lado. O pedido e os recados
	// ficam no balão da criatura. A criança escolhe entre atacar na hora (as peças) e desenhar.
	Control BuildCompactBar()
	{
		undo = BookStyle.Button("Apagar", Undo, "Undo", ButtonSize);
		pieces = new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center, Name = "Pieces" };
		pieces.AddThemeConstantOverride("h_separation", 12);
		var bar = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		bar.AddThemeConstantOverride("separation", 8);
		var bag = BookStyle.Panel(pieces);
		bag.Name = "Bag";
		bag.CustomMinimumSize = new Vector2(0, PieceSize + 28);
		bag.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		bar.SizeFlagsVertical = SizeFlags.ShrinkEnd;
		bar.AddChild(bag);
		if (presenter.TypesLetters) writeButton = BookStyle.Button(WriteLabel, () => SetWriting(!writing), "Write", ButtonSize);
		Button[] buttons = [.. new[] { writeButton }.OfType<Button>(), undo, BookStyle.Button("Dica", AskHint, "Hint", ButtonSize), BookStyle.Button("Ouvir", () => ReadRequested?.Invoke(), "Read", ButtonSize), BookStyle.Button("Soltar alvo", Abandon, "Release", ButtonSize)];
		foreach (var button in buttons)
		{
			button.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			bar.AddChild(button);
		}
		bar.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide, LayoutPresetMode.Minsize);
		bar.OffsetLeft = Margin;
		bar.OffsetRight = -Margin;
		bar.OffsetBottom = -Margin;
		bar.GrowVertical = GrowDirection.Begin;
		return bar;
	}

	public bool Writing => writing;

	string WriteLabel => writing ? "Andar [Esc]" : "Escrever [Enter]";

	// Com letras, o teclado só escreve depois de escolher escrever (como na forca); sem isso, WASD anda.
	void SetWriting(bool on)
	{
		if (!presenter.TypesLetters || on == writing) return;
		writing = on;
		Controls.TypingPanels += on ? 1 : -1;
		layer?.SetWriting(on, presenter.GuideText?.Length ?? 0);
		if (!on) layer?.Clear();
		if (writeButton is not null) writeButton.Text = WriteLabel;
		if (on) Notice("Escreva com o teclado. Esc volta a andar.");
	}

	// Um aviso curto: no balão da criatura e em voz.
	void Notice(string text)
	{
		Said?.Invoke(text);
		voice.Say(text, interrupt: true);
	}

	void AskHint()
	{
		HintRequested?.Invoke();
		hintShown = true;
		RefreshGuide();
	}

	// O pontilhado fica sempre à vista onde a regra manda (Nível 1) e, fora isso, só depois de pedir a Dica.
	void RefreshGuide()
	{
		if (layer is null) return;
		if (pending.Count > 0)
		{
			layer.SetGuide(string.Concat(pending.Select(p => p.Drawing)), true);
			return;
		}
		var text = hintShown ? presenter.HintGuide ?? presenter.GuideText : presenter.GuideText;
		var shown = text is not null && (hintShown || presenter.GuideAlways);
		layer.SetGuide(text ?? "", shown);
		layer.SetSlots(text?.Length ?? 0);
	}

	// Tocar na peça mostra o pontilhado dela no centro; a peça entra na conta quando a criança o contorna.
	// Escolher mais peças antes de contornar põe todas na fila: o pontilhado mostra as peças juntas, na ordem, para desenhar de uma vez.
	void AskToTrace(int piece, string drawing)
	{
		if (pending.Any(p => p.Piece == piece)) return;
		pending.Add((piece, drawing));
		layer!.Clear();
		RefreshGuide();
		RefreshPieces();
		Notice("Siga o pontilhado.");
	}

	// Apagar tira a última peça da fila, e o pontilhado das outras continua.
	void DropPending()
	{
		if (pending.Count > 0) pending.RemoveAt(pending.Count - 1);
		layer?.Clear();
		RefreshGuide();
		RefreshPieces();
	}

	void OnRead(TextGuess guess)
	{
		if (presenter.Resolved)
		{
			Notice(presenter.ResolvedText);
			return;
		}
		if (!inRange())
		{
			Notice($"Chegue mais perto {creature.Of} para atacar.");
			return;
		}
		if (!rhythm.PlayerReady)
		{
			Notice("Espere um instante.");
			return;
		}
		switch (guess.Kind)
		{
			case GuessKind.Unknown:
				Notice("Não consegui ler. Tente de novo.");
				return;
			case GuessKind.Ambiguous:
				Notice("Fiquei na dúvida. Tente de novo.");
				return;
		}
		if (pending.Count > 0)
		{
			TraceChecked(guess.Text);
			return;
		}
		if (presenter.Write(guess.Text, out var refusal))
		{
			rhythm.PlayerAnswered();
			hintShown = false;
		}
		else if (refusal is not null) Notice(refusal);
		Refresh();
	}

	// A peça escolhida foi desenhada: se bate, entra na conta; se não, a criança tenta de novo, sem custo.
	// Vale desenhar todas de uma vez ou só as primeiras: as peças que bateram entram na conta, e as outras seguem esperando.
	void TraceChecked(string drawn)
	{
		var matched = Enumerable.Range(1, pending.Count).Reverse().FirstOrDefault(k => string.Equals(drawn, string.Concat(pending.Take(k).Select(p => p.Drawing)), StringComparison.OrdinalIgnoreCase));
		if (matched == 0)
		{
			Notice("Quase! Siga o pontilhado.");
			return;
		}
		var placing = pending.Take(matched).ToList();
		pending.RemoveRange(0, matched);
		foreach (var (piece, _) in placing)
		{
			if (presenter.Pieces[piece] is not { Enabled: true })
			{
				pending.Clear();
				Notice("Essa peça não cabe aí. Escolha outra.");
				break;
			}
			presenter.Place(piece);
		}
		AfterPlace();
	}

	// Digitar vale como escrever: cada tecla é um símbolo e, na pausa ou no Enter, o jogo lê.
	void TypeKey(InputEventKey key)
	{
		if (layer is null) return;
		bool handled;
		if (presenter.TypesLetters && !writing)
		{
			if (key.PhysicalKeycode is not (Key.Enter or Key.KpEnter)) return;
			SetWriting(true);
			GetViewport().SetInputAsHandled();
			return;
		}
		switch (key.PhysicalKeycode)
		{
			case Key.Backspace:
				handled = layer.IsActive;
				if (!layer.Backspace()) layer.Clear();
				break;
			case Key.Enter or Key.KpEnter:
				handled = layer.TypedText.Length > 0;
				layer.Submit();
				break;
			default:
				handled = key.Unicode != 0 && layer.Type((char)key.Unicode);
				break;
		}
		if (handled) GetViewport().SetInputAsHandled();
	}
}
