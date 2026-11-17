namespace ArquipelagoDoCeu;

// O menu da Dungeon: fora de luta, o botão Menu e o Esc abrem; voltar ao Lobby é daqui ou do totem do Fast travel.
public partial class DungeonRun
{
	// Os modelos de criatura de cada torre, feitos no Blender (tools/blender/criaturas): autômatos nos Números e objetos e animais de papel nas Letras.
	static string[] CreaturesOf(string discipline) => discipline == "matematica"
		? ["criaturas/tartaruga", "criaturas/besouro", "criaturas/coruja"]
		: ["criaturas/mapa", "criaturas/pato", "criaturas/sapo"];

	// Sem Energia: uma tela gentil explica o que aconteceu e o que ficou guardado, e só então a criança volta ao Lobby.
	internal void OnLost()
	{
		if (left) return;
		var screen = new DefeatScreen(ctx.TowerName, () => Leave(""));
		endScreen = screen;
		if (ctx.ShowOverlay is { } show) show(screen);
		else Leave("A névoa ficou forte demais desta vez. Vamos recarregar a energia?");
	}

	void OpenMenu()
	{
		if (left || !enabled || hud.InCombat() || ctx.ShowOverlay is not { } show) return;
		show(new DungeonMenu(() => ctx.OpenSettings?.Invoke(), () => Leave("")));
	}
}
