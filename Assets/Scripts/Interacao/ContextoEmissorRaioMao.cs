using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRSNC.Interacao
{
    /// <summary>Informações de origem gravadas por um decorador de caster antes de o XRI invocar o filtro de alvo.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainHandRayCasterContext")]
    public static class ContextoEmissorRaioMao
    {
        public enum Origem
        {
            Nenhum,
            Proximo,
            Distante
        }

        [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "State")]
        private sealed class Estado
        {
            [UnityEngine.Serialization.FormerlySerializedAs("source")]
            public Origem fonte;
            [UnityEngine.Serialization.FormerlySerializedAs("nearHadTargets")]
            public bool proximoTinhaAlvos;
            [UnityEngine.Serialization.FormerlySerializedAs("colliders")]
            public List<Collider> colisores;
            [UnityEngine.Serialization.FormerlySerializedAs("raycastHits")]
            public List<RaycastHit> raioAcertos;
        }

        private static readonly Dictionary<NearFarInteractor, Estado> estados = new Dictionary<NearFarInteractor, Estado>();
        public static void MarcarInteracaoProxima(NearFarInteractor responsavel, bool tinhaAlvos)
        {
            if (responsavel == null)
                return;
            var estado = ObterEstado(responsavel);
            estado.fonte = Origem.Proximo;
            estado.proximoTinhaAlvos = tinhaAlvos;
            estado.colisores = null;
            estado.raioAcertos = null;
        }

        public static void MarcarInteracaoDistante(NearFarInteractor responsavel, List<Collider> colisores, List<RaycastHit> raioAcertos)
        {
            if (responsavel == null)
                return;
            var estado = ObterEstado(responsavel);
            estado.fonte = Origem.Distante;
            estado.proximoTinhaAlvos = false;
            estado.colisores = colisores;
            estado.raioAcertos = raioAcertos;
        }

        public static bool TentarObter(IXRInteractor interator, out Origem fonte, out List<Collider> colisores, out List<RaycastHit> raioAcertos)
        {
            if (interator is NearFarInteractor responsavel && estados.TryGetValue(responsavel, out var estado))
            {
                fonte = estado.fonte;
                colisores = estado.colisores;
                raioAcertos = estado.raioAcertos;
                return true;
            }

            fonte = Origem.Nenhum;
            colisores = null;
            raioAcertos = null;
            return false;
        }

        public static bool InteracaoProximaPossuiaAlvos(IXRInteractor interator)
        {
            return interator is NearFarInteractor responsavel && estados.TryGetValue(responsavel, out var estado) && estado.fonte == Origem.Proximo && estado.proximoTinhaAlvos;
        }

        public static bool TentarObterDistanciaAtual(IXRInteractable interagivel, List<Collider> colisores, List<RaycastHit> raioAcertos, out float distancia)
        {
            distancia = float.PositiveInfinity;
            if (interagivel == null || colisores == null || raioAcertos == null)
                return false;
            var colisoresInteragivel = interagivel.colliders;
            for (var indice = 0; indice < raioAcertos.Count && indice < colisores.Count; indice++)
            {
                var colisor = colisores[indice];
                if (colisor == null || !colisoresInteragivel.Contains(colisor))
                    continue;
                if (raioAcertos[indice].distance < distancia)
                    distancia = raioAcertos[indice].distance;
            }

            return !float.IsPositiveInfinity(distancia);
        }

        public static void Limpar(NearFarInteractor responsavel)
        {
            if (responsavel != null)
                estados.Remove(responsavel);
        }

        private static Estado ObterEstado(NearFarInteractor responsavel)
        {
            if (!estados.TryGetValue(responsavel, out var estado))
            {
                estado = new Estado();
                estados.Add(responsavel, estado);
            }

            return estado;
        }
    }
}