# Inventário das criaturas do jogo (ticket #229)

Levantamento em 17/11/2026, branch `feat/modelos-das-dungeons-e-ajustes-de-jogo`, somente leitura. Legenda: **[F]** fato verificado no repositório; **[I]** inferência minha.

## Resumo do que existe

- **[F]** Há **7 modelos de criatura** em `.glb`: 6 em `game/assets/modelos/criaturas/` (besouro, coruja, mapa, pato, sapo, tartaruga) e o Boneco em `game/assets/modelos/boneco.glb`. Cada um tem script em `tools/blender/criaturas/*.py` (`tools/blender/LEIA-ME.md:5-15`).
- **[F]** Nenhum modelo tem esqueleto nem clipes. O movimento é feito por código no Godot sobre o modelo inteiro (`game/Dungeon/CreatureModel.cs:5`, `:60-100`; `docs/arte/guia-de-estilo-visual.md:144`).
- **[F]** As **4 folhas de conceito** (`docs/arte/folhas/23` a `26`) mostram **24 criaturas planejadas** (6 por folha). Só 5 delas têm modelo: coruja-ábaco, besouro, tartaruga, pato e sapo. O Mapa tem modelo, mas não aparece em nenhuma folha.
- **[F]** O jogo usa 3 criaturas por torre, sorteadas pela posição da Sala (`DungeonRun.Menu.cs:7-9`; `DungeonWorld.cs:109`). Não há lista de criaturas por Nível nem por Andar profundo nos dados. O que aparece em cada Nível é decidido só pelo Conceito (ver "Nível" abaixo).

## Como o jogo escolhe a criatura

- **[F] Torre dos Números** (`matematica`): `tartaruga`, `besouro`, `coruja`. Escolhe `CreatureSet[roomId % 3]` (`DungeonRun.Menu.cs:7-8`; `DungeonWorld.cs:109`).
- **[F] Torre das Letras** (`portugues`): `mapa`, `pato`, `sapo`. Mesma regra, mas se a Missão tem `params.image` e esse nome está no conjunto, ele manda (`DungeonWorld.cs:98-110`). Fora do conjunto vale o **Boneco** como substituto.
- **[F]** A Torre das Letras tem 71 palavras diferentes em `params.image` nas Missões (`game/content/missions/**`), mas só 3 delas têm modelo (pato, sapo, mapa). As outras 68 aparecem como Boneco.
- **[F]** A Matemática não usa `image`. O modelo vem só do sorteio por Sala, sem ligação com o Conceito.
- **[F]** O Boneco (`id: boneco`, `rules.json:439-442`) é a criatura do treino no Lobby. Também é o Guardião do Nível no Desafio mestre (`GLOSSARY.md:168-169`) e o substituto quando falta modelo.

## Tabela: criaturas com modelo (existentes)

Colunas fixas: **Animação no jogo** = só a procedural de `CreatureModel` (respirar, achatar no acerto, balançar no erro, travessura, libertada, investida, pulo, giro, estonteada, cansaço; `CreatureModel.cs:60-100`). **Variações** = nenhuma cor nem tamanho alternativo no código; o tamanho é fixo por criatura (`CreatureModel.cs:12-20`) com limite de 0,8 a 1,5.

