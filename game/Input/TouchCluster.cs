using System;
using Godot;

namespace ArquipelagoDoCeu;

// O canto inferior esquerdo do celular: o direcional onde ficava o Inventário e, em arco em volta dele, os botões
// do Inventário, de trocar o Papel e de Correr. Os botões ficam a um polegar de distância do direcional.
public partial class TouchCluster : Control
{
	const float Pad = 14f;
	const float Ring = 145f;
	const float Side = Pad + TouchStick.Radius + Ring + RoundButton.Radius + Pad;

	readonly RoundButton inventory = new("mochila", "Inventário");
	readonly RoundButton role = new("swap", "Trocar de Papel");
	readonly RoundButton run = new("run", "Correr", toggle: true);
	readonly TouchStick stick = new();

	public TouchCluster()
	{
		Name = "TouchCluster";
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.BottomLeft);
		OffsetLeft = 0;
		OffsetBottom = 0;
		OffsetRight = Side;
		OffsetTop = -Side;
		var center = new Vector2(Pad + TouchStick.Radius, Side - Pad - TouchStick.Radius);
		AddChild(stick);
		stick.Position = center - (stick.CustomMinimumSize / 2f);
		Place(inventory, center + new Vector2(0, -Ring));
		Place(role, center + (new Vector2(1, -1).Normalized() * Ring));
		Place(run, center + new Vector2(Ring, 0));
		inventory.Pressed += () => InventoryPressed?.Invoke();
		role.Pressed += () => RolePressed?.Invoke();
		run.Pressed += () => SetRun(run.On);
		// Sair da tela (luta, tela aberta por cima) solta o Correr: o botão não pode ficar preso ligado.
		VisibilityChanged += () => { if (!IsVisibleInTree()) SetRun(false); };
	}

	public event Action? InventoryPressed;

	public event Action? RolePressed;

	public void SetInventoryBadge(string text) => inventory.Badge = text;

	public void LockRole() => role.Locked = true;

	public void SetCompact(bool compact)
	{
		inventory.Visible = role.Visible = run.Visible = !compact;
	}

	void Place(RoundButton button, Vector2 center)
	{
		AddChild(button);
		button.Position = center - (button.CustomMinimumSize / 2f);
	}

	void SetRun(bool pressed)
	{
		run.On = pressed;
		if (pressed) Input.ActionPress(Controls.Run);
		else Input.ActionRelease(Controls.Run);
	}
}
