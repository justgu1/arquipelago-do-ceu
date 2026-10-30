using System.Collections.Generic;
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

	// Ataque provisório (até existir a animação do modelo): inclina o corpo para a frente e volta, como quem lança o cajado.
	// Com a redução de animações o Personagem não se mexe; o raio e o aviso escrito já mostram o ataque.
	public void PlayAttack()
	{
		if (ReduceMotion) return;
		attackTween?.Kill();
		Rotation = Vector3.Zero;
		attackTween = CreateTween();
		attackTween.TweenProperty(this, "rotation:x", 0.5f, 0.12).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		attackTween.TweenProperty(this, "rotation:x", -0.15f, 0.1);
		attackTween.TweenProperty(this, "rotation:x", 0f, 0.18);
	}

	public bool IsAttacking => attackTween?.IsRunning() == true;
}
