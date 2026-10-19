namespace ArquipelagoDoCeu;

// O menu da Dungeon: fora de luta, o botão Menu e o Esc abrem; voltar ao Lobby é daqui ou do totem do Fast travel.
public partial class DungeonRun
{
	void OpenMenu()
	{
		if (left || !enabled || hud.InCombat() || ctx.ShowOverlay is not { } show) return;
		show(new DungeonMenu(() => ctx.OpenSettings?.Invoke(), () => Leave("")));
	}
}
