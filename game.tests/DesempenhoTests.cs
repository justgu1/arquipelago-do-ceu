using System.Text.Json;
using ArquipelagoDoCeu.Performance;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public sealed class DesempenhoTests
{
	static double[] Quadros(int quantos, double ms) => Enumerable.Repeat(ms, quantos).ToArray();

	// --- Estatística de quadros

	[Fact]
	public void Sem_quadros_nao_ha_fps_e_nao_passa_da_meta()
	{
		var stats = FrameStats.From([]);

		Assert.Equal(0, stats.Count);
		Assert.Equal(0, stats.AverageFps);
		Assert.False(stats.MeetsTarget);
	}

	[Fact]
	public void Quadros_de_16_e_meio_milissegundos_dao_60_fps_e_passam_da_meta()
	{
		var stats = FrameStats.From(Quadros(300, 1000.0 / 60));

		Assert.Equal(60, stats.AverageFps, precision: 3);
		Assert.Equal(60, stats.OnePercentLowFps, precision: 3);
		Assert.True(stats.MeetsTarget);
	}

	[Fact]
	public void Um_quadro_lento_em_cem_derruba_o_um_por_cento_pior_mas_nao_a_media()
	{
		var quadros = Quadros(99, 1000.0 / 60).Append(100).ToArray();

		var stats = FrameStats.From(quadros);

		Assert.Equal(10, stats.OnePercentLowFps, precision: 3);
		Assert.True(stats.AverageFps > 50);
		Assert.False(stats.MeetsTarget);
	}

	[Fact]
	public void O_um_por_cento_pior_usa_ao_menos_um_quadro_mesmo_com_poucos_quadros()
	{
		var stats = FrameStats.From([10, 10, 40]);

		Assert.Equal(25, stats.OnePercentLowFps, precision: 3);
		Assert.Equal(40, stats.WorstFrameMs);
	}

	[Fact]
	public void Media_de_45_e_pior_de_30_nao_passam_a_meta_de_60_e_45()
	{
		var stats = FrameStats.From(Quadros(200, 1000.0 / 45));

		Assert.False(stats.MeetsTarget);
	}

	[Fact]
	public void A_meta_e_60_de_media_e_45_no_um_por_cento_pior()
	{
		Assert.Equal(60, FrameStats.TargetAverageFps);
		Assert.Equal(45, FrameStats.TargetOnePercentLowFps);
	}

	// --- Perfis de qualidade

	[Theory]
	[InlineData(7000, QualityTier.High)]
	[InlineData(6000, QualityTier.High)]
	[InlineData(5999, QualityTier.Medium)]
	[InlineData(3500, QualityTier.Medium)]
	[InlineData(3499, QualityTier.Low)]
	[InlineData(2000, QualityTier.Low)]
	public void O_perfil_inicial_vem_da_memoria_do_aparelho(int ramMb, QualityTier esperado)
	{
		Assert.Equal(esperado, Quality.Initial(ramMb));
	}

	[Fact]
	public void Sem_saber_a_memoria_o_perfil_e_o_medio()
	{
		Assert.Equal(QualityTier.Medium, Quality.Initial(0));
	}

	[Fact]
	public void Cada_perfil_mais_baixo_tem_no_maximo_o_mesmo_de_tudo_e_menos_em_algo()
	{
		var alto = Quality.For(QualityTier.High);
		var medio = Quality.For(QualityTier.Medium);
		var baixo = Quality.For(QualityTier.Low);

		Assert.True(alto.Msaa >= medio.Msaa && medio.Msaa >= baixo.Msaa);
		Assert.True(alto.RenderScale >= medio.RenderScale && medio.RenderScale >= baixo.RenderScale);
		Assert.True(alto.Shadows || !medio.Shadows);
		Assert.True(medio.Shadows || !baixo.Shadows);
		Assert.NotEqual(alto, medio);
		Assert.NotEqual(medio, baixo);
	}

	[Fact]
	public void O_perfil_alto_nao_reduz_a_resolucao()
	{
		Assert.Equal(1f, Quality.For(QualityTier.High).RenderScale);
	}

	[Fact]
	public void Nenhum_perfil_desce_abaixo_de_70_por_cento_da_resolucao()
	{
		Assert.All(Enum.GetValues<QualityTier>(), t => Assert.True(Quality.For(t).RenderScale >= 0.7f));
	}

	[Fact]
	public void Descer_de_perfil_vai_do_alto_ao_medio_ao_baixo_e_para()
	{
		Assert.Equal(QualityTier.Medium, Quality.Lower(QualityTier.High));
		Assert.Equal(QualityTier.Low, Quality.Lower(QualityTier.Medium));
		Assert.Null(Quality.Lower(QualityTier.Low));
	}

	// --- Relatório do benchmark

	static BenchmarkRun Relatorio() => new(
		DeviceModel: "SM-S911B", Gpu: "Adreno (TM) 740", Android: "16", RamMb: 7072, GameVersion: "0.1.0",
		Tier: QualityTier.High,
		Scenes: [new("lobby", FrameStats.From(Quadros(120, 1000.0 / 60))), new("sala-mapa", FrameStats.From(Quadros(120, 1000.0 / 50)))]);

	[Fact]
	public void O_evento_do_benchmark_leva_o_aparelho_o_perfil_e_cada_cena()
	{
		using var json = JsonDocument.Parse(Relatorio().ToJson());
		var raiz = json.RootElement;

		Assert.Equal("benchmark_run", raiz.GetProperty("type").GetString());
		Assert.Equal("SM-S911B", raiz.GetProperty("device").GetString());
		Assert.Equal("high", raiz.GetProperty("tier").GetString());
		Assert.Equal(2, raiz.GetProperty("scenes").GetArrayLength());
		var primeira = raiz.GetProperty("scenes")[0];
		Assert.Equal("lobby", primeira.GetProperty("name").GetString());
		Assert.Equal(60, primeira.GetProperty("avg_fps").GetDouble(), precision: 1);
		Assert.True(primeira.GetProperty("meets_target").GetBoolean());
	}

	[Fact]
	public void O_evento_do_benchmark_nunca_leva_perfil_nem_apelido()
	{
		var texto = Relatorio().ToJson();

		Assert.DoesNotContain("profile", texto, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("nickname", texto, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void O_relatorio_so_passa_se_todas_as_cenas_passam()
	{
		Assert.False(Relatorio().MeetsTarget);
		var boa = Relatorio() with { Scenes = [new("lobby", FrameStats.From(Quadros(120, 1000.0 / 60)))] };
		Assert.True(boa.MeetsTarget);
	}

	[Fact]
	public void O_texto_para_copiar_traz_o_aparelho_e_uma_linha_por_cena()
	{
		var linhas = Relatorio().ToText().Split('\n', StringSplitOptions.RemoveEmptyEntries);

		Assert.Contains("SM-S911B", linhas[0]);
		Assert.Contains(linhas, l => l.StartsWith("lobby") && l.Contains("60"));
		Assert.Contains(linhas, l => l.StartsWith("sala-mapa") && l.Contains("50"));
	}
}

public sealed class DesempenhoRenderTests
{
	static readonly RenderInfo Info = new(DrawCalls: 420, Primitives: 180_000, ProcessMs: 9.5, PhysicsMs: 1.2, VideoMemoryMb: 310.5);

	static BenchmarkRun Relatorio() => new("SM-S911B", "Adreno (TM) 740", "16", 7072, "0.1.0", QualityTier.High,
		[new SceneResult("lobby", FrameStats.From(Enumerable.Repeat(1000.0 / 60, 120).ToArray()), Info)]);

	[Fact]
	public void A_cena_com_dados_de_render_leva_chamadas_triangulos_tempo_e_memoria_de_video_no_evento()
	{
		using var json = System.Text.Json.JsonDocument.Parse(Relatorio().ToJson());
		var cena = json.RootElement.GetProperty("scenes")[0];

		Assert.Equal(420, cena.GetProperty("draw_calls").GetInt32());
		Assert.Equal(180_000, cena.GetProperty("primitives").GetInt64());
		Assert.Equal(9.5, cena.GetProperty("process_ms").GetDouble(), precision: 1);
		Assert.Equal(310.5, cena.GetProperty("vram_mb").GetDouble(), precision: 1);
	}

	[Fact]
	public void Cena_sem_dados_de_render_nao_inventa_numeros()
	{
		var run = Relatorio() with { Scenes = [new SceneResult("lobby", FrameStats.From([16.0]))] };

		using var json = System.Text.Json.JsonDocument.Parse(run.ToJson());
		Assert.False(json.RootElement.GetProperty("scenes")[0].TryGetProperty("draw_calls", out _));
	}

	[Fact]
	public void O_texto_para_copiar_mostra_as_chamadas_de_desenho_e_os_triangulos()
	{
		var linha = Relatorio().ToText().Split('\n').First(l => l.StartsWith("lobby"));

		Assert.Contains("420", linha);
		Assert.Contains("180", linha);
	}

	[Fact]
	public void A_memoria_de_video_media_de_varias_amostras_e_a_media_das_amostras()
	{
		var media = RenderInfo.Average([new(100, 1000, 4, 1, 100), new(300, 3000, 8, 3, 200)])!;

		Assert.Equal(200, media.DrawCalls);
		Assert.Equal(2000, media.Primitives);
		Assert.Equal(6, media.ProcessMs, precision: 3);
		Assert.Equal(2, media.PhysicsMs, precision: 3);
		Assert.Equal(150, media.VideoMemoryMb, precision: 3);
	}

	[Fact]
	public void Sem_amostras_nao_ha_dados_de_render()
	{
		Assert.Null(RenderInfo.Average([]));
	}
}