| Criatura | Tipo | Torre | Nível | .glb | Altura no jogo | Conceito ligado | Fonte |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Coruja-ábaco | Aves (folha 23) | Números | Qualquer Nível: sorteio por Sala [F]; sem dado por Nível [F] | Sim (`criaturas/coruja.glb`) | 1,3 | Nenhum direto [F] | `game/Dungeon/CreatureModel.cs:19`; `DungeonRun.Menu.cs:8`; `tools/blender/criaturas/coruja.py:1` |
| Besouro de engrenagens | Mecânicas (folha 24) | Números | idem | Sim | 1,0 | Nenhum direto [F] | `CreatureModel.cs:18`; `DungeonRun.Menu.cs:8`; `besouro.py:1` |
| Tartaruga-régua | Mecânicas (folha 24) | Números | idem | Sim | 1,0 | Nenhum direto [F] | `CreatureModel.cs:17`; `DungeonRun.Menu.cs:8`; `tartaruga.py:1` |
| Pato | Animais (folha 25, origami no script) | Letras | Palavra "pato" aparece em Missões do 1º, 2º e 3º ano (7 Missões: `por-let-002`, `por-seg-001`, `por-dis-001` no 1º; `por-troca-001` no 2º; `por-adj-003`, `por-ss-001`, `por-sv-003` no 3º) [F] | Sim | 1,0 | `nome-e-som-das-letras`, `segmentacao-silabica-oral`, `palavras-dissilabas-grupo-1`, `trocar-silaba-palavra-nova`, `adjetivo`, `ortografia-s-ss`, `substantivo-e-verbo` | `CreatureModel.cs:15`; `DungeonRun.Menu.cs:9`; `pato.py:1`; Missões em `game/content/missions/` |
| Sapo | Animais (folha 25) | Letras | Missão `por-esc-003`, `year: 2` (2º ano) [F] | Sim | 0,9 | `escrever-palavras-silabas-ja-dominadas` | `CreatureModel.cs:16`; `game/content/missions/escrever-palavras-silabas-ja-dominadas/por-esc-003.json:10`; `sapo.py:1` |
| Mapa | Objetos (não está na folha 26) | Letras | Palavra "mapa" em 3 Missões do 1º ano (`por-let-001`, `por-dis-002`, `por-seg-002`) [F] | Sim | 1,2 | `nome-e-som-das-letras`, `palavras-dissilabas-grupo-1`, `segmentacao-silabica-oral` | `CreatureModel.cs:14`; `DungeonRun.Menu.cs:9`; `mapa.py:1` |
| Boneco de treino | Objetos (substituto e treino) | Lobby; também substituto nas duas torres e Guardião do Nível | Todos | Sim (`modelos/boneco.glb`) | 1,2 (padrão) | Nenhum (treino não conta progresso) | `rules.json:439-442`; `GLOSSARY.md:29,168`; `boneco.py:1`; `DungeonWorld.cs:109-115` |

Notas dessa tabela:
- **[F]** Os scripts são a fonte dos modelos e o `.blend` não vai para o repositório (`pipeline-de-arte.md:10`).
- **[I]** A coruja é "ábaco" porque o script diz "asas abertas com varetas e contas" e a folha 23 mostra essa variação no topo do meio.
- **[F]** O Pato e o Sapo do script são de origami (`pato.py:1`, `sapo.py:1`), mas a folha 25 desenha ambos como bichinhos macios. O modelo difere da folha e segue a decisão posterior ("padrão de inimigos": objetos com punhos e tênis, animais de origami com patas; está na memória do projeto, não em `docs/`) [F, fora do repo].

## Tabela: criaturas planejadas nas folhas 23 a 26

Todas sem `.glb`, sem animação própria e sem variação definida em dados, exceto as marcadas "tem modelo". A leitura das imagens é minha [I]; os nomes de folha vêm dos arquivos (`docs/arte/folhas/23-criaturas-numeros-aves.jpg` etc.).

### Folha 23, Números, Aves (6 corujas) [I: leitura da imagem]

A folha inteira é uma única espécie (coruja de latão e roxo, escudo liso na barriga) em 6 variações de peça. Não há nome escrito na folha; os nomes abaixo são descrições minhas.

| # | Variação (descrição) | .glb |
| --- | --- | --- |
| 1 | Coruja base, escudo liso | Não |
| 2 | Coruja-ábaco, asas abertas com varetas e contas | **Sim** (`coruja.glb`) |
| 3 | Coruja com monóculo de latão (tampa de olho com parafusos) | Não |
| 4 | Coruja pequena com hélice na cauda | Não |
| 5 | Coruja com relógio sem algarismos de escudo | Não |
| 6 | Coruja com duas engrenagens grandes nas asas | Não |

### Folha 24, Números, Mecânicas (6 animais mecânicos) [I]

| # | Criatura | .glb |
| --- | --- | --- |
| 1 | Besouro de engrenagens, escudo redondo | **Sim** |
| 2 | Caranguejo com pinças e pernas de compasso | Não |
| 3 | Tartaruga-régua (casco com régua) | **Sim** |
| 4 | Caracol com casca de engrenagem roxa | Não |
| 5 | Gato fofo com cauda de contas de ábaco e escudo | Não |
| 6 | Urso de armadura de latão com escudo | Não |

### Folha 25, Letras, Animais (6) [I]

| # | Criatura | Palavra provável | .glb |
| --- | --- | --- | --- |
| 1 | Pato | pato | **Sim** |
| 2 | Bode com chifres azuis | bode (citado no guia de estilo) | Não |
| 3 | Sapo | sapo | **Sim** |
| 4 | Gato felpudo | gato | Não |
| 5 | Criança loira baixinha | fada (palavra citada nas ordens; a figura não tem asas, dúvida) | Não |
| 6 | Burro com sela azul | mula (citada no guia de estilo) | Não |

