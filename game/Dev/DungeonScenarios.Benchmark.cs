using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArquipelagoDoCeu.Performance;
using Godot;
using Vector2 = System.Numerics.Vector2;

namespace ArquipelagoDoCeu;

// Benchmark do aparelho: mede o Lobby e cada criatura das duas torres; --perfis=todos mede os três perfis e --cenas=lobby só o Lobby.
// Sem --perfis desce de perfil só enquanto não chega na meta. O relatório sai no log (BENCHMARK_JSON), em user://benchmark.json e na tela.
static partial class DungeonScenarios
{
	public static async Task Benchmark(DevDriver d)
	{
		var probe = new FrameProbe();
		d.AddChild(probe);
		var all = d.Arg("perfis") == "todos";
		var tier = all ? QualityTier.High : Quality.Initial(DeviceInfo.RamMb);
		var onlyLobby = d.Arg("cenas") == "lobby";
		var runs = new List<BenchmarkRun>();
		while (true)
		{
			QualityApplier.Apply(d.GetTree(), tier);
			var run = new BenchmarkRun(DeviceInfo.Model, DeviceInfo.Gpu, DeviceInfo.Os, DeviceInfo.RamMb, DeviceInfo.GameVersion, tier, await MeasureScenes(d, probe, onlyLobby));
			runs.Add(run);
			GD.Print("BENCHMARK_JSON " + run.ToJson());
			if (Quality.Lower(tier) is not { } lower || (!all && run.MeetsTarget)) break;
			tier = lower;
		}

		var text = string.Join("\n\n", runs.Select(r => r.ToText()));
		File.WriteAllText(ProjectSettings.GlobalizePath("user://benchmark.json"), "[" + string.Join(",", runs.Select(r => r.ToJson())) + "]");
		File.WriteAllText(ProjectSettings.GlobalizePath("user://benchmark.txt"), text);
		d.Expect(runs.Count > 0, "o benchmark gerou o relatório");
		ShowReport(d, text);
		await d.Wait(20);
	}

	static async Task<List<SceneResult>> MeasureScenes(DevDriver d, FrameProbe probe, bool onlyLobby)
	{
		var scenes = new List<SceneResult>();
		await EnsureProfile(d);
		scenes.Add(await Measure(d, probe, "lobby", () => d.Hold(Controls.Right, 6.0)));
		if (onlyLobby) return scenes;
		foreach (var (letters, tower) in new[] { (false, "numeros"), (true, "letras") })
		{
			var run = await Enter(d, letters);
			if (run is null) continue;
			var done = new HashSet<string>();
			for (var k = 0; k < run.World.CreatureSet.Count; k++)
			{
				var wanted = k;
				await GoTo(d, run, r => run.Expedition.NeedsClearing(r) && r.Id % run.World.CreatureSet.Count == wanted);
				if (!run.Expedition.NeedsClearing(run.Expedition.Current) || run.Expedition.CurrentRoom % run.World.CreatureSet.Count != wanted) continue;
				var name = run.World.CreatureSet[wanted].Replace("criaturas/", "");
				run.Player.PlaceAt(new Vector2(0f, 1.5f));
				await d.Wait(0.5);
				await d.Tap(Controls.Target);
				if (done.Add(name)) scenes.Add(await Measure(d, probe, $"{tower}-{name}", () => d.Wait(4.0)));
				await d.PressKey(Key.Escape);
			}

			run.Leave("");
			// Voltar ao Lobby leva um instante; medir ou entrar de novo antes disso falha em silêncio.
			for (var i = 0; i < 40 && d.Player is not { Enabled: true }; i++) await d.Wait(0.25);
			await d.Wait(0.5);
		}

		return scenes;
	}

	static async Task<SceneResult> Measure(DevDriver d, FrameProbe probe, string name, System.Func<Task> action)
	{
		await d.Wait(0.5);
		probe.Start();
		await action();
		return probe.Stop(name);
	}

	static void ShowReport(DevDriver d, string text)
	{
		var layer = new CanvasLayer { Layer = 100 };
		var panel = new PanelContainer { Position = new Godot.Vector2(20, 20) };
		panel.AddChild(new Label { Text = text });
		layer.AddChild(panel);
		d.AddChild(layer);
	}
}
