# Convenções do código próprio

O código do projeto usa nomes em português. APIs de Unity, XRI e .NET e arquivos de terceiros conservam seus nomes originais.

- Arquivo e classe de componente têm o mesmo nome.
- Classes, métodos e propriedades usam PascalCase; campos, parâmetros e variáveis usam camelCase.
- Identificadores novos evitam acentos e indicam a ação ou condição: `RestaurarTudo`, `TentarObterPosicaoFisicaPinca`, `EstaExpandido`.
- Quantidades importantes indicam sua unidade: `frequenciaAmostragemHz`, `valorMicrovolts`.
- Ferramentas do Editor ficam em `Assets/Editor`; código de execução fica em `Assets/Scripts`.

`FormerlySerializedAs` conserva os nomes históricos de campos serializados, e `MovedFrom` conserva informações de migração das classes. Esses atributos e os GUIDs dos arquivos `.meta` devem ser preservados ao renomear código que já aparece em cenas e prefabs.

Os arquivos científicos mantêm suas chaves, IDs, textos e bibliografia originais. `LeitorJsonCompativel` traduz as chaves conhecidas durante a leitura; uma chamada direta a `JsonUtility.FromJson` não aplica essa migração. O dicionário `NOMES_CODIGO_PTBR.txt` registra traduções e exceções e não deve ser usado como substituição global nos ativos ou pacotes.

Alterações de dados e cena devem passar pelas verificações do Editor e pelo verificador externo de invariantes. Testes sintéticos precisam ser identificados como tal; resultados físicos de interação e desempenho exigem execução no headset.
