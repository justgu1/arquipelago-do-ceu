# Arquipélago do Céu

Roguelite educativo em 3D isométrico, feito em Godot, para crianças de 6 a 10 anos aprenderem matemática e português jogando.

## O jogo

Uma névoa do esquecimento cobriu as ilhas do céu: criaturas esqueceram seus nomes, máquinas perderam seus números. A criança parte do Lobby e sobe as torres, uma por matéria, para restaurar o que a névoa apagou. A matéria é a mecânica do jogo, não um questionário com desenhos em volta: para vencer, a criança precisa pensar.

- **Torres e Níveis:** a Torre dos Números (Matemática) e a Torre das Letras (Português), com 3 Níveis cada, um por ano escolar (1º ao 3º). Cada Andar é gerado na hora; as portas são Puzzles, e avançar numa torre pede um item achado na outra. Todo Perfil começa no Nível 1, e o Atalho de Nível dá uma única tentativa de vencer direto o Desafio épico.
- **Combate desenhando, sem violência:** a criança responde desenhando em qualquer lugar da tela, com o dedo, o mouse ou a caneta, e o tempo desacelera enquanto ela desenha. Na Matemática a criatura pede um resultado e a criança desenha uma conta que chegue nele, ou responde uma conta pronta. Na Torre das Letras, o Escriba junta sílabas no 1º ano, escreve a palavra no 2º e completa frases no 3º. O acerto dissolve a névoa e liberta a criatura.
- **Errar não pune:** errar custa 1 de Energia, dica nunca custa, e zerar a Energia só leva de volta ao Lobby, sem perder o que foi aprendido.
- **Feito para crianças:** gratuito, sem anúncios, compras, ranking ou castigo por ficar sem jogar. Funciona offline, salva sozinho e só coleta dados com autorização do responsável. Voz em todas as instruções, legendas, modo calmo e fonte para dislexia.

Primeira versão: computador (Windows), matemática e português, 1º ao 3º ano.

## Estrutura

```text
game/          o jogo no Godot: cenas, telas, entrada, voz, dados (game/data) e modelos; é o que vira o executável
core/          as regras em C# puro, sem Godot: combate, geração dos Andares, save, economia
game.tests/    testes automáticos (xUnit) do core e dos dados; rodam sem abrir o Godot e não vão no jogo
content/       missões base em JSON e o schema
tools/         scripts de desenvolvimento (portões de qualidade, geração de dados, otimização de modelos)
docs/
  produto/       o jogo: regras, mundo, combate, conteúdo educacional e escopo do MVP
  tecnico/       o sistema: arquitetura, geração procedural, save, missões e cliente da API (o backend é outro repositório)
  adr/           decisões de arquitetura e de regra
  pedagogia/     ordem recomendada dos Conceitos e habilidades da BNCC
  arte/          direção de arte, HUD, acessibilidade, guia de estilo e referências visuais
  legal/         privacidade (LGPD), ECA Digital e licenças
```

## Ambiente

Para rodar o jogo: .NET SDK 9, Godot 4.7.2 (versão .NET) e Node LTS (validação do schema das missões). A variável `GODOT` deve apontar para o executável de console do Godot. O backend não faz parte deste repositório.

### Como rodar

O projeto Godot está em `game/`. No PowerShell, na raiz do repositório (a variável `GODOT` já aponta para o executável de console):

```powershell
# compilar e abrir o jogo de uma vez (argumentos extras vão para o jogo; o .cmd dispensa mudar a política de scripts do PowerShell)
.\tools\run.cmd

# ou, passo a passo:
# compilar o C#
dotnet build game/ArquipelagoDoCeu.csproj

# abrir o jogo (janela)
& $env:GODOT --path game

# rodar sem janela e sair depois de alguns quadros (teste rápido)
& $env:GODOT --headless --path game --quit-after 30
```

Para rodar todos os testes (não precisam do Godot aberto; a CI roda este mesmo comando a cada pull request):

```powershell
dotnet test game.tests
```

