# Pesquisa: ritmo real da Maestria e encaixes no código de combate

Ticket #227 (filho do mapa #221, Árvore de talentos). Só fatos lidos no código e nos dados da branch `research/arvore-de-talentos-maestria` (base `main`, commit `b4f5bce`). Nenhum desenho é proposto aqui. Onde há conta própria (simulação), ela vem marcada como estimativa.

## 1. Quantos pontos de Maestria um Papel acumula

### 1.1 Como a Maestria sobe hoje

- A Maestria de um Papel é a **quantidade de Conceitos dominados** com a chave da torre: `MasteryService.Of` conta, em `Progress.Mastered`, as chaves que começam com `disciplina:` (`core/Saves/ProgressGrowth.cs`). Não há XP, não há outro tipo de ponto e não há teto escrito no código; o teto é o número de Conceitos que existem.
- Um Conceito é dominado na vitória de número `dungeon.conceptSolves` (valor 3 em `game/data/rules.json`, linha 305; a validação em `core/Rules/DungeonRules.cs` exige um valor entre 1 e um limite). Depois de dominado, novas vitórias nele não contam nada (`MasteryService.Record` sai cedo).
- A chave do Conceito é `disciplina:conceito` (`ConceptKey`, `core/Saves/ProgressKeys.cs`) e **não leva o ano**. Conceito com o mesmo id em anos diferentes é um só (só `sistema-monetario`, que existe nos anos 1, 2 e 3 de Matemática).
- Quem registra é `DungeonRewards.RecordSolved` (`game/Dungeon/DungeonRewards.cs`), chamado por `DungeonRun.OnSolved`, que o evento `TrainingArea.ChallengeSolved` dispara **uma vez por Sala vencida** (`CompleteRoomFight` e `Won`, em `game/Combat/TrainingArea.Fog.cs`), com a Missão que estava em jogo no fim da luta (a da última Etapa). Ou seja: uma vitória de Conceito por Sala de Combate, Puzzle ou Desafio épico, e não uma por Etapa nem por acerto.
- Não conta: treino do Lobby, Atalho de Nível (`if (inShortcut()) return;`) e Desafio mestre (não passa por `DungeonRewards`). Marco, Descanso, Fast travel, entrada e saída não têm Desafio.
- O Conceito sai da Missão sorteada: `Training.cs` e `WordBattle.cs` sorteiam com `rng.NextInt(pool.Count)` entre as Missões do ano e da dificuldade da Sala. Logo o peso de cada Conceito é o número de arquivos dele naquela dificuldade.
- Quem lê a Maestria hoje: a exigência mínima dos equipamentos (`GearItem.MinMastery`; valores em uso: 1, 2 e 3, o maior em `guardian`) e o card do canto.

### 1.2 Conteúdo que existe (`content/missions/**`, 159 arquivos JSON; `game/data/conceitos.json` nomeia 24 Conceitos)

| Disciplina | Ano | Missões por dificuldade (1/2/3/4/5) | Conceitos |
|---|---|---|---|
| Matemática | 1 | 7 / 10 / 6 / 2 / 4 | adicao-ate-10, composicao-ate-10, composicao-ate-5 (dif. 1 a 4); sistema-monetario (só dif. 5) |
| Matemática | 2 | 6 / 7 / 6 / 2 / 4 | adicao-ate-99-sem-reagrupamento, fatos-adicao-subtracao-ate-20, subtracao-ate-99-sem-reagrupamento (dif. 1 a 4); sistema-monetario (só dif. 5) |
| Matemática | 3 | 0 / 0 / 0 / 0 / 4 | só sistema-monetario (dif. 5) |
| Português | 1 | 12 / 8 / 6 / 2 / 0 | nome-e-som-das-letras, palavras-dissilabas-grupo-1, segmentacao-silabica-oral, vogais-letra-som (dif. 4: só palavras-dissilabas-grupo-1) |
| Português | 2 | 13 / 15 / 15 / 4 / 0 | 8 Conceitos nas dif. 1 a 3 (escrever-c-qu, escrever-p-b-t-d-f-v, escrever-palavras-silabas-ja-dominadas, nasalidade-m-n-til, silabas-ccv, silabas-cvc-e-v, trocar-silaba-palavra-nova, vogal-atona-final-e-o); na dif. 4 só c-qu, nasalidade, ccv e cvc |
| Português | 3 | 10 / 10 / 6 / 0 / 0 | adjetivo, ortografia-r-rr, ortografia-s-ss, pontuacao-e-dialogo, substantivo-e-verbo |

