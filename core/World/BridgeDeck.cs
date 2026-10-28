using System.Numerics;

namespace ArquipelagoDoCeu.World;

// O piso em arco de uma ponte ou corredor: cada peça cobre dois hexágonos, começa e termina rente ao chão e sobe no meio.
// O Personagem anda sobre essa altura, para os pés nunca afundarem no piso nem flutuarem sobre ele.
public static class BridgeDeck
{
	public const float HalfLength = 1.7275f;
	public const float HalfWidth = 0.72f;
	public const float Rise = 0.26f;
	// O guarda-corpo fica a 0,64 do centro: o corpo anda só até aqui, para não atravessá-lo.
	public const float WalkHalfWidth = 0.5f;

	// A altura do arco sob o ponto, acima do chão; 0 fora de qualquer peça. `Direction` é unitária e aponta ao longo da peça.
	public static float Height(IReadOnlyList<(Vector2 Center, Vector2 Direction)> pieces, Vector2 ground)
	{
		var best = 0f;
		foreach (var (center, direction) in pieces)
		{
			var offset = ground - center;
			var along = Vector2.Dot(offset, direction);
			var across = offset.X * -direction.Y + offset.Y * direction.X;
			if (MathF.Abs(along) > HalfLength || MathF.Abs(across) > HalfWidth) continue;
			var ratio = along / HalfLength;
			best = MathF.Max(best, Rise * (1f - ratio * ratio));
		}
		return best;
	}

	// Falso quando o ponto está ao lado do piso de uma ponte, onde fica o guarda-corpo.
	public static bool Allows(IReadOnlyList<(Vector2 Center, Vector2 Direction)> pieces, Vector2 ground)
	{
		foreach (var (center, direction) in pieces)
		{
			var offset = ground - center;
			var along = Vector2.Dot(offset, direction);
			var across = offset.X * -direction.Y + offset.Y * direction.X;
			if (MathF.Abs(along) <= HalfLength && MathF.Abs(across) > WalkHalfWidth) return false;
		}
		return true;
	}
}
