using Godot;

namespace ArquipelagoDoCeu;

// A ambientação da Sala: as cores do tema da torre (fundo, luz), o brilho das brasas e a poeira que flutua no ar.
public partial class DungeonWorld
{
	readonly DirectionalLight3D sun = new() { LightEnergy = 1.1f, ShadowEnabled = true, RotationDegrees = new Vector3(-50f, -30f, 0f) };
	DungeonTheme theme = DungeonTheme.Numbers;
	CpuParticles3D? dust;

	public DungeonTheme Theme
	{
		get => theme;
		set
		{
			theme = value;
			if (IsInsideTree()) ApplyTheme();
		}
	}

	void ApplyTheme()
	{
		sun.LightColor = Color.FromHtml(theme.Sun);
		if (Camera.Environment is { } environment)
		{
			environment.BackgroundColor = Color.FromHtml(theme.Void);
			environment.AmbientLightColor = Color.FromHtml(theme.Ambient);
			// O brilho das brasas e do anel de ouro: leve, para não cansar a vista de quem tem sensibilidade à luz.
			environment.GlowEnabled = !ReduceMotion;
			environment.GlowIntensity = 0.5f;
			environment.GlowBloom = 0.05f;
		}
		AddDust();
	}

	// Poucos grãos de luz subindo devagar em volta da Sala; sem eles com "reduzir animações".
	void AddDust()
	{
		dust?.QueueFree();
		dust = null;
		if (ReduceMotion) return;
		var mote = new QuadMesh { Size = new Vector2(0.07f, 0.07f) };
		mote.Material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			AlbedoColor = Color.FromHtml(theme.Dust) with { A = 0.55f },
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};
		dust = new CpuParticles3D
		{
			Name = "Poeira",
			Amount = 36,
			Lifetime = 9.0,
			Preprocess = 9.0,
			Mesh = mote,
			EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(7f, 0.2f, 7f),
			Direction = Vector3.Up,
			Spread = 20f,
			Gravity = Vector3.Zero,
			InitialVelocityMin = 0.1f,
			InitialVelocityMax = 0.35f,
			Position = new Vector3(0f, 0.3f, 0f),
		};
		AddChild(dust);
	}
}
