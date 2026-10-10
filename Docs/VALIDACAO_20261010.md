# Validação do projeto — 10/10/2026

## Ambiente e resultado

Validação executada no Unity 6000.3.5f2, em modo batch, com Android como plataforma-alvo. A primeira abertura partiu de uma cópia contendo somente os arquivos do projeto, sem o cache `Library` do ambiente de origem. O Unity resolveu 71 pacotes do registro oficial e dos módulos internos, importou os ativos e compilou o código.

Durante a compilação inicial foi encontrada uma atribuição do parâmetro `dispositivoVR` para ele mesmo em `AncoraEspacialPulso.Configurar`. A configuração agora grava o campo do componente. A suíte inclui uma verificação dessa referência e foi executada novamente após a correção.

Resultado final registrado em **2026-10-10T19:47:04.7912813Z**: dez grupos aprovados, nenhuma falha e encerramento do Unity com código 0.

| Grupo | Resultado |
| --- | --- |
| Atalhos exclusivos e alternativa sem mapeador | Aprovado |
| Referência de câmera configurada na âncora de pulso | Aprovado |
| Migração JSON e limites científicos das 272 fichas | Aprovado |
| Reset global ativo, concluído e retomada sem salto | Aprovado |
| Histerese da seleção por raio | Aprovado |
| Entrada com mão aberta | Aprovado |
| Contexto de raio próximo e distante | Aprovado |
| Agarre com controlador | Aprovado |
| Referências e valores migrados da cena | Aprovado |
| Configuração serializada da interface espacial | Aprovado |

O verificador externo também aprovou seus quatro cenários: cena atual íntegra, alteração anatômica detectada, referência ausente recusada e referência adulterada recusada.

## Integridade dos arquivos

Os 953 arquivos `.meta` existentes foram preservados. A cena e o catálogo científico mantêm seus conteúdos originais:

- `Assets/Scenes/SampleScene.unity`: SHA256 `D68112F84D158EA3F72E395D4D8BCBFE51279227ED64E7D3851405A51609D2B0`.
- `Assets/Resources/AnatomicalOracle.json`: SHA256 `1BFBC9FF59DB7B479BB043654F1EE6758ADB56E5F281986198C27FDCE60567C2`.

Manifesto e arquivo de resolução usam dependências versionadas do registro Unity ou módulos internos, sem caminhos absolutos para pacotes locais. A compilação Android usa um diretório temporário do usuário, e suas alterações de ambiente ficam restritas ao processo da compilação.

## Limites desta validação

Os cenários de interação foram executados no Editor com instrumentação e objetos de teste. Esta validação não inclui um APK executado no Quest, rastreamento físico, conforto visual, memória, temperatura ou desempenho no headset. A versão de Unity validada é 6000.3.5f2.

O desenvolvimento de EEG permanece pendente. A validação técnica não substitui a revisão docente das fichas científicas. Os recibos e logs locais ficam em `outputs/validacao-commit`, excluídos do controle de versão. As instruções para reproduzir as verificações estão no README.
