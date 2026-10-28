# O combate tem duas camadas: Ataques básicos tiram a Névoa e o QTE educacional faz a luta avançar

O combate de hoje (a criança desenha a resposta, o mundo desacelera e o fim do traço ataca) foi considerado bom no desenho, mas sem desafio e sem empolgação. Em 27/10/2026 os sócios decidiram a direção abaixo, **a validar num protótipo descartável do Escriba contra o Boneco antes de virar implementação definitiva** (ver `docs/agents/roteiro-de-playtest.md`). As referências são God of War 1 e Metal Gear Rising, adaptadas sem violência.

## A decisão

- **Névoa em Etapas.** A criatura tem uma Névoa dividida em Etapas (as instâncias de dano), uma depois da outra. Os Ataques básicos tiram a Névoa da Etapa atual.
- **Ataques básicos.** São três tipos (Precisa, Diagonal e Carregado, vindos do experimento F9). Cada um tem a sua Recarga (em dados, na ordem de 2 a 4 s), então a criança decide qual usar e por quê. Cada criatura tem uma fraqueza entre os três, que **multiplica o dano**: o ataque certo tira mais Névoa, o errado tira menos, nunca zero. A fraqueza aparece por forma e ícone (nunca só cor) e muda uma vez na luta, com aviso antes. Errar o tipo nunca custa Energia. Os Ataques básicos não têm conteúdo educacional.
- **QTE educacional ao zerar a Etapa.** Quando a Névoa da Etapa zera, abre o QTE, onde entra a educação. No Escriba é a pesca de sílabas (escolher as certas entre distratores) e depois o Desenho livre; na Matemática, escolher as peças e desenhar a conta. Cada QTE é um Desafio completo, para cada acerto contar para o Conceito.
- **Passou: avança. Falhou: recua.** Passar no QTE encerra a Etapa de vez e a luta segue para a próxima, com menos Névoa. Falhar devolve uma fração da Névoa da Etapa (em dados, cerca de metade), e a criança precisa tirar essa parte de novo para voltar ao QTE, que traz outra palavra ou outros números do mesmo Conceito. O QTE da última Etapa é o golpe final.
- **Quantas Etapas.** Uma criatura comum tem 2 Etapas (um QTE no meio e o golpe final), o Desafio épico e o Guardião do Nível têm 3, igual nos três Níveis (a ação não fica mais difícil com o ano; só o conteúdo). Quantidade em dados.
- **Duração.** Alvo de 90 a 120 s para uma criatura comum, sem falhas, e cerca de 20 a 30 s a mais por falha. É um alvo de ajuste de dados, não uma regra.
- **Cutscenes.** Curta (até 1 s, sem parar o jogo) no Ataque básico; longa (até 3 s, com o tempo parado, pulável e mais curta a partir da segunda vez) no golpe final.
- **O que vem de cada referência.** Do God of War 1, a finalização que aparece quando a Etapa zera, com a cutscene do golpe final. Do Metal Gear Rising, o tempo desacelerado e o traço como corte: o traço do Desenho livre é um traço de luz que escreve a resposta, sem desmembrar nada. Defesa no tempo certo (parry) e combos de ataque ficam para depois do primeiro teste.

## Custo e prêmio

Só resposta errada, reconhecida com segurança, custa 1 de Energia. Fim da janela de tempo e ambiguidade devolvem a fração de Névoa e nada mais; a dica nunca custa. Há três níveis de prêmio, que mudam espetáculo e Energia mas nunca o progresso: acerto, golpe perfeito (primeira tentativa, sem dica, com tempo sobrando; recupera 2) e acerto com dica ou no limite do tempo.

## A exceção à regra de velocidade

Até aqui valia "nada depende de velocidade ou precisão motora para avançar". Passa a valer: **nenhuma resposta educacional depende de velocidade no Modo calmo**. No modo normal o QTE tem uma janela de tempo generosa (em dados) e a Recarga dos Ataques básicos conta; o ritmo é parte do desafio, que "sempre precisa existir". No Modo calmo não há relógio no QTE, a Recarga cai para cerca de 1 s e o desafio vem da composição (fraquezas, traços das criaturas, sílabas ou peças a mais). O Modo calmo é a garantia de acessibilidade.

## O que não muda

Ambiguidade nunca custa Energia, a dica nunca custa, a Energia máxima e a derrota são as mesmas, e o conteúdo educacional e a dificuldade por Nível não mudam. Toda ação funciona com toque, mouse e teclado: os Ataques básicos ficam em botões do lado oposto ao do movimento, ou em teclas.

## Alternativas rejeitadas

Manter o combate de hoje, que dá só um lance a cada 20 a 30 s, não resolve a falta de desafio. Fazer o próprio Desenho livre correr contra um relógio nos dois modos fere a regra de acessibilidade. Uma Guarda que abre uma única vez e deixa o QTE só no fim da luta dá pouca repetição do ciclo, que é onde está a tensão.

## Em aberto

Se o anel do Lance continua (o Boneco do treino não lança, então o protótipo não o testa), os valores de Recarga, de fração devolvida e de janela de tempo, e o QTE do Matemático (o do Escriba é o que o protótipo prova primeiro).
