# Créditos

## Motor e bibliotecas que vão no jogo

- **Godot Engine 4.7** (https://godotengine.org), sob a **licença MIT**, com os componentes de terceiros que o próprio motor embute (FreeType, ENet, mbedTLS e outros, cada um com a sua licença). O jogo distribuído precisa levar o aviso de copyright e a licença do Godot e desses componentes; o texto completo sai de `Engine.GetLicenseText()` e `Engine.GetCopyrightInfo()`. **Pendente:** mostrar esse aviso numa tela de créditos ou num arquivo ao lado do executável.
- **.NET 8** (runtime e bibliotecas base), da .NET Foundation, sob a **licença MIT**, com os avisos de terceiros que acompanham o runtime.
- **GodotSharp**, a ponte C# do Godot, sob a **licença MIT**.
- **JsonSchema.Net 9.4.1**, **JsonPointer.Net** e **Json.More.Net**, de Greg Dennis (https://github.com/json-everything/json-everything). O código-fonte é **MIT**; o pacote compilado do NuGet vem sob o *Open Source Maintenance Fee Agreement*, que cobra uma taxa só de quem usa a biblioteca em atividade com receita anual de US$ 10 mil ou mais. Abaixo disso, ou compilando a partir do código-fonte, não há taxa. **Reavaliar antes de qualquer uso comercial do jogo.**
- **Humanizer.Core 2.14.1** (dependência do JsonSchema.Net), sob a **licença MIT**.

## Fonte para dislexia (`game/assets/fontes/`)

**OpenDyslexic Regular**, de Abbie Gonzalez (https://opendyslexic.org), sob a **SIL Open Font License 1.1**, que permite usar e distribuir a fonte junto com o jogo, desde que o aviso e a licença acompanhem (`OFL-OpenDyslexic.txt`). Baixada de https://github.com/antijingoist/opendyslexic.

## Modelos 3D (`game/assets/modelos/`)

Desde 23/10/2026 os modelos novos são feitos no Blender por script, neste repositório (ADR 0009). Os que ainda vieram do Meshy saem um por um, conforme cada substituto fica pronto.

Os modelos atuais foram gerados com a **Meshy** (https://www.meshy.ai) pela API de Image to 3D, a partir de folhas de referência também geradas lá (`docs/arte-e-ux/meshy/`), e otimizados com `tools/otimizar-modelos`: o Personagem com rig e animações básicas de andar, correr e ficar parado, o Guia, o Boneco de treino, o portal, o atril, o grupo de árvores, as duas torres, os marcos do Lobby, o kit do chão e da ponte (`lote02`), a coruja, o pato, as criaturas, o cenário das Salas, os obstáculos e os itens.

**A licença de uso desses modelos depende do plano da conta da Meshy** (o uso comercial e a atribuição variam por plano). **Confirmar os termos antes de qualquer distribuição pública do jogo**, ou trocar todos antes dela.

## Ferramentas de desenvolvimento (não vão no jogo)

- Analisadores **Roslynator** (Apache 2.0) e **SonarAnalyzer.CSharp** (LGPL 3.0), só no build.
- **xUnit**, **coverlet** e **Microsoft.NET.Test.Sdk**, só nos testes.
- **glTF Transform** (MIT) e **sharp** (Apache 2.0), em `tools/otimizar-modelos`.
- Skills de agente em `.claude/skills/`, de Matt Pocock, sob a **licença MIT** (`LICENSE-mattpocock-skills`).
