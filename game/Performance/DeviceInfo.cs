using System.IO;
using System.Linq;
using ArquipelagoDoCeu.Touch;
using Godot;

namespace ArquipelagoDoCeu;

// O que o jogo sabe do aparelho: modelo, GPU, sistema, memória, tela de toque e margens da tela. Nada do jogador.
public static class DeviceInfo
{
	// Folga fixa para os cantos arredondados, que o Android não informa como área segura.
	const float CornerMillimeters = 1.5f;

	public static string Model => OS.GetModelName();

	public static string Gpu => RenderingServer.GetVideoAdapterName();

	public static string Os => OS.GetVersion();

	public static bool IsTouch => DisplayServer.IsTouchscreenAvailable();

	public static float Dpi => DisplayServer.ScreenGetDpi();

	// No Android o motor não informa a memória física; o kernel informa em /proc/meminfo, em kB.
	public static int RamMb
	{
		get
		{
			if (OS.GetMemoryInfo().TryGetValue("physical", out var bytes) && (long)bytes > 0) return (int)((long)bytes / (1024 * 1024));
			var line = File.Exists("/proc/meminfo") ? File.ReadLines("/proc/meminfo").FirstOrDefault() ?? "" : "";
			return long.TryParse(string.Concat(line.Where(char.IsDigit)), out var kb) ? (int)(kb / 1024) : 0;
		}
	}

	public static string GameVersion => ProjectSettings.GetSetting("application/config/version", "0.1.0").AsString();

	public static Insets SafeInsets()
	{
		if (!IsTouch) return new Insets(0, 0, 0, 0);
		var window = DisplayServer.WindowGetSize();
		var safe = DisplayServer.GetDisplaySafeArea();
		return ScreenInsets.From(window.X, window.Y, safe.Position.X, safe.Position.Y, safe.Size.X, safe.Size.Y, ScreenInsets.CornerMargin(Dpi, CornerMillimeters));
	}
}