### Folha 26, Letras, Objetos (6) [I]

| # | Criatura | Palavra provável | .glb |
| --- | --- | --- | --- |
| 1 | Bota com cadarço azul | bota | Não |
| 2 | Bola com faixas azuis | bola | Não |
| 3 | Mala com fivelas | mala | Não |
| 4 | Nave de papel azul | nave | Não |
| 5 | Cacho de uvas | uva | Não |
| 6 | Ovo rachado com folhinhas | ovo | Não |

Fontes das palavras: `docs/arte/guia-de-estilo-visual.md:70` ("pato, bode, sapo, gato, fada, mula, bota, bola, mala, nave, uva, ovo são exemplos da ordem recomendada provisória") e `docs/pedagogia/ordem-recomendada-mvp.md:184`. **[F]** que a lista bate com as 12 palavras das folhas 25 e 26; **[I]** o casamento figura com palavra.

## Criaturas previstas em outros documentos (sem folha, sem modelo)

- **[F]** Folha de pedidos (histórico do Meshy, 18/10/2026; as peças foram removidas do pipeline): `automato-base`, `automato-pesado` (com relógio), `automato-voador` (com hélice), `automato-cristal` (prioridade B), `papel-fantasma`, `livro-com-pernas`, `pergaminho`, `tinta-roxa` (prioridade B). Total 8, todas sem modelo hoje (`docs/agents/arte/folha-de-pedidos.md:28-35`; `pipeline-de-arte.md:7`: nenhum modelo do Meshy está em uso). O arquivo diz que a lista "continua valendo como o que refazer no Blender". **[I]** Isso conflita com as folhas 23 a 26 (autômatos genéricos e criaturas de papel contra animais e aves); o guia de estilo de hoje (seção 5) segue as folhas, não a folha de pedidos.
- **[F]** Documento de produto: Números têm "autômatos com escudos numéricos"; Letras têm "criaturas feitas de letras apagadas, sem nome" (`documento-de-produto.md:34-35`). Está desatualizado em relação ao guia de estilo, que proíbe letras no modelo (`guia-de-estilo-visual.md:65`).

## Variações (cor, tamanho)

- **[F]** Cor: nenhuma criatura tem variação de cor em dados ou código. O guia de estilo diz que a Torre dos Números é "latão, roxo e dourado" e a das Letras "papel e pergaminho com acentos verde e azul-céu" (`guia-de-estilo-visual.md:68-70`).
- **[F]** Tamanho: "Criaturas de Nível mais alto são maiores e têm mais peças, mas a ação não fica mais difícil" (`guia-de-estilo-visual.md:68`). **Mas no código o tamanho é fixo por criatura**; nenhum tamanho depende do Nível (`CreatureModel.cs:12-20`, limites 0,8 e 1,5 em `:8-9`). Ou seja, é intenção de arte ainda não implementada [I].
- **[F]** Tabela de escala do guia: "pato 1,0; coruja 1,1; autômatos 1,2 a 1,4" (`guia-de-estilo-visual.md:129`). A coruja no código é 1,3, não 1,1 (discrepância).
- **[I]** As variações da folha 23 (6 corujas) sugerem que "Aves" são uma espécie com peças trocáveis, que poderia ser uma única página com 6 variantes no Álbum, ou 6 páginas.

## Conceito educacional ligado

- **[F] Letras:** a criatura é a própria palavra-alvo; o nome escondido é a resposta (`DungeonWorld.cs:98-110`; `guia-de-estilo-visual.md:70`; `documento-de-produto.md:106-108`). Ligação por `params.image` na Missão, e o conceito vem do campo `concept` da Missão. Há 71 palavras em Missões, 3 com modelo.
- **[F] Números:** nenhuma criatura é ligada a Conceito. O Conceito vem da Trilha do Andar, a criatura é só visual (`conceitos.json`, `DungeonRun.Menu.cs:7`). As Mecânicas da Torre são `compor_numero` e `pagar_troco` (`torres.json:6-9`).
- **[F]** Não existe lista de criaturas nem Nível em `torres.json`, `conceitos.json` nem `trilhas.json` (`trilhas.json` guarda só os glifos do reconhecedor de desenho). O Nível das criaturas das Letras só se sabe pelo campo `year` das Missões.

## Palavras de Missão (71) que não têm modelo

