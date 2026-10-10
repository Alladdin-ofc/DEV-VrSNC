using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Comandos de compilação repetíveis, priorizando o headset, para o perfil de desenvolvimento Quest.
/// O primeiro comando cria o APK-base; iterações posteriores usam o caminho nativo do Unity
/// PatchPackage + BuildScriptsOnly + AutoRunPlayer e implantam diretamente no
/// headset selecionado. O comando de patch é intencionalmente só de código: mudanças em cena,
/// ativo, pacote, shader, plugin nativo e Configurações do Player exigem uma nova compilação-base.
/// </summary>
public static class CompilarAplicarEExecutarQuest
{
    private const string AplicativoIdentificador = "com.vrsnc.tcc2026";
    private const string CaminhoRelativoSaida = "Builds/Quest/VR-SNC-dev.apk";
    private const string CaminhoRelativoSaidaApresentacao = "Builds/Quest/VR-SNC-release.apk";
    private static string CaminhoTemporarioGradleWindows => Path.Combine(Path.GetTempPath(), "VRSNC");
    [MenuItem("Ferramentas/VR SNC/Quest/01 Compilar e Executar (base)")]
    public static void CompilarEExecutarVersaoBase()
    {
        CompilacaoEExecutar(corrigirPlayerExistente: false, versaoApresentacao: false);
    }

    [MenuItem("Ferramentas/VR SNC/Quest/02 Corrigir e Executar")]
    public static void AplicarAjustesEExecutar()
    {
        CompilacaoEExecutar(corrigirPlayerExistente: true, versaoApresentacao: false);
    }

    [MenuItem("Ferramentas/VR SNC/Quest/03 Compilar Versão de Apresentação")]
    public static void CompilarVersaoDeApresentacao()
    {
        CompilacaoEExecutar(corrigirPlayerExistente: false, versaoApresentacao: true);
    }

    [MenuItem("Ferramentas/VR SNC/Quest/Reparar Sistema de Eventos")]
    public static void RepararSistemaDeEventos()
    {
        CorrigirEventoSistemaESalvar(registrarResultado: true);
    }

    private static void CompilacaoEExecutar(bool corrigirPlayerExistente, bool versaoApresentacao)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new BuildFailedException("Saia do Modo de Jogo antes de compilar para Quest.");
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
        {
            throw new BuildFailedException("Unity não conseguiu alternar o destino de compilação ativo para Android.");
        }

        string[] cenas = EditorBuildSettings.scenes.Where(cena => cena.enabled).Select(cena2 => cena2.path).ToArray();
        if (cenas.Length == 0)
        {
            throw new BuildFailedException("Nenhuma cena habilitada foi encontrada nas Configurações de Compilação.");
        }