Total de Conceitos distintos por Papel hoje: **Matemática 7** e **Português 17**. É o teto da Maestria com os dados atuais, qualquer que seja `conceptSolves`.

### 1.3 Quantas vitórias de Conceito uma Expedição rende

Fatos de estrutura (`core/Generation/LevelPlan.cs`, `FloorGenerator.cs`, `RoomTypes.cs`, `game/data/rules.json`):

- Um Nível tem 3 ou 4 Andares (`floorsMin` 3, `floorsMax` 4); um Andar tem de 6 a 9 Salas (`roomsMin` 6, `roomsMax` 9).
- Fixas por Andar: entrada, Fast travel, Descanso e saída; o Marco existe em um Andar (e ocupa uma Sala); o último Andar tem o Desafio épico no lugar da saída. As Salas restantes são sorteadas só entre Combate e Puzzle (`Free = true`). Fórmula do código: Salas livres = `total - 4 - (Andar tem Marco ? 1 : 0)`, de 2 a 5 por Andar.
- Logo: 2 a 5 Salas livres por Andar, média 3,5, menos 1 no Andar do Marco, mais 1 (o épico) no último. **Média de 12,25 Salas com Desafio por Nível** (10,5 com 3 Andares, 14 com 4), cerca de 3,5 por Andar. Cada uma rende no máximo uma vitória de Conceito, só na primeira vez que é vencida.
- A dificuldade da Sala é a do número do Andar (`DungeonCombat.Setup`: `clamp(Andar, 1, 5)`, usando a dificuldade existente mais próxima) e o ano é o maior ano com Missões que seja menor ou igual ao Nível. Com no máximo 4 Andares, a dificuldade 5 (e portanto `sistema-monetario`) nunca aparece na Progressão de Nível 1 ou 2, e a dificuldade 4 só existe no 4º Andar.
- **Matemática Nível 3**: como existem Missões de ano 3 (só dif. 5), `Setup` escolhe o ano 3 e a dificuldade 5 em todos os Andares. Toda Sala da Progressão do Nível 3 de Matemática dá `sistema-monetario` (1 Conceito possível). O documento de produto ("Matemática do 3º ano ainda não tem Missões próprias e usa as do 2º") não bate com o código lido; não rodei o jogo para confirmar.
- Conceitos alcançáveis na Progressão: Matemática N1 = 3, N2 = 3, N3 = 1 (total 7); Português N1 = 4, N2 = 8, N3 = 5 (total 17).
- Sala já vencida de um Andar da Progressão **não luta de novo**: `StartFight` só abre luta com `!expedition.IsCleared(...)` e a exploração é gravada no save (`Progress.Explored`, `core/Saves/ProgressExploration.cs`). Uma segunda Expedição no mesmo Nível não rende novas vitórias na Progressão (não executei isso no motor).
- **Andares profundos**: não restauram exploração (`!deep && explored(key)` em `core/Dungeon/Expedition.cs`), então as Salas voltam a existir a cada Expedição. Rendem vitórias de Conceito (não há trava para o profundo em `RecordSolved`). Cada Andar profundo tem de 2 a 5 Salas livres (média 3,5), sem Marco, Item nem épico; a dificuldade acompanha a profundidade (`DungeonCombat.FreeSetup`, até ±1 em busca de problema novo, com `MissionHistory` para não repetir) e o ano é o do Nível liberado do Perfil.

### 1.4 Estimativa (simulação minha, não é número do jogo)

Simulei em Node, com os arquivos reais de `content/missions`, uma passada da Progressão por Nível (sorteio uniforme de Missão dentro do ano e da dificuldade, `conceptSolves` = 3, sem Atalho, sem Andar profundo, Salas pela fórmula do item 1.3, 20 000 repetições). Não usa o PCG32 do jogo, então vale como ordem de grandeza.

| Papel | Salas com Desafio por Nível (média) | Maestria média depois de 1 passada em N1 / N1+N2 / N1+N2+N3 | Ganho por Nível (N1 / N2 / N3) | Teto de Conceitos |
|---|---|---|---|---|
| Matemática | 12,25 | 2,35 / 4,72 / 5,72 | 2,35 / 2,37 / 1,00 | 7 |
| Português | 12,26 | 2,11 / 3,68 / 5,90 | 2,11 / 1,57 / 2,22 | 17 |

