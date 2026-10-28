using System.Threading.Tasks;
using Godot;

namespace ArquipelagoDoCeu;

// Cenários do piloto sobre a vida do Lobby: moradores, ciclo do dia e as portas das torres.
static class LobbyScenarios
{
	public static async Task Village(DevDriver d)
	{
		await d.Type("Luna");
		await d.Press("Criar");
		await d.Wait(1.0);
		await d.Shot("01-vilarejo");
		await d.Wait(8.0);
		await d.Shot("02-oito-segundos-depois");
		if (d.GetTree().Root.FindChild("LobbyLife", true, false) is LobbyLife life) GD.Print("Vida do Lobby: ", life.Diagnostics());
		await d.Hold(Controls.Right, 1.5);
		await d.Shot("03-andando");
		await d.Hold(Controls.Up, 1.6);
		await d.Shot("04-atras-das-arvores");
		await d.Hold(Controls.Left, 1.8);
		await d.Shot("05-mais-adiante");
	}

	public static void KitMeasures(DevDriver d)
	{
		foreach (var asset in new[] { "hex-base-v02", "margem-reta", "margem-convexa", "ponte-piso", "ponte-guarda-corpo", "ponte-cabeceira", "rocha-inferior-avaliacao", "grama-tres-pontas-v01" })
		{
			var node = GD.Load<PackedScene>($"res://assets/modelos/lote02/{asset}.glb").Instantiate<Node3D>();
			d.AddChild(node);
			var box = default(Aabb);
			var first = true;
			foreach (var mesh in System.Linq.Enumerable.Cast<MeshInstance3D>(node.FindChildren("*", "MeshInstance3D", true, false)))
			{
				var b = mesh.GlobalTransform * mesh.GetAabb();
				box = first ? b : box.Merge(b);
				first = false;
			}
			GD.Print($"KIT {asset}: pos={box.Position} tamanho={box.Size}");
			node.QueueFree();
		}
	}
}
