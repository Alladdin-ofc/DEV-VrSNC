using UnityEngine;

namespace VRSNC.Interacao
{
    /// <summary>
    /// Histerese de candidato sem alocação compartilhada pelos proprietários de raio. O chamador informa
    /// se o candidato estável atual está presente no conjunto de acertos visíveis deste frame.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainCandidateHysteresis")]
    public sealed class HistereseCandidatoSelecao
    {
        private readonly float tolerancia;
        private readonly float esperaSegundos;
        private object candidatoEstavel;
        private object candidatoPendente;
        private float distanciaPendente = float.PositiveInfinity;
        private float pendenteDesde = -1f;
        public HistereseCandidatoSelecao(float espacialTolerancia, float espera)
        {
            tolerancia = Mathf.Max(0f, espacialTolerancia);
            esperaSegundos = Mathf.Max(0f, espera);
        }

        public object CandidatoEstavel => candidatoEstavel;
        public void Redefinir()
        {
            candidatoEstavel = null;
            candidatoPendente = null;
            distanciaPendente = float.PositiveInfinity;
            pendenteDesde = -1f;
        }

        public object Avaliar(object melhorCandidato, float melhorDistancia, bool candidatoEstavelPresente, float distanciaEstavelAtual, float agora)
        {
            if (melhorCandidato == null)
            {
                Redefinir();
                return null;
            }

            if (candidatoEstavel == null)
            {
                Confirmar(melhorCandidato, melhorDistancia);
            }
            else if (ReferenceEquals(melhorCandidato, candidatoEstavel))
            {
                LimparPendente();
            }
            else if (candidatoEstavelPresente && melhorDistancia + tolerancia < distanciaEstavelAtual)
            {
                // Um candidato visível claramente melhor pode trocar imediatamente.
                Confirmar(melhorCandidato, melhorDistancia);
            }
            else
            {
                // Se o candidato antigo está ausente, uma distância histórica não pode retê-lo
                // para sempre. A substituição ainda recebe a mesma breve permanência.
                RastrearPendente(melhorCandidato, melhorDistancia, agora);
            }

            return candidatoEstavel;
        }

        private void RastrearPendente(object candidato, float distancia, float agora)
        {
            if (!ReferenceEquals(candidato, candidatoPendente))
            {
                candidatoPendente = candidato;
                distanciaPendente = distancia;
                pendenteDesde = agora;
            }
            else if (agora - pendenteDesde >= esperaSegundos)
            {
                Confirmar(candidato, distanciaPendente);
            }
            else
            {
                distanciaPendente = distancia;
            }
        }

        private void Confirmar(object candidato, float distancia)
        {
            candidatoEstavel = candidato;
            LimparPendente();
        }

        private void LimparPendente()
        {
            candidatoPendente = null;
            distanciaPendente = float.PositiveInfinity;
            pendenteDesde = -1f;
        }
    }
}