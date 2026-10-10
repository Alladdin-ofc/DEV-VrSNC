using UnityEngine;

namespace VRSNC.Interacao
{
    // Nome funcional em português: Manipulador do Modelo Anatômico
    // Nome técnico do script: ManipuladorModeloAnatomico
    // Função: controla rotação, zoom, grab e reset de transformação do modelo/raiz do SNC.
    // Relação com o protótipo: permite manipular o conjunto anatômico sem alterar seleção, colliders,
    // hierarquia ou componentes XR existentes.
    // Objetos da cena: usado no Sistema_Nervoso_Central.
    /// <summary>
    /// Manipula a raiz anatômica com rotação, escala e grab.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainModelManipulator")]
    public sealed class ManipuladorModeloAnatomico : MonoBehaviour
    {
        // Transform alvo, normalmente a raiz visual do SNC organizado.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("targetRoot")]
        private Transform alvoRaiz;
        // Velocidade de rotação horizontal.
        [UnityEngine.Serialization.FormerlySerializedAs("rotacaovelocidade")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("rotationSpeed")]
        private float velocidadeRotacao = 80f;
        // Permite rotação vertical pelo eixo Y do analógico quando esse fluxo estiver configurado.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("allowVerticalRotation")]
        private bool permitirVerticalRotacao = true;
        // Corrige o sentido horizontal do analógico quando o input chega invertido.
        [SerializeField]
        private bool inverterEixoHorizontal = true;
        // Velocidade de rotação vertical.
        [UnityEngine.Serialization.FormerlySerializedAs("verticalrotacaovelocidade")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("verticalRotationSpeed")]
        private float velocidadeRotacaoVertical = 50f;
        // Velocidade e limites de zoom por escala local.
        [UnityEngine.Serialization.FormerlySerializedAs("zoomVelocidade")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("zoomSpeed")]
        private float velocidadeAproximacao = 1.5f;
        [UnityEngine.Serialization.FormerlySerializedAs("minimoescala")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("minScale")]
        private float escalaMinima = 0.35f;
        [UnityEngine.Serialization.FormerlySerializedAs("maximoescala")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("maxScale")]
        private float escalaMaxima = 3.5f;
        // Transformação original capturada para o reset.
        private Vector3 posicaoLocalOriginal;
        private Quaternion rotacaoLocalOriginal;
        private Vector3 escalaLocalOriginal = Vector3.one;
        // Multiplicador atual de escala aplicado sobre a escala original.
        private float escalaMultiplicador = 1f;
        // Estado temporário de grab.
        private bool ehAgarrado;
        private Vector3 posicaoInicialAgarre;
        private Quaternion rotacaoInicialAgarre;
        private Vector3 posicaoInicialAlvo;
        private Quaternion rotacaoInicialAlvo;
        public Transform AlvoRaiz => alvoRaiz;
        public bool EstaAgarrado => ehAgarrado;
        /// <summary>
        /// Resolve o alvo padrão e captura a transformação original.
        /// </summary>
        private void Awake()
        {
            if (alvoRaiz == null)
            {
                alvoRaiz = transform;
            }

            CapturarTransformacaoOriginal();
        }

        /// <summary>
        /// Configura o alvo raiz sem criar novos objetos.
        /// </summary>
        public void Configurar(Transform alvo)
        {
            alvoRaiz = alvo != null ? alvo : transform;
            CapturarTransformacaoOriginal();
        }

        /// <summary>
        /// Guarda posição, rotação e escala iniciais para reset.
        /// </summary>
        public void CapturarTransformacaoOriginal()
        {
            if (alvoRaiz == null)
            {
                return;
            }

            posicaoLocalOriginal = alvoRaiz.localPosition;
            rotacaoLocalOriginal = alvoRaiz.localRotation;
            escalaLocalOriginal = alvoRaiz.localScale;
            escalaMultiplicador = 1f;
        }

        /// <summary>
        /// Inicia o grab da raiz usando a pose do controle.
        /// </summary>
        public void IniciarAgarre(Transform agarrador)
        {
            if (alvoRaiz == null || agarrador == null)
            {
                return;
            }

            ehAgarrado = true;
            posicaoInicialAgarre = agarrador.position;
            rotacaoInicialAgarre = agarrador.rotation;
            posicaoInicialAlvo = alvoRaiz.position;
            rotacaoInicialAlvo = alvoRaiz.rotation;
        }

        /// <summary>
        /// Atualiza a transformação da raiz enquanto o grip está ativo.
        /// </summary>
        public void AtualizarAgarre(Transform agarrador)
        {
            if (!ehAgarrado || alvoRaiz == null || agarrador == null)
            {
                return;
            }

            var variacaoRotacao = agarrador.rotation * Quaternion.Inverse(rotacaoInicialAgarre);
            alvoRaiz.rotation = variacaoRotacao * rotacaoInicialAlvo;
            alvoRaiz.position = posicaoInicialAlvo + (agarrador.position - posicaoInicialAgarre);
        }

        /// <summary>
        /// Finaliza o grab sem mexer na hierarquia.
        /// </summary>
        public void EncerrarAgarre()
        {
            ehAgarrado = false;
        }

        /// <summary>
        /// Rotaciona o modelo por input Vetor2, com correção opcional do eixo horizontal.
        /// </summary>
        public void Girar(Vector2 entrada)
        {
            if (alvoRaiz == null || entrada.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (inverterEixoHorizontal)
            {
                entrada.x = -entrada.x;
            }

            alvoRaiz.Rotate(Vector3.up, entrada.x * velocidadeRotacao * Time.deltaTime, Space.World);
            if (permitirVerticalRotacao)
            {
                alvoRaiz.Rotate(Vector3.right, -entrada.y * velocidadeRotacaoVertical * Time.deltaTime, Space.World);
            }
        }

        /// <summary>
        /// Aplica zoom por escala local, respeitando limites mínimo e máximo.
        /// </summary>
        public void AproximarOuAfastar(float entrada)
        {
            if (alvoRaiz == null || Mathf.Abs(entrada) < 0.0001f)
            {
                return;
            }

            escalaMultiplicador = Mathf.Clamp(escalaMultiplicador + entrada * velocidadeAproximacao * Time.deltaTime, escalaMinima, escalaMaxima);
            alvoRaiz.localScale = escalaLocalOriginal * escalaMultiplicador;
        }

        /// <summary>
        /// Restaura a transformação original da raiz anatômica.
        /// </summary>
        public void RedefinirTransformacao()
        {
            if (alvoRaiz == null)
            {
                return;
            }

            EncerrarAgarre();
            alvoRaiz.localPosition = posicaoLocalOriginal;
            alvoRaiz.localRotation = rotacaoLocalOriginal;
            alvoRaiz.localScale = escalaLocalOriginal;
            escalaMultiplicador = 1f;
        }
    }
}