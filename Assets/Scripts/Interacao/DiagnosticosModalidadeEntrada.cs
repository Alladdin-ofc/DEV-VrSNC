using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace VRSNC.Interacao
{
    /// <summary>
    /// Diagnóstico independente opcional. Observa apenas o gerenciador XRI nativo e o Input
    /// System; nunca habilita ações, altera grupos ou controla estado de modalidade.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainInputModalityDiagnostics")]
    public sealed class DiagnosticosModalidadeEntrada : MonoBehaviour
    {
        private const float IntervaloInstantaneoSegundos = 1f;
        [UnityEngine.Serialization.FormerlySerializedAs("entradamodalidadegerenciador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("inputModalityManager")]
        private XRInputModalityManager gerenciadorModalidadeEntrada;
        private readonly StringBuilder construtor = new StringBuilder(512);
        private string ultimaAssinatura;
        private string motivoPendente;
        private float tempoProximoInstantaneo;
        private bool forcarInstantaneoPendente;
        private void Awake()
        {
            if (gerenciadorModalidadeEntrada == null)
                gerenciadorModalidadeEntrada = GetComponent<XRInputModalityManager>();
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += EmDispositivoAlterar;
            SolicitarInstantaneo("enabled", forcar: true);
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= EmDispositivoAlterar;
        }

        private void Update()
        {
            if (Time.unscaledTime < tempoProximoInstantaneo)
                return;
            tempoProximoInstantaneo = Time.unscaledTime + IntervaloInstantaneoSegundos;
            EmitirSeAlterado();
        }

        private void OnApplicationFocus(bool possuiFoco)
        {
            SolicitarInstantaneo(possuiFoco ? "focus gained" : "focus lost", forcar: true);
        }

        private void OnApplicationPause(bool pausado)
        {
            SolicitarInstantaneo(pausado ? "application paused" : "application resumed", forcar: true);
        }

        private void EmDispositivoAlterar(InputDevice dispositivo, InputDeviceChange alteracao)
        {
            if (UsaRastreadoMao(dispositivo, CommonUsages.LeftHand) || UsaRastreadoMao(dispositivo, CommonUsages.RightHand))
                SolicitarInstantaneo($"device {alteracao}", forcar: true);
        }

        /// <summary>Pode ser invocado por um menu do editor durante execução no headset.</summary>
        public void EmitirRelatorioAgora()
        {
            SolicitarInstantaneo("manual report", forcar: true);
        }

        private void SolicitarInstantaneo(string motivo, bool forcar)
        {
            motivoPendente = motivoPendente == null ? motivo : motivoPendente + ", " + motivo;
            forcarInstantaneoPendente |= forcar;
        }

        private void EmitirSeAlterado()
        {
            if (gerenciadorModalidadeEntrada == null)
            {
                if (motivoPendente != null)
                    Debug.LogWarning("[Modalidade de entrada] Falta XRInputModalityManager; os diagnósticos não podem observar a modalidade.", this);
                motivoPendente = null;
                forcarInstantaneoPendente = false;
                return;
            }

            construtor.Clear();
            construtor.Append("managerEnabled=").Append(gerenciadorModalidadeEntrada.enabled);
            AcrescentarGrupo(" L.hand", gerenciadorModalidadeEntrada.leftHand);
            AcrescentarGrupo(" L.controller", gerenciadorModalidadeEntrada.leftController);
            AcrescentarGrupo(" R.hand", gerenciadorModalidadeEntrada.rightHand);
            AcrescentarGrupo(" R.controller", gerenciadorModalidadeEntrada.rightController);
            AcrescentarDispositivos();
            var assinatura = construtor.ToString();
            var alterado = assinatura != ultimaAssinatura;
            if (alterado || forcarInstantaneoPendente)
            {
                Debug.Log("[Modalidade de entrada] " + (motivoPendente ?? "estado alterado") + "; " + assinatura, this);
                ultimaAssinatura = assinatura;
            }

            motivoPendente = null;
            forcarInstantaneoPendente = false;
        }

        private void AcrescentarGrupo(string rotulo, GameObject grupo)
        {
            construtor.Append(rotulo).Append('=');
            if (grupo == null)
            {
                construtor.Append("missing");
                return;
            }

            construtor.Append(grupo.activeSelf ? "self-on" : "self-off").Append('/').Append(grupo.activeInHierarchy ? "active" : "inactive");
        }

        private void AcrescentarDispositivos()
        {
            var encontrado = false;
            for (var indice = 0; indice < InputSystem.devices.Count; ++indice)
            {
                var dispositivo = InputSystem.devices[indice];
                var esquerda = UsaRastreadoMao(dispositivo, CommonUsages.LeftHand);
                var direito = UsaRastreadoMao(dispositivo, CommonUsages.RightHand);
                if (!esquerda && !direito)
                    continue;
                encontrado = true;
                var rastreado = dispositivo.TryGetChildControl<ButtonControl>("isTracked");
                var rastreamentoEstado = dispositivo.TryGetChildControl<IntegerControl>("trackingState");
                construtor.Append(" device[").Append(esquerda ? 'L' : 'R').Append(']').Append(" layout=").Append(dispositivo.layout).Append(" tracked=").Append(rastreado != null && rastreado.isPressed).Append(" trackingState=");
                if (rastreamentoEstado == null)
                    construtor.Append("n/a");
                else
                    construtor.Append(rastreamentoEstado.ReadValue());
            }

            if (!encontrado)
                construtor.Append(" devices=none");
        }

        private static bool UsaRastreadoMao(InputDevice dispositivo, InternedString maoUso)
        {
            return dispositivo != null && dispositivo.usages.Contains(maoUso);
        }
    }
}