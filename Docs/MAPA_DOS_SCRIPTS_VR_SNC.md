# Mapa dos Scripts VR SNC

## Visão geral da cena

A cena principal usada pelo protótipo é `Assets/Scenes/SampleScene.unity`. O objeto anatômico ativo é `Sistema_Nervoso_Central`, antigo `Brain_AnatomicalOrganization`, que contém a organização hierárquica do sistema nervoso central. O objeto `Brain` original permanece desativado e preservado na cena para referência, enquanto o painel antigo foi mantido como `Painel_Antigo_SNC` e segue desativado.

O painel ativo é `Painel_Anatomico_VR_SNC`. Ele exibe o título `VR SNC`, o foco atual, a estrutura selecionada, o caminho anatômico e a lista de estruturas do nível atual. O ponteiro visual do controle direito foi identificado como `Ponteiro_Direito_SNC`. O botão físico de reset foi identificado como `Botao_Reset_SNC`.

## Fluxo de interação

A seleção funciona em camadas. O `GerenciadorSelecaoHierarquica` mantém um foco atual e só aceita seleção de filhos diretos desse foco. O `ControladorSelecaoPorRaio` atualiza o hover por raycast. Quando o usuário pressiona o trigger, o `MapeadorEntradaMetaQuest2` chama a seleção do nó apontado. O grip direito inicia o grab do alvo ativo por `ManipuladorObjetoSelecionado`. O painel chama os mesmos controladores para voltar, entrar, limpar seleção, isolar, restaurar e resetar.

O input do Meta Quest 2 vem do Action Map `BrainControls`. O trigger direito seleciona, o grip direito agarra, os botões direitos navegam entre níveis e o analógico direito manipula a estrutura selecionada. O campo serializado `inverterEixoHorizontal` controla o sentido do eixo horizontal, sem alterar o InputActionAsset.

O reset chama `GerenciadorRestauracao.RestaurarTudo()`. Esse fluxo mantém o `Brain` original desativado, mantém a organização anatômica ativa, encerra grab, restaura vista explodida, visibilidade, transformações, hover, seleção e foco.

## Mapa dos scripts

