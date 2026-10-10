# Decisões do projeto e Oráculo anatômico

Registro da entrevista de 06/09/2026. Este documento distingue escolhas aprovadas, mapeamento técnico e questões editoriais ainda abertas.

## Escolhas confirmadas

| Item | Decisão do autor | Consequência |
|---|---|---|
| 1 | Manter Sistema Nervoso Central e a árvore atual | A medula espinal poderá ser adicionada no futuro; não criar Encéfalo nem alterar a raiz nesta etapa. |
| 2 | Modelos obtidos em BodyParts3D; base FMA; bibliografia ainda indefinida | Preservar dados locais e pesquisar referências para terminologia e conteúdo separadamente. |
| 3 | Preservar nomes e IDs originais | Manter nomes de assets, GameObjects, GUIDs e strings de procedência; usar campos de exibição separados. |
| 4 | `rigth` foi erro de digitação do autor | Corrigir somente a exibição para `right`; não trocar geometria ou vínculos esquerdo/direito com base nessa correção. |
| 5 | Agrupar conceitos sem fundir malhas | Manter as 240 representações físicas e associá-las ao respectivo conceito. |
| 6 | Seleção 3D e lista, limitada ao grupo aberto | Preservar navegação por filhos diretos; pesquisa e novo painel fazem parte da próxima etapa de UI. |
| 7 | Ficha própria por grupo e Oráculo de informações reais; também marcou primeira peça | A apresentação da primeira peça precisa de esclarecimento. Proposta: atalho identificado, sem substituir a ficha do grupo. |
| 8 | Seleção destacada; demais peças translúcidas; isolamento explícito | Implementar após definir o comportamento visual, com restauração do estado completo. |
| 9 | Cor por estado e modo de estudo com legenda | Legenda em mini UI perto do punho ou controle, fora do painel principal. |
| 10 | Código, nome científico e português no painel principal | Explicação didática em português num pop-up separado junto ao punho. |
| 11 | Fontes guardadas e acessíveis por ação | Mostrar somente quando o aluno clicar em Exibir fonte. |
| 12 | Criar e aplicar vínculos, depois documentar | Mapeamento técnico autorizado agora. A revisão científica do conteúdo continua identificada separadamente. |

## Estrutura proposta para o Oráculo

Uma peça selecionada aponta para sua representação e seu conceito. Um grupo aponta para sua própria ficha e sua coleção de membros. O sistema não deve escolher uma descrição de descendente como se descrevesse o ancestral.

| Entidade | Identidade | Conteúdo |
|---|---|---|
| Representação | GUID do asset, File ID, Representation ID e Concept ID do OBJ; GlobalObjectId da instância | Metadados de origem, vínculo direto ao grupo, nome de exibição e pendências específicas. |
| Conceito | FMA e edição de origem | Nome normalizado e informação compartilhada pelas representações daquele conceito. |
| Grupo | Identificador próprio do nó da cena | Descrição do grupo, subgrupos e associação às representações. Não atribuir um FMA ao grupo por semelhança textual. |
| Informação | Entidade alvo e tipo de informação | Texto em português, fonte, seção/página, revisão e versão editorial. |
| Fonte | Referência bibliográfica | Autor, título, edição, URL/DOI e localizador da afirmação. |

O catálogo deve preservar a versão `brain5.0` e a relação `FMA 3.0 is_a` ou `part_of` encontrada no arquivo, mesmo que a documentação atual do site descreva outra versão. O BodyParts3D distingue conceito FMA, arquivo geométrico e representação BP; a documentação também descreve representações compostas. Fonte: https://lifesciencedb.jp/bp3d/info_en/index.html (seções Data types, Naming of data versions e Terminology).

A documentação do fornecedor reconhece lacunas e possíveis erros na geometria e atribuição de conceitos. Registrar a identidade original é uma etapa de rastreabilidade; a validação anatômica continua sendo uma atividade separada. A análise atual não reclassifica as peças nem atesta que cada classificação histórica esteja correta.

## Conteúdo científico e participação da IA

Proposta para discussão: o aplicativo consulta conteúdo local versionado. Uma IA pode auxiliar a localizar fontes, preparar paráfrases e apontar inconsistências durante a autoria. O texto exibido ao aluno deve vir da edição publicada do Oráculo. Cada alteração editorial deve indicar o texto anterior, a fonte e quem revisou.

O simples uso de busca ou RAG não comprova a correção de uma resposta gerada. Para esta aplicação, a publicação de textos estáticos revisáveis permite que o orientador confira exatamente o que o aluno verá.

