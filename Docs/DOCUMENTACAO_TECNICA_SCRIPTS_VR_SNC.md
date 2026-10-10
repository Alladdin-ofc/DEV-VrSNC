# Documentação Técnica dos Scripts VR SNC

Este documento descreve os scripts customizados usados no protótipo VR do Sistema Nervoso Central. A cena principal é `Assets/Scenes/SampleScene.unity`, com o objeto anatômico ativo `Sistema_Nervoso_Central` e o painel ativo `Painel_Anatomico_VR_SNC`.

## Visão geral da arquitetura atual

O projeto usa a seguinte arquitetura documentada:

- `NoInteracaoAnatomica` identifica cada nó anatômico navegável.
- `GerenciadorSelecaoHierarquica` mantém o foco, hover, seleção e navegação por camadas.
- `ControladorSelecaoPorRaio` aponta para estruturas com raycast e envia candidatos ao gerenciador.
- `MapeadorEntradaMetaQuest2` liga ações do Meta Quest 2 aos métodos já existentes.
- `PainelInformacoesAnatomicas` exibe estrutura selecionada, foco e caminho anatômico.
- `PainelNavegacaoAnatomica` lista apenas os filhos diretos do foco atual.
- `ManipuladorObjetoSelecionado` move, rotaciona ou agarra a estrutura selecionada; sem seleção, manipula o SNC raiz.

Não há substituição de arquitetura nesta documentação. O objetivo é explicar o sistema existente e registrar os pontos que podem ou não ser alterados futuramente.

## Tabela de scripts

| Script técnico | Nome funcional em português | Objeto onde está anexado | Função | Pode mexer? |
|---|---|---|---|---|
| `NoInteracaoAnatomica.cs` | Nó de Interação Anatômica | `Sistema_Nervoso_Central` e nós anatômicos principais | Guarda pai, filhos, nível, nome exibido, estado original e regras de pertencimento anatômico. | Evitar. Alterar só comentários ou correções muito justificadas. |
| `GerenciadorSelecaoHierarquica.cs` | Gerenciador de Seleção Anatômica em Camadas | `Sistema_Nervoso_Central` | Controla foco, hover, seleção, seleção por filhos diretos, isolamento opcional, painel e highlights. | Evitar. É o núcleo funcional da seleção. |
| `InformacaoEstruturaAnatomica.cs` | Informações Acadêmicas da Estrutura | Muitos nós anatômicos e estruturas individuais | Guarda divisão, subdivisão, região, descrição curta e notas para exibição/documentação. | Evitar mudanças de lógica; conteúdo pode ser preenchido com cuidado. |
| `EnumeracoesAnatomicas.cs` | Enumerações Anatômicas do SNC | Asset de código, usado por `InformacaoEstruturaAnatomica` | Define categorias anatômicas usadas como metadados. | Evitar, pois afeta serialização. |
| `ControladorSelecaoPorRaio.cs` | Controlador de Seleção por Raio | `Sistema_Nervoso_Central` | Faz raycast a partir do controle/câmera, localiza `NoInteracaoAnatomica` e atualiza hover/seleção. | Pode ajustar com cuidado, sem mudar regra de seleção. |
| `MapeadorEntradaMetaQuest2.cs` | Mapeador de Entrada Meta Quest 2 | `Sistema_Nervoso_Central` | Liga trigger, grip, analógico e botões A/B/X/Y às ações de seleção, grab, reset, painel, isolamento e exploded view. | Pode ajustar entradas e prioridades, sem recriar InputActions. |
| `ManipuladorObjetoSelecionado.cs` | Manipulador da Estrutura Selecionada | `Sistema_Nervoso_Central` | Move/rotaciona pelo analógico e faz grab do selecionadoNo ou do root quando não há seleção. | Pode ajustar fluidez, deadzone e sentido do eixo. |
| `ManipuladorModeloAnatomico.cs` | Manipulador do Modelo Raiz | `Sistema_Nervoso_Central` | Manipula o modelo completo por rotação, zoom e escala quando usado por entradas antigas/fallbacks. | Pode ajustar parâmetros, mas não substituir o manipulador atual. |
| `RoteadorEntradaXR.cs` | Roteador de Entrada XR | `Sistema_Nervoso_Central` | Encaminha chamadas genéricas de XR para seleção, reset, painel e manipulação. | Pode ajustar integração, preservando chamadas públicas. |
| `GerenciadorRestauracao.cs` | Gerenciador de Reset do SNC | `Sistema_Nervoso_Central` | Captura/restaura posição, rotação, escala, seleção, visibilidade e exploded view. | Pode ajustar bugs de reset, sem remover estado original. |
| `ControladorVisibilidadeAnatomica.cs` | Controlador de Visibilidade Anatômica | `Sistema_Nervoso_Central` | Isola, oculta, restaura e mantém visibilidade de nós e descendentes. | Pode ajustar regras visuais, sem alterar hierarquia. |
| `ControladorVisaoExplodida.cs` | Controlador de Vista Explodida | `Sistema_Nervoso_Central` | Afasta filhos diretos do foco/seleção para visualização didática e restaura posições. | Pode ajustar distância e elegibilidade. |
| `PainelInformacoesAnatomicas.cs` | Painel de Informações Anatômicas | `Painel_Anatomico_VR_SNC`; também no painel antigo desativado | Atualiza textos do painel com seleção, foco, caminho, descrição, divisão e nível. | Pode ajustar textos e atualização visual. |
| `PainelNavegacaoAnatomica.cs` | Painel de Navegação Anatômica | `Painel_Anatomico_VR_SNC`; também no painel antigo desativado | Cria botões para filhos diretos do `atualFocoNo` e liga botões Voltar/Entrar/Resetar/Isolar. | Pode ajustar UI e atualização da lista. |
| `AcompanhamentoCameraPainelInterface.cs` | Painel VR Seguindo a Câmera | `Painel_Anatomico_VR_SNC` | Mantém o painel à frente da câmera com distância e offset configuráveis. | Pode ajustar offsets e suavização. |
| `VisualPonteiroLaser.cs` | Visual do Ponteiro Direito do SNC | `Ponteiro_Direito_SNC` | Mostra linha/ponto do laser do controle direito. | Pode ajustar apenas visual. |
| `BotaoFisicoRestaurar.cs` | Botão Físico de Reset do SNC | `Botao_Reset_SNC` desativado | Aciona reset por colisão/interação física quando o botão está ativo. | Pode ajustar se o botão for reativado. |
| `AdaptadorEntradaRastreamentoMao.cs` | Adaptador de Entrada por Mãos | `Sistema_Nervoso_Central` | Adaptador para gestos/hand tracking quando usados como entrada alternativa. | Auxiliar; mexer só se hand tracking for foco. |
| `GrupoAnatomico.cs` | Grupo Anatômico do SNC | Nós anatômicos de grupo | Marca grupos anatômicos, relações e tipo de agrupamento para documentação/organização. | Evitar mudanças estruturais. |

