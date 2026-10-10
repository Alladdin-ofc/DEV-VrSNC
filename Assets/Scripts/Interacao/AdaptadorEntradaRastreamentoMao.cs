using UnityEngine;
using UnityEngine.InputSystem;

namespace VRSNC.Interacao
{
    // Nome funcional em português: Adaptador de Entrada por Rastreamento de Mãos
    // Nome técnico do script: AdaptadorEntradaRastreamentoMao
    // Função: encaminha gestos de mão, como pinça e grab, para o roteador XR do SNC.
    // Relação com o protótipo: mantém suporte a hand tracking sem alterar a lógica de seleção por raycast/trigger.
    // Objetos da cena: usado no Sistema_Nervoso_Central quando ações de mão estão configuradas.
    /// <summary>
    /// Adapta eventos de rastreamento de mãos para chamadas do BrainXRInputRouter.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainHandTrackingInputAdapter")]
    public sealed class AdaptadorEntradaRastreamentoMao : MonoBehaviour
    {
        // Roteador que recebe comandos de seleção, reset e grab.
        [UnityEngine.Serialization.FormerlySerializedAs("entradaroteador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("inputRouter")]
        private RoteadorEntradaXR roteadorEntrada;
        // Ação de pinça usada para selecionar a estrutura apontada.
        [UnityEngine.Serialization.FormerlySerializedAs("pincaSelecionarAcao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("pinchSelectAction")]
        private InputActionReference acaoSelecionarPorPinca;
        /// <summary>
        /// Resolve o roteador local quando necessário.
        /// </summary>
        private void Awake()
        {
            if (roteadorEntrada == null)
            {
                roteadorEntrada = GetComponent<RoteadorEntradaXR>();
            }
        }

        /// <summary>
        /// Habilita a ação de pinça e registra callback.
        /// </summary>
        private void OnEnable()
        {
            if (acaoSelecionarPorPinca != null && acaoSelecionarPorPinca.action != null)
            {
                acaoSelecionarPorPinca.action.Enable();
                acaoSelecionarPorPinca.action.performed += EmPincaExecutado;
            }
        }

        /// <summary>
        /// Remove o callback da pinça ao desabilitar o objeto.
        /// </summary>
        private void OnDisable()
        {
            if (acaoSelecionarPorPinca != null && acaoSelecionarPorPinca.action != null)
            {
                acaoSelecionarPorPinca.action.performed -= EmPincaExecutado;
            }
        }

        /// <summary>
        /// Seleciona a estrutura atualmente apontada por gesto de pinça.
        /// </summary>
        public void SelecionarPorPinca()
        {
            roteadorEntrada?.SelecionarPorPinca();
        }

        /// <summary>
        /// Aciona reset por gesto de palma quando configurado.
        /// </summary>
        public void RestaurarPorGestoDaPalma()
        {
            roteadorEntrada?.RestaurarPorGestoDaPalma();
        }

        /// <summary>
        /// Inicia grab por mão rastreada.
        /// </summary>
        public void IniciarAgarrePorMao(Transform mao)
        {
            roteadorEntrada?.IniciarAgarrePorMao(mao);
        }

        /// <summary>
        /// Finaliza grab por mão rastreada.
        /// </summary>
        public void EncerrarAgarrePorMao()
        {
            roteadorEntrada?.EncerrarAgarrePorMao();
        }

        /// <summary>
        /// Callback interno da InputAction de pinça.
        /// </summary>
        private void EmPincaExecutado(InputAction.CallbackContext _)
        {
            SelecionarPorPinca();
        }
    }
}