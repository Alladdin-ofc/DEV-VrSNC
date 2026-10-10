using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRSNC.Anatomia;
using VRSNC.Interacao;

/// <summary>Mapeamento aditivo da cena BP3D existente; não infere conteúdo científico.</summary>
public static class EditorMapeamentoAnatomico
{
    private const string CaminhoMenu = "Ferramentas/VR SNC/Anatomia/Aplicar e Exportar Mapeamento Validado";
    private const string CenaEsperada = "Assets/Scenes/SampleScene.unity";
    private const string NomeRaiz = "Sistema_Nervoso_Central";
    private const string DiretorioExportacao = "Docs/Anatomy";
    [MenuItem(CaminhoMenu)]
    public static void AplicarEExportar()
    {
        ExigirEditorPronto();
        var cena = SceneManager.GetActiveScene();
        if (cena.path != CenaEsperada)
            throw new InvalidOperationException("Abra a SampleScene antes de aplicar o mapeamento anatômico.");
        if (cena.isDirty)
            throw new InvalidOperationException("Salve ou revise as alterações atuais da cena antes de mapear a anatomia.");
        var raiz = cena.GetRootGameObjects().Single(objeto => objeto.name == NomeRaiz);
        var informacoes = raiz.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true).OrderBy(informacao => CaminhoDe(informacao.transform), StringComparer.Ordinal).ToArray();
        var grupos = raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true).Where(no => no.GetComponent<InformacaoEstruturaAnatomica>() == null).OrderBy(no2 => CaminhoDe(no2.transform), StringComparer.Ordinal).ToArray();
        if (informacoes.Length != 240 || grupos.Length != 32)
            throw new InvalidOperationException($"Eram esperadas 240 representações e 32 grupos; foram encontradas {informacoes.Length}/{grupos.Length}.");
        foreach (var informacao in informacoes)
        {
            var malhas = informacao.GetComponentsInChildren<MeshFilter>(true);
            if (malhas.Length != 1 || malhas[0].sharedMesh == null)
                throw new InvalidOperationException($"Era esperada uma malha para {CaminhoDe(informacao.transform)}.");
            var corpo = malhas[0].GetComponentInParent<Rigidbody>();
            if (corpo != null && !corpo.isKinematic)
                throw new InvalidOperationException($"Um corpo não cinemático rejeitaria um colisor de malha exato: {CaminhoDe(informacao.transform)}.");
        }

        var documento = CriarDocumento(cena, raiz, informacoes, grupos);
        if (documento.problemas.Any(problema => problema.gravidade == "error"))
        {
            Exportar(documento);
            throw new InvalidOperationException("A verificação preliminar da identidade da origem falhou. Consulte anatomical-mapping.json.");
        }

        var antes = AssinaturaGeometria(raiz);
        documento.validacao.assinaturaGeometriaAntes = antes;
        Directory.CreateDirectory(DiretorioExportacao);
        var pastaCopiaSeguranca = Path.Combine(DiretorioExportacao, "Backups");
        Directory.CreateDirectory(pastaCopiaSeguranca);
        documento.caminhoCopiaSegurancaCena = Path.Combine(pastaCopiaSeguranca, "SampleScene-before-mapping-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".unity").Replace('\\', '/');
        File.Copy(cena.path, documento.caminhoCopiaSegurancaCena, false);
        Undo.IncrementCurrentGroup();
        var desfazerGrupo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Mapear as estruturas BP3D sem alterar a anatomia");
        try
        {
            AplicarVinculos(raiz, informacoes, grupos, documento, out var nosAdicionados, out var colisoresAdicionados);
            ValidarGrafo(raiz, informacoes, grupos, documento);
            var grafoUmaVez = AssinaturaGrafo(raiz);
            AplicarVinculos(raiz, informacoes, grupos, documento, out var nosDuplicados, out var colisoresDuplicados);
            ValidarGrafo(raiz, informacoes, grupos, documento);
            documento.validacao.idempotenciaValidada = nosDuplicados == 0 && colisoresDuplicados == 0 && grafoUmaVez == AssinaturaGrafo(raiz);
            documento.validacao.nosFolhaAdicionados = nosAdicionados;
            documento.validacao.colisoresMalhaAdicionados = colisoresAdicionados;
            documento.validacao.assinaturaGeometriaDepois = AssinaturaGeometria(raiz);
            documento.validacao.invariantesGeometricasPreservadas = antes == documento.validacao.assinaturaGeometriaDepois;
            if (!documento.validacao.invariantesGeometricasPreservadas || !documento.validacao.idempotenciaValidada)
                throw new InvalidOperationException("A verificação de geometria ou de idempotência falhou; o mapeamento foi desfeito.");
            EditorSceneManager.MarkSceneDirty(cena);
            if (!EditorSceneManager.SaveScene(cena))
                throw new IOException("Não foi possível salvar a cena com o mapeamento aplicado.");
            Undo.CollapseUndoOperations(desfazerGrupo);
            documento.applied = true;
            Debug.Log($"[Mapeamento anatômico] APLICADO: {documento.validacao.quantidadeRepresentacoes} estruturas, {documento.validacao.quantidadeGrupos} grupos, " + $"{documento.validacao.quantidadeConceitos} concepts; {nosAdicionados} novos elementos terminais, {colisoresAdicionados} novos colisores de malha exatos. " + "Geometry unchanged; graph and idempotence validated. Export: Docs/Anatomy/anatomical-mapping.json");
        }
        catch
        {
            Undo.RevertAllDownToGroup(desfazerGrupo);
            throw;
        }

        // Uma falha na exportação não deve desfazer apenas a cena ativa depois de salvá-la em disco.
        Exportar(documento);
    }

    [MenuItem("Ferramentas/VR SNC/Anatomia/Validar Mapeamento Atual")]
    public static void ValidarAtual()
    {
        ExigirEditorPronto();
        var raiz = SceneManager.GetActiveScene().GetRootGameObjects().Single(objeto2 => objeto2.name == NomeRaiz);
        var informacoes = raiz.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true);
        var grupos = raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true).Where(n => n.EstruturaInformacao == null).ToArray();
        var documento = CriarDocumento(SceneManager.GetActiveScene(), raiz, informacoes, grupos);
        if (documento.problemas.Any(problema2 => problema2.gravidade == "error"))
            throw new InvalidOperationException("A validação da identidade da origem falhou; verifique os problemas do mapeamento.");
        ValidarGrafo(raiz, informacoes, grupos, documento);
        Debug.Log($"[Mapeamento anatômico] VALIDADO: {informacoes.Length} own-structure bindings, {grupos.Length} independent grupos, {documento.conceitos.Count} concepts.");
    }

    private static void ExigirEditorPronto()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Para mapear a anatomia, o Editor deve estar ocioso no Modo de Edição, sem compilação ou atualização de assets em andamento.");
    }

    private static DocumentoMapeamentoAnatomico CriarDocumento(Scene cena, GameObject raiz, InformacaoEstruturaAnatomica[] informacoes, NoInteracaoAnatomica[] grupos)
    {
        var documento = new DocumentoMapeamentoAnatomico { geradoEmUtc = DateTime.UtcNow.ToString("o"), caminhoCena = cena.path, caminhoRaiz = CaminhoDe(raiz.transform) };
        var grupoDefinir = new HashSet<NoInteracaoAnatomica>(grupos);
        foreach (var informacao in informacoes)
        {
            var malha = informacao.GetComponentInChildren<MeshFilter>(true).sharedMesh;
            var caminhoRecurso = AssetDatabase.GetAssetPath(malha);
            var cabecalho = LerOBJCabecalho(caminhoRecurso);
            var grupo = GrupoMaisProximo(informacao.transform.parent, grupoDefinir);
            var entrada = new RegistroRepresentacaoAnatomica
            {
                idObjetoCena = ObjetoId(informacao.gameObject),
                caminhoCena = CaminhoDe(informacao.transform),
                nomeOriginalObjeto = informacao.gameObject.name,
                nomeAnatomicoOriginal = informacao.NomeAnatomico,
                nomeAnatomicoExibido = VinculoAnatomico.CorrigirGrafiaExibida(informacao.NomeAnatomico),
                nomePortuguesOriginal = informacao.NomePortugues,
                nomePortuguesExibido = VinculoAnatomico.CorrigirGrafiaExibida(informacao.NomePortugues),
                nomeOriginalObjetoFonte = informacao.NomeObjetoOrigem,
                caminhoCenaOriginal = informacao.CaminhoCenaOriginal,
                grupoPaiOriginal = informacao.GrupoPaiOriginal,
                caminhoRecurso = caminhoRecurso,
                guidRecurso = AssetDatabase.AssetPathToGUID(caminhoRecurso),
                idArquivoObj = Cabecalho(cabecalho, "File ID"),
                idRepresentacaoObj = Cabecalho(cabecalho, "Representation ID"),
                idConceitoFma = Cabecalho(cabecalho, "Concept ID"),
                nomeInglesObj = Cabecalho(cabecalho, "English name"),
                logicaComposicaoObj = Cabecalho(cabecalho, "Build-up logic"),
                versaoCompatibilidadeObj = Cabecalho(cabecalho, "Compatibility version"),
                idGrupo = grupo != null ? ObjetoId(grupo.gameObject) : "",
                caminhoGrupo = grupo != null ? CaminhoDe(grupo.transform) : "",
                divisaoPrincipalSerializada = NomeSerializado(informacao.DivisaoPrincipal),
                subdivisaoSerializada = NomeSerializado(informacao.Subdivisao),
                grupoAnatomicoSerializado = NomeSerializado(informacao.GrupoAnatomico),
                loboCorticalSerializado = NomeSerializado(informacao.LoboCortical),
                ladoSerializado = NomeSerializado(informacao.HemisferioOuEspacialDivisao),
                estadoRevisaoSerializado = NomeSerializado(informacao.EstadoRevisao),
                divisaoCatalogoFonte = informacao.DivisaoCatalogoOrigem,
                subdivisaoCatalogoFonte = informacao.SubdivisaoCatalogoOrigem,
                descricaoBreveLegada = informacao.DescricaoBreve,
                descricaoFuncionalLegada = informacao.DescricaoFuncional
            };
            entrada.chaveRepresentacao = string.Join("|", entrada.guidRecurso, entrada.idArquivoObj, entrada.idRepresentacaoObj, entrada.idConceitoFma);
            documento.representacoes.Add(entrada);
            if (grupo == null)
                Problema(documento, entrada, "MISSING_GROUP", "error", "No existing anatomical group ancestor.");
            if (string.IsNullOrEmpty(entrada.guidRecurso) || string.IsNullOrEmpty(entrada.idArquivoObj) || string.IsNullOrEmpty(entrada.idRepresentacaoObj) || string.IsNullOrEmpty(entrada.idConceitoFma))
                Problema(documento, entrada, "INCOMPLETE_SOURCE_IDENTITY", "error", "Missing GUID or required OBJ header identifier.");
            if (entrada.nomeAnatomicoOriginal != entrada.nomeAnatomicoExibido || entrada.nomePortuguesOriginal != entrada.nomePortuguesExibido)
                Problema(documento, entrada, "DISPLAY_SPELLING_CORRECTED", "info", "User-confirmed rigth spelling corrected only for display; raw source text preserved.");
            if (!string.Equals(VinculoAnatomico.CorrigirGrafiaExibida(informacao.gameObject.name), entrada.nomeInglesObj, StringComparison.OrdinalIgnoreCase))
                Problema(documento, entrada, "SCENE_NAME_DIFFERS_FROM_OBJ", "warning", "Scene label differs from actual mesh OBJ English header; relationship retained for review.");
            if (!string.Equals(informacao.NomeObjetoOrigem, Path.GetFileName(caminhoRecurso), StringComparison.OrdinalIgnoreCase))
                Problema(documento, entrada, "CATALOG_SOURCE_DIFFERS_FROM_MESH", "warning", "Serialized sourceObjectName differs from referenced mesh asset; neither was changed.");
            if (string.IsNullOrWhiteSpace(informacao.NomePortugues))
                Problema(documento, entrada, "PORTUGUESE_NAME_PENDING", "warning", "No verified Portuguese label is stored; translation was not generated.");
            if (informacao.HemisferioOuEspacialDivisao == LadoAnatomico.NaoEspecificado)
                Problema(documento, entrada, "SIDE_UNSPECIFIED", "warning", "Serialized laterality is unspecified; no side was inferred from names.");
        }

        foreach (var colisao in documento.representacoes.GroupBy(r => r.chaveRepresentacao).Where(g => g.Count() > 1))
            foreach (var entrada2 in colisao)
                Problema(documento, entrada2, "DUPLICATE_REPRESENTATION_KEY", "error", "Composite source identity is not unique in this scene.");
        foreach (var grupo2 in grupos)
        {
            var pai = GrupoMaisProximo(grupo2.transform.parent, grupoDefinir);
            var registro = new RegistroGrupoAnatomico
            {
                idGrupo = ObjetoId(grupo2.gameObject),
                caminhoCena = CaminhoDe(grupo2.transform),
                nomeNoOriginal = grupo2.NomeNo,
                nomeExibicao = grupo2.NomeExibicao,
                idGrupoPai = pai != null ? ObjetoId(pai.gameObject) : "",
                nivel = grupo2.NivelNo
            };
            var metadadosGrupo = grupo2.GetComponent<GrupoAnatomico>();
            if (metadadosGrupo != null)
            {
                registro.divisaoPrincipalSerializada = NomeSerializado(metadadosGrupo.DivisaoPrincipal);
                registro.subdivisaoSerializada = NomeSerializado(metadadosGrupo.Subdivisao);
            }

            registro.idsGruposFilhosDiretos.AddRange(grupos.Where(g2 => GrupoMaisProximo(g2.transform.parent, grupoDefinir) == grupo2).Select(g3 => ObjetoId(g3.gameObject)));
            registro.chavesRepresentacoesDiretas.AddRange(documento.representacoes.Where(r2 => r2.idGrupo == registro.idGrupo).Select(r3 => r3.chaveRepresentacao));
            registro.chavesRepresentacoesDescendentes.AddRange(documento.representacoes.Where(r4 => r4.caminhoCena.StartsWith(registro.caminhoCena + "/", StringComparison.Ordinal)).Select(r5 => r5.chaveRepresentacao));
            documento.grupos.Add(registro);
            if (registro.chavesRepresentacoesDescendentes.Count == 0)
                documento.problemas.Add(new ProblemaMapeamentoAnatomico { codigo = "EMPTY_GROUP", gravidade = "warning", idAlvo = registro.idGrupo, mensagem = "Existing group has no representation; group and name are preserved." });
        }

        foreach (var conceito in documento.representacoes.GroupBy(r6 => r6.idConceitoFma).OrderBy(g4 => g4.Key, StringComparer.Ordinal))
        {
            var registro2 = new RegistroConceitoAnatomico { chaveConceito = conceito.Key, idConceitoFma = conceito.Key };
            registro2.nomesInglesObj.AddRange(conceito.Select(r7 => r7.nomeInglesObj).Distinct().OrderBy(n2 => n2, StringComparer.Ordinal));
            registro2.chavesRepresentacoes.AddRange(conceito.Select(r8 => r8.chaveRepresentacao));
            documento.conceitos.Add(registro2);
        }

        documento.validacao.quantidadeRepresentacoes = informacoes.Length;
        documento.validacao.quantidadeGrupos = grupos.Length;
        documento.validacao.quantidadeConceitos = documento.conceitos.Count;
        documento.validacao.quantidadeErros = documento.problemas.Count(i => i.gravidade == "error");
        documento.validacao.quantidadeAvisos = documento.problemas.Count(i => i.gravidade == "warning");
        return documento;
    }

    private static void AplicarVinculos(GameObject raiz, InformacaoEstruturaAnatomica[] informacoes, NoInteracaoAnatomica[] grupos, DocumentoMapeamentoAnatomico documento, out int nosAdicionados, out int colisoresAdicionados)
    {
        nosAdicionados = 0;
        colisoresAdicionados = 0;
        var registros = documento.representacoes.ToDictionary(r9 => r9.idObjetoCena);
        foreach (var informacao in informacoes)
        {
            var no = informacao.GetComponent<NoInteracaoAnatomica>();
            if (no == null)
            {
                no = Undo.AddComponent<NoInteracaoAnatomica>(informacao.gameObject);
                nosAdicionados++;
            }

            var filtroMalha = informacao.GetComponentInChildren<MeshFilter>(true);
            if (filtroMalha.GetComponent<Collider>() == null)
            {
                var colisor = Undo.AddComponent<MeshCollider>(filtroMalha.gameObject);
                colisor.sharedMesh = filtroMalha.sharedMesh;
                colisor.convex = false;
                colisor.isTrigger = false;
                Marcar(colisor);
                colisoresAdicionados++;
            }

            var entrada = registros[ObjetoId(informacao.gameObject)];
            var vinculo = informacao.GetComponent<VinculoAnatomico>() ?? Undo.AddComponent<VinculoAnatomico>(informacao.gameObject);
            Undo.RecordObject(vinculo, "Definir a identidade da origem BP3D");
            vinculo.Configurar(false, entrada.chaveRepresentacao, entrada.idConceitoFma, entrada.nomeInglesObj, VinculoAnatomico.CorrigirGrafiaExibida(entrada.nomeInglesObj), new[] { informacao });
            Marcar(vinculo);
        }

        var nos = raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true);
        foreach (var no2 in nos.OrderBy(n3 => Profundidade(n3.transform)))
        {
            var pai = NoMaisProximo(no2.transform.parent);
            if (no2.gameObject == raiz)
                pai = null;
            var filhos = nos.Where(filho => filho != no2 && NoMaisProximo(filho.transform.parent) == no2).ToArray();
            var informacao2 = no2.GetComponent<InformacaoEstruturaAnatomica>();
            var vinculo2 = no2.GetComponent<VinculoAnatomico>() ?? Undo.AddComponent<VinculoAnatomico>(no2.gameObject);
            if (informacao2 == null)
            {
                Undo.RecordObject(vinculo2, "Definir os membros do grupo");
                vinculo2.Configurar(true, ObjetoId(no2.gameObject), "", "", "", no2.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true));
                Marcar(vinculo2);
            }

            var nomeExibicao = informacao2 == null ? no2.NomeExibicao : !string.IsNullOrWhiteSpace(informacao2.NomePortugues) ? VinculoAnatomico.CorrigirGrafiaExibida(informacao2.NomePortugues) : VinculoAnatomico.CorrigirGrafiaExibida(informacao2.NomeAnatomico);
            Undo.RecordObject(no2, "Definir vínculos anatômicos diretos");
            no2.Configurar(no2.NomeNo, nomeExibicao, pai != null ? pai.NivelNo + 1 : 0, pai, filhos, no2.EhSelecionavel, no2.EhExpansivel, no2.gameObject == raiz);
            no2.VincularAnatomia(informacao2, vinculo2);
            Marcar(no2);
        }
    }

    private static void ValidarGrafo(GameObject raiz, InformacaoEstruturaAnatomica[] informacoes, NoInteracaoAnatomica[] grupos, DocumentoMapeamentoAnatomico documento)
    {
        var nos = raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true);
        if (nos.Length != informacoes.Length + grupos.Length)
            throw new InvalidOperationException("A quantidade de nós de interação é diferente da esperada.");
        foreach (var informacao in informacoes)
        {
            var no = informacao.GetComponent<NoInteracaoAnatomica>();
            if (no == null || no.EstruturaInformacao != informacao || no.NosFilhos.Count != 0 || no.NoPai == null || !no.NoPai.NosFilhos.Contains(no) || no.VinculoAnatomico == null || no.VinculoAnatomico.EhGrupo)
                throw new InvalidOperationException("Vínculo inválido para o elemento terminal: " + CaminhoDe(informacao.transform));
            if (informacao.GetComponentInChildren<Collider>(true) == null)
                throw new InvalidOperationException("Não há colisor selecionável em: " + CaminhoDe(informacao.transform));
        }

        foreach (var grupo in grupos)
        {
            if (grupo.EstruturaInformacao != null || grupo.VinculoAnatomico == null || !grupo.VinculoAnatomico.EhGrupo)
                throw new InvalidOperationException("O grupo recebeu metadados que pertencem a uma estrutura individual: " + grupo.NomeNo);
            var esperado = grupo.GetComponentsInChildren<InformacaoEstruturaAnatomica>(true);
            if (!new HashSet<InformacaoEstruturaAnatomica>(esperado).SetEquals(grupo.VinculoAnatomico.MembroEstruturas))
                throw new InvalidOperationException("A lista de membros do grupo está incompleta: " + grupo.NomeNo);
        }

        foreach (var no2 in nos)
        {
            if (no2.NosFilhos.Distinct().Count() != no2.NosFilhos.Count || no2.NosFilhos.Any(filho2 => filho2.NoPai != no2))
                throw new InvalidOperationException("A relação entre pai e filho está inconsistente: " + no2.NomeNo);
            var visitados = new HashSet<NoInteracaoAnatomica>();
            for (var atual = no2; atual != null; atual = atual.NoPai)
                if (!visitados.Add(atual))
                    throw new InvalidOperationException("Foi encontrado um ciclo na hierarquia anatômica.");
        }

        documento.validacao.quantidadeNosInteracao = nos.Length;
        documento.validacao.vinculosDiretos = informacoes.Count(informacao2 => informacao2.GetComponent<NoInteracaoAnatomica>().EstruturaInformacao == informacao2);
        documento.validacao.representacoesComColisor = informacoes.Count(informacao3 => informacao3.GetComponentInChildren<Collider>(true) != null);
        documento.validacao.grafoValidado = true;
    }

    private static void Exportar(DocumentoMapeamentoAnatomico documento)
    {
        Directory.CreateDirectory(DiretorioExportacao);
        File.WriteAllText(Path.Combine(DiretorioExportacao, "anatomical-mapping.json"), JsonUtility.ToJson(documento, true), new UTF8Encoding(false));
        var campos = typeof(RegistroRepresentacaoAnatomica).GetFields().Where(f => f.FieldType == typeof(string)).ToArray();
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", campos.Select(f2 => CSV(f2.Name)).Concat(new[] { CSV("problemas") })));
        foreach (var entrada in documento.representacoes)
            csv.AppendLine(string.Join(",", campos.Select(f3 => CSV((string)f3.GetValue(entrada))).Concat(new[] { CSV(string.Join(";", entrada.problemas)) })));
        File.WriteAllText(Path.Combine(DiretorioExportacao, "anatomical-representations.csv"), csv.ToString(), new UTF8Encoding(true));
        csv.Clear().AppendLine("groupId,scenePath,nodeNameRaw,displayName,parentGroupId,level,directChildGroupIds,directRepresentationKeys,descendantRepresentationKeys,contentStatus,approvedDescription");
        foreach (var grupo in documento.grupos)
            csv.AppendLine(string.Join(",", new[] { grupo.idGrupo, grupo.caminhoCena, grupo.nomeNoOriginal, grupo.nomeExibicao, grupo.idGrupoPai, grupo.nivel.ToString(CultureInfo.InvariantCulture), string.Join(";", grupo.idsGruposFilhosDiretos), string.Join(";", grupo.chavesRepresentacoesDiretas), string.Join(";", grupo.chavesRepresentacoesDescendentes), grupo.estadoConteudo, grupo.descricaoAprovada }.Select(CSV)));
        File.WriteAllText(Path.Combine(DiretorioExportacao, "anatomical-groups.csv"), csv.ToString(), new UTF8Encoding(true));
    }

    private static Dictionary<string, string> LerOBJCabecalho(string caminho)
    {
        var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!caminho.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            return resultado;
        foreach (var linha in File.ReadLines(caminho))
        {
            if (!linha.StartsWith("#", StringComparison.Ordinal))
                break;
            var separador = linha.IndexOf(':');
            if (separador > 1)
                resultado[linha.Substring(1, separador - 1).Trim()] = linha.Substring(separador + 1).Trim();
        }

        return resultado;
    }

    private static string Cabecalho(Dictionary<string, string> cabecalho, string chave) => cabecalho.TryGetValue(chave, out var valor) ? valor : "";
    private static string CSV(string valor) => "\"" + (valor ?? "").Replace("\"", "\"\"") + "\"";
    private static string ObjetoId(UnityEngine.Object valor) => GlobalObjectId.GetGlobalObjectIdSlow(valor).ToString();
    private static NoInteracaoAnatomica NoMaisProximo(Transform transformacao) => transformacao != null ? transformacao.GetComponentInParent<NoInteracaoAnatomica>(true) : null;
    private static NoInteracaoAnatomica GrupoMaisProximo(Transform transformacao, HashSet<NoInteracaoAnatomica> grupos)
    {
        for (var atual = transformacao; atual != null; atual = atual.parent)
        {
            var no = atual.GetComponent<NoInteracaoAnatomica>();
            if (no != null && grupos.Contains(no))
                return no;
        }

        return null;
    }

    private static string CaminhoDe(Transform transformacao)
    {
        var partes = new Stack<string>();
        for (var atual = transformacao; atual != null; atual = atual.parent)
            partes.Push(atual.name);
        return string.Join("/", partes);
    }

    private static int Profundidade(Transform transformacao)
    {
        var contagem = 0;
        for (var pai = transformacao.parent; pai != null; pai = pai.parent)
            contagem++;
        return contagem;
    }

    private static void Marcar(UnityEngine.Object alvo)
    {
        EditorUtility.SetDirty(alvo);
        if (PrefabUtility.IsPartOfPrefabInstance(alvo))
            PrefabUtility.RecordPrefabInstancePropertyModifications(alvo);
    }

    private static void Problema(DocumentoMapeamentoAnatomico documento, RegistroRepresentacaoAnatomica entrada, string codigo, string gravidade, string mensagem)
    {
        entrada.problemas.Add(codigo);
        documento.problemas.Add(new ProblemaMapeamentoAnatomico { codigo = codigo, gravidade = gravidade, idAlvo = entrada.chaveRepresentacao, mensagem = mensagem });
    }

    private static string AssinaturaGeometria(GameObject raiz)
    {
        var construtor = new StringBuilder();
        foreach (var transformacao in raiz.GetComponentsInChildren<Transform>(true).OrderBy(t => ObjetoId(t), StringComparer.Ordinal))
        {
            construtor.Append(ObjetoId(transformacao)).Append('|').Append(CaminhoDe(transformacao)).Append('|').Append(transformacao.localPosition.ToString("R")).Append('|').Append(transformacao.localRotation.ToString("R")).Append('|').Append(transformacao.localScale.ToString("R")).Append('|').Append(transformacao.gameObject.activeSelf).AppendLine();
        }

        var fonteCaminhos = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var malha in raiz.GetComponentsInChildren<MeshFilter>(true).OrderBy(m => ObjetoId(m), StringComparer.Ordinal))
        {
            var fonte = AssetDatabase.GetAssetPath(malha.sharedMesh);
            construtor.Append(ObjetoId(malha)).Append('|').Append(ObjetoId(malha.sharedMesh)).AppendLine();
            if (!string.IsNullOrEmpty(fonte))
                fonteCaminhos.Add(fonte);
        }

        foreach (var renderizador in raiz.GetComponentsInChildren<Renderer>(true).OrderBy(r10 => ObjetoId(r10), StringComparer.Ordinal))
        {
            construtor.Append(ObjetoId(renderizador)).Append('|').Append(renderizador.enabled).Append('|');
            foreach (var material in renderizador.sharedMaterials)
                construtor.Append(material != null ? ObjetoId(material) : "null").Append(';');
            construtor.AppendLine();
        }

        using (var sha = SHA256.Create())
        {
            foreach (var caminho in fonteCaminhos)
                using (var fluxo = File.OpenRead(caminho))
                    construtor.Append(caminho).Append('|').Append(Convert.ToBase64String(sha.ComputeHash(fluxo))).AppendLine();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(construtor.ToString())));
        }
    }

    private static string AssinaturaGrafo(GameObject raiz)
    {
        return string.Join("\n", raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true).OrderBy(n4 => CaminhoDe(n4.transform), StringComparer.Ordinal).Select(n5 => CaminhoDe(n5.transform) + "|" + n5.NomeExibicao + "|" + n5.NivelNo + "|" + (n5.NoPai != null ? CaminhoDe(n5.NoPai.transform) : "") + "|" + n5.VinculoAnatomico.CatalogoId + "|" + string.Join(";", n5.NosFilhos.Select(candidato => CaminhoDe(candidato.transform)))));
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(DivisaoAnatomicaPrincipal valor)
    {
        switch (valor)
        {
            case DivisaoAnatomicaPrincipal.Desconhecido:
                return "Unknown";
            case DivisaoAnatomicaPrincipal.Cerebro:
                return "Cerebro";
            case DivisaoAnatomicaPrincipal.Telencefalo:
                return "Telencefalo";
            case DivisaoAnatomicaPrincipal.Diencefalo:
                return "Diencefalo";
            case DivisaoAnatomicaPrincipal.Cerebelo:
                return "Cerebelo";
            case DivisaoAnatomicaPrincipal.TroncoEncefalico:
                return "TroncoEncefalico";
            case DivisaoAnatomicaPrincipal.SistemaVentricular:
                return "SistemaVentricular";
            case DivisaoAnatomicaPrincipal.OutrosRevisao:
                return "OutrosRevisao";
            default:
                return valor.ToString();
        }
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(SubdivisaoAnatomica valor)
    {
        switch (valor)
        {
            case SubdivisaoAnatomica.Desconhecido:
                return "Unknown";
            case SubdivisaoAnatomica.CortexCerebral:
                return "CortexCerebral";
            case SubdivisaoAnatomica.FormacaoHipocampal:
                return "FormacaoHipocampal";
            case SubdivisaoAnatomica.SistemaLimbico:
                return "SistemaLimbico";
            case SubdivisaoAnatomica.GangliosDaBase:
                return "GangliosDaBase";
            case SubdivisaoAnatomica.SistemaOlfatorio:
                return "SistemaOlfatorio";
            case SubdivisaoAnatomica.SubstanciaBrancaComissurasETratos:
                return "SubstanciaBrancaComissurasETratos";
            case SubdivisaoAnatomica.AreaSeptalEProsencefaloBasal:
                return "AreaSeptalEProsencefaloBasal";
            case SubdivisaoAnatomica.Talamo:
                return "Talamo";
            case SubdivisaoAnatomica.Hipotalamo:
                return "Hipotalamo";
            case SubdivisaoAnatomica.Epitalamo:
                return "Epitalamo";
            case SubdivisaoAnatomica.Subtalamo:
                return "Subtalamo";
            case SubdivisaoAnatomica.Cerebelo:
                return "Cerebelo";
            case SubdivisaoAnatomica.Mesencefalo:
                return "Mesencefalo";
            case SubdivisaoAnatomica.Ponte:
                return "Ponte";
            case SubdivisaoAnatomica.BulboMedulaOblonga:
                return "BulboMedulaOblonga";
            case SubdivisaoAnatomica.VentriculosLaterais:
                return "VentriculosLaterais";
            case SubdivisaoAnatomica.TerceiroVentriculo:
                return "TerceiroVentriculo";
            case SubdivisaoAnatomica.AquedutoCerebral:
                return "AquedutoCerebral";
            case SubdivisaoAnatomica.QuartoVentriculo:
                return "QuartoVentriculo";
            case SubdivisaoAnatomica.PlexoCoroide:
                return "PlexoCoroide";
            case SubdivisaoAnatomica.NaoClassificado:
                return "NaoClassificado";
            case SubdivisaoAnatomica.RevisaoAnatomicaNecessaria:
                return "RevisaoAnatomicaNecessaria";
            default:
                return valor.ToString();
        }
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(SistemaAnatomico valor)
    {
        switch (valor)
        {
            case SistemaAnatomico.NaoEspecificado:
                return "Unspecified";
            case SistemaAnatomico.Cortical:
                return "Cortical";
            case SistemaAnatomico.Limbico:
                return "Limbic";
            case SistemaAnatomico.Motor:
                return "Motor";
            case SistemaAnatomico.Sensorial:
                return "Sensory";
            case SistemaAnatomico.Olfatorio:
                return "Olfactory";
            case SistemaAnatomico.Ventricular:
                return "Ventricular";
            case SistemaAnatomico.SubstanciaBranca:
                return "WhiteMatter";
            case SistemaAnatomico.GangliosDaBase:
                return "BasalGanglia";
            case SistemaAnatomico.Diencefalico:
                return "Diencephalic";
            case SistemaAnatomico.TroncoEncefalico:
                return "Brainstem";
            case SistemaAnatomico.Cerebelar:
                return "Cerebellar";
            case SistemaAnatomico.RevisaoNecessaria:
                return "ReviewRequired";
            default:
                return valor.ToString();
        }
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(LoboCortical valor)
    {
        switch (valor)
        {
            case LoboCortical.Nenhum:
                return "None";
            case LoboCortical.Frontal:
                return "Frontal";
            case LoboCortical.Parietal:
                return "Parietal";
            case LoboCortical.Temporal:
                return "Temporal";
            case LoboCortical.Occipital:
                return "Occipital";
            case LoboCortical.Insula:
                return "Insula";
            case LoboCortical.Limbico:
                return "Limbic";
            case LoboCortical.Occipitotemporal:
                return "Occipitotemporal";
            case LoboCortical.RevisaoNecessaria:
                return "ReviewRequired";
            default:
                return valor.ToString();
        }
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(LadoAnatomico valor)
    {
        switch (valor)
        {
            case LadoAnatomico.NaoEspecificado:
                return "Unspecified";
            case LadoAnatomico.Esquerda:
                return "Left";
            case LadoAnatomico.Direita:
                return "Right";
            case LadoAnatomico.LinhaMedia:
                return "Midline";
            case LadoAnatomico.Bilateral:
                return "Bilateral";
            case LadoAnatomico.RevisaoNecessaria:
                return "ReviewRequired";
            default:
                return valor.ToString();
        }
    }

    // Preserva o valor JSON legado.
    private static string NomeSerializado(EstadoRevisaoEstrutura valor)
    {
        switch (valor)
        {
            case EstadoRevisaoEstrutura.Classificado:
                return "Classified";
            case EstadoRevisaoEstrutura.NaoEmCatalogo:
                return "NotInCatalog";
            case EstadoRevisaoEstrutura.AusenteEmCena:
                return "MissingInScene";
            case EstadoRevisaoEstrutura.DuplicadoNome:
                return "DuplicateName";
            case EstadoRevisaoEstrutura.RequerRevisao:
                return "NeedsReview";
            default:
                return valor.ToString();
        }
    }
}