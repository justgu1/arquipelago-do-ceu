using System.Collections.Generic;
using Godot;

namespace ArquipelagoDoCeu;

// A ambientação de uma torre, separada da estrutura do Andar: o tema só troca a cor dos materiais de nome fixo do kit
// (pedra, pedra_cl, pedra_esc, faixa, chama), o fundo e a luz. A cor da função da Sala entra na `faixa` do chão.
public sealed record DungeonTheme(string Void, string Ambient, string Sun, string Stone, string StoneLight, string StoneDark, string Trim, string Flame, string Dust)
{
	// Torre dos Números: oficina de latão sob um céu roxo.
	public static readonly DungeonTheme Numbers = new("#1B1530", "#C9BFDC", "#FFE4BC", "#9A8FB8", "#B9AEDB", "#655C85", "#EBC24E", "#FFB347", "#FFE9A8");

	// Torre das Letras: biblioteca de arenito sob um céu verde-azulado.
	public static readonly DungeonTheme Letters = new("#12252C", "#CFE3E0", "#FFF0D2", "#A89F88", "#C9C0A8", "#6F6855", "#5BB6E4", "#7FE3D2", "#CFF5EE");

	public static DungeonTheme For(string discipline) => discipline == "matematica" ? Numbers : Letters;

	static readonly Dictionary<string, Material> Cache = [];

	// A pedra lisa de uma laje feita por código (o piso dos corredores).
	public Material StoneMaterial()
	{
		var key = $"laje|{Stone}";
		if (Cache.TryGetValue(key, out var cached) && GodotObject.IsInstanceValid(cached)) return cached;
		var material = new StandardMaterial3D { AlbedoColor = Color.FromHtml(StoneLight).Lerp(Color.FromHtml(Stone), 0.5f), Roughness = 0.9f };
		Cache[key] = material;
		return material;
	}

	// Troca a cor dos materiais do modelo pelas do tema; `trim` pinta a faixa e `wash` (a cor da função da Sala) tinge de leve a pedra do chão.
	// `tone` clareia ou escurece a pedra, para as lajes não saírem todas iguais.
	public void Tint(Node3D model, Color? trim = null, Color? wash = null, float tone = 1f)
	{
		foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
		{
			var mesh = (MeshInstance3D)node;
			if (mesh.Mesh is null) continue;
			for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
				if (mesh.Mesh.SurfaceGetMaterial(surface) is StandardMaterial3D source && Colored(source, trim ?? Color.FromHtml(Trim), wash, tone) is { } material)
					mesh.SetSurfaceOverrideMaterial(surface, material);
		}
	}

	Color Washed(string html, Color? wash, float tone)
	{
		var stone = Color.FromHtml(html);
		if (wash is { } hue) stone = stone.Lerp(hue, 0.22f);
		return new Color(stone.R * tone, stone.G * tone, stone.B * tone);
	}

	Material? Colored(StandardMaterial3D source, Color trim, Color? wash, float tone)
	{
		var color = source.ResourceName switch
		{
			"pedra" => Washed(Stone, wash, tone),
			"pedra_cl" => Washed(StoneLight, wash, tone),
			"pedra_esc" => Washed(StoneDark, wash, tone),
			"faixa" => trim,
			"chama" => Color.FromHtml(Flame),
			_ => (Color?)null,
		};
		if (color is not { } tint) return null;
		var key = $"{Void}|{source.ResourceName}|{tint.ToHtml()}|{trim.ToHtml()}";
		if (Cache.TryGetValue(key, out var cached) && GodotObject.IsInstanceValid(cached)) return cached;
		var copy = (StandardMaterial3D)source.Duplicate();
		copy.AlbedoColor = tint;
		if (source.ResourceName == "chama")
		{
			copy.EmissionEnabled = true;
			copy.Emission = tint;
			copy.EmissionEnergyMultiplier = 2.5f;
		}
		Cache[key] = copy;
		return copy;
	}
}
