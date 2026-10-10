using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRSNC.Anatomia;

namespace VRSNC.Interacao
{
    // Nome funcional em português: Painel de Informações Anatômicas
    // Nome técnico do script: PainelInformacoesAnatomicas
    // Função: atualiza os textos visíveis do painel com foco, seleção, caminho anatômico e metadados do SNC.
    // Relação com o protótipo: recebe dados do GerenciadorSelecaoHierarquica e mostra ao usuário o que está em foco
    // ou selecionado durante a navegação anatômica por raycast/trigger.
    // Objetos da cena: usado no Painel_Anatomico_VR_SNC e, quando presente, no Painel_Antigo_SNC.
    /// <summary>
    /// Controla os textos e a expansão/recolhimento do painel de anatomia em VR.
    /// A classe só altera conteúdo visual; a seleção anatômica continua sendo controlada pelo gerenciador de seleção.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainInfoPanel")]
    public sealed class PainelInformacoesAnatomicas : MonoBehaviour
    {
        [UnityEngine.Serialization.FormerlySerializedAs("painelraiz")]
        [Header("Painel")]
        // Raiz geral do painel. Em painel world-space, é o GameObject que contém Canvas e GraphicRaycaster.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("panelRoot")]
        private GameObject raizPainel;
        // Conteúdo completo do painel quando ele está aberto.
        [UnityEngine.Serialization.FormerlySerializedAs("expandidoRaiz")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("expandedRoot")]
        private GameObject raizExpandida;
        // Aba compacta usada para reabrir o painel quando ele está oculto.
        [UnityEngine.Serialization.FormerlySerializedAs("recolhidaAbaRaiz")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("collapsedTabRoot")]
        private GameObject raizAbaRecolhida;
        // Título principal exibido no cabeçalho.
        [UnityEngine.Serialization.FormerlySerializedAs("padraotitulo")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("defaultTitle")]
        private string tituloPadrao = "VR SNC";
        // Mensagem padrão quando nenhuma estrutura anatômica foi selecionada.
        [UnityEngine.Serialization.FormerlySerializedAs("padraocorpo")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("defaultBody")]
        private string corpoPadrao = "Nenhuma estrutura selecionada";
        [UnityEngine.Serialization.FormerlySerializedAs("titulotexto")]
        [Header("Texto")]
        // Campos TMP opcionais mantidos para compatibilidade com variações do painel.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("titleText")]
        private TMP_Text textoTitulo;
        [UnityEngine.Serialization.FormerlySerializedAs("corpotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("bodyText")]
        private TMP_Text textoCorpo;
        [UnityEngine.Serialization.FormerlySerializedAs("estadotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("statusText")]
        private TMP_Text textoEstado;
        [UnityEngine.Serialization.FormerlySerializedAs("caminhotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("pathText")]
        private TMP_Text textoCaminho;
        [UnityEngine.Serialization.FormerlySerializedAs("nonometexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("nodeNameText")]
        private TMP_Text textoNomeNo;
        [UnityEngine.Serialization.FormerlySerializedAs("exibicaonometexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("displayNameText")]
        private TMP_Text textoNomeExibicao;
        [UnityEngine.Serialization.FormerlySerializedAs("descricaotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("descriptionText")]
        private TMP_Text textoDescricao;
        [UnityEngine.Serialization.FormerlySerializedAs("divisaotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("divisionText")]
        private TMP_Text textoDivisao;
        [UnityEngine.Serialization.FormerlySerializedAs("niveltexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("levelText")]
        private TMP_Text textoNivel;
        // Campos UnityEngine.UI.Text usados pelo painel atual da cena.
        [UnityEngine.Serialization.FormerlySerializedAs("alternativotitulotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackTitleText")]
        private Text textoTituloAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativocorpotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackBodyText")]
        private Text textoCorpoAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativoestadotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackStatusText")]
        private Text textoEstadoAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativocaminhotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackPathText")]
        private Text textoCaminhoAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativononometexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackNodeNameText")]
        private Text textoNomeNoAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativoexibicaonometexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackDisplayNameText")]
        private Text textoNomeExibicaoAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativodescricaotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackDescriptionText")]
        private Text textoDescricaoAlternativa;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativodivisaotexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackDivisionText")]
        private Text textoDivisaoAlternativa;
        [UnityEngine.Serialization.FormerlySerializedAs("alternativoniveltexto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("fallbackLevelText")]
        private Text textoNivelAlternativo;
        [UnityEngine.Serialization.FormerlySerializedAs("alternarpainelbotao")]
        [Header("Controles")]
        // Botão que oculta o painel expandido. A aba recolhida possui seu próprio botão na cena.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("togglePanelButton")]
        private Button botaoAlternarPainel;
        // Botão da aba compacta que reabre o painel.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("expandPanelButton")]
        private Button expandirPainelBotao;
        [UnityEngine.Serialization.FormerlySerializedAs("complementarTitulo")]
        [Header("Dados Futuros")]
        // Espaço reservado para conteúdo acadêmico complementar sem mudar a lógica de seleção.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("supplementaryTitle")]
        private string tituloComplementar;
        [UnityEngine.Serialization.FormerlySerializedAs("complementarCorpo")]
        [TextArea(2, 6)]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("supplementaryBody")]
        private string corpoComplementar;
        // Estado visual atual do painel: aberto ou recolhido.
        private bool painelVisivel = true;
        // Identidade já apresentada. Evita devolver a descrição ao topo quando o mesmo
        // nó é apenas redesenhado por uma atualização de foco ou de controles.
        private NoInteracaoAnatomica ultimoNoExibido;
        /// <summary>
        /// Liga o botão do painel e inicializa os textos em português.
        /// </summary>
        private void Awake()
        {
            ResolverReferenciasControles();
            painelVisivel = raizExpandida == null || raizExpandida.activeSelf;
            ConectarBotoes();
            DefinirVisibilidadePainel(painelVisivel);
            Limpar();
        }

        /// <summary>
        /// Configuração simples para versões antigas do painel que usam apenas título e corpo.
        /// </summary>
        public void Configurar(TMP_Text titulo, TMP_Text corpo, Text tituloAlternativo, Text corpoAlternativo)
        {
            textoTitulo = titulo;
            textoCorpo = corpo;
            textoTituloAlternativo = tituloAlternativo;
            textoCorpoAlternativo = corpoAlternativo;
            Limpar();
        }

        /// <summary>
        /// Recebe todas as referências do painel VR criado na cena e prepara os textos iniciais.
        /// </summary>
        public void ConfigurarPainelVR(GameObject raiz, GameObject expandida, GameObject recolhida, Text titulo, Text estado, Text caminho, Text nomeNo, Text nomeExibicao, Text descricao, Text divisao, Text nivel, Button alternarBotao, Button expandirBotao)
        {
            raizPainel = raiz;
            raizExpandida = expandida;
            raizAbaRecolhida = recolhida;
            textoTituloAlternativo = titulo;
            textoEstadoAlternativo = estado;
            textoCaminhoAlternativo = caminho;
            textoNomeNoAlternativo = nomeNo;
            textoNomeExibicaoAlternativo = nomeExibicao;
            textoDescricaoAlternativa = descricao;
            textoDivisaoAlternativa = divisao;
            textoNivelAlternativo = nivel;
            botaoAlternarPainel = alternarBotao;
            expandirPainelBotao = expandirBotao;
            ResolverReferenciasControles();
            ConectarBotoes();
            DefinirVisibilidadePainel(true);
            Limpar();
        }

        /// <summary>
        /// Mostra uma estrutura selecionada, com caminho anatômico, nível e metadados disponíveis.
        /// </summary>
        public void ExibirNo(NoInteracaoAnatomica no, string caminhoAtual)
        {
            if (no == null)
            {
                Limpar();
                return;
            }

            var noAlterado = ultimoNoExibido != no;
            var informacao = no.EstruturaInformacao != null ? no.EstruturaInformacao : no.GetComponent<InformacaoEstruturaAnatomica>();
            var vinculo = no.VinculoAnatomico;
            OraculoAnatomico.TentarObter(vinculo, out var oraculo);
            var caminho = string.IsNullOrWhiteSpace(caminhoAtual) ? no.NomeExibicao : caminhoAtual.Replace(" / ", " > ");
            var descricao = oraculo != null ? oraculo.descricaoCientifica : MontarDescricao(no, informacao);
            descricao = OraculoAnatomico.ObterEstadoVerificacao(oraculo) + "\n\n" + descricao;
            var divisao = informacao != null ? $"Divisão: {NoInteracaoAnatomica.ConverterParaNomeExibidoEmPortugues(informacao.DivisaoPrincipal.ToString())}\nSubdivisão: {NoInteracaoAnatomica.ConverterParaNomeExibidoEmPortugues(informacao.Subdivisao.ToString())}" : $"Grupo: {no.NomeExibicao}\nRepresentações: {ContarRepresentacoesGrupo(no)}";
            // O nome em português selecionado é o cabeçalho visual. Mantém categoria/caminho como
            // metadados secundários em vez de repetir o mesmo nome em cada linha de texto.
            var cabecalho = oraculo != null && !string.IsNullOrWhiteSpace(oraculo.nomePortugues) ? oraculo.nomePortugues : no.NomeExibicao;
            DefinirTexto(textoTitulo, textoTituloAlternativo, cabecalho);
            DefinirTexto(textoEstado, textoEstadoAlternativo, vinculo != null && vinculo.EhGrupo ? "Categoria: grupo anatômico" : "Categoria: estrutura anatômica");
            DefinirTexto(textoCaminho, textoCaminhoAlternativo, $"Caminho: {caminho}");
            // O cabeçalho em português já carrega esta identidade. Inglês/latim permanecem
            // na linha dedicada de identidade abaixo, sem uma pilha de rótulos duplicada.
            DefinirTexto(textoNomeNo, textoNomeNoAlternativo, string.Empty);
            var identidade = vinculo != null && !vinculo.EhGrupo ? $"Código: {vinculo.ConceitoId}\nNome de origem (inglês): {vinculo.NomeCientificoExibido}" : $"Grupo anatômico: {no.NomeExibicao}";
            if (oraculo != null)
            {
                var codigo = string.IsNullOrWhiteSpace(oraculo.idFma) ? "Grupo anatômico" : $"{oraculo.idArquivo} | {oraculo.idFma}";
                identidade = $"{codigo}\n{oraculo.nomeIngles}";
                if (!string.IsNullOrWhiteSpace(oraculo.nomeLatim))
                    identidade += $"\n{oraculo.nomeLatim}";
            }

            DefinirTexto(textoNomeExibicao, textoNomeExibicaoAlternativo, identidade);
            DefinirTexto(textoDescricao, textoDescricaoAlternativa, descricao);
            DefinirTexto(textoDivisao, textoDivisaoAlternativa, divisao);
            DefinirTexto(textoNivel, textoNivelAlternativo, $"Nível: {no.NivelNo}");
            DefinirTexto(textoCorpo, textoCorpoAlternativo, MontarCorpo(no, caminho, descricao, divisao));
            if (noAlterado)
            {
                RedefinirRolagemDescricao();
            }

            ultimoNoExibido = no;
        }

        /// <summary>
        /// Mostra o foco atual quando não há estrutura selecionada.
        /// </summary>
        public void ExibirFoco(NoInteracaoAnatomica focoNo)
        {
            if (focoNo != null)
                ExibirNo(focoNo, focoNo.NomeExibicao);
            var focoNome = focoNo != null ? focoNo.NomeExibicao : "Sistema Nervoso Central";
            DefinirTexto(textoEstado, textoEstadoAlternativo, $"Estrutura em foco: {focoNome}");
            DefinirTexto(textoCaminho, textoCaminhoAlternativo, $"Caminho anatômico: {focoNome}");
        }

        /// <summary>
        /// Guarda texto acadêmico complementar para ser exibido junto da descrição.
        /// </summary>
        public void DefinirDadosComplementares(string titulo, string corpo)
        {
            tituloComplementar = titulo;
            corpoComplementar = corpo;
        }

        /// <summary>
        /// Alterna entre painel aberto e aba compacta.
        /// </summary>
        public void AlternarPainel()
        {
            DefinirVisibilidadePainel(!painelVisivel);
        }

        /// <summary>
        /// Define a visibilidade do painel sem recriar objetos ou trocar componentes.
        /// </summary>
        public void DefinirVisibilidadePainel(bool visivel)
        {
            painelVisivel = visivel;
            if (raizExpandida != null)
            {
                raizExpandida.SetActive(visivel);
            }

            if (raizAbaRecolhida != null)
            {
                raizAbaRecolhida.SetActive(!visivel);
            }

            if (raizPainel != null && raizExpandida == null)
            {
                raizPainel.SetActive(visivel);
            }

            DefinirTextoBotao(botaoAlternarPainel, "Ocultar painel");
            DefinirTextoBotao(expandirPainelBotao, "Mostrar painel");
        }

        /// <summary>
        /// Limpa a seleção visual e deixa o painel com mensagens iniciais em português.
        /// </summary>
        public void Limpar()
        {
            DefinirTexto(textoTitulo, textoTituloAlternativo, tituloPadrao);
            DefinirTexto(textoEstado, textoEstadoAlternativo, "Nenhuma estrutura selecionada");
            DefinirTexto(textoCaminho, textoCaminhoAlternativo, "Caminho anatômico: Sistema Nervoso Central");
            DefinirTexto(textoNomeNo, textoNomeNoAlternativo, "Nenhuma estrutura selecionada");
            DefinirTexto(textoNomeExibicao, textoNomeExibicaoAlternativo, "-");
            DefinirTexto(textoDescricao, textoDescricaoAlternativa, corpoPadrao);
            DefinirTexto(textoDivisao, textoDivisaoAlternativa, "Divisão: -");
            DefinirTexto(textoNivel, textoNivelAlternativo, "Nível: -");
            DefinirTexto(textoCorpo, textoCorpoAlternativo, corpoPadrao);
            ultimoNoExibido = null;
            RedefinirRolagemDescricao();
        }

        /// <summary>
        /// Reposiciona a descrição apenas quando a identidade mostrada muda ou é limpa.
        /// Funciona tanto com a variante TMP quanto com o Text legado preservado na cena.
        /// </summary>
        private void RedefinirRolagemDescricao()
        {
            var descricaoTransformacao = textoDescricao != null ? textoDescricao.transform : textoDescricaoAlternativa != null ? textoDescricaoAlternativa.transform : null;
            var rolagem = descricaoTransformacao != null ? descricaoTransformacao.GetComponentInParent<ScrollRect>() : null;
            if (rolagem == null || rolagem.content == null)
            {
                return;
            }

            rolagem.StopMovement();
            var ancorado = rolagem.content.anchoredPosition;
            rolagem.content.anchoredPosition = new Vector2(ancorado.x, 0f);
            rolagem.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// Resolve o botão da aba compacta em cenas antigas que ainda não serializaram a referência.
        /// </summary>
        private void ResolverReferenciasControles()
        {
            if (expandirPainelBotao == null && raizAbaRecolhida != null)
            {
                expandirPainelBotao = raizAbaRecolhida.GetComponentInChildren<Button>(true);
            }
        }

        /// <summary>
        /// Liga os botões de ocultar e mostrar painel sem acumular listeners antigos.
        /// </summary>
        private void ConectarBotoes()
        {
            if (botaoAlternarPainel != null)
            {
                botaoAlternarPainel.onClick.RemoveListener(AlternarPainel);
                botaoAlternarPainel.onClick.AddListener(AlternarPainel);
            }

            if (expandirPainelBotao != null)
            {
                expandirPainelBotao.onClick.RemoveListener(AlternarPainel);
                expandirPainelBotao.onClick.AddListener(AlternarPainel);
            }
        }

        /// <summary>
        /// Monta a descrição textual da estrutura selecionada.
        /// </summary>
        private string MontarDescricao(NoInteracaoAnatomica no, InformacaoEstruturaAnatomica informacao)
        {
            var construtor = new StringBuilder();
            if (informacao != null && !string.IsNullOrWhiteSpace(informacao.DescricaoBreve))
            {
                construtor.AppendLine(informacao.DescricaoBreve);
            }
            else
            {
                var vinculo = no.VinculoAnatomico;
                if (informacao == null)
                    construtor.AppendLine($"{ContarRepresentacoesGrupo(no)} representações neste grupo. Explore os subgrupos e as estruturas pela lista.");
            }

            if (no.VinculoAnatomico != null || informacao == null)
                construtor.AppendLine("Conteúdo didático aguardando revisão bibliográfica.");
            if (!string.IsNullOrWhiteSpace(tituloComplementar) || !string.IsNullOrWhiteSpace(corpoComplementar))
            {
                construtor.AppendLine();
                construtor.AppendLine(tituloComplementar);
                construtor.AppendLine(corpoComplementar);
            }

            return construtor.ToString().Trim();
        }

        private static int ContarRepresentacoesGrupo(NoInteracaoAnatomica no)
        {
            return no.VinculoAnatomico != null ? no.VinculoAnatomico.MembroEstruturas.Count : no.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true).Length;
        }

        /// <summary>
        /// Gera o corpo de texto detalhado para versões do painel que usam um campo único.
        /// </summary>
        private static string MontarCorpo(NoInteracaoAnatomica no, string caminho, string descricao, string divisao)
        {
            return $"Estrutura selecionada: {no.NomeExibicao}\nCaminho anatômico: {caminho}\nNome técnico: {no.NomeNo}\nNome de exibição: {no.NomeExibicao}\nNível: {no.NivelNo}\n{divisao}\n\n{descricao}";
        }

        /// <summary>
        /// Escreve o mesmo valor em TMP_Text e Text quando ambos existem.
        /// </summary>
        private static void DefinirTexto(TMP_Text alvo, Text alternativa, string valor)
        {
            if (alvo != null)
            {
                alvo.text = valor;
            }

            if (alternativa != null)
            {
                alternativa.text = valor;
            }
        }

        /// <summary>
        /// Atualiza o rótulo de um botão sem depender de uma classe específica de UI.
        /// </summary>
        private static void DefinirTextoBotao(Button botao, string valor)
        {
            if (botao == null)
            {
                return;
            }

            var texto = botao.GetComponentInChildren<Text>(true);
            if (texto != null)
            {
                texto.text = valor;
            }
        }
    }
}
