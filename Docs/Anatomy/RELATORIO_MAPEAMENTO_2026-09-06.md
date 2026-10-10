# Mapeamento anatômico e base do Oráculo

Data local: 06/09/2026. Escopo autorizado: associação das 240 representações aos grupos existentes, correção de exibição e relatório. Não houve mudança em meshes, materiais, árvore de GameObjects, pacotes, permissões ou perfis de renderização nesta etapa.

## Resultado e limite atual

O catálogo técnico foi extraído e validado para 240 objetos da cena salva. Os scripts de aplicação e seleção foram implementados e passaram na compilação C# fora do Editor. **A aplicação dos novos vínculos na SampleScene não ocorreu**, porque o Unity encerrou durante a abertura por falta de licença reconhecida. Não há teste Play Mode ou Quest desta alteração.

| Verificação | Resultado |
|---|---:|
| Representações OBJ na cena | 240 |
| Assets distintos referenciados | 240 |
| File IDs distintos | 240 |
| Conceitos FMA distintos | 216 |
| Representações BP distintas | 216 |
| Grupos e raiz preservados | 32 |
| Metadados BrainStructureInfo existentes | 240 |
| Novos nós-folha aplicados na cena | 0 |
| Nós-folha previstos pela ferramenta | 240 |
| Nós previstos após aplicação | 272 |
| Registros editoriais iniciais | 248 |
| Textos científicos aprovados nesta etapa | 0 |
| Compilação C# dos assemblies Runtime e Editor | Zero erros |

As duas malhas do cerebelo exemplificam a identidade composta: ambas têm `FMA67944` e `BP37207`, mas os File IDs são `FJ3834` e `FJ3876`. Os arquivos continuam separados. A chave do catálogo combina GUID do asset, File ID, Representation ID e FMA.

## Arquivos entregues

- `anatomical-mapping.json`: associação de representações, conceitos e grupos, pendências, hashes dos arquivos e estado `applied: false`.
- `anatomical-representations.csv`: 240 linhas com nomes originais/de exibição, classificação preservada e IDs.
- `anatomical-groups.csv`: 32 grupos, pais, membros diretos e descendentes.
- `../../outputs/oraculo-20260906/Oraculo_Anatomico.xlsx`: planilha com as abas Inicio, Representacoes, Grupos, Conceitos, Conteudo, Pendencias e Fontes.
- `DECISOES_E_ORACULO_2026-09-06.md`: decisões da entrevista, proposta de autoria e perguntas editoriais abertas.
- `oracle-sources.json`: referências iniciais e seu papel na associação ou na redação.
- `exportar-auditoria-fontes.mjs`: extração reproduzível dos arquivos salvos, sem editar a cena.

Os IDs de instância do export offline usam `sceneYaml:<GUID da cena>:<fileID serializado>`. São identificadores de auditoria do arquivo salvo. Não foram apresentados como GlobalObjectId confirmado pelo Editor; a ferramenta de aplicação gera seus próprios IDs via API Unity. A planilha deve ser regenerada do export do Editor após a aplicação, antes de desenvolver um importador de conteúdo.

## Implementação preparada

- `Assets/Scripts/Anatomy/BrainAnatomyBinding.cs`: identidade do conceito/representação e coleção de membros para grupos.
- `Assets/Editor/BrainAnatomyMappingEditor.cs`: aplicação aditiva, backup da cena, referência direta das folhas ao BrainStructureInfo próprio, colliders quando necessários e validação de grafo/geometria/idempotência.
- `BrainInteractionNode.cs`: referência ao binding e correção de `rigth` somente na exibição.
- `BrainNestedSelectionManager.cs` e `BrainRaySelectionController.cs`: resolver um acerto numa folha como o filho direto do foco atual, preservando a navegação por camadas.
- `BrainInfoPanel.cs`: identificação individual por código e nome de origem; grupos mostram identidade e número de representações, sem herdar a ficha da primeira peça. A contagem funciona também antes de aplicar o novo binding.
- `.gitignore`: exceção específica para versionar a ferramenta de mapeamento e seu `.meta`, preservando as demais exclusões de ferramentas locais.

O menu preparado é `Tools > VR SNC > Anatomy > Apply and Export Validated Mapping`. Ele exige SampleScene salva, Editor ocioso e ausência de build em execução. Depois aplica os vínculos, verifica a repetição sem duplicação e salva a cena. O menu `Validate Current Mapping` verifica a associação persistida. **Esses menus ainda não foram executados nesta entrega.**

## Verificação realizada

1. Auditoria independente contou os 240 OBJs, GUIDs e File IDs e confirmou os 216 conceitos. Todos os modelos registram `brain5.0`.
2. O export lê o GUID do prefab de modelo realmente referenciado e os cabeçalhos do respectivo OBJ. Não usa apenas sourceObjectName para escolher uma malha.
3. Uma segunda leitura, em outro parser, comparou os seis campos de cabeçalho dos 240 arquivos com o JSON e conferiu chaves, pertencimento e hashes.
4. Os assemblies `Assembly-CSharp` e `Assembly-CSharp-Editor` passaram pelo Roslyn instalado com Unity 6000.3.5f2, incluindo os novos scripts. Foram usados arquivos de resposta do cache com saídas direcionadas para uma pasta local separada. Uma referência antiga do Code Coverage foi atualizada somente no arquivo temporário de compilação para a versão instalada; Packages e Library não foram modificados por essa verificação.
5. A planilha teve fórmulas e contagens verificadas, todas as abas renderizadas para inspeção visual e o XLSX reaberto por um leitor independente. Isso incluiu filtros, congelamento de cabeçalho, lista de estados editoriais e comparação de campos do catálogo.

Compilar os assemblies fora do Editor verifica sintaxe, tipos e referências. Não comprova callbacks da Unity, serialização de novas instâncias, execução de raycasts ou ergonomia no Quest.

## Pendências de dados

| Ocorrência | Quantidade | Tratamento |
|---|---:|---|
| Nome português ausente | 240 | Campo editorial pendente; não traduzido automaticamente. |
| Lateralidade serializada Unspecified | 41 | Preservada; mistura peças medianas e rótulos laterais não classificados. |
| Grafia rigth/Rigth | 5 | Correção no texto de exibição; original preservado. |
| Nome da cena diferente do inglês no OBJ | 40 | Registrado para revisão; inclui acréscimos de lado, não significa 40 erros confirmados. |
| Grupo vazio | 1 | Plexo_Coroide preservado. |

São 327 ocorrências, não 327 estruturas com erro. As duas associações superiores de brachium permanecem cruzadas entre rótulo e asset. A confirmação do erro ortográfico não autoriza inferir qual geometria é esquerda/direita; nenhuma troca de malha foi feita.

Há ainda um ponto de interação a validar depois de adicionar as folhas: os pivôs dos OBJs podem estar afastados da geometria anatômica, e o manipulador atual gira em torno de transform.position. A rotação individual pode produzir órbita em torno de um pivô distante. Não foram alterados vértices ou pivôs nesta fase.

## Bloqueio de licença

O log `Logs/AnatomyMappingEditor.log` registra `No valid Unity Editor license found. Please activate your license.`, ausência de entitlement `com.unity.editor.ui` e saída 198. A Unity Hub foi chamada pelo próprio Editor. Não houve compra, troca de conta ou alteração de licença.

Próximo passo dependente: recuperar a licença na Unity Hub e abrir este projeto. Depois executar a compilação/validação do Editor, aplicar o menu, validar as 272 referências e testar seleção por grupo/folha, isolamento e restauração. A publicação no Quest permanece pendente dessas verificações.
