using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Rules;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class RhythmTests
{
	static readonly CombatRules Rules = TrainingFixtures.Rules;

	static void Run(AttackRhythm rhythm, float seconds)
	{
		for (var t = 0f; t < seconds; t += 0.1f) rhythm.Tick(0.1f);
	}

	[Fact]
	public void O_jogador_so_tem_uma_recarga_curta_entre_uma_resposta_e_a_proxima()
	{
		var rhythm = new AttackRhythm(Rules);
		Assert.True(rhythm.PlayerReady);

		rhythm.PlayerAnswered();

		Assert.False(rhythm.PlayerReady);
		Run(rhythm, Rules.Rhythm.PlayerRechargeSeconds - 0.2f);
		Assert.False(rhythm.PlayerReady);
		Run(rhythm, 0.5f);
		Assert.True(rhythm.PlayerReady);
	}

	[Fact]
	public void Uma_luta_nova_comeca_sem_recarga()
	{
		var rhythm = new AttackRhythm(Rules);
		rhythm.PlayerAnswered();

		rhythm.Reset();

		Assert.True(rhythm.PlayerReady);
	}
}
