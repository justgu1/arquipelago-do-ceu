namespace ArquipelagoDoCeu.Performance;

// O resumo de uma cena medida. O "1% pior" é o fps dos quadros mais lentos: a média sozinha esconde engasgo.
public sealed record FrameStats(int Count, double AverageFps, double OnePercentLowFps, double WorstFrameMs)
{
	public const double TargetAverageFps = 60;
	public const double TargetOnePercentLowFps = 45;

	// O vsync trava em 60 e a média nunca chega exata; 1 fps de folga evita reprovar por arredondamento.
	const double Tolerance = 1;

	public bool MeetsTarget => Count > 0 && AverageFps >= TargetAverageFps - Tolerance && OnePercentLowFps >= TargetOnePercentLowFps - Tolerance;

	public static FrameStats From(IReadOnlyList<double> frameMs)
	{
		if (frameMs.Count == 0) return new FrameStats(0, 0, 0, 0);
		var slowest = frameMs.OrderDescending().Take(Math.Max(1, frameMs.Count / 100)).ToArray();
		return new FrameStats(frameMs.Count, 1000.0 * frameMs.Count / frameMs.Sum(), 1000.0 / slowest.Average(), slowest[0]);
	}
}
