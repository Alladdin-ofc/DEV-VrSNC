using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRSNC.Interface;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Relata o rig real da cena e os pré-requisitos de arbitragem para a captura do painel principal. Intencionalmente
    /// não é um teste de captura sintético: deixa claro quais fatos ainda exigem modo de execução e uma mão/controle rastreado.
    /// </summary>
    internal static class ValidacaoInterfacePainelMundo
    {
        private const string NomePainel = "Painel_Anatomico_VR_SNC";
        private const string NomeControle = "Barra_Manipulacao_Mundo";
        [MenuItem("Ferramentas/VR SNC/Interface/Validar Pré-requisitos de Entrada do Painel Principal", false, 211)]
        private static void ValidarPrecondicoesEntradaPainelPrincipal()
        {
            var problemas = new List<string>();
            var relatorio = new StringBuilder("[VR SNC] Pré-validação da entrada do painel espacial principal\n");
            var painel = GameObject.Find(NomePainel);
            if (painel == null)
            {
                Debug.LogError(relatorio.Append("FAIL: Painel_Anatomico_VR_SNC nao foi encontrado.").ToString());
                return;
            }

            var alca = painel.GetComponent<ControlePainelMundo>();
            var agarrar = alca != null ? alca.InteragivelAgarre : painel.GetComponent<XRGrabInteractable>();
            var seguir = painel.GetComponent<AcompanhamentoCameraPainelInterface>();
            Verificar(alca != null, "BrainWorldPanelHandle está presente na raiz do painel", problemas);
            Verificar(agarrar != null, "XRGrabInteractable está presente na raiz do painel", problemas);
            Verificar(seguir != null, "BrainUIPanelFollowCamera está presente na raiz do painel", problemas);
            if (seguir != null)
            {
                relatorio.Append($"Pose: follow={seguir.EhAcompanhamentoCamera}, distance={seguir.DistanciaMetros:F2}m, side={seguir.DeslocamentoHorizontalMetros:F2}m.\n");
                Verificar(!seguir.EhAcompanhamentoCamera, "o painel começa fixado no mundo em vez de acompanhar o HMD", problemas);
            }

            if (agarrar != null)
            {
                Verificar(agarrar.selectMode == InteractableSelectMode.Multiple, "o modo de seleção da captura é Multiple para duas mãos", problemas);
                Verificar(agarrar.trackPosition && agarrar.trackRotation && agarrar.trackScale, "a captura rastreia posição, rotação e escala", problemas);
                Verificar(agarrar.colliders.Count == 1, "uma superfície física de empunhadura da alça inferior está registrada", problemas);
                for (var indice = 0; indice < agarrar.colliders.Count; indice++)
                {
                    var colisor = agarrar.colliders[indice];
                    var validos = colisor != null && colisor.enabled && colisor.gameObject.activeInHierarchy && colisor.transform.IsChildOf(painel.transform) && colisor.isTrigger;
                    Verificar(validos, $"registered grip {indice + 1} está ativo, é baseado em gatilho e é filho do painel", problemas);
                    if (colisor != null)
                    {
                        var caixa = colisor as BoxCollider;
                        var profundidade = caixa != null ? caixa.size.z * Mathf.Abs(caixa.transform.lossyScale.z) : colisor.bounds.size.z;
                        var largura = caixa != null ? caixa.size.x * Mathf.Abs(caixa.transform.lossyScale.x) : colisor.bounds.size.x;
                        relatorio.Append($"Grip {indice + 1}: {colisor.name}, layer={LayerMask.LayerToName(colisor.gameObject.layer)}, width={largura:F3}m, depth={profundidade:F3}m.\n");
                        Verificar(caixa != null, $"grip {indice + 1} é um BoxCollider com forma física conhecida", problemas);
                        Verificar(profundidade >= 0.012f && profundidade <= 0.040f, $"grip {indice + 1} a profundidade física é de 12–40 mm", problemas);
                        Verificar(largura >= 0.12f, $"grip {indice + 1} possui um alvo invisível de pinça/gatilho de 12 cm ou mais de largura", problemas);
                    }
                }

                RelatorioCompatibilidadeInteratorReal(agarrar, relatorio, problemas);
            }

            var alcaVisual = painel.transform.Find("ExpandedRoot/" + NomeControle);
            var alcaImagem = alcaVisual != null ? alcaVisual.GetComponent<Image>() : null;
            var alcaRetangulo = alcaVisual as RectTransform;
            var alcaColisor = alcaVisual != null ? alcaVisual.GetComponent<BoxCollider>() : null;
            Verificar(alcaVisual != null && alcaImagem != null, "o ponto/barra visível de manipulação inferior existe", problemas);
            Verificar(alcaImagem != null && !alcaImagem.raycastTarget, "o visual da alça inferior não antecipa os raycasts de UI do dispositivo rastreado", problemas);
            Verificar(alcaRetangulo != null && alcaRetangulo.anchoredPosition.y <= -35f, "o ponto/barra inferior está 35 mm ou mais abaixo do retângulo do painel", problemas);
            var alcaColisorLarguraMetros = alcaColisor != null ? alcaColisor.transform.TransformVector(Vector3.right * alcaColisor.size.x).magnitude : 0f;
            Verificar(alcaColisorLarguraMetros >= .223f, "o colisor da alça inferior cobre toda a barra de foco de 224 mm", problemas);
            var janela = painel.GetComponent<JanelaEspacialPrincipal>();
            var redimensionarRaiz = painel.transform.Find("ExpandedRoot/Bordas_Redimensionar_Janela");
            Verificar(janela != null, "MainSpatialWindow gerencia a arbitragem de mover, redimensionar e maximizar o painel principal", problemas);
            Verificar(redimensionarRaiz != null && redimensionarRaiz.GetComponentsInChildren<LimiteRedimensionamentoJanelaPrincipal>(true).Length == 8, "oito recursos ativos de borda/canto de redimensionamento estão presentes", problemas);
            var colisorRedimensionamentoInferior = redimensionarRaiz != null ? redimensionarRaiz.Find("Bottom")?.GetComponent<BoxCollider>() : null;
            Verificar(alcaColisor != null && colisorRedimensionamentoInferior != null && !alcaColisor.bounds.Intersects(colisorRedimensionamentoInferior.bounds), "o alvo da alça inferior não se sobrepõe ao alvo de redimensionamento Bottom", problemas);
            var rastreadoEmissorRaio = painel.GetComponent<TrackedDeviceGraphicRaycaster>();
            var elementoVisualEmissorRaio = painel.GetComponent<GraphicRaycaster>();
            relatorio.Append($"Arbitragem de UI: TrackedDeviceGraphicRaycaster ativo={rastreadoEmissorRaio != null && rastreadoEmissorRaio.isActiveAndEnabled}; " + $"GraphicRaycaster active={elementoVisualEmissorRaio != null && elementoVisualEmissorRaio.isActiveAndEnabled}; " + $"proximity zones={Object.FindObjectsByType<ZonaProximidadePainelInterface>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length}.\n");
            Verificar(rastreadoEmissorRaio != null && rastreadoEmissorRaio.isActiveAndEnabled, "o raycaster de UI do dispositivo rastreado existe para os botões", problemas);
            Verificar(elementoVisualEmissorRaio != null && elementoVisualEmissorRaio.isActiveAndEnabled, "o raycaster de UI legado existe para o painel existente", problemas);
            var raioAlvosEmBarra = ContarAlvosRaioNaBarra(alcaVisual);
            relatorio.Append($"Alvos de raycast da alça inferior={raioAlvosEmBarra} (esperado: zero; os controles permanecem separados).\n");
            Verificar(raioAlvosEmBarra == 0, "nenhum elemento de UI consome raios do dispositivo rastreado dentro da alça inferior física", problemas);
            relatorio.Append("Nota de bloqueio: as verificações de proximidade/UI de BrainHandInteractionBridge arbitram a seleção anatômica, não a seleção XRI deste painel. " + "O comportamento de NearFar e dos dispositivos ainda deve ser testado no conjunto XR real; este menu não comprova seleção física.\n");
            if (problemas.Count == 0)
            {
                relatorio.Append("APROVADO: os pré-requisitos serializados do painel principal são coerentes. Próxima prova: Modo de Jogo com cada mão/controle real e, depois, captura de tela.");
                Debug.Log(relatorio.ToString(), painel);
            }
            else
            {
                relatorio.Append("FAIL:\n - ").Append(string.Join("\n - ", problemas));
                Debug.LogError(relatorio.ToString(), painel);
            }
        }

        private static void RelatorioCompatibilidadeInteratorReal(XRGrabInteractable agarrar, StringBuilder relatorio, List<string> problemas)
        {
            var interatores = Object.FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var proximoDistante = 0;
            var direto = 0;
            var raio = 0;
            var toque = 0;
            foreach (var interator in interatores)
            {
                if (interator == null || !interator.gameObject.scene.IsValid())
                    continue;
                if (interator is NearFarInteractor proximoDistanteInterator)
                {
                    proximoDistante++;
                    relatorio.Append($"  NearFar casting: near={proximoDistanteInterator.enableNearCasting}, far={proximoDistanteInterator.enableFarCasting}, " + $"nearCaster={proximoDistanteInterator.nearInteractionCaster != null}, farCaster={proximoDistanteInterator.farInteractionCaster != null}.\n");
                }

                if (interator is XRDirectInteractor)
                    direto++;
                if (interator is XRRayInteractor)
                    raio++;
                if (interator is XRPokeInteractor)
                    toque++;
                var camadasSobreposicao = (interator.interactionLayers.value & agarrar.interactionLayers.value) != 0;
                relatorio.Append($"Interactor: {ObterCaminho(interator.transform)} [{interator.GetType().Name}], active={interator.isActiveAndEnabled}, layerOverlap={camadasSobreposicao}.\n");
            }

            relatorio.Append($"Rig totals: all={interatores.Length}, NearFar={proximoDistante}, Direct={direto}, Ray={raio}, Poke={toque}.\n");
            RelatorioModalidadeGrupos(interatores, agarrar, relatorio, problemas);
            Verificar(proximoDistante > 0 || (direto > 0 && raio > 0), "o rig expõe NearFar ou ambos os caminhos de interação direta e por raio", problemas);
        }

        private static void RelatorioModalidadeGrupos(XRBaseInteractor[] interatores, XRGrabInteractable agarrar, StringBuilder relatorio, List<string> problemas)
        {
            var modalidade = Object.FindFirstObjectByType<XRInputModalityManager>(FindObjectsInactive.Include);
            Verificar(modalidade != null, "XRInputModalityManager existe para classificar os quatro grupos reais do rig", problemas);
            if (modalidade == null)
                return;
            RelatorioGrupoModalidade("mão esquerda", modalidade.leftHand, interatores, agarrar, relatorio, problemas);
            RelatorioGrupoModalidade("mão direita", modalidade.rightHand, interatores, agarrar, relatorio, problemas);
            RelatorioGrupoModalidade("controle esquerdo", modalidade.leftController, interatores, agarrar, relatorio, problemas);
            RelatorioGrupoModalidade("controle direito", modalidade.rightController, interatores, agarrar, relatorio, problemas);
        }

        private static void RelatorioGrupoModalidade(string nome, GameObject grupo, XRBaseInteractor[] interatores, XRGrabInteractable agarrar, StringBuilder relatorio, List<string> problemas)
        {
            Verificar(grupo != null, nome + " o grupo de modalidade existe", problemas);
            if (grupo == null)
                return;
            var total = 0;
            var compativel = 0;
            foreach (var interator in interatores)
            {
                if (interator == null || !interator.transform.IsChildOf(grupo.transform))
                    continue;
                total++;
                if ((interator.interactionLayers.value & agarrar.interactionLayers.value) != 0)
                    compativel++;
            }

            relatorio.Append($"Modality {nome}: ativo={grupo.activeInHierarchy}, XRI interactors={total}, compatible={compativel}.\n");
            Verificar(total > 0, nome + " modality has a real XRI interactor", problemas);
            Verificar(compativel > 0, nome + " a modalidade tem um interator que se sobrepõe à camada de interação do painel", problemas);
        }

        private static int ContarAlvosRaioNaBarra(Transform barra)
        {
            if (barra == null)
                return 0;
            var contagem = 0;
            foreach (var elementoVisual in barra.GetComponentsInChildren<Graphic>(true))
                if (elementoVisual != null && elementoVisual.isActiveAndEnabled && elementoVisual.gameObject.activeInHierarchy && elementoVisual.raycastTarget)
                    contagem++;
            return contagem;
        }

        private static string ObterCaminho(Transform transformacao)
        {
            var caminho = transformacao.name;
            while (transformacao.parent != null)
            {
                transformacao = transformacao.parent;
                caminho = transformacao.name + "/" + caminho;
            }

            return caminho;
        }

        private static void Verificar(bool condicao, string falha, List<string> problemas)
        {
            if (!condicao)
                problemas.Add(falha);
        }
    }
}
