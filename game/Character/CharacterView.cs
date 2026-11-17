using System.Collections.Generic;
using ArquipelagoDoCeu.Combat;
using ArquipelagoDoCeu.Equipment;
using ArquipelagoDoCeu.Profiles;
using Godot;

namespace ArquipelagoDoCeu;

// O Personagem na cena: o corpo do Blender (CharacterModel), as cores escolhidas no catálogo de aparência e o gesto de ataque.
// Serve igual à prévia do editor, ao Lobby e à Dungeon.
public partial class CharacterView : Node3D
{
	// Categoria do catálogo -> material do modelo. Categorias sem entrada aqui (cabelo, olhos...) esperam o catálogo ganhar peças para elas.
	static readonly Dictionary<string, string> MaterialOf = new()
	{
		["skinTone"] = "pele",
		["clothesColor"] = "roupa",
	};

	readonly CharacterModel model = new();
	EquipmentLoadout loadout = new();
	List<ChosenOption> pendingColors = [];
	Tween? attackTween;

	// Redução de animações: o Personagem fica na pose de repouso, sem balançar os braços e pernas ao andar.
	public bool ReduceMotion { get; set; }

	public bool IsWalkingAnimation => model.IsWalking;

	public override void _Ready()
	{
		AddChild(model);
		model.Show(loadout);
		Apply(pendingColors);
	}

	public void Apply(IEnumerable<ChosenOption> chosen)
	{
		pendingColors = [.. chosen];
		if (!model.IsInsideTree()) return;
		foreach (var item in pendingColors)
			if (MaterialOf.TryGetValue(item.Category.Id, out var material))
				model.SetTint(material, Color.FromHtml(item.Option.Color));
	}

	public void ApplyEquipment(EquipmentLoadout next)
	{
		loadout = next;
		if (model.IsInsideTree()) model.Show(next);
	}

	public void SetLocomotion(float speed, bool running)
	{
		model.ReduceMotion = ReduceMotion;
		model.SetLocomotion(speed, running);
	}

	// Feitos por código sobre o corpo inteiro (o modelo não tem esqueleto): Precisa é uma estocada, Diagonal um corte de lado e Carregado recua antes do golpe.
	// Com a redução de animações o Personagem não se mexe; o raio e o aviso escrito já mostram o ataque.
	public void PlayAttack(BasicAttack? attack = null)
	{
		if (ReduceMotion) return;
		attackTween?.Kill();
		Rotation = Vector3.Zero;
		attackTween = CreateTween();
		switch (attack)
		{
			case BasicAttack.Precise:
				Step("rotation:x", 0.35f, 0.07);
				Step("rotation:x", 0f, 0.1);
				break;
			case BasicAttack.Diagonal:
				Step("rotation:z", -0.35f, 0.05);
				attackTween.Parallel().TweenProperty(this, "rotation:x", 0.3f, 0.12);
				Step("rotation:z", 0.4f, 0.12);
				attackTween.Parallel().TweenProperty(this, "rotation:y", 0.35f, 0.12);
				attackTween.TweenProperty(this, "rotation", Vector3.Zero, 0.15);
				break;
			case BasicAttack.Charged:
				Step("rotation:x", -0.25f, 0.09);
				Step("rotation:x", 0.7f, 0.09);
				Step("rotation:x", -0.1f, 0.08);
				Step("rotation:x", 0f, 0.14);
				break;
			default:
				Step("rotation:x", 0.5f, 0.12);
				Step("rotation:x", -0.15f, 0.1);
				Step("rotation:x", 0f, 0.18);
				break;
		}
	}

	void Step(string property, float to, double seconds) =>
		attackTween!.TweenProperty(this, property, to, seconds).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

	public bool IsAttacking => attackTween?.IsRunning() == true;
}
