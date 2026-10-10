# DEV-VrSNC

Projeto Unity de exploração do sistema nervoso central em realidade virtual, com seleção de estruturas, navegação hierárquica, isolamento, expansão, restauração e consulta de fichas anatômicas. O desenvolvimento usa C# e nomes próprios em português.

## Ambiente

- Unity **6000.3.5f2**, conforme `ProjectSettings/ProjectVersion.txt`.
- Android Build Support, Android SDK/NDK e OpenJDK instalados pelo Unity Hub para compilar para Meta Quest.
- Git e Git LFS para obter os modelos e imagens do repositório.
- Acesso à internet na primeira abertura para resolver os pacotes Unity versionados no manifesto.

Os pacotes têm versões explícitas em `Packages/manifest.json`; a resolução do Unity é registrada em `Packages/packages-lock.json`. A execução e a compilação usam o próprio Unity Editor. Node.js é opcional e serve somente ao verificador externo de invariantes da cena.

## Abrir o projeto

1. Clone o repositório e execute `git lfs pull` na pasta clonada.
2. No Unity Hub, adicione a pasta do projeto e abra com Unity 6000.3.5f2.
3. Aguarde a resolução dos pacotes, importação dos ativos e compilação.
4. Abra `Assets/Scenes/SampleScene.unity`.
5. Use Play Mode para os testes no Editor. Para Quest, configure o dispositivo e use Android como plataforma-alvo.

A primeira importação recria `Library/`, `Temp/`, `Logs/` e os arquivos locais do Editor. Essas pastas não são versionadas. Os arquivos `.meta` acompanham os ativos e preservam as referências da cena.

## Compilar para Quest

O menu **Ferramentas > VR SNC > Quest** oferece:

- **01 Compilar e Executar (base)**: gera um APK de desenvolvimento e solicita execução no aparelho conectado.
- **02 Corrigir e Executar**: aplica alterações somente de código sobre o APK-base. Alterações de cena, ativos, pacotes, shaders ou plugins exigem uma nova compilação-base.
- **03 Compilar Versão de Apresentação**: gera o APK sem solicitar instalação automática.

Os APKs ficam em `Builds/Quest/`, fora do controle de versão. A instalação exige um Quest com modo de desenvolvedor e depuração USB autorizada. A assinatura de publicação deve ser configurada localmente; chaves e senhas não fazem parte do repositório.

## Verificações

No Editor, execute **Ferramentas > VR SNC > Executar verificações de preparação do commit**. A suíte verifica atalhos, migração dos dados, restauração, seleção, referências da cena e configuração da interface. O resultado é gravado em `outputs/validacao-commit/editor.json`.

Com Node.js instalado, o verificador externo pode ser executado na raiz:

```text
node Tools/VR/testar-verificador-invariantes.mjs
```

Ele compara a cena com a referência versionada e exige que alterações anatômicas, referência ausente e referência adulterada sejam detectadas.

## Organização

| Pasta | Conteúdo |
| --- | --- |
| `Assets/Scripts/Anatomia` | Catálogo científico, leitura e vínculos anatômicos |
| `Assets/Scripts/Interacao` | Entrada, gestos, seleção e manipulação do modelo |
| `Assets/Scripts/Interface` | Painéis e interface espacial |
| `Assets/Editor` | Configuração, verificações e comandos de compilação |
| `Assets/Resources/AnatomicalOracle.json` | Fichas anatômicas carregadas em execução |
| `Assets/Scenes` | Cena principal e ativos associados |
| `Docs/Anatomy` | Fontes, mapeamento e documentação científica |
| `Tools/VR` | Verificação externa e referência da cena |
| `Packages` | Manifesto e resolução das dependências |
| `ProjectSettings` | Configuração compartilhada do Unity |

## Estado do desenvolvimento

O projeto permanece em desenvolvimento. A aquisição, filtragem e classificação de EEG ainda não estão implementadas. O catálogo contém fichas com situações editoriais distintas e informa a ausência de revisão docente. Fontes e limitações devem acompanhar o conteúdo exibido.

As verificações no Editor não medem rastreamento físico, conforto visual, desempenho, memória ou temperatura no Quest. Esses resultados precisam ser obtidos no aparelho. Outras versões do Unity devem ser avaliadas em uma cópia separada antes de migrar o projeto.

## Ativos e atribuições

O catálogo preserva as fontes e referências dos modelos BodyParts3D e das fichas científicas. Consulte `Docs/Anatomy`, os dados do catálogo e as licenças incluídas nos ativos e amostras de terceiros. Este repositório não altera os termos de uso desses materiais.
