using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRSNC.Anatomia
{
    /// <summary>Conteúdo científico offline e versionado, exportado do oráculo anatômico. Sem respostas geradas em execução.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainAnatomicalOracle")]
    public static class OraculoAnatomico
    {
        private static Dictionary<string, RegistroOraculoAnatomico> registros;
        private static Dictionary<string, FonteOraculoAnatomico> fontes;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void LimparCache()
        {
            registros = null;
            fontes = null;
        }

        public static bool TentarObter(VinculoAnatomico vinculo, out RegistroOraculoAnatomico registro)
        {
            registro = null;
            if (vinculo == null)
                return false;
            GarantirCarregado();
            return registros.TryGetValue(vinculo.CatalogoId, out registro);
        }

        private static void GarantirCarregado()
        {
            if (registros != null)
                return;
            registros = new Dictionary<string, RegistroOraculoAnatomico>(StringComparer.Ordinal);
            fontes = new Dictionary<string, FonteOraculoAnatomico>(StringComparer.Ordinal);
            var recurso = Resources.Load<TextAsset>("AnatomicalOracle");
            if (recurso == null)
            {
                Debug.LogWarning("[Oráculo Anatômico] Recurso não instalado; nenhuma descrição científica será inventada.");
                return;
            }

            try
            {
                var documento = LeitorJsonCompativel.Ler<DocumentoOraculoAnatomico>(recurso.text);
                if (documento == null || documento.registros == null || documento.fontes == null)
                    throw new FormatException("Faltam registros ou fontes.");
                foreach (var fonte in documento.fontes)
                    fontes.Add(fonte.id, fonte);
                foreach (var registro in documento.registros)
                {
                    if (string.IsNullOrWhiteSpace(registro.idCatalogo) || string.IsNullOrWhiteSpace(registro.nomePortugues) || string.IsNullOrWhiteSpace(registro.descricaoCientifica) || string.IsNullOrWhiteSpace(registro.resumoDidatico) || registro.idsFontesInformacao == null || registro.idsFontesInformacao.Length == 0)
                        throw new FormatException("Registro científico incompleto: " + registro.chave);
                    foreach (var id in registro.idsFontesInformacao)
                        if (!fontes.ContainsKey(id))
                            throw new FormatException("Falta bibliografia: " + id);
                    registros.Add(registro.idCatalogo, registro);
                }
            }
            catch (Exception erro)
            {
                registros.Clear();
                fontes.Clear();
                Debug.LogError("[Oráculo Anatômico] Dados inválidos: " + erro.Message);
            }
        }

        /// <summary>Exibe o estado editorial registrado, sem alegar validação científica independente.</summary>
        public static string ObterEstadoVerificacao(RegistroOraculoAnatomico registro)
        {
            if (registro == null)
                return "Verificação: ficha científica indisponível.\nRevisão docente: não registrada.";
            var estado = registro.estadoVerificacao switch
            {
                "source_checked" => "conferida nas fontes registradas",
                "limited_specificity" => "especificidade limitada — a fonte pode tratar de um grupo mais amplo",
                _ => "não confirmada no catálogo",
            };
            var revisao = string.IsNullOrWhiteSpace(registro.revisaoDocente) ? "não registrada" : registro.revisaoDocente;
            var texto = $"Verificação: {estado}.\nRevisão docente: {revisao}.";
            if (!string.IsNullOrWhiteSpace(registro.notasRevisao))
                texto += "\nLimitações: " + registro.notasRevisao;
            return texto;
        }

        public static string ObterBibliografia(RegistroOraculoAnatomico registro)
        {
            if (registro == null)
                return string.Empty;
            GarantirCarregado();
            var texto = new StringBuilder();
            foreach (var id in registro.idsFontesInformacao)
            {
                if (!fontes.TryGetValue(id, out var fonte))
                    continue;
                if (texto.Length > 0)
                    texto.AppendLine().AppendLine();
                texto.Append(fonte.autores).Append(". ").Append(fonte.titulo).Append(". ").Append(fonte.ano).AppendLine(".");
                if (registro.localizadoresFontes != null)
                    foreach (var localizador in registro.localizadoresFontes)
                        if (localizador.idFonte == id && localizador.conteudoSustentado == "description")
                            texto.AppendLine(localizador.localizador);
                texto.Append(fonte.url);
            }

            return texto.ToString();
        }
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainOracleDocument")]
    public sealed class DocumentoOraculoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("schemaVersion")]
        public string versaoEsquema;
        [UnityEngine.Serialization.FormerlySerializedAs("version")]
        public string versao;
        [UnityEngine.Serialization.FormerlySerializedAs("records")]
        public RegistroOraculoAnatomico[] registros;
        [UnityEngine.Serialization.FormerlySerializedAs("sources")]
        public FonteOraculoAnatomico[] fontes;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainOracleRecord")]
    public sealed class RegistroOraculoAnatomico
    {
        [UnityEngine.Serialization.FormerlySerializedAs("key")]
        public string chave;
        [UnityEngine.Serialization.FormerlySerializedAs("catalogId")]
        public string idCatalogo;
        [UnityEngine.Serialization.FormerlySerializedAs("parentKey")]
        public string chavePai;
        [UnityEngine.Serialization.FormerlySerializedAs("type")]
        public string tipo;
        [UnityEngine.Serialization.FormerlySerializedAs("scenePath")]
        public string caminhoCena;
        [UnityEngine.Serialization.FormerlySerializedAs("fileId")]
        public string idArquivo;
        [UnityEngine.Serialization.FormerlySerializedAs("bpId")]
        public string idBodyParts;
        [UnityEngine.Serialization.FormerlySerializedAs("fmaId")]
        public string idFma;
        [UnityEngine.Serialization.FormerlySerializedAs("nameEn")]
        public string nomeIngles;
        [UnityEngine.Serialization.FormerlySerializedAs("namePt")]
        public string nomePortugues;
        [UnityEngine.Serialization.FormerlySerializedAs("latinName")]
        public string nomeLatim;
        [UnityEngine.Serialization.FormerlySerializedAs("scientificDescriptionPt")]
        public string descricaoCientifica;
        [UnityEngine.Serialization.FormerlySerializedAs("didacticSummaryPt")]
        public string resumoDidatico;
        [UnityEngine.Serialization.FormerlySerializedAs("verification")]
        public string estadoVerificacao;
        [UnityEngine.Serialization.FormerlySerializedAs("editorialReview")]
        public string revisaoDocente;
        [UnityEngine.Serialization.FormerlySerializedAs("reviewNotes")]
        public string notasRevisao;
        [UnityEngine.Serialization.FormerlySerializedAs("informationSourceIds")]
        public string[] idsFontesInformacao;
        [UnityEngine.Serialization.FormerlySerializedAs("sourceLocators")]
        public LocalizadorFonteAnatomica[] localizadoresFontes;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainOracleSource")]
    public sealed class FonteOraculoAnatomico
    {
        public string id;
        [UnityEngine.Serialization.FormerlySerializedAs("title")]
        public string titulo;
        [UnityEngine.Serialization.FormerlySerializedAs("authors")]
        public string autores;
        [UnityEngine.Serialization.FormerlySerializedAs("year")]
        public string ano;
        public string url;
        [UnityEngine.Serialization.FormerlySerializedAs("accessed")]
        public string dataAcesso;
    }

    [Serializable]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Anatomy", "Assembly-CSharp", "BrainOracleLocator")]
    public sealed class LocalizadorFonteAnatomica
    {
        [UnityEngine.Serialization.FormerlySerializedAs("sourceId")]
        public string idFonte;
        [UnityEngine.Serialization.FormerlySerializedAs("locator")]
        public string localizador;
        [UnityEngine.Serialization.FormerlySerializedAs("supports")]
        public string conteudoSustentado;
    }
}
