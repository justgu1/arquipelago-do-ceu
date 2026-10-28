namespace ArquipelagoDoCeu.Touch;

// O direcional virtual do toque: o deslocamento do dedo vira uma direção de força 0 a 1, com Y para cima como no teclado.
public static class VirtualStick
{
	// Uma folga no centro evita que o tremor do dedo parado faça o Personagem andar.
	const float DeadZone = 0.2f;

	public static (float X, float Y) Direction(float dx, float dy, float radius)
	{
		var length = MathF.Sqrt((dx * dx) + (dy * dy));
		var dead = radius * DeadZone;
		if (radius <= 0 || length <= dead) return (0, 0);
		var strength = MathF.Min(1f, (length - dead) / (radius - dead));
		return (dx / length * strength, -dy / length * strength);
	}
}
