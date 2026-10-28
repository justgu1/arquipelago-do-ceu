using System.Linq;
using Godot;

namespace ArquipelagoDoCeu;

// A chave de desenvolvimento do protótipo do combate em duas camadas (ADR 0009): F10 alterna entre o combate atual e o novo,
// sem recompilar, e `--combate-novo` já abre ligado. Só existe com o build de depuração: o jogo exportado nunca a vê.
public static class FogMode
{
	static bool on = OS.IsDebugBuild() && OS.GetCmdlineUserArgs().Contains("--combate-novo");

	// Lida quando o treino começa: trocar no meio de uma luta vale só para a próxima.
	public static bool On => on;

	// Verdadeiro quando o evento era a chave; `text` diz como ficou.
	public static bool TryToggle(InputEvent @event, out string text)
	{
		text = "";
		if (!OS.IsDebugBuild() || @event is not InputEventKey { PhysicalKeycode: Key.F10, Pressed: true, Echo: false }) return false;
		on = !on;
		text = on ? "Combate novo (Névoa e QTE) ligado para o próximo treino." : "Combate atual ligado para o próximo treino.";
		return true;
	}
}
