# Guia de estilo visual

Padrão que toda arte do jogo segue. Complementa [direcao-de-arte-e-ux.md](direcao-de-arte-e-ux.md) (câmera, luz, HUD) com o que faltava para as peças combinarem entre si: cores, formas, regras por tipo de asset e animação das torres. Os valores marcados como **proposta** ainda precisam de aprovação.

## 1. Paleta

Cor viva, nunca pastel. O erro mais comum das primeiras folhas foi o chão sair pastel e o resto saturado, o que faz as peças parecerem de jogos diferentes.

| Papel | Cor | Hex (proposta) |
| --- | --- | --- |
| Lobby, principal | Azul | `#2E7DE8` |
| Lobby, apoio | Amarelo | `#FFC83D` |
| Torre dos Números, principal | Roxo | `#7A44D1` |
| Torre dos Números, apoio | Dourado | `#F2B632` |
| Torre dos Números, pedra | Pedra quente | `#C9A27E` |
| Torre das Letras, principal | Verde | `#35B96A` |
| Torre das Letras, apoio | Azul-céu | `#4FB9F0` |
| Torre das Letras, papel | Pergaminho | `#F3E3BE` |
| Grama | Verde vivo | `#62B83A` (sombra `#3F9A3A`) |
| Terra do caminho | Marrom quente | `#C98A48` |
| Água | Azul | `#34A6E8` |
| Rocha | Cinza quente | `#9A8F8A` |
| Madeira | Marrom | `#8A5A36` |
| Fundo das folhas de geração | Cinza claro | `#E6E8EB` |

**Regra de saturação:** as superfícies principais ficam com saturação de 55% a 80%. Só nuvens, papel, pedra e neutros podem ficar abaixo de 35%. Dourado e azul de brilho continuam reservados ao que a criança pode usar (números, sílabas, energia), como na direção de arte.

## 2. Formas e materiais

- **Volumes grossos e arredondados**, com chanfros suaves. Nada de aresta afiada, ponta de lança ou dente.
- **Textura pintada à mão**, com variação leve de tom dentro de cada superfície. Sem contorno preto, sem foto-realismo.
- **Silhueta legível** a 128 px. Se duas criaturas se confundem na silhueta, uma delas muda de forma, não só de cor.
- **Proporção chibi** em personagens e criaturas: cabeça grande, olhos grandes e simpáticos.
- **Sem texto, logotipo, marca ou etiqueta** em nenhum modelo. Número, palavra e nome vêm do jogo (balão de pedido, UI e fonte do HUD), para nunca aparecer letra ou conta errada.

## 3. Chão hexagonal (decidido)

O chão do Lobby e das salas é de **hexágonos**. O padrão de **forma** é a folha `18-chao-hexagonal-lobby-b` (peças lisas e simples, que se encaixam numa grade) e o de **cor e vida** é a `30-chao-hexagonal-vivo`, que é a mesma folha repintada com as cores da seção 1.

- Um prisma hexagonal por tile, com topo e uma lateral de terra ou rocha visível.
- Variantes: grama lisa, grama com flores, caminho (reto, curva, T, ponta), água, borda de penhasco com rocha embaixo, ponte, poço de pedra.
- Orientação (vértice para cima ou para o lado) e tamanho em metros ficam para a prova técnica do gerador de Andares.

## 4. Personagem: um corpo, muitas combinações

Há **um corpo base só**, neutro, sem gênero marcado. A aparência vem de partes combináveis, e o catálogo é único (sem "masculino" e "feminino").

| Parte | Como entra | Mínimo no MVP (proposta) |
| --- | --- | --- |
| Tom de pele | Cor no material | 6 tons, do claro ao escuro |
| Olhos | Atlas de texturas no rosto | 6 formatos (redondo, amendoado, grande com brilho, sonolento, arregalado, com cílios) e cor da íris |
| Sobrancelhas | Atlas de texturas | 6 estilos (fina, grossa, arqueada, reta, inclinada, nenhuma) |
| Boca | Atlas de texturas | 3 sorrisos |
| Sardas e bochechas | Atlas de texturas | liga e desliga |
| Cabelo | Peça presa ao osso da cabeça, tingida por material | 12 penteados e 10 cores |
| Roupa e acessórios | Pelo Papel e pelos Cosméticos | os kits do Matemático e do Escriba |

