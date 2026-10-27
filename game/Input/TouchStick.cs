using System;
using ArquipelagoDoCeu.Touch;
using Godot;

namespace ArquipelagoDoCeu;

// O direcional virtual do celular. Aperta as mesmas ações do teclado, com força, então o Personagem e a câmera
// não sabem que o dedo está ali. Só um dedo o controla; os outros ficam livres para os botões em volta.
public partial class TouchStick : Control
{
	public const float Radius = 80f;
	public const float Side = (Radius * 2) + 8f;

	int finger = -1;
	Vector2 offset;

	// O tamanho vem do construtor: quem o encaixa precisa dele antes de o direcional entrar na árvore.
	public TouchStick()
	{
		MouseFilter = MouseFilterEnum.Stop;
		CustomMinimumSize = new Vector2(Side, Side);
		Size = CustomMinimumSize;
		VisibilityChanged += () => { if (!IsVisibleInTree()) Release(); };
	}

	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventScreenTouch { Pressed: true } touch when finger < 0:
				finger = touch.Index;
				Follow(touch.Position);
				AcceptEvent();
				break;
			case InputEventScreenTouch { Pressed: false } up when up.Index == finger:
				Release();
				AcceptEvent();
				break;
			case InputEventScreenDrag drag when drag.Index == finger:
				Follow(drag.Position);
				AcceptEvent();
				break;
			// O toque também chega como clique emulado; se passasse, o chão sob o direcional receberia o mesmo toque.
			case InputEventMouse mouse when mouse.Device == InputEvent.DeviceIdEmulation:
				AcceptEvent();
				break;
		}
	}

	void Follow(Vector2 localPoint)
	{
		offset = localPoint - Center;
		var (x, y) = VirtualStick.Direction(offset.X, offset.Y, Radius);
		Press(Controls.Right, MathF.Max(0f, x));
		Press(Controls.Left, MathF.Max(0f, -x));
		Press(Controls.Up, MathF.Max(0f, y));
		Press(Controls.Down, MathF.Max(0f, -y));
		QueueRedraw();
	}

	void Release()
	{
		finger = -1;
		offset = Vector2.Zero;
		foreach (var action in new[] { Controls.Right, Controls.Left, Controls.Up, Controls.Down }) Input.ActionRelease(action);
		QueueRedraw();
	}

	static void Press(string action, float strength)
	{
		if (strength > 0.01f) Input.ActionPress(action, strength);
		else Input.ActionRelease(action);
	}

	Vector2 Center => Size / 2f;

	// Um círculo claro e um botão que segue o dedo; o contorno escuro garante que apareça sobre qualquer chão.
	public override void _Draw()
	{
		DrawCircle(Center, Radius, new Color(1f, 1f, 1f, 0.18f));
		DrawArc(Center, Radius, 0f, Mathf.Tau, 48, new Color(0.18f, 0.13f, 0.1f, 0.7f), 4f, antialiased: true);
		var knob = Center + offset.LimitLength(Radius);
		DrawCircle(knob, Radius * 0.42f, new Color(1f, 1f, 1f, 0.55f));
		DrawArc(knob, Radius * 0.42f, 0f, Mathf.Tau, 32, new Color(0.18f, 0.13f, 0.1f, 0.8f), 4f, antialiased: true);
	}
}
