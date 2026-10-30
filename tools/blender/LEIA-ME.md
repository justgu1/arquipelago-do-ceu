# Modelos feitos no Blender por script

Cada criatura do jogo tem o **seu próprio script** em `criaturas/` (um arquivo por criatura). Os scripts são a fonte dos modelos: para mudar uma cor ou uma proporção, muda-se o script e refaz-se o modelo, em vez de mexer no `.blend`.

| Script | Criatura | Tipo |
| --- | --- | --- |
| `criaturas/mapa.py` | Mapa (Torre das Letras) | objeto, com punhos fechados e tênis |
| `criaturas/pato.py` | Pato (Torre das Letras) | animal de origami, com patas |
| `criaturas/sapo.py` | Sapo (Torre das Letras) | animal de origami, com patas |
| `criaturas/tartaruga.py` | Tartaruga-régua (Torre dos Números) | autômato terrestre de latão, com escudo liso |
| `criaturas/besouro.py` | Besouro de engrenagens (Torre dos Números) | autômato terrestre de seis pernas, com escudo liso |
| `criaturas/coruja.py` | Coruja-ábaco (Torre dos Números) | autômato voador, asas de ábaco, com escudo liso |
| `criaturas/boneco.py` | Boneco de treino (Lobby) | objeto: saco de juta remendado, palha, placa em branco, com punhos fechados e tênis; sai em `modelos/boneco.glb` |

Os seis modelos são os que o jogo usa, em `game/assets/modelos/criaturas/<nome>.glb`.

## Modelos do Lobby

Os props do Lobby ficam em `lobby/` (um script por modelo, com o bloco comum das criaturas). Cada script termina com `fundir(...)`, que junta as peças numa malha só com um material por cor, e `exportar_lobby.py` reduz cada modelo ao orçamento de triângulos e grava os GLB em `game/assets/modelos/`:

