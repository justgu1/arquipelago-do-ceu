using Godot;

namespace ArquipelagoDoCeu;

// Tempo desde o início do motor até cada passo da abertura; só no build de depuração, para achar o que a deixa lenta.
public static class Startup
{
	public static void Mark(string step)
	{
		if (OS.IsDebugBuild()) GD.Print($"[abertura] {step}: {Time.GetTicksMsec()} ms");
	}
}
