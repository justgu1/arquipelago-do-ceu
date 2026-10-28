using System.Collections.Generic;
using System.Linq;
using ArquipelagoDoCeu.World;
using Godot;

namespace ArquipelagoDoCeu;

// O vilarejo da ilha principal: casas, moinho, poço e canteiros nos lugares do VillagePlan, virados para a câmera (a porta aparece), com chaminés que soltam fumaça
// e pás de moinho que giram (quem anima é o LobbyLife).
public partial class Lobby
{
	// Altura de cada peça no jogo e o raio da parte dura (o resto do hexágono fica livre para andar).
	static readonly Dictionary<VillagePiece, (string Model, float Height, float Body)> Pieces = new()
	{
		[VillagePiece.CasaA] = ("vilarejo/casa-a", 2.7f, 0.95f),
		[VillagePiece.CasaB] = ("vilarejo/casa-b", 3.1f, 0.9f),
		[VillagePiece.Moinho] = ("vilarejo/moinho", 3.6f, 0.85f),
		[VillagePiece.Poco] = ("vilarejo/poco", 2.0f, 0.6f),
		[VillagePiece.Canteiro] = ("vilarejo/canteiro", 0.55f, 0.6f),
	};

	// A câmera olha do canto sudeste: a frente (+Z do modelo) vira para lá.
	const float TowardCamera = Mathf.Pi / 4f;

	IReadOnlyList<VillageSpot>? villageSpots;

	// Onde sai fumaça e o que gira, em coordenadas do mundo; o LobbyLife lê isto ao nascer.
	internal List<Vector3> Chimneys { get; } = new();
	internal List<(Node3D Node, Vector3 Axis, float Speed)> Spinners { get; } = new();

	IReadOnlyList<VillageSpot> VillageSpots => villageSpots ??= VillagePlan.Build(
		map.Main.Where(c => !plan.Lake.Contains(c)),
		plan.Path,
		plan.Trees.Concat(Rules.Islands.Select(i => new HexCell(i.FromQ, i.FromR))).Concat(LampCells),
		Rules.Objects.Select(o => new HexCell(o.Q, o.R)));

	void AddVillage()
	{
		foreach (var spot in VillageSpots)
		{
			var (name, height, _) = Pieces[spot.Piece];
			if (LoadModel(name) is not { } model) continue;
			var ground = HexToWorld(spot.Cell.Q, spot.Cell.R);
			AddChild(model);
			model.Rotation = new Vector3(0f, TowardCamera, 0f);
			FitToHeight(model, height, ground);
			if (spot.Piece != VillagePiece.Canteiro) occluders.Add(model);
			if (spot.Piece == VillagePiece.CasaA) Chimneys.Add(model.ToGlobal(new Vector3(0.6f, 2.65f, -0.4f)));
			if (spot.Piece == VillagePiece.CasaB) Chimneys.Add(model.ToGlobal(new Vector3(0.45f, 2.75f, -0.35f)));
			if (spot.Piece == VillagePiece.Moinho) AddSails(model);
		}
	}

	// As pás ficam num pivô no cubo do moinho, de frente para o mesmo lado que ele; o jogo gira o pivô em volta do eixo dele.
	void AddSails(Node3D mill)
	{
		if (LoadModel("vilarejo/moinho-pas") is not { } sails) return;
		var pivot = new Node3D { Name = "MoinhoPas", Rotation = mill.Rotation, Position = mill.ToGlobal(new Vector3(0f, 2.55f, 0.82f)) };
		AddChild(pivot);
		sails.Scale = mill.Scale;
		pivot.AddChild(sails);
		Spinners.Add((sails, Vector3.Back, 0.5f));
	}

	// Cada peça bloqueia só o círculo dela, no hexágono onde está.
	IEnumerable<(HexCell Cell, float Radius)> VillageObstacles() => VillageSpots.Select(s => (s.Cell, Pieces[s.Piece].Body));
}
