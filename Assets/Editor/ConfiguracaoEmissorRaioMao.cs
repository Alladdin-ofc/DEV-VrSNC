using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Instala de modo idempotente decoradores de caster apenas nos interactors Near/Far
    /// existentes das mãos. Nunca busca nem edita proprietários de raio exclusivos de controle
    /// e nunca salva a cena automaticamente.
    /// </summary>
    public static class ConfiguracaoEmissorRaioMao
    {
        [MenuItem("Ferramentas/VR SNC/Mãos/Instalar Contexto de Raio Próximo/Distante (Mãos)")]
        public static void Instalar()
        {
            var interatores = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var gerenciadoresModalidade = Object.FindObjectsByType<XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var alterado = 0;
            foreach (var interator in interatores)
            {
                if (interator == null || !EhEmMaoGrupo(interator, gerenciadoresModalidade))
                {
                    continue;
                }

                Undo.RecordObject(interator, "Instalar o contexto de raio da mão do SNC");
                InstalarProximo(interator);
                InstalarDistante(interator);
                InstalarFiltro(interator);
                EditorUtility.SetDirty(interator);
                alterado++;
            }

            Debug.Log($"[VR SNC] Contexto Near/Far da mão preparado em {alterado} interator(es) de mão já existentes. Revise e salve a cena manualmente.");
        }

        private static bool EhEmMaoGrupo(NearFarInteractor interator, XRInputModalityManager[] gerenciadoresModalidade)
        {
            for (var indice = 0; indice < gerenciadoresModalidade.Length; indice++)
            {
                var modalidade = gerenciadoresModalidade[indice];
                if (modalidade == null)
                    continue;
                if (EhFilhoDe(interator.transform, modalidade.leftHand) || EhFilhoDe(interator.transform, modalidade.rightHand))
                    return true;
            }

            return false;
        }

        private static bool EhFilhoDe(Transform candidato, GameObject pai)
        {
            return candidato != null && pai != null && (candidato == pai.transform || candidato.IsChildOf(pai.transform));
        }

        private static void InstalarProximo(NearFarInteractor interator)
        {
            var atual = interator.nearInteractionCaster;
            var adaptador = interator.GetComponent<AdaptadorEmissorInteracaoProxima>();
            if (atual is AdaptadorEmissorInteracaoProxima existente)
            {
                adaptador = existente;
                return;
            }

            if (atual == null)
                return;
            if (adaptador == null)
                adaptador = Undo.AddComponent<AdaptadorEmissorInteracaoProxima>(interator.gameObject);
            adaptador.Configurar(interator, atual);
            interator.nearInteractionCaster = adaptador;
            EditorUtility.SetDirty(adaptador);
        }

        private static void InstalarDistante(NearFarInteractor interator)
        {
            var atual = interator.farInteractionCaster;
            var adaptador = interator.GetComponent<AdaptadorEmissorInteracaoDistante>();
            if (atual is AdaptadorEmissorInteracaoDistante existente)
            {
                adaptador = existente;
                return;
            }

            if (atual == null)
                return;
            if (adaptador == null)
                adaptador = Undo.AddComponent<AdaptadorEmissorInteracaoDistante>(interator.gameObject);
            adaptador.Configurar(interator, atual);
            interator.farInteractionCaster = adaptador;
            EditorUtility.SetDirty(adaptador);
        }

        private static void InstalarFiltro(NearFarInteractor interator)
        {
            var existente = interator.GetComponent<FiltroAlvoEstavelRaioMao>();
            if (existente != null)
            {
                return;
            }

            var anterior = interator.targetFilter ?? interator.startingTargetFilter;
            if (anterior != null && !(anterior is XRBaseTargetFilter))
            {
                Debug.LogWarning($"[VR SNC] Filtro de raio da mão ignorado em '{interator.name}': o filtro de alvo existente não é um XRBaseTargetFilter e não pode ser serializado com segurança.");
                return;
            }

            var filtro = Undo.AddComponent<FiltroAlvoEstavelRaioMao>(interator.gameObject);
            filtro.Configurar(interator, anterior);
            if (anterior == null)
                interator.sortNearTargetsAfterTargetFilter = true;
            interator.startingTargetFilter = filtro;
            interator.targetFilter = filtro;
            EditorUtility.SetDirty(filtro);
        }
    }
}
