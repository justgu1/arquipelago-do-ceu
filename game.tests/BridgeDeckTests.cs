using System.Numerics;
using ArquipelagoDoCeu.World;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public sealed class BridgeDeckTests
{
	static readonly (Vector2 Center, Vector2 Direction)[] Piece = [(new Vector2(10f, 4f), new Vector2(1f, 0f))];

	[Fact]
	public void No_meio_do_piso_a_altura_e_a_do_arco()
	{
		Assert.Equal(BridgeDeck.Rise, BridgeDeck.Height(Piece, new Vector2(10f, 4f)), 3);
	}

	[Fact]
	public void Nas_pontas_o_piso_esta_rente_ao_chao()
	{
		Assert.Equal(0f, BridgeDeck.Height(Piece, new Vector2(10f + BridgeDeck.HalfLength, 4f)), 3);
		Assert.Equal(0f, BridgeDeck.Height(Piece, new Vector2(10f - BridgeDeck.HalfLength, 4f)), 3);
	}

	[Fact]
	public void A_altura_sobe_devagar_da_ponta_ao_meio()
	{
		var quarter = BridgeDeck.Height(Piece, new Vector2(10f + BridgeDeck.HalfLength / 2f, 4f));

		Assert.InRange(quarter, 0.01f, BridgeDeck.Rise - 0.01f);
	}

	[Fact]
	public void Fora_da_largura_ou_do_comprimento_o_chao_e_plano()
	{
		Assert.Equal(0f, BridgeDeck.Height(Piece, new Vector2(10f, 4f + BridgeDeck.HalfWidth + 0.2f)));
		Assert.Equal(0f, BridgeDeck.Height(Piece, new Vector2(10f + BridgeDeck.HalfLength + 0.2f, 4f)));
		Assert.Equal(0f, BridgeDeck.Height([], new Vector2(0f, 0f)));
	}

	[Fact]
	public void A_peca_girada_vale_na_direcao_dela()
	{
		(Vector2, Vector2)[] diagonal = [(new Vector2(0f, 0f), Vector2.Normalize(new Vector2(1f, 1f)))];

		Assert.Equal(BridgeDeck.Rise, BridgeDeck.Height(diagonal, new Vector2(0f, 0f)), 3);
		Assert.True(BridgeDeck.Height(diagonal, new Vector2(0.5f, 0.5f)) > 0f);
		Assert.Equal(0f, BridgeDeck.Height(diagonal, new Vector2(1f, -1f)));
	}

	[Fact]
	public void Quem_anda_na_ponte_nao_atravessa_o_guarda_corpo()
	{
		Assert.True(BridgeDeck.Allows(Piece, new Vector2(10f, 4f + BridgeDeck.WalkHalfWidth - 0.01f)));
		Assert.False(BridgeDeck.Allows(Piece, new Vector2(10f, 4f + BridgeDeck.WalkHalfWidth + 0.05f)));
		Assert.False(BridgeDeck.Allows(Piece, new Vector2(10f - 1f, 4f - BridgeDeck.HalfWidth)));
	}

	[Fact]
	public void Fora_do_comprimento_da_ponte_tudo_e_permitido()
	{
		Assert.True(BridgeDeck.Allows(Piece, new Vector2(10f + BridgeDeck.HalfLength + 0.1f, 4f + 1f)));
		Assert.True(BridgeDeck.Allows([], new Vector2(0f, 0f)));
	}
}