Leitura (fato da simulação): com 3 vitórias por Conceito e cerca de 12 Salas por Nível, a Progressão sozinha deixa Português com menos de 6 dos 17 pontos depois dos 3 Níveis. Matemática chega a cerca de 5,7 de 7. Cada Nível completo (uma Expedição que conclui o épico) rende cerca de 1 a 2,4 pontos.

Faltam fatos que não saem do código: quantas Expedições a criança faz, quanto tempo ela passa nos Andares profundos e se vence todas as Salas.

## 2. Como `core/Combat` modela o combate

Observação de caminho: `core/Combat/Battle.cs` (85 linhas) **não tem dano nem Recarga**. Ele só define `IBattle`, `BattleResult(Concept, Hits, Mistakes, Calmed)`, `IBattleMechanic` e `BattleMechanics.Default` (Mecânicas `nome`, `palavra`, `conta`, na ordem em que reconhecem os Desafios; `ChangeMechanic` entra em `game/Main.cs:80` se houver moedas em dados). O combate de Névoa mora em `FogFight.cs`, `CreatureAttacks.cs`, `CombatSession.cs`, `Energy.cs`, `AttackRhythm.cs` e `CombatTime.cs`. Os dados são `combat.fog.*` em `game/data/rules.json` e os tipos estão em `core/Rules/FogRules.cs` e `CombatRules.cs`.

### 2.1 Ataque básico, fraqueza, Recarga, Etapa (`core/Combat/FogFight.cs`)

- `BasicAttack { Precise, Diagonal, Charged }`. Cada um tem dano base em `fog.damage` = [3, 4, 6] e Recarga em `fog.rechargeSeconds` = [0,8; 1,2; 1,8].
- `Strike(attack)` só vale com `Phase == Fog` e Recarga zerada. Dano = `max(1, round(damage[attack] x (fraco ? weakMultiplier(2) : otherMultiplier(0,5))))`. Subtrai de `Fog`, coloca a Recarga do ataque (`RechargeTotal`: no Modo calmo `calmRechargeSeconds` 0,6; senão a do ataque) e soma `tirednessPerStrike` (0,12) ao cansaço. Devolve `StrikeResult(Damage, Weak, StageCleared)`.
- **Fraqueza**: `Weakness` é sorteada no construtor (`rng.NextInt(3)`); na primeira Etapa limpa, uma única troca é agendada (`ScheduleShift`, aviso de `shiftWarningSeconds` 2,5 s, depois `ShiftApplied`; só uma troca por luta, flag `shifted`).
- **Etapa**: `Stages` vem de `fog.stages` (2) ou do parâmetro `stages` do construtor (o épico e o Guardião usam `BossStages` = 3; `game/Combat/TrainingArea.cs:96`). Cada Etapa tem `stageFog` = 40 de Névoa. Zerar a Névoa muda a fase para `Qte` (janela `qteSeconds` 25, sem relógio no Modo calmo). `QtePassed()` encerra a Etapa (a Névoa dela não volta); na última Etapa é `Won` e `FinalBlow = true`. `QteFailed()` (erro, fim da janela ou dúvida) devolve `ceil(stageFog x returnFraction(0,5))` de Névoa e zera as Recargas.
- **Prêmio**: `Prize { Hit, Perfect, Close }` (dica, ou pouco tempo sobrando, vira `Close`; primeira tentativa com `perfectTimeLeft` 0,3 de tempo sobrando vira `Perfect`). `FogFight.EnergyFor(prize, EnergyRules)`: `Perfect` devolve `energy.perfect` (2), `Hit` devolve `energy.hit` (1), `Close` zero. O comentário do código diz que o prêmio muda espetáculo e Energia, nunca o progresso.
- **Cansaço** (`Tiredness`, 0 a 1): sobe a cada golpe, cai `tirednessDecayPerSecond` (0,02) por segundo na fase de Névoa, zera ao passar de Etapa, e alonga a Recarga e o respiro da criatura em até `maxSlowdown` (1,5) a mais (`CreatureAttacks.Warn`). Estonteada: `Stunned => Phase == Qte` (a criatura não ataca durante o QTE).
- `FogFight` lê `FogRules` (um `record` imutável) e o `Pcg32` recebido. Não há hoje parâmetro nem evento para somar ou multiplicar dano, Recarga, `stageFog` ou `returnFraction` fora de `FogRules` (as regras são globais; `CombatRules.ForYear(year)` só troca a bolsa e os sinais de Matemática por ano).