Etapas editoriais propostas: Pendente → Rascunho com fonte → Revisado → Aprovado. Os 240 componentes atualmente marcados como Classified não equivalem a 240 fichas aprovadas cientificamente. A planilha inicial não converte esse estado legado em aprovação editorial.

Campos didáticos propostos: nome em português, termo científico, código, localização, relações anatômicas, funções principais e resumo curto para o punho. Conexões detalhadas e correlações clínicas dependem da definição de público e profundidade. Cada campo factual pode precisar de uma fonte diferente; uma única URL genérica por ficha não deve cobrir afirmações que a referência não contém.

Para começar com tabelas simples, a aba Conteudo reserva uma ficha por grupo e por conceito. Quando houver múltiplas fontes ou diferenças de lateralidade, a versão editorial deverá desdobrar os textos por campo/entidade e registrar a respectiva fonte. As 240 instâncias continuam individualmente auditáveis na aba Representacoes.

## Bibliografia inicial candidata

- Identidade dos modelos: BodyParts3D, https://lifesciencedb.jp/bp3d/.
- Descrição da base: Mitsuhashi N. et al. BodyParts3D: 3D structure database for anatomical concepts. Nucleic Acids Research. 2009;37:D782–D785. DOI: 10.1093/nar/gkn613. https://pmc.ncbi.nlm.nih.gov/articles/PMC2686534/.
- Nomenclatura: FIPAT, Terminologia Neuroanatomica, https://libraries.dal.ca/Fipat/tna.html. Confirmar cruzamento de códigos e termos antes de preencher cada registro.
- Critério para traduções: guia da TA2, https://cdn.dal.ca/content/dam/dalhousie/pdf/library/FIPAT/TA2/FIPAT-TA2-Front-Matter.pdf. O nome inglês no OBJ não deve ser rotulado como termo latino. A bibliografia portuguesa ainda será definida.
- Exemplo didático institucional: James Knierim, Neuroscience Online, Cerebellum, seção 3, capítulo 5. https://nba.uth.tmc.edu/neuroscience/s3/chapter05.html.

Essas referências iniciam a bibliografia; não constituem pesquisa concluída de todas as estruturas. A fonte de uma nomenclatura não substitui uma fonte que explique função ou conexões.

## Exemplo de ficha para discutir

Entidade: grupo Cerebelo. Rascunho ilustrativo, fora dos dados publicados da aplicação.

Texto curto possível: “O cerebelo participa do equilíbrio, da postura, da coordenação dos movimentos e da aprendizagem motora. Ele ajuda a ajustar os movimentos para que sejam executados com precisão.”

Fonte da paráfrase: Knierim, Neuroscience Online, capítulo 5, seção 5.1. https://nba.uth.tmc.edu/neuroscience/s3/chapter05.html.

O texto do grupo não implica que cada região ou representação do cerebelo tenha exatamente a mesma função. Uma peça específica receberá sua ficha no nível de precisão sustentado pela literatura.

## Questões editoriais abertas

1. Público principal: alunos iniciantes de graduação, alunos com neuroanatomia prévia ou público geral.
2. Profundidade: localização/relações/função; acrescentar conexões e vias; ou incluir também correlações clínicas.
3. Grupo e primeira peça: ficha própria com atalho identificado, ficha sem atalho ou seleção automática explicitamente visível.
4. Revisor científico e bibliografia em português a adotar.

## Escopo desta entrega

Mapeamento, correção de exibição e catálogo podem ser concluídos antes das definições editoriais acima. Transparência, legenda no punho, novo painel espacial, mãos e limpeza de pacotes permanecem para suas etapas de implementação. A aplicação do mapeamento e seus testes devem ser registrados no relatório técnico desta pasta, com resultado de compilação, cobertura de 240 peças, integridade das referências e limitações de validação no headset.

### Bloqueio encontrado na aplicação

A tentativa de abertura do Unity 6000.3.5f2 encerrou com código 198: `No valid Unity Editor license found. Please activate your license.` O log também informa token de acesso indisponível e ausência de entitlement do Editor. Por isso os scripts não chegaram à compilação e execução pelo Editor nesta tentativa, e os novos vínculos não foram gravados na SampleScene.

O catálogo extraído fora do Editor representa os arquivos já salvos. A planilha documenta a associação existente entre OBJ, metadado e grupo, enquanto a ferramenta de Editor preparada cria os nós e vínculos serializados após a recuperação da licença. Esses dois resultados não devem ser tratados como equivalentes. Nenhum teste em headset dessa mudança foi realizado.
