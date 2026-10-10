using UnityEngine;

namespace VRSNC.Interface
{
    // Nome funcional em português: Painel VR Seguindo a Câmera
    // Nome técnico do script: AcompanhamentoCameraPainelInterface
    // Função: mantém o painel anatômico à frente da câmera, com distância confortável e rotação voltada ao usuário.
    // Relação com o protótipo: controla apenas posicionamento visual do painel do SNC, sem recriar UI ou trocar componentes.
    // Objetos da cena: usado no Painel_Anatomico_VR_SNC.
    /// <summary>
    /// Posiciona suavemente o painel em world-space em relação à câmera do usuário.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.UI", "Assembly-CSharp", "BrainUIPanelFollowCamera")]
    public sealed class AcompanhamentoCameraPainelInterface : MonoBehaviour
    {
        // Câmera do usuário em VR; quando ausente, usa Camera.main.
        [UnityEngine.Serialization.FormerlySerializedAs("cameratransformacao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("cameraTransform")]
        private Transform transformacaoCamera;
        // Permite desligar o acompanhamento se o painel precisar ficar parado.
        [UnityEngine.Serialization.FormerlySerializedAs("acompanharcamera")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("followCamera")]
        private bool acompanharCamera = true;
        // Distância frontal confortável para leitura e interação.
        [SerializeField, Min(0.45f)]
        [UnityEngine.Serialization.FormerlySerializedAs("distance")]
        private float distancia = 0.82f;
        // Começa ao lado da anatomia central para o modelo não ocultar o painel científico.
        [UnityEngine.Serialization.FormerlySerializedAs("horizontaldeslocamento")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("horizontalOffset")]
        private float deslocamentoHorizontal = 0.42f;
        // Offset vertical leve para não colar no rosto nem subir demais.
        [UnityEngine.Serialization.FormerlySerializedAs("verticaldeslocamento")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("verticalOffset")]
        private float deslocamentoVertical = -0.04f;
        // Suavizações de posição e rotação para evitar saltos bruscos.
        [UnityEngine.Serialization.FormerlySerializedAs("posicaoSuavizacao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("positionSmoothing")]
        private float suavizacaoPosicao = 8f;
        [UnityEngine.Serialization.FormerlySerializedAs("rotacaoSuavizacao")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("rotationSmoothing")]
        private float suavizacaoRotacao = 10f;
        /// <summary>Verdadeiro apenas enquanto o usuário escolheu explicitamente manter o painel seguindo o HMD.</summary>
        public bool EhAcompanhamentoCamera => acompanharCamera;
        public float DistanciaMetros => distancia;
        public float DeslocamentoHorizontalMetros => deslocamentoHorizontal;
        /// <summary>
        /// Resolve a câmera principal quando a referência não está serializada.
        /// </summary>
        private void Awake()
        {
            if (transformacaoCamera == null && Camera.main != null)
            {
                transformacaoCamera = Camera.main.transform;
            }
        }

        /// <summary>
        /// Reposiciona o painel após a câmera se mover, mantendo-o voltado para o usuário.
        /// </summary>
        private void LateUpdate()
        {
            if (!acompanharCamera)
            {
                return;
            }

            var camera = transformacaoCamera != null ? transformacaoCamera : Camera.main != null ? Camera.main.transform : null;
            if (camera == null)
            {
                return;
            }

            var posicaoDesejada = camera.position + camera.forward * distancia + camera.right * deslocamentoHorizontal + camera.up * deslocamentoVertical;
            var olharDirecao = (transform.position - camera.position).normalized;
            if (olharDirecao.sqrMagnitude < 0.0001f)
            {
                olharDirecao = camera.forward;
            }

            var rotacaoDesejada = Quaternion.LookRotation(olharDirecao, Vector3.up);
            transform.position = Vector3.Lerp(transform.position, posicaoDesejada, 1f - Mathf.Exp(-suavizacaoPosicao * Time.deltaTime));
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoDesejada, 1f - Mathf.Exp(-suavizacaoRotacao * Time.deltaTime));
        }

        /// <summary>
        /// Configura a câmera seguida e se o painel deve acompanhar a visão.
        /// </summary>
        public void Configurar(Transform camera, bool devemSeguir)
        {
            transformacaoCamera = camera;
            acompanharCamera = devemSeguir;
        }

        /// <summary>
        /// Ativa ou desativa o follow do painel sem alterar os componentes de UI.
        /// </summary>
        public void DefinirAcompanhamentoAtivo(bool habilitado)
        {
            acompanharCamera = habilitado;
        }

        /// <summary>Define o deslocamento lateral explícito usado no posicionamento inicial e em Recentrar.</summary>
        public void DefinirDeslocamentoHorizontal(float deslocamentoMetros)
        {
            deslocamentoHorizontal = deslocamentoMetros;
        }

        /// <summary>
        /// Define a apresentação compacta, lado a lado, usada pelo painel espacial principal. Isto altera
        /// apenas sua pose inicial/de recentramento; um painel solto continua fixado no mundo por BrainWorldPanelHandle.
        /// </summary>
        public void ConfigurarPosicionamentoNoMundo(float distanciaMetros, float deslocamentoLateralMetros, float alturaDeslocamentoMetros)
        {
            distancia = Mathf.Clamp(distanciaMetros, 0.45f, 1.35f);
            deslocamentoHorizontal = Mathf.Clamp(deslocamentoLateralMetros, -0.75f, 0.75f);
            deslocamentoVertical = Mathf.Clamp(alturaDeslocamentoMetros, -0.35f, 0.35f);
        }

        /// <summary>
        /// Reposiciona imediatamente o painel à frente da câmera atual.
        /// Pode ser ligado a um botão no futuro sem mudar a arquitetura do painel.
        /// </summary>
        public void ReposicionarDianteDaCamera()
        {
            var camera = transformacaoCamera != null ? transformacaoCamera : Camera.main != null ? Camera.main.transform : null;
            if (camera == null)
            {
                return;
            }

            transform.position = camera.position + camera.forward * distancia + camera.right * deslocamentoHorizontal + camera.up * deslocamentoVertical;
            var olharDirecao = (transform.position - camera.position).normalized;
            transform.rotation = Quaternion.LookRotation(olharDirecao, Vector3.up);
        }

        /// <summary>Posiciona uma janela espacial expandida no campo frontal confortável do usuário e depois a mantém fixada.</summary>
        public void PosicionarJanelaConfortavelmenteDianteDaCamera()
        {
            var camera = transformacaoCamera != null ? transformacaoCamera : Camera.main != null ? Camera.main.transform : null;
            if (camera == null)
                return;
            transform.position = camera.position + camera.forward * Mathf.Clamp(distancia, 0.70f, 1.05f) + camera.up * deslocamentoVertical;
            transform.rotation = Quaternion.LookRotation((transform.position - camera.position).normalized, Vector3.up);
            acompanharCamera = false;
        }
    }
}