### 2.2 A criatura (`core/Combat/CreatureAttacks.cs`)

- `CreatureAttackKind { Lunge, Slam, Spin }` (Investida, Pulo, Giro), com `cooldownSeconds` [6, 8, 10] e `warningSeconds` [1,5; 1,8; 2], mais as áreas (`lungeWidth/Range`, `slamRadius`, `spinRadius`) e `minGapSeconds` 3. O aviso trava a mira (`Aim`); sair da área durante o aviso é desviar. Eventos: `Warned`, `Hit`, `Missed`. O comentário do código diz que recarga, aviso e cansaço não dependem do Nível.
- Quem aplica o dano à criança é o adaptador: `game/Combat/TrainingArea.cs:374` chama `session.CreatureHit()` (`CombatSession.CreatureHit`: `Energy.Lose(energy.creatureHit)` = 1).

### 2.3 Energia e erro (`CombatSession.cs`, `Energy.cs`)

- `Energy(max)`: `Max`, `Current`, `Lose`, `Recover`, `SetMaximum` (equipamento amplia a capacidade sem curar). Máximo de dados: `maxEnergy` (5; limite de validação 99, `core/Rules/GameRules.cs`).
- `CombatSession.Hit(perfect)`: recupera `energy.hit` ou `energy.perfect`. `Mistake()`: perde `energy.mistake` (1), conta a sequência de erros e oferece dica a partir de `MistakesForHint`; dica nunca custa. Na Expedição a mesma `Energy` passa de uma luta para a outra (`game/Dungeon/DungeonRun.cs:56` cria `new Energy(ctx.Rules.MaxEnergy + ctx.ExtraEnergy)`).
- `CombatTime.Scale`: `slowTimeScale` (0,3) enquanto a criança desenha ou no Modo calmo; o adaptador do Godot só copia `Scale` para `Engine.TimeScale`. `AttackRhythm` é a recarga de 0,8 s do jogador entre respostas.

### 2.4 Fronteira `core` e Godot (o que já existe para um modificador)

- `core` é C# puro e é barrado de ver o Godot por `ArquiteturaTests`. O motor fica em `game/Combat/TrainingArea*.cs` (partial): chama `fog.Strike`, `fog.QtePassed`, `fog.Tick`, `creatureAttacks.Update`, aplica o `Prize` à Energia (`TrainingArea.Fog.cs`, `QtePassed`, calculando o alvo a partir da Energia no início do QTE) e dispara `BattleEnded` e `ChallengeSolved`.
- O único caminho **já existente** para um bônus de Papel chegar ao combate é o do equipamento: `EquipmentBonuses(SpeedMultiplier, ExtraEnergy, InteractionReachBonus, Extras)` (`core/Equipment/EquipmentLooks.cs`), em que `Extras` é um `IReadOnlyDictionary<string, double>?` para "efeitos que não são os três de sempre". Hoje nenhum código lê `Extras` (a busca em `core` e `game` só acha a declaração). `ExtraEnergy` chega ao combate por `Energy.SetMaximum` e `new Energy(MaxEnergy + extra)` (`game/Combat/TrainingArea.Energy.cs`, com `Math.Clamp(bonus, 0, 2)` fixo no código, `game/Dungeon/DungeonContext.cs`, `game/Combat/MasterChallenge.cs`); velocidade e alcance vão para `ConfigureDungeonPlayer` e o Lobby (`game/Main.Inventory.cs`).
- Pontos de `core/Combat` onde hoje entra um número de dados: `FogFight` (construtor, `Strike`, `QteFailed`, `RechargeTotal`, `QtePassed`, via `rules`), `CreatureAttacks` (construtor com `CreatureRules`), `CombatSession.Hit/Mistake/CreatureHit` (via `rules.Energy`) e `FogFight.EnergyFor`. Todos recebem as regras por construtor ou parâmetro, sem Godot.
- Testes que fixam esse comportamento: `game.tests/FogFightTests.cs`, `CreatureAttacksTests.cs`, `CombatTests.cs`, `EquipmentEnergyTests.cs` e `ArquiteturaTests.cs` (core sem Godot, arquivo de código até 400 linhas).

## 3. Como o equipamento e a Maestria são guardados

### 3.1 Save (`core/Saves/SaveData.cs`, `Progress.cs`, `ProgressGrowth.cs`)

