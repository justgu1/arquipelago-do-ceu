# O combate tem duas camadas: Ataques básicos tiram a Névoa e o QTE educacional faz a luta avançar

O combate de hoje (a criança desenha a resposta, o mundo desacelera e o fim do traço ataca) foi considerado bom no desenho, mas sem desafio e sem empolgação. Em 27/10/2026 os sócios decidiram a direção abaixo, e em 30/10/2026, depois de jogar o protótipo, passaram a valer para o jogo todo (ver `docs/agents/roteiro-de-playtest.md` para o playtest com crianças). As referências são God of War 1 e Metal Gear Rising, adaptadas sem violência.

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

## Atualização de 30/10/2026: o combate novo é o único

O protótipo agradou e o combate de antes (a resposta aberta desde o começo, com Bolha de névoa e Lance) foi removido. As mudanças:

- **No jogo todo.** Vale no treino e nas Dungeons, no Escriba e no Matemático, no Desafio épico e no Guardião. O Puzzle (porta) e o Atalho de Nível só pedem a resposta, sem Névoa. A chave F10 deixou de existir.
- **Ataques básicos mais rápidos.** A Recarga cai para cerca de 0,8, 1,2 e 1,8 s (Precisa, Diagonal e Carregado), o dano sobe e a Névoa da Etapa é 40, para a Etapa durar poucos golpes. Cada tipo ganhou a sua animação no Personagem.
- **A criatura ataca de volta, e a Bolha e o Lance deixam de existir.** Ela tem três ataques básicos, cada um com Recarga e aviso próprios, e escolhe entre os que estão prontos sem repetir o anterior: a Investida (uma faixa até a criança), o Pulo (um círculo onde a criança estava) e o Giro (um anel em volta dela). A mira trava no aviso, que usa forma, texto e som; sair da área é desviar, e ser alcançada custa 1 de Energia. Não há ataque no Modo calmo.
- **Cansaço cumulativo da criatura.** Cada Ataque básico que ela leva sobe o cansaço (0 a 1); cansada, ela espera até 2,5 vezes mais para atacar de novo e fica mais baixa e lenta. Ele cai com o tempo e zera quando a Etapa acaba.
- **Estonteada no QTE.** Zerada a Névoa, a criatura fica parada, com estrelas girando sobre ela, enquanto a criança responde; ao passar ou falhar ela se recupera.
- **Dados.** Tudo isso está em `combat.fog` (`creature.attacks`, cansaço e Recarga); `combat.bubble` e os ritmos por tipo de criatura foram removidos.

## O que não muda

Ambiguidade nunca custa Energia, a dica nunca custa, a Energia máxima e a derrota são as mesmas, e o conteúdo educacional e a dificuldade por Nível não mudam. Toda ação funciona com toque, mouse e teclado: os Ataques básicos ficam em botões do lado oposto ao do movimento, ou em teclas.

## Alternativas rejeitadas

Manter o combate de hoje, que dá só um lance a cada 20 a 30 s, não resolve a falta de desafio. Fazer o próprio Desenho livre correr contra um relógio nos dois modos fere a regra de acessibilidade. Uma Guarda que abre uma única vez e deixa o QTE só no fim da luta dá pouca repetição do ciclo, que é onde está a tensão.

## Em aberto

Os valores de Recarga, de cansaço, dos ataques da criatura, de fração devolvida e de janela de tempo, que o playtest com crianças vai ajustar; e as cutscenes de ataque por criatura, que hoje são movimentos feitos por código (os modelos não têm esqueleto).
