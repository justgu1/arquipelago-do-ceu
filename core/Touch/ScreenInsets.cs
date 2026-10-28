namespace ArquipelagoDoCeu.Touch;

public sealed record Insets(float Left, float Top, float Right, float Bottom);

// As margens onde a interface não deve ficar: o recorte da câmera (área segura do sistema) e os cantos arredondados,
// que o sistema não informa e por isso entram como uma folga fixa em milímetros.
public static class ScreenInsets
{
	const float MillimetersPerInch = 25.4f;

	public static float CornerMargin(float dpi, float millimeters) => dpi <= 0 ? 0 : millimeters / MillimetersPerInch * dpi;

	public static Insets From(float screenW, float screenH, float safeX, float safeY, float safeW, float safeH, float cornerMargin)
	{
		if (safeW <= 0 || safeH <= 0) return new Insets(cornerMargin, cornerMargin, cornerMargin, cornerMargin);
		return new Insets(
			Math.Max(cornerMargin, safeX),
			Math.Max(cornerMargin, safeY),
			Math.Max(cornerMargin, screenW - (safeX + safeW)),
			Math.Max(cornerMargin, screenH - (safeY + safeH)));
	}
}
