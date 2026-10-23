# Privacidade, ECA Digital e licenças

O que a lei pede ao jogo e à API, o que já é cumprido e o que falta.

## LGPD (Lei 13.709/2018)

O art. 14 exige consentimento específico de um responsável para tratar dados de crianças, e o art. 6º manda coletar o mínimo. Por isso:

- A criança tem só apelido e um identificador aleatório; nada de nome real, foto, escola ou CPF.
- A telemetria só é gravada depois do consentimento do responsável; sem ele, os eventos nem entram na fila, e o jogo funciona igual.
- Nenhum dado da criança vai para serviços de IA de terceiros.
- O responsável pode pedir para apagar tudo.

O desenho das contas e do consentimento está em [Dados e privacidade](../produto/documento-de-produto.md#dados-e-privacidade) e a telemetria no [documento técnico](../tecnico/documento-tecnico.md).

## ECA Digital (Lei 15.211/2025)

A lei vale para todo produto ou serviço de tecnologia "direcionado a crianças e a adolescentes" (art. 1º), e a definição cita programas de computador e jogos eletrônicos (art. 2º, I). O jogo é feito para crianças de 6 a 10 anos, então a lei se aplica, e com a API (contas, sincronização e telemetria) se aplica sem dúvida. A lei está em vigor desde 17 de março de 2026, e as obrigações são moduladas pelo porte do fornecedor e pelo número de usuários (art. 39).

| O que a lei pede | Artigo | Como o jogo atende | Falta |
| --- | --- | --- | --- |
| Privacidade e proteção de dados no nível mais alto **por padrão** | 7º | Perfil só com apelido; telemetria desligada até o consentimento do responsável (LGPD art. 14) | Nada no jogo; a API segue a mesma regra |
| Configurações que evitem o **uso compulsivo** | 8º, IV; 17, §4º, II | Sem sequência diária, relógio, "só hoje", notificações nem recompensa por tempo jogado (ADR 0007) | Nada |
| Informar a **faixa etária indicada** no momento do acesso, conforme a classificação indicativa | 8º, II e V | — | Obter a classificação indicativa e mostrá-la na abertura do jogo e onde ele for distribuído |
| Informações sobre **riscos e medidas de segurança**, acessíveis sem precisar adquirir o jogo | 16 | — | Página pública com o que o jogo coleta, por quê e como apagar |
| **Supervisão parental**: ferramentas fáceis de usar, informação em local de fácil acesso, aviso quando estiverem ativas e **limitar e acompanhar o tempo de uso** | 17, I a IV; 17, §4º, IV | — | Área do responsável (protegida por pergunta de controle parental) com limite de tempo, tempo total de uso e as configurações de privacidade |
| O responsável vê e gerencia a conta e a privacidade, vê o **tempo total de uso**, ativa ou desativa salvaguardas, tudo em português | 18 | — | A mesma área do responsável; na API, a conta do responsável |
| Nenhuma interface que manipule a escolha ou enfraqueça as salvaguardas | 18, §2º | Nada pressiona a criança: sem punição por ficar sem jogar e derrota que fala da aventura, nunca da criança | Revisar a área do responsável quando existir |
| **Caixas de recompensa** proibidas | 20 | Não existem; nada se compra e nada é sorteado (ADR 0007) | Nada |
| Interação entre usuários só com salvaguardas e, por padrão, limitada até o consentimento do responsável | 21 | Não há interação entre usuários | Se um dia houver, aplicar o art. 21 e a Lei 14.852/2024 |
| Sem perfilamento para publicidade | 22; 26 | Sem anúncios; o que o jogo aprende da criança (repetição espaçada, dificuldade) serve só ao conteúdo | Nada |
| Contas de crianças até 16 anos **vinculadas à conta de um responsável** | 24 | Previsto: uma conta de responsável com vários perfis de criança | Implementar na API |
| Canal para notificar violações de direitos de crianças | 28 | Não há conteúdo feito por usuários | Um canal de contato público junto da página do art. 16 |
| Ferramentas de IA revistas e com opção de desligar o que não é essencial | 17, §4º, VIII | O jogo não usa IA; as missões chegam prontas e são validadas na borda | Nada no jogo |

Relatório semestral (art. 31) só vale acima de 1 milhão de usuários nessa faixa etária. As sanções vão de advertência a multa de até 10% do faturamento no Brasil ou, sem faturamento, de R$ 10 a R$ 1.000 por usuário cadastrado, limitada a R$ 50 milhões por infração (art. 35).

O texto integral da lei, com as alterações de 2026, fica em [docs/agents/fontes/legislacao/lei-15211-2025-eca-digital.md](../agents/fontes/legislacao/lei-15211-2025-eca-digital.md).

## Licenças de terceiros

O motor, as bibliotecas e a fonte que vão no jogo, com as licenças, estão no [CREDITS.md](../../CREDITS.md) e na tela de Créditos do jogo. A única com condição comercial é o pacote do JsonSchema.Net: taxa de manutenção para quem fatura US$ 10 mil ou mais por ano. **Reavaliar antes de qualquer uso comercial.**
