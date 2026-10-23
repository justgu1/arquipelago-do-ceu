using System.Linq;
using System.Text;
using ArquipelagoDoCeu.Data;
using Godot;

namespace ArquipelagoDoCeu;

// Quem fez o jogo e as licenças de terceiros. As licenças precisam acompanhar o jogo distribuído, então cada uma abre aqui inteira.
public partial class CreditsScreen : OverlayScreen
{
	readonly Result<Credits> credits;
	CreditedComponent? reading;

	public CreditsScreen(Result<Credits> credits) => this.credits = credits;

	public override void _Ready()
	{
		base._Ready();
		Build();
	}

	void Build()
	{
		var focus = UiKit.FocusIndex(this);
		var box = UiKit.Card(this, reading?.Name ?? "Créditos", out var footer);
		if (!credits.IsOk) box.AddChild(UiKit.Hint("Não foi possível abrir os créditos."));
		else if (reading is null) List(box, credits.Value!);
		else
		{
			box.AddChild(LicenseText(reading));
			footer.AddChild(UiKit.BigButton("Voltar", () => Open(null)));
		}
		footer.AddChild(UiKit.BigButton("Fechar", Close));
		UiKit.RestoreFocus(this, focus);
	}

	void List(VBoxContainer box, Credits list)
	{
		box.AddChild(UiKit.Caption("Equipe"));
		foreach (var name in list.Team) box.AddChild(UiKit.Hint(name));
		box.AddChild(UiKit.Caption("Feito com"));
		foreach (var component in list.Components)
		{
			var author = component.Author is null ? "" : $" · {component.Author}";
			box.AddChild(UiKit.Hint($"{component.Name}{author} · {component.License}"));
			var target = component;
			box.AddChild(UiKit.BigButton($"Ler a licença: {component.Name}", () => Open(target), 520));
		}
	}

	void Open(CreditedComponent? component)
	{
		reading = component;
		CallDeferred(nameof(Build));
	}

	static Label LicenseText(CreditedComponent component)
	{
		var label = new Label { Text = component.File is null ? EngineLicense() : ReadFile(component.File), AutowrapMode = TextServer.AutowrapMode.WordSmart };
		label.AddThemeFontSizeOverride("font_size", 14);
		label.AddThemeColorOverride("font_color", UiKit.TextColor);
		return label;
	}

	static string ReadFile(string file)
	{
		using var text = FileAccess.Open("res://" + file, FileAccess.ModeFlags.Read);
		if (text is not null) return text.GetAsText();
		GD.PushWarning($"Não foi possível abrir res://{file}: {FileAccess.GetOpenError()}");
		return "Não foi possível abrir o texto desta licença.";
	}

	// O próprio motor guarda a licença dele e as dos componentes que embute; o texto vem daí, sempre da versão em uso.
	static string EngineLicense()
	{
		var text = new StringBuilder(Engine.GetLicenseText()).Append("\n\n");
		foreach (var info in Engine.GetCopyrightInfo())
		{
			text.Append(info["name"].AsString()).Append('\n');
			foreach (var part in info["parts"].AsGodotArray<Godot.Collections.Dictionary>())
			{
				foreach (var line in part["copyright"].AsStringArray()) text.Append("  © ").Append(line).Append('\n');
				text.Append("  ").Append(part["license"].AsString()).Append('\n');
			}
		}
		foreach (var (license, body) in Engine.GetLicenseInfo().Select(p => (p.Key.AsString(), p.Value.AsString())))
			text.Append("\n\n").Append(license).Append("\n\n").Append(body);
		return text.ToString();
	}
}
