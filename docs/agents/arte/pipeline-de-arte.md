# Pipeline de arte

Como os modelos do jogo são feitos. A documentação do jogo (`docs/arte-e-ux/`) diz como a arte deve ficar; este documento diz com que ferramentas e passos ela é produzida.

## Decisão de 23/10/2026: modelos novos feitos no Blender por script

Todo modelo novo (Personagem, kits dos Papéis, criaturas, cenário e itens) é feito no Blender por scripts Python versionados no repositório, com o Claude conduzindo o Blender pelo MCP (`mcp-for-blender`, em modo seguro, sem telemetria e só em localhost). Nenhum gerador de IA entra no pipeline. Os modelos do Meshy que o jogo já usa ficam até o substituto ficar pronto, e cada troca remove o modelo antigo.

- **Formas simples e cor chapada**, no padrão do guia de estilo: o que um script faz bem (primitivas, subdivisão, chanfros, cor por material) e o que a máquina de desenvolvimento, sem GPU dedicada, renderiza rápido.
- **O script é a fonte**, não o `.blend`: refazer um modelo é rodar o script de novo, e mudar cor ou proporção é mudar um parâmetro.
- **Saída em glTF** com origem nos pés, escala em metros e nomes de peça fixos, para o jogo carregar pelo nome, como hoje.
- **Peças trocáveis no mesmo esqueleto**: Capacete, Armadura, Acessório e ferramenta são malhas separadas presas aos ossos do Personagem.
- **Aprovação visual é humana**: o Claude confere por capturas, e o usuário aprova cada modelo no jogo.
- **Triângulos medidos no arquivo** contra a tabela do guia de estilo antes de o modelo entrar.

Alternativas rejeitadas: continuar no Meshy, que cobra por geração, tem a licença dependente do plano e devolve malha única, difícil de modularizar; e geradores locais de imagem para 3D (TRELLIS, Hunyuan3D, TripoSR), que pedem GPU dedicada e devolvem malha sem rig. O preço é o acabamento: rosto, cabelo e pesos do rig pedem mais rodadas de ajuste do que um modelador faria à mão.

## Pendência legal dos modelos do Meshy

A licença dos modelos gerados no Meshy depende do plano da conta (uso comercial e atribuição variam por plano). Confirmar os termos antes de qualquer distribuição pública, ou trocar todos antes dela. Se o plano exigir atribuição, ela precisa voltar ao `CREDITS.md` e à tela de Créditos (`game/data/creditos.json`).

## Histórico: como os modelos atuais foram gerados no Meshy

Os modelos em `game/assets/modelos/` vieram da API de Image to 3D do Meshy, a partir das folhas em `docs/arte-e-ux/folhas/` (geradas lá também), e foram otimizados com `tools/otimizar-modelos` (só cor base, 1024 px). Os prompts estão em [prompts-meshy.md](prompts-meshy.md) e o último lote pedido em [folha-de-pedidos.md](folha-de-pedidos.md).

**Prompts de folha.** As cores da paleta iam por extenso no prompt ("vivid purple and gold"), com o fechamento `stylized 3D game asset, vivid saturated colors, chunky rounded shapes, soft bevels, hand-painted textures, storybook cartoon style, clean readable silhouette, plain light gray background, game-ready` e o negativo no próprio texto (`no text, no letters, no numbers, no labels, no logo, no watermark, no teeth, no weapons`), porque a API de imagem não tem campo de negative prompt. Imagem de referência ajuda no estilo, mas o modelo copia as peças dela.

**Triângulos.** O Image to 3D com `target_polycount` 12.000 gerou 12.461 triângulos e uma textura de 2048 px no corpo base. O *remesh* (5 créditos) levou o mesmo modelo a 3.671 triângulos com visual quase idêntico, mas devolveu 3 texturas PNG (8 MB); só a cor base era usada. Por isso o pedido saía com `should_remesh` e `target_polycount` já no limite.

**Rig.** O Meshy faz rig automático só de humanoide, com andar e correr; criaturas e torres nunca tiveram rig e se animam no Godot, sem esqueleto. Para outras animações do humanoide a opção era a biblioteca do Mixamo. O Motion2MixamoRig (movimento copiado de vídeo) ficou fora: experimental, cerca de 5 GB de pesos, lento em CPU, e o modelo corporal SMPL-X é de uso não comercial por padrão. A dobra da capa na corrida foi conferida contra a animação original no Blender (quadros 11 e 21 equivalem aos ticks 10 e 20 no Godot).
