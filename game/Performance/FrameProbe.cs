using System.Collections.Generic;
using ArquipelagoDoCeu.Performance;
using Godot;

namespace ArquipelagoDoCeu;

// Mede o tempo real de cada quadro. Usa o relógio do sistema e não o delta, que o Engine.TimeScale encolhe durante o desenho.
public partial class FrameProbe : Node
{
	// O custo de desenho é lido a cada tantos quadros; ler em todos mediria a própria leitura.
	const int RenderEvery = 10;

	readonly List<double> frames = [];
	readonly List<RenderInfo> render = [];
	ulong last;
	bool running;

	public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

	public void Start()
	{
		frames.Clear();
		render.Clear();
		last = 0;
		running = true;
	}

	public SceneResult Stop(string name)
	{
		running = false;
		return new SceneResult(name, FrameStats.From(frames), RenderInfo.Average(render));
	}

	public override void _Process(double delta)
	{
		if (!running) return;
		var now = Time.GetTicksUsec();
		if (last != 0) frames.Add((now - last) / 1000.0);
		last = now;
		if (frames.Count % RenderEvery == 0) render.Add(Sample());
	}

	static RenderInfo Sample() => new(
		(int)Godot.Performance.GetMonitor(Godot.Performance.Monitor.RenderTotalDrawCallsInFrame),
		(long)Godot.Performance.GetMonitor(Godot.Performance.Monitor.RenderTotalPrimitivesInFrame),
		Godot.Performance.GetMonitor(Godot.Performance.Monitor.TimeProcess) * 1000,
		Godot.Performance.GetMonitor(Godot.Performance.Monitor.TimePhysicsProcess) * 1000,
		Godot.Performance.GetMonitor(Godot.Performance.Monitor.RenderVideoMemUsed) / (1024 * 1024));
}
