namespace ArquipelagoDoCeu.Data;

// Um componente de terceiros que vai no jogo. `File` aponta para o texto da licença que acompanha o jogo, quando há um.
public sealed record CreditedComponent(string Name, string? Author, string License, string? File);

// Quem fez o jogo e o que ele usa de terceiros, com as licenças que precisam acompanhá-lo.
public sealed class Credits
{
	const string LicenseFolder = "licencas/";

	Credits(IReadOnlyList<string> team, IReadOnlyList<CreditedComponent> components)
	{
		Team = team;
		Components = components;
	}

	public IReadOnlyList<string> Team { get; }
	public IReadOnlyList<CreditedComponent> Components { get; }

	public static Result<Credits> Parse(string json)
	{
		var read = CatalogJson.Deserialize<FileDto>(json, "O arquivo de créditos");
		if (!read.IsOk) return Result<Credits>.Fail(read.Error!);
		var dto = read.Value!;
		if (dto.Team is not { Count: > 0 } || dto.Team.Any(string.IsNullOrWhiteSpace)) return Result<Credits>.Fail("Os créditos precisam do nome de cada pessoa da equipe.");
		if (dto.Components is not { Count: > 0 }) return Result<Credits>.Fail("Os créditos não trazem nenhum componente.");

		var components = new List<CreditedComponent>();
		foreach (var c in dto.Components)
		{
			if (string.IsNullOrWhiteSpace(c.Name) || string.IsNullOrWhiteSpace(c.License)) return Result<Credits>.Fail("Todo componente dos créditos precisa de nome e licença.");
			if (c.File is not null && !InLicenseFolder(c.File)) return Result<Credits>.Fail($"A licença de {c.Name} precisa ser um .txt em {LicenseFolder}.");
			components.Add(new CreditedComponent(c.Name, c.Author, c.License, c.File));
		}
		if (CatalogJson.Duplicate(components, c => c.Name) is { } repeated) return Result<Credits>.Fail($"O componente {repeated} aparece duas vezes nos créditos.");
		return Result<Credits>.Ok(new Credits(dto.Team, components));
	}

	// O jogo abre esse arquivo pelo caminho que vem de dados; nada fora da pasta de licenças.
	static bool InLicenseFolder(string file) =>
		file.StartsWith(LicenseFolder, StringComparison.Ordinal) && file.EndsWith(".txt", StringComparison.Ordinal) && !file.Contains("..", StringComparison.Ordinal);

	sealed record FileDto(List<string>? Team, List<ComponentDto>? Components);

	sealed record ComponentDto(string? Name, string? Author, string? License, string? File);
}
