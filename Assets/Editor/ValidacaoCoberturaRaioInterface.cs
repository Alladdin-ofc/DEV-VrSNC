using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Inventário somente leitura da cobertura ativa de raycast da UI em espaço de mundo. Não
    /// regrava raycastTarget, máscaras, raycasters, callbacks nem o estado da cena.
    /// </summary>
    public static class ValidacaoCoberturaRaioInterface
    {
        private const int InterfaceCamada = 5;
        [MenuItem("Ferramentas/VR SNC/Interface/Validar Cobertura de Raycast (Somente Leitura)")]
        public static void ValidarPeloMenu()
        {
            var relatorio = GerarRelatorio(SceneManager.GetActiveScene(), out var erros, out var avisos);
            if (erros > 0)
                Debug.LogError(relatorio);
            else if (avisos > 0)
                Debug.LogWarning(relatorio);
            else
                Debug.Log(relatorio);
        }

        [MenuItem("Ferramentas/VR SNC/Interface/Validar Raio do Botão Alternativo em Execução (Somente Leitura)")]
        public static void ValidarRaioAlternativoDosBotoesPeloMenu()
        {
            GameObject raiz = null;
            GameObject cameraObjeto = null;
            GameObject eventoSistemaObjeto = null;
            RenderTexture renderizarTextura = null;
            var naFila = false;
            var criacaoQuadro = Time.frameCount;
            void Limpar()
            {
                if (cameraObjeto != null)
                    cameraObjeto.GetComponent<Camera>().targetTexture = null;
                if (renderizarTextura != null)
                    Object.DestroyImmediate(renderizarTextura);
                if (eventoSistemaObjeto != null)
                    Object.DestroyImmediate(eventoSistemaObjeto);
                if (cameraObjeto != null)
                    Object.DestroyImmediate(cameraObjeto);
                if (raiz != null)
                    Object.DestroyImmediate(raiz);
            }

            if (!Application.isPlaying)
                throw new System.InvalidOperationException("Execute a fixture de fallback de raio no Modo de Jogo para que os gráficos Canvas recebam quadros de renderização.");
            try
            {
                raiz = new GameObject("__VR_SNC_FallbackButtonRayFixture", typeof(Canvas), typeof(TrackedDeviceGraphicRaycaster), typeof(GraphicRaycaster));
                raiz.layer = InterfaceCamada;
                raiz.transform.position = new Vector3(10000f, 10000f, 10000f);
                var canvas = raiz.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                cameraObjeto = new GameObject("__VR_SNC_FallbackButtonRayCamera", typeof(Camera));
                cameraObjeto.transform.position = raiz.transform.position + new Vector3(0f, 0f, -2f);
                canvas.worldCamera = cameraObjeto.GetComponent<Camera>();
                renderizarTextura = new RenderTexture(64, 64, 16);
                canvas.worldCamera.targetTexture = renderizarTextura;
                var conteudoObjeto = new GameObject("Content", typeof(RectTransform));
                conteudoObjeto.layer = InterfaceCamada;
                conteudoObjeto.transform.SetParent(raiz.transform, false);
                var metodo = typeof(PainelNavegacaoAnatomica).GetMethod("CriarBotaoAlternativo", BindingFlags.Static | BindingFlags.NonPublic);
                if (metodo == null)
                    throw new System.MissingMethodException(nameof(PainelNavegacaoAnatomica), "CriarBotaoAlternativo");
                var botao = metodo.Invoke(null, new object[] { conteudoObjeto.transform }) as Button;
                if (botao == null)
                    throw new System.InvalidOperationException("A fábrica de botões de fallback retornou nulo.");
                var eventoSistema = EventSystem.current;
                if (eventoSistema == null)
                {
                    eventoSistemaObjeto = new GameObject("__VR_SNC_FallbackButtonRayEventSystem", typeof(EventSystem));
                    eventoSistema = eventoSistemaObjeto.GetComponent<EventSystem>();
                }

                void VerificarAposConstruirCanvas()
                {
                    try
                    {
                        if (!Application.isPlaying)
                        {
                            EditorApplication.update -= VerificarAposConstruirCanvas;
                            Limpar();
                            return;
                        }

                        Canvas.ForceUpdateCanvases();
                        if (Time.frameCount - criacaoQuadro < 3)
                            return;
                        var retangulo = (RectTransform)botao.transform;
                        var alvo = retangulo.TransformPoint(retangulo.rect.center);
                        var origem = cameraObjeto.transform.position;
                        var eventoDados = new TrackedDeviceEventData(eventoSistema)
                        {
                            position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, alvo),
                            rayPoints = new List<Vector3> { origem, alvo + (alvo - origem).normalized * 0.05f },
                            layerMask = 1 << InterfaceCamada,

                        };
                        var resultados = new List<RaycastResult>(8);
                        var rastreado = raiz.GetComponent<TrackedDeviceGraphicRaycaster>();
                        rastreado.Raycast(eventoDados, resultados);
                        var imagem = botao.targetGraphic as Image;
                        var texto = botao.GetComponentInChildren<Text>(true);
                        var superior = resultados.FirstOrDefault().gameObject;
                        var acertoBotao = superior != null && superior.GetComponentInParent<Button>() == botao;
                        var passagem = botao.gameObject.layer == InterfaceCamada && imagem != null && imagem.raycastTarget && texto != null && !texto.raycastTarget && acertoBotao;
                        var relatorio = $"[VR SNC] Teste alternativo de seleção de botão por raio {(passagem ? "PASS" : "FAIL")}: " + $"layer={botao.gameObject.layer}, targetImage={imagem != null && imagem.raycastTarget}, " + $"textRaycast={texto != null && texto.raycastTarget}, top={(superior != null ? superior.name : "<none>")}, hits={resultados.Count}";
                        if (!passagem)
                        {
                            var elementosVisuais = GraphicRegistry.GetGraphicsForCanvas(canvas);
                            relatorio += $" depth={imagem?.depth}, cull={imagem?.canvasRenderer.cull}, GraphicRegistryCount={elementosVisuais.Count}";
                        }

                        if (passagem)
                            Debug.Log(relatorio);
                        else
                            Debug.LogError(relatorio);
                        EditorApplication.update -= VerificarAposConstruirCanvas;
                        Limpar();
                    }
                    catch (System.Exception excecao)
                    {
                        EditorApplication.update -= VerificarAposConstruirCanvas;
                        Limpar();
                        Debug.LogError("[VR SNC] ERRO da fixture de raio de botão de fallback: " + excecao.GetBaseException().Message);
                    }
                }

                EditorApplication.update += VerificarAposConstruirCanvas;
                naFila = true;
                return;
            }
            catch (System.Exception excecao)
            {
                Debug.LogError("[VR SNC] ERRO da fixture de raio de botão de fallback: " + excecao.GetBaseException().Message);
            }
            finally
            {
                if (!naFila)
                    Limpar();
            }
        }

        [MenuItem("Ferramentas/VR SNC/Interface/Aplicar Cobertura de Raycast aos Painéis Principal e de Pulso (Desfazer, Sem Salvar)")]
        public static void AplicarCoberturaDeRaioAoPainel()
        {
            var cena = SceneManager.GetActiveScene();
            if (!cena.IsValid() || !cena.isLoaded || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new System.InvalidOperationException("Use uma cena carregada e ociosa no Modo de Edição.");
            var raizes = new[] { EncontrarObjetoCena("Painel_Anatomico_VR_SNC"), EncontrarObjetoCena("Oraculo_Anatomico_Punho") }.Where(x => x != null && x.scene == cena).ToArray();
            if (raizes.Length == 0)
                throw new System.InvalidOperationException("Nem o painel de UI principal nem o de pulso foram encontrados na cena ativa.");
            Undo.IncrementCurrentGroup();
            var desfazerGrupo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Aplicar cobertura de raycast da UI aos painéis do SNC");
            try
            {
                foreach (var raiz in raizes)
                    AplicarCoberturaPainel(raiz);
                Debug.Log("[VR SNC] A configuração de cobertura de raycast da UI foi aplicada somente com Undo; a cena não foi salva.");
                Undo.CollapseUndoOperations(desfazerGrupo);
            }
            catch
            {
                Undo.RevertAllDownToGroup(desfazerGrupo);
                throw;
            }
        }

        private static void AplicarCoberturaPainel(GameObject painelRaiz)
        {
            var botoes = painelRaiz.GetComponentsInChildren<Button>(true).Distinct().ToArray();
            var canvas = botoes.Select(x => x.GetComponentInParent<Canvas>(true)).Where(x => x != null).Distinct().ToArray();
            if (canvas.Length == 0)
            {
                Debug.LogWarning($"[VR SNC] Nenhum botão encontrado em {ObterCaminho(painelRaiz.transform)}; nenhuma configuração de cobertura foi aplicada.", painelRaiz);
                return;
            }

            foreach (var canvas2 in canvas)
                AplicarCoberturaCanvas(canvas2, botoes);
        }

        private static void AplicarCoberturaCanvas(Canvas canvas, Button[] todosBotoes)
        {
            var canvasBotoes = todosBotoes.Where(x => x.GetComponentInParent<Canvas>(true) == canvas).ToArray();
            foreach (var botao in canvasBotoes)
            {
                if (botao == null)
                    continue;
                foreach (var transformacao in botao.GetComponentsInChildren<Transform>(true))
                {
                    Undo.RecordObject(transformacao.gameObject, "Definir a subárvore do botão para a camada de UI");
                    transformacao.gameObject.layer = InterfaceCamada;
                }

                if (botao.targetGraphic != null)
                {
                    Undo.RecordObject(botao.targetGraphic, "Ativar o raycast gráfico do alvo do botão");
                    botao.targetGraphic.raycastTarget = true;
                }

                foreach (var texto in botao.GetComponentsInChildren<Text>(true))
                {
                    Undo.RecordObject(texto, "Desativar o raycast do rótulo do botão");
                    texto.raycastTarget = false;
                }

                foreach (var texto2 in botao.GetComponentsInChildren<TMP_Text>(true))
                {
                    Undo.RecordObject(texto2, "Desativar o raycast do rótulo TMP do botão");
                    texto2.raycastTarget = false;
                }
            }

            var rastreado = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
            var criado = rastreado == null;
            if (rastreado == null)
            {
                rastreado = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
                rastreado.checkFor3DOcclusion = true;
                rastreado.blockingMask = ~0;
                rastreado.raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
            }
            else
            {
                Debug.Log($"[VR SNC] Configuração TDR existente preservada em {ObterCaminho(canvas.transform)}: " + $"occlusion={rastreado.checkFor3DOcclusion}, blockingMask={rastreado.blockingMask.value}, trigger={rastreado.raycastTriggerInteraction}");
                Undo.RecordObject(rastreado, "Preservar a configuração existente do raycaster de UI rastreada");
            }

            var camera = canvas.worldCamera;
            if (camera == null && Camera.main != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                Undo.RecordObject(canvas, "Atribuir a câmera XR principal ao Canvas de UI");
                canvas.worldCamera = Camera.main;
            }

            var decoracoesLimpas = 0;
            foreach (var elementoVisual in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (!elementoVisual.isActiveAndEnabled || !elementoVisual.gameObject.activeInHierarchy || !elementoVisual.raycastTarget)
                    continue;
                if (elementoVisual.GetComponentInParent<Selectable>(true) != null || elementoVisual.GetComponentInParent<ScrollRect>(true) != null)
                    continue;
                Undo.RecordObject(elementoVisual, "Desativar o bloqueador decorativo de raycast da UI");
                elementoVisual.raycastTarget = false;
                decoracoesLimpas++;
            }

            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(rastreado);
            Debug.Log($"[VR SNC] Coverage {(criado ? "created" : "reused")} {ObterCaminho(canvas.transform)} " + $"buttons={canvasBotoes.Length} camera={(canvas.worldCamera != null ? canvas.worldCamera.name : "<none>")} " + $"decorationsCleared={decoracoesLimpas}; não salvar.", canvas);
        }

        private static GameObject EncontrarObjetoCena(string objetoNome)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(x => x.name == objetoNome)?.gameObject;
        }

        public static string GerarRelatorio(Scene cena, out int erros, out int avisos)
        {
            erros = 0;
            avisos = 0;
            var saida = new StringBuilder("[VR SNC] Cobertura de seleção por raio da UI (somente leitura)\n");
            if (!cena.IsValid() || !cena.isLoaded)
            {
                erros++;
                return saida.AppendLine("ERRO: a cena não está carregada.").ToString();
            }

            var eventoSistemas = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(x => x.gameObject.scene == cena).ToArray();
            saida.AppendLine($"EventSystems={eventoSistemas.Length}");
            if (eventoSistemas.Length != 1)
            {
                erros++;
                saida.AppendLine("ERRO: era esperado exatamente um EventSystem na cena.");
            }

            var painelRaizes = new[] { EncontrarObjetoCena("Painel_Anatomico_VR_SNC"), EncontrarObjetoCena("Oraculo_Anatomico_Punho") }.Where(x => x != null && x.scene == cena).ToArray();
            var canvas = painelRaizes.SelectMany(raiz => raiz.GetComponentsInChildren<Canvas>(true)).Where(x => x.isActiveAndEnabled && x.gameObject.activeInHierarchy).Distinct().ToArray();
            var botoesAtivos = 0;
            var retangulosRolagemAtivos = 0;
            var possiveisBloqueadores = 0;
            saida.AppendLine($"ActiveCanvases={canvas.Length}");
            foreach (var canvas2 in canvas)
            {
                var elementoVisual = canvas2.GetComponent<GraphicRaycaster>();
                var rastreado = canvas2.GetComponent<TrackedDeviceGraphicRaycaster>();
                saida.AppendLine($"Canvas={ObterCaminho(canvas2.transform)} mode={canvas2.renderMode} layer={canvas2.gameObject.layer} " + $"GraphicRaycaster={elementoVisual != null && elementoVisual.isActiveAndEnabled} " + $"TrackedDeviceGraphicRaycaster={rastreado != null && rastreado.isActiveAndEnabled}");
                if (canvas2.renderMode == RenderMode.WorldSpace && (rastreado == null || !rastreado.isActiveAndEnabled))
                {
                    erros++;
                    saida.AppendLine("  ERRO: o canvas em espaço de mundo não possui TrackedDeviceGraphicRaycaster ativo.");
                }

                var botoes = canvas2.GetComponentsInChildren<Button>(false).Where(x => x.isActiveAndEnabled && x.gameObject.activeInHierarchy).Distinct().ToArray();
                botoesAtivos += botoes.Length;
                foreach (var botao in botoes)
                {
                    var alvo = botao.targetGraphic;
                    var quantidadeChamadas = botao.onClick.GetPersistentEventCount();
                    var caminho = ObterCaminho(botao.transform);
                    var visivel = EhPotencialmenteVisivel(botao);
                    saida.AppendLine($"  Button={caminho} layer={botao.gameObject.layer} interactable={botao.IsInteractable()} " + $"target={(alvo != null ? alvo.GetType().Name : "null")} persistentCallbacks={quantidadeChamadas}");
                    saida.AppendLine($"    Visibility={(visivel ? "visible-or-unmasked" : "masked/outside-viewport; skipped")}");
                    if (!visivel)
                        continue;
                    if (botao.gameObject.layer != InterfaceCamada)
                    {
                        erros++;
                        saida.AppendLine("    ERRO: o botão está fora da camada de UI usada pelos raios das mãos.");
                    }

                    if (alvo == null || !alvo.raycastTarget)
                    {
                        erros++;
                        saida.AppendLine("    ERRO: o gráfico de destino do botão está ausente ou raycastTarget=false.");
                    }

                    if (quantidadeChamadas == 0)
                    {
                        avisos++;
                        saida.AppendLine("    AVISO: o botão não possui callback persistente; listeners de execução não podem ser comprovados no modo de edição.");
                    }

                    SondarBotaoCentro(canvas2, rastreado, botao, eventoSistemas.FirstOrDefault(), saida, ref avisos);
                }

                var rolagemRetangulos = canvas2.GetComponentsInChildren<ScrollRect>(false).Where(x => x.isActiveAndEnabled && x.gameObject.activeInHierarchy).Distinct().ToArray();
                retangulosRolagemAtivos += rolagemRetangulos.Length;
                foreach (var rolagem in rolagemRetangulos)
                    saida.AppendLine($"  ScrollRect={ObterCaminho(rolagem.transform)} active=true");
                foreach (var candidato in canvas2.GetComponentsInChildren<Graphic>(false).Distinct())
                {
                    if (!candidato.isActiveAndEnabled || !candidato.gameObject.activeInHierarchy || !candidato.raycastTarget)
                        continue;
                    if (candidato.GetComponentInParent<Selectable>() != null || candidato.GetComponentInParent<Mask>() != null || candidato.GetComponentInParent<ScrollRect>() != null)
                        continue;
                    if (candidato is Text || candidato is TMP_Text || candidato is Image)
                    {
                        possiveisBloqueadores++;
                        avisos++;
                        saida.AppendLine($"  AVISO: gráfico decorativo pode bloquear a UI: {ObterCaminho(candidato.transform)} " + $"type={candidato.GetType().Name} raycastTarget=true");
                    }
                }
            }

            saida.AppendLine($"Resumo buttons={botoesAtivos} scrollRects={retangulosRolagemAtivos} possibleDecorativeBlockers={possiveisBloqueadores} errors={erros} warnings={avisos}");
            return saida.ToString();
        }

        private static bool EhPotencialmenteVisivel(Button botao)
        {
            if (botao == null || !botao.gameObject.activeInHierarchy)
                return false;
            var retangulo = botao.transform as RectTransform;
            if (retangulo == null)
                return false;
            foreach (var mascara in botao.GetComponentsInParent<RectMask2D>(true))
            {
                var limites = RectTransformUtility.CalculateRelativeRectTransformBounds(mascara.transform, retangulo);
                var mascaraRetangulo = mascara.rectTransform.rect;
                var limitesRetangulo = new Rect(limites.min.x, limites.min.y, limites.size.x, limites.size.y);
                if (!limitesRetangulo.Overlaps(mascaraRetangulo))
                    return false;
            }

            foreach (var mascara2 in botao.GetComponentsInParent<Mask>(true))
            {
                var mascaraRetangulo2 = mascara2.transform as RectTransform;
                if (mascaraRetangulo2 == null)
                    continue;
                var limites2 = RectTransformUtility.CalculateRelativeRectTransformBounds(mascara2.transform, retangulo);
                var limitesRetangulo2 = new Rect(limites2.min.x, limites2.min.y, limites2.size.x, limites2.size.y);
                if (!limitesRetangulo2.Overlaps(mascaraRetangulo2.rect))
                    return false;
            }

            return true;
        }

        private static void SondarBotaoCentro(Canvas canvas, TrackedDeviceGraphicRaycaster rastreado, Button botao, EventSystem eventoSistema, StringBuilder saida, ref int avisos)
        {
            if (rastreado == null || eventoSistema == null || canvas.worldCamera == null)
            {
                avisos++;
                saida.AppendLine($"    AVISO: não há sonda central determinística de dispositivo rastreado para {ObterCaminho(botao.transform)} (câmera/raycaster indisponível).");
                return;
            }

            var retangulo = botao.transform as RectTransform;
            if (retangulo == null)
                return;
            var alvo = retangulo.TransformPoint(retangulo.rect.center);
            var origem = canvas.worldCamera.transform.position;
            var direcao = (alvo - origem).normalized;
            var pontoFinal = alvo + direcao * 0.05f;
            var eventoDados = new TrackedDeviceEventData(eventoSistema)
            {
                position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, alvo),
                rayPoints = new List<Vector3> { origem, pontoFinal },
                layerMask = 1 << InterfaceCamada,
            };
            var rastreadoResultados = new List<RaycastResult>(8);
            rastreado.Raycast(eventoDados, rastreadoResultados);
            var globalResultados = new List<RaycastResult>(8);
            eventoSistema.RaycastAll(eventoDados, globalResultados);
            var rastreadoSuperior = rastreadoResultados.FirstOrDefault().gameObject;
            var superior = globalResultados.FirstOrDefault().gameObject;
            if (rastreadoSuperior == null)
            {
                avisos++;
                saida.AppendLine($"    AVISO: a sonda central TDR não retornou Graphic para {ObterCaminho(botao.transform)}; globalTop={(superior != null ? superior.name : "<none>")}.");
                return;
            }

            var rastreadoBotao = rastreadoSuperior.GetComponentInParent<Button>();
            var globalBotao = superior != null ? superior.GetComponentInParent<Button>() : null;
            saida.AppendLine($"    Probe={ObterCaminho(botao.transform)} TDRTop={ObterCaminho(rastreadoSuperior.transform)} " + $"TDRButton={(rastreadoBotao != null ? ObterCaminho(rastreadoBotao.transform) : "<none>")} " + $"GlobalTop={(superior != null ? ObterCaminho(superior.transform) : "<none>")} " + $"GlobalButton={(globalBotao != null ? ObterCaminho(globalBotao.transform) : "<none>")} " + $"tdrHits={rastreadoResultados.Count} globalHits={globalResultados.Count}");
            if (rastreadoBotao != botao || (globalBotao != null && globalBotao != botao))
            {
                avisos++;
                saida.AppendLine("    AVISO: a sonda central TDR/global está ocluída ou resolve para outro Graphic/Button.");
            }
        }

        private static string ObterCaminho(Transform transformacao)
        {
            if (transformacao == null)
                return "<null>";
            var nomes = transformacao.GetComponentsInParent<Transform>(true).Reverse().Select(x => x.name).ToArray();
            return string.Join("/", nomes);
        }
    }
}
