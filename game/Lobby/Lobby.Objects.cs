using System.Collections.Generic;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

public partial class Lobby
{
	// O que se escolhe por clique ganha o realce, e o nome fica em cima, como nas torres.
	void AddLandmarkSign(Node3D model, string id, string name, Vector3 ground, float top)
	{
		pickable[id] = model;
		var sign = new Label3D { Text = name, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 44, PixelSize = 0.012f, OutlineSize = 14, OutlineModulate = Color.FromHtml("#1B1424"), Modulate = Color.FromHtml("#FFF6E0"), NoDepthTest = true, Position = ground + new Vector3(0f, top + 0.7f, 0f) };
		AddChild(sign);
		signs.Add(sign);
		sign.AddToGroup(CameraController.LabelGroup);
	}

	// Objetos com que a criança interage: o modelo, os hexágonos bloqueados (em BlockedCells) e um indicador
	// em forma de seta, que só aparece quando o Personagem está ao alcance (a cor nunca é a única pista).
	void AddObjects()
	{
		var list = new List<Interactable>();
		foreach (var item in Rules.Objects)
		{
			var ground = HexToWorld(item.Q, item.R);
			// O Boneco é uma criatura: escolhida com clique ou tecla F, e não usada pelo botão de interação.
			if (item.Id != "boneco") list.Add(new Interactable(item.Id, new System.Numerics.Vector2(ground.X, ground.Z)));
			var height = item.Id switch { "portal" => PortalHeight, "guia" => GuideHeight, "boneco" => DummyHeight, "mestre" => MasterHeight, "mercador" => StallHeight, _ => LecternHeight };
			var top = height;
			if (LoadModel(item.Model) is { } model)
			{
				AddChild(model);
				// O Portal do Mestre e a barraca do Mercador mostram a frente para a câmera.
				if (item.Id is "mestre" or "mercador") model.Rotation = new Vector3(0f, TowardCamera, 0f);
				top = FitToHeight(model, height, ground);
				if (item.Id is "mestre" or "mercador") AddLandmarkSign(model, item.Id, item.Name, ground, top);
				if (item.Id == "portal")
					model.AddChild(new LobbyAmbience(model, true, () => ReduceMotion, () => reducedEffects));
				else if (item.Id == "guia")
					model.AddChild(new GuideIdle(model, () => ReduceMotion));
			}
			else if (item.Id.StartsWith("torre-")) top = TowerBaseHeight;
			else if (item.Id == "mestre") top = AddHall(ground, item.Name);
			else if (item.Id == "mercador") top = AddStall(ground, item.Name);
			var marker = new MeshInstance3D
			{
				Mesh = new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0f, Height = 0.55f, RadialSegments = 4, Rings = 0 },
				MaterialOverride = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#F2B632"), ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
				Position = ground + new Vector3(0f, top + 0.7f, 0f),
				Visible = false,
			};
			AddChild(marker);
			markers[item.Id] = marker;
			footprints[item.Id] = (ground, top);
		}
		Interactables = list;
	}

	// A barraca do Mercador: balcão, dois postes e um toldo. Greybox, até a arte definitiva.
	float AddStall(Vector3 ground, string name)
	{
		const float height = 2.0f;
		var wood = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#8A5A32"), Roughness = 0.9f };
		var cloth = new StandardMaterial3D { AlbedoColor = Color.FromHtml("#D9534F"), Roughness = 0.8f };
		AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.6f, 0.8f, 0.7f) }, MaterialOverride = wood, Position = ground + new Vector3(0f, 0.4f, 0f) });
		foreach (var side in new[] { -0.75f, 0.75f })
			AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.12f, height, 0.12f) }, MaterialOverride = wood, Position = ground + new Vector3(side, height / 2f, -0.3f) });
		AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(1.9f, 0.1f, 1.1f) }, MaterialOverride = cloth, Position = ground + new Vector3(0f, height, -0.1f), Rotation = new Vector3(0.18f, 0f, 0f) });
		var sign = new Label3D { Text = name, Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, FontSize = 44, PixelSize = 0.012f, OutlineSize = 14, OutlineModulate = Color.FromHtml("#1B1424"), Modulate = Color.FromHtml("#FFF6E0"), NoDepthTest = true, Position = ground + new Vector3(0f, height + 0.7f, 0f) };
		AddChild(sign);
		signs.Add(sign);
		sign.AddToGroup(CameraController.LabelGroup);
		return height;
	}
}
