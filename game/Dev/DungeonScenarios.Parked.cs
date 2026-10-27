using System.Threading.Tasks;
using Vector2 = System.Numerics.Vector2;

namespace ArquipelagoDoCeu;

// Entra numa torre e para numa Sala com criatura, para quem testa no celular mexer pelo adb: --driver=torre-parada,
// e --letras=sim para a Torre das Letras.
static partial class DungeonScenarios
{
	// Os cenários feitos para o celular: medir, e parar numa torre ou no Mercador para quem testa mexer pelo adb.
	public static Task Mobile(DevDriver d, string name) => name switch
	{
		"benchmark" => Benchmark(d),
		"torre-parada" => Parked(d),
		_ => ParkedMerchant(d),
	};

	public static async Task Parked(DevDriver d)
	{
		var run = await Enter(d, letters: d.Arg("letras") == "sim");
		if (run is null) return;
		await GoTo(d, run, r => run.Expedition.NeedsClearing(r));
		run.Player.PlaceAt(new Vector2(0f, 1.5f));
		// Com --bolhas=nao a Energia não cai enquanto quem testa olha a luta.
		if (d.Arg("bolhas") == "nao") run.Combat.Bubbles = false;
		await d.Wait(900);
	}
}
