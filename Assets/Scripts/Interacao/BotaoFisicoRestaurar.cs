using UnityEngine;

namespace VRSNC.Interacao
{
    // Nome funcional em português: Botão Físico de Reset do SNC
    // Nome técnico do script: BotaoFisicoRestaurar
    // Função: aciona o reset geral quando o botão físico/colisor é pressionado.
    // Relação com o protótipo: chama GerenciadorRestauracao sem recriar painel, cérebro ou hierarquia anatômica.
    // Objetos da cena: usado no Botao_Reset_SNC, quando o botão físico está presente.
    /// <summary>
    /// Adaptador simples para acionar o reset por clique no editor ou colisão/trigger em VR.
    /// </summary>
    [DisallowMultipleComponent]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "VRSNC.Interaction", "Assembly-CSharp", "BrainPhysicalResetButton")]
    public sealed class BotaoFisicoRestaurar : MonoBehaviour
    {
        // Gerenciador que executa o reset completo.
        [UnityEngine.Serialization.FormerlySerializedAs("redefinirgerenciador")]
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("resetManager")]
        private GerenciadorRestauracao gerenciadorRestauracao;
        // Intervalo mínimo entre acionamentos para evitar múltiplos resets por uma mesma colisão.
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("debounceSeconds")]
        private float antirruidoSegundos = 0.35f;
        // Marca o último momento em que o botão foi aceito.
        private float ultimoPressionadoTempo = -999f;
        /// <summary>
        /// Configura o gerenciador chamado pelo botão.
        /// </summary>
        public void Configurar(GerenciadorRestauracao gerenciador)
        {
            gerenciadorRestauracao = gerenciador;
        }

        /// <summary>
        /// Executa o reset respeitando debounce.
        /// </summary>
        public void Acionar()
        {
            if (Time.time - ultimoPressionadoTempo < antirruidoSegundos)
            {
                return;
            }

            ultimoPressionadoTempo = Time.time;
            gerenciadorRestauracao?.RestaurarTudo();
        }

        /// <summary>
        /// Fallback de editor para clicar no botão com o mouse.
        /// </summary>
        private void OnMouseDown()
        {
            Acionar();
        }

        /// <summary>
        /// Entrada física/VR: qualquer collider que entrar no botão aciona reset com debounce.
        /// </summary>
        private void OnTriggerEnter(Collider outro)
        {
            if (outro != null)
            {
                Acionar();
            }
        }
    }
}