| Script técnico | Nome funcional em português | Função | Objeto da cena onde é usado | Tipo | Resumo para documentação acadêmica |
|---|---|---|---|---|---|
| `GerenciadorSelecaoHierarquica` | Gerenciador de Seleção Anatômica em Camadas | Controla foco, hover, seleção, isolamento e navegação hierárquica. | `Sistema_Nervoso_Central` | Essencial | Define a regra central de interação: apenas filhos diretos do foco atual podem ser selecionados, mantendo a exploração anatômica por níveis. |
| `ControladorSelecaoPorRaio` | Controlador de Seleção por Raio | Faz raycast a partir do controle/câmera e informa o nó em hover. | `Sistema_Nervoso_Central` | Essencial | Converte o apontamento do usuário em hover anatômico validado pelo gerenciador de seleção. |
| `MapeadorEntradaMetaQuest2` | Mapeador de Entrada Meta Quest 2 para o SNC | Liga trigger, grip, botões e analógicos aos controladores. | `Sistema_Nervoso_Central` | Essencial | Centraliza o mapeamento do Quest 2 para seleção, grab, navegação, reset, isolamento, painel e manipulação. |
| `ManipuladorObjetoSelecionado` | Manipulador da Estrutura Selecionada | Move, rotaciona e agarra a estrutura selecionada. | `Sistema_Nervoso_Central` | Essencial | Permite manipulação do alvo selecionado com analógico e grip, preservando seleção, colliders e hierarquia. |
| `GerenciadorRestauracao` | Gerenciador de Reset do SNC | Restaura estado original de seleção, visibilidade, transformações e foco. | `Sistema_Nervoso_Central`, `Botao_Reset_SNC`, painel | Essencial | Garante retorno ao estado inicial do protótipo sem reconstruir a cena ou remover componentes. |
| `ControladorVisibilidadeAnatomica` | Controlador de Visibilidade Anatômica | Mostra, oculta, isola e restaura renderers/nós. | `Sistema_Nervoso_Central` | Essencial | Permite isolar uma estrutura selecionada mantendo contexto anatômico e preservando componentes. |
| `ControladorVisaoExplodida` | Controlador de Vista Explodida Anatômica | Afasta temporariamente filhos do foco atual e restaura posições. | `Sistema_Nervoso_Central`, painel | Auxiliar | Facilita inspeção visual de estruturas agrupadas sem alterar a classificação anatômica. |
| `PainelInformacoesAnatomicas` | Painel de Informações Anatômicas | Atualiza textos de seleção, foco, caminho e metadados. | `Painel_Anatomico_VR_SNC`, `Painel_Antigo_SNC` | Essencial | Apresenta ao usuário informações anatômicas em português durante a interação. |
| `PainelNavegacaoAnatomica` | Painel de Navegação Anatômica | Lista filhos diretos do foco e liga botões de navegação. | `Painel_Anatomico_VR_SNC`, `Painel_Antigo_SNC` | Essencial | Transforma a hierarquia anatômica em lista navegável no painel, respeitando o foco atual. |
| `NoInteracaoAnatomica` | Nó de Interação Anatômica | Armazena pai, filhos, nível, seleção, estado original e nome exibido. | `Sistema_Nervoso_Central` e nós anatômicos | Essencial | Representa cada unidade navegável da anatomia e fornece nomes amigáveis em português para exibição. |
| `InformacaoEstruturaAnatomica` | Informações Acadêmicas da Estrutura Anatômica | Armazena metadados anatômicos e descrições. | Nós anatômicos quando presente | Auxiliar | Dá base textual e classificatória para documentação acadêmica e painel. |
| `AcompanhamentoCameraPainelInterface` | Painel VR Seguindo a Câmera | Mantém painel à frente da câmera com offset confortável. | `Painel_Anatomico_VR_SNC` | Essencial | Garante leitura e interação do painel em VR, agora com offset horizontal mais central. |
| `VisualPonteiroLaser` | Visual do Ponteiro Direito do SNC | Desenha linha do ponteiro e muda cor em hover. | `Ponteiro_Direito_SNC` | Auxiliar | Fornece feedback visual do raycast de seleção. |
| `RoteadorEntradaXR` | Roteador de Entrada XR do SNC | Encaminha InputActions genéricas para seleção, grab, reset, zoom e visibilidade. | `Sistema_Nervoso_Central` | Auxiliar | Mantém compatibilidade com fluxos XR genéricos e fallbacks de editor. |
| `ManipuladorModeloAnatomico` | Manipulador do Modelo Anatômico | Rotaciona, escala, agarra e reseta a raiz anatômica. | `Sistema_Nervoso_Central` | Auxiliar | Permite manipulação global do modelo quando esse fluxo está configurado. |
| `BotaoFisicoRestaurar` | Botão Físico de Reset do SNC | Aciona reset por clique/trigger físico. | `Botao_Reset_SNC` | Auxiliar | Permite reset por objeto físico na cena, com debounce. |
| `AdaptadorEntradaRastreamentoMao` | Adaptador de Entrada por Rastreamento de Mãos | Encaminha pinça, palma e grab de mãos ao roteador. | `Sistema_Nervoso_Central` quando configurado | Auxiliar | Mantém possibilidade de hand tracking sem mudar a lógica principal. |
| `GrupoAnatomico` | Grupo Anatômico do SNC | Guarda metadados de grupos anatômicos. | Grupos anatômicos quando presente | Auxiliar | Documenta agrupamentos sem controlar interação. |
| `EnumeracoesAnatomicas` | Enumerações Anatômicas do SNC | Define categorias anatômicas usadas por metadados. | Usado por scripts de anatomia | Auxiliar | Padroniza divisões, subdivisões, lados, lobos e status de revisão. |

## Limitações conhecidas

- O botão visível `Reposicionar painel` não foi adicionado nesta etapa para evitar recriar a UI ou alterar a arquitetura do painel. O script `AcompanhamentoCameraPainelInterface` disponibiliza o método `ReposicionarDianteDaCamera()` para uma ligação futura, caso seja decidido adicionar um botão dedicado.
- A tradução de nomes anatômicos individuais é feita apenas para exibição quando há mapeamento conhecido. Os 240 objetos anatômicos individuais não foram renomeados.
- A ferramenta de setup dentro de `MapeadorEntradaMetaQuest2` continua existindo para manutenção no editor, mas não foi executada nesta tarefa.
