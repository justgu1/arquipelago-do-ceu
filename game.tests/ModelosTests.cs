using System.Text.RegularExpressions;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

// Os modelos do jogo saem de scripts do Blender (docs/agents/arte/pipeline-de-arte.md): nenhum modelo entra à mão nem vem de gerador.
public class ModelosTests
{
	static readonly string[] ForaDoExportador = ["criaturas/", "personagem/", "guia.glb", "boneco.glb"];

	static string Modelos => Path.Combine(ArquiteturaTests.RaizDoRepositorio(), "game", "assets", "modelos");

	static IEnumerable<string> Glbs() =>
		Directory.EnumerateFiles(Modelos, "*.glb", SearchOption.AllDirectories)
			.Select(f => Path.GetRelativePath(Modelos, f).Replace(Path.DirectorySeparatorChar, '/'));

	static List<string> Exportados()
	{
		var exportador = File.ReadAllText(Path.Combine(ArquiteturaTests.RaizDoRepositorio(), "tools", "blender", "exportar_lobby.py"));
		return Regex.Matches(exportador, @"""([^""]+)"": \(\(").Select(m => m.Groups[1].Value + ".glb").ToList();
	}

	// Textura solta ao lado do GLB (`<nome>_0.jpg`) é sinal de modelo de gerador: os do Blender levam só a cor do material.
	[Fact]
	public void Nenhum_modelo_tem_textura_solta_de_gerador()
	{
		var soltas = Directory.EnumerateFiles(Modelos, "*_0.jpg", SearchOption.AllDirectories).ToList();

		Assert.Empty(soltas);
	}

	[Fact]
	public void Todo_modelo_do_cenario_dos_itens_e_do_lobby_sai_do_exportador()
	{
		var listados = Exportados().ToHashSet();

		var fora = Glbs().Where(g => !ForaDoExportador.Any(g.StartsWith) && !listados.Contains(g)).ToList();

		Assert.Empty(fora);
	}

	[Fact]
	public void Todo_modelo_do_exportador_tem_o_arquivo_no_jogo()
	{
		var faltando = Exportados().Where(g => !File.Exists(Path.Combine(Modelos, g))).ToList();

		Assert.Empty(faltando);
	}

	// O tema da torre (DungeonTheme) troca a cor dos materiais do kit pelo nome: se o nome mudar no Blender, o tema deixa de pintar a peça sem avisar.
	[Theory]
	[InlineData("cenario/piso-dungeon-a.glb", "pedra", "pedra_cl", "faixa")]
	[InlineData("cenario/piso-dungeon-b.glb", "pedra", "pedra_cl", "faixa")]
	[InlineData("cenario/parede-baixa.glb", "pedra", "pedra_cl", "pedra_esc", "faixa")]
	[InlineData("cenario/coluna-dungeon.glb", "pedra", "pedra_cl", "pedra_esc", "faixa")]
	[InlineData("cenario/coluna-braseiro.glb", "pedra", "pedra_esc", "faixa", "chama")]
	public void O_kit_de_dungeon_traz_os_materiais_que_o_tema_pinta(string modelo, params string[] materiais)
	{
		var texto = System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(Path.Combine(Modelos, modelo)));

		var faltando = materiais.Where(m => !Regex.IsMatch(texto, @"""name""\s*:\s*""" + m + @"""")).ToList();

		Assert.Empty(faltando);
	}
}
