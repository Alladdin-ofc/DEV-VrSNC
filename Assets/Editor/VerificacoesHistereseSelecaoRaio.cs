using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Verificações determinísticas que preservam a cena para o contrato de candidato do raio.
    /// Cria apenas objetos temporários e nunca salva, entra no modo de execução ou edita
    /// a cena ativa. O comportamento em execução/dispositivo ainda exige a barreira do operador.
    /// </summary>
    public static class VerificacoesHistereseSelecaoRaio
    {
        [MenuItem("Ferramentas/VR SNC/Mãos/Validar Histerese do Raio (Determinístico)")]
        public static void Validar()
        {
            var falhas = new List<string>();
            GameObject temporarioRaiz = null;
            try
            {
                VerificarTransicoesHisterese(falhas);
                VerificarResolucaoHierarquia(falhas, out temporarioRaiz);
                VerificarProtecaoSaturacaoFonte(falhas);
                if (falhas.Count == 0)
                {
                    Debug.Log("[VR SNC] APROVADO: histerese do raio, regra de reinicialização da interface, resolução de elemento terminal para grupo, alternativa para raio saturado e verificações de clique estável.");
                }
                else
                {
                    Debug.LogError("[VR SNC] FAIL: " + string.Join(" | ", falhas));
                }
            }
            finally
            {
                if (temporarioRaiz != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporarioRaiz);
                }
            }
        }

        private static void VerificarTransicoesHisterese(List<string> falhas)
        {
            var histerese = new HistereseCandidatoSelecao(0.015f, 0.08f);
            var candidatoA = new object();
            var candidatoB = new object();
            var resultado = histerese.Avaliar(candidatoA, 1f, true, 1f, 0f);
            Exigir(ReferenceEquals(resultado, candidatoA), "initial A commit", falhas);
            // A não está mais no conjunto de acertos atual. B a 1,1 m não pode herdar a
            // distância histórica de A, mas ainda deve se comprovar durante a breve permanência.
            resultado = histerese.Avaliar(candidatoB, 1.1f, false, float.PositiveInfinity, 0.01f);
            Exigir(ReferenceEquals(resultado, candidatoA), "permanência A->B inicia sem A", falhas);
            resultado = histerese.Avaliar(candidatoB, 1.1f, false, float.PositiveInfinity, 0.09f);
            Exigir(ReferenceEquals(resultado, candidatoB), "A->B confirma após permanência", falhas);
            histerese.Redefinir();
            histerese.Avaliar(candidatoA, 1f, true, 1f, 0f);
            resultado = histerese.Avaliar(candidatoB, 0.99f, true, 1f, 0.04f);
            Exigir(ReferenceEquals(resultado, candidatoA), "Oscilação A/B dentro da tolerância", falhas);
            resultado = histerese.Avaliar(candidatoA, 1f, true, 1f, 0.05f);
            Exigir(ReferenceEquals(resultado, candidatoA), "jitter re-arms stable A", falhas);
            // A invalidação por destruição/foco e a posse de UI próxima usam o mesmo
            // contrato explícito de redefinição; nenhuma deixa um alvo de clique obsoleto.
            histerese.Redefinir();
            resultado = histerese.Avaliar(null, float.PositiveInfinity, false, float.PositiveInfinity, 1f);
            Exigir(resultado == null && histerese.CandidatoEstavel == null, "foco inválido limpa estado", falhas);
            histerese.Avaliar(candidatoA, 1f, true, 1f, 2f);
            histerese.Redefinir();
            Exigir(histerese.CandidatoEstavel == null, "A interface, ao assumir a interação, limpa o alvo estável", falhas);
            histerese.Avaliar(candidatoA, 1f, true, 1f, 3f);
            resultado = histerese.Avaliar(candidatoB, 0.5f, true, 1f, 3.01f);
            Exigir(ReferenceEquals(resultado, candidatoB), "O candidato claramente mais próximo passa a ser o selecionado", falhas);
            Exigir(ReferenceEquals(histerese.CandidatoEstavel, resultado), "O alvo do clique corresponde ao indicado no feedback", falhas);
        }

        private static void VerificarResolucaoHierarquia(List<string> falhas, out GameObject temporarioRaiz)
        {
            temporarioRaiz = new GameObject("__VR_SNC_RayCheck_TemporaryRoot");
            var grupoObjeto = new GameObject("__VR_SNC_RayCheck_Group");
            grupoObjeto.transform.SetParent(temporarioRaiz.transform, false);
            var folhaObjeto = new GameObject("__VR_SNC_RayCheck_Leaf");
            folhaObjeto.transform.SetParent(grupoObjeto.transform, false);
            var raizNo = temporarioRaiz.AddComponent<NoInteracaoAnatomica>();
            var noGrupo = grupoObjeto.AddComponent<NoInteracaoAnatomica>();
            var folhaNo = folhaObjeto.AddComponent<NoInteracaoAnatomica>();
            folhaNo.Configurar("Leaf", "Leaf", 2, noGrupo, null, true, true, false);
            noGrupo.Configurar("Group", "Group", 1, raizNo, new[] { folhaNo }, true, true, false);
            raizNo.Configurar("Root", "Root", 0, null, new[] { noGrupo }, true, true, true);
            var gerenciador = temporarioRaiz.AddComponent<GerenciadorSelecaoHierarquica>();
            gerenciador.Configurar(raizNo, null, null, null);
            var resolvido = gerenciador.ResolverCandidato(folhaNo);
            Exigir(ReferenceEquals(resolvido, noGrupo), "O colisor do elemento terminal identifica o grupo direto em foco", falhas);
            // Dezesseis colliders não anatômicos saturam RaycastNonAlloc. O collider
            // anatômico válido fica deliberadamente atrás deles; a API pública do controlador
            // deve usar seu fallback RaycastAll e ainda retornar o nó válido.
            var origemObjeto = new GameObject("__VR_SNC_RayCheck_Origin");
            origemObjeto.transform.SetParent(temporarioRaiz.transform, false);
            var controlador = temporarioRaiz.AddComponent<ControladorSelecaoPorRaio>();
            controlador.Configurar(gerenciador, origemObjeto.transform);
            folhaObjeto.transform.localPosition = new Vector3(0f, 0f, 3f);
            folhaObjeto.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.2f, 0.2f);
            for (var indice = 0; indice < 16; indice++)
            {
                var bloqueador = new GameObject("__VR_SNC_RayCheck_NonAnatomy_" + indice);
                bloqueador.transform.SetParent(temporarioRaiz.transform, false);
                bloqueador.transform.localPosition = new Vector3(0f, 0f, 0.4f + indice * 0.1f);
                bloqueador.AddComponent<BoxCollider>().size = new Vector3(0.2f, 0.2f, 0.05f);
            }

            Physics.SyncTransforms();
            var encontrado = controlador.TentarObterAcertoValidoMaisProximo(out var maisProximo, out var distancia);
            Exigir(encontrado && ReferenceEquals(maisProximo, noGrupo), "A alternativa para raio saturado identifica uma estrutura anatômica válida", falhas);
            Exigir(Mathf.Abs(distancia - 2.9f) < 0.2f, "saturated ray fallback returns valid distance", falhas);
        }

        private static void VerificarProtecaoSaturacaoFonte(List<string> falhas)
        {
            var controladorCaminho = Path.Combine(Application.dataPath, "Scripts", "Interacao", "ControladorSelecaoPorRaio.cs");
            var fonte = File.ReadAllText(controladorCaminho);
            Exigir(fonte.Contains("Physics.RaycastNonAlloc"), "caminho de raio NonAlloc preservado", falhas);
            Exigir(fonte.Contains("quantidadeAcertos == raioBuffer.Length"), "ramo de saturação preservado", falhas);
            Exigir(fonte.Contains("Physics.RaycastAll"), "fallback de overflow preservado", falhas);
        }

        private static void Exigir(bool condicao, string nome, List<string> falhas)
        {
            if (!condicao)
            {
                falhas.Add(nome);
            }
        }
    }
}
