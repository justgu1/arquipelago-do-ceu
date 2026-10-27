using System;
using Godot;

namespace ArquipelagoDoCeu;

// O HUD do Lobby no tema Livro: o card do avatar em cima à esquerda, Perfil e Ajustes em cima à direita, o dock do Inventário
// embaixo à esquerda e, embaixo no centro, o recado e o botão do objeto que a criança tem por perto.
public partial class LobbyHud : Control
{
	const int Margin = 12;
	const int ContextWidth = 420;

	readonly AvatarCard card;
	readonly Label notice = BookStyle.Text("", 18);
	readonly Button interact = BookStyle.Button("", () => { }, "Interact");
	readonly PanelContainer context;
	readonly Control topRight;
	string interaction = "";

	public LobbyHud(Action<string> speak)
	{
		Name = "LobbyHud";
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		card = new AvatarCard(speak, canEdit: true);
		card.EditRequested += () => EditRequested?.Invoke();
		Dock = new Dock();
		Dock.InventoryRequested += () => InventoryRequested?.Invoke();
		Dock.RoleSwitchRequested += () => RoleSwitchRequested?.Invoke();
		context = BuildContext();
		AddChild(card);
		topRight = BuildTopRight();
		AddChild(topRight);
		AddChild(Dock);
		AddChild(context);
		interact.Pressed += () => InteractRequested?.Invoke();
		UpdateContext();
	}

	public event Action? EditRequested;

	public event Action? ProfilesRequested;

	public event Action? SettingsRequested;

	public event Action? InteractRequested;

	public event Action? InventoryRequested;

	public event Action? RoleSwitchRequested;

	public AvatarCard Card => card;

	public Dock Dock { get; }

	public void SetCard(string nickname, string roleName, int mastery)
	{
		card.Set(nickname, roleName, mastery);
		Dock.SetRole(roleName);
	}

	public void SetNotice(string text)
	{
		notice.Text = text;
		UpdateContext();
	}

	public void SetInteraction(string? text)
	{
		// A tecla entre colchetes é dica de teclado; no celular só atrapalha.
		interaction = DeviceInfo.IsTouch ? (text ?? "").Replace(" [E]", "") : text ?? "";
		interact.Text = interaction;
		UpdateContext();
	}

	// Com uma luta aberta ou fora do jogo livre, o HUD some; a luta tem a própria tela.
	// No celular o direcional fica na luta de treino, para desviar das bolhas; só os botões em volta e o resto do HUD somem.
	public void SetPlayState(bool enabled, bool fighting = false)
	{
		var keepStick = fighting && DeviceInfo.IsTouch;
		Visible = enabled || keepStick;
		card.Visible = topRight.Visible = context.Visible = !keepStick;
		Dock.SetInCombat(keepStick);
	}

	Control BuildTopRight()
	{
		var row = new HBoxContainer { Name = "TopRight" };
		row.AddThemeConstantOverride("separation", 8);
		row.AddChild(BookStyle.Button("Perfil", () => ProfilesRequested?.Invoke(), "Profiles"));
		row.AddChild(BookStyle.Button("Ajustes", () => SettingsRequested?.Invoke(), "Settings"));
		row.SetAnchorsPreset(LayoutPreset.TopRight);
		row.GrowHorizontal = GrowDirection.Begin;
		row.OffsetRight = -Margin;
		row.OffsetTop = Margin;
		return row;
	}

	PanelContainer BuildContext()
	{
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 4);
		notice.MaxLinesVisible = 2;
		column.AddChild(notice);
		column.AddChild(interact);
		var panel = BookStyle.Panel(column);
		panel.Name = "Context";
		panel.CustomMinimumSize = new Vector2(ContextWidth, 0);
		panel.SetAnchorsPreset(LayoutPreset.CenterBottom);
		panel.GrowHorizontal = GrowDirection.Both;
		panel.GrowVertical = GrowDirection.Begin;
		panel.OffsetBottom = -Margin;
		return panel;
	}

	void UpdateContext()
	{
		interact.Visible = interaction.Length != 0;
		context.Visible = notice.Text.Length != 0 || interaction.Length != 0;
	}
}