O rosto **não troca de malha**, só de textura, para não quebrar as expressões. O cabelo é gerado em tom neutro e ganha cor no Godot.

**O corpo base tem de sair com o rosto liso**, sem olhos, sobrancelhas nem boca pintados, porque essas partes entram por cima, como decalque; rosto pintado na textura impede a troca. Os olhos seguem a folha `31a-olhos`; **sobrancelhas e bocas são formas simples, desenhadas em vetor** com a paleta do guia, sem dentes e sem nada que pareça bigode.

## 5. Criaturas

- **Reconhecíveis à primeira vista.** Se a criança não vê o que é, não tem como saber o que escrever (Letras) ou como lutar (Números).
- **Sem etiqueta, letra, número ou texto** no modelo nem em volta. Letras soltas em volta de uma criatura de Letras podem sugerir uma grafia errada. O pedido aparece no balão acima da criatura.
- **Olhos grandes, boca sem dentes**, nunca armas ou ossos (regra da direção de arte). Os chifrinhos e as bochechas rosadas da direção de arte ficam como detalhe opcional.

**Torre dos Números: animais e aves mecânicos** em latão, roxo e dourado, com algo que lembre matemática (ábaco, compasso, régua, relógio sem algarismos, engrenagens). Cada um tem um **escudo liso na frente**, de 30% a 40% da largura do tronco, sem nada na frente do rosto, na mesma posição em todas as espécies, para o jogo escrever o número em cima. Criaturas de Nível mais alto são maiores e têm mais peças, mas a ação não fica mais difícil.

**Torre das Letras: coisas do dia a dia que ganharam vida**, em papel e pergaminho com acentos verde e azul-céu. Cada criatura é **a própria palavra-alvo** (pato, bode, sapo, gato, fada, mula, bota, bola, mala, nave, uva, ovo são exemplos da ordem recomendada provisória). As palavras reais vêm das Missões.

## 6. Torres: elementos animados

Cada torre tem uma identidade própria que se vê de longe e se move devagar. **Proposta**, para aprovar.

| | Torre dos Números | Torre das Letras |
| --- | --- | --- |
| Cores | Roxo e dourado, pedra quente | Verde e azul-céu, pergaminho |
| Anéis orbitando | 2 a 3 anéis de medalhões dourados com os algarismos de 0 a 9, em sentidos opostos | 2 a 3 espirais subindo, de peças de madeira com letras e sílabas |
| Expressões | Placas flutuantes que mostram uma conta e a resolvem em sequência (por exemplo, 2 + 3 = 5) | Peças que se juntam em sílabas e palavras curtas (por exemplo, BA + TA) |
| Mecanismo | Engrenagens girando em sincronia, ponteiros de relógio, cristal do topo pulsando | Livros que abrem e fecham, páginas virando, pena escrevendo um rastro de luz no ar |
| Reação ao progresso | Os medalhões do Nível já vencido acendem em dourado | As peças do Nível já vencido acendem em azul-céu |

**Como se faz:** o modelo traz só a torre e as **peças em branco** (medalhão, peça de madeira, engrenagem, livro, pena). **O algarismo, a letra, a conta e a sílaba são desenhados pelo jogo**, com a fonte do HUD, a partir dos dados do conteúdo. Assim nunca aparece uma conta errada ou uma letra deformada, e a fonte para dislexia vale também aqui. As peças são instâncias de um mesmo modelo, no máximo 40 por torre.

**Regras de movimento (proposta):**

- Lento: uma volta de anel leva de 30 a 60 segundos. Nada pisca mais de 3 vezes por segundo.
- Nada depende de timer nem interfere no jogo; é só cenário vivo.
- A opção **Redução de animações** deixa os anéis parados e as expressões fixas; o Modo calmo reduz as partículas.
- Os números e as letras em órbita seguem o Nível: no Nível 1, só o que o 1º ano usa.

## 7. Aprovação de uma peça nova

As referências aprovadas ficam em `referencias/` e as folhas de assets em `folhas/`, nesta pasta. Uma peça nova parte delas e passa pelo checklist antes de entrar no jogo.

**Checklist de aprovação:** cores da paleta e saturação viva; silhueta legível; sem texto, letra, número ou etiqueta; sem dente, arma ou osso; chanfros e arredondados; combina com as outras folhas aprovadas.

## 8. Orçamento de polígonos e texturas