amigo aqui ave banco barco bata blusa bola bomba bota bule cachorro campo carro carta casa cola copo criatura cubo dente dia ema escola faca fada festa flor fruta gato grama jato leite limao lobo maca mala mao massa mata mesa meta moto nata ovo pano parque pasta pata pena pente pipa porta pote prato puma queijo quero rato suco tapete tatu terra tia tomate trem uva vaca. (Com modelo: pato, sapo, mapa.) **[F]** Lista tirada de `params.image` nas 101 Missões que têm `image`; algumas dessas palavras não são coisas (amigo, aqui, quero, tia, criatura), **[I]** então não viram criatura.

## Totais

Contagem de **criaturas distintas planejadas nas folhas 23 a 26** (24 figuras):

| Categoria | Folha | Torre | Planejadas | Com modelo | Sem modelo |
| --- | --- | --- | --- | --- | --- |
| Aves | 23 | Números | 6 (variações de 1 espécie, coruja) | 1 | 5 |
| Mecânicas | 24 | Números | 6 | 2 (besouro, tartaruga) | 4 |
| Animais | 25 | Letras | 6 | 2 (pato, sapo) | 4 |
| Objetos | 26 | Letras | 6 | 0 | 6 |
| **Total** | | | **24** | **5** | **19** |

Por torre: Números 12 planejadas (3 com modelo); Letras 12 planejadas (2 com modelo). Fora das folhas: o Mapa (Letras, Objetos) tem modelo; o Boneco (Lobby) tem modelo. **Total de modelos existentes: 7** (3 Números, 3 Letras com o Mapa, 1 Lobby). **[F]** As categorias Aves, Animais, Mecânicas e Objetos vêm dos nomes dos arquivos das folhas, não de um termo do GLOSSARY.

Por Nível: **[F] sem dado** por Nível; as criaturas não são amarradas a Nível, só a Conceito (Letras) ou a sorteio (Números). Por Andares profundos: **[F] sem dado** de criatura própria.

## Lacunas

1. **Planejadas sem modelo (19):** 5 corujas (base, monóculo, hélice, relógio, engrenagens), caranguejo, caracol, gato mecânico, urso; bode, gato, fada, mula; bota, bola, mala, nave, uva, ovo.
2. **Modelos sem uso na folha:** o Mapa e o Boneco (não estão em 23 a 26).
3. **Modelos com uso limitado:** todos os 6 são usados, mas só 3 por torre e repetidos por Sala; nenhuma criatura planejada além delas aparece no jogo.
4. **Palavra com modelo vs. palavra de Missão:** 68 das 71 palavras de Missão caem no Boneco.
5. **Folha de pedidos desatualizada:** 8 itens (autômatos genéricos, papel fantasma, livro, pergaminho, tinta) que não batem com as folhas 23 a 26.
6. **Docs desalinhados:** documento de produto fala em "letras apagadas" e o guia de estilo proíbe letras no modelo; escala da coruja (1,1 no guia, 1,3 no código).
7. **Animação:** nenhuma criatura tem clipes; todas só têm a procedural comum. Não há animação por espécie.
8. **Álbum:** o código de Álbum existe como objeto do Lobby (`rules.json:399-404`, modelo `atril`), mas o conteúdo "criaturas" é "vazio por enquanto" e "sem código" (`documento-de-produto.md:245,258`). Cada criatura libertada vira uma carta, com a criatura e o desafio na frente e a explicação no verso (`documento-de-produto.md:201`).

## Dúvidas só do usuário

1. O Álbum mostra **criaturas** (espécies/modelos) ou **cartas por Missão libertada** (como diz o produto na linha 201)? Isso muda o número de páginas de 7 ou 24 para dezenas (71 palavras) ou centenas de cartas.
2. Entram no livro só as criaturas **com modelo**, ou também as **planejadas** (silhuetas bloqueadas)? E o Boneco entra?
3. As 6 corujas da folha 23 são **uma espécie com variações** ou 6 criaturas? E as 12 palavras das folhas 25/26 (bode, mula, fada...) estão confirmadas como criaturas, com a fada sendo uma criança?
4. Vale a **folha de pedidos** (autômato-base, papel-fantasma...) ou foi substituída pelas folhas 23 a 26?
5. O Nível em que cada criatura aparece vai ser **definido** (hoje não há dado)? A regra "Nível mais alto é maior, com mais peças" será implementada como variantes por Nível?
6. O **Mapa** (sem folha) permanece como criatura de Objetos? E pato e sapo seguem de origami ou voltam ao desenho macio da folha 25?
7. Na Matemática, o Álbum deve ligar criatura a Conceito (hoje não há ligação)?
