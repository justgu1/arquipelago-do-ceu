# Modelos feitos no Blender por script

Cada criatura do jogo tem o **seu próprio script** em `criaturas/` (um arquivo por criatura). Os scripts são a fonte dos modelos: para mudar uma cor ou uma proporção, muda-se o script e refaz-se o modelo, em vez de mexer no `.blend`.

| Script | Criatura | Tipo |
| --- | --- | --- |
| `criaturas/mapa.py` | Mapa (Torre das Letras) | objeto, com punhos fechados e tênis |
| `criaturas/pato.py` | Pato (Torre das Letras) | animal de origami, com patas |
| `criaturas/sapo.py` | Sapo (Torre das Letras) | animal de origami, com patas |

## Como rodar

- **Pelo MCP do Blender (modo seguro):** abra o Blender, conecte o addon e rode o conteúdo do script em `execute_blender_code`. Cada script constrói a criatura na posição `ORIGEM_X`, ao lado das outras, e apaga a versão anterior dela.
- **Sem janela:** `blender -b --python tools/blender/criaturas/pato.py`.

## O bloco comum

O modo seguro do MCP só aceita script **autocontido** (sem `import` de arquivo local, sem `lambda`, sem função passada como valor e sem chamada calculada como `d[x](...)`). Por isso as ferramentas comuns (materiais, olhos, penas, folhas de papel) ficam em `comum.py` e `node tools/blender/sincronizar.mjs` copia esse bloco para dentro de cada script, entre os marcadores `# >>> comum` e `# <<< comum`. **Edite o bloco só em `comum.py`.**

`node tools/blender/sincronizar.mjs --verificar` roda nos portões de qualidade (`tools\qualidade.cmd` e a CI): falha se algum bloco estiver desatualizado ou se um script usar algo que o modo seguro recusa.

## Padrão dos inimigos

- Objetos (como o Mapa): olhos, **punhos fechados** e tênis.
- Animais: de origami, **com patas e sem mãos**.
- Olhos de todos (`kit_olhos`): pupila em espiral, em sentidos opostos, e pálpebra inclinada para o meio (bravo e confuso).
- As referências aprovadas ficam em `docs/arte/referencias/` e `docs/arte/folhas/`.

Os modelos ainda não passaram pela otimização: ficam acima do limite de triângulos do guia de estilo e usam modificadores (Shrinkwrap, Skin, Solidify) que precisam ser aplicados antes de exportar para o Godot.