A otimização é requisito: o jogo roda em PC e em tablets Android de gama média. Os limites abaixo são **proposta** e devem ser confirmados na prova técnica, medindo em tablet real. Valem para **triângulos por modelo**, depois da redução.

| Asset | Limite |
| --- | --- |
| Corpo base com roupa básica | 3.500 |
| Cabelo (por peça) | 600 |
| Roupa do Papel (conjunto) | 1.000 |
| Acessório ou ferramenta | 400 |
| Criatura | 2.500 (chefe: 5.000) |
| Guia e outros NPCs | 3.000 |
| Tile hexagonal | 100, instanciado em lote (um mesmo modelo repetido) |
| Árvore, pedra e prop pequeno | 300 a 600 |
| Prop grande (casa, ponte) | 1.500 |
| Torre | 6.000 |
| Peça que orbita a torre (medalhão, peça de madeira) | 150, no máximo 40 por torre, instanciadas |
| Tudo o que aparece numa sala ou no Lobby | 100.000 a 150.000 (a medir em tablet) |

**Texturas:** um material por modelo, **só cor base** (sem normal, metálico nem rugosidade, porque o estilo é pintado à mão). 1024 pixels para personagem e criatura, 512 para prop e atlas compartilhado de 1024 para os tiles, com compressão de GPU no import do Godot.

Todo modelo tem os **triângulos medidos no arquivo** antes de entrar no jogo. Uma verificação automática dos triângulos na CI é recomendada.

### Escala no jogo (altura em unidades do mundo; o Personagem tem cerca de 1,5)

Todo modelo é reescalado pela altura quando entra no jogo, então o arquivo pode vir de qualquer tamanho. O que vale é esta tabela, para pessoa, animal e objeto ficarem em proporção:

| Tipo | Mínimo | Máximo | Exemplos |
| --- | --- | --- | --- |
| Personagem e NPC | 1,5 | 1,8 | Personagem, Guia |
| Criatura | 0,8 | 1,5 | pato 1,0; coruja 1,1; autômatos 1,2 a 1,4 |
| Objeto de chão | 0,5 | 1,2 | fonte 1,0; caixote; pilha de livros |
| Objeto de mão ou item | 0,2 | 0,5 | orbe, medalhão, placa de letra |
| Porta e portal | 1,8 | 2,8 | porta-arco 1,6 (a subir); portal 2,0 a 2,8 |
| Escada | 0,8 | 1,2 | escada principal 1,1; lateral 0,8 |
| Torre | 4 | 6 | torres do Lobby 5 |

A criatura nunca passa do Personagem nem fica menor que um objeto de chão. Em código, a altura de cada criatura está em `CreatureModel` (com mínimo e máximo), e a dos objetos da Sala em `RoomProps`.

## 9. Animação, física e efeitos

Tudo aqui é **proposta** para o MVP. A regra geral é a mesma da seção 8: o que o motor faz de graça não se gera em modelo.

**Personagem (humanoide).** Tem esqueleto humanoide, e o equipamento e os Cosméticos se prendem aos ossos dele. Clipes do MVP: parado, andar, interagir, atacar (lançar o orbe ou conjurar sílabas), acerto, golpe perfeito, erro (tropeço leve, sem queda) e voltar flutuando ao Lobby.

**Criaturas e torres (não humanoides).** Não têm esqueleto. Elas se animam **no Godot**: pairar e balançar, esticar e achatar, inclinar, pulsar o escudo, balançar por shader de vértice. Clipes: parada, mover devagar, avisar antes de atacar, lançar bolha, travessura, ser libertada (sorri e se dissolve). Os anéis e as peças das torres giram por script.

**Física.** O MVP não usa `RigidBody3D` em nenhuma regra de jogo. O Personagem e as criaturas são corpos cinemáticos que andam na grade hexagonal, e as bolhas de névoa são áreas lentas com aviso. Física torna o tempo do jogo menos previsível e pesa em tablet. Só objetos decorativos opcionais podem usá-la, e o Modo calmo os desliga.

**Efeitos.** Feitos com partículas e shaders do Godot, sem modelo 3D: raio de energia da conta ou do nome, névoa que se dissolve, brilho dourado do acerto, rajada do golpe perfeito, bolha com anel de aviso, tremida leve do erro (nunca vermelho forte), brilho do portal e pulso do cristal. Limite proposto: até 200 partículas na tela, e o Modo calmo reduz as partículas.
