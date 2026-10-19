using System;
using Godot;

namespace ArquipelagoDoCeu;

// O menu da Dungeon: continuar, os ajustes e voltar ao Lobby. Abre com o botão Menu ou com Esc, fora de luta; Esc fecha.
public partial class DungeonMenu : OverlayScreen
{
	readonly Action openSettings;
	readonly Action leave;

	public DungeonMenu(Action openSettings, Action leave)
	{
		Name = "DungeonMenu";
		this.openSettings = openSettings;
		this.leave = leave;
		MouseFilter = MouseFilterEnum.Stop;
	}

	public override void _Ready()
	{
		base._Ready();
		var shade = new ColorRect { Color = new Color(0.03f, 0.06f, 0.09f, 0.55f), MouseFilter = MouseFilterEnum.Stop };
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(shade);

		var column = new VBoxContainer { CustomMinimumSize = new Vector2(380, 0) };
		column.AddThemeConstantOverride("separation", 10);
		column.AddChild(BookStyle.Text("Menu", 30));
		column.AddChild(BookStyle.Button("Continuar", Close, "Continue", 64));
		column.AddChild(BookStyle.Button("Ajustes", () => { Close(); openSettings(); }, "Settings", 64));
		column.AddChild(BookStyle.Button("Voltar ao Lobby", () => { Close(); leave(); }, "BackToLobby", 64));
		var panel = BookStyle.Panel(column);
		panel.Name = "MenuPanel";
		var center = new CenterContainer();
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		center.AddChild(panel);
		AddChild(center);
	}
}
