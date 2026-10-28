namespace ArquipelagoDoCeu;

// O menu da Dungeon: fora de luta, o botão Menu e o Esc abrem; voltar ao Lobby é daqui ou do totem do Fast travel.
public partial class DungeonRun
{
	// Os modelos de criatura de cada torre, feitos no Blender (tools/blender/criaturas): autômatos nos Números e objetos e animais de papel nas Letras.
	static string[] CreaturesOf(string discipline) => discipline == "matematica"
		? ["criaturas/tartaruga", "criaturas/besouro", "criaturas/coruja"]
		: ["criaturas/mapa", "criaturas/pato", "criaturas/sapo"];

	void OpenMenu()
	{
		if (left || !enabled || hud.InCombat() || ctx.ShowOverlay is not { } show) return;
		show(new DungeonMenu(() => ctx.OpenSettings?.Invoke(), () => Leave("")));
	}
}
