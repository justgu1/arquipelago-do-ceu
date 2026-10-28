using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// O ícone de um Ataque básico, em forma (nunca só cor): Precisa é um alvo, Diagonal é um risco inclinado, Carregado é um triângulo cheio.
// Com `Progress` abaixo de 1 mostra o anel da Recarga em volta; pronto, o anel fecha inteiro.
public partial class AttackIcon : Control
{
	static readonly Color Ink = Color.FromHtml("#3D2C25");
	static readonly Color Ring = Color.FromHtml("#946239");
	readonly BasicAttack attack;
	float progress = 1f;

	public AttackIcon(BasicAttack attack, int size = 44)
	{
		this.attack = attack;
		CustomMinimumSize = new Vector2(size, size);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void SetProgress(float value)
	{
		if (Mathf.IsEqualApprox(value, progress)) return;
		progress = value;
		QueueRedraw();
	}

	public override void _Draw()
	{
		var center = Size / 2f;
		var radius = Mathf.Min(Size.X, Size.Y) / 2f - 3f;
		DrawArc(center, radius, 0f, Mathf.Tau, 32, Ring with { A = 0.3f }, 3f);
		DrawArc(center, radius, -Mathf.Pi / 2f, -Mathf.Pi / 2f + Mathf.Tau * progress, 32, Ring, 4f);
		var inner = radius * 0.55f;
		switch (attack)
		{
			case BasicAttack.Precise:
				DrawArc(center, inner, 0f, Mathf.Tau, 24, Ink, 3f);
				DrawCircle(center, inner * 0.25f, Ink);
				break;
			case BasicAttack.Diagonal:
				DrawLine(center + new Vector2(-inner, inner), center + new Vector2(inner, -inner), Ink, 5f);
				break;
			default:
				DrawColoredPolygon([center + new Vector2(0f, -inner), center + new Vector2(inner, inner), center + new Vector2(-inner, inner)], Ink);
				break;
		}
	}
}