Para editar no Godot, abra a pasta `game/` pelo gerenciador de projetos. A cena inicial, `Main`, monta o Lobby, o Personagem do Perfil, as telas de Perfil, Personagem e configurações, o treino contra o Boneco, o Inventário (equipamentos e Cosméticos) e o HUD no tema Livro. As duas torres são Dungeons jogáveis em greybox: a criança entra pela porta da torre na beira da ilha (a Torre dos Números e a Torre das Letras), anda de Sala em Sala pelas portas (E ou o botão), luta, abre Puzzles, descansa, acha o Fast travel, ganha o Item de troca, passa o Marco, vence o Desafio épico e, com as duas torres concluídas, enfrenta o Desafio mestre no portal roxo do Lobby. Cada Andar guarda o que a criança explorou (a escada vale nos dois sentidos), o mapa mostra névoa e cadeado, e completar um Andar rende um Cosmético no Álbum. Para abrir o jogo com um comando: `.\tools\run.cmd`. Para rodar os portões de qualidade da CI (formatação, análise estática, testes com cobertura, comentários e commits): `tools\qualidade.cmd`.

Controles no Lobby: WASD ou setas (relativo à tela), Shift corre, roda do mouse, pinça ou Page Up e Page Down para o zoom (Home reinicia), F8 compara a capa, clique do mouse ou toque no chão para ir até lá (o caminho contorna árvores e lagoa). Perto do Álbum ou do portal aparece um botão (ou a tecla E) para usar o objeto. Em Configurações dá para mudar tamanho do texto, voz, redução de animações, fonte para dislexia, Modo calmo e as dicas de controle; tudo é gravado no save.

### Piloto de desenvolvimento

O `DevDriver` percorre o jogo como uma criança faria (clica, digita, segura teclas) e grava capturas. Só liga com argumentos de usuário, depois do `--`; rode com janela, não com `--headless`, para as capturas saírem:

```powershell
& $env:GODOT --path game -- --driver=movimento --saida=C:\caminho\da\pasta
```

Cenários: `criar`, `escolher`, `personagem`, `movimento`, `mouse`, `lobby`, `album`, `portal`, `tecla`, `config`, `creditos` (a equipe e as licenças), `animacao` (imprime no console se a animação de andar está ligada), `camera`, `efeitos`, `boneco`, `boneco-dislexia`, `erro`, `energia`, `bolha`, `calmo`, `treino-nivel`, `nome`, `nome-erro`, `nivel2-matematica`, `nivel2-escrita`, `nivel3-frase`, `andar` (desenha o Plano de um Nível), `torre`, `torre-salas`, `torre-letras`, `torre-itens`, `torre-epico`, `torre-atalho`, `torre-atalho-erro`, `torre-fast-travel`, `torre-derrota`, `torre-profunda` (os Andares profundos) e `mestre` (as torres e o Desafio mestre). Em build de depuração, `--seed-do-mundo=N` fixa a seed dos Andares e `--sem-voz` desliga a voz. `--reduzir-animacoes` liga a redução de animações e `--nivel=N` libera os Níveis até N no treino. O piloto trabalha numa cópia do save do usuário, então não mexe nos seus Perfis.

### Teste de ponta a ponta do Nível 1

O roteiro `e2e-3` joga o Nível 1 inteiro, sem janela: o Atalho de Nível numa torre, a outra torre jogada (Descanso, Puzzle, Fast travel, Sala do Item, Marco, Desafio épico) e o Desafio mestre, que libera o Nível 2. O `e2e-4`, num segundo processo, reabre o save e confere o progresso. A CI roda os dois depois do e2e do treino.

```powershell
$save = "$env:TEMPe2e-nivel.json"; Remove-Item $save, "$save.snapshot" -ErrorAction SilentlyContinue
& $env:GODOT --headless --path game -- --driver=e2e-3 --save=$save --seed-do-mundo=1
& $env:GODOT --headless --path game -- --driver=e2e-4 --save=$save --seed-do-mundo=1
```

### Teste de ponta a ponta do treino

O roteiro `e2e-1` cria o Perfil, anda, treina nos dois Papéis e confere que o save não mudou; o `e2e-2`, num segundo processo, reabre o jogo e confere que o Perfil e o progresso são os mesmos. Uma verificação que falha faz o jogo sair com código 1. A CI roda os dois no executável de depuração exportado (job `e2e-windows`). Para rodar aqui, sem janela:

```powershell
$save = "$env:TEMP\e2e-save.json"; Remove-Item $save, "$save.snapshot" -ErrorAction SilentlyContinue
& $env:GODOT --headless --path game -- --driver=e2e-1 --save=$save
& $env:GODOT --headless --path game -- --driver=e2e-2 --save=$save
```

