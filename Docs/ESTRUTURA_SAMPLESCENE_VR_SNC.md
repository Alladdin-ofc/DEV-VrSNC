# Estrutura da SampleScene VR SNC

Este documento descreve os objetos principais de `Assets/Scenes/SampleScene.unity` após a organização visual do protótipo VR do Sistema Nervoso Central. O foco é explicar o que está ativo, o que está desativado e o que pertence ao template VR ou ao sistema customizado do SNC.

## Resumo da cena

A cena combina o template VR/XR com o sistema anatômico customizado:

- o rig XR fornece câmera, controladores, mãos e eventos de UI;
- `Sistema_Nervoso_Central` contém a organização anatômica ativa;
- `Painel_Anatomico_VR_SNC` é o painel world-space ativo;
- `Brain` permanece desativado como modelo original preservado;
- teleporte/locomoção por área permanecem desativados;
- o painel antigo permanece desativado para compatibilidade.

## Objetos principais

| Objeto | Estado | Origem | Função | Pode remover? |
|---|---|---|---|---|
| `XR Origin Hands (XR Rig)` | Ativo por prefab/instância XR | Template VR/XR | Contém câmera, controladores, mãos e referências usadas por input, raycast e UI. | Não. É base da interação VR. |
| `UI` | Desativado | Template VR | UI original do template, preservada como referência. | Não nesta etapa. |
| `Environment` | Ativo | Template/ambiente | Ambiente visual da cena. | Não nesta etapa. |
| `Lighting` | Ativo | Template/ambiente | Luzes e configuração visual geral. | Não nesta etapa. |
| `EventSystem` | Ativo | Template VR/UI | Permite interação com Canvas e botões do painel. | Não. Necessário para UI. |
| `Hands Permissions Manager` | Ativo por prefab/instância | Template/Meta XR | Gerencia permissões de hand tracking quando o template usa mãos. | Não nesta etapa. |
| `Brain` | Desativado | Modelo original | Modelo original preservado. Não é o objeto anatômico interativo ativo. | Não. Deve permanecer preservado. |
| `Sistema_Nervoso_Central` | Ativo | Sistema customizado SNC | Objeto principal da hierarquia anatômica organizada. Era `Brain_AnatomicalOrganization`. | Não. É o núcleo do protótipo. |
| `Painel_Anatomico_VR_SNC` | Ativo | Sistema customizado SNC | Painel VR de informações, navegação e botões principais. Era `Brain_VR_AnatomyPanel`. | Não. É o painel atual. |
| `Painel_Antigo_SNC` | Desativado | Sistema customizado/legado | Painel antigo preservado para compatibilidade e referência. Era `Brain_UI_Panel`. | Não nesta etapa. |
| `Ponteiro_Direito_SNC` | Ativo | Sistema customizado SNC | Visual do laser/ponteiro direito usado para mirar estruturas e painel. Era `Brain_Right_LaserPointer`. | Não. Ajuda feedback visual. |
| `Botao_Reset_SNC` | Desativado | Sistema customizado SNC | Botão físico de reset preservado. Era `Reset_SNC_Button`. | Não nesta etapa. |
| `Teleport Area Setup` | Desativado | Template VR | Configuração de área de teleporte do template. | Não remover; manter desativado. |
| `Teleport Area` | Desativado | Template VR | Área de teleporte do template. | Não remover; manter desativado. |

## Sistema_Nervoso_Central

`Sistema_Nervoso_Central` é o objeto principal ativo do sistema anatômico. Ele preserva a organização já montada e contém os scripts centrais:

- `NoInteracaoAnatomica`;
- `GerenciadorSelecaoHierarquica`;
- `ControladorSelecaoPorRaio`;
- `MapeadorEntradaMetaQuest2`;
- `ManipuladorObjetoSelecionado`;
- `ManipuladorModeloAnatomico`;
- `GerenciadorRestauracao`;
- `ControladorVisibilidadeAnatomica`;
- `ControladorVisaoExplodida`;
- `RoteadorEntradaXR`;
- `AdaptadorEntradaRastreamentoMao`.

Esse objeto substitui visualmente o nome antigo `Brain_AnatomicalOrganization`, mas os scripts mantêm fallback para reconhecer os dois nomes quando necessário. A hierarquia anatômica abaixo dele não deve ser reconstruída nem reclassificada.

## Hierarquia anatômica ativa

A hierarquia ativa contém grupos como:

- `Cerebro`;
- `Cerebelo`;
- `Tronco_Encefalico`;
- `Sistema_Ventricular`;
- `Telencefalo`;
- `Diencefalo`;
- `Cortex`;
- lobos corticais;
- ventrículos e estruturas relacionadas.

