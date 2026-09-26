# Recuperação do CidadeUnity — 2026-09-25

Rastreio do que foi pedido, o que foi feito e o que ficou em aberto.

## Etapa 1 — Recuperar o cenário

**Pedido:** recuperar a última versão funcional sem reconstruir a cidade e sem perder
o que já funcionava. Diferenciar objeto apagado, desativado, deslocado, não carregado
e não renderizado. Não declarar causa sem evidência.

**Causa comprovada.** A cidade nunca foi apagada. `SceneDiag.Report` mostrou `City 02`
ativo, 5143 renderers, 4942 visíveis, bounds 1000×1000 m, zero `ShadowsOnly`, zero
renderer desligado. Os 77 sub-prefabs referenciados existiam, os 3750 `PrefabInstance`
estavam íntegros e o guid `711bca12f8388764fbf913a5e2004a1d` batia com o `.meta`.

O que quebrou foi o shader:

```
Shader error in 'City/Kenney Detail': redefinition of '_BaseMap_ST'
Shader error in 'City/Kenney Detail': redefinition of '_Surface'
```

`KenneyDetailInput.hlsl` **substitui** o `LitInput.hlsl` do URP. O URP 17.3 renomeou o
include guard (`UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED` → `UNIVERSAL_LIT_INPUT_INCLUDED`)
e tirou `_Surface` do CBUFFER, que agora vem de `Utils/SurfaceType.hlsl`. O original
passou a entrar duas vezes, o shader falhou, e tudo que o usava sumiu. A FECAP
sobreviveu porque usa URP Lit padrão.

**Preservado:** traçado das ruas, posições, FECAP, árvores, postes, carros, APV, decals,
272 ocorrências. Nada apagado. Backup em `Tools/backups/2026-09-25-RECOVERY/`.

Antes disso o projeto estava em Safe Mode por `error CS0619: 'Object.GetInstanceID()' is
obsolete`. O Unity 6.3 promoveu a API obsoleta a erro; `#pragma warning disable` não
suprime CS0619. Trocado por `GetEntityId()`, com os `HashSet`/`Dictionary` mudando de
`int` para `EntityId`.

## Etapa 2 — Bandeiras

**Causa comprovada.** `FlagProbe.Report` mediu: ponta do mastro em `(x, 8.275, 4.984)`,
finial em `(x, 8.300, 3.950)` — 1,03 m à frente, solto no ar. Pano 0,85 m fora do mastro.
O Z era fixo e não acompanhava a inclinação de 14°. Passo entre mastros 1,6 m contra
2,03 m de pano projetado em X: sobreposição de 0,42 m. Sem `Animator` ou `Animation` —
não era animação.

**Correção:** tudo derivado do próprio mastro de cada grupo, passo 2,4 m. Nada apagado.

## Etapa 3 — Marcadores e validação das ocorrências

- 272 ocorrências, 8 tipos, **zero códigos duplicados** (verificado).
- `DefectScanner` gravava `sharedMaterial.color`, o que **escreve no arquivo `.mat` do
  projeto**: a cor da última ocorrência vista ficava salva em disco. Trocado por
  `MaterialPropertyBlock`.
- Registro passou a exigir evidência: enquadramento no viewport, linha de visada por
  `RaycastAll` (descartando o próprio rover, já que a câmera vai montada nele) e
  distância. Cada recusa diz o motivo.
- Uma única função (`RegisterBlockedReason`) decide, e o HUD e a tecla `E` leem a mesma
  resposta — a dica não promete um registro que a confirmação recusaria.
- `AnalysisHUD` é um Canvas raiz separado de `GameUI`; por isso continuava desenhado
  sobre a tela de resultado. Agora é desligado junto.

## Etapa 4 — Dificuldade e pontuação

Valores no Inspector: evidência inédita +40, categoria correta +30, prioridade correta
+20, enquadramento excelente +10, registro falso −25, duplicata 0. Rodada de 180 s.

Modo desafio: o painel mostra "POSSÍVEL PROBLEMA" e "GRAVIDADE A CLASSIFICAR" até o
jogador responder. Fluxo: `E` captura → `1`–`8` tipo → `1`–`4` prioridade. Exige 2 s de
estabilidade. Registro falso pede `E` duas vezes em 2 s, para punir só confirmação
consciente.

**Fonte única de verdade:** sumiram `score`, `registered`, `countByType` e `pointsByType`
como estados paralelos. Existe só a lista `confirmed`; contador, resumo e ranking derivam
dela. `EndRound` tem guarda `roundClosed`. Reiniciar limpa a rodada e preserva o ranking
em `PlayerPrefs`.

## Etapa 5 — Interatividade

Central de chamados (região aproximada, distância arredondada e rumo em 8 direções, sem
marcar o alvo), indicador de qualidade sempre visível
(`ENCOBERTA` / `INSUFICIENTE` / `ADEQUADA` / `EXCELENTE`), e relatório final que separa
não achar de não fotografar.

## Bateria de aceitação

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Unity.exe" -batchmode -quit -nographics \
  -projectPath "C:/Users/gabri/Downloads/CidadeUnity" \
  -executeMethod AcceptanceTest.Run -logFile "Logs/accept.log"
```

Sai com código 1 se algo regredir. Estado atual: **tudo passou**.

Esta bateria pegou um defeito introduzido na etapa 3: o filtro de "ocorrência sem
geometria" exigia `Renderer`, mas rachadura é `DecalProjector` — as 23 rachaduras tinham
`renderers=0` e o tipo inteiro sairia do jogo. O teste agora usa a mesma função do
runtime, então não pode aprovar uma cena que o jogo recusaria.

## Em aberto — exige teste humano

Nenhum teste estático alcança estes:

- Dirigir o mapa com as duas câmeras e conferir se nada some indevidamente.
- Registrar uma ocorrência atrás de uma parede (deve ser bloqueado com motivo).
- Manter a tecla de registro pressionada (uma ocorrência não pode render duas vezes).
- Três reinícios consecutivos sem objeto duplicado nem perda de elemento.
- Calibrar `stabilitySeconds` (2 s), `roundSeconds` (180 s) e as faixas do chamado
  (50–260 m) com jogo real.

Performance nunca foi medida: 5143 renderers e 481 spot lights realtime.
