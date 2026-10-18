using Godot;

namespace ArquipelagoDoCeu;

// A criatura 3D da Sala e o movimento dela, feito por código sobre o modelo inteiro: os modelos não têm esqueleto nem clipes.
public sealed class CreatureModel
{
	// Escala no jogo: o Personagem tem uns 1,5; criatura nunca passa de 1,5 nem fica abaixo de 0,8 (guia de estilo, seção Escala).
	const float MinHeight = 0.8f;
	const float MaxHeight = 1.5f;
	const float DefaultHeight = 1.2f;
	static readonly System.Collections.Generic.Dictionary<string, float> Heights = new()
	{
		["pato"] = 1.0f,
		["coruja"] = 1.1f,
		["criaturas/automato-base"] = 1.3f,
		["criaturas/automato-pesado"] = 1.4f,
		["criaturas/automato-voador"] = 1.2f,
		["criaturas/automato-cristal"] = 1.4f,
	};
	const float ReactionSeconds = 0.8f;

	readonly Node3D model;
	readonly float height;
	Vector3 baseScale;
	Vector3 basePosition;
	double clock;
	double since = ReactionSeconds;
	string reaction = "";

	CreatureModel(Node3D model, string name)
	{
		this.model = model;
		height = System.Math.Clamp((Heights.TryGetValue(name, out var preset) ? preset : DefaultHeight), MinHeight, MaxHeight);
		baseScale = model.Scale;
	}

	public Node3D Node => model;

	public bool ReduceMotion { get; set; }

	// O modelo pelo nome combinado (por exemplo "criaturas/automato-base"); nulo se não carregar.
	public static CreatureModel? Load(string name) => Lobby.LoadModel(name) is { } node ? new CreatureModel(node, name) : null;

	// Só depois de entrar na árvore: a escala sai da caixa do modelo no mundo.
	public void Fit()
	{
		Lobby.FitToHeight(model, height, Vector3.Zero);
		baseScale = model.Scale;
		basePosition = model.Position;
	}

	// acerto, erro, libertada ou travessura.
	public void React(string kind)
	{
		reaction = kind;
		since = 0;
	}

	public void Tick(double delta, Vector3 anchor)
	{
		clock += delta;
		since += delta;
		if (ReduceMotion)
		{
			model.Scale = baseScale;
			model.Position = basePosition + anchor;
			model.Rotation = Vector3.Zero;
			return;
		}
		var breath = (float)System.Math.Sin(clock * 2.2);
		var lift = 0.05f * breath;
		var squash = 1f + 0.03f * breath;
		var turn = 0f;
		var sway = 0f;
		var t = (float)System.Math.Min(since / ReactionSeconds, 1.0);
		var live = since < ReactionSeconds;
		if (live && reaction == "hit") squash = 1f - 0.22f * Mathf.Sin(t * Mathf.Pi);
		else if (live && reaction == "miss") sway = 0.12f * Mathf.Sin(t * 6f * Mathf.Pi) * (1f - t);
		else if (live && reaction == "prank") turn = 0.2f * Mathf.Sin(t * 4f * Mathf.Pi) * (1f - t);
		else if (live && reaction == "freed") (lift, turn) = (0.6f * Mathf.Sin(t * Mathf.Pi), Mathf.Tau * t);
		model.Scale = new Vector3(baseScale.X / Mathf.Sqrt(squash), baseScale.Y * squash, baseScale.Z / Mathf.Sqrt(squash));
		model.Position = basePosition + anchor + new Vector3(sway, lift, 0f);
		model.Rotation = new Vector3(0f, turn, 0f);
	}
}
