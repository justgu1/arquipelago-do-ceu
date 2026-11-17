using Godot;

namespace ArquipelagoDoCeu;

// A folha de papiro das telas e do HUD, gerada por código (ruído de manchas e fibras, borda queimada e irregular): não depende de arquivo de imagem.
public static class PaperTexture
{
	const int Size = 256;
	const int Edge = 14;
	static StyleBoxTexture? shared;

	public static StyleBoxTexture Style()
	{
		if (shared is not null && GodotObject.IsInstanceValid(shared)) return (StyleBoxTexture)shared.Duplicate();
		shared = new StyleBoxTexture { Texture = ImageTexture.CreateFromImage(Build()) };
		shared.SetTextureMarginAll(Edge);
		shared.SetContentMarginAll(12);
		shared.AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Tile;
		shared.AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Tile;
		return (StyleBoxTexture)shared.Duplicate();
	}

	static Image Build()
	{
		var blotch = new FastNoiseLite { Frequency = 0.012f, Seed = 3, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
		var stain = new FastNoiseLite { Frequency = 0.03f, Seed = 21, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
		var fibre = new FastNoiseLite { Frequency = 0.09f, Seed = 11, NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular };
		var rough = new FastNoiseLite { Frequency = 0.4f, Seed = 5 };
		var torn = new FastNoiseLite { Frequency = 0.16f, Seed = 8, NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth };
		var light = Color.FromHtml("#F2E4C0");
		var dark = Color.FromHtml("#D9BE8A");
		var aged = Color.FromHtml("#B98F58");
		var image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
		for (var y = 0; y < Size; y++)
			for (var x = 0; x < Size; x++)
			{
				var t = 0.5f + 0.5f * blotch.GetNoise2D(x, y);
				t = Mathf.Clamp(t * 0.7f + 0.25f * (0.5f + 0.5f * fibre.GetNoise2D(x * 0.5f, y * 2.2f)) + 0.05f * rough.GetNoise2D(x, y), 0f, 1f);
				var color = dark.Lerp(light, t);
				// Manchas de tempo pelo miolo, e a borda mais velha, mas só um tom: nada de moldura escura.
				color = color.Lerp(aged, Mathf.Clamp(0.5f + 0.5f * stain.GetNoise2D(x, y), 0f, 1f) * 0.22f);
				var fromEdge = Mathf.Min(Mathf.Min(x, Size - 1 - x), Mathf.Min(y, Size - 1 - y));
				var wear = Mathf.Clamp(1f - (fromEdge + 4f * torn.GetNoise2D(x, y)) / Edge, 0f, 1f);
				color = color.Lerp(aged, wear * 0.55f);
				// A beirada é rasgada: o contorno oscila e some aos poucos, em vez de uma linha reta.
				var cut = fromEdge + 5f * torn.GetNoise2D(x * 1.7f, y * 1.7f) + 2f * rough.GetNoise2D(x * 2f, y * 2f);
				color.A = Mathf.Clamp((cut - 1.5f) / 3f, 0f, 1f);
				image.SetPixel(x, y, color);
			}
		return image;
	}
}
