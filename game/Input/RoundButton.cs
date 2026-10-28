using System;
using Godot;

namespace ArquipelagoDoCeu;

// Botão redondo com ícone, para o dedo: o papel do Inventário, do Papel e do Correr no celular. O estado aparece em formas
// (anel grosso e um visto, cadeado, número), nunca só em cor.
public partial class RoundButton : Control
{
	public const float Radius = 40f;

	static readonly Color Paper = Color.FromHtml("#F6EACB");
	static readonly Color Border = Color.FromHtml("#946239");
	static readonly Color Ink = Color.FromHtml("#3D2C25");

	readonly Texture2D? icon;
	string badge = "";
	bool on;
	bool locked;

	public RoundButton(string iconName, string tooltip, bool toggle = false)
	{
		icon = Icons.For(iconName, 96);
		Toggle = toggle;
		TooltipText = tooltip;
		MouseFilter = MouseFilterEnum.Stop;
		CustomMinimumSize = new Vector2(Radius * 2, Radius * 2);
		Size = CustomMinimumSize;
	}

	public bool Toggle { get; }

	public bool On
	{
		get => on;
		set
		{
			on = value;
			QueueRedraw();
		}
	}

	public bool Locked
	{
		get => locked;
		set
		{
			locked = value;
			QueueRedraw();
		}
	}

	// Um número pequeno no canto, como o dos itens novos do Inventário; vazio some.
	public string Badge
	{
		get => badge;
		set
		{
			badge = value;
			QueueRedraw();
		}
	}

	public event Action? Pressed;

	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventScreenTouch { Pressed: true }:
				Activate();
				break;
			// O toque também chega como clique emulado: só o clique de mouse de verdade conta, para não acionar duas vezes.
			case InputEventMouse mouse:
				AcceptEvent();
				if (mouse is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && mouse.Device != InputEvent.DeviceIdEmulation) Activate();
				break;
		}
	}

	void Activate()
	{
		AcceptEvent();
		if (Locked) return;
		if (Toggle) On = !On;
		Pressed?.Invoke();
	}

	public override void _Draw()
	{
		var center = Size / 2f;
		var alpha = Locked ? 0.5f : 1f;
		DrawCircle(center, Radius, Paper with { A = 0.92f * alpha });
		DrawArc(center, Radius - 2f, 0f, Mathf.Tau, 40, Border with { A = alpha }, On ? 8f : 4f, antialiased: true);
		if (icon is not null) DrawTextureRect(icon, new Rect2(center - new Vector2(26, 26), new Vector2(52, 52)), false, new Color(1, 1, 1, alpha));
		if (On) DrawCheck(new Vector2(Radius * 2 - 12, 12));
		if (Locked && Icons.For("lock", 48) is { } lockIcon) DrawTextureRect(lockIcon, new Rect2(new Vector2(Radius * 2 - 26, Radius * 2 - 26), new Vector2(24, 24)), false);
		if (badge.Length > 0) DrawBadge(new Vector2(Radius * 2 - 10, 10));
	}

	void DrawCheck(Vector2 at)
	{
		DrawCircle(at, 11f, Ink);
		DrawPolyline([at + new Vector2(-5, 0), at + new Vector2(-1, 4), at + new Vector2(6, -4)], Paper, 3f, antialiased: true);
	}

	void DrawBadge(Vector2 at)
	{
		DrawCircle(at, 13f, Ink);
		var font = ThemeDB.FallbackFont;
		var size = font.GetStringSize(badge, HorizontalAlignment.Center, -1, 18);
		DrawString(font, at + new Vector2(-size.X / 2f, size.Y / 3f), badge, HorizontalAlignment.Left, -1, 18, Paper);
	}
}
