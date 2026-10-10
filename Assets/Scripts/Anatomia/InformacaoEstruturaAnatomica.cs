using UnityEngine;

namespace VRSNC.Anatomia
{
    // Nome funcional em português: Informações Acadêmicas da Estrutura Anatômica
    // Nome técnico do script: InformacaoEstruturaAnatomica
    // Função: armazena nome anatômico, nome em português, divisão, subdivisão e descrição de uma estrutura do SNC.
    // Relação com o protótipo: fornece dados para o painel de informações sem interferir na seleção, raycast ou grab.
    // Objetos da cena: pode ser usado em nós anatômicos dentro do Sistema_Nervoso_Central.
    /// <summary>
    /// Guarda metadados anatômicos usados para documentação e exibição no painel.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainStructureInfo")]
    public sealed class InformacaoEstruturaAnatomica : MonoBehaviour
    {
        // Nome anatômico de referência, preservado para rastreabilidade acadêmica.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("anatomicalName")]
        private string nomeAnatomico;
        // Nome em português usado como fonte de exibição quando preenchido.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("portugueseName")]
        private string nomePortugues;
        // Caminho original da estrutura na cena antes da organização anatômica.
        [UnityEngine.Serialization.FormerlySerializedAs("originalCenaCaminho")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalScenePath")]
        private string caminhoCenaOriginal;
        // Grupo pai original usado para auditoria da classificação.
        [UnityEngine.Serialization.FormerlySerializedAs("originalPaiGrupo")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalParentGroup")]
        private string grupoPaiOriginal;
        // Classificações anatômicas principais usadas no painel e na documentação.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("majorDivision")]
        private DivisaoAnatomicaPrincipal divisaoPrincipal = DivisaoAnatomicaPrincipal.Desconhecido;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("subdivision")]
        private SubdivisaoAnatomica subdivisao = SubdivisaoAnatomica.Desconhecido;
        [UnityEngine.Serialization.FormerlySerializedAs("anatomicoGrupo")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("anatomicalGroup")]
        private SistemaAnatomico grupoAnatomico = SistemaAnatomico.NaoEspecificado;
        [UnityEngine.Serialization.FormerlySerializedAs("lobarLobo")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("corticalLobe")]
        private LoboCortical loboCortical = LoboCortical.Nenhum;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("hemisphereOrSpatialDivision")]
        private LadoAnatomico hemisferioOuEspacialDivisao = LadoAnatomico.NaoEspecificado;
        // Camada hierárquica/classificatória registrada durante a organização.
        [UnityEngine.Serialization.FormerlySerializedAs("camadaIndice")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("layerIndex")]
        private int indiceCamada = -1;
        // Texto curto e descrição funcional para uso acadêmico.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("shortDescription")]
        private string curtaDescricao;
        [UnityEngine.Serialization.FormerlySerializedAs("funcionalDescricao")]
        [TextArea(2, 5)]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("functionalDescription")]
        private string descricaoFuncional;
        // Estado de revisão do metadado anatômico.
        [UnityEngine.Serialization.FormerlySerializedAs("revisaoEstado")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("reviewStatus")]
        private EstadoRevisaoEstrutura estadoRevisao = EstadoRevisaoEstrutura.RequerRevisao;
        // Dados de origem para rastrear de onde veio a classificação.
        [UnityEngine.Serialization.FormerlySerializedAs("fonteObjetoNome")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("sourceObjectName")]
        private string nomeObjetoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("fonteCatalogoDivisao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("sourceCatalogDivision")]
        private string divisaoCatalogoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("fonteCatalogoSubdivisao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("sourceCatalogSubdivision")]
        private string subdivisaoCatalogoFonte;
        public string NomeAnatomico => nomeAnatomico;
        public string NomePortugues => nomePortugues;
        public string CaminhoCenaOriginal => caminhoCenaOriginal;
        public string GrupoPaiOriginal => grupoPaiOriginal;
        public DivisaoAnatomicaPrincipal DivisaoPrincipal => divisaoPrincipal;
        public SubdivisaoAnatomica Subdivisao => subdivisao;
        public SistemaAnatomico GrupoAnatomico => grupoAnatomico;
        public LoboCortical LoboCortical => loboCortical;
        public LadoAnatomico HemisferioOuEspacialDivisao => hemisferioOuEspacialDivisao;
        public int CamadaIndice => indiceCamada;
        public string DescricaoBreve => curtaDescricao;
        public string DescricaoFuncional => descricaoFuncional;
        public EstadoRevisaoEstrutura EstadoRevisao => estadoRevisao;
        public string NomeObjetoOrigem => nomeObjetoFonte;
        public string DivisaoCatalogoOrigem => divisaoCatalogoFonte;
        public string SubdivisaoCatalogoOrigem => subdivisaoCatalogoFonte;
        /// <summary>
        /// Preenche todos os metadados da estrutura sem alterar sua hierarquia ou componentes de interação.
        /// </summary>
        public void DefinirMetadados(string nomeAnatomico, string nomePortugues, string caminhoCenaOriginal, string grupoPaiOriginal, DivisaoAnatomicaPrincipal divisaoPrincipal, SubdivisaoAnatomica subdivisao, SistemaAnatomico grupoAnatomico, LoboCortical loboCortical, LadoAnatomico hemisferioOuEspacialDivisao, int indiceCamada, string curtaDescricao, string descricaoFuncional, EstadoRevisaoEstrutura estadoRevisao, string nomeObjetoFonte, string divisaoCatalogoFonte, string subdivisaoCatalogoFonte)
        {
            this.nomeAnatomico = nomeAnatomico;
            this.nomePortugues = nomePortugues;
            this.caminhoCenaOriginal = caminhoCenaOriginal;
            this.grupoPaiOriginal = grupoPaiOriginal;
            this.divisaoPrincipal = divisaoPrincipal;
            this.subdivisao = subdivisao;
            this.grupoAnatomico = grupoAnatomico;
            this.loboCortical = loboCortical;
            this.hemisferioOuEspacialDivisao = hemisferioOuEspacialDivisao;
            this.indiceCamada = indiceCamada;
            this.curtaDescricao = curtaDescricao;
            this.descricaoFuncional = descricaoFuncional;
            this.estadoRevisao = estadoRevisao;
            this.nomeObjetoFonte = nomeObjetoFonte;
            this.divisaoCatalogoFonte = divisaoCatalogoFonte;
            this.subdivisaoCatalogoFonte = subdivisaoCatalogoFonte;
        }
    }
}