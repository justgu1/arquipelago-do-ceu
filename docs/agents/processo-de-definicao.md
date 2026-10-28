# Processo de definição: micro sessões

Nasceu em 27/10/2026, da conversa entre os sócios: "está tudo no freestyle" e "a gente fica empurrando coisa aleatória pra dentro do repo". A mecânica do jogo pesa mais que lore, missão e animação; o jogo cai por terra se não for divertido de jogar. O processo existe para decidir antes de produzir.

## A micro sessão

- **Duração:** 30 a 45 minutos, no máximo uma por semana.
- **Uma pergunta só.** Ex.: "o Desenho livre com o tempo desacelerado é divertido para uma criança de 6 a 10 anos?". Se surgir outra pergunta, ela vira a pauta da sessão seguinte.
- **Uma decisão registrada no mesmo dia.** O destino do registro:
  - regra do jogo: ADR em `docs/adr/` e o documento de produto;
  - termo novo: `GLOSSARY.md`;
  - trabalho a fazer: issue no GitHub;
  - resultado de teste: issue com o rótulo `playtest` (ver `roteiro-de-playtest.md`).
- **Quem decide:** o responsável pelo produto tem a palavra final em mecânica e o responsável pela animação, em animação. Divergência não vira discussão: vira experimento no playtest, com uma variação por vez.

## O que fica congelado até haver resultado de teste

Arte nova, criaturas novas, Papéis novos e equipamentos novos. Seguem livres: a animação, o export mobile, a telemetria de benchmark e as correções de combate. Voltar a produzir conteúdo exige que a mecânica central tenha passado no critério do roteiro de playtest.

## Antes de propor algo

1. Leia o `GLOSSARY.md` e os ADRs da área.
2. Confira se o documento de produto já decidiu. Se a ideia contradiz uma decisão, a sessão trata da contradição primeiro.
3. Prefira um protótipo descartável (skill `prototype`) a uma discussão longa.
