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

	// Troca a cor dos materiais do modelo pelas do tema; `trim` pinta a faixa (o chão passa a cor da função da Sala).
	public void Tint(Node3D model, Color? trim = null)
	{
		foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
		{
			var mesh = (MeshInstance3D)node;
			if (mesh.Mesh is null) continue;
			for (var surface = 0; surface < mesh.Mesh.GetSurfaceCount(); surface++)
				if (mesh.Mesh.SurfaceGetMaterial(surface) is StandardMaterial3D source && Colored(source, trim ?? Color.FromHtml(Trim)) is { } material)
					mesh.SetSurfaceOverrideMaterial(surface, material);
		}
	}

	Material? Colored(StandardMaterial3D source, Color trim)
	{
		var color = source.ResourceName switch
		{
			"pedra" => Color.FromHtml(Stone),
			"pedra_cl" => Color.FromHtml(StoneLight),
			"pedra_esc" => Color.FromHtml(StoneDark),
			"faixa" => trim,
			"chama" => Color.FromHtml(Flame),
			_ => (Color?)null,
		};
		if (color is not { } tint) return null;
		var key = $"{Void}|{source.ResourceName}|{tint.ToHtml()}";
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
