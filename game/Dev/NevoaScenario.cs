using System.Linq;
using System.Threading.Tasks;
using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// O combate em duas camadas (ADR 0009) jogado de ponta a ponta: ataque da criatura, cansaço, estonteada no QTE, Etapas e golpe final.
// Uso: <jogo> --headless -- --driver=e2e-nevoa --ataques
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
		d.Expect(area is not null && d.GetTree().Root.FindChild("FogHud", true, false) is not null, "o combate abriu a HUD da Névoa");
		d.Expect(d.Hud is null, "a HUD de resposta só abre quando a Névoa zera");
		if (area?.Fight is not { } fight) return;
		await d.Shot("nevoa-1-ataques");
		d.Expect(fight.Fog == fight.StageFog && fight.Stage == 1, "a luta começa na Etapa 1 com a Névoa cheia");

		await CreatureStrikes(d, area);
		TrainingArea.PilotPeace = true;

		fight.Strike(fight.Weakness);
		d.Expect(fight.Tiredness > 0f && d.HasVisible("Criatura"), "cada golpe cansa a criatura e a HUD diz o quanto");

		await ClearFog(d, fight);
		d.Expect(fight.Phase == FogPhase.Qte && d.Hud is not null, "zerar a Névoa abriu o QTE");
		d.Expect(fight.Stunned && d.GetTree().Root.FindChildren("*", "Label3D", true, false).Cast<Label3D>().Any(l => l.Text == "★" && l.IsVisibleInTree()), "a criatura ficou estonteada, com estrelas sobre ela");

		await d.Shot("nevoa-2-qte");
		var energy = area.Session!.Energy.Current;
		await d.PlayName(wrong: true);
		d.Expect(area.Session.Energy.Current == energy - 1, "a resposta errada custou 1 de Energia");
		d.Expect(fight.Phase == FogPhase.Fog && fight.Fog > 0 && fight.Fog < fight.StageFog && d.Hud is null && !fight.Stunned, "falhar devolveu parte da Névoa, fechou o QTE e a criatura se recuperou");

		await ClearFog(d, fight);
		await d.PlayName();
		d.Expect(fight.Stage == 2 && fight.Phase == FogPhase.Fog && fight.Tiredness == 0f, "passar no QTE avançou para a Etapa 2 com a criatura descansada");

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

	// Parada, a criança é alcançada: o primeiro ataque da criatura é a Investida, que avisa a faixa antes e custa 1 de Energia.
	static async Task CreatureStrikes(DevDriver d, TrainingArea area)
	{
		var energy = area.Session!.Energy.Current;
		var warned = false;
		for (var i = 0; i < 100 && area.Session.Energy.Current == energy; i++)
		{
			warned |= d.HasVisible("Saia da faixa");
			await d.Wait(0.2);
		}
		d.Expect(warned, "a criatura avisou a Investida antes de atacar");
		d.Expect(area.Session.Energy.Current == energy - 1, "ficar parado na faixa custou 1 de Energia");
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
