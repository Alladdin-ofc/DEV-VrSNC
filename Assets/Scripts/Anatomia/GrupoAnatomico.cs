using UnityEngine;

namespace VRSNC.Anatomia
{
    // Nome funcional em português: Grupo Anatômico do SNC
    // Nome técnico do script: GrupoAnatomico
    // Função: registra nome, caminho e divisão anatômica de um grupo da hierarquia.
    // Relação com o protótipo: ajuda a documentar a organização anatômica sem controlar seleção ou input.
    // Objetos da cena: usado em grupos anatômicos dentro do Sistema_Nervoso_Central quando o metadado está presente.
    /// <summary>
    /// Metadado leve para grupos anatômicos.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomicalGroup")]
    public sealed class GrupoAnatomico : MonoBehaviour
    {
        // Nome e caminho do grupo anatômico.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("groupName")]
        private string nomeGrupo;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("groupPath")]
        private string caminhoGrupo;
        // Divisão principal e subdivisão associadas ao grupo.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("majorDivision")]
        private DivisaoAnatomicaPrincipal divisaoPrincipal = DivisaoAnatomicaPrincipal.Desconhecido;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("subdivision")]
        private SubdivisaoAnatomica subdivisao = SubdivisaoAnatomica.Desconhecido;
        // Regra/fonte que explicou a classificação do grupo.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("sourceRule")]
        private string fonteRegra;
        public string NomeGrupo => nomeGrupo;
        public string CaminhoGrupo => caminhoGrupo;
        public DivisaoAnatomicaPrincipal DivisaoPrincipal => divisaoPrincipal;
        public SubdivisaoAnatomica Subdivisao => subdivisao;
        public string FonteRegra => fonteRegra;
        /// <summary>
        /// Define os metadados do grupo sem alterar sua posição na cena.
        /// </summary>
        public void DefinirGrupo(string nomeGrupo, string caminhoGrupo, DivisaoAnatomicaPrincipal divisaoPrincipal, SubdivisaoAnatomica subdivisao, string fonteRegra)
        {
            this.nomeGrupo = nomeGrupo;
            this.caminhoGrupo = caminhoGrupo;
            this.divisaoPrincipal = divisaoPrincipal;
            this.subdivisao = subdivisao;
            this.fonteRegra = fonteRegra;
        }
    }
}