namespace ArquipelagoDoCeu.Performance;

public enum QualityTier { Low, Medium, High }

// O que cada perfil muda. Rosto, silhueta e geometria visível não entram aqui: só sombra, antisserrilhado e resolução.
public sealed record QualitySettings(bool Shadows, int Msaa, float RenderScale);

public static class Quality
{
	// Só a memória decide o ponto de partida; o benchmark desce de perfil se o aparelho não chegar na meta.
	public static QualityTier Initial(int ramMb) => ramMb switch
	{
		<= 0 => QualityTier.Medium,
		>= 6000 => QualityTier.High,
		>= 3500 => QualityTier.Medium,
		_ => QualityTier.Low,
	};

	public static QualitySettings For(QualityTier tier) => tier switch
	{
		QualityTier.High => new QualitySettings(Shadows: true, Msaa: 2, RenderScale: 1f),
		QualityTier.Medium => new QualitySettings(Shadows: true, Msaa: 0, RenderScale: 0.85f),
		_ => new QualitySettings(Shadows: false, Msaa: 0, RenderScale: 0.75f),
	};

	public static QualityTier? Lower(QualityTier tier) => tier switch
	{
		QualityTier.High => QualityTier.Medium,
		QualityTier.Medium => QualityTier.Low,
		_ => null,
	};
}
