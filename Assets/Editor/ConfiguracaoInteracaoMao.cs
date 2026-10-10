using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRSNC.Anatomia;
using VRSNC.Interacao;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

public static class ConfiguracaoInteracaoMao
{
    /// <summary>
    /// Verificações determinísticas no modo de execução. O caminho de catálogo/hierarquia não exige
    /// rastreamento físico de mão XR; as verificações de seleção XRI injetadas seguem opcionais para o fluxo completo.
    /// </summary>
    public static string ValidarExecucao(bool incluirCaminhoMaoInjetado = true)
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Entre no Modo de Jogo para validar o funcionamento em execução.");
        var verificacoes = new System.Collections.Generic.List<string>();
        Action<bool, string> exigir = (ok, mensagem) =>
        {
            if (!ok)
                throw new InvalidOperationException(mensagem);
            verificacoes.Add(mensagem);
        };
        var raiz = GameObject.Find("Sistema_Nervoso_Central");
        exigir(raiz != null, "A raiz Sistema_Nervoso_Central não foi encontrada");
        var selecao = raiz.GetComponent<GerenciadorSelecaoHierarquica>();
        var redefinir = raiz.GetComponent<GerenciadorRestauracao>();
        var ponte = raiz.GetComponent<PonteInteracaoMao>();
        var expansaoGlobal = raiz.GetComponent<ControladorExpansaoGlobal>();
        var manipulador = raiz.GetComponent<ManipuladorObjetoSelecionado>();
        var raizNo = raiz.GetComponent<NoInteracaoAnatomica>();
        var folhas = raiz.GetComponentsInChildren<ElementoInteragivelMao>(true);
        var nos = raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true);
        var vinculos = raiz.GetComponentsInChildren<VinculoAnatomico>(true);
        var modalidade = UnityEngine.Object.FindFirstObjectByType<XRInputModalityManager>();
        var interacoes = UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        exigir(selecao != null && redefinir != null && ponte != null && expansaoGlobal != null && manipulador != null && raizNo != null, "Gerenciadores de interação anatômica ou nó raiz ausentes");
        // Protege todo o grafo de cena/catálogo antes que ResetAll ou uma interação injetada altere o estado de execução.
        exigir(nos.Length == 272 && vinculos.Length == 272, "272 nós e vínculos anatômicos");
        exigir(folhas.Length == 240, "240 interagíveis físicos das mãos");
        var oraculoRecurso = Resources.Load<TextAsset>("AnatomicalOracle");
        exigir(oraculoRecurso != null, "O recurso AnatomicalOracle está instalado");
        var oraculoDocumento = LeitorJsonCompativel.Ler<DocumentoOraculoAnatomico>(oraculoRecurso.text);
        exigir(oraculoDocumento != null && oraculoDocumento.registros != null, "AnatomicalOracle possui registros");
        exigir(oraculoDocumento.registros.Length == 272, "272 registros de AnatomicalOracle");
        var oraculoPorCatalogoId = new System.Collections.Generic.Dictionary<string, RegistroOraculoAnatomico>(StringComparer.Ordinal);
        var oraculoChaves = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var oraculoGrupos = 0;
        var oraculoEstruturas = 0;
        foreach (var registro in oraculoDocumento.registros)
        {
            exigir(registro != null && !string.IsNullOrWhiteSpace(registro.idCatalogo) && !string.IsNullOrWhiteSpace(registro.chave), "o registro do oráculo possui identidade de catálogo e chave");
            exigir(oraculoChaves.Add(registro.chave), "chave única do oráculo " + registro.chave);
            exigir(!oraculoPorCatalogoId.ContainsKey(registro.idCatalogo), "identidade única de catálogo do oráculo " + registro.idCatalogo);
            oraculoPorCatalogoId.Add(registro.idCatalogo, registro);
            if (registro.tipo == "group")
                oraculoGrupos++;
            else if (registro.tipo == "structure")
                oraculoEstruturas++;
            else
                throw new InvalidOperationException("Tipo inesperado no registro do oráculo: " + registro.tipo + " para " + registro.chave);
        }

        exigir(oraculoGrupos == 32 && oraculoEstruturas == 240, "32 grupos e 240 estruturas no oráculo");
        var idsCatalogoVinculos = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        var nosEstrutura = new System.Collections.Generic.HashSet<NoInteracaoAnatomica>();
        var gruposVinculados = 0;
        var estruturasVinculadas = 0;
        foreach (var vinculo in vinculos)
        {
            var no = vinculo.GetComponent<NoInteracaoAnatomica>();
            exigir(no != null && no.VinculoAnatomico == vinculo, "o vínculo pertence ao próprio nó anatômico " + vinculo.name);
            exigir(!string.IsNullOrWhiteSpace(vinculo.CatalogoId), "o vínculo possui identidade de catálogo " + vinculo.name);
            exigir(idsCatalogoVinculos.Add(vinculo.CatalogoId), "identidade única de catálogo do vínculo " + vinculo.CatalogoId);
            exigir(oraculoPorCatalogoId.TryGetValue(vinculo.CatalogoId, out var registro2), "a identidade de catálogo do vínculo existe em AnatomicalOracle " + vinculo.CatalogoId);
            exigir(registro2.idCatalogo == vinculo.CatalogoId && oraculoChaves.Contains(registro2.chave), "o vínculo corresponde ao seu registro único de AnatomicalOracle " + vinculo.CatalogoId);
            exigir((vinculo.EhGrupo && registro2.tipo == "group") || (!vinculo.EhGrupo && registro2.tipo == "structure"), "o tipo do vínculo corresponde a AnatomicalOracle " + vinculo.CatalogoId);
            if (vinculo.EhGrupo)
                gruposVinculados++;
            else
            {
                estruturasVinculadas++;
                exigir(nosEstrutura.Add(no), "um vínculo de estrutura por nó anatômico " + vinculo.CatalogoId);
            }
        }

        exigir(gruposVinculados == 32 && estruturasVinculadas == 240, "32 grupos da cena e 240 estruturas da cena");
        exigir(idsCatalogoVinculos.SetEquals(oraculoPorCatalogoId.Keys), "as identidades de vínculo da cena correspondem exatamente a AnatomicalOracle");
        var nosFolha = new System.Collections.Generic.HashSet<NoInteracaoAnatomica>();
        foreach (var folha in folhas)
        {
            exigir(folha != null && folha.No != null && folha.No.VinculoAnatomico != null && !folha.No.VinculoAnatomico.EhGrupo, "o interagível da mão pertence a um nó de estrutura");
            exigir(nosFolha.Add(folha.No), "um interagível da mão por nó de estrutura " + folha.No.name);
        }

        exigir(nosFolha.SetEquals(nosEstrutura), "240 interagíveis das mãos cobrem exatamente os 240 nós de estrutura");
        exigir(expansaoGlobal.GarantirIndiceAtualizado() && expansaoGlobal.QuantidadeFolhasIndexadas == ControladorExpansaoGlobal.QuantidadeFolhasEsperada, "a expansão global indexa exatamente 240 folhas e 32 grupos");
        var folhasVerificadas = 0;
        var registrosVerificados = 0;
        bool? maoEstavaAtiva = null;
        try
        {
            redefinir.RestaurarTudo();
            foreach (var folha2 in folhas)
            {
                selecao.EntrarNo(folha2.No.NoPai);
                exigir(selecao.ResolverCandidato(folha2.No) == folha2.No, "candidato de folha " + folha2.No.name);
                selecao.SelecionarNo(folha2.No);
                if (selecao.NoSelecionado != folha2.No)
                    throw new InvalidOperationException("Não é possível selecionar " + folha2.No.name);
                if (folha2.colliders.Count != 1 || !(folha2.colliders[0] is MeshCollider malha) || malha.convex || malha.sharedMesh == null)
                    throw new InvalidOperationException("Falha ao associar o colisor exato " + folha2.No.name);
                if (malha != ObterColisorPreciso(folha2.GetComponent<InformacaoEstruturaAnatomica>()))
                    throw new InvalidOperationException("O colisor pertence a outra estrutura anatômica: " + folha2.No.name);
                folhasVerificadas++;
            }

            foreach (var vinculo2 in vinculos)
            {
                if (!OraculoAnatomico.TentarObter(vinculo2, out var registro3))
                    throw new InvalidOperationException("Oráculo não encontrado: " + vinculo2.CatalogoId);
                exigir(registro3.idCatalogo == vinculo2.CatalogoId && oraculoChaves.Contains(registro3.chave), "A consulta ao oráculo em execução preserva a identidade do vínculo" + vinculo2.CatalogoId);
                if (string.IsNullOrWhiteSpace(registro3.resumoDidatico) || string.IsNullOrWhiteSpace(OraculoAnatomico.ObterBibliografia(registro3)))
                    throw new InvalidOperationException("Texto didático ou bibliografia ausente" + registro3.chave);
                registrosVerificados++;
            }

            exigir(registrosVerificados == 272, "Os 272 registros do oráculo em execução foram verificados");
            redefinir.RestaurarTudo();
            var alvo = folhas[0];
            var raizCandidato = selecao.ResolverCandidato(alvo.No);
            exigir(raizCandidato != null && raizCandidato.NoPai == selecao.NoEmFoco, "Um acerto na raiz identifica o grupo filho direto");
            selecao.EntrarNo(alvo.No.NoPai);
            var raio = raiz.GetComponent<ControladorSelecaoPorRaio>();
            selecao.DefinirNoSobFoco(alvo.No, raio);
            selecao.DefinirNoSobFoco(alvo.No, alvo.No);
            raio.LimparFoco();
            exigir(selecao.EmFocoNo == alvo.No, "a limpeza do hover do controlador preserva o hover mais recente da mão no mesmo nó");
            ponte.LimparFoco(alvo.No);
            exigir(selecao.EmFocoNo == null, "a limpeza do hover da mão remove seu próprio destaque");
            if (!incluirCaminhoMaoInjetado)
            {
                return $"APROVADO no catálogo: {folhasVerificadas}/240 seleções de folhas e colisores exatos; {registrosVerificados}/272 registros do oráculo; hierarquia e propriedade do hover. CAMINHO FÍSICO/XRI dependente de rastreamento ignorado (sem headset).";
            }

            exigir(modalidade != null && modalidade.leftHand != null && modalidade.rightController != null && interacoes != null, "Modalidade de mão/controlador XRI ou gerenciador de interação ausente");
            var mao = modalidade.leftHand.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true);
            var controlador = modalidade.rightController.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor>(true);
            exigir(mao != null && controlador != null, "Referências de mão/controlador NearFarInteractor ausentes");
            maoEstavaAtiva = modalidade.leftHand.activeSelf;
            modalidade.leftHand.SetActive(true);
            exigir(ponte.PodeSelecionar(alvo, mao), "o caminho da mão rastreada aceita a seleção");
            exigir(!ponte.PodeSelecionar(alvo, controlador), "o caminho do controlador não duplica o tratamento de evento da mão");
            interacoes.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)mao, alvo);
            exigir(ponte.EstaAgarrando && selecao.NoSelecionado == alvo.No && manipulador.EstaAgarrando, "a seleção XRI inicia o agarramento da peça selecionada");
            var agarre = mao.GetAttachTransform(alvo);
            var iniciar = alvo.transform.position;
            var rotacao = alvo.transform.rotation;
            var inicioAgarre = agarre.position;
            var agarreRotacao = agarre.rotation;
            agarre.SetPositionAndRotation(inicioAgarre + Vector3.right * 0.05f, Quaternion.AngleAxis(30f, Vector3.up) * agarreRotacao);
            manipulador.AtualizarAgarre(agarre);
            exigir(Vector3.Distance(alvo.transform.position, iniciar) > 0.00001f || Quaternion.Angle(alvo.transform.rotation, rotacao) > 0.001f, "a pose de agarramento injetada move a peça selecionada");
            agarre.SetPositionAndRotation(inicioAgarre, agarreRotacao);
            interacoes.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)mao, alvo);
            exigir(!ponte.EstaAgarrando && !manipulador.EstaAgarrando, "a liberação XRI encerra o agarramento");
            manipulador.IniciarAgarre(agarre);
            var antesAlavanca = alvo.transform.position;
            var antesAlavancaRotacao = alvo.transform.rotation;
            manipulador.Manipular(Vector2.one);
            exigir(alvo.transform.position == antesAlavanca && alvo.transform.rotation == antesAlavancaRotacao, "o direcional analógico não pode alterar durante um agarramento controlado");
            selecao.LimparSelecao();
            manipulador.AtualizarAgarre(agarre);
            exigir(!manipulador.EstaAgarrando, "alterar a seleção cancela o agarramento antes do movimento");
            selecao.SelecionarNo(alvo.No);
            manipulador.IniciarAgarre(agarre);
            selecao.EntrarNo(raiz.GetComponent<NoInteracaoAnatomica>());
            manipulador.AtualizarAgarre(agarre);
            exigir(!manipulador.EstaAgarrando, "alterar o foco anatômico cancela o agarramento");
            selecao.EntrarNo(alvo.No.NoPai);
            selecao.SelecionarNo(alvo.No);
            interacoes.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)mao, alvo);
            modalidade.leftHand.SetActive(false);
            exigir(!ponte.EstaAgarrando && !manipulador.EstaAgarrando, "a desativação da mão/perda do modo de rastreamento cancela o agarramento");
            redefinir.RestaurarTudo();
            exigir(selecao.NoEmFoco == raiz.GetComponent<NoInteracaoAnatomica>() && selecao.NoSelecionado == null, "redefinir restaura o foco na raiz e limpa a seleção");
            var pulso = UnityEngine.Object.FindFirstObjectByType<PainelOraculoNoPulso>();
            var rolagem = pulso.GetComponentInChildren<ScrollRect>(true);
            exigir(rolagem.content.GetComponent<ContentSizeFitter>().verticalFit == ContentSizeFitter.FitMode.PreferredSize, "o layout de texto do pulso usa altura preferida");
            return $"APROVADO: {folhasVerificadas}/240 seleções de folhas e colisores exatos; {registrosVerificados}/272 registros do oráculo; hierarquia, agarramento/movimento/liberação XRI injetados, desativação da mão, propriedade do hover, layout do pulso e redefinição. O rastreamento físico das mãos e o conforto com headset não são simulados por estas verificações.";
        }
        finally
        {
            ponte.Soltar();
            if (maoEstavaAtiva.HasValue && modalidade != null && modalidade.leftHand != null)
            {
                modalidade.leftHand.SetActive(maoEstavaAtiva.Value);
            }

            redefinir.RestaurarTudo();
        }
    }

    public static MeshCollider ObterColisorPreciso(InformacaoEstruturaAnatomica folha)
    {
        if (folha == null)
            throw new InvalidOperationException("Elemento terminal anatômico ausente.");
        var controlados = folha.GetComponentsInChildren<MeshCollider>(true).Where(x => x.GetComponentInParent<InformacaoEstruturaAnatomica>() == folha).ToArray();
        var malhas = folha.GetComponentsInChildren<MeshFilter>(true).Where(x => x.GetComponentInParent<InformacaoEstruturaAnatomica>() == folha).ToArray();
        if (controlados.Length != 1 || malhas.Length != 1 || controlados[0].convex || controlados[0].sharedMesh == null || controlados[0].sharedMesh != malhas[0].sharedMesh || controlados[0].transform != malhas[0].transform)
            throw new InvalidOperationException("Esperado um colisor de malha exato na mesma transformação para " + folha.name);
        return controlados[0];
    }

    [MenuItem("Ferramentas/VR SNC/Mãos/Validar Execução em Tempo Real (Modo de Jogo)")]
    public static void ValidarExecucaoPeloMenu()
    {
        try
        {
            Debug.Log("[Mãos anatômicas] " + ValidarExecucao());
        }
        catch (Exception excecao)
        {
            Debug.LogException(excecao);
            throw;
        }
    }

    [MenuItem("Ferramentas/VR SNC/Mãos/Validar Catálogo Anatômico (Modo de Jogo)")]
    public static void ValidarCatalogoAnatomicoPeloMenu()
    {
        try
        {
            Debug.Log("[Mãos anatômicas] " + ValidarExecucao(false));
        }
        catch (Exception excecao)
        {
            Debug.LogException(excecao);
            throw;
        }
    }

    [MenuItem("Ferramentas/VR SNC/Mãos/Instalar Painel Nativo do Oráculo no Pulso")]
    public static void InstalarPainelNoPulso()
    {
        var cena = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer || cena.isDirty || cena.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Use a SampleScene salva, com o Editor ocioso no Modo de Edição.");
        if (cena.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PainelOraculoNoPulso>(true)).Any())
        {
            Debug.Log("[Anatomy Hands] Wrist oracle already installed; existing panel preserved.");
            return;
        }

        var selecao = cena.GetRootGameObjects().Single(x => x.name == "Sistema_Nervoso_Central").GetComponent<GerenciadorSelecaoHierarquica>();
        var modalidade = UnityEngine.Object.FindObjectsByType<XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.gameObject.scene == cena);
        const string caminhoPrefab = "Assets/Samples/XR Interaction Toolkit/3.4.1/Hands Interaction Demo/DemoAssets/Prefabs/UI/ScrollHandMenu.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(caminhoPrefab);
        if (prefab == null || modalidade.leftHand == null)
            throw new InvalidOperationException("O menu nativo da mão ou o rig rastreado não foi encontrado.");
        Directory.CreateDirectory("Docs/Anatomy/Backups");
        File.Copy(cena.path, "Docs/Anatomy/Backups/SampleScene-before-wrist-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity", false);
        Undo.IncrementCurrentGroup();
        var desfazerGrupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Instalar o painel nativo do oráculo no pulso");
        try
        {
            var instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cena);
            Undo.RegisterCreatedObjectUndo(instancia, "Criar uma instância nativa do menu da mão");
            // Desempacota apenas esta cópia nova; nunca altera o modelo-fonte nem a UI existente do usuário.
            PrefabUtility.UnpackPrefabInstance(instancia, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instancia.name = "Oraculo_Anatomico_Punho";
            instancia.transform.SetParent(modalidade.leftHand.transform.parent, false);
            var maoMenu = instancia.GetComponent<HandMenu>();
            maoMenu.hideMenuOnSelect = false;
            maoMenu.menuHandedness = HandMenu.MenuHandedness.Left;
            var rolagem = instancia.GetComponentInChildren<ScrollRect>(true);
            var conteudo = rolagem.content;
            var cabecalho = instancia.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "Header");
            var fonteTipografica = cabecalho.font;
            var painel = rolagem.transform.parent.GetComponent<RectTransform>();
            var arredondado = painel.GetComponent<Image>().sprite;
            painel.sizeDelta = new Vector2(440f, 520f);
            var canvas = instancia.GetComponentInChildren<Canvas>(true);
            canvas.transform.localScale = Vector3.one * 0.00065f;
            var cabecalhoRetangulo = cabecalho.rectTransform;
            cabecalhoRetangulo.anchorMin = new Vector2(0f, 1f);
            cabecalhoRetangulo.anchorMax = Vector2.one;
            cabecalhoRetangulo.offsetMin = new Vector2(20f, -84f);
            cabecalhoRetangulo.offsetMax = new Vector2(-20f, -12f);
            cabecalho.fontSize = 22f;
            cabecalho.enableAutoSizing = true;
            cabecalho.fontSizeMin = 18f;
            cabecalho.fontSizeMax = 22f;
            cabecalho.text = "Oráculo anatômico";
            cabecalho.raycastTarget = false;
            var rolagemRetangulo = rolagem.GetComponent<RectTransform>();
            rolagemRetangulo.anchorMin = Vector2.zero;
            rolagemRetangulo.anchorMax = Vector2.one;
            rolagemRetangulo.offsetMin = new Vector2(18f, 18f);
            rolagemRetangulo.offsetMax = new Vector2(-18f, -90f);
            foreach (Transform filho in conteudo.Cast<Transform>().ToArray())
                Undo.DestroyObjectImmediate(filho.gameObject);
            var organizacao = conteudo.GetComponent<VerticalLayoutGroup>();
            organizacao.childControlHeight = true;
            organizacao.childControlWidth = true;
            organizacao.childForceExpandHeight = false;
            organizacao.childForceExpandWidth = true;
            organizacao.spacing = 10f;
            // A amostra usa MinSize para itens de demonstração com altura fixa. Parágrafos TMP
            // dinâmicos precisam da altura preferida; caso contrário, todos os botões colapsam.
            conteudo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var resumo = Paragrafo("Explicacao_Didatica", conteudo, fonteTipografica, 21f);
            resumo.text = "Selecione uma estrutura para consultar sua ficha anatômica.";
            var botaoFonte = BotaoPulso("Exibir_Fonte", "Exibir fonte", conteudo, fonteTipografica, arredondado);
            var referencias = Paragrafo("Referencias_Bibliograficas", conteudo, fonteTipografica, 17f);
            referencias.gameObject.SetActive(false);
            var entrar = BotaoPulso("Entrar", "Entrar na estrutura", conteudo, fonteTipografica, arredondado);
            var voltar = BotaoPulso("Voltar", "Voltar ao grupo", conteudo, fonteTipografica, arredondado);
            var isolar = BotaoPulso("Isolar", "Isolar / restaurar", conteudo, fonteTipografica, arredondado);
            var controlador = Undo.AddComponent<PainelOraculoNoPulso>(instancia);
            controlador.Configurar(selecao, cabecalho, resumo, referencias, botaoFonte, entrar, voltar, isolar, rolagem);
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
                throw new IOException("Não foi possível salvar o painel do pulso.");
            Undo.CollapseUndoOperations(desfazerGrupo);
            Debug.Log("[Anatomy Hands] Native scroll Hand Menu installed: Portuguese didactic text, opt-in sources, enter/back/isolate. Original template assets preserved.");
        }
        catch
        {
            Undo.RevertAllDownToGroup(desfazerGrupo);
            throw;
        }
    }

    private static TMP_Text Paragrafo(string nome, Transform pai, TMP_FontAsset fonte, float tamanho)
    {
        var objeto = new GameObject(nome, typeof(RectTransform));
        objeto.layer = 5;
        objeto.transform.SetParent(pai, false);
        var texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.font = fonte;
        texto.fontSize = tamanho;
        texto.color = Color.white;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.overflowMode = TextOverflowModes.Overflow;
        texto.raycastTarget = false;
        texto.richText = false;
        return texto;
    }

    private static Button BotaoPulso(string nome, string rotulo, Transform pai, TMP_FontAsset fonte, Sprite arredondado)
    {
        var objeto = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        objeto.layer = 5;
        objeto.transform.SetParent(pai, false);
        var imagem = objeto.GetComponent<Image>();
        imagem.sprite = arredondado;
        imagem.type = Image.Type.Sliced;
        imagem.color = new Color(0.12f, 0.20f, 0.28f, 1f);
        objeto.GetComponent<LayoutElement>().preferredHeight = 40f;
        var texto = Paragrafo("Label", objeto.transform, fonte, 19f);
        texto.text = rotulo;
        texto.alignment = TextAlignmentOptions.Center;
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.offsetMin = new Vector2(8f, 4f);
        texto.rectTransform.offsetMax = new Vector2(-8f, -4f);
        return objeto.GetComponent<Button>();
    }

    [MenuItem("Ferramentas/VR SNC/Mãos/Conectar Rig de Mãos Existente")]
    public static void Aplicar()
    {
        var cena = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer || cena.isDirty || cena.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Use o Editor ocioso no Modo de Edição, com a SampleScene salva.");
        var raiz = cena.GetRootGameObjects().Single(x => x.name == "Sistema_Nervoso_Central");
        var modalidade = UnityEngine.Object.FindObjectsByType<XRInputModalityManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(x => x.gameObject.scene == cena);
        if (modalidade.leftHand == null || modalidade.rightHand == null || modalidade.leftController == null || modalidade.rightController == null)
            throw new InvalidOperationException("Primeiro, configure as referências de modalidade do template de mão e controle.");
        var folhas = raiz.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true);
        if (folhas.Length != 240 || folhas.Any(x => x.GetComponent<NoInteracaoAnatomica>() == null || x.GetComponentInChildren<MeshCollider>(true) == null))
            throw new InvalidOperationException("Aplique e valide os 240 vínculos anatômicos antes de conectar as mãos.");
        foreach (var folha in folhas)
            ObterColisorPreciso(folha);
        var gerenciador = raiz.GetComponent<GerenciadorSelecaoHierarquica>();
        var manipulador = raiz.GetComponent<ManipuladorObjetoSelecionado>();
        if (gerenciador == null || manipulador == null)
            throw new InvalidOperationException("Os gerenciadores de seleção ou manipulação anatômica não foram encontrados.");
        Directory.CreateDirectory("Docs/Anatomy/Backups");
        var backup = "Docs/Anatomy/Backups/SampleScene-before-hands-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity";
        File.Copy(cena.path, backup, false);
        Undo.IncrementCurrentGroup();
        var desfazerGrupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Conectar as mãos rastreadas existentes à anatomia");
        try
        {
            var ponte = raiz.GetComponent<PonteInteracaoMao>() ?? Undo.AddComponent<PonteInteracaoMao>(raiz);
            var expansaoGlobal = raiz.GetComponent<ControladorExpansaoGlobal>() ?? Undo.AddComponent<ControladorExpansaoGlobal>(raiz);
            var gestoCoordenador = raiz.GetComponent<CoordenadorGestoAnatomico>() ?? Undo.AddComponent<CoordenadorGestoAnatomico>(raiz);
            Undo.RecordObject(ponte, "Configurar a ponte entre a mão e a anatomia");
            Undo.RecordObject(expansaoGlobal, "Configurar o estado inicial da expansão global da anatomia");
            Undo.RecordObject(gestoCoordenador, "Calibrar o gesto global da anatomia");
            ponte.Configurar(gerenciador, manipulador, modalidade.leftHand.transform, modalidade.rightHand.transform);
            expansaoGlobal.Configurar(raiz.transform);
            var gestoPropriedades = new SerializedObject(gestoCoordenador);
            gestoPropriedades.FindProperty("separacaoZonaMorta").floatValue = 0.004f;
            gestoPropriedades.FindProperty("variacaoSeparacaoCompleta").floatValue = 0.18f;
            gestoPropriedades.ApplyModifiedProperties();
            EditorUtility.SetDirty(ponte);
            EditorUtility.SetDirty(gestoCoordenador);
            foreach (var folha2 in folhas)
            {
                var alvo = folha2.GetComponent<ElementoInteragivelMao>() ?? Undo.AddComponent<ElementoInteragivelMao>(folha2.gameObject);
                Undo.RecordObject(alvo, "Vincular o objeto interativo da mão ao colisor original da malha");
                alvo.Configurar(folha2.GetComponent<NoInteracaoAnatomica>(), ponte, ObterColisorPreciso(folha2));
                EditorUtility.SetDirty(alvo);
            }

            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
                throw new IOException("Não foi possível salvar as conexões da mão.");
            Undo.CollapseUndoOperations(desfazerGrupo);
            Debug.Log("[Anatomy Hands] Connected 240 exact mesh colliders to existing XRI hand interactions. Template tracking, controllers, materials and geometry preserved. Backup: " + backup);
        }
        catch
        {
            Undo.RevertAllDownToGroup(desfazerGrupo);
            throw;
        }
    }
}
