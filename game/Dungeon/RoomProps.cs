using Godot;

namespace ArquipelagoDoCeu;

// Os modelos 3D dos objetos da Sala (porta, escada, fonte, totem), no lugar do que antes era feito de caixas.
// Se o modelo não carregar, quem chama segue com o desenho simples.
public static class RoomProps
{
	// `yaw` gira em volta do eixo vertical; o modelo fica com a base em `at` e a altura pedida. Devolve nulo se não carregou.
	public static Node3D? Add(Node3D parent, string name, float height, Vector3 at, float yaw = 0f)
	{
		if (Lobby.LoadModel(name) is not { } model) return null;
		parent.AddChild(model);
		Lobby.FitToHeight(model, height, at);
		model.Rotation = new Vector3(0f, yaw, 0f);
		return model;
	}

	// Um véu cinza por cima, para mostrar que o objeto já foi usado.
	public static void Dim(Node3D model)
	{
		var veil = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.43f, 0.55f, 0.55f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha };
		foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
			((MeshInstance3D)node).MaterialOverlay = veil;
	}
}
