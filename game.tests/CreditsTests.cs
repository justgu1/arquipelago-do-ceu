using ArquipelagoDoCeu.Data;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class CreditsTests
{
	static string GameFolder => Path.Combine(MissionFiles.RepositoryRoot(), "game");

	static Credits Real => Credits.Parse(File.ReadAllText(Path.Combine(GameFolder, "data", "creditos.json"))).Value!;

	const string Valid = """
		{
		  "team": ["Ana", "Bia"],
		  "components": [
		    { "name": "Motor", "author": "Fulano", "license": "MIT", "file": "licencas/motor.txt" },
		    { "name": "Fonte", "license": "OFL 1.1" }
		  ]
		}
		""";

	[Fact]
	public void Le_a_equipe_e_os_componentes_na_ordem_dos_dados()
	{
		var credits = Credits.Parse(Valid).Value!;

		Assert.Equal(["Ana", "Bia"], credits.Team);
		Assert.Equal(["Motor", "Fonte"], credits.Components.Select(c => c.Name));
		Assert.Equal("licencas/motor.txt", credits.Components[0].File);
		Assert.Null(credits.Components[1].File);
	}

	[Fact]
	public void Os_creditos_do_jogo_trazem_a_equipe_o_motor_e_a_fonte()
	{
		Assert.Equal(["Guilherme Santos", "Rudan Borges"], Real.Team);
		Assert.Contains(Real.Components, c => c.Name.StartsWith("Godot", StringComparison.Ordinal));
		Assert.Contains(Real.Components, c => c.Name.StartsWith("OpenDyslexic", StringComparison.Ordinal));
	}

	[Fact]
	public void Todo_arquivo_de_licenca_citado_existe_no_jogo()
	{
		foreach (var file in Real.Components.Select(c => c.File).OfType<string>())
			Assert.True(File.Exists(Path.Combine(GameFolder, file)), $"Falta {file}.");
	}

	[Theory]
	[InlineData("{")]
	[InlineData("""{"team":[],"components":[{"name":"A","license":"MIT"}]}""")]
	[InlineData("""{"team":[" "],"components":[{"name":"A","license":"MIT"}]}""")]
	[InlineData("""{"team":["Ana"],"components":[]}""")]
	[InlineData("""{"team":["Ana"],"components":[{"name":"A"}]}""")]
	[InlineData("""{"team":["Ana"],"components":[{"name":" ","license":"MIT"}]}""")]
	[InlineData("""{"team":["Ana"],"components":[{"name":"A","license":"MIT"},{"name":"A","license":"MIT"}]}""")]
	public void Recusa_creditos_incompletos(string json)
	{
		var result = Credits.Parse(json);

		Assert.False(result.IsOk);
		Assert.False(string.IsNullOrWhiteSpace(result.Error));
	}

	// O arquivo vem de dados e é aberto pelo jogo: só vale um .txt dentro da pasta de licenças.
	[Theory]
	[InlineData("../save.json")]
	[InlineData("licencas/../data/rules.json")]
	[InlineData("licencas/motor.json")]
	[InlineData("/licencas/motor.txt")]
	public void Recusa_arquivo_de_licenca_fora_da_pasta(string file)
	{
		var json = $$"""{"team":["Ana"],"components":[{"name":"A","license":"MIT","file":"{{file}}"}]}""";

		Assert.False(Credits.Parse(json).IsOk);
	}
}
