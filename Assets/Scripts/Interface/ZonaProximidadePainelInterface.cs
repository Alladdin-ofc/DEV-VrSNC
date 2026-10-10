using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VRSNC.Interface
{
    /// <summary>
    /// Arbitragem de proximidade em espaço de mundo para a UI do painel. Usa deliberadamente a posição
    /// física do transform do interactor, não poses de raycast/anexação da UI, para um raio distante não suprimir a entrada anatômica.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.UI", "Assembly-CSharp", "BrainPanelUiProximityZone")]
    public sealed class ZonaProximidadePainelInterface : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        [UnityEngine.Serialization.FormerlySerializedAs("nearDistanceMeters")]
        private float proximoDistanciaMetros = 0.12f;
        private static readonly List<ZonaProximidadePainelInterface> zonasAtivas = new List<ZonaProximidadePainelInterface>(4);
        private RectTransform retanguloTransformacao;
        private CanvasGroup[] visibilidadeGrupos;
        private void Awake()
        {
            retanguloTransformacao = (RectTransform)transform;
            visibilidadeGrupos = GetComponentsInParent<CanvasGroup>(true);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RedefinirRegistro()
        {
            zonasAtivas.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RessincronizarRegistro()
        {
            zonasAtivas.Clear();
            foreach (var zona in FindObjectsByType<ZonaProximidadePainelInterface>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                zonasAtivas.Add(zona);
            }
        }

        private void OnEnable()
        {
            if (!zonasAtivas.Contains(this))
                zonasAtivas.Add(this);
        }

        private void OnDisable() => zonasAtivas.Remove(this);
        /// <summary>Retorna verdadeiro apenas quando a posição física do interactor está em uma zona ativa e visível do painel.</summary>
        public static bool InteratorEstaProximoDoPainel(IXRInteractor interator)
        {
            return interator != null && PontoEstaProximoDoPainel(interator.transform.position);
        }

        /// <summary>Verifica sem alocação a distância em metros no mundo até cada zona retangular ativa registrada.</summary>
        public static bool PontoEstaProximoDoPainel(Vector3 pontoMundo)
        {
            for (var indice = zonasAtivas.Count - 1; indice >= 0; indice--)
            {
                var zona = zonasAtivas[indice];
                if (zona == null)
                {
                    zonasAtivas.RemoveAt(indice);
                    continue;
                }

                if (zona.EstaVisivel() && zona.EstaProximo(pontoMundo))
                    return true;
            }

            return false;
        }

        /// <summary>Informa o retângulo ativo de UI mais próximo atingido por um raio de mundo, em metros desde a origem.</summary>
        public static bool TentarObterDistanciaDeBloqueioDoRaio(Ray raio, float distanciaMaxima, out float distancia)
        {
            distancia = float.PositiveInfinity;
            for (var indice = zonasAtivas.Count - 1; indice >= 0; indice--)
            {
                var zona = zonasAtivas[indice];
                if (zona == null)
                {
                    zonasAtivas.RemoveAt(indice);
                    continue;
                }

                if (zona.EstaVisivel() && zona.TentarObterDistanciaRaio(raio, distanciaMaxima, out var candidato))
                    distancia = Mathf.Min(distancia, candidato);
            }

            return !float.IsPositiveInfinity(distancia);
        }

        private bool EstaProximo(Vector3 pontoMundo)
        {
            if (retanguloTransformacao == null)
                retanguloTransformacao = (RectTransform)transform;
            var local = retanguloTransformacao.InverseTransformPoint(pontoMundo);
            var retangulo = retanguloTransformacao.rect;
            var maisProximo = new Vector3(Mathf.Clamp(local.x, retangulo.xMin, retangulo.xMax), Mathf.Clamp(local.y, retangulo.yMin, retangulo.yMax), 0f);
            return Vector3.Distance(pontoMundo, retanguloTransformacao.TransformPoint(maisProximo)) <= proximoDistanciaMetros;
        }

        private bool EstaVisivel()
        {
            if (!gameObject.activeInHierarchy)
            {
                return false;
            }

            if (visibilidadeGrupos == null)
            {
                visibilidadeGrupos = GetComponentsInParent<CanvasGroup>(true);
            }

            for (var indice = 0; indice < visibilidadeGrupos.Length; indice++)
            {
                var grupo = visibilidadeGrupos[indice];
                if (grupo != null && grupo.alpha <= 0.001f)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TentarObterDistanciaRaio(Ray raio, float distanciaMaxima, out float distancia)
        {
            distancia = 0f;
            if (retanguloTransformacao == null)
                retanguloTransformacao = (RectTransform)transform;
            var localOrigem = retanguloTransformacao.InverseTransformPoint(raio.origin);
            var localDirecao = retanguloTransformacao.InverseTransformVector(raio.direction);
            if (Mathf.Abs(localDirecao.z) < 0.00001f)
                return false;
            var t = -localOrigem.z / localDirecao.z;
            if (t < 0f)
                return false;
            var localAcerto = localOrigem + localDirecao * t;
            var retangulo = retanguloTransformacao.rect;
            if (localAcerto.x < retangulo.xMin || localAcerto.x > retangulo.xMax || localAcerto.y < retangulo.yMin || localAcerto.y > retangulo.yMax)
                return false;
            distancia = Vector3.Distance(raio.origin, retanguloTransformacao.TransformPoint(localAcerto));
            return distancia <= distanciaMaxima;
        }
    }
}