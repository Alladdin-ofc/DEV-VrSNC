using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRSNC.Interacao
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainFarInteractionCasterAdapter")]
    public sealed class AdaptadorEmissorInteracaoDistante : MonoBehaviour, ICurveInteractionCaster, IUIModelUpdater
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("owner")]
        private NearFarInteractor proprietario;
        [UnityEngine.Serialization.FormerlySerializedAs("originalobjeto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalObject")]
        private Object objetoOriginal;
        private ICurveInteractionCaster Original => objetoOriginal as ICurveInteractionCaster;
        private IUIModelUpdater AtualizadorInterfaceOriginal => objetoOriginal is IUIModelUpdater atualizador && !ReferenceEquals(atualizador, this) ? atualizador : null;
        public void Configurar(NearFarInteractor proximoDistante, ICurveInteractionCaster original)
        {
            proprietario = proximoDistante;
            objetoOriginal = original as Object;
        }

        public bool isInitialized => Original != null && Original.isInitialized;
        public Transform castOrigin
        {
            get => Original != null ? Original.castOrigin : null;
            set
            {
                if (Original != null)
                    Original.castOrigin = value;
            }
        }

        // Nomes exigidos pelos contratos públicos do XR Interaction Toolkit.
        public Transform effectiveCastOrigin => Original != null ? Original.effectiveCastOrigin : null;
        public NativeArray<Vector3> samplePoints => Original != null ? Original.samplePoints : default;
        public Vector3 lastSamplePoint => Original != null ? Original.lastSamplePoint : Vector3.zero;
        public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> alvos)
        {
            var resultado = Original != null && Original.TryGetColliderTargets(gerenciadorInteracao, alvos);
            ContextoEmissorRaioMao.MarcarInteracaoDistante(proprietario, alvos, null);
            return resultado;
        }

        public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> colisores, List<RaycastHit> acertosRaio)
        {
            var resultado = Original != null && Original.TryGetColliderTargets(gerenciadorInteracao, colisores, acertosRaio);
            ContextoEmissorRaioMao.MarcarInteracaoDistante(proprietario, colisores, acertosRaio);
            return resultado;
        }

        // NearFarInteractor obtém seu modelo de UI XR pelo caster distante. Preserva esta
        // capacidade ao decorar CurveInteractionCaster para o contexto de raio do SNC.
        public bool UpdateUIModel(ref TrackedDeviceModel modeloInterface, bool selecaoAtiva, in Vector2 deslocamentoRolagem)
        {
            var atualizador = AtualizadorInterfaceOriginal;
            return atualizador != null && atualizador.UpdateUIModel(ref modeloInterface, selecaoAtiva, deslocamentoRolagem);
        }

        private void OnEnable()
        {
            if (proprietario != null && !ReferenceEquals(proprietario.farInteractionCaster, this))
                proprietario.farInteractionCaster = this;
        }

        private void OnDisable()
        {
            if (proprietario != null && ReferenceEquals(proprietario.farInteractionCaster, this))
                proprietario.farInteractionCaster = Original;
            ContextoEmissorRaioMao.Limpar(proprietario);
        }
    }
}
