namespace ArquipelagoDoCeu;

// Como o jogo chama a criatura que a criança enfrenta (o Boneco no Lobby, uma criatura da névoa nas Dungeons), com o artigo
// e o pronome certos.
public sealed record CreatureWords(string The, string Of, string Pronoun, string Title, bool Moves = true)
{
	public static readonly CreatureWords Dummy = new("o Boneco", "do Boneco", "lo", "Boneco de treino");

	// No Puzzle o alvo é uma porta ou ponte trancada, sem criatura.
	public static readonly CreatureWords Gate = new("a porta", "da porta", "la", "Porta trancada", Moves: false);

	// No Desafio mestre o Boneco vira o Guardião do Nível.
	public static readonly CreatureWords Guardian = new("o Guardião", "do Guardião", "lo", "Guardião do Nível");

	public static readonly CreatureWords Wild = new("a criatura", "da criatura", "la", "Criatura da névoa");

	// "o Boneco" no começo de uma frase.
	public string Start => char.ToUpperInvariant(The[0]) + The[1..];
}
