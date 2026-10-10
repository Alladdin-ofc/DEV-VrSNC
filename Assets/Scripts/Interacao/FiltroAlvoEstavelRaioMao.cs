using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRSNC.Interface;

namespace VRSNC.Interacao
{
    /// <summary>
    /// Decora o filtro de alvo existente e aplica histerese apenas depois que o decorador de
    /// caster Far identificou a consulta atual. Alvos Near e um filtro existente
    /// mantêm seu comportamento original.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VR SNC/Mãos/Filtro estável de alvo do raio da mão")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainHandRayStableTargetFilter")]
    public sealed class FiltroAlvoEstavelRaioMao : XRBaseTargetFilter
    {
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("owner")]
        private NearFarInteractor proprietario;
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("wrappedFilter")]
        private XRBaseTargetFilter encapsuladoFiltro;
        [SerializeField, Min(0f)]
        [UnityEngine.Serialization.FormerlySerializedAs("candidateSwitchTolerance")]
        private float opcaoTrocaTolerancia = 0.015f;
        [SerializeField, Range(0.06f, 0.10f)]
        [UnityEngine.Serialization.FormerlySerializedAs("candidateSwitchDwellSeconds")]
        private float opcaoTrocaEsperaSegundos = 0.08f;
        private readonly Dictionary<IXRInteractor, HistereseCandidatoSelecao> estados = new Dictionary<IXRInteractor, HistereseCandidatoSelecao>();
        private readonly List<IXRInteractable> encapsuladoResultados = new List<IXRInteractable>();
        [UnityEngine.Serialization.FormerlySerializedAs("anteriorOrdemProximoAlvosAposAlvoFiltro")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("previousSortNearTargetsAfterTargetFilter")]
        private bool ordemAnteriorFiltroAlvosProximos;
        public void Configurar(NearFarInteractor proximoDistante, IXRTargetFilter anteriorFiltro)
        {
            proprietario = proximoDistante;
            encapsuladoFiltro = anteriorFiltro as XRBaseTargetFilter;
            ordemAnteriorFiltroAlvosProximos = proximoDistante != null && proximoDistante.sortNearTargetsAfterTargetFilter;
        }

        public override void Link(IXRInteractor interator)
        {
            encapsuladoFiltro?.Link(interator);
            ObterEstado(interator).Redefinir();
        }

        public override void Unlink(IXRInteractor interator)
        {
            encapsuladoFiltro?.Unlink(interator);
            if (interator != null)
                estados.Remove(interator);
        }

        public override void Process(IXRInteractor interator, List<IXRInteractable> alvos, List<IXRInteractable> results)
        {
            results.Clear();
            encapsuladoResultados.Clear();
            AplicarEncapsulado(interator, alvos, encapsuladoResultados);
            if (!ContextoEmissorRaioMao.TentarObter(interator, out var fonte, out var raioColisores, out var raioAcertos) || fonte != ContextoEmissorRaioMao.Origem.Distante)
            {
                // O caster Near pode retornar colliders brutos que o gerenciador de
                // interação rejeita. Somente a lista de alvos válidos após o filtro pode
                // redefinir a histerese Far.
                if (fonte == ContextoEmissorRaioMao.Origem.Proximo && encapsuladoResultados.Count > 0)
                {
                    ObterEstado(interator).Redefinir();
                }

                CopiarTodos(encapsuladoResultados, results);
                return;
            }

            var maisProximoAnatomia = float.PositiveInfinity;
            var maisProximoInterface = float.PositiveInfinity;
            var possuiAnatomia = false;
            var possuiInterface = false;
            for (var indice = 0; indice < encapsuladoResultados.Count; indice++)
            {
                var alvo = encapsuladoResultados[indice];
                var ehAnatomia = alvo is ElementoInteragivelMao;
                if (ehAnatomia)
                    possuiAnatomia = true;
                else
                    possuiInterface = true;
                if (ContextoEmissorRaioMao.TentarObterDistanciaAtual(alvo, raioColisores, raioAcertos, out var distancia))
                {
                    if (ehAnatomia)
                        maisProximoAnatomia = Mathf.Min(maisProximoAnatomia, distancia);
                    else
                        maisProximoInterface = Mathf.Min(maisProximoInterface, distancia);
                }
            }

            var interfaceControlaRaio = ZonaProximidadePainelInterface.InteratorEstaProximoDoPainel(interator) || (possuiAnatomia && possuiInterface && maisProximoInterface <= maisProximoAnatomia + 0.001f);
            var estado = ObterEstado(interator);
            if (interfaceControlaRaio)
            {
                estado.Redefinir();
                for (var indice2 = 0; indice2 < encapsuladoResultados.Count; indice2++)
                {
                    if (!(encapsuladoResultados[indice2] is ElementoInteragivelMao))
                        results.Add(encapsuladoResultados[indice2]);
                }

                return;
            }

            if (!possuiAnatomia)
            {
                estado.Redefinir();
                CopiarTodos(encapsuladoResultados, results);
                return;
            }

            IXRInteractable melhor = null;
            var melhorDistancia = float.PositiveInfinity;
            for (var indice3 = 0; indice3 < encapsuladoResultados.Count; indice3++)
            {
                var alvo2 = encapsuladoResultados[indice3];
                if (!(alvo2 is ElementoInteragivelMao))
                    continue;
                if (melhor == null)
                    melhor = alvo2;
                if (ContextoEmissorRaioMao.TentarObterDistanciaAtual(alvo2, raioColisores, raioAcertos, out var distancia2) && distancia2 < melhorDistancia)
                {
                    melhor = alvo2;
                    melhorDistancia = distancia2;
                }
            }

            var candidatoEstavelPresente = Contem(encapsuladoResultados, estado.CandidatoEstavel);
            var distanciaEstavel = float.PositiveInfinity;
            if (candidatoEstavelPresente)
            {
                ContextoEmissorRaioMao.TentarObterDistanciaAtual(estado.CandidatoEstavel as IXRInteractable, raioColisores, raioAcertos, out distanciaEstavel);
            }

            var estavel = estado.Avaliar(melhor, melhorDistancia, candidatoEstavelPresente, distanciaEstavel, Time.unscaledTime) as IXRInteractable;
            if (estavel != null && Contem(encapsuladoResultados, estavel))
                results.Add(estavel);
        }

        private void AplicarEncapsulado(IXRInteractor interator, List<IXRInteractable> alvos, List<IXRInteractable> resultados)
        {
            if (encapsuladoFiltro != null && encapsuladoFiltro != this && encapsuladoFiltro.canProcess)
            {
                encapsuladoFiltro.Process(interator, alvos, resultados);
            }
            else
            {
                CopiarTodos(alvos, resultados);
            }
        }

        private HistereseCandidatoSelecao ObterEstado(IXRInteractor interator)
        {
            if (!estados.TryGetValue(interator, out var estado))
            {
                estado = new HistereseCandidatoSelecao(opcaoTrocaTolerancia, opcaoTrocaEsperaSegundos);
                estados.Add(interator, estado);
            }

            return estado;
        }

        private static bool Contem(List<IXRInteractable> alvos, object candidato)
        {
            if (candidato == null)
                return false;
            for (var indice = 0; indice < alvos.Count; indice++)
            {
                if (ReferenceEquals(alvos[indice], candidato))
                    return true;
            }

            return false;
        }

        private static void CopiarTodos(List<IXRInteractable> alvos, List<IXRInteractable> resultados)
        {
            for (var indice = 0; indice < alvos.Count; indice++)
                resultados.Add(alvos[indice]);
        }

        private void OnEnable()
        {
            if (proprietario != null)
            {
                if (!ReferenceEquals(proprietario.startingTargetFilter, this))
                    proprietario.startingTargetFilter = this;
                if (!ReferenceEquals(proprietario.targetFilter, this))
                    proprietario.targetFilter = this;
                if (encapsuladoFiltro == null)
                    proprietario.sortNearTargetsAfterTargetFilter = true;
            }
        }

        private void OnDisable()
        {
            if (proprietario != null && ReferenceEquals(proprietario.targetFilter, this))
                proprietario.targetFilter = encapsuladoFiltro;
            if (proprietario != null && ReferenceEquals(proprietario.startingTargetFilter, this))
                proprietario.startingTargetFilter = encapsuladoFiltro;
            if (proprietario != null)
                proprietario.sortNearTargetsAfterTargetFilter = ordemAnteriorFiltroAlvosProximos;
            estados.Clear();
        }
    }
}