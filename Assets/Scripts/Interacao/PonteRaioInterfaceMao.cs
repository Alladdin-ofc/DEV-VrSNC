using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRSNC.Interface;

namespace VRSNC.Interacao
{
    /// <summary>
    /// Dá a uma mão rastreada um raio de UI XRI nativo independente. O NearFarInteractor da mão
    /// continua sendo o único interactor de anatomia próxima/distante e captura do painel.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRRayInteractor))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainHandUiRayBridge")]
    public sealed class PonteRaioInterfaceMao : MonoBehaviour
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("handNearFar")]
        private NearFarInteractor maoProximoDistante;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("uiRay")]
        private XRRayInteractor interfaceRaio;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("uiLineVisual")]
        private XRInteractorLineVisual interfaceLinhaVisual;
        [UnityEngine.Serialization.FormerlySerializedAs("maoraiz")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("handRoot")]
        private Transform raizMao;
        private XRPokeInteractor[] toqueInteratores;
        private bool interfaceProximoDistanteHabilitadaAnteriormente;
        private bool interfaceProximoDistanteHabilitadaCapturada;
        private CurveVisualController[] proximoDistanteCurvaVisuais;
        private bool[] estadosVisuaisCurvaSalvos;
        private bool interfaceVisualAtivo;
        private static readonly Dictionary<NearFarInteractor, PonteRaioInterfaceMao> Rotas = new Dictionary<NearFarInteractor, PonteRaioInterfaceMao>();
        public NearFarInteractor InteratorMaoProximoDistante => maoProximoDistante;
        public XRRayInteractor InterfaceRaio => interfaceRaio;
        /// <summary>Verdadeiro apenas para um alvo UGUI real, nunca para decoração transparente do painel.</summary>
        public bool PossuiInterfaceAlvo
        {
            get
            {
                if (interfaceRaio == null || !interfaceRaio.isActiveAndEnabled || !interfaceRaio.TryGetUIModel(out var modelo) || !modelo.currentRaycast.isValid)
                    return false;
                var alvo = modelo.currentRaycast.gameObject;
                var canvas = alvo != null ? alvo.GetComponentInParent<Canvas>() : null;
                if (canvas == null || !canvas.isActiveAndEnabled || !canvas.gameObject.activeInHierarchy)
                    return false;
                var selecionavel = alvo.GetComponentInParent<Selectable>();
                if (selecionavel != null)
                    return selecionavel.IsInteractable();
                var rolagemRetangulo = alvo.GetComponentInParent<ScrollRect>();
                return rolagemRetangulo != null && rolagemRetangulo.isActiveAndEnabled;
            }
        }

        public static bool RaioEstaAlvejandoInterface(NearFarInteractor fonte)
        {
            return fonte != null && Rotas.TryGetValue(fonte, out var rota) && rota != null && rota.PossuiInterfaceAlvo;
        }

        public void Configurar(NearFarInteractor fonte, XRRayInteractor raio, XRInteractorLineVisual linhaVisual, Transform raiz)
        {
            maoProximoDistante = fonte;
            interfaceRaio = raio;
            interfaceLinhaVisual = linhaVisual;
            raizMao = raiz;
            ArmazenarInteratoresToque();
            if (isActiveAndEnabled)
                RegistrarRota();
        }

        private void Awake()
        {
            interfaceRaio ??= GetComponent<XRRayInteractor>();
            ArmazenarInteratoresToque();
            ArmazenarVisuaisCurvaProximoDistante();
            RegistrarRota();
        }

        private void OnEnable()
        {
            // Inicia inerte mesmo se uma edição no Inspector reativou o raio. Isso evita
            // um segundo ponteiro por um frame enquanto o rig nativo de mão é ativado.
            if (interfaceRaio != null)
                interfaceRaio.enabled = false;
            if (interfaceLinhaVisual != null)
                interfaceLinhaVisual.enabled = false;
            DefinirInterfaceVisualAtivo(false);
            if (maoProximoDistante != null)
            {
                RegistrarRota();
                interfaceProximoDistanteHabilitadaAnteriormente = maoProximoDistante.enableUIInteraction;
                interfaceProximoDistanteHabilitadaCapturada = true;
                // Exatamente um ponteiro de UI consome o pinch desta mão. NearFar mantém
                // todos os casts sem UI para anatomia e alças do painel espacial continuarem nativas.
                maoProximoDistante.enableUIInteraction = false;
            }
        }

        private void OnDisable()
        {
            DefinirInterfaceVisualAtivo(false);
            if (interfaceRaio != null)
                interfaceRaio.enabled = false;
            if (interfaceLinhaVisual != null)
                interfaceLinhaVisual.enabled = false;
            if (maoProximoDistante != null && Rotas.TryGetValue(maoProximoDistante, out var rota) && rota == this)
                Rotas.Remove(maoProximoDistante);
            if (maoProximoDistante != null && interfaceProximoDistanteHabilitadaCapturada)
                maoProximoDistante.enableUIInteraction = interfaceProximoDistanteHabilitadaAnteriormente;
            interfaceProximoDistanteHabilitadaCapturada = false;
        }

        private void RegistrarRota()
        {
            if (maoProximoDistante != null)
                Rotas[maoProximoDistante] = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RedefinirRotas() => Rotas.Clear();
        private void LateUpdate()
        {
            if (interfaceRaio == null || maoProximoDistante == null)
                return;
            // Esta barreira local preserva a prioridade nativa de poke e impede que um pinch
            // retido de anatomia/painel também ative um botão.
            var permitirInterfaceRaio = maoProximoDistante.isActiveAndEnabled && raizMao != null && raizMao.gameObject.activeInHierarchy && !maoProximoDistante.hasSelection && !ControlePainelMundo.AgarrePainelAtivoParaMao(raizMao) && !PossuiAtivoToqueInterfaceAlvo();
            if (interfaceRaio.enabled != permitirInterfaceRaio)
                interfaceRaio.enabled = permitirInterfaceRaio;
            DefinirInterfaceVisualAtivo(permitirInterfaceRaio && PossuiInterfaceAlvo);
        }

        private void ArmazenarInteratoresToque()
        {
            if (raizMao != null)
                toqueInteratores = raizMao.GetComponentsInChildren<XRPokeInteractor>(true);
        }

        private void ArmazenarVisuaisCurvaProximoDistante()
        {
            if (raizMao == null)
                return;
            proximoDistanteCurvaVisuais = raizMao.GetComponentsInChildren<CurveVisualController>(true);
            estadosVisuaisCurvaSalvos = new bool[proximoDistanteCurvaVisuais.Length];
        }

        private void DefinirInterfaceVisualAtivo(bool ativo)
        {
            if (interfaceVisualAtivo == ativo)
                return;
            interfaceVisualAtivo = ativo;
            if (interfaceLinhaVisual != null)
                interfaceLinhaVisual.enabled = ativo;
            if (proximoDistanteCurvaVisuais == null)
                ArmazenarVisuaisCurvaProximoDistante();
            if (proximoDistanteCurvaVisuais == null)
                return;
            for (var indice = 0; indice < proximoDistanteCurvaVisuais.Length; indice++)
            {
                var visual = proximoDistanteCurvaVisuais[indice];
                if (visual == null)
                    continue;
                if (ativo)
                {
                    estadosVisuaisCurvaSalvos[indice] = visual.enabled;
                    visual.enabled = false;
                }
                else
                {
                    visual.enabled = estadosVisuaisCurvaSalvos[indice];
                }
            }
        }

        private bool PossuiAtivoToqueInterfaceAlvo()
        {
            if (toqueInteratores == null)
                ArmazenarInteratoresToque();
            if (toqueInteratores == null)
                return false;
            for (var indice = 0; indice < toqueInteratores.Length; indice++)
            {
                var toque = toqueInteratores[indice];
                if (toque != null && toque.isActiveAndEnabled && toque.TryGetUIModel(out var modelo) && modelo.currentRaycast.isValid)
                    return true;
            }

            return false;
        }
    }
}