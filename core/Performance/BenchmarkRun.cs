using System.Globalization;
using System.Text.Json;

namespace ArquipelagoDoCeu.Performance;

public sealed record SceneResult(string Name, FrameStats Stats, RenderInfo? Render = null);

// Uma rodada do benchmark, no formato do evento benchmark_run. Leva só o aparelho e os números: nunca Perfil nem apelido.
public sealed record BenchmarkRun(string DeviceModel, string Gpu, string Android, int RamMb, string GameVersion, QualityTier Tier, IReadOnlyList<SceneResult> Scenes)
{
	public bool MeetsTarget => Scenes.Count > 0 && Scenes.All(s => s.Stats.MeetsTarget);

	public string ToJson() => JsonSerializer.Serialize(new
	{
		type = "benchmark_run",
		device = DeviceModel,
		gpu = Gpu,
		android = Android,
		ram_mb = RamMb,
		game_version = GameVersion,
		tier = Tier.ToString().ToLowerInvariant(),
		meets_target = MeetsTarget,
		scenes = Scenes.Select(Describe),
	});

	static Dictionary<string, object> Describe(SceneResult scene)
	{
		var stats = scene.Stats;
		var json = new Dictionary<string, object>
		{
			["name"] = scene.Name,
			["frames"] = stats.Count,
			["avg_fps"] = Math.Round(stats.AverageFps, 1),
			["low_fps"] = Math.Round(stats.OnePercentLowFps, 1),
			["worst_ms"] = Math.Round(stats.WorstFrameMs, 1),
			["meets_target"] = stats.MeetsTarget,
		};
		if (scene.Render is not { } render) return json;
		json["draw_calls"] = render.DrawCalls;
		json["primitives"] = render.Primitives;
		json["process_ms"] = Math.Round(render.ProcessMs, 2);
		json["physics_ms"] = Math.Round(render.PhysicsMs, 2);
		json["vram_mb"] = Math.Round(render.VideoMemoryMb, 1);
		return json;
	}

	// O texto que o testador copia e manda quando está sem rede.
	public string ToText()
	{
		var linhas = new List<string> { $"{DeviceModel} · {Gpu} · Android {Android} · {RamMb} MB · perfil {Tier.ToString().ToLowerInvariant()} · v{GameVersion}" };
		linhas.AddRange(Scenes.Select(Line));
		linhas.Add(MeetsTarget ? "Resultado: passa na meta (60 de média e 45 no 1% pior)." : "Resultado: não passa na meta (60 de média e 45 no 1% pior).");
		return string.Join('\n', linhas);
	}

	static string Line(SceneResult scene)
	{
		var stats = scene.Stats;
		var text = string.Create(CultureInfo.InvariantCulture,
			$"{scene.Name}: {stats.AverageFps:0.#} fps médio, {stats.OnePercentLowFps:0.#} no 1% pior, pior quadro {stats.WorstFrameMs:0} ms, {(stats.MeetsTarget ? "passa" : "não passa")}");
		if (scene.Render is not { } r) return text;
		return text + string.Create(CultureInfo.InvariantCulture, $" · {r.DrawCalls} chamadas, {r.Primitives / 1000}k triângulos, processo {r.ProcessMs:0.#} ms, vídeo {r.VideoMemoryMb:0} MB");
	}
}
