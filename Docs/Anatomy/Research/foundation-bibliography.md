# Fundação científica do Oráculo anatômico

Pesquisa e verificação: 2026-09-07. Este documento registra a proveniência dos modelos e as fontes usadas no conjunto `foundation.json`. Ele não altera a cena, os OBJ ou a hierarquia Unity.

## O que o BodyParts3D realmente usa

Não há um único “atlas do BodyParts3D”. O artigo fundador informa que o modelo foi construído sobre o fantoma voxel **TARO**, derivado de ressonâncias magnéticas de corpo inteiro de um voluntário adulto do sexo masculino; detalhes e contornos foram depois modificados por ilustradores com consulta a livros, atlas e modelos anatômicos. O artigo também ressalta que o recurso representa conceitos FMA por segmentos tridimensionais, combinando geometria e morfologia com uma ontologia simbólica complementar.

- Mitsuhashi N, Fujieda K, Tamura T, Kawamoto S, Takagi T, Okubo K. *BodyParts3D: 3D structure database for anatomical concepts*. Nucleic Acids Research. 2009;37:D782-D785. [Artigo e DOI](https://academic.oup.com/nar/article/37/suppl_1/D782/1000752). Evidência: resumo; “Construction process”; “Data access and availability”.
- O [portal de informação do BodyParts3D](https://lifesciencedb.jp/bp3d/info_en/index.html) distingue conceito FMA, arquivo de contorno FJ e representação BP, e adverte que peças podem ter sido desenhadas ou deformadas para ajuste, que há conceitos ainda não representados e que a segmentação fina pode tornar a atribuição conceitual ambígua. Evidência: “Notice”, “Data types” e “Terminology”.
- A [nota de versão 3.0](https://dbarchive.biosciencedbc.jp/data/bodyparts3d/20110915/release_3.0_e.html) é a evidência mais direta para o encéfalo desta família de dados. Ela afirma que o SNC foi refinado com *The Human Central Nervous System*, 4ª ed. (Nieuwenhuys, Voogd e van Huijzen, 2008), *Atlas of the Human Brain*, 3ª ed. (Mai, Paxinos e Voss, 2007), *Gray's Anatomy*, 40ª ed. (Standring, 2008) e *Prometheus Kopf, Hals und Neuroanatomie* (Schünke, Schulte e Schumacher, 2009), usando também SPL-PNL Brain Atlas 2008 e Talairach Label Data como atlas de referência.
- A [nota de modelagem 2.0](https://lifesciencedb.jp/bp3d/info_en/userGuide/releaseNotes/modelingNote-2.0.pdf) documenta, para cabeça e encéfalo, digitalização do modelo cerebral transparente SBS25ST e do modelo cerebral SBS23; para alinhamento e limites menciona, entre outras, figuras de **Netter** e páginas de **Grant**, além de *Gray's Anatomy*, *Gray's Anatomy for Students*, Moore, Terminologia Anatomica japonesa, Matsumura, Rauber-Kopsch, Rohen, Snell e Kahle. Evidência: seções 3.1-3.2 e bibliografia, páginas 8-10 e 38-40 do PDF.
- A própria página de informação ainda lista, como gabaritos ou referências de imagem, TARO, Stanford, MAI e Talairach; como livros muito usados, FENEIS, o site de Funato e Rauber-Kopsch; e, como dados 3D de referência, Visible Human, BodyBrowser e Lung CD. Esses itens descrevem o histórico geral do recurso e **não provam que cada malha cerebral desta versão derive de uma única dessas referências**.

## FMA `is_a` e `part_of` não são um atlas

FMA é uma ontologia de referência da anatomia canônica, não uma imagem ou uma parcellação geométrica. `is_a` classifica um conceito como subtipo de outro; `part_of` expressa composição espacial/anatômica. No BodyParts3D essas duas árvores são escolhas de **lógica de montagem**: arquivos elementares podem ser agregados para gerar representações compostas de conceitos superiores. Assim, trocar `is_a` por `part_of` pode mudar o conjunto de OBJ agregado, mas não converte a árvore em um atlas nem garante completude morfológica.

- O [portal BodyParts3D](https://lifesciencedb.jp/bp3d/info_en/index.html) declara que os contornos elementares são mapeados ao conceito FMA correspondente ou mais próximo, que agregações reutilizam relações FMA e que uma mesma entidade pode ter compostos diferentes segundo a lógica escolhida. A densidade do composto mede apenas quantos subordinados representados existem.
- Rosse C, Mejino JLV. *A reference ontology for biomedical informatics: the Foundational Model of Anatomy*. Journal of Biomedical Informatics. 2003;36:478-500. [PubMed](https://pubmed.ncbi.nlm.nih.gov/14759820/). Sustenta o papel da FMA como ontologia estrutural canônica.
- Mejino JLV Jr, Agoncillo AV, Rickard KL, Rosse C. *Representing complexity in part-whole relationships within the Foundational Model of Anatomy*. AMIA Annual Symposium Proceedings. 2003:450-454. [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC1480337/). Distingue partições constitucionais e regionais e mostra que relações parte-todo têm semântica própria.

Consequência editorial: `objBuildUpLogic` em `anatomical-mapping.json` registra como o BodyParts3D montou aquela representação. Não deve ser transformado em afirmação funcional. Os textos científicos abaixo usam o FMA/BodyParts3D para identidade e fontes neuroanatômicas independentes para localização e papel.

## Nomenclatura

- FIPAT. *Terminologia Neuroanatomica: International Neuroanatomical Terminology*. 2017, adotada pela IFAA em 2019. [Front matter oficial](https://fipat.library.dal.ca/wp-content/uploads/2017/02/FIPAT-TNA-Front-Matter.pdf) e [documentação](https://ifaa.unifr.ch/Public/TNAEntryPage/pdf/TNADocumentation.pdf). A fonte explicita que os termos oficiais são latinos; equivalentes ingleses acompanham a terminologia. `latinName` só é preenchido quando conferido, evitando latinizações inventadas.
- A identidade de cada arquivo permanece a combinação `assetGuid | FJ | BP | FMA` do `anatomical-mapping.json`. Nomes ingleses das estruturas vêm do cabeçalho OBJ catalogado. A tradução portuguesa é editorial e não substitui os identificadores.

## Neuroanatomia usada nos textos

- Purves D, Augustine GJ, Fitzpatrick D, et al., eds. *Neuroscience*, 2nd ed. 2001. [The Ventricular System, NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK11083/). Localização e continuidade dos ventrículos, aqueduto e plexos coroides.
- UTHealth / McGovern Medical School. *Neuroanatomy Online*. [Fórnix, corpo caloso e comissura anterior](https://nba.uth.tmc.edu/neuroanatomy/L10/Lab10p18_index.html); [sistema límbico, fórnix e estria terminal](https://nba.uth.tmc.edu/neuroanatomy/L11/Lab11p09_index.html); [cápsula interna](https://nba.uth.tmc.edu/neuroanatomy/L10/Lab10p01_index.html). Localização e conexões de substância branca.
- Young CB, Reddy V, Sonne J. *Neuroanatomy, Basal Ganglia*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK537141/). Organização de caudado, putâmen, globo pálido e circuitos motores/associativos.
- Jimsheleishvili S, Dididze M. *Neuroanatomy, Cerebellum*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK538167/). Localização, conexões e participação na coordenação, equilíbrio e aprendizagem motora.
- Emos MC, Khan Suheb MZ, Agarwal S. *Neuroanatomy, Internal Capsule*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK542181/). Limites e feixes ascendentes/descendentes da cápsula interna.
- Driscoll ME, Tadi P. *Neuroanatomy, Inferior Colliculus*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK554468/); Zubricky RD, Das JM. *Neuroanatomy, Superior Colliculus*. atualização 2024. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK544224/). Vias auditivas e orientação visuomotora.
- Maldonado KA, Alsayouri K. *Physiology, Brain*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK551718/); Rahman M, Tadi P. *Neuroanatomy, Pons*. atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK560589/); *Neuroanatomy, Brainstem*. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/sites/books/NBK544297/). Organização do encéfalo e do tronco encefálico.
- *Neuroanatomy, Mesencephalon Midbrain*. StatPearls. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK551509/). Aqueduto, pedúnculos/crura, núcleos rubros, substância negra e teto mesencefálico.
- Goldstein A, Covington BP, Mahabadi N, Mesfin FB. *Neuroanatomy, Corpus Callosum*. StatPearls, atualização 2023. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/sites/books/NBK448209/).
- Fuller GN. *Neuroanatomy Online*, University of Texas Health Science Center. [Nervo/trato e estrias olfatórias](https://nba.uth.tmc.edu/neuroanatomy/L9/Lab09p01_index.html); complemento: *Neuroanatomy, Cranial Nerve 1 (Olfactory)*. [NCBI Bookshelf](https://www.ncbi.nlm.nih.gov/books/NBK556051/).
- NCBI MeSH. [Diagonal Band of Broca](https://www.ncbi.nlm.nih.gov/mesh/68020667); Geula C, Ayala I, Gefen T, Mesulam MM. *Organization of the basal forebrain cholinergic system in the human brain*. [PMC](https://pmc.ncbi.nlm.nih.gov/articles/PMC13375150/). Localização e projeções dos grupos colinérgicos septo-banda diagonal.

## Limites e discrepâncias do acervo

- O complemento dos quatro ramos reservados contém **63 representações**, além dos 32 grupos. O conjunto usa as chaves reais do arquivo atual, sem omitir OBJ.
- As duas malhas “brachium of superior colliculus” estão com a lateralidade invertida no nome de cena. O registro usa como canônicos o cabeçalho OBJ e o FMA: FJ3793/FMA73461 é direito; FJ1735/FMA73462 é esquerdo. Nenhuma malha foi trocada ou renomeada.
- “rigth” em nomes de cena é tratado apenas como erro ortográfico de exibição. A identidade FJ/BP/FMA permanece intacta.
- Para duplicatas cujo cabeçalho OBJ é não lateralizado (por exemplo, pares de cerebelo, mesencéfalo, ponte e medula oblonga), o `nameEn` permanece igual ao cabeçalho. A lateralidade aparente do nome de cena é mantida apenas em `reviewNotes`, porque o identificador FMA compartilhado não a confirma.
- Textos de fragmentos e feixes dizem “participa”, “conduz” ou “integra” conforme a literatura. Não atribuem a uma malha isolada a função integral do grupo pai.
