using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VRSNC.Anatomia
{
    /// <summary>Conecta um objeto de cena existente à sua identidade auditável de catálogo.</summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyBinding")]
    public sealed class VinculoAnatomico : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("isGroup")]
        private bool ehGrupo;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("catalogId")]
        private string catalogoId;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("conceptId")]
        private string conceitoId;
        [UnityEngine.Serialization.FormerlySerializedAs("fonteCientificoNome")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("sourceScientificName")]
        private string nomeCientificoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("exibicaoCientificoNome")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("displayScientificName")]
        private string nomeCientificoExibicao;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("memberStructures")]
        private List<InformacaoEstruturaAnatomica> membroEstruturas = new();
        public bool EhGrupo => ehGrupo;
        public string CatalogoId => catalogoId;
        public string ConceitoId => conceitoId;
        public string NomeCientificoOrigem => nomeCientificoFonte;
        public string NomeCientificoExibido => nomeCientificoExibicao;
        public IReadOnlyList<InformacaoEstruturaAnatomica> MembroEstruturas => membroEstruturas;
        public void Configurar(bool grupo, string id, string idConceitoFma, string fonteNome, string nomeExibicao, IEnumerable<InformacaoEstruturaAnatomica> membros)
        {
            ehGrupo = grupo;
            catalogoId = id;
            conceitoId = idConceitoFma;
            nomeCientificoFonte = fonteNome;
            nomeCientificoExibicao = nomeExibicao;
            membroEstruturas.Clear();
            if (membros != null)
                foreach (var membro in membros)
                    if (membro != null && !membroEstruturas.Contains(membro))
                        membroEstruturas.Add(membro);
        }

        /// <summary>Corrige apenas o erro ortográfico confirmado pelo usuário; os campos-fonte são preservados.</summary>
        public static string CorrigirGrafiaExibida(string valor)
        {
            return Regex.Replace(valor ?? string.Empty, @"\brigth\b", correspondencia => char.IsUpper(correspondencia.Value[0]) ? "Right" : "right", RegexOptions.IgnoreCase);
        }
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyMappingDocument")]
    public sealed class DocumentoMapeamentoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("schemaVersion")]
        public string versaoEsquema = "1.0.0";
        [UnityEngine.Serialization.FormerlySerializedAs("generatedUtc")]
        public string geradoEmUtc;
        [UnityEngine.Serialization.FormerlySerializedAs("scenePath")]
        public string caminhoCena;
        [UnityEngine.Serialization.FormerlySerializedAs("rootPath")]
        public string caminhoRaiz;
        [UnityEngine.Serialization.FormerlySerializedAs("sourceUrl")]
        public string urlFonte = "https://lifesciencedb.jp/bp3d/";
        [UnityEngine.Serialization.FormerlySerializedAs("sceneBackupPath")]
        public string caminhoCopiaSegurancaCena;
        public bool applied;
        [UnityEngine.Serialization.FormerlySerializedAs("representations")]
        public List<RegistroRepresentacaoAnatomica> representacoes = new();
        [UnityEngine.Serialization.FormerlySerializedAs("groups")]
        public List<RegistroGrupoAnatomico> grupos = new();
        [UnityEngine.Serialization.FormerlySerializedAs("concepts")]
        public List<RegistroConceitoAnatomico> conceitos = new();
        [UnityEngine.Serialization.FormerlySerializedAs("issues")]
        public List<ProblemaMapeamentoAnatomico> problemas = new();
        [UnityEngine.Serialization.FormerlySerializedAs("validation")]
        public ValidacaoMapeamentoAnatomico validacao = new();
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyRepresentationRecord")]
    public sealed class RegistroRepresentacaoAnatomica
    {
        [UnityEngine.Serialization.FormerlySerializedAs("representationKey")]
        public string chaveRepresentacao;
        [UnityEngine.Serialization.FormerlySerializedAs("sceneObjectId")]
        public string idObjetoCena;
        [UnityEngine.Serialization.FormerlySerializedAs("scenePath")]
        public string caminhoCena;
        [UnityEngine.Serialization.FormerlySerializedAs("gameObjectNameRaw")]
        public string nomeOriginalObjeto;
        [UnityEngine.Serialization.FormerlySerializedAs("anatomicalNameRaw")]
        public string nomeAnatomicoOriginal;
        [UnityEngine.Serialization.FormerlySerializedAs("anatomicalNameDisplay")]
        public string nomeAnatomicoExibido;
        [UnityEngine.Serialization.FormerlySerializedAs("portugueseNameRaw")]
        public string nomePortuguesOriginal;
        [UnityEngine.Serialization.FormerlySerializedAs("portugueseNameDisplay")]
        public string nomePortuguesExibido;
        [UnityEngine.Serialization.FormerlySerializedAs("sourceObjectNameRaw")]
        public string nomeOriginalObjetoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("originalScenePath")]
        public string caminhoCenaOriginal;
        [UnityEngine.Serialization.FormerlySerializedAs("originalParentGroup")]
        public string grupoPaiOriginal;
        [UnityEngine.Serialization.FormerlySerializedAs("assetPath")]
        public string caminhoRecurso;
        [UnityEngine.Serialization.FormerlySerializedAs("assetGuid")]
        public string guidRecurso;
        [UnityEngine.Serialization.FormerlySerializedAs("objFileId")]
        public string idArquivoObj;
        [UnityEngine.Serialization.FormerlySerializedAs("objRepresentationId")]
        public string idRepresentacaoObj;
        [UnityEngine.Serialization.FormerlySerializedAs("fmaConceptId")]
        public string idConceitoFma;
        [UnityEngine.Serialization.FormerlySerializedAs("objEnglishName")]
        public string nomeInglesObj;
        [UnityEngine.Serialization.FormerlySerializedAs("objBuildUpLogic")]
        public string logicaComposicaoObj;
        [UnityEngine.Serialization.FormerlySerializedAs("objCompatibilityVersion")]
        public string versaoCompatibilidadeObj;
        [UnityEngine.Serialization.FormerlySerializedAs("groupId")]
        public string idGrupo;
        [UnityEngine.Serialization.FormerlySerializedAs("groupPath")]
        public string caminhoGrupo;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedMajorDivision")]
        public string divisaoPrincipalSerializada;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedSubdivision")]
        public string subdivisaoSerializada;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedAnatomicalGroup")]
        public string grupoAnatomicoSerializado;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedCorticalLobe")]
        public string loboCorticalSerializado;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedSide")]
        public string ladoSerializado;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedReviewStatus")]
        public string estadoRevisaoSerializado;
        [UnityEngine.Serialization.FormerlySerializedAs("sourceCatalogDivision")]
        public string divisaoCatalogoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("sourceCatalogSubdivision")]
        public string subdivisaoCatalogoFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("legacyShortDescription")]
        public string descricaoBreveLegada;
        [UnityEngine.Serialization.FormerlySerializedAs("legacyFunctionalDescription")]
        public string descricaoFuncionalLegada;
        [UnityEngine.Serialization.FormerlySerializedAs("contentStatus")]
        public string estadoConteudo = "pending_review";
        [UnityEngine.Serialization.FormerlySerializedAs("issues")]
        public List<string> problemas = new();
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyGroupRecord")]
    public sealed class RegistroGrupoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("groupId")]
        public string idGrupo;
        [UnityEngine.Serialization.FormerlySerializedAs("scenePath")]
        public string caminhoCena;
        [UnityEngine.Serialization.FormerlySerializedAs("nodeNameRaw")]
        public string nomeNoOriginal;
        [UnityEngine.Serialization.FormerlySerializedAs("displayName")]
        public string nomeExibicao;
        [UnityEngine.Serialization.FormerlySerializedAs("parentGroupId")]
        public string idGrupoPai;
        [UnityEngine.Serialization.FormerlySerializedAs("level")]
        public int nivel;
        [UnityEngine.Serialization.FormerlySerializedAs("directChildGroupIds")]
        public List<string> idsGruposFilhosDiretos = new();
        [UnityEngine.Serialization.FormerlySerializedAs("directRepresentationKeys")]
        public List<string> chavesRepresentacoesDiretas = new();
        [UnityEngine.Serialization.FormerlySerializedAs("descendantRepresentationKeys")]
        public List<string> chavesRepresentacoesDescendentes = new();
        [UnityEngine.Serialization.FormerlySerializedAs("contentStatus")]
        public string estadoConteudo = "pending_review";
        [UnityEngine.Serialization.FormerlySerializedAs("approvedDescription")]
        public string descricaoAprovada = "";
        [UnityEngine.Serialization.FormerlySerializedAs("serializedMajorDivision")]
        public string divisaoPrincipalSerializada;
        [UnityEngine.Serialization.FormerlySerializedAs("serializedSubdivision")]
        public string subdivisaoSerializada;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyConceptRecord")]
    public sealed class RegistroConceitoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("conceptKey")]
        public string chaveConceito;
        [UnityEngine.Serialization.FormerlySerializedAs("fmaConceptId")]
        public string idConceitoFma;
        [UnityEngine.Serialization.FormerlySerializedAs("objEnglishNames")]
        public List<string> nomesInglesObj = new();
        [UnityEngine.Serialization.FormerlySerializedAs("representationKeys")]
        public List<string> chavesRepresentacoes = new();
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyIssue")]
    public sealed class ProblemaMapeamentoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("code")]
        public string codigo;
        [UnityEngine.Serialization.FormerlySerializedAs("severity")]
        public string gravidade;
        [UnityEngine.Serialization.FormerlySerializedAs("targetId")]
        public string idAlvo;
        [UnityEngine.Serialization.FormerlySerializedAs("message")]
        public string mensagem;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomyValidation")]
    public sealed class ValidacaoMapeamentoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("representationCount")]
        public int quantidadeRepresentacoes;
        [UnityEngine.Serialization.FormerlySerializedAs("groupCount")]
        public int quantidadeGrupos;
        [UnityEngine.Serialization.FormerlySerializedAs("conceptCount")]
        public int quantidadeConceitos;
        [UnityEngine.Serialization.FormerlySerializedAs("interactionNodeCount")]
        public int quantidadeNosInteracao;
        [UnityEngine.Serialization.FormerlySerializedAs("directBindings")]
        public int vinculosDiretos;
        [UnityEngine.Serialization.FormerlySerializedAs("representationsWithCollider")]
        public int representacoesComColisor;
        [UnityEngine.Serialization.FormerlySerializedAs("addedLeafNodes")]
        public int nosFolhaAdicionados;
        [UnityEngine.Serialization.FormerlySerializedAs("addedMeshColliders")]
        public int colisoresMalhaAdicionados;
        [UnityEngine.Serialization.FormerlySerializedAs("errorCount")]
        public int quantidadeErros;
        [UnityEngine.Serialization.FormerlySerializedAs("warningCount")]
        public int quantidadeAvisos;
        [UnityEngine.Serialization.FormerlySerializedAs("geometricInvariantsPreserved")]
        public bool invariantesGeometricasPreservadas;
        [UnityEngine.Serialization.FormerlySerializedAs("graphValidated")]
        public bool grafoValidado;
        [UnityEngine.Serialization.FormerlySerializedAs("idempotenceValidated")]
        public bool idempotenciaValidada;
        [UnityEngine.Serialization.FormerlySerializedAs("geometryFingerprintBefore")]
        public string assinaturaGeometriaAntes;
        [UnityEngine.Serialization.FormerlySerializedAs("geometryFingerprintAfter")]
        public string assinaturaGeometriaDepois;
        [UnityEngine.Serialization.FormerlySerializedAs("validationScope")]
        public string escopoValidacao = "Editor mapping, graph, source identity and unchanged geometry; no headset runtime test";
    }
}