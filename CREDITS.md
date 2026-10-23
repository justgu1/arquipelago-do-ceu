# Créditos

O jogo mostra estes créditos em Configurações > Jogo > Créditos, com o texto inteiro de cada licença. O conteúdo da tela fica em `game/data/creditos.json` e os textos das licenças em `game/licencas/`, que vão junto no jogo exportado.

## Equipe

- Guilherme Santos
- Rudan Borges

## Motor e bibliotecas que vão no jogo

- **Godot Engine 4.7** (https://godotengine.org), sob a **licença MIT**, com os componentes de terceiros que o próprio motor embute (FreeType, ENet, mbedTLS e outros, cada um com a sua licença). A tela de Créditos mostra o texto que o motor fornece (`Engine.GetLicenseText()`, `Engine.GetCopyrightInfo()` e `Engine.GetLicenseInfo()`), sempre da versão em uso.
- **.NET 8** (runtime e bibliotecas base), da .NET Foundation, sob a **licença MIT** (`game/licencas/dotnet.txt`), com os avisos de terceiros do runtime (`game/licencas/dotnet-avisos-de-terceiros.txt`). A ponte C# do Godot (GodotSharp) faz parte do motor e segue a licença dele.
- **JsonSchema.Net 9.4.1**, **JsonPointer.Net** e **Json.More.Net** (https://github.com/json-everything/json-everything), sob a **licença MIT** (`game/licencas/json-everything.txt`). O pacote compilado do NuGet vem com o *Open Source Maintenance Fee Agreement*, que cobra uma taxa só de quem usa a biblioteca em atividade com receita anual de US$ 10 mil ou mais; abaixo disso, ou compilando a partir do código-fonte, não há taxa. **Reavaliar antes de qualquer uso comercial do jogo.**
- **Humanizer 2.14.1**, dependência do JsonSchema.Net, sob a **licença MIT** (`game/licencas/humanizer.txt`).

## Fonte para dislexia

**OpenDyslexic Regular** (`game/assets/fontes/`), de Abbie Gonzalez (https://opendyslexic.org), sob a **SIL Open Font License 1.1**, que permite usar e distribuir a fonte junto com o jogo, desde que o aviso e a licença acompanhem (`game/licencas/opendyslexic-ofl.txt`).

## Arte

Modelos, texturas, ilustrações e telas de referência são da equipe.