| Script | Modelo no jogo | Observação |
| --- | --- | --- |
| `lobby/torre_numeros.py` | `torre-numeros` | roxo e dourado, relógio sem algarismos, engrenagens e cristal com anéis |
| `lobby/torre_letras.py` | `torre-letras` | pergaminho, trepadeira verde, faixa azul-céu, livro aberto e pena |
| `lobby/portal.py` | `portal` | o `portal_energy.gdshader` lê a abertura em coordenadas do modelo (meia-largura 0,5, centro do arco em z = 0,015, chão em z = -0,9, modelo centrado, 1,9 de altura) e pinta a membrana; não mude essas medidas sem mudar o shader |
| `lobby/atril.py` | `atril` | atril do Álbum com livro de figurinhas, sem texto |
| `lobby/arvore.py` | `arvores` | uma árvore, repetida em cada ponto do Lobby |
| `lobby/ciencias.py` | `landmarks/ciencias` | estufa; a peça `Ciencias_Foliage` é separada porque o jogo balança as folhas |
| `lobby/historia.py` | `landmarks/historia` | ruínas de um templo |
| `lobby/geografia.py` | `landmarks/geografia` | globo sobre uma rosa dos ventos |
| `lobby/vilarejo.py` | `vilarejo/casa-a`, `casa-b`, `poco`, `canteiro`, `moinho`, `moinho-pas` | as peças do vilarejo da ilha principal; as pás são separadas porque o jogo as gira em volta do cubo do moinho (x = 0, y = -0,82, z = 2,55) |
| `lobby/mercado.py` | `mercado` | a barraca do Mercador, com toldo listrado, balcão e placa sem texto |
| `lobby/portal_mestre.py` | `portal-mestre` | o Portal do Desafio mestre: arco roxo e dourado, com a membrana (material emissivo) no plano x-z |
| `lobby/adornos_torres.py` | `torre-engrenagem`, `torre-livro` | adornos centrados na origem que o jogo põe no alto de cada torre, gira e faz flutuar |
| `lobby/chao.py` | `lote02/hex-base-v02`, `margem-reta`, `margem-convexa`, `rocha-inferior-avaliacao`, `grama-tres-pontas-v01` | o chão hexagonal. O jogo escolhe o caminho trocando o deslocamento de UV do material `Terreno_Atlas64` (8x8 células, a da máscara m na coluna m % 8 e na linha m // 8, do canto de baixo à esquerda; o bit i da máscara é o vizinho i de `Lobby.Neighbors`); o script pinta esse atlas, de 64 caminhos, em pixels. Hexágono de raio 0,955 com o topo em z = 0 e 0,5 de espessura; margens com 1 de comprimento, o lado de fora em +y; a convexa gira em volta do vértice em x = 0,575. Não renomeie o material nem mude essas medidas sem mudar `Lobby.Kit.cs` |
| `lobby/ponte.py` | `lote02/ponte-piso`, `ponte-guarda-corpo`, `ponte-cabeceira` | pontes e corredores das Salas (`CorridorKit.cs`): piso de 3,455 em arco (topo em 0,30 no meio), guarda-corpo a 0,64 do centro |

```text
cd tools/blender
blender -b --factory-startup --python lobby/vilarejo.py --python lobby/mercado.py --python lobby/portal_mestre.py --python lobby/adornos_torres.py --python lobby/torre_numeros.py --python lobby/torre_letras.py --python lobby/portal.py --python lobby/atril.py --python lobby/arvore.py --python lobby/ciencias.py --python lobby/historia.py --python lobby/geografia.py --python exportar_lobby.py -- ../../game/assets/modelos
```

Todos olham para -Y no Blender (a frente vira +Z no jogo). Os modelos não levam textura: o jogo lê a cor do material (`albedo_color` no shader do portal, `leaf_color` no das folhas). O cenário das Dungeons, os itens e os obstáculos ainda são do Meshy.

## O Personagem

Os scripts do Personagem ficam em `personagem/` e usam o bloco comum das criaturas mais `comum_personagem.py` (esqueleto com os nomes de osso do Mixamo, pesos, fusão de peças e animação). Rodam nesta ordem, na mesma cena, e cada um refaz as suas peças:

| Script | O que faz |
| --- | --- |
| `personagem/corpo.py` | esqueleto, `Corpo` (material `pele`), `Rosto_padrao`, `Roupa_basica` (material `roupa`) e `Cabelo_padrao` (material `cabelo`) |
| `personagem/rosto.py` | o rosto pintado: desenha olhos, sobrancelhas e boca numa textura com transparência (renderiza sem janela) e a cola num remendo sobre a cabeça, como `Rosto_padrao` com o material `rosto`; o jogo troca o rosto trocando a textura |
| `personagem/matematico.py` | kit do Matemático: `Matematico_jaqueta`, `_shorts`, `_tenis`, `_oculos`, `_cachecol`, `_bolsa` |
| `personagem/escriba.py` | kit do Escriba: `Escriba_tunica`, `_capa`, `_calca`, `_botas`, `_boina`, `_saco` |
| `personagem/equipamentos.py` | um `Equip_<id>` por item de `equipamentos.json` (open, crystal, scout, scholar, guardian, wings, lantern, amulet) |
| `personagem/cosmeticos.py` | um `Cosm_<tipo>` por tipo de `cosmeticos.json` (material `cosmetico`, tingido pelo jogo) |
| `personagem/animacoes.py` | as ações `parado`, `andando` e `correndo` |
| `personagem/guia.py` | o Guia do Lobby: sábio chibi de barba branca, manto remendado, gorro, óculos e lanterna, no mesmo esqueleto, com as ações `Repouso`, `Bocejo`, `Olhar` e `Alongar` que o `GuideIdle` toca; sai em `modelos/guia.glb` com `exportar_guia.py` |
| `personagem/vitrine.py` | só para conferir: copia o Personagem em cinco combinações lado a lado |

Todas as peças são malhas únicas por item, presas ao mesmo esqueleto, e saem num GLB só com `exportar_personagem.py`:

```text
cd tools/blender
blender -b --factory-startup --python personagem/corpo.py --python personagem/rosto.py --python personagem/matematico.py --python personagem/escriba.py --python personagem/equipamentos.py --python personagem/cosmeticos.py --python personagem/animacoes.py --python exportar_personagem.py -- ../../game/assets/modelos/personagem
```

O jogo (`CharacterModel.cs`) liga e desliga cada peça pelo nome e tinge pelo nome do material (`pele`, `roupa`, `cabelo`, `cosmetico`, `equip_pano`, `equip_metal`, `equip_detalhe`). O Personagem olha para -Y no Blender (+Z no jogo) e a esquerda dele é +X. Os triângulos de cada peça saem contra o guia no fim do export; hoje os kits passam do limite de 1.000 por conjunto e vários equipamentos passam de 400, o que ainda depende de medir em tablet real.

O Boneco exporta com `blender -b --factory-startup --python criaturas/boneco.py --python exportar.py -- <pasta> Boneco` (o nome depois da pasta limita o que sai; sem nomes saem as seis criaturas) e o Guia com `blender -b --factory-startup --python personagem/guia.py --python exportar_guia.py -- <pasta>`. O rosto pintado (`rosto.py`, `guia.py`) é uma textura com transparência desenhada por script e colada num remendo na cabeça; o rosto não passa pela redução de triângulos porque ela mexe nas UVs.

## Como rodar

- **Pelo MCP do Blender (modo seguro):** abra o Blender, conecte o addon e rode o conteúdo do script em `execute_blender_code`. Cada script constrói a criatura na posição `ORIGEM_X`, ao lado das outras, e apaga a versão anterior dela.
- **Sem janela:** `blender -b --python tools/blender/criaturas/pato.py`.

## O bloco comum

O modo seguro do MCP só aceita script **autocontido** (sem `import` de arquivo local, sem `lambda`, sem função passada como valor e sem chamada calculada como `d[x](...)`). Por isso as ferramentas comuns (materiais, olhos, penas, folhas de papel) ficam em `comum.py` e `node tools/blender/sincronizar.mjs` copia esse bloco para dentro de cada script, entre os marcadores `# >>> comum` e `# <<< comum`. **Edite o bloco só em `comum.py`.**

`node tools/blender/sincronizar.mjs --verificar` roda nos portões de qualidade (`tools\qualidade.cmd` e a CI): falha se algum bloco estiver desatualizado ou se um script usar algo que o modo seguro recusa.

## Como criar uma criatura nova

1. Copie o script de uma criatura do mesmo tipo e troque o nome em quatro lugares: `def construir_<nome>(ox)`, `apagar("<Nome>")`, `vazio("<Nome>", (ox, 0, 0), None)` e a chamada `construir_<nome>(ORIGEM_X)` no fim. Todo objeto da criatura nasce filho da raiz e com o prefixo `<Nome>_`.
2. Escolha o próximo `ORIGEM_X`: as criaturas ficam a 3,2 m uma da outra (hoje de 0,0 a 16,0; a próxima é 19,2). O `sincronizar.mjs --verificar` barra nome fora do padrão e `ORIGEM_X` repetido.
3. Use só as ferramentas do bloco comum; se faltar uma, ela entra em `comum.py` e `node tools/blender/sincronizar.mjs` a copia para todos.
4. Rode pelo MCP, confira por capturas (frente, lado e três-quartos) e só então peça a aprovação.

## Exportar para o jogo

O jogo é mobile primeiro, então o export sai pelo Blender sem janela, que roda todos os scripts na mesma sessão e depois `exportar.py` (raiz em `x = 0`, só a seleção, modificadores aplicados, Y para cima, sem imagens) e imprime os triângulos de cada criatura contra o limite do guia:

```text
cd tools/blender
blender -b --factory-startup --python criaturas/mapa.py --python criaturas/pato.py --python criaturas/sapo.py --python criaturas/tartaruga.py --python criaturas/besouro.py --python criaturas/coruja.py --python exportar.py -- <pasta de saída>
```

A pasta de saída é `game/assets/modelos/criaturas`. Depois, o Godot reimporta (`godot --headless --import`). O jogo carrega pelo nome (`criaturas/<nome>`, em `DungeonRun.Menu.cs`) e a altura vem de `CreatureModel.cs`.

## Qualidade (LOD)

`LOD` no bloco comum (`comum.py`) escala os segmentos de esferas, canos, aros e olhos e corta os micro-detalhes (`SEM_DETALHE`: rebites, parafusos, pinos, furos, narinas, unhas, ilhós). `LOD = 1.0` é a referência de alta qualidade; o padrão `0.4` é o do jogo. Qualquer script novo deve usar as ferramentas do bloco comum e `LOD` em vez de números fixos de segmentos; quem usa modificador de subdivisão ou curva própria precisa respeitar o `LOD` também (ver `mapa.py`).

## Padrão dos inimigos

- Objetos (como o Mapa): olhos, **punhos fechados** e tênis.
- Animais: de origami, **com patas e sem mãos**.
- Autômatos (Torre dos Números): latão, aros e engrenagens roxos e dourados, chave de dar corda, olhos com aro de máquina (`aro_olho`) e um **escudo liso** (`escudo_liso`) para o jogo escrever o número.
- Olhos de todos (`kit_olhos`): pupila em espiral, em sentidos opostos, e pálpebra inclinada para o meio (bravo e confuso).
- As referências aprovadas ficam em `docs/arte/referencias/` e `docs/arte/folhas/`.

Triângulos com `LOD = 0.4` (limite do guia: 2.500 por criatura): Sapo 3.184, Pato 3.980, Besouro 5.560, Tartaruga 6.116, Coruja 6.308 e Mapa 8.376; com `LOD = 1.0` eram de 14 mil a 100 mil. Ainda estão acima do limite, que é uma proposta a confirmar em tablet real: cada Sala tem 100 a 150 mil triângulos no total e há uma criatura por vez. Para chegar a 2.500 falta decidir o que sai do desenho (por exemplo os olhos em espiral viram textura).

Cada criatura exporta com uma malha por peça e um material por cor, não o material único do guia; juntar por material e assar a cor em textura é o próximo passo.
