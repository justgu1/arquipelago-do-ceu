using System.Numerics;
using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.Rules;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class CreatureAttacksTests
{
	static readonly CreatureRules Rules = CreatureRules.Default;
	static readonly Vector2 Creature = Vector2.Zero;

	static CreatureAttacks Start(ulong seed = 1) => new(Rules, new Pcg32(seed));

	// Avança o relógio em passos de 0,1 s até o evento pedido; devolve o tempo gasto.
	static float Until(CreatureAttacks attacks, CreatureAttackEvent wanted, Func<float, Vector2> player, float tiredness = 0f)
	{
		var t = 0f;
		for (var i = 0; i < 4000; i++)
		{
			t += 0.1f;
			if (attacks.Update(0.1f, Creature, player(t), tiredness) == wanted) return t;
		}
		throw new Xunit.Sdk.XunitException($"O evento {wanted} não aconteceu.");
	}

	static Func<float, Vector2> Still(Vector2 at) => _ => at;

	[Fact]
	public void O_golpe_so_sai_depois_de_um_aviso_e_o_primeiro_espera_um_respiro()
	{
		var attacks = Start();
		var player = Still(new Vector2(5, 0));

		var warned = Until(attacks, CreatureAttackEvent.Warned, player);
		var warning = Rules.Attacks[(int)attacks.Current].WarningSeconds;
		Assert.Equal(CreatureAttackPhase.Warning, attacks.Phase);
		Assert.InRange(warned, Rules.MinGapSeconds - 0.15f, Rules.MinGapSeconds + 0.15f);

		var struck = Until(attacks, CreatureAttackEvent.Missed, Still(new Vector2(5, 20)));
		Assert.InRange(struck, warning - 0.15f, warning + 0.15f);
	}

	[Fact]
	public void O_aviso_trava_a_mira_e_sair_da_area_a_tempo_desvia()
	{
		var attacks = Start();
		Until(attacks, CreatureAttackEvent.Warned, Still(new Vector2(5, 0)));
		var aim = attacks.Aim;

		var result = Until(attacks, CreatureAttackEvent.Missed, Still(new Vector2(40, 40)));

		Assert.True(result > 0f);
		Assert.Equal(new Vector2(5, 0), aim);
		Assert.Equal(CreatureAttackPhase.Waiting, attacks.Phase);
	}

	[Theory]
	[InlineData(CreatureAttackKind.Lunge, 4, 0, true)]
	[InlineData(CreatureAttackKind.Lunge, 4, 3, false)]
	[InlineData(CreatureAttackKind.Slam, 5.5f, 0.5f, true)]
	[InlineData(CreatureAttackKind.Slam, 5.5f, 4, false)]
	[InlineData(CreatureAttackKind.Spin, 2, 0, true)]
	[InlineData(CreatureAttackKind.Spin, 6, 0, false)]
	public void Cada_ataque_alcanca_a_sua_area(CreatureAttackKind kind, float x, float y, bool hit)
	{
		var attacks = Warning(kind);

		Assert.Equal(hit, attacks.Covers(Creature, new Vector2(x, y)));
	}

	// Uma criatura no aviso do ataque pedido: o primeiro golpe é sempre a Investida (os outros ainda recarregam), então se esperam os seguintes.
	static CreatureAttacks Warning(CreatureAttackKind kind)
	{
		var attacks = Start();
		for (var i = 0; i < 30; i++)
		{
			Until(attacks, CreatureAttackEvent.Warned, Still(new Vector2(5, 0)));
			if (attacks.Current == kind) return attacks;
			Until(attacks, CreatureAttackEvent.Missed, Still(new Vector2(40, 40)));
		}
		throw new Xunit.Sdk.XunitException($"A criatura não usou {kind}.");
	}

	[Fact]
	public void A_criatura_nao_repete_o_ataque_enquanto_ha_outro_pronto()
	{
		var attacks = Start();
		var seen = new List<CreatureAttackKind>();
		for (var i = 0; i < 12; i++)
		{
			Until(attacks, CreatureAttackEvent.Warned, Still(new Vector2(5, 0)));
			seen.Add(attacks.Current);
			Until(attacks, CreatureAttackEvent.Missed, Still(new Vector2(40, 40)));
		}

		Assert.Contains(CreatureAttackKind.Lunge, seen);
		Assert.True(seen.Distinct().Count() >= 2);
	}

	[Fact]
	public void Cansada_a_criatura_espera_mais_para_atacar_de_novo()
	{
		Assert.True(Gap(1f) > Gap(0f));
	}

	// O tempo entre o fim de um golpe e o aviso do próximo.
	static float Gap(float tiredness)
	{
		var attacks = Start();
		var far = Still(new Vector2(40, 40));
		Until(attacks, CreatureAttackEvent.Warned, far, tiredness);
		Until(attacks, CreatureAttackEvent.Missed, far, tiredness);
		return Until(attacks, CreatureAttackEvent.Warned, far, tiredness);
	}

	[Fact]
	public void Reset_desfaz_o_aviso_no_ar()
	{
		var attacks = Start();
		Until(attacks, CreatureAttackEvent.Warned, Still(new Vector2(5, 0)));

		attacks.Reset();

		Assert.Equal(CreatureAttackPhase.Waiting, attacks.Phase);
	}

	// O cansaço alonga a espera do golpe seguinte; uma Etapa nova (Reset) começa de novo com a espera normal.
	[Fact]
	public void Reset_devolve_a_espera_normal_depois_de_um_golpe_de_criatura_cansada()
	{
		var attacks = Start();
		var player = Still(new Vector2(20, 0));
		Until(attacks, CreatureAttackEvent.Warned, player, tiredness: 1f);
		Until(attacks, CreatureAttackEvent.Missed, player, tiredness: 1f);
		attacks.Reset();

		var warned = Until(attacks, CreatureAttackEvent.Warned, player);

		Assert.InRange(warned, Rules.MinGapSeconds - 0.15f, Rules.MinGapSeconds + 0.15f);
	}
}
