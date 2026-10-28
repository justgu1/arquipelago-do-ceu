using System.Numerics;
using ArquipelagoDoCeu.World;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public sealed class EntryTriggerTests
{
	static readonly Interactable[] Portal = [new("portal", new Vector2(10f, 0f))];

	[Fact]
	public void Pisar_na_soleira_ativa_uma_vez()
	{
		var trigger = new EntryTrigger(1f, 2f);

		Assert.Null(trigger.Step(new Vector2(5f, 0f), Portal));
		Assert.Equal("portal", trigger.Step(new Vector2(9.5f, 0f), Portal));
		Assert.Null(trigger.Step(new Vector2(10f, 0f), Portal));
	}

	[Fact]
	public void Depois_de_se_afastar_a_soleira_ativa_de_novo()
	{
		var trigger = new EntryTrigger(1f, 2f);
		trigger.Step(new Vector2(10f, 0f), Portal);

		Assert.Null(trigger.Step(new Vector2(11.5f, 0f), Portal));
		Assert.Null(trigger.Step(new Vector2(7f, 0f), Portal));
		Assert.Equal("portal", trigger.Step(new Vector2(9f, 0f), Portal));
	}

	[Fact]
	public void Quem_ja_estava_na_soleira_nao_ativa_ate_sair()
	{
		var trigger = new EntryTrigger(1f, 2f);
		trigger.Settle(new Vector2(10f, 0f), Portal);

		Assert.Null(trigger.Step(new Vector2(10f, 0f), Portal));
		trigger.Step(new Vector2(14f, 0f), Portal);
		Assert.Equal("portal", trigger.Step(new Vector2(10f, 0f), Portal));
	}
}