- `SaveData(Version, Profiles, Settings)`; `CurrentVersion = 1`. Campos novos opcionais são lidos como ausentes e saves antigos abrem na mesma versão (comentário do arquivo e `ProgressSave`). `SaveStore` tem migração por passos (`Migrate`, dicionário `migrations`): versão maior que a atual vira `SaveNotice.NewerVersion` e recomeça com cópia; falha de migração vira "save ilegível", nunca queda.
- `ProfileSave(Id, Nickname, Level, Appearance?, Progress?, Gear?, Worn?, Seen?, GuideSeen?)`: `Level` é o Nível liberado; `Gear` é `Dictionary<string Papel, Gear>`; `Worn` são os ids de Cosméticos vestidos.
- `ProgressSave` (tudo opcional): `EpicDone`, `Items`, `Floors`, `FastTravels`, `ShortcutsTried`, `Explored`, `FloorsDone`, `Cosmetics`, **`Solved`** (`Dictionary<string, int>`: vitórias por Conceito, chave `disciplina:conceito`), **`Mastered`** (lista de chaves `disciplina:conceito`), `Coins`, `Occurrences`, **`Equipment`** (lista de ids de equipamento que a criança possui) e `Merchant`.
- **Não existe** campo para pontos de talento, talentos escolhidos ou build, nem para a Maestria como número: ela é calculada a cada leitura contando `Mastered` com o prefixo da disciplina. Só existe Maestria por Papel (a disciplina); o Papel é a chave de texto da torre (`matematica`, `portugues`).
- Escrita: `ProgressStore.Update(profileId, p => p with {...})` regrava o `ProfileSave` inteiro e confirma por `SaveSession.Commit` (o comentário do código diz que cada mudança só vale depois de gravada). `ProgressServices` reúne `LevelProgress`, `ExplorationService`, `MasteryService` e `WalletService`. O treino não escreve em `Progress` (teste de arquitetura, segundo o documento técnico).
- A Maestria hoje nunca cai e não é consumida: nenhum código a gasta, ela só é comparada com `MinMastery`.

### 3.2 Equipamento (`core/Equipment/Gear.cs`, `Inventory.cs`, `game/data/equipamentos.json`)

- Dados: 3 espaços (`helmet`, `armor`, `accessory`), 3 efeitos com limite (`speed` 0,12, `energy` 2, `reach` 0,5) e 8 itens: `open` (helmet, energia 1, Maestria 1), `crystal` (helmet, alcance 0,15, Maestria 2), `scout` (armor, velocidade 0,12, 1), `scholar` (armor, alcance 0,35, 2), `guardian` (armor, energia 2, 3), `wings` (accessory, velocidade 0,04, 1), `lantern` (accessory, alcance 0,1, 1), `amulet` (accessory, energia 1, 2).
- `GearCatalog.Parse` valida na borda (espaço, efeito e limite existentes, id repetido, valor acima do limite). `GearCatalog.Bonuses(Gear)` soma por efeito, limitado pelo `limit` do efeito; `Equip` devolve `Result` com erro se `mastery < MinMastery`.
- Efeitos são classes `IGearEffect` (`EnergyEffect`, `SpeedEffect`, `ReachEffect`) registradas em `GearEffects.Default`; um efeito novo é uma classe mais uma linha de registro e uma entrada em `effects` no JSON (o catálogo recusa efeito do JSON sem classe).
- `Gear` guarda `slot -> id do item` e serializa como objeto simples (`GearJsonConverter`). `GearService.Get/Equip/Unequip/Set(profileId, role, ...)` grava em `ProfileSave.Gear[role]`; a Maestria exigida vem de fora (`MasteryService.Of`).
- A posse vive em `ProgressSave.Equipment`; `InventoryService.OwnedGear` junta isso ao que já está vestido. Equipamentos vêm das recompensas (`game/data/recompensas.json`, entradas com `gear`) e do Mercador (`game/data/mercador.json`, seção `gear`).
- Os bônus do equipamento valem em qualquer combate, inclusive no treino (documento de produto), e vestir nunca cura.

## Limitações

- Não rodei o jogo nem os testes: tudo vem de leitura de código e dados, mais a simulação do item 1.4.
- A simulação usa sorteio uniforme de Missão por dificuldade, não o PCG32 real, e supõe que a criança vence todas as Salas da Progressão.
- A afirmação sobre Matemática Nível 3 vem de `DungeonCombat.Setup` e dos arquivos de Missão; o documento de produto diz outra coisa.
