using ArquipelagoDoCeu.Equipment;
using ArquipelagoDoCeu.Rules;
using Xunit;

namespace ArquipelagoDoCeu.Tests;

public class BackpackTests
{
	static readonly string[] Owned = ["a", "b", "c", "d", "e"];

	[Fact]
	public void O_que_esta_vestido_nao_ocupa_slot()
	{
		var contents = Backpack.Fill(Owned, new HashSet<string> { "a", "b" }, 6);

		Assert.Equal(["c", "d", "e"], contents.InPack);
		Assert.Empty(contents.InChest);
		Assert.Equal(3, contents.Free);
	}

	[Fact]
	public void O_que_passa_do_limite_vai_para_o_bau_e_nada_se_perde()
	{
		var contents = Backpack.Fill(Owned, new HashSet<string>(), 3);

		Assert.Equal(["a", "b", "c"], contents.InPack);
		Assert.Equal(["d", "e"], contents.InChest);
		Assert.Equal(0, contents.Free);
		Assert.Equal(Owned.Length, contents.InPack.Count + contents.InChest.Count);
	}

	[Fact]
	public void Vender_um_item_da_mochila_traz_o_primeiro_do_bau_de_volta()
	{
		var sold = Owned.Where(id => id != "b").ToList();

		var contents = Backpack.Fill(sold, new HashSet<string>(), 3);

		Assert.Equal(["a", "c", "d"], contents.InPack);
		Assert.Equal(["e"], contents.InChest);
	}

	[Theory]
	[InlineData(1, 6)]
	[InlineData(2, 9)]
	[InlineData(3, 12)]
	[InlineData(0, 6)]
	[InlineData(9, 12)]
	public void Os_slots_crescem_com_o_Nivel_e_o_Nivel_fora_da_faixa_vale_o_limite(int level, int slots) =>
		Assert.Equal(slots, BackpackRules.Default.Slots(level));

	[Fact]
	public void As_regras_da_mochila_vem_dos_dados_e_valores_ruins_sao_recusados()
	{
		var json = File.ReadAllText(Path.Combine(MissionFiles.RepositoryRoot(), "game", "data", "rules.json"));

		var real = GameRules.Parse(json).Value!;

		Assert.Equal(BackpackRules.Default.SlotsByLevel, real.Backpack.SlotsByLevel);
		bool Accepts(string slots) => GameRules.Parse(json.Replace("\"slotsByLevel\": [6, 9, 12]", "\"slotsByLevel\": " + slots)).IsOk;

		Assert.True(Accepts("[6, 9, 12]"));
		Assert.False(Accepts("[]"));
		Assert.False(Accepts("[6, 4]"));
		Assert.False(Accepts("[0, 3]"));
	}
}
