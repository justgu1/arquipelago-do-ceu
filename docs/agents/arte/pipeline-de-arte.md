# Os modelos novos são feitos no Blender por script, e os do Meshy saem um por um

A partir de 23/10/2026, todo modelo novo do jogo (Personagem, kits dos Papéis, criaturas, cenário e itens) é feito no Blender por scripts Python versionados no repositório, com o Claude conduzindo o Blender pelo MCP (`mcp-for-blender`, em modo seguro, sem telemetria e só em localhost). Nenhum gerador de IA entra no pipeline. Os modelos do Meshy que o jogo já usa ficam até o substituto ficar pronto, e cada troca remove o modelo antigo.

Regras do pipeline:

- **Formas simples e cor chapada**, no padrão do guia de estilo: o que um script faz bem (primitivas, subdivisão, chanfros, cor por material) e o que a máquina de desenvolvimento, sem GPU dedicada, renderiza rápido.
- **O script é a fonte**, não o `.blend`: refazer um modelo é rodar o script de novo, e mudar cor ou proporção é mudar um parâmetro.
- **Saída em glTF** com origem nos pés, escala em metros e nomes de peça fixos, para o jogo carregar pelo nome, como hoje.
- **Peças trocáveis no mesmo esqueleto**: Capacete, Armadura, Acessório e ferramenta são malhas separadas presas aos ossos do Personagem.
- **Aprovação visual é humana**: o Claude confere por capturas, e o usuário aprova cada modelo no jogo.

Alternativas rejeitadas: continuar no Meshy, que cobra por geração, tem a licença dependente do plano e devolve malha única, difícil de modularizar; e geradores locais de imagem para 3D (TRELLIS, Hunyuan3D, TripoSR), que pedem GPU dedicada e devolvem malha sem rig. O preço é o acabamento: rosto, cabelo e pesos do rig pedem mais rodadas de ajuste do que um modelador faria à mão.
