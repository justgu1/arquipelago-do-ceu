using System.Linq;
using System.Threading.Tasks;
using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// O combate em duas camadas (ADR 0009) jogado de ponta a ponta no jogo de verdade, como a criança: Névoa, QTE errado, QTE certo,
// segunda Etapa e golpe final com a cutscene pulada. Uso: <jogo> --headless -- --driver=e2e-nevoa --combate-novo
static class NevoaScenario
{
	public static async Task Play(DevDriver d)
	{
		await d.Press("Criar");
		await d.Type("Luna");
		await d.Press("Criar");
		await d.TeleportNear("boneco");
		await d.Tap(Controls.Target);
		await d.Press("Escriba");
		await d.Press("Começar");

		var area = d.GetTree().Root.FindChildren("*", "", true, false).OfType<TrainingArea>().FirstOrDefault(a => a.Fight is not null);
		d.Expect(area is not null && d.GetTree().Root.FindChild("FogHud", true, false) is not null, "o combate novo abriu a HUD da Névoa");
		d.Expect(d.Hud is null, "a HUD de resposta só abre quando a Névoa zera");
		if (area?.Fight is not { } fight) return;
		await d.Shot("nevoa-1-ataques");
		d.Expect(fight.Fog == fight.StageFog && fight.Stage == 1, "a luta começa na Etapa 1 com a Névoa cheia");

		await ClearFog(d, fight);
		d.Expect(fight.Phase == FogPhase.Qte && d.Hud is not null, "zerar a Névoa abriu o QTE");

		await d.Shot("nevoa-2-qte");
		var energy = area.Session!.Energy.Current;
		await d.PlayName(wrong: true);
		d.Expect(area.Session.Energy.Current == energy - 1, "a resposta errada custou 1 de Energia");
		d.Expect(fight.Phase == FogPhase.Fog && fight.Fog > 0 && fight.Fog < fight.StageFog && d.Hud is null, "falhar devolveu parte da Névoa e fechou o QTE");

		await ClearFog(d, fight);
		await d.PlayName();
		d.Expect(fight.Stage == 2 && fight.Phase == FogPhase.Fog, "passar no QTE avançou para a Etapa 2");

		await d.Wait(1.6);
		await ClearFog(d, fight);
		await d.PlayName();
		d.Expect(d.GetTree().Root.FindChild("FogCutscene", true, false) is not null, "o golpe final abriu a cutscene");
		await d.Wait(1.2);
		await d.Shot("nevoa-3-golpe-final");
		d.Expect(Engine.TimeScale == 0.0, "a cutscene parou o jogo");
		await d.PressKey(Key.Escape);
		await d.Wait(0.5);
		d.Expect(d.GetTree().Root.FindChild("FogCutscene", true, false) is null && Engine.TimeScale > 0.0, "pular a cutscene devolveu o jogo");
		d.Expect(area.Fight is { Stage: 1, Phase: FogPhase.Fog }, "a criatura foi libertada e uma luta nova começou");
		await d.Press("Sair do treino");
	}

	// Ataca com o ataque da fraqueza, que a criança também vê na tela, até a Névoa zerar.
	static async Task ClearFog(DevDriver d, FogFight fight)
	{
		for (var i = 0; i < 400 && fight.Phase == FogPhase.Fog; i++)
		{
			await d.PressKey(Key.Key1 + (int)fight.Weakness);
			await d.Wait(0.5);
		}
		await d.Wait(0.5);
	}
}