## Scripts que controlam seleção anatômica

`NoInteracaoAnatomica` representa cada unidade navegável. Ele guarda o pai, os filhos diretos, o nível do nó, se é selecionável e o estado original para reset.

`GerenciadorSelecaoHierarquica` é o núcleo da seleção em camadas. A regra importante é: quando existe um foco atual, apenas filhos diretos desse foco são candidatos válidos. Isso preserva a navegação anatômica por níveis e impede selecionar estruturas fora do contexto atual.

`ControladorSelecaoPorRaio` faz o raycast a partir do controle direito ou da câmera. Ele encontra o `NoInteracaoAnatomica` mais próximo no objeto atingido ou em seus pais, envia esse nó ao `GerenciadorSelecaoHierarquica` como hover e confirma a seleção quando o trigger chama `SelecionarFocoAtual`.

Esses scripts preservam as cores de hover e de seleção por `MaterialPropertyBlock`, sem trocar materiais importados.

## Scripts que controlam painel

`PainelInformacoesAnatomicas` cuida dos textos principais:

- título `VR SNC`;
- status de estrutura selecionada ou foco;
- caminho anatômico;
- nome exibido;
- nome técnico;
- divisão/subdivisão;
- descrição e nível.

`PainelNavegacaoAnatomica` cuida da lista de navegação. A lista deve mostrar apenas `FilhoNos` diretos do `atualFocoNo`. O título da lista usa `Estruturas de: {nomeDaEstruturaAtual}` e acompanha o foco atual. Os botões chamam métodos já existentes:

- `VoltarAoNoPai` para Voltar;
- `EntrarNoSelecionado` para Entrar;
- `RestaurarTudo` para Resetar SNC;
- `AlternarIsolamento` para Isolar/Restaurar;
- `LimparSelecao` para Limpar seleção;
- `AlternarLista` para Mostrar/Ocultar lista.

`AcompanhamentoCameraPainelInterface` posiciona o painel em world-space em relação à câmera, com offset horizontal zerado para manter o painel mais centralizado.

## Scripts que controlam input Meta Quest 2

`MapeadorEntradaMetaQuest2` é o ponto de ligação entre o Input System e o sistema anatômico. Ele resolve o Action Map `BrainControls`, habilita as ações e chama os controladores corretos:

