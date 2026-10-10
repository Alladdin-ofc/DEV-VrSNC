using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

namespace VRSNC.Interacao
{
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainNearInteractionCasterAdapter")]
    public sealed class AdaptadorEmissorInteracaoProxima : MonoBehaviour, IInteractionCaster
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("owner")]
        private NearFarInteractor proprietario;
        [UnityEngine.Serialization.FormerlySerializedAs("originalobjeto")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("originalObject")]
        private Object objetoOriginal;
        private IInteractionCaster Original => objetoOriginal as IInteractionCaster;
        public void Configurar(NearFarInteractor proximoDistante, IInteractionCaster original)
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

        public Transform effectiveCastOrigin => Original != null ? Original.effectiveCastOrigin : null;
        public bool TryGetColliderTargets(XRInteractionManager gerenciadorInteracao, List<Collider> alvos)
        {
            var resultado = Original != null && Original.TryGetColliderTargets(gerenciadorInteracao, alvos);
            ContextoEmissorRaioMao.MarcarInteracaoProxima(proprietario, alvos != null && alvos.Count > 0);
            return resultado;
        }

        private void OnEnable()
        {
            if (proprietario != null && !ReferenceEquals(proprietario.nearInteractionCaster, this))
                proprietario.nearInteractionCaster = this;
        }

        private void OnDisable()
        {
            if (proprietario != null && ReferenceEquals(proprietario.nearInteractionCaster, this))
                proprietario.nearInteractionCaster = Original;
            ContextoEmissorRaioMao.Limpar(proprietario);
        }
    }
}
