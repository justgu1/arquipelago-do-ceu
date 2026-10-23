namespace ArquipelagoDoCeu;

// O menu da Dungeon: fora de luta, o botão Menu e o Esc abrem; voltar ao Lobby é daqui ou do totem do Fast travel.
public partial class DungeonRun
{
	// Os modelos de criatura de cada torre: autômatos e a coruja nos Números; nas Letras só o pato, até haver criaturas melhores.
	static string[] CreaturesOf(string discipline) => discipline == "matematica"
		? ["criaturas/automato-base", "criaturas/automato-pesado", "criaturas/automato-voador", "criaturas/automato-cristal", "coruja"]
		: ["pato"];

	void OpenMenu()
	{
		if (left || !enabled || hud.InCombat() || ctx.ShowOverlay is not { } show) return;
		show(new DungeonMenu(() => ctx.OpenSettings?.Invoke(), () => Leave("")));
	}
}
