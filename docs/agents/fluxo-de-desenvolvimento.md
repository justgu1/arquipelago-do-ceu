# Fluxo de desenvolvimento

O desenvolvimento segue um fluxo único, com skills do Claude Code instaladas em `.claude/skills/`:

```text
(/wayfinder | /grill-with-docs)  ->  /to-tickets  ->  /implement
```

| Etapa | Quando usar | O que produz |
| --- | --- | --- |
| `/setup-matt-pocock-skills` | Uma vez por repositório | Define onde ficam os tickets (GitHub Issues ou Markdown local) e grava a configuração no `CLAUDE.md` |
| `/wayfinder` | O trabalho é grande (várias sessões) e o caminho ainda não está claro | Um mapa de tickets de decisão, resolvidos um a um até não sobrar nada a decidir. Só planeja, não escreve código |
| `/grill-with-docs` | O trabalho cabe numa sessão e o plano ainda está vago | Entrevista até o plano ficar claro e grava o vocabulário em `GLOSSARY.md` e as decisões difíceis em ADRs |
| `/to-tickets` | Já existe um plano ou spec | Tickets pequenos, cada um uma fatia completa e demonstrável, com as dependências entre eles |
| `/implement` | Há um ticket (ex.: `/implement #12`) ou uma spec | Implementa com testes primeiro (TDD), roda os testes, pede revisão e faz commit na branch atual |

Quando o fluxo apontar uma mudança que contradiz algo já decidido, o documento correspondente em `docs/` é atualizado. Guias detalhados de cada skill estão em [skills/](skills/).
