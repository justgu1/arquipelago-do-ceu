using ArquipelagoDoCeu.Touch;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public sealed class ToqueTests
{
	// --- Direcional virtual: o deslocamento do dedo vira uma direção de 0 a 1, com Y para cima.

	[Fact]
	public void Dentro_da_zona_morta_o_dedo_nao_move_o_Personagem()
	{
		var (x, y) = VirtualStick.Direction(dx: 5, dy: 5, radius: 100);

		Assert.Equal(0, x);
		Assert.Equal(0, y);
	}

	[Fact]
	public void Dedo_no_raio_para_a_direita_anda_com_forca_total_para_a_direita()
	{
		var (x, y) = VirtualStick.Direction(dx: 100, dy: 0, radius: 100);

		Assert.Equal(1, x, precision: 3);
		Assert.Equal(0, y, precision: 3);
	}

	[Fact]
	public void Dedo_acima_do_centro_da_tela_anda_para_cima_com_Y_positivo()
	{
		var (_, y) = VirtualStick.Direction(dx: 0, dy: -100, radius: 100);

		Assert.Equal(1, y, precision: 3);
	}

	[Fact]
	public void Alem_do_raio_a_forca_para_em_um()
	{
		var (x, y) = VirtualStick.Direction(dx: 300, dy: 400, radius: 100);

		Assert.Equal(1, Math.Sqrt((x * x) + (y * y)), precision: 3);
	}

	[Fact]
	public void A_forca_cresce_com_a_distancia_depois_da_zona_morta()
	{
		var pertinho = VirtualStick.Direction(dx: 30, dy: 0, radius: 100).X;
		var meio = VirtualStick.Direction(dx: 60, dy: 0, radius: 100).X;

		Assert.True(pertinho > 0);
		Assert.True(meio > pertinho);
		Assert.True(meio < 1);
	}

	[Fact]
	public void A_diagonal_mantem_a_proporcao_entre_os_eixos()
	{
		var (x, y) = VirtualStick.Direction(dx: 80, dy: -80, radius: 100);

		Assert.Equal(x, y, precision: 3);
	}

	[Fact]
	public void Raio_zero_nao_divide_por_zero()
	{
		var (x, y) = VirtualStick.Direction(dx: 10, dy: 10, radius: 0);

		Assert.Equal(0, x);
		Assert.Equal(0, y);
	}

	// --- Escala da interface: a UI do Lobby usa 1 unidade = 1 pixel físico vezes a escala do texto, então o alvo mínimo
	// precisa ser medido em milímetros no aparelho. As telas foram desenhadas para 720 de altura: crescer além disso as corta.

	[Fact]
	public void Celular_de_480_dpi_e_1080_de_altura_cresce_ate_o_limite_da_altura()
	{
		Assert.Equal(1.5f, TouchScale.Effective(dpi: 480, textScale: 1f, windowHeightPx: 1080));
	}

	[Fact]
	public void Janela_de_720_de_altura_nao_cresce_porque_as_telas_ja_enchem_a_altura()
	{
		Assert.Equal(1f, TouchScale.Effective(dpi: 480, textScale: 1f, windowHeightPx: 720));
	}

	[Fact]
	public void Tablet_de_264_dpi_cresce_so_o_que_o_dedo_pede()
	{
		var tablet = TouchScale.Effective(dpi: 264, textScale: 1f, windowHeightPx: 1640);

		Assert.True(tablet > 1.5f && tablet < 1.8f);
	}

	[Fact]
	public void Monitor_de_PC_mantem_a_escala_do_texto()
	{
		Assert.Equal(1f, TouchScale.Effective(dpi: 96, textScale: 1f, windowHeightPx: 1080));
		Assert.Equal(1.3f, TouchScale.Effective(dpi: 96, textScale: 1.3f, windowHeightPx: 1080));
	}

	[Fact]
	public void Texto_maior_escolhido_pela_crianca_vale_quando_passa_do_minimo_do_aparelho()
	{
		Assert.Equal(2.6f, TouchScale.Effective(dpi: 480, textScale: 2.6f, windowHeightPx: 1080));
	}

	[Fact]
	public void Sem_saber_a_densidade_ou_a_altura_vale_a_escala_do_texto()
	{
		Assert.Equal(1.2f, TouchScale.Effective(dpi: 0, textScale: 1.2f, windowHeightPx: 1080));
		Assert.Equal(1.2f, TouchScale.Effective(dpi: 480, textScale: 1.2f, windowHeightPx: 0));
	}

	// --- Margens da tela: cantos arredondados e recortes da câmera.

	[Fact]
	public void Sem_recorte_as_margens_sao_so_a_folga_dos_cantos()
	{
		var margens = ScreenInsets.From(2340, 1080, safeX: 0, safeY: 0, safeW: 2340, safeH: 1080, cornerMargin: 60);

		Assert.Equal(new Insets(60, 60, 60, 60), margens);
	}

	[Fact]
	public void O_recorte_da_camera_a_esquerda_vence_a_folga_quando_e_maior()
	{
		var margens = ScreenInsets.From(2340, 1080, safeX: 110, safeY: 0, safeW: 2230, safeH: 1080, cornerMargin: 60);

		Assert.Equal(110, margens.Left);
		Assert.Equal(60, margens.Right);
	}

	[Fact]
	public void Area_segura_invalida_cai_na_folga_dos_cantos()
	{
		var margens = ScreenInsets.From(2340, 1080, safeX: 0, safeY: 0, safeW: 0, safeH: 0, cornerMargin: 60);

		Assert.Equal(new Insets(60, 60, 60, 60), margens);
	}

	[Fact]
	public void A_folga_dos_cantos_em_milimetros_vira_pixels_pela_densidade()
	{
		Assert.Equal(94.5f, ScreenInsets.CornerMargin(dpi: 480, millimeters: 5), 0.5f);
		Assert.Equal(0f, ScreenInsets.CornerMargin(dpi: 0, millimeters: 5));
	}
}
