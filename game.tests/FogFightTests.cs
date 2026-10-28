using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.Rules;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class FogFightTests
{
	static readonly FogRules Rules = FogRules.Default;

	static FogFight Start(bool calm = false, FogRules? rules = null) => new(rules ?? Rules, calm, new Pcg32(7));

	static BasicAttack Other(FogFight fight) => Enum.GetValues<BasicAttack>().First(a => a != fight.Weakness);

	// Ataca com o tipo pedido até a Névoa da Etapa zerar e o QTE abrir.
	static void ClearStage(FogFight fight)
	{
		while (fight.Phase == FogPhase.Fog)
		{
			fight.Tick(10f);
			fight.Strike(fight.Weakness);
		}
	}

	[Fact]
	public void A_luta_comeca_na_primeira_etapa_com_a_nevoa_cheia()
	{
		var fight = Start();

		Assert.Equal(FogPhase.Fog, fight.Phase);
		Assert.Equal(1, fight.Stage);
		Assert.Equal(Rules.StageFog, fight.Fog);
	}

	[Fact]
	public void O_ataque_da_fraqueza_tira_mais_nevoa_e_o_errado_menos_mas_nunca_zero()
	{
		foreach (var attack in Enum.GetValues<BasicAttack>())
		{
			var damageWeak = WeaknessAs(attack).Strike(attack)!.Damage;
			var damageOther = OtherThan(attack).Strike(attack)!.Damage;

			Assert.True(damageWeak > damageOther);
			Assert.True(damageOther >= 1);
		}
	}

	static IEnumerable<FogFight> Fights() => Enumerable.Range(0, 50).Select(seed => new FogFight(Rules, false, new Pcg32((ulong)seed)));

	static FogFight WeaknessAs(BasicAttack attack) => Fights().First(f => f.Weakness == attack);

	static FogFight OtherThan(BasicAttack attack) => Fights().First(f => f.Weakness != attack);

	[Fact]
	public void Depois_de_atacar_o_botao_recarrega_e_so_volta_pronto_no_fim()
	{
		var fight = Start();
		var attack = fight.Weakness;

		fight.Strike(attack);

		Assert.False(fight.IsReady(attack));
		Assert.Null(fight.Strike(attack));
		fight.Tick(Rules.RechargeSeconds[(int)attack] / 2f);
		Assert.InRange(fight.RechargeProgress(attack), 0.4f, 0.6f);
		fight.Tick(Rules.RechargeSeconds[(int)attack]);
		Assert.True(fight.IsReady(attack));
		Assert.Equal(1f, fight.RechargeProgress(attack));
	}

	[Fact]
	public void Cada_ataque_tem_a_sua_recarga_e_nao_trava_os_outros()
	{
		var fight = Start();
		var first = Enum.GetValues<BasicAttack>()[0];

		fight.Strike(first);

		Assert.True(Enum.GetValues<BasicAttack>().Where(a => a != first).All(fight.IsReady));
	}

	[Fact]
	public void Zerar_a_nevoa_abre_o_QTE_e_nada_ataca_enquanto_ele_esta_aberto()
	{
		var fight = Start();
		StrikeResult? last = null;
		while (fight.Phase == FogPhase.Fog)
		{
			fight.Tick(10f);
			last = fight.Strike(fight.Weakness);
		}

		Assert.True(last!.StageCleared);
		Assert.Equal(0, fight.Fog);
		Assert.Equal(FogPhase.Qte, fight.Phase);
		Assert.Null(fight.Strike(fight.Weakness));
	}

	[Fact]
	public void Passar_encerra_a_etapa_de_vez_e_a_proxima_comeca_com_nevoa_cheia()
	{
		var fight = Start();
		ClearStage(fight);

		var pass = fight.QtePassed();

		Assert.False(pass!.FinalBlow);
		Assert.Equal(2, fight.Stage);
		Assert.Equal(FogPhase.Fog, fight.Phase);
		Assert.Equal(Rules.StageFog, fight.Fog);
	}

	[Fact]
	public void Passar_o_QTE_da_ultima_etapa_e_o_golpe_final_e_vence_a_criatura()
	{
		var fight = Start();
		ClearStage(fight);
		fight.QtePassed();
		ClearStage(fight);

		var pass = fight.QtePassed();

		Assert.True(pass!.FinalBlow);
		Assert.Equal(FogPhase.Won, fight.Phase);
	}

	[Fact]
	public void Falhar_devolve_a_fracao_da_nevoa_da_etapa_e_exige_tirar_de_novo()
	{
		var fight = Start();
		ClearStage(fight);

		fight.QteFailed();

		Assert.Equal(FogPhase.Fog, fight.Phase);
		Assert.Equal((int)Math.Ceiling(Rules.StageFog * Rules.ReturnFraction), fight.Fog);
		Assert.Equal(1, fight.Stage);
		ClearStage(fight);
		Assert.Equal(FogPhase.Qte, fight.Phase);
	}

	[Fact]
	public void Fora_do_QTE_passar_ou_falhar_nao_faz_nada()
	{
		var fight = Start();

		Assert.Null(fight.QtePassed());
		fight.QteFailed();

		Assert.Equal(Rules.StageFog, fight.Fog);
	}

	[Fact]
	public void O_fim_da_janela_devolve_a_nevoa_no_modo_normal()
	{
		var fight = Start();
		ClearStage(fight);

		Assert.Equal(FogEvent.None, fight.Tick(Rules.QteSeconds - 1f));
		Assert.Equal(FogEvent.QteTimedOut, fight.Tick(2f));

		Assert.Equal(FogPhase.Fog, fight.Phase);
		Assert.True(fight.Fog > 0);
	}

	[Fact]
	public void No_modo_calmo_nao_ha_relogio_e_a_recarga_cai()
	{
		var fight = Start(calm: true);
		var attack = fight.Weakness;
		fight.Strike(attack);

		Assert.Equal(Rules.CalmRechargeSeconds, fight.RechargeLeft(attack));
		Assert.False(fight.HasClock);
		ClearStage(fight);
		Assert.Equal(FogEvent.None, fight.Tick(Rules.QteSeconds * 10f));
		Assert.Equal(FogPhase.Qte, fight.Phase);
	}

	[Fact]
	public void A_fraqueza_muda_uma_so_vez_depois_de_um_aviso()
	{
		var fight = Start();
		var before = fight.Weakness;
		ClearStage(fight);
		fight.QtePassed();

		Assert.NotNull(fight.ShiftingTo);
		Assert.Equal(before, fight.Weakness);
		Assert.Equal(FogEvent.None, fight.Tick(Rules.ShiftWarningSeconds - 1f));
		Assert.Equal(FogEvent.ShiftApplied, fight.Tick(2f));
		Assert.NotEqual(before, fight.Weakness);
		Assert.Null(fight.ShiftingTo);
	}

	[Fact]
	public void Errar_o_tipo_do_ataque_nao_muda_nada_alem_do_dano()
	{
		var fight = Start();

		var result = fight.Strike(Other(fight));

		Assert.False(result!.Weak);
		Assert.Equal(FogPhase.Fog, fight.Phase);
	}

	[Fact]
	public void O_premio_tem_tres_niveis_conforme_dica_tempo_e_tentativa()
	{
		var perfect = Start();
		ClearStage(perfect);
		perfect.Tick(1f);
		Assert.Equal(Prize.Perfect, perfect.QtePassed()!.Prize);

		var hinted = Start();
		ClearStage(hinted);
		hinted.NoteHint();
		Assert.Equal(Prize.Close, hinted.QtePassed()!.Prize);

		var late = Start();
		ClearStage(late);
		late.Tick(Rules.QteSeconds - 1f);
		Assert.Equal(Prize.Close, late.QtePassed()!.Prize);

		var retry = Start();
		ClearStage(retry);
		retry.QteFailed();
		ClearStage(retry);
		Assert.Equal(Prize.Hit, retry.QtePassed()!.Prize);
	}

	[Fact]
	public void No_modo_calmo_o_tempo_nunca_rebaixa_o_premio()
	{
		var fight = Start(calm: true);
		ClearStage(fight);
		fight.Tick(1000f);

		Assert.Equal(Prize.Perfect, fight.QtePassed()!.Prize);
	}

	[Fact]
	public void A_energia_do_premio_vem_dos_dados_e_quem_usa_dica_avanca_igual()
	{
		var energy = new EnergyRules(1, 2, 1, 1);

		Assert.Equal(2, FogFight.EnergyFor(Prize.Perfect, energy));
		Assert.Equal(1, FogFight.EnergyFor(Prize.Hit, energy));
		Assert.Equal(0, FogFight.EnergyFor(Prize.Close, energy));
	}

	[Fact]
	public void As_regras_da_nevoa_vem_dos_dados_e_valores_ruins_sao_recusados()
	{
		var valid = TestJson.Rules;
		const string fog = "\"fog\": { \"stages\": 3, \"stageFog\": 20, \"damage\": [1, 2, 3], \"rechargeSeconds\": [2, 3, 4], \"weakMultiplier\": 2, \"otherMultiplier\": 0.5, \"returnFraction\": 0.5, \"qteSeconds\": 20, \"calmRechargeSeconds\": 1, \"shiftWarningSeconds\": 2, \"perfectTimeLeft\": 0.3, \"closeTimeLeft\": 0.1 },";

		Assert.Equal(Rules, GameRules.Parse(valid).Value!.Combat.Fog);
		var parsed = GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { " + fog));
		Assert.Equal(3, parsed.Value!.Combat.Fog.Stages);
		Assert.False(GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { " + fog.Replace("\"stages\": 3", "\"stages\": 9"))).IsOk);
		Assert.False(GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { " + fog.Replace("[1, 2, 3]", "[1, 2]"))).IsOk);
		Assert.False(GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { " + fog.Replace("[2, 3, 4]", "[2, 3, 40]"))).IsOk);
		Assert.False(GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { " + fog.Replace("\"closeTimeLeft\": 0.1", "\"closeTimeLeft\": 0.4").Replace("\"perfectTimeLeft\": 0.3", "\"perfectTimeLeft\": 0.2"))).IsOk);
		Assert.False(GameRules.Parse(valid.Replace("\"combat\": {", "\"combat\": { \"fog\": { \"stages\": 2 },")).IsOk);
	}
}
