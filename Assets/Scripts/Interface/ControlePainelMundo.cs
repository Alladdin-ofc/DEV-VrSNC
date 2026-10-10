using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRSNC.Interface
{
    /// <summary>
    /// Transforma a barra inferior do painel anatômico em uma alça espacial. O painel permanece fixado após uma captura;
    /// o comando explícito de recentrar é a única operação que o coloca à frente do usuário novamente.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.UI", "Assembly-CSharp", "BrainWorldPanelHandle")]
    public sealed class ControlePainelMundo : MonoBehaviour
    {
        public enum EstadoPosturaPainel
        {
            Acoplado,
            Agarrado,
            FixadoNoMundo
        }

        [UnityEngine.Serialization.FormerlySerializedAs("painelacompanhar")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("panelFollow")]
        private AcompanhamentoCameraPainelInterface acompanhamentoPainel;
        private static int quantidadeAgarresPainelAtivos;
        private static readonly HashSet<ControlePainelMundo> alcasAtivas = new HashSet<ControlePainelMundo>();
        private readonly HashSet<IXRSelectInteractor> selecionandoInteratores = new HashSet<IXRSelectInteractor>();
        private readonly HashSet<IXRSelectInteractor> agarrarSelecionandoInteratores = new HashSet<IXRSelectInteractor>();
        private readonly HashSet<IXRSelectInteractor> interatoresGestoExterno = new HashSet<IXRSelectInteractor>();
        private XRGrabInteractable agarrarInteragivel;
        private bool agarreDesabilitadoParaGestoExterno;
        private EstadoPosturaPainel estadoPostura = EstadoPosturaPainel.FixadoNoMundo;
        /// <summary>Verdadeiro enquanto qualquer alça configurada do painel de mundo possui uma ou mais seleções XRI.</summary>
        public static bool EhQualquerPainelAgarreAtivo => quantidadeAgarresPainelAtivos > 0;
        /// <summary>Expõe o objeto XRI configurado para diagnósticos de configuração exclusivos do Editor.</summary>
        public XRGrabInteractable InteragivelAgarre => agarrarInteragivel != null ? agarrarInteragivel : GetComponent<XRGrabInteractable>();
        /// <summary>Número de mãos atualmente retidas pelo ciclo de vida de seleção XRI deste painel.</summary>
        public int QuantidadeInteratoresSelecionando => selecionandoInteratores.Count;
        public static bool AgarrePainelAtivoParaMao(Transform maoRaiz)
        {
            if (maoRaiz == null)
                return false;
            foreach (var alca in alcasAtivas)
            {
                foreach (var interator in alca.selecionandoInteratores)
                {
                    if (interator != null && EhMesmaRaizMao(interator.transform, maoRaiz))
                        return true;
                }
            }

            return false;
        }

        public static bool AgarrePainelAtivoPara(IXRInteractor interator)
        {
            return interator != null && AgarrePainelAtivoParaMao(interator.transform);
        }

        private static bool EhMesmaRaizMao(Transform primeiro, Transform segundo)
        {
            return primeiro == segundo || primeiro.IsChildOf(segundo) || segundo.IsChildOf(primeiro);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RedefinirRegistroEstatico()
        {
            quantidadeAgarresPainelAtivos = 0;
            alcasAtivas.Clear();
        }

        public EstadoPosturaPainel EstadoPostura => estadoPostura;
        private void Awake()
        {
            agarrarInteragivel = GetComponent<XRGrabInteractable>();
            if (acompanhamentoPainel == null)
            {
                acompanhamentoPainel = GetComponent<AcompanhamentoCameraPainelInterface>();
            }
        }

        private void OnEnable()
        {
            alcasAtivas.Add(this);
            if (agarrarInteragivel == null)
            {
                agarrarInteragivel = GetComponent<XRGrabInteractable>();
            }

            if (agarrarInteragivel != null)
            {
                agarrarInteragivel.selectEntered.AddListener(EmAgarrarIniciado);
                agarrarInteragivel.selectExited.AddListener(EmAgarrarEncerrado);
            }
        }

        private void OnDisable()
        {
            alcasAtivas.Remove(this);
            if (agarrarInteragivel != null)
            {
                agarrarInteragivel.selectEntered.RemoveListener(EmAgarrarIniciado);
                agarrarInteragivel.selectExited.RemoveListener(EmAgarrarEncerrado);
            }

            quantidadeAgarresPainelAtivos = Mathf.Max(0, quantidadeAgarresPainelAtivos - selecionandoInteratores.Count);
            selecionandoInteratores.Clear();
            agarrarSelecionandoInteratores.Clear();
            interatoresGestoExterno.Clear();
            if (agarreDesabilitadoParaGestoExterno && agarrarInteragivel != null)
                agarrarInteragivel.enabled = true;
            agarreDesabilitadoParaGestoExterno = false;
        }

        /// <summary>Chamado pelo botão da barra inferior; reposiciona uma vez e retorna imediatamente ao modo fixado no mundo.</summary>
        public void RecentralizarPainel()
        {
            if (selecionandoInteratores.Count > 0)
                return;
            if (acompanhamentoPainel == null)
            {
                return;
            }

            acompanhamentoPainel.ReposicionarDianteDaCamera();
            acompanhamentoPainel.DefinirAcompanhamentoAtivo(false);
            estadoPostura = EstadoPosturaPainel.FixadoNoMundo;
        }

        /// <summary>Controle explícito de acompanhamento para a UI Seguir/Fixar do painel; nunca é chamado implicitamente ao soltar.</summary>
        public void DefinirAcoplamento(bool acoplado)
        {
            if (selecionandoInteratores.Count > 0)
                return;
            if (acompanhamentoPainel == null)
                return;
            acompanhamentoPainel.DefinirAcompanhamentoAtivo(acoplado);
            estadoPostura = acoplado ? EstadoPosturaPainel.Acoplado : EstadoPosturaPainel.FixadoNoMundo;
        }

        public void Configurar(AcompanhamentoCameraPainelInterface seguir)
        {
            acompanhamentoPainel = seguir;
            agarrarInteragivel = GetComponent<XRGrabInteractable>();
        }

        /// <summary>
        /// Retém a posse do painel para uma affordance sem captura, como uma borda de redimensionamento. Usa o mesmo
        /// registro de raiz da mão da alça física inferior, para a entrada anatômica não competir com esse gesto.
        /// </summary>
        public bool TentarIniciarGestoExterno(IXRSelectInteractor interator)
        {
            if (interator == null || selecionandoInteratores.Count != 0)
                return false;
            interatoresGestoExterno.Add(interator);
            ManterInterator(interator);
            if (agarrarInteragivel != null && agarrarInteragivel.enabled)
            {
                // Um redimensionamento possui o painel; impede uma segunda mão de selecionar a captura raiz enquanto está ativo.
                agarrarInteragivel.enabled = false;
                agarreDesabilitadoParaGestoExterno = true;
            }

            acompanhamentoPainel?.DefinirAcompanhamentoAtivo(false);
            estadoPostura = EstadoPosturaPainel.Agarrado;
            return true;
        }

        /// <summary>Libera de modo idempotente a posse retida por <see cref = "TryBeginExternalGesture"/>.</summary>
        public void EncerrarGestoExterno(IXRSelectInteractor interator)
        {
            if (interator == null || !interatoresGestoExterno.Remove(interator))
                return;
            SoltarInteratorSeSemUso(interator);
            if (interatoresGestoExterno.Count == 0 && agarreDesabilitadoParaGestoExterno && agarrarInteragivel != null)
            {
                agarrarInteragivel.enabled = true;
                agarreDesabilitadoParaGestoExterno = false;
            }

            if (selecionandoInteratores.Count == 0)
                estadoPostura = EstadoPosturaPainel.FixadoNoMundo;
        }

        private void EmAgarrarIniciado(SelectEnterEventArgs argumentos)
        {
            if (argumentos.interactorObject != null)
            {
                agarrarSelecionandoInteratores.Add(argumentos.interactorObject);
                ManterInterator(argumentos.interactorObject);
            }

            if (acompanhamentoPainel != null)
            {
                acompanhamentoPainel.DefinirAcompanhamentoAtivo(false);
            }

            estadoPostura = EstadoPosturaPainel.Agarrado;
        }

        private void EmAgarrarEncerrado(SelectExitEventArgs argumentos)
        {
            if (argumentos.interactorObject != null)
            {
                agarrarSelecionandoInteratores.Remove(argumentos.interactorObject);
                SoltarInteratorSeSemUso(argumentos.interactorObject);
            }

            if (selecionandoInteratores.Count == 0)
                estadoPostura = EstadoPosturaPainel.FixadoNoMundo;
        }

        private void ManterInterator(IXRSelectInteractor interator)
        {
            if (selecionandoInteratores.Add(interator))
                quantidadeAgarresPainelAtivos++;
        }

        private void SoltarInteratorSeSemUso(IXRSelectInteractor interator)
        {
            if ((agarrarSelecionandoInteratores.Contains(interator) || interatoresGestoExterno.Contains(interator)) || !selecionandoInteratores.Remove(interator))
                return;
            quantidadeAgarresPainelAtivos = Mathf.Max(0, quantidadeAgarresPainelAtivos - 1);
        }
    }
}