Os objetos anatômicos individuais não devem ser renomeados nesta etapa. A tradução amigável para o usuário acontece por texto exibido no painel, principalmente via `NoInteracaoAnatomica.Exibicaonome`.

## Painel_Anatomico_VR_SNC

`Painel_Anatomico_VR_SNC` é o painel atual ativo. Ele é um Canvas em world-space e contém:

- textos de título, status, caminho anatômico e descrição;
- lista de navegação;
- botões de Voltar, Entrar, Resetar SNC, Isolar/Restaurar, Limpar seleção e Mostrar/Ocultar lista;
- script `PainelInformacoesAnatomicas`;
- script `PainelNavegacaoAnatomica`;
- script `AcompanhamentoCameraPainelInterface`.

O painel deve permanecer como está em termos de arquitetura. Ajustes permitidos são textos, offsets, ligações de botão e correções pontuais da lista.

## Painel_Antigo_SNC

`Painel_Antigo_SNC` está desativado. Ele ainda possui componentes e scripts, mas não é o painel principal em uso. Deve ser preservado para referência, sem apagar, mover para legado ou remover scripts.

## Brain desativado

`Brain` está desativado e representa o modelo original preservado. Ele não deve ser usado como root da seleção anatômica atual e não deve ser removido.

## Ponteiro_Direito_SNC

`Ponteiro_Direito_SNC` está ativo e fornece feedback visual do raio do controle direito. Ele apoia:

- hover;
- seleção com trigger;
- mira no painel;
- percepção espacial do usuário.

Não substitui o `XR Ray Interactor` e não deve ser usado para trocar a arquitetura de input.

## EventSystem e UI

`EventSystem` está ativo e deve permanecer ativo para que botões do painel funcionem. A UI do template está desativada, mas preservada.

O painel do SNC tem prioridade quando o raio aponta para ele: nessa situação, a manipulação anatômica por grip/analógico é bloqueada para evitar mover o cérebro enquanto o usuário tenta clicar na UI.

## Teleporte e locomoção

`Teleport Area Setup` e `Teleport Area` estão desativados. Essa decisão preserva o comportamento atual do protótipo, em que o foco é seleção anatômica e manipulação do SNC, não navegação por teleporte.

## Fluxo de seleção na cena

1. O controle direito aponta para uma estrutura.
2. `ControladorSelecaoPorRaio` faz raycast.
3. O nó encontrado é enviado para `GerenciadorSelecaoHierarquica`.
4. O gerenciador valida se o nó é filho direto do foco atual.
5. A cor de hover é aplicada se o candidato for válido.
6. Trigger confirma seleção.
7. A cor de seleção é aplicada.
8. `PainelInformacoesAnatomicas` atualiza os textos.
9. `PainelNavegacaoAnatomica` atualiza a lista de filhos diretos do foco.

## Fluxo do painel

O painel exibe:

- estrutura selecionada;
- estrutura em foco;
- caminho anatômico;
- lista de filhos diretos do foco atual;
- botões para comandos já existentes.

Quando não há seleção, o painel mostra `Nenhuma estrutura selecionada` e mantém o caminho em `Sistema Nervoso Central`.

O título da lista deve ser sempre `Estruturas de: {estrutura atual}`, usando o foco atual da navegação.

## Fluxo de input Meta Quest 2

O `MapeadorEntradaMetaQuest2` concentra as entradas:

- trigger direito seleciona a estrutura apontada;
- grip direito inicia/encerra grab do alvo ativo;
- analógico manipula aproximação/afastamento e rotação;
- botões de navegação chamam Voltar/Entrar;
- botões do controle esquerdo chamam reset, limpar seleção, isolamento, visibilidade e painel.

Quando o raio direito está sobre o painel do SNC, o painel recebe prioridade e o SNC não é manipulado.

## Fluxo de reset

`GerenciadorRestauracao` usa os estados originais armazenados nos nós para restaurar:

- transform;
- escala;
- estado ativo;
- seleção;
- hover;
- isolamento;
- exploded view.

O reset não deve remover componentes nem reconstruir a hierarquia.

## Fluxo de visibilidade e exploded view

`ControladorVisibilidadeAnatomica` controla isolamento e restauração visual de nós. `ControladorVisaoExplodida` desloca temporariamente filhos diretos para visualização didática e restaura as posições depois.

Essas operações dependem dos nós anatômicos existentes e não devem reclassificar objetos.

## Limitações conhecidas

- O botão físico `Botao_Reset_SNC` está desativado.
- O painel antigo permanece na cena, mas não deve ser usado como painel principal.
- A lista de navegação depende de `atualFocoNo` e dos `FilhoNos` já serializados.
- A documentação não altera os 240 objetos anatômicos individuais.
- O botão visível “Reposicionar painel” não foi criado nesta etapa para evitar recriar UI; o script do painel expõe um método público de reposicionamento para uma ligação futura.
