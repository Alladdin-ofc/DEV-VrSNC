using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRSNC.Interacao
{
    // Nome funcional em português: Painel de Navegação Anatômica
    // Nome técnico do script: PainelNavegacaoAnatomica
    // Função: lista os filhos diretos do foco anatômico e liga botões como voltar, entrar, resetar, isolar e restaurar.
    // Relação com o protótipo: recebe o foco do GerenciadorSelecaoHierarquica e cria botões para navegar no SNC por camadas.
    // Objetos da cena: usado no Painel_Anatomico_VR_SNC e preserva compatibilidade com o Painel_Antigo_SNC.
    /// <summary>
    /// Controla a lista de estruturas navegáveis do painel.
    /// A seleção continua limitada aos filhos diretos do foco atual para preservar a organização anatômica em camadas.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainNavigationPanel")]
    public sealed class PainelNavegacaoAnatomica : MonoBehaviour
    {
        // Gerenciador central que sabe qual nó está em foco e qual nó está selecionado.
        [UnityEngine.Serialization.FormerlySerializedAs("selecaogerenciador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("selectionManager")]
        private GerenciadorSelecaoHierarquica gerenciadorSelecao;
        // Transform que recebe os botões gerados para cada filho direto do foco.
        [UnityEngine.Serialization.FormerlySerializedAs("listaContêiner")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("listContainer")]
        private Transform conteinerLista;
        // Raiz visual da lista, usada para mostrar/ocultar sem destruir o painel.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("listRoot")]
        private GameObject listaRaiz;
        // Prefab opcional de botão; quando ausente, o script cria um botão simples em runtime.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("buttonPrefab")]
        private Button botaoPrefab;
        // Título da lista, exibido como "Estruturas de: ...".
        [UnityEngine.Serialization.FormerlySerializedAs("titulotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("titleText")]
        private Text textoTitulo;
        // Texto do botão que alterna entre mostrar e ocultar a lista.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("toggleListText")]
        private Text alternarListaTexto;
        // Botões utilitários do painel VR.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("backButton")]
        private Button voltarBotao;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("enterButton")]
        private Button entrarBotao;
        [UnityEngine.Serialization.FormerlySerializedAs("redefinirbotao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("resetButton")]
        private Button botaoRestaurar;
        [UnityEngine.Serialization.FormerlySerializedAs("expandirbotao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("explodeButton")]
        private Button botaoExpandir;
        [UnityEngine.Serialization.FormerlySerializedAs("isolarbotao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("isolateButton")]
        private Button botaoIsolar;
        [UnityEngine.Serialization.FormerlySerializedAs("limparselecaobotao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("clearSelectionButton")]
        private Button botaoLimparSelecao;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("toggleListButton")]
        private Button alternarListaBotao;
        // Controladores acionados pelos botões do painel.
        [UnityEngine.Serialization.FormerlySerializedAs("redefinirgerenciador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("resetManager")]
        private GerenciadorRestauracao gerenciadorRestauracao;
        [UnityEngine.Serialization.FormerlySerializedAs("expandidoVisaoControlador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("explodedViewController")]
        private ControladorVisaoExplodida controladorVisaoExplodida;
        // Botões criados em runtime para a lista de filhos; são removidos e recriados a cada Refresh.
        private readonly List<GameObject> botoesGerados = new();
        // Estado visual da lista de filhos.
        private bool listaExpandida = true;
        // Foco para o qual a lista foi montada por último. A rolagem deve sobreviver a
        // atualizações repetidas do mesmo foco e voltar ao topo apenas ao navegar.
        private NoInteracaoAnatomica ultimoRenderizadoFoco;
        /// <summary>
        /// Resolve referências locais e liga os botões utilitários sem recriar a arquitetura do painel.
        /// </summary>
        private void Awake()
        {
            if (gerenciadorSelecao == null)
            {
                gerenciadorSelecao = FindFirstObjectByType<GerenciadorSelecaoHierarquica>();
            }

            ConectarUtilitarioBotoes();
            DefinirRotulosBotoesUtilitarios();
        }

        /// <summary>
        /// Gera a lista somente depois que todos os componentes da cena concluíram Awake.
        /// </summary>
        private void Start()
        {
            Atualizar(gerenciadorSelecao != null ? gerenciadorSelecao.NoEmFoco : null);
        }

        /// <summary>
        /// Configuração compacta usada por versões antigas do painel.
        /// </summary>
        public void Configurar(GerenciadorSelecaoHierarquica selecao, Transform contêiner, Text titulo, GerenciadorRestauracao redefinir, ControladorVisaoExplodida expandido)
        {
            gerenciadorSelecao = selecao;
            conteinerLista = contêiner;
            textoTitulo = titulo;
            gerenciadorRestauracao = redefinir;
            controladorVisaoExplodida = expandido;
            ConectarUtilitarioBotoes();
            DefinirRotulosBotoesUtilitarios();
            Atualizar(gerenciadorSelecao != null ? gerenciadorSelecao.NoEmFoco : null);
        }

        /// <summary>
        /// Configura todas as referências do painel VR atual, incluindo lista, título e botões.
        /// </summary>
        public void ConfigurarVR(GerenciadorSelecaoHierarquica selecao, Transform contêiner, GameObject listaPainelRaiz, Text titulo, Text alternarTexto, Button voltar, Button entrar, Button redefinir, Button expandir, Button isolar, Button limpar, Button alternarLista, GerenciadorRestauracao redefinirControlador, ControladorVisaoExplodida controladorExpansao)
        {
            gerenciadorSelecao = selecao;
            conteinerLista = contêiner;
            listaRaiz = listaPainelRaiz;
            textoTitulo = titulo;
            alternarListaTexto = alternarTexto;
            voltarBotao = voltar;
            entrarBotao = entrar;
            botaoRestaurar = redefinir;
            botaoExpandir = expandir;
            botaoIsolar = isolar;
            botaoLimparSelecao = limpar;
            alternarListaBotao = alternarLista;
            gerenciadorRestauracao = redefinirControlador;
            controladorVisaoExplodida = controladorExpansao;
            ConectarUtilitarioBotoes();
            DefinirRotulosBotoesUtilitarios();
            DefinirListaExpandida(listaExpandida);
            Atualizar(gerenciadorSelecao != null ? gerenciadorSelecao.NoEmFoco : null);
        }

        /// <summary>
        /// Atualiza a lista de botões para os filhos diretos do foco atual.
        /// </summary>
        public void Atualizar(NoInteracaoAnatomica focoNo)
        {
            var focoAlterado = ultimoRenderizadoFoco != focoNo;
            LimparGeradoBotoes();
            if (textoTitulo != null)
            {
                textoTitulo.text = $"Estruturas de: {ResolverNoTituloNavegacao(focoNo)}";
            }

            // Configure/ConfigureVR também são chamados pela ferramenta de editor. Os botões
            // navegáveis pertencem ao runtime e não devem ser serializados sem listeners válidos.
            if (!Application.isPlaying || focoNo == null || conteinerLista == null)
            {
                if (focoAlterado)
                {
                    RedefinirRolagemNavegacao();
                }

                ultimoRenderizadoFoco = focoNo;
                AtualizarEstadosControles();
                return;
            }

            foreach (var filho in focoNo.NosFilhos)
            {
                if (filho == null || !filho.EhSelecionavel)
                {
                    continue;
                }

                var botao = CriarBotao(filho.NomeExibicao);
                // Cada botão seleciona exatamente o filho direto mostrado, preservando a seleção por camada.
                botao.onClick.AddListener(() => gerenciadorSelecao?.SelecionarNo(filho));
                botoesGerados.Add(botao.gameObject);
            }

            if (focoAlterado)
            {
                RedefinirRolagemNavegacao();
            }

            ultimoRenderizadoFoco = focoNo;
            AtualizarEstadosControles();
        }

        /// <summary>
        /// Deixa a primeira estrutura visível depois de uma troca real de foco, sem
        /// perturbar a leitura quando o mesmo foco é atualizado repetidamente.
        /// </summary>
        private void RedefinirRolagemNavegacao()
        {
            var rolagem = listaRaiz != null ? listaRaiz.GetComponent<ScrollRect>() : null;
            rolagem ??= listaRaiz != null ? listaRaiz.GetComponentInParent<ScrollRect>() : null;
            rolagem ??= conteinerLista != null ? conteinerLista.GetComponentInParent<ScrollRect>() : null;
            if (rolagem == null || rolagem.content == null)
            {
                return;
            }

            // Os botões acabaram de ser reconstruídos para o novo foco. Reconstrói apenas esta
            // subárvore de rolagem antes de normalizar; caso contrário, ScrollRect lê limites obsoletos e pode
            // reter uma posição fora da viewport depois que o grupo de layout se atualiza.
            if (rolagem.viewport != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rolagem.viewport);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(rolagem.content);
            rolagem.StopMovement();
            rolagem.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// Alterna a lista de estruturas sem alterar a seleção atual.
        /// </summary>
        public void AlternarLista()
        {
            DefinirListaExpandida(!listaExpandida);
        }

        /// <summary>
        /// Mostra ou oculta apenas a lista, mantendo painel e botões principais ativos.
        /// </summary>
        public void DefinirListaExpandida(bool expandida)
        {
            listaExpandida = expandida;
            if (listaRaiz != null)
            {
                listaRaiz.SetActive(listaExpandida);
            }

            if (alternarListaTexto != null)
            {
                alternarListaTexto.text = listaExpandida ? "Ocultar lista" : "Mostrar lista";
            }

            AtualizarEstadosControles();
        }

        /// <summary>
        /// Cria um botão visual para uma estrutura anatômica da lista.
        /// </summary>
        private Button CriarBotao(string rotulo)
        {
            var botao = botaoPrefab != null ? Instantiate(botaoPrefab, conteinerLista) : CriarBotaoAlternativo(conteinerLista);
            if (botaoPrefab != null)
                AplicarContratoRaioInterface(botao);
            var texto = botao.GetComponentInChildren<Text>();
            if (texto != null)
            {
                texto.text = rotulo;
            }

            botao.gameObject.SetActive(true);
            return botao;
        }

        /// <summary>
        /// Cria um botão simples quando não há prefab definido na cena.
        /// </summary>
        private static Button CriarBotaoAlternativo(Transform pai)
        {
            var botaoObjeto = new GameObject("Navigation_Button");
            botaoObjeto.transform.SetParent(pai, false);
            var retangulo = botaoObjeto.AddComponent<RectTransform>();
            retangulo.sizeDelta = new Vector2(260f, 44f);
            var organizacao = botaoObjeto.AddComponent<LayoutElement>();
            organizacao.minHeight = 44f;
            organizacao.preferredHeight = 44f;
            var imagem = botaoObjeto.AddComponent<Image>();
            imagem.color = new Color(0.96f, 0.97f, 0.98f, 1f);
            imagem.raycastTarget = true;
            var botao = botaoObjeto.AddComponent<Button>();
            botao.targetGraphic = imagem;
            var cores = botao.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = new Color(0.82f, 0.86f, 0.90f, 1f);
            cores.pressedColor = new Color(0.66f, 0.72f, 0.79f, 1f);
            botao.colors = cores;
            var textoObjeto = new GameObject("Text");
            textoObjeto.transform.SetParent(botaoObjeto.transform, false);
            var textoRetangulo = textoObjeto.AddComponent<RectTransform>();
            textoRetangulo.anchorMin = Vector2.zero;
            textoRetangulo.anchorMax = Vector2.one;
            textoRetangulo.offsetMin = new Vector2(10f, 2f);
            textoRetangulo.offsetMax = new Vector2(-10f, -2f);
            var texto = textoObjeto.AddComponent<Text>();
            texto.color = Color.black;
            texto.raycastTarget = false;
            texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.alignment = TextAnchor.MiddleLeft;
            texto.resizeTextForBestFit = false;
            texto.fontSize = 18;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Truncate;
            AplicarContratoRaioInterface(botao);
            return botao;
        }

        /// <summary>
        /// Mantém botões de navegação gerados e derivados de prefab na camada de UI do Canvas
        /// e garante que somente a Image-alvo receba o raio de UI XR.
        /// </summary>
        private static void AplicarContratoRaioInterface(Button botao)
        {
            if (botao == null)
                return;
            DefinirCamadaInterface(botao.transform, 5);
            var imagem = botao.targetGraphic as Image ?? botao.GetComponent<Image>() ?? botao.GetComponentInChildren<Image>(true);
            if (imagem == null)
                imagem = botao.gameObject.AddComponent<Image>();
            botao.targetGraphic = imagem;
            foreach (var elementoVisual in botao.GetComponentsInChildren<Graphic>(true))
                elementoVisual.raycastTarget = elementoVisual == imagem;
            imagem.raycastTarget = true;
        }

        private static void DefinirCamadaInterface(Transform raiz, int camada)
        {
            raiz.gameObject.layer = camada;
            for (var indice = 0; indice < raiz.childCount; indice++)
                DefinirCamadaInterface(raiz.GetChild(indice), camada);
        }

        /// <summary>
        /// Liga os botões do painel aos controladores existentes sem remover componentes.
        /// </summary>
        private void ConectarUtilitarioBotoes()
        {
            if (voltarBotao != null)
            {
                voltarBotao.onClick.RemoveAllListeners();
                voltarBotao.onClick.AddListener(() => gerenciadorSelecao?.VoltarAoNoPai());
            }

            if (entrarBotao != null)
            {
                entrarBotao.onClick.RemoveAllListeners();
                entrarBotao.onClick.AddListener(() => gerenciadorSelecao?.EntrarNoSelecionado());
            }

            if (botaoRestaurar != null)
            {
                botaoRestaurar.onClick.RemoveAllListeners();
                botaoRestaurar.onClick.AddListener(() => gerenciadorRestauracao?.RestaurarTudo());
            }

            if (botaoExpandir != null)
            {
                botaoExpandir.onClick.RemoveListener(AlternarVisaoExplodidaPeloPainel);
                botaoExpandir.onClick.AddListener(AlternarVisaoExplodidaPeloPainel);
            }

            if (botaoIsolar != null)
            {
                botaoIsolar.onClick.RemoveListener(AlternarIsolamentoDePainel);
                botaoIsolar.onClick.AddListener(AlternarIsolamentoDePainel);
            }

            if (botaoLimparSelecao != null)
            {
                botaoLimparSelecao.onClick.RemoveListener(LimparSelecaoDePainel);
                botaoLimparSelecao.onClick.AddListener(LimparSelecaoDePainel);
            }

            if (alternarListaBotao != null)
            {
                alternarListaBotao.onClick.RemoveAllListeners();
                alternarListaBotao.onClick.AddListener(AlternarLista);
            }
        }

        /// <summary>
        /// Remove os botões gerados anteriormente antes de reconstruir a lista do foco atual.
        /// </summary>
        private void LimparGeradoBotoes()
        {
            if (conteinerLista != null)
            {
                for (var indice = conteinerLista.childCount - 1; indice >= 0; indice--)
                {
                    var filho = conteinerLista.GetChild(indice);
                    if (filho == null)
                    {
                        continue;
                    }

                    var item = filho.gameObject;
                    if (botaoPrefab != null && item == botaoPrefab.gameObject)
                    {
                        continue;
                    }

                    // Content é dedicado aos itens de navegação. Filhos decorativos que não
                    // sejam Button são preservados.
                    if (item.GetComponent<Button>() == null)
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        // Destroy só conclui no fim do frame. Desativar e retirar do container
                        // impede duplicação visual/layout mesmo com dois Refresh no mesmo frame.
                        item.SetActive(false);
                        filho.SetParent(null, false);
                        Destroy(item);
                    }
                    else
                    {
                        DestroyImmediate(item);
                    }
                }
            }

            botoesGerados.Clear();
        }

        /// <summary>
        /// Alterna a vista explodida e atualiza imediatamente rótulo e disponibilidade dos controles.
        /// </summary>
        private void AlternarVisaoExplodidaPeloPainel()
        {
            controladorVisaoExplodida?.AlternarExpansaoNivelAtual();
            AtualizarEstadosControles();
        }

        /// <summary>
        /// Limpa a seleção e reflete a mudança imediatamente na disponibilidade dos controles.
        /// </summary>
        private void LimparSelecaoDePainel()
        {
            gerenciadorSelecao?.LimparSelecao();
            AtualizarEstadosControles();
        }

        /// <summary>
        /// Alterna o isolamento e atualiza imediatamente o estado e o rótulo do botão.
        /// </summary>
        private void AlternarIsolamentoDePainel()
        {
            gerenciadorSelecao?.AlternarIsolamento();
            AtualizarEstadosControles();
        }

        /// <summary>
        /// Mantém a disponibilidade dos botões coerente com foco, seleção e vista explodida.
        /// </summary>
        public void AtualizarEstadosControles()
        {
            var foco = gerenciadorSelecao != null ? gerenciadorSelecao.NoEmFoco : null;
            var selecionado = gerenciadorSelecao != null ? gerenciadorSelecao.NoSelecionado : null;
            var ehIsolando = gerenciadorSelecao != null && gerenciadorSelecao.EhIsolando;
            if (voltarBotao != null)
            {
                voltarBotao.interactable = foco != null && foco.NoPai != null;
            }

            if (entrarBotao != null)
            {
                entrarBotao.interactable = selecionado != null && selecionado.NosFilhos.Count > 0;
            }

            if (botaoIsolar != null)
            {
                botaoIsolar.interactable = selecionado != null || ehIsolando;
                DefinirTextoBotao(botaoIsolar, ehIsolando ? "Restaurar" : "Isolar");
            }

            if (botaoLimparSelecao != null)
            {
                botaoLimparSelecao.interactable = selecionado != null;
            }

            if (botaoExpandir != null)
            {
                botaoExpandir.interactable = foco != null && foco.NosFilhos.Count > 0;
                DefinirTextoBotao(botaoExpandir, controladorVisaoExplodida != null && controladorVisaoExplodida.EstaExpandido ? "Restaurar" : "Explodir");
            }
        }

        /// <summary>
        /// Escolhe o nome do título da lista conforme a regra do Navigation Panel.
        /// A lista mostra filhos diretos do foco atual; por isso o título deve acompanhar o foco, não a seleção.
        /// </summary>
        private string ResolverNoTituloNavegacao(NoInteracaoAnatomica focoNo)
        {
            var tituloNo = focoNo != null ? focoNo : gerenciadorSelecao != null ? gerenciadorSelecao.NoEmFoco : null;
            return tituloNo != null ? tituloNo.NomeExibicao : "Sistema Nervoso Central";
        }

        /// <summary>
        /// Garante rótulos em português nos botões principais do painel.
        /// </summary>
        private void DefinirRotulosBotoesUtilitarios()
        {
            DefinirTextoBotao(voltarBotao, "Voltar");
            DefinirTextoBotao(entrarBotao, "Entrar");
            DefinirTextoBotao(botaoRestaurar, "Resetar SNC");
            DefinirTextoBotao(botaoExpandir, controladorVisaoExplodida != null && controladorVisaoExplodida.EstaExpandido ? "Restaurar" : "Explodir");
            DefinirTextoBotao(botaoIsolar, gerenciadorSelecao != null && gerenciadorSelecao.EhIsolando ? "Restaurar" : "Isolar");
            DefinirTextoBotao(botaoLimparSelecao, "Limpar seleção");
            DefinirTextoBotao(alternarListaBotao, listaExpandida ? "Ocultar lista" : "Mostrar lista");
        }

        /// <summary>
        /// Atualiza o texto de um botão quando ele possui um componente Text em seus filhos.
        /// </summary>
        private static void DefinirTextoBotao(Button botao, string rotulo)
        {
            if (botao == null)
            {
                return;
            }

            var texto = botao.GetComponentInChildren<Text>(true);
            if (texto != null)
            {
                texto.text = rotulo;
            }
        }
    }
}