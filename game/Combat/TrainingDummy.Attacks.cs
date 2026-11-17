using ArquipelagoDoCeu.Combat;
using Godot;

namespace ArquipelagoDoCeu;

// O que a criança vê dos ataques da criatura e do estado dela: o aviso da área, o golpe, a estonteada do QTE e o cansaço.
public partial class TrainingDummy
{
	readonly MeshInstance3D warningDisc = new()
	{
		Mesh = new CylinderMesh { TopRadius = 1f, BottomRadius = 1f, Height = 0.04f, RadialSegments = 32 },
		MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.94f, 0.44f, 0.37f, 0.45f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, NoDepthTest = true },
		Visible = false,
	};
	readonly MeshInstance3D warningRing = new()
	{
		Mesh = new TorusMesh { InnerRadius = 0.92f, OuterRadius = 1f, Rings = 48, RingSegments = 8 },
		MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.94f, 0.44f, 0.37f, 0.85f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, NoDepthTest = true },
		Visible = false,
	};
	// As estrelas que giram sobre a criatura estonteada: forma e movimento, nunca só cor.
	readonly Node3D dizzy = new() { Visible = false };
	MeshInstance3D? shownWarning;
	Vector3 creatureBase;
	Vector3 strikeOffset;
	Tween? strikeTween;
	Tween? dizzyTween;

	// O aviso do golpe da criatura: uma forma no chão que mostra por onde ou onde ele vai cair; a criança só precisa sair dela.
	// Forma, texto e som: nunca só cor. A área se fecha enquanto o aviso corre.
	public void ShowStrip(Vector3 from, Vector3 direction, float length, float width, float progress)
	{
		Announce(warning, "Investida! Saia da faixa.");
		warning.Position = from + direction * (length / 2f) + new Vector3(0f, 0.08f, 0f);
		warning.Rotation = new Vector3(0f, Mathf.Atan2(direction.X, direction.Z), 0f);
		var narrowing = ReduceMotion ? 1f : 1f - 0.3f * progress;
		warning.Scale = new Vector3(width * narrowing, 1f, length);
	}

	// O Pulo cai onde a criança estava; o círculo enche até o golpe.
	public void ShowDisc(Vector3 at, float radius, float progress)
	{
		Announce(warningDisc, "Pulo! Saia do círculo.");
		warningDisc.Position = at + new Vector3(0f, 0.08f, 0f);
		var filled = ReduceMotion ? 1f : 0.35f + 0.65f * progress;
		warningDisc.Scale = new Vector3(radius * filled, 1f, radius * filled);
	}

	// O Giro alcança em volta da criatura: o anel marca até onde, e dentro dele é preciso sair.
	public void ShowRing(Vector3 at, float radius)
	{
		Announce(warningRing, "Giro! Afaste-se da criatura.");
		warningRing.Position = at + new Vector3(0f, 0.1f, 0f);
		warningRing.Scale = new Vector3(radius, 0.15f, radius);
	}

	void Announce(MeshInstance3D shape, string text)
	{
		if (shownWarning == shape) return;
		HideWarning();
		shownWarning = shape;
		shape.Visible = true;
		Play(Tones.Warning);
		Say(text, good: false);
	}

	public void HideWarning()
	{
		warning.Visible = false;
		warningDisc.Visible = false;
		warningRing.Visible = false;
		shownWarning = null;
	}

	// O golpe sai: a Investida corre até onde a faixa mostrava, o Pulo sobe e cai, o Giro roda. O modelo 3D anda junto.
	public void Strike(CreatureAttackKind kind, Vector3 direction, float reach)
	{
		Reacted?.Invoke(kind.ToString().ToLowerInvariant());
		Play(Tones.Strike);
		if (ReduceMotion) return;
		strikeTween?.Kill();
		strikeTween = CreateTween();
		switch (kind)
		{
			case CreatureAttackKind.Lunge:
				Offset(strikeTween, direction * Mathf.Min(reach, 3f), 0.18);
				Offset(strikeTween, Vector3.Zero, 0.4);
				break;
			case CreatureAttackKind.Slam:
				Offset(strikeTween, Vector3.Up * 0.9f, 0.25).SetEase(Tween.EaseType.Out);
				Offset(strikeTween, Vector3.Zero, 0.15).SetEase(Tween.EaseType.In);
				break;
			default:
				creature.RotationDegrees = Vector3.Zero;
				strikeTween.TweenProperty(creature, "rotation_degrees:y", 360f, 0.5);
				strikeTween.Parallel().TweenProperty(mist, "scale", Vector3.One * 1.3f, 0.25);
				strikeTween.Chain().TweenProperty(mist, "scale", Vector3.One, 0.25);
				break;
		}
	}

	MethodTweener Offset(Tween tween, Vector3 to, double seconds) =>
		tween.TweenMethod(Callable.From<Vector3>(value => SetStrikeOffset(value)), strikeOffset, to, seconds);

	void SetStrikeOffset(Vector3 value)
	{
		strikeOffset = value;
		mist.Position = center + value;
		creature.Position = creatureBase + value;
	}

	// Zerada a Névoa, a criatura fica estonteada com estrelas girando sobre ela até o QTE acabar.
	public void SetStunned(bool on)
	{
		if (dizzy.Visible == on) return;
		dizzy.Visible = on;
		dizzyTween?.Kill();
		Reacted?.Invoke(on ? "stunned" : "recovered");
		if (!on || ReduceMotion) return;
		dizzyTween = CreateTween().SetLoops();
		dizzyTween.TweenProperty(dizzy, "rotation_degrees:y", 360f, 1.6).From(0f);
	}

	// O cansaço da criatura, de 0 a 1: ela fica mais baixa e mais lenta (o modelo 3D lê o evento).
	public void SetTired(float tiredness)
	{
		TirednessChanged?.Invoke(tiredness);
		if (!ReduceMotion) mist.Scale = new Vector3(1f, 1f - 0.15f * tiredness, 1f);
	}

	public event System.Action<float>? TirednessChanged;

	void BuildDizzy(Vector3 ground, float top)
	{
		dizzy.Position = new Vector3(ground.X, top + 1.1f, ground.Z);
		for (var i = 0; i < 3; i++)
		{
			var angle = Mathf.Tau * i / 3f;
			var star = new Label3D
			{
				Text = "★",
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				FontSize = 96,
				PixelSize = 0.009f,
				OutlineSize = 16,
				Modulate = Gold,
				NoDepthTest = true,
				Position = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.7f,
			};
			star.AddToGroup(CameraController.LabelGroup);
			dizzy.AddChild(star);
		}
		body.AddChild(dizzy);
	}
}