- trigger direito: seleção por raio;
- grip direito: início/fim de grab;
- analógico direito: aproximação/afastamento e rotação;
- botões de navegação: voltar/entrar;
- botões do controle esquerdo: reset, limpar seleção, isolamento, visibilidade e painel.

O script também tem fallbacks de editor para teclado/mouse, úteis para validar sem headset.

## Scripts que controlam movimentação e grab

`ManipuladorObjetoSelecionado` é o manipulador principal da estrutura ativa. A ordem de prioridade documentada é:

1. se existe `selecionadoNo`, manipula somente o transform desse nó;
2. se não existe `selecionadoNo`, manipula `Sistema_Nervoso_Central`;
3. o grip captura posição e rotação iniciais do controle e do alvo;
4. enquanto o grip está pressionado, o alvo segue a mão com suavização;
5. ao soltar grip, o alvo para de seguir.

O analógico usa uma zona morta (*deadzone*) para evitar deriva. O eixo vertical aproxima ou afasta pela direção da câmera. O eixo horizontal rotaciona em torno do eixo vertical da câmera, e `inverterEixoHorizontal` define a orientação configurada para esse movimento.

`ManipuladorModeloAnatomico` ainda existe para compatibilidade com manipulação do modelo raiz, mas a manipulação selecionada deve passar por `ManipuladorObjetoSelecionado`.

## Scripts que controlam reset, visibilidade e exploded view

`GerenciadorRestauracao` restaura o estado original registrado nos nós, limpa seleção, restaura visibilidade e desfaz exploded view.

`ControladorVisibilidadeAnatomica` controla ocultar/restaurar estruturas e isolamento visual. Ele trabalha sobre nós existentes, sem reconstruir hierarquia.

`ControladorVisaoExplodida` desloca filhos diretos para visualização didática e guarda posições locais para restaurar. Ele deve continuar respeitando o foco atual e os nós marcados como explodíveis.

## Relação entre scripts principais

`ControladorSelecaoPorRaio` detecta uma estrutura por raycast e chama `GerenciadorSelecaoHierarquica`.

`GerenciadorSelecaoHierarquica` valida se o nó é filho direto do foco atual, aplica as cores de hover e seleção, atualiza `PainelInformacoesAnatomicas` e chama `PainelNavegacaoAnatomica.Atualizar`.

`PainelNavegacaoAnatomica` mostra botões para os filhos diretos do foco atual. Quando o usuário clica em um filho, o painel chama `SelecionarNo` no gerenciador.

`PainelInformacoesAnatomicas` apenas exibe o estado atual; ele não decide a seleção.

`MapeadorEntradaMetaQuest2` liga controles físicos a esses scripts. Ele não deve conter regra anatômica própria, apenas prioridades de input.

`ManipuladorObjetoSelecionado` consulta `GerenciadorSelecaoHierarquica.Selecionadono` para saber qual transform deve ser manipulado.

## Scripts que não devem ser alterados sem necessidade

Devem permanecer estáveis:

- `NoInteracaoAnatomica.cs`;
- `GerenciadorSelecaoHierarquica.cs`;
- `InformacaoEstruturaAnatomica.cs`;
- `EnumeracoesAnatomicas.cs`.

Esses scripts sustentam a seleção anatômica em camadas, nomes exibidos, metadados serializados e regras de foco/filhos diretos.

## Ajustes futuros possíveis

Podem ser ajustados futuramente com menor risco:

- melhorar layout visual do `PainelInformacoesAnatomicas`;
- adicionar botão de reposicionar painel ligando a `AcompanhamentoCameraPainelInterface.ReposicionarDianteDaCamera`;
- ajustar velocidades do `ManipuladorObjetoSelecionado`;
- ajustar distância do `AcompanhamentoCameraPainelInterface`;
- melhorar dados acadêmicos em `InformacaoEstruturaAnatomica`;
- criar testes de play mode para seleção por camada.

## Limitações conhecidas

- A documentação dos 240 objetos anatômicos individuais depende dos metadados existentes em cada `InformacaoEstruturaAnatomica`.
- O painel antigo ainda possui scripts, mas está desativado e deve ser mantido para compatibilidade até decisão posterior.
- A detecção de UI bloqueadora no input prioriza Canvas world-space do painel do SNC; outros painéis do template VR não são foco desta etapa.
- O botão visual “Reposicionar painel” não foi recriado nesta etapa; `AcompanhamentoCameraPainelInterface` expõe um método público para ligação futura, sem mudança arquitetural.
