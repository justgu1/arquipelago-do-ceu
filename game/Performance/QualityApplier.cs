using ArquipelagoDoCeu.Performance;
using Godot;

namespace ArquipelagoDoCeu;

// Aplica um perfil de qualidade ao jogo. Só liga ou desliga sombra, antisserrilhado e resolução; a geometria nunca muda.
public static class QualityApplier
{
	const string ShadowMeta = "sombra_original";
	static bool shadows = true;
	static bool hooked;

	public static void Apply(SceneTree tree, QualityTier tier)
	{
		var settings = Quality.For(tier);
		var root = tree.Root;
		root.Msaa3D = settings.Msaa switch { 2 => Viewport.Msaa.Msaa2X, 4 => Viewport.Msaa.Msaa4X, _ => Viewport.Msaa.Disabled };
		root.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
		root.Scaling3DScale = settings.RenderScale;
		shadows = settings.Shadows;
		foreach (var light in root.FindChildren("*", "DirectionalLight3D", true, false)) Adjust((DirectionalLight3D)light);
		if (hooked) return;
		hooked = true;
		// As Salas criam a luz na hora; sem isto o perfil só valeria para as luzes que já existiam.
		tree.NodeAdded += node => { if (node is DirectionalLight3D light) Adjust(light); };
	}

	static void Adjust(DirectionalLight3D light)
	{
		if (!light.HasMeta(ShadowMeta)) light.SetMeta(ShadowMeta, light.ShadowEnabled);
		light.ShadowEnabled = shadows && light.GetMeta(ShadowMeta).AsBool();
	}
}
