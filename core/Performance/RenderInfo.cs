namespace ArquipelagoDoCeu.Performance;

// O que o motor diz sobre o custo de uma cena: ajuda a achar se o gargalo é desenho, geometria ou código.
public sealed record RenderInfo(int DrawCalls, long Primitives, double ProcessMs, double PhysicsMs, double VideoMemoryMb)
{
	public static RenderInfo? Average(IReadOnlyList<RenderInfo> samples) => samples.Count == 0 ? null : new RenderInfo(
		(int)samples.Average(s => s.DrawCalls),
		(long)samples.Average(s => s.Primitives),
		samples.Average(s => s.ProcessMs),
		samples.Average(s => s.PhysicsMs),
		samples.Average(s => s.VideoMemoryMb));
}
