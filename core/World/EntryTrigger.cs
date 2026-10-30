using System.Numerics;

namespace ArquipelagoDoCeu.World;

// A soleira de um portal ou de uma porta de torre: passar por ela ativa o objeto uma vez, e só ativa de novo depois de a criança se afastar.
public sealed class EntryTrigger(float enterRadius, float leaveRadius)
{
	readonly HashSet<string> inside = [];

	// O id do objeto cuja soleira o Personagem acabou de pisar, se houve; os que ele já estava pisando não repetem.
	public string? Step(Vector2 player, IEnumerable<Interactable> thresholds)
	{
		string? entered = null;
		foreach (var item in thresholds)
		{
			var distance = Vector2.Distance(player, item.Position);
			if (distance > leaveRadius) inside.Remove(item.Id);
			else if (distance <= enterRadius && inside.Add(item.Id)) entered ??= item.Id;
		}
		return entered;
	}

	// Quem já está na soleira (ao voltar de uma tela, por exemplo) não reativa até sair e voltar.
	public void Settle(Vector2 player, IEnumerable<Interactable> thresholds)
	{
		foreach (var item in thresholds)
			if (Vector2.Distance(player, item.Position) <= enterRadius) inside.Add(item.Id);
	}
}
