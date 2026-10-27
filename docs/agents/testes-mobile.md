# Testes no celular (Android)

Como testar o jogo em aparelhos de verdade, o que medir e o que já se sabe. A meta é **60 fps de média e 45 no 1% pior** em cada aparelho da matriz. O 1% pior é o fps dos quadros mais lentos: a média sozinha esconde engasgo.

## Matriz de aparelhos

| Aparelho | Chip e GPU | RAM | Papel na matriz | Estado |
| --- | --- | --- | --- | --- |
| Galaxy S23 (SM-S911B) | Snapdragon 8 Gen 2, Adreno 740 | 7 GB | topo de linha, perfil alto | medido |
| Galaxy A15 5G (dois aparelhos) | Dimensity 6100+, Mali-G57 | 4 a 6 GB | gama de entrada, o caso que importa | a medir |
| iPad 11ª geração | A16 | 6 GB | tablet; exige macOS (ver spike de iOS) | fora desta rodada |

Nenhum Android da matriz é tablet: o layout de tablet se confere no PC, com janelas 4:3 e 16:10.

## Como rodar

- **Implantar:** `node tools/android/implantar.mjs` compila o APK de debug (versão = número de commits, para atualizar por cima sem perder o save) e instala em todos os celulares ligados ao `adb`. Com `--firebase` envia aos testadores (precisa de `FIREBASE_APP_ID` e do login do `firebase-tools`).
- **Benchmark:** `node tools/android/implantar.mjs --sem-instalar --args="--driver=benchmark --perfis=todos"` abre o jogo já medindo. `--cenas=lobby` mede só o Lobby. O relatório sai no log (`BENCHMARK_JSON`), em `user://benchmark.json` e na tela.
- **Preparar o aparelho:** Opções do desenvolvedor com **Depuração USB** e **Permanecer ativo** ligados; no Samsung, **Auto Blocker** desligado. Se o `adb` perder o aparelho, a depuração costuma ter desligado sozinha.
- **Quem testa de longe** instala o APK pelo Firebase (liga "instalar apps desconhecidos") e manda o texto do relatório da tela.

## Medições

Galaxy S23, versão 0.1.0. Os três perfis (alto, médio, baixo) só mexem em sombra, antisserrilhado e resolução.

| Momento | Desenho | Triângulos | Média | 1% pior |
| --- | --- | --- | --- | --- |
| Lobby antes das correções, perfil alto | 905 | 5,4 milhões | 38 fps | 22 fps |
| Depois de reduzir as torres e a rocha | 905 | 0,9 milhão | 39 fps | 22 fps |
| Depois da instanciação em lote, perfil alto | 205 | 0,6 milhão | 54 fps | 25 fps |
| Mesmo, perfil médio | 206 | 0,6 milhão | 60 fps | 47 fps (passa) |
| Mesmo, perfil baixo | 206 | 0,3 milhão | 60 fps | 32 fps |

O que mais pesou: as torres tinham 770 mil e 490 mil triângulos (hoje 40 mil), uma rocha de 24 mil triângulos ficava sob cada hexágono da beira (hoje 1,5 mil) e cada hexágono era uma chamada de desenho. A primeira solução, fundir as malhas por material, derrubou o fps mas custava 9,7 s na abertura; a instanciação em lote (`MultiMesh`) leva 0,3 s.

| Outras medições (S23) | Valor |
| --- | --- |
| Abertura, do motor ao Lobby | 14,8 s antes; 5,6 s agora (Lobby 2,4 s, Missões 0,3 s) |
| Memória do app depois de uma hora | 1,09 GB (576 MB de gráficos, 283 MB de heap nativo) |
| Calor depois de uma hora de teste | bateria 41,5 °C, estado térmico leve |

## Resultado do roteiro no S23

| Item | Resultado |
| --- | --- |
| Abre e chega ao Lobby | sim, em ~6 s |
| Direcional anda o Personagem; tocar no chão não o move | sim |
| Interface fora de câmera e cantos; botões no tamanho do dedo | sim, com a escala em 1,5× |
| Teclado virtual não cobre o campo do apelido | sim |
| Voltar fecha a tela, abre o Menu da Dungeon e nunca fecha o jogo | sim |
| Ir para segundo plano e voltar; fechar e reabrir sem perder o Perfil | sim |
| Inventário, trocar Papel, Correr | sim (o Papel muda no card; sem kit visual por Papel, o modelo não muda) |
| Dungeon: HUD, mapa, combate, Energia zerada volta ao Lobby | sim |
| Direcional visível durante o combate, ao lado da bolsa de peças | sim |
| Mercador: botão de comprar sempre à vista | sim |
| Mensagens da criatura sem se sobrepor | sim |
| Pontilhado das sílabas | guia 1,5× maior no celular; um traço sintético foi lido como "dúvida" e não custou Energia; **falta o dedo de verdade** |
| Desenho livre com o dedo | **não testado** |
| Voz do TTS em português | **não testado** |

## Roteiro por aparelho

Marcar em cada aparelho, na versão testada:

1. Abre e chega ao Lobby; tempo da tela inicial até o Lobby (hoje ~8 s no S23, a maior parte lendo as 159 Missões).
2. Direcional virtual anda o Personagem em todas as direções; tocar no chão não o move.
3. Nada da interface some atrás de câmera ou cantos arredondados; botões acertam com o dedo.
4. Teclado virtual não cobre o campo do apelido.
5. Desenho livre de números e letras com o dedo, inclusive com a mão apoiada.
6. Pontilhado das sílabas: a faixa aceita o traço do dedo e "Completar sozinho" funciona.
7. Voz do TTS em português.
8. Salvar e retomar: ir para segundo plano e voltar, e fechar e abrir de novo.
9. Botão Voltar fecha a tela ou abre o Menu da Dungeon, e nunca fecha o jogo.
10. Modo calmo e câmera lenta só durante o desenho.
11. Benchmark nos três perfis e relatório anexado.
12. Calor e bateria depois de 15 minutos de jogo.

## Pendências conhecidas

- A memória de 1,09 GB cabe no S23 (7 GB) mas é risco nos aparelhos de 4 GB; texturas e o guerreiro (100 mil triângulos, esqueleto e capa pré-animada) são o alvo.
- O 1% pior ainda fica abaixo de 45 fps nos perfis alto e baixo do Lobby; os engasgos são de 25 a 60 ms.
- O reconhecimento do desenho livre com o dedo e a voz do TTS seguem sem teste no aparelho.
- A tela de abertura ainda é a do Godot e o app usa o ícone padrão.
