namespace ArquipelagoDoCeu.Touch;

// Quanto a interface precisa crescer para o alvo de toque caber no dedo. A interface usa 1 unidade = 1 pixel físico
// vezes a escala do texto; num celular de 480 dpi o botão mínimo de 44 unidades fica com 2,3 mm, e um dedo pede uns 7.
public static class TouchScale
{
	// As telas foram desenhadas para 720 de altura; numa janela de 1080 px, crescer além de 1,5 as corta embaixo.
	const float DesignHeight = 720f;
	const float MinTouchUnits = 44f;
	const float TargetMillimeters = 7f;
	const float MillimetersPerInch = 25.4f;

	// A escala que vale: a do texto que a criança escolheu ou a mínima do aparelho, a que for maior.
	public static float Effective(float dpi, float textScale, float windowHeightPx)
	{
		if (dpi <= 0 || windowHeightPx <= 0) return textScale;
		var millimeters = MinTouchUnits / dpi * MillimetersPerInch;
		var limit = Math.Max(1f, windowHeightPx / DesignHeight);
		return Math.Max(textScale, Math.Min(limit, Math.Max(1f, TargetMillimeters / millimeters)));
	}
}
