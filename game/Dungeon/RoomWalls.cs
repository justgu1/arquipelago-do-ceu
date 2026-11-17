using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// As paredes e as colunas na borda das Salas, do kit de Dungeon feito no Blender. A câmera olha de um canto: os lados de trás levam parede alta (a Sala parece
// fechada), os da frente parede baixa, para não esconder a criança. Parte das paredes altas tem tocha acesa e uma coluna em sete é um braseiro, e cada um deles ilumina.
public static class RoomWalls
{
	const float LowWall = 0.75f;
	const float HighWall = 2f;
	const float LowPillar = 1f;
	const float HighPillar = 2.2f;
	const float BrazierHeight = 1.85f;
	const float Edge = 1f;
	const float Apothem = 0.866f;
	const float HideRadius = 2f;
	const float FrontFacing = 0.35f;
	const int MaxLights = 8;

	// Uma peça do cenário que some quando esconde alguém. A transparência é do próprio modelo (dithering), então os materiais do tema não mudam.
	public sealed class Piece(Node3D root, float height)
	{
		public Node3D Root { get; } = root;
		// O meio do corpo da peça: a parede tem 2 de altura, e medir pela base a deixava de fora da linha de visão.
		public Vector3 Center { get; } = root.Position + new Vector3(0f, height / 2f, 0f);
		public IReadOnlyList<MeshInstance3D> Meshes { get; } = [.. root.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>()];
		public float Alpha { get; set; } = 1f;
		// As peças das Salas guardadas estão em coordenadas do Andar, e as dos corredores nas da Sala de agora.
		public bool Absolute { get; set; }
	}

	// Uma parede em cada lado de hexágono do chão que dá para o vazio; os corredores e as portas ficam abertos.
	public static IReadOnlyList<Piece> Add(Node3D parent, IReadOnlyDictionary<HexCell, float> floor, IReadOnlySet<HexCell> open, DungeonTheme theme, IList<Light3D> lights)
	{
		var pieces = new List<Piece>();
		var pillars = new HashSet<(int, int)>();
		var count = 0;
		foreach (var (cell, height) in floor)
		{
			var center = HexGrid.ToWorld(cell);
			foreach (var next in cell.Neighbors().Where(n => !floor.ContainsKey(n) && !open.Contains(n)))
			{
				var way = System.Numerics.Vector2.Normalize(HexGrid.ToWorld(next) - center);
				var front = (way.X + way.Y) / 1.4142f > FrontFacing;
				var mid = new Vector3(center.X + way.X * Apothem, height, center.Y + way.Y * Apothem);
				var along = new Vector3(-way.Y, 0f, way.X);
				var angle = Mathf.Atan2(along.X, along.Z) - Mathf.Pi / 2f;
				var torch = !front && Hash(cell, next) % 4 == 0 && count < MaxLights;
				Place(parent, pieces, theme, front ? "cenario/parede-baixa" : torch ? "cenario/parede-tocha" : "cenario/parede-alta", front ? LowWall : HighWall, mid, angle);
				if (torch) { lights.Add(Light(parent, theme, mid + new Vector3(-way.X * 0.5f, 1.6f, -way.Y * 0.5f))); count++; }
				foreach (var end in new[] { mid + along * (Edge / 2f), mid - along * (Edge / 2f) })
				{
					var key = (Mathf.RoundToInt(end.X * 20f), Mathf.RoundToInt(end.Z * 20f));
					if (!pillars.Add(key)) continue;
					var lit = front && Mathf.PosMod(key.Item1 / 7 + key.Item2 / 5, 5) == 0 && count < MaxLights;
					Place(parent, pieces, theme, lit ? "cenario/coluna-braseiro" : "cenario/coluna-dungeon", lit ? BrazierHeight : front ? LowPillar : HighPillar, end, 0f);
					if (lit) { lights.Add(Light(parent, theme, end + new Vector3(0f, 2.3f, 0f))); count++; }
				}
			}
		}
		return pieces;
	}

	// Pilares do meio da Sala (e a mobília): entram na lista que some quando esconde alguém.
	public static Piece? Place(Node3D parent, IList<Piece> pieces, DungeonTheme theme, string model, float height, Vector3 at, float yaw, bool themed = true)
	{
		if (RoomProps.Add(parent, model, height, at, yaw) is not { } node) return null;
		if (themed) theme.Tint(node);
		var piece = new Piece(node, height);
		pieces.Add(piece);
		return piece;
	}

	// Uma luz quente de tocha ou braseiro: sem sombra, para custar pouco no celular.
	static OmniLight3D Light(Node3D parent, DungeonTheme theme, Vector3 at)
	{
		var light = new OmniLight3D { LightColor = Color.FromHtml(theme.Flame), LightEnergy = 1.3f, OmniRange = 5.5f, ShadowEnabled = false, Position = at };
		parent.AddChild(light);
		return light;
	}

	static int Hash(HexCell a, HexCell b) => unchecked(a.Q * 73856093 ^ a.R * 19349663 ^ b.Q * 83492791 ^ b.R * 2654435) & int.MaxValue;

	// Só as peças que escondem alguém (na linha de visão da câmera, entre ela e a criança ou a criatura) somem; as outras voltam.
	public static void Fade(IReadOnlyList<Piece> pieces, Vector3 forward, IReadOnlyList<Vector3> seen, float delta, Vector3 offset)
	{
		foreach (var piece in pieces)
		{
			if (!GodotObject.IsInstanceValid(piece.Root)) continue;
			var hides = seen.Any(target => Hides(piece.Absolute ? piece.Center - offset : piece.Center, target, forward));
			var alpha = Mathf.MoveToward(piece.Alpha, hides ? 0.05f : 1f, delta * 8f);
			if (Mathf.IsEqualApprox(alpha, piece.Alpha)) continue;
			piece.Alpha = alpha;
			foreach (var mesh in piece.Meshes) mesh.Transparency = 1f - alpha;
		}
	}

	// A câmera é ortogonal: todos os raios seguem `forward`, então a peça esconde quem está atrás dela a menos de um raio de distância da linha.
	static bool Hides(Vector3 piece, Vector3 target, Vector3 forward)
	{
		var between = piece - target;
		var along = between.Dot(forward);
		return along < 0f && (between - forward * along).Length() < HideRadius;
	}
}
