using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRSNC.Interface
{
    /// <summary>
    /// Adiciona seleção por gatilho do controle somente a uma affordance em foco do painel principal. O interactor Near/Far
    /// ainda recebe seu leitor Grip original para cada outro interactable, inclusive anatomia.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.UI", "Assembly-CSharp", "MainSpatialWindowControllerSelectRelay")]
    public sealed class IntermediarioSelecaoControladorJanelaPrincipal : MonoBehaviour, IXRInputButtonReader
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("interactor")]
        private NearFarInteractor interator;
        [UnityEngine.Serialization.FormerlySerializedAs("originalselecionar")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalSelect")]
        private XRInputButtonReader selecaoOriginal;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("triggerPress")]
        private XRInputButtonReader gatilhoPressionar;
        [UnityEngine.Serialization.FormerlySerializedAs("instaladoSelecionar")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("installedSelect")]
        private XRInputButtonReader selecaoInstalada;
        private readonly HashSet<Object> focoFontes = new HashSet<Object>();
        private bool gatilhoRetido;
        private bool executado;
        private bool executadoEsteQuadro;
        private bool concluidoEsteQuadro;
        private int atualizadoQuadro = -1;
        public bool EstaInstaladoPara(NearFarInteractor candidato) => candidato != null && candidato == interator && selecaoInstalada != null && ReferenceEquals(candidato.selectInput.GetObjectReference(), this);
        /// <summary>Substitui de modo idempotente apenas este leitor Near/Far do controle por um invólucro ObjectReference.</summary>
        public void Configurar(NearFarInteractor alvo)
        {
            if (alvo == null)
                return;
            if (EstaInstaladoPara(alvo))
                return;
            interator = alvo;
            selecaoOriginal = alvo.selectInput;
            gatilhoPressionar = alvo.uiPressInput;
            selecaoInstalada = new XRInputButtonReader("Main Spatial Window Trigger Select", inputSourceMode: XRInputButtonReader.InputSourceMode.ObjectReference);
            selecaoInstalada.SetObjectReference(this);
            alvo.selectInput = selecaoInstalada;
            RedefinirEstado();
        }

        /// <summary>Chamado somente pela alça inferior e pelas bordas de redimensionamento deste interactor Near/Far específico.</summary>
        public void DefinirFocoNoPainel(Object fonte, bool emFoco)
        {
            if (fonte == null)
                return;
            if (emFoco)
                focoFontes.Add(fonte);
            else
                focoFontes.Remove(fonte);
            // Uma liberação deve ser observada imediatamente mesmo que o foco termine antes de o XRI avaliar a seleção.
            if (!LerGatilho())
                gatilhoRetido = false;
        }

        private void OnDisable() => RedefinirEstado();
        public bool ReadIsPerformed()
        {
            Atualizar();
            return executado;
        }

        public bool ReadWasPerformedThisFrame()
        {
            Atualizar();
            return executadoEsteQuadro;
        }

        public bool ReadWasCompletedThisFrame()
        {
            Atualizar();
            return concluidoEsteQuadro;
        }

        public float ReadValue()
        {
            Atualizar();
            return executado ? 1f : 0f;
        }

        public bool TryReadValue(out float valor)
        {
            Atualizar();
            valor = executado ? 1f : 0f;
            return executado;
        }

        private void Atualizar()
        {
            if (atualizadoQuadro == Time.frameCount)
                return;
            atualizadoQuadro = Time.frameCount;
            var anteriormenteExecutado = executado;
            var gatilhoSegurado = LerGatilho();
            if (!gatilhoSegurado)
                gatilhoRetido = false;
            else if (!gatilhoRetido && focoFontes.Count > 0)
                gatilhoRetido = true;
            executado = (selecaoOriginal != null && selecaoOriginal.ReadIsPerformed()) || gatilhoRetido;
            executadoEsteQuadro = !anteriormenteExecutado && executado;
            concluidoEsteQuadro = anteriormenteExecutado && !executado;
        }

        private bool LerGatilho() => gatilhoPressionar != null && gatilhoPressionar.ReadIsPerformed();
        private void RedefinirEstado()
        {
            focoFontes.Clear();
            gatilhoRetido = false;
            executado = false;
            executadoEsteQuadro = false;
            concluidoEsteQuadro = false;
            atualizadoQuadro = -1;
        }
    }
}