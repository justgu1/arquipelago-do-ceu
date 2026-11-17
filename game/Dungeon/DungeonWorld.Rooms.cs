using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Generation;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// As Salas montadas em segundo plano: a estrutura de cada Sala (paredes, colunas, mobília, luzes) é montada uma vez, em coordenadas do Andar,
// e guardada; as Salas até `LoadRadius` hexágonos da de agora são montadas aos poucos, uma por quadro, então chegar a uma Sala não engasga.
public partial class DungeonWorld
{
	sealed class RoomBuild
	{
		public required Node3D Structure { get; init; }
		public required Node3D Floor { get; init; }
		public string FloorKey { get; set; } = "";
		public List<RoomWalls.Piece> Pieces { get; } = [];
		public List<Light3D> Lights { get; } = [];
		public List<RoomFurniture.Prop> Props { get; } = [];
		public HashSet<HexCell> Occupied { get; } = [];
	}

	readonly Dictionary<int, RoomBuild> built = [];
	// As Salas guardadas ficam aqui, em coordenadas do Andar: o nó anda para o centro da Sala de agora ficar na origem.
	readonly Node3D absolute = new() { Name = "Salas" };
	readonly Queue<int> ahead = new();
	Floor? shownFloorData;

	// Até onde, em hexágonos a partir da Sala de agora, as Salas são montadas antes de a criança chegar (vem dos dados).
	public int LoadRadius { get; set; } = 24;

	System.Numerics.Vector2 SpotWorld => HexGrid.ToWorld(spot);

	RoomBuild Ensure(Floor floor, int id, Color color, bool cleared, bool withFloor)
	{
		if (!built.TryGetValue(id, out var build))
		{
			build = new RoomBuild { Structure = new Node3D { Name = $"Estrutura{id}", Visible = false }, Floor = new Node3D { Name = $"Chao{id}", Visible = false } };
			absolute.AddChild(build.Structure);
			absolute.AddChild(build.Floor);
			BuildStructure(floor, id, build);
			built[id] = build;
		}
		if (!withFloor) return build;
		var key = $"{color.ToHtml()}|{cleared}";
		if (build.FloorKey == key) return build;
		foreach (var child in build.Floor.GetChildren()) child.QueueFree();
		var placed = layout!.Room(id);
		RoomMeshes.AddFloor(build.Floor, placed.Cells.Select(c => (c, placed.HeightAt(c))), color, cleared, Theme);
		build.FloorKey = key;
		return build;
	}

	void BuildStructure(Floor floor, int id, RoomBuild build)
	{
		var placed = layout!.Room(id);
		var cells = placed.Cells.ToHashSet();
		var heights = placed.Cells.ToDictionary(c => c, placed.HeightAt);
		var doorCells = floor.Neighbors(id).Select(n => layout.CorridorBetween(id, n)![0]).ToHashSet();
		var network = new HashSet<HexCell>(cells);
		foreach (var n in floor.Neighbors(id)) network.UnionWith(layout.CorridorBetween(id, n)!);
		var keep = new HashSet<HexCell>(doorCells);
		keep.UnionWith(doorCells.SelectMany(c => c.Neighbors()).ToList());
		keep.Add(placed.SideSpot);
		var seed = Seed.Mix((ulong)(placed.Origin.Q * 7919 + placed.Origin.R), placed.Form.Id, floor.Number, id, 1);
		var plan = RoomFurnishing.Plan(cells, network, keep, placed.Spot, seed);
		build.Occupied.UnionWith(plan.Select(f => f.Cell));
		build.Pieces.AddRange(RoomWalls.Add(build.Structure, heights, doorCells, Theme, build.Lights));
		build.Pieces.AddRange(RoomFurniture.Add(build.Structure, plan, network, heights, Theme, Theme == DungeonTheme.Letters, build.Props));
		foreach (var piece in build.Pieces) piece.Absolute = true;
	}

	void DropBuilt()
	{
		foreach (var build in built.Values)
		{
			build.Structure.QueueFree();
			build.Floor.QueueFree();
		}
		built.Clear();
		ahead.Clear();
	}

	// Mostra só a Sala de agora e as abertas ao lado; a luz fica só na de agora. As outras ficam guardadas, escondidas.
	void ShowBuilt(IReadOnlySet<int> active, int current)
	{
		foreach (var (id, build) in built)
		{
			var on = active.Contains(id);
			build.Structure.Visible = on;
			build.Floor.Visible = on;
			foreach (var light in build.Lights) light.Visible = id == current;
		}
	}

	// As Salas dentro do raio, da mais perto para a mais longe, entram na fila para serem montadas aos poucos.
	void QueueAhead(int current)
	{
		ahead.Clear();
		var here = layout!.Room(current).Spot;
		foreach (var other in layout.Rooms.Where(r => !built.ContainsKey(r.Room) && r.Spot.DistanceTo(here) <= LoadRadius).OrderBy(r => r.Spot.DistanceTo(here)))
			ahead.Enqueue(other.Room);
		foreach (var id in built.Keys.Where(id => layout.Room(id).Spot.DistanceTo(here) > LoadRadius * 2).ToList())
		{
			built[id].Structure.QueueFree();
			built[id].Floor.QueueFree();
			built.Remove(id);
		}
	}

	void BuildOneAhead()
	{
		if (ahead.Count == 0 || shownFloorData is not { } floor) return;
		var id = ahead.Dequeue();
		if (!built.ContainsKey(id)) Ensure(floor, id, Colors.White, false, withFloor: false);
	}

	public IReadOnlyList<RoomFurniture.Prop> Props =>
		built.TryGetValue(currentId, out var build) ? [.. build.Props.Select(p => p with { Ground = p.Ground - SpotWorld })] : [];

	public string PropPrompt(string id) => Props.FirstOrDefault(p => p.Id == id)?.Prompt + " [E]";

	// A peça dá um pulinho e a criança ouve o que ela é; com "reduzir animações" só a frase.
	public string? UseProp(string id)
	{
		if (!built.TryGetValue(currentId, out var build) || build.Props.FirstOrDefault(p => p.Id == id) is not { } prop) return null;
		if (!ReduceMotion && GodotObject.IsInstanceValid(prop.Model))
		{
			var scale = prop.Model.Scale;
			var bounce = CreateTween();
			bounce.TweenProperty(prop.Model, "scale", scale * 1.12f, 0.12);
			bounce.TweenProperty(prop.Model, "scale", scale, 0.18);
		}
		return prop.Line;
	}
}
