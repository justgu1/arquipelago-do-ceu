using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.Equipment;
using ArquipelagoDoCeu.World;
using Godot;
using Point = System.Numerics.Vector2;

namespace ArquipelagoDoCeu;

// Um morador do Lobby: passeia por um caminho até um ponto sorteado, descansa um pouco e escolhe outro; à noite vai para perto de um lampião.
// Usa o corpo do Personagem, só que sem equipamento, e não atrapalha a criança.
public partial class Villager : Node3D
{
	const float Pace = 1.1f;

	readonly HexGrid grid;
	readonly IReadOnlyList<HexCell> roads;
	readonly IReadOnlyList<HexCell> gathering;
	readonly CharacterModel model = new();
	readonly RandomNumberGenerator random = new();
	readonly string role;
	readonly string clothes;
	readonly Queue<Point> waypoints = new();
	Point position;
	float rest;
	bool gathered;
	bool dressed;

	public Villager(HexGrid grid, IReadOnlyList<HexCell> roads, IReadOnlyList<HexCell> gathering, HexCell home, string role, string clothes, uint seed)
	{
		this.grid = grid;
		this.roads = roads;
		this.gathering = gathering;
		this.role = role;
		this.clothes = clothes;
		position = HexGrid.ToWorld(home);
		random.Seed = seed;
		rest = random.RandfRange(0.5f, 4f);
		Name = "Villager";
	}

	public bool Walking => waypoints.Count > 0;

	public override void _Ready()
	{
		AddChild(model);
		Place();
	}

	public void Tick(float delta, bool night, bool reduced)
	{
		Dress();
		if (reduced)
		{
			model.SetLocomotion(0f, false);
			return;
		}
		if (night != gathered && waypoints.Count == 0) GoSomewhere(night);
		var moved = Step(delta);
		model.SetLocomotion(moved / Mathf.Max(delta, 0.0001f), false);
		if (waypoints.Count == 0)
		{
			rest -= delta;
			if (rest <= 0f && !night) GoSomewhere(false);
		}
		Place();
	}

	// O corpo só carrega no primeiro quadro; a roupa e a cor entram assim que ele existe.
	void Dress()
	{
		if (dressed || !model.Loaded) return;
		model.Show(new EquipmentLoadout(Role: role));
		model.SetTint("roupa", Color.FromHtml(clothes));
		dressed = true;
	}

	void GoSomewhere(bool night)
	{
		var cells = night ? gathering : roads;
		gathered = night;
		rest = random.RandfRange(3f, 8f);
		if (cells.Count == 0) return;
		var path = grid.FindPath(HexGrid.FromWorld(position), cells[random.RandiRange(0, cells.Count - 1)]);
		if (path is null) return;
		waypoints.Clear();
		foreach (var cell in path.Skip(1)) waypoints.Enqueue(HexGrid.ToWorld(cell));
	}

	float Step(float delta)
	{
		var remaining = Pace * delta;
		var total = 0f;
		while (remaining > 0.0001f && waypoints.Count > 0)
		{
			var toNext = waypoints.Peek() - position;
			var distance = toNext.Length();
			var step = Mathf.Min(distance, remaining);
			if (distance > 0.0001f)
			{
				var direction = toNext / distance;
				position += direction * step;
				Rotation = new Vector3(0f, Mathf.LerpAngle(Rotation.Y, Mathf.Atan2(direction.X, direction.Y), 0.2f), 0f);
			}
			if (step >= distance) waypoints.Dequeue();
			remaining -= step;
			total += step;
		}
		return total;
	}

	void Place() => Position = new Vector3(position.X, 0f, position.Y);
}