        CorrigirEventoSistemaESalvar(registrarResultado: false);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AplicativoIdentificador);
        PlayerSettings.productName = "VR SNC";
        PlayerSettings.stripEngineCode = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        string projetoRaiz = Directory.GetParent(Application.dataPath)?.FullName ?? throw new BuildFailedException("Não foi possível resolver a raiz do projeto Unity.");
        string caminhoRelativoSaida = versaoApresentacao ? CaminhoRelativoSaidaApresentacao : CaminhoRelativoSaida;
        string caminhoApk = Path.GetFullPath(Path.Combine(projetoRaiz, caminhoRelativoSaida));
        Directory.CreateDirectory(Path.GetDirectoryName(caminhoApk) ?? throw new BuildFailedException("Não foi possível resolver o diretório de saída do APK."));
        BuildOptions opcoes = versaoApresentacao ? BuildOptions.DetailedBuildReport : BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.AutoRunPlayer | BuildOptions.DetailedBuildReport;
        if (corrigirPlayerExistente)
        {
            if (!File.Exists(caminhoApk))
            {
                throw new BuildFailedException($"Patch e Executar precisa primeiro de um APK base. Execute '01 Compilar e Executar (base)': {caminhoApk}");
            }

            // O caminho de patch Android só de scripts do Unity exige ambas as flags.
            // Sem BuildScriptsOnly, PatchPackage pode voltar a recompilar muito mais
            // do player Android/IL2CPP do que o necessário.
            opcoes |= BuildOptions.BuildScriptsOnly | BuildOptions.PatchPackage;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        string modo = versaoApresentacao ? "PRESENTATION_RELEASE" : corrigirPlayerExistente ? "PATCH_AND_RUN" : "BASE_BUILD_AND_RUN";
        Debug.Log($"[VR SNC][{modo}] START alvo={AplicativoIdentificador} apk={caminhoApk} " + $"scriptsOnly={corrigirPlayerExistente}");
        string anteriorTemporario = null;
        string anteriorTMP = null;
        string anteriorGradleOpcoes = null;
        string anteriorJavaFerramentaOpcoes = null;
        bool usarCaminhoCurtoTemporarioGradle = Application.platform == RuntimePlatform.WindowsEditor;
        BuildReport relatorio;
        string caminhoTemporarioGradle = CaminhoTemporarioGradleWindows;
        if (usarCaminhoCurtoTemporarioGradle)
        {
            // Usa um diretório temporário gravável do usuário, limitado a esta compilação.
            // As variáveis do processo são restauradas ao terminar.
            Directory.CreateDirectory(caminhoTemporarioGradle);
            anteriorTemporario = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Process);
            anteriorTMP = Environment.GetEnvironmentVariable("TMP", EnvironmentVariableTarget.Process);
            anteriorGradleOpcoes = Environment.GetEnvironmentVariable("GRADLE_OPTS", EnvironmentVariableTarget.Process);
            anteriorJavaFerramentaOpcoes = Environment.GetEnvironmentVariable("JAVA_TOOL_OPTIONS", EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("TEMP", caminhoTemporarioGradle, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("TMP", caminhoTemporarioGradle, EnvironmentVariableTarget.Process);
            string opcoesTemporarias = $"-Djava.io.tmpdir=\"{caminhoTemporarioGradle.Replace('\\', '/')}\" -Djava.net.preferIPv4Stack=true -Djava.net.preferIPv4Addresses=true";
            string gradleOpcoes = $"{anteriorGradleOpcoes} -Dorg.gradle.daemon=false {opcoesTemporarias}".Trim();
            Environment.SetEnvironmentVariable("GRADLE_OPTS", gradleOpcoes, EnvironmentVariableTarget.Process);
            string javaFerramentaOpcoes = $"{anteriorJavaFerramentaOpcoes} {opcoesTemporarias}".Trim();
            Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS", javaFerramentaOpcoes, EnvironmentVariableTarget.Process);
            Debug.Log($"[VR SNC][{modo}] Gradle TEMP/TMP={caminhoTemporarioGradle} (process scoped)");
            Debug.Log($"[VR SNC][{modo}] O daemon do Gradle foi desativado para esta compilação (escopo do processo)");
        }

        try
        {
            relatorio = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = cenas, locationPathName = caminhoApk, targetGroup = BuildTargetGroup.Android, target = BuildTarget.Android, options = opcoes });
        }
        finally
        {
            if (usarCaminhoCurtoTemporarioGradle)
            {
                Environment.SetEnvironmentVariable("TEMP", anteriorTemporario, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("TMP", anteriorTMP, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("GRADLE_OPTS", anteriorGradleOpcoes, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS", anteriorJavaFerramentaOpcoes, EnvironmentVariableTarget.Process);
            }
        }

        BuildSummary resumo = relatorio.summary;
        Debug.Log($"[VR SNC][{modo}] RESULT={resumo.result} duration={resumo.totalTime} " + $"size={resumo.totalSize} warnings={resumo.totalWarnings} errors={resumo.totalErrors} apk={caminhoApk}");
        if (resumo.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"Quest {modo} falhou com {resumo.totalErrors} erro(s). Consulte o Console do Unity e o Editor.log.");
        }
    }

    private static void CorrigirEventoSistemaESalvar(bool registrarResultado)
    {
        EventSystem eventoSistema = UnityEngine.Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventoSistema == null)
        {
            if (registrarResultado)
            {
                Debug.LogWarning("[VR SNC] EventSystem não foi encontrado; nenhuma alteração foi feita.");
            }

            return;
        }

        XRUIInputModule interfaceXR = eventoSistema.GetComponent<XRUIInputModule>();
        InputSystemUIInputModule entradaSistemaInterface = eventoSistema.GetComponent<InputSystemUIInputModule>();
        int removido = 0;
        if (interfaceXR != null && entradaSistemaInterface != null)
        {
            UnityEngine.Object.DestroyImmediate(entradaSistemaInterface);
            removido++;
        }

        // Remove o BaseInput legado criado pela leitura de BaseInputModule.input.
        foreach (BaseInput baseEntrada in eventoSistema.GetComponents<BaseInput>())
        {
            UnityEngine.Object.DestroyImmediate(baseEntrada);
            removido++;
        }

        if (removido > 0)
        {
            EditorSceneManager.MarkSceneDirty(eventoSistema.gameObject.scene);
            EditorSceneManager.SaveScene(eventoSistema.gameObject.scene);
        }

        if (registrarResultado)
        {
            Debug.Log($"[VR SNC] Reparo do EventSystem concluído. Removidos {removido} redundant component(s).");
        }
    }
}