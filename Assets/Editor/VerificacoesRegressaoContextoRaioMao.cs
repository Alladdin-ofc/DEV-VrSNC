using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Verificações exclusivas do modo de edição para os decoradores de caster Near/Far. Cada cenário usa
    /// uma fixture HideAndDontSave isolada e a destrói em seu próprio bloco finally.
    /// </summary>
    public static class VerificacoesRegressaoContextoRaioMao
    {
        [MenuItem("Ferramentas/VR SNC/Mãos/Validar Contexto de Raio Próximo/Distante (Determinístico)")]
        public static void Validar()
        {
            var falhas = new List<string>();
            VerificarDelegacaoEFonteEmissor(falhas);
            VerificarRaioProximoVazioPreservaEstadoDistante(falhas);
            VerificarFiltroDesabilitadoPermitePassagemDiretaEReabilitacao(falhas);
            if (falhas.Count == 0)
            {
                Debug.Log("[VR SNC] PASS: contexto de origem Near/Far, delegação de modelo UI Far falso/verdadeiro com encaminhamento de seleção/rolagem, caminho Far pré-seleção, delegação única de caster, preservação Near vazio, passagem de filtro desabilitado e restauração ao reabilitar.");
            }
            else
            {
                Debug.LogError("[VR SNC] FAIL: " + string.Join(" | ", falhas));
            }
        }

        private static void VerificarDelegacaoEFonteEmissor(List<string> falhas)
        {
            var cenarioTeste = CriarCenarioTeste("__VR_SNC_RayContext_Caster");
            var responsavel = cenarioTeste.GetComponentInChildren<NearFarInteractor>();
            EmissorProximoRegressao proximoProvedor = null;
            EmissorDistanteRegressao distanteProvedor = null;
            try
            {
                proximoProvedor = ScriptableObject.CreateInstance<EmissorProximoRegressao>();
                var proximoAdaptador = responsavel.gameObject.AddComponent<AdaptadorEmissorInteracaoProxima>();
                proximoAdaptador.Configurar(responsavel, proximoProvedor);
                proximoAdaptador.TryGetColliderTargets(null, new List<Collider>());
                Exigir(proximoProvedor.chamadas == 1, "Near delegates exactly once", falhas);
                Exigir(ContextoEmissorRaioMao.TentarObter(responsavel, out var proximoFonte, out _, out _) && proximoFonte == ContextoEmissorRaioMao.Origem.Proximo, "origem Near registrada antes da seleção", falhas);
                distanteProvedor = ScriptableObject.CreateInstance<EmissorDistanteRegressao>();
                var distanteAdaptador = responsavel.gameObject.AddComponent<AdaptadorEmissorInteracaoDistante>();
                distanteAdaptador.Configurar(responsavel, distanteProvedor);
                distanteAdaptador.TryGetColliderTargets(null, new List<Collider>(), new List<RaycastHit>());
                Exigir(distanteProvedor.chamadas == 1, "Far delegates exactly once", falhas);
                VerificarDelegacaoModeloInterfaceDistante(distanteAdaptador, distanteProvedor, falhas);
                Exigir(responsavel.selectionRegion.Value == NearFarInteractor.Region.None, "No cenário de pré-seleção distante, a região continua sendo Region.None", falhas);
                Exigir(ContextoEmissorRaioMao.TentarObter(responsavel, out var distanteFonte, out _, out _) && distanteFonte == ContextoEmissorRaioMao.Origem.Distante, "origem Far registrada antes da seleção", falhas);
            }
            finally
            {
                DestruirProvedor(proximoProvedor);
                DestruirProvedor(distanteProvedor);
                ContextoEmissorRaioMao.Limpar(responsavel);
                Object.DestroyImmediate(cenarioTeste);
            }
        }

        // O caster distante decorado também é a fonte do modelo de UI do NearFarInteractor. O
        // adaptador deve preservar essa interface e retransmitir tanto o retorno quanto o estado
        // de entrada; caso contrário, o NearFarInteractor redefine o modelo antes dos raycasts de UI XR.
        private static void VerificarDelegacaoModeloInterfaceDistante(AdaptadorEmissorInteracaoDistante adaptador, EmissorDistanteRegressao provedor, List<string> falhas)
        {
            var atualizador = adaptador as IUIModelUpdater;
            Exigir(provedor is IUIModelUpdater, "Regression far provider exposes IUIModelUpdater", falhas);
            Exigir(atualizador != null, "Far adapter preserves IUIModelUpdater", falhas);
            if (atualizador == null)
                return;
            provedor.interfaceResultado = false;
            var rejeitado = new TrackedDeviceModel();
            Exigir(!atualizador.UpdateUIModel(ref rejeitado, true, new Vector2(1.25f, -0.5f)), "Far UI model delegate preserves false return", falhas);
            Exigir(provedor.interfaceChamadas == 1 && provedor.ultimoSelecionar && provedor.ultimaRolagem == new Vector2(1.25f, -0.5f), "delegado de modelo UI Far encaminha valores de seleção e rolagem", falhas);
            provedor.interfaceResultado = true;
            var aceito = new TrackedDeviceModel();
            Exigir(atualizador.UpdateUIModel(ref aceito, false, new Vector2(-2f, 0.75f)), "Far UI model delegate preserves true return", falhas);
            Exigir(provedor.interfaceChamadas == 2 && !provedor.ultimoSelecionar && provedor.ultimaRolagem == new Vector2(-2f, 0.75f) && aceito.select == false && aceito.scrollDelta == new Vector2(-2f, 0.75f), "delegado de modelo UI Far encaminha dados do modelo sem substituição", falhas);
        }

        private static void VerificarRaioProximoVazioPreservaEstadoDistante(List<string> falhas)
        {
            var cenarioTeste = CriarCenarioTeste("__VR_SNC_RayContext_NearEmpty");
            var responsavel = cenarioTeste.GetComponentInChildren<NearFarInteractor>();
            EmissorProximoRegressao proximoProvedor = null;
            try
            {
                var primeiroObjeto = CriarOcultoFilho(cenarioTeste, "__VR_SNC_RayContext_A");
                var segundoObjeto = CriarOcultoFilho(cenarioTeste, "__VR_SNC_RayContext_B");
                var primeiro = primeiroObjeto.AddComponent<ElementoInteragivelMao>();
                var segundo = segundoObjeto.AddComponent<ElementoInteragivelMao>();
                var filtro = responsavel.gameObject.AddComponent<FiltroAlvoEstavelRaioMao>();
                filtro.Configurar(responsavel, null);
                var resultados = new List<IXRInteractable>();
                ContextoEmissorRaioMao.MarcarInteracaoDistante(responsavel, null, null);
                filtro.Process(responsavel, new List<IXRInteractable> { primeiro }, resultados);
                Exigir(resultados.Count == 1 && ReferenceEquals(resultados[0], primeiro), "À distância, o primeiro candidato se estabiliza antes da seleção", falhas);
                // O Near bruto vê um collider, mas o gerenciador de interação o rejeitaria;
                // os alvos válidos após o registro continuam vazios.
                proximoProvedor = ScriptableObject.CreateInstance<EmissorProximoRegressao>();
                proximoProvedor.colisorBruto = responsavel.gameObject.AddComponent<BoxCollider>();
                var proximoAdaptador = responsavel.gameObject.AddComponent<AdaptadorEmissorInteracaoProxima>();
                proximoAdaptador.Configurar(responsavel, proximoProvedor);
                proximoAdaptador.TryGetColliderTargets(null, new List<Collider>());
                resultados.Clear();
                filtro.Process(responsavel, new List<IXRInteractable>(), resultados);
                Exigir(resultados.Count == 0, "Na interação próxima, a passagem sem alvo não seleciona nenhum objeto", falhas);
                ContextoEmissorRaioMao.MarcarInteracaoDistante(responsavel, null, null);
                resultados.Clear();
                filtro.Process(responsavel, new List<IXRInteractable> { segundo }, resultados);
                Exigir(resultados.Count == 0, "hit somente bruto Near não redefiniu candidato estável Far; substituto ainda permanece", falhas);
            }
            finally
            {
                DestruirProvedor(proximoProvedor);
                ContextoEmissorRaioMao.Limpar(responsavel);
                Object.DestroyImmediate(cenarioTeste);
            }
        }

        private static void VerificarFiltroDesabilitadoPermitePassagemDiretaEReabilitacao(List<string> falhas)
        {
            var cenarioTeste = CriarCenarioTeste("__VR_SNC_RayContext_Filter");
            var responsavel = cenarioTeste.GetComponentInChildren<NearFarInteractor>();
            try
            {
                var alvoObjeto = CriarOcultoFilho(cenarioTeste, "__VR_SNC_RayContext_Passthrough");
                var alvo = alvoObjeto.AddComponent<ElementoInteragivelMao>();
                var anterior = responsavel.gameObject.AddComponent<FiltroRegressaoDesativado>();
                var filtro = responsavel.gameObject.AddComponent<FiltroAlvoEstavelRaioMao>();
                filtro.Configurar(responsavel, anterior);
                ContextoEmissorRaioMao.MarcarInteracaoDistante(responsavel, null, null);
                var resultados = new List<IXRInteractable>();
                filtro.Process(responsavel, new List<IXRInteractable> { alvo }, resultados);
                Exigir(resultados.Count == 1 && ReferenceEquals(resultados[0], alvo), "filtro encapsulado desabilitado repassa alvos", falhas);
                responsavel.startingTargetFilter = filtro;
                responsavel.targetFilter = filtro;
                responsavel.sortNearTargetsAfterTargetFilter = false;
                InvocarCicloVida(filtro, "OnDisable");
                Exigir(ReferenceEquals((object)responsavel.targetFilter, (object)anterior) && ReferenceEquals((object)responsavel.startingTargetFilter, (object)anterior), "desabilitar restaura referências do filtro encapsulado", falhas);
                Exigir(!responsavel.sortNearTargetsAfterTargetFilter, "Ao desativar, a ordenação anterior da interação próxima é restaurada", falhas);
                InvocarCicloVida(filtro, "OnEnable");
                Exigir(ReferenceEquals((object)responsavel.targetFilter, (object)filtro) && ReferenceEquals((object)responsavel.startingTargetFilter, (object)filtro), "reabilitar restaura referências do filtro estável", falhas);
                Exigir(!responsavel.sortNearTargetsAfterTargetFilter, "Ao reativar, a ordenação anterior da interação próxima é mantida", falhas);
            }
            finally
            {
                ContextoEmissorRaioMao.Limpar(responsavel);
                Object.DestroyImmediate(cenarioTeste);
            }
        }

        private static GameObject CriarCenarioTeste(string nome)
        {
            var raiz = new GameObject(nome)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var responsavelObjeto = new GameObject(nome + "_Owner")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            responsavelObjeto.transform.SetParent(raiz.transform, false);
            responsavelObjeto.AddComponent<NearFarInteractor>();
            return raiz;
        }

        private static GameObject CriarOcultoFilho(GameObject pai, string nome)
        {
            var filho = new GameObject(nome)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            filho.transform.SetParent(pai.transform, false);
            return filho;
        }

        private static void DestruirProvedor(Object provedor)
        {
            if (provedor != null)
                Object.DestroyImmediate(provedor);
        }

        private static void InvocarCicloVida(Object alvo, string metodoNome)
        {
            var metodo = alvo.GetType().GetMethod(metodoNome, BindingFlags.Instance | BindingFlags.NonPublic);
            metodo?.Invoke(alvo, null);
        }

        private static void Exigir(bool condicao, string nome, List<string> falhas)
        {
            if (!condicao)
                falhas.Add(nome);
        }

        private sealed class EmissorProximoRegressao : ScriptableObject, IInteractionCaster
        {
            [UnityEngine.Serialization.FormerlySerializedAs("calls")]
            public int chamadas;
            [UnityEngine.Serialization.FormerlySerializedAs("brutocolisor")]
            [UnityEngine.Serialization.FormerlySerializedAs("rawCollider")]
            public Collider colisorBruto;
            public bool isInitialized => true;
            public Transform castOrigin
            {
                get; set;
            }

            public Transform effectiveCastOrigin => castOrigin;
            public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> alvos)
            {
                chamadas++;
                alvos?.Clear();
                if (colisorBruto != null)
                    alvos?.Add(colisorBruto);
                return false;
            }
        }

        private sealed class EmissorDistanteRegressao : ScriptableObject, ICurveInteractionCaster, IUIModelUpdater
        {
            [UnityEngine.Serialization.FormerlySerializedAs("calls")]
            public int chamadas;
            [UnityEngine.Serialization.FormerlySerializedAs("uiCalls")]
            public int interfaceChamadas;
            [UnityEngine.Serialization.FormerlySerializedAs("uiResult")]
            public bool interfaceResultado;
            [UnityEngine.Serialization.FormerlySerializedAs("lastSelect")]
            public bool ultimoSelecionar;
            [UnityEngine.Serialization.FormerlySerializedAs("ultimoRolagem")]
            [UnityEngine.Serialization.FormerlySerializedAs("lastScroll")]
            public Vector2 ultimaRolagem;
            public bool isInitialized => true;
            public Transform castOrigin
            {
                get; set;
            }

            public Transform effectiveCastOrigin => castOrigin;
            public Unity.Collections.NativeArray<Vector3> samplePoints => default;
            public Vector3 lastSamplePoint => Vector3.zero;
            public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> alvos)
            {
                chamadas++;
                alvos?.Clear();
                return false;
            }

            public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> colisores, List<RaycastHit> acertosRaio)
            {
                chamadas++;
                colisores?.Clear();
                acertosRaio?.Clear();
                return false;
            }

            public bool UpdateUIModel(ref TrackedDeviceModel modeloInterface, bool selecaoAtiva, in Vector2 deslocamentoRolagem)
            {
                interfaceChamadas++;
                ultimoSelecionar = selecaoAtiva;
                ultimaRolagem = deslocamentoRolagem;
                modeloInterface.select = selecaoAtiva;
                modeloInterface.scrollDelta = deslocamentoRolagem;
                return interfaceResultado;
            }
        }

        private sealed class FiltroRegressaoDesativado : XRBaseTargetFilter
        {
            public override bool canProcess => false;
            public override void Process(IXRInteractor interator, List<IXRInteractable> alvos, List<IXRInteractable> results)
            {
                results.Clear();
            }
        }
    }
}
