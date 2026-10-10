using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRSNC.Anatomia;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Comando de regressão determinístico para o modo de edição. Cria apenas fixtures inativas em memória
    /// e as destrói no finally; não usa cena, prefab, ativo, pacote ou dispositivo.
    /// </summary>
    public static class MenuRegressaoGestoSeparacaoAnatomica
    {
        private const string CaminhoMenu = "Ferramentas/VR SNC/Executar Teste de Regressão do Gesto de Expansão";
        [MenuItem(CaminhoMenu)]
        private static void Executar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Execute este teste determinístico fora do Modo de Jogo.");
            var cenariosTeste = new List<GameObject>();
            try
            {
                SemSelecaoMoveTodasFolhasEPreservaPosesSeguradas(cenariosTeste);
                EscalaRaizPontoZeroZeroUmUsaMetrosMundo(cenariosTeste);
                GrupoSelecionadoMoveSomenteDescendentesERetomaSemSalto(cenariosTeste);
                FolhaSelecionadaEExpansaoGlobalRetidaRecusamGestoLocal(cenariosTeste);
                ResetGlobalLimpaGestoAtivoERetido(cenariosTeste, concluirAntes: false);
                ResetGlobalLimpaGestoAtivoERetido(cenariosTeste, concluirAntes: true);
                Debug.Log("[VR SNC] APROVADO — teste do gesto de expansão: 6 cenários determinísticos no Modo de Edição.");
            }
            catch (Exception excecao)
            {
                Debug.LogException(excecao);
                throw;
            }
            finally
            {
                foreach (var raiz in cenariosTeste)
                {
                    if (raiz != null)
                        UnityEngine.Object.DestroyImmediate(raiz);
                }
            }
        }

        private static void ResetGlobalLimpaGestoAtivoERetido(List<GameObject> cenariosTeste, bool concluirAntes)
        {
            var cenario = CriarCenarioTeste(cenariosTeste);
            foreach (var no in cenario.Raiz.GetComponentsInChildren<NoInteracaoAnatomica>(true))
                no.CapturarEstadoOriginal();
            var originais = CapturarLocalPosicoes(cenario.Folhas);
            Exigir(cenario.Coordenador.TentarIniciar(cenario.Folhas[0], cenario.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "O cenário de reset não iniciou o gesto.");
            Exigir(cenario.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.35f), "O cenário de reset não expandiu as folhas.");
            if (concluirAntes)
                Exigir(cenario.Coordenador.Concluir(), "O cenário de reset não concluiu o gesto.");
            var reset = cenario.Raiz.gameObject.AddComponent<GerenciadorRestauracao>();
            // Sem raiz de ativação: mantém a fixture inativa e não habilita componentes XRI.
            reset.Configurar(null, null, cenario.Raiz, cenario.Selecao, null,
                cenario.Raiz.GetComponent<ControladorVisaoExplodida>(), null, null);
            reset.RestaurarTudo();
            Exigir(cenario.Global.Fator == 0f && !cenario.Global.GestoAtivo && !cenario.Global.EstaExpandido, "Reset conservou fator ou gesto global.");
            Exigir(!cenario.Coordenador.EstaAtivo && !cenario.Coordenador.PossuiEstadoSeparacaoRetido, "Reset conservou proprietário ou estado retido no coordenador.");
            ExigirLocalPosicoes(cenario.Folhas, originais, "Reset global não restaurou a posição das folhas.");
            Exigir(cenario.Coordenador.TentarIniciar(cenario.Folhas[0], cenario.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "Não foi possível iniciar novo gesto após reset.");
            Exigir(cenario.Global.Fator == 0f, "Novo gesto reaplicou o fator anterior.");
            ExigirLocalPosicoes(cenario.Folhas, originais, "Novo gesto depois do reset provocou salto.");
        }

        private static void SemSelecaoMoveTodasFolhasEPreservaPosesSeguradas(List<GameObject> cenariosTeste)
        {
            var cenarioTeste = CriarCenarioTeste(cenariosTeste);
            Exigir(cenarioTeste.Global.GarantirIndiceAtualizado(), "Não foi possível indexar o catálogo global.");
            Exigir(cenarioTeste.Global.QuantidadeFolhasIndexadas == ControladorExpansaoGlobal.QuantidadeFolhasEsperada, "O catálogo deve conter 240 elementos terminais.");
            cenarioTeste.GrupoEscopo.transform.position += new Vector3(0.23f, -0.16f, 0.11f);
            cenarioTeste.GrupoEscopo.transform.rotation = Quaternion.Euler(9f, 41f, -12f) * cenarioTeste.GrupoEscopo.transform.rotation;
            var raizPosicao = cenarioTeste.Raiz.transform.position;
            var raizRotacao = cenarioTeste.Raiz.transform.rotation;
            var grupoPosicao = cenarioTeste.GrupoEscopo.transform.localPosition;
            var grupoRotacao = cenarioTeste.GrupoEscopo.transform.localRotation;
            var originais = CapturarLocalPosicoes(cenarioTeste.Folhas);
            Exigir(cenarioTeste.Coordenador.TentarIniciar(cenarioTeste.Folhas[0], cenarioTeste.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "O gesto global não foi iniciado.");
            Exigir(cenarioTeste.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.35f), "O gesto global não foi atualizado.");
            Exigir(ContarFolhasMovidas(cenarioTeste.Folhas, originais) == ControladorExpansaoGlobal.QuantidadeFolhasEsperada, "O gesto global não moveu todos os 240 elementos terminais.");
            Exigir(Aproximadamente(cenarioTeste.Raiz.transform.position, raizPosicao) && Aproximadamente(cenarioTeste.Raiz.transform.rotation, raizRotacao), "O gesto global alterou a posição da raiz do SNC.");
            Exigir(Aproximadamente(cenarioTeste.GrupoEscopo.transform.localPosition, grupoPosicao) && Aproximadamente(cenarioTeste.GrupoEscopo.transform.localRotation, grupoRotacao), "O gesto global alterou a posição do grupo que estava sendo segurado.");
            cenarioTeste.Coordenador.CancelarERestaurar();
            ExigirLocalPosicoes(cenarioTeste.Folhas, originais, "A limpeza global não restaurou todos os elementos terminais.");
        }

        private static void EscalaRaizPontoZeroZeroUmUsaMetrosMundo(List<GameObject> cenariosTeste)
        {
            var cenarioTeste = CriarCenarioTeste(cenariosTeste, Vector3.one * 0.001f);
            var posicaoMundoOriginal = cenarioTeste.Folhas[0].transform.position;
            Exigir(cenarioTeste.Coordenador.TentarIniciar(cenarioTeste.Folhas[0], cenarioTeste.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "O gesto global com escala não foi iniciado.");
            Exigir(cenarioTeste.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.35f), "O gesto global com escala não foi atualizado.");
            var deslocamento = Vector3.Distance(cenarioTeste.Folhas[0].transform.position, posicaoMundoOriginal);
            Exigir(Mathf.Abs(deslocamento - 0.22f) <= 0.0005f, $"Era esperado um deslocamento de 0,22 m no mundo com a escala da raiz em 0,001; obtido {deslocamento:0.000000}m.");
        }

        private static void GrupoSelecionadoMoveSomenteDescendentesERetomaSemSalto(List<GameObject> cenariosTeste)
        {
            var cenarioTeste = CriarCenarioTeste(cenariosTeste);
            DefinirNoSelecionado(cenarioTeste.Selecao, cenarioTeste.GrupoEscopo);
            var folhasEscopoLimitado = cenarioTeste.EscopoFolhas;
            var folhasExternas = new List<NoInteracaoAnatomica>(cenarioTeste.Folhas);
            folhasExternas.RemoveAll(no => folhasEscopoLimitado.Contains(no));
            var originaisEscopoLimitado = CapturarLocalPosicoes(folhasEscopoLimitado);
            var originaisExternos = CapturarLocalPosicoes(folhasExternas);
            Exigir(cenarioTeste.Coordenador.TentarIniciar(folhasEscopoLimitado[0], folhasEscopoLimitado[1], Vector3.zero, Vector3.right * 0.10f), "O gesto limitado ao grupo não foi iniciado.");
            Exigir(cenarioTeste.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.35f), "O gesto limitado ao grupo não foi atualizado.");
            Exigir(ContarFolhasMovidas(folhasEscopoLimitado, originaisEscopoLimitado) == folhasEscopoLimitado.Count, "O gesto limitado ao grupo não moveu todos os elementos terminais descendentes.");
            ExigirLocalPosicoes(folhasExternas, originaisExternos, "O gesto limitado ao grupo moveu um elemento terminal que não fazia parte dele.");
            Exigir(cenarioTeste.Coordenador.Concluir(), "O gesto limitado ao grupo não foi concluído.");
            var posicoesRetidas = CapturarLocalPosicoes(folhasEscopoLimitado);
            Exigir(cenarioTeste.Coordenador.TentarIniciar(folhasEscopoLimitado[0], folhasEscopoLimitado[1], Vector3.zero, Vector3.right * 0.10f), "A retomada do gesto limitado ao grupo não preservou o estado inicial registrado.");
            Exigir(cenarioTeste.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.10f), "Ao retomar o gesto limitado ao grupo, não houve atualização na distância inicial.");
            ExigirLocalPosicoes(folhasEscopoLimitado, posicoesRetidas, "A retomada do gesto limitado ao grupo provocou um salto de posição.");
            cenarioTeste.Coordenador.CancelarERestaurar();
            ExigirLocalPosicoes(folhasEscopoLimitado, originaisEscopoLimitado, "A limpeza do gesto limitado ao grupo não restaurou os elementos descendentes.");
            ExigirLocalPosicoes(folhasExternas, originaisExternos, "Scoped cleanup changed outsiders.");
        }

        private static void FolhaSelecionadaEExpansaoGlobalRetidaRecusamGestoLocal(List<GameObject> cenariosTeste)
        {
            var folhaCenarioTeste = CriarCenarioTeste(cenariosTeste);
            DefinirNoSelecionado(folhaCenarioTeste.Selecao, folhaCenarioTeste.FolhaRaiz);
            var originais = CapturarLocalPosicoes(folhaCenarioTeste.Folhas);
            Exigir(!folhaCenarioTeste.Coordenador.TentarIniciar(folhaCenarioTeste.Folhas[0], folhaCenarioTeste.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "Um elemento terminal selecionado, sem descendentes, não deve ser expandido.");
            ExigirLocalPosicoes(folhaCenarioTeste.Folhas, originais, "A recusa do elemento terminal alterou sua posição.");
            var retidoCenarioTeste = CriarCenarioTeste(cenariosTeste);
            Exigir(retidoCenarioTeste.Coordenador.TentarIniciar(retidoCenarioTeste.Folhas[0], retidoCenarioTeste.Folhas[1], Vector3.zero, Vector3.right * 0.10f), "A configuração do gesto global retido não foi iniciada.");
            Exigir(retidoCenarioTeste.Coordenador.TentarAtualizar(Vector3.zero, Vector3.right * 0.35f), "A configuração do gesto global retido não foi atualizada.");
            Exigir(retidoCenarioTeste.Coordenador.Concluir(), "A configuração do gesto global retido não foi concluída.");
            Exigir(retidoCenarioTeste.Global.EstaExpandido && retidoCenarioTeste.Global.Fator > 0f, "O cenário de teste não manteve o estado expandido global antes da solicitação limitada ao grupo.");
            DefinirNoSelecionado(retidoCenarioTeste.Selecao, retidoCenarioTeste.GrupoEscopo);
            Exigir(retidoCenarioTeste.Selecao.NoSelecionado == retidoCenarioTeste.GrupoEscopo, "O cenário de teste não selecionou o grupo usado na solicitação limitada ao grupo.");
            var externosRetidos = new List<NoInteracaoAnatomica>(retidoCenarioTeste.Folhas);
            externosRetidos.RemoveAll(no2 => retidoCenarioTeste.EscopoFolhas.Contains(no2));
            var posicoesExternas = CapturarLocalPosicoes(externosRetidos);
            Exigir(!retidoCenarioTeste.Coordenador.TentarIniciar(retidoCenarioTeste.EscopoFolhas[0], retidoCenarioTeste.EscopoFolhas[1], Vector3.zero, Vector3.right * 0.10f), "O gesto limitado ao grupo deve ser recusado enquanto o estado global controlar elementos terminais externos.");
            ExigirLocalPosicoes(externosRetidos, posicoesExternas, "A tentativa recusada de iniciar o gesto limitado restaurou os elementos externos controlados pelo gesto global.");
        }

        private static CenarioTeste CriarCenarioTeste(List<GameObject> cenariosTeste, Vector3? raizEscala = null)
        {
            var raizObjeto = new GameObject("ExplodeRegression_SNC");
            raizObjeto.SetActive(false);
            cenariosTeste.Add(raizObjeto);
            raizObjeto.transform.SetPositionAndRotation(new Vector3(0.42f, -0.31f, 0.77f), Quaternion.Euler(17f, 31f, -8f));
            raizObjeto.transform.localScale = raizEscala ?? new Vector3(1.2f, 0.9f, 1.1f);
            var raiz = raizObjeto.AddComponent<NoInteracaoAnatomica>();
            var raizVinculo = raizObjeto.AddComponent<VinculoAnatomico>();
            raizVinculo.Configurar(true, "root", "", "", "", null);
            raiz.VincularAnatomia(null, raizVinculo);
            var grupos = new List<NoInteracaoAnatomica>();
            var filhosGrupo = new List<List<NoInteracaoAnatomica>>();
            for (var grupoIndice = 0; grupoIndice < ControladorExpansaoGlobal.QuantidadeGruposEsperada - 1; grupoIndice++)
            {
                var grupoObjeto = new GameObject($"Group_{grupoIndice}");
                grupoObjeto.transform.SetParent(raizObjeto.transform, false);
                grupoObjeto.transform.localPosition = new Vector3(grupoIndice * 0.013f, (grupoIndice % 4) * 0.017f, -grupoIndice * 0.011f);
                grupoObjeto.transform.localRotation = Quaternion.Euler(grupoIndice % 7, grupoIndice * 3f, grupoIndice % 5);
                var grupo = grupoObjeto.AddComponent<NoInteracaoAnatomica>();
                var vinculo = grupoObjeto.AddComponent<VinculoAnatomico>();
                vinculo.Configurar(true, $"group-{grupoIndice}", "", "", "", null);
                grupo.VincularAnatomia(null, vinculo);
                grupos.Add(grupo);
                filhosGrupo.Add(new List<NoInteracaoAnatomica>());
            }

            var folhas = new List<NoInteracaoAnatomica>();
            NoInteracaoAnatomica raizFolha = null;
            for (var folhaIndice = 0; folhaIndice < ControladorExpansaoGlobal.QuantidadeFolhasEsperada; folhaIndice++)
            {
                var pai = folhaIndice == ControladorExpansaoGlobal.QuantidadeFolhasEsperada - 1 ? raiz : grupos[folhaIndice % grupos.Count];
                var folhaObjeto = GameObject.CreatePrimitive(PrimitiveType.Cube);
                folhaObjeto.name = $"Leaf_{folhaIndice}";
                folhaObjeto.transform.SetParent(pai.transform, false);
                folhaObjeto.transform.localPosition = new Vector3(0.06f + (folhaIndice % 10) * 0.021f, -0.04f + ((folhaIndice / 10) % 8) * 0.019f, 0.03f + (folhaIndice / 80) * 0.027f);
                folhaObjeto.transform.localScale = Vector3.one * 0.005f;
                var folha = folhaObjeto.AddComponent<NoInteracaoAnatomica>();
                var vinculo2 = folhaObjeto.AddComponent<VinculoAnatomico>();
                vinculo2.Configurar(false, $"leaf-{folhaIndice}", "", "", "", null);
                folha.Configurar(folhaObjeto.name, folhaObjeto.name, 2, pai, null, true, true, false);
                folha.VincularAnatomia(null, vinculo2);
                folhaObjeto.AddComponent<ElementoInteragivelMao>().Configurar(folha, null, folhaObjeto.GetComponent<Collider>());
                folhas.Add(folha);
                if (pai == raiz)
                    raizFolha = folha;
                else
                    filhosGrupo[grupos.IndexOf(pai)].Add(folha);
            }

            for (var grupoIndice2 = 0; grupoIndice2 < grupos.Count; grupoIndice2++)
                grupos[grupoIndice2].Configurar(grupos[grupoIndice2].name, grupos[grupoIndice2].name, 1, raiz, filhosGrupo[grupoIndice2], true, true, false);
            var raizFilhos = new List<NoInteracaoAnatomica>(grupos)
            {raizFolha};
            raiz.Configurar(raizObjeto.name, raizObjeto.name, 0, null, raizFilhos, true, true, true);
            var selecao = raizObjeto.AddComponent<GerenciadorSelecaoHierarquica>();
            selecao.Configurar(raiz, null, null, null);
            var local = raizObjeto.AddComponent<ControladorVisaoExplodida>();
            local.Configurar(selecao);
            var global = raizObjeto.AddComponent<ControladorExpansaoGlobal>();
            global.Configurar(raizObjeto.transform);
            // A fixture permanece inativa intencionalmente. Awake normalmente fornece esta
            // referência em uma cena ativa; conecta-a explicitamente para testar a mesma
            // proteção global retida sem ativar 240 objetos XRI.
            DefinirCampoPrivado(local, "controladorExpansaoGlobal", global);
            var coordenador = raizObjeto.AddComponent<CoordenadorGestoAnatomico>();
            coordenador.Configurar(global, local, selecao);
            return new CenarioTeste(raiz, grupos[0], raizFolha, folhas, filhosGrupo[0], selecao, global, coordenador);
        }

        private static void DefinirNoSelecionado(GerenciadorSelecaoHierarquica selecao, NoInteracaoAnatomica no)
        {
            DefinirCampoPrivado(selecao, "selecionadoNo", no);
        }

        private static void DefinirCampoPrivado(object alvo, string nomeCampo, object valor)
        {
            const BindingFlags Indicadores = BindingFlags.Instance | BindingFlags.NonPublic;
            var campo = alvo.GetType().GetField(nomeCampo, Indicadores);
            if (campo == null)
                throw new MissingFieldException(alvo.GetType().Name, nomeCampo);
            campo.SetValue(alvo, valor);
        }

        private static Dictionary<NoInteracaoAnatomica, Vector3> CapturarLocalPosicoes(IEnumerable<NoInteracaoAnatomica> nos)
        {
            var resultado = new Dictionary<NoInteracaoAnatomica, Vector3>();
            foreach (var no in nos)
                resultado[no] = no.transform.localPosition;
            return resultado;
        }

        private static int ContarFolhasMovidas(IEnumerable<NoInteracaoAnatomica> nos, IReadOnlyDictionary<NoInteracaoAnatomica, Vector3> originais)
        {
            var contagem = 0;
            foreach (var no in nos)
                if (Vector3.Distance(no.transform.localPosition, originais[no]) > 0.00001f)
                    contagem++;
            return contagem;
        }

        private static void ExigirLocalPosicoes(IEnumerable<NoInteracaoAnatomica> nos, IReadOnlyDictionary<NoInteracaoAnatomica, Vector3> esperado, string mensagem)
        {
            foreach (var no in nos)
                Exigir(Aproximadamente(no.transform.localPosition, esperado[no]), mensagem + " Nó: " + no.name);
        }

        private static bool Aproximadamente(Vector3 primeiro, Vector3 segundo) => Vector3.SqrMagnitude(primeiro - segundo) <= 0.0000000001f;
        private static bool Aproximadamente(Quaternion primeiro, Quaternion segundo) => Quaternion.Angle(primeiro, segundo) <= 0.0001f;
        private static void Exigir(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException("[VR SNC] O teste do gesto de expansão falhou: " + mensagem);
        }

        private sealed class CenarioTeste
        {
            public CenarioTeste(NoInteracaoAnatomica raiz, NoInteracaoAnatomica escopoGrupo, NoInteracaoAnatomica raizFolha, List<NoInteracaoAnatomica> folhas, List<NoInteracaoAnatomica> escopoFolhas, GerenciadorSelecaoHierarquica selecao, ControladorExpansaoGlobal global, CoordenadorGestoAnatomico coordenador)
            {
                Raiz = raiz;
                GrupoEscopo = escopoGrupo;
                FolhaRaiz = raizFolha;
                Folhas = folhas;
                EscopoFolhas = escopoFolhas;
                Selecao = selecao;
                Global = global;
                Coordenador = coordenador;
            }

            public NoInteracaoAnatomica Raiz
            {
                get;
            }

            public NoInteracaoAnatomica GrupoEscopo
            {
                get;
            }

            public NoInteracaoAnatomica FolhaRaiz
            {
                get;
            }

            public List<NoInteracaoAnatomica> Folhas
            {
                get;
            }

            public List<NoInteracaoAnatomica> EscopoFolhas
            {
                get;
            }

            public GerenciadorSelecaoHierarquica Selecao
            {
                get;
            }

            public ControladorExpansaoGlobal Global
            {
                get;
            }

            public CoordenadorGestoAnatomico Coordenador
            {
                get;
            }
        }
    }
}
