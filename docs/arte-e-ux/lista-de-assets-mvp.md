# Lista de assets para testar a ideia

Escopo **mínimo** para validar o jogo: andar no Lobby, treinar contra o Boneco e ver uma criatura de cada torre. **Sem customização por enquanto:** nada de cabelos, partes do rosto, roupas por Papel nem criaturas extras. Os limites de triângulos estão no [guia de estilo visual](guia-de-estilo-visual.md), seção 8.

## Modelos (em `game/assets/modelos/`)

| Grupo | Asset | Triângulos | Limite |
| --- | --- | --- | --- |
| Personagem | Corpo base, com rig de 24 ossos, andar e correr | 3.671 | 3.500 |
| Lobby | Boneco de treino | 2.529 | 2.500 |
| | Guia | 3.132 | 3.000 |
| | Portal de fast travel | 1.469 | 1.500 |
| | Atril do Álbum | 796 | 800 |
| | Grupo de árvores | 420 | 600 |
| Torres | Torre dos Números | 5.851 | 6.000 |
| | Torre das Letras | 5.849 | 6.000 |
| Criaturas | Coruja mecânica (Números) | 2.580 | 2.500 |
| | Pato (Letras) | 2.615 | 2.500 |

Cada modelo tem **uma textura de cor base a 1024 px** (`tools/otimizar-modelos` reduz as que vierem maiores, sem alterar a malha). Os que passam um pouco do limite (até 5%) foram aceitos para o teste.

## Sem modelo 3D

- **Chão hexagonal:** um prisma hexagonal simples no Godot, com texturas pintadas a partir da folha `30-chao-hexagonal-vivo`.
- **Água, nuvens, névoa e efeitos:** shaders e partículas do Godot.
- **Animação de criaturas e torres:** no Godot, sem esqueleto (seção 9 do guia).
- **Interface:** Control nodes com a fonte do HUD.

## Fica para depois

Customização do Personagem (cabelos, partes do rosto, roupas e ferramentas dos Papéis), criaturas além de uma por torre, as outras três torres, interiores das salas, balão e ilhas do céu.
