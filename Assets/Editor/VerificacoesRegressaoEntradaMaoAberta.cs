using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using VRSNC.Interacao;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Regressão determinística no modo de edição para a barreira deliberada de entrada com mão aberta.
    /// Isola o estado da ponte e não entra no modo de execução nem toca na cena.
    /// </summary>
    public static class VerificacoesRegressaoEntradaMaoAberta
    {
        [MenuItem("Ferramentas/VR SNC/Mãos/Validar Mecanismo de Entrada com Mão Aberta (Determinístico)")]
        public static void Validar()
        {
            var falhas = new List<string>();
            GameObject raiz = null;
            try
            {
                raiz = new GameObject("__VR_SNC_OpenEntry_Regression")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                raiz.SetActive(false);
                var focoObjeto = new GameObject("Focus")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                focoObjeto.transform.SetParent(raiz.transform, false);
                var foco = focoObjeto.AddComponent<NoInteracaoAnatomica>();
                var grupoObjeto = new GameObject("Group")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                grupoObjeto.transform.SetParent(raiz.transform, false);
                var grupo = grupoObjeto.AddComponent<NoInteracaoAnatomica>();
                var filhoObjeto = new GameObject("Child")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                filhoObjeto.transform.SetParent(grupoObjeto.transform, false);
                var filho = filhoObjeto.AddComponent<NoInteracaoAnatomica>();
                foco.Configurar("Focus", "Focus", 0, null, new[] { grupo }, true, true, true);
                grupo.Configurar("Group", "Group", 1, foco, new[] { filho }, true, true, false);
                filho.Configurar("Child", "Child", 2, grupo, Array.Empty<NoInteracaoAnatomica>(), true, true, false);
                var selecao = raiz.AddComponent<GerenciadorSelecaoHierarquica>();
                selecao.Configurar(foco, null, null, null);
                raiz.SetActive(true);
                selecao.SelecionarNo(grupo);
                VerificarCenario(raiz, selecao, foco, grupo, false, true, "one-hand movement still arms deliberate open-hand entry", falhas);
                VerificarCenario(raiz, selecao, foco, grupo, true, false, "completed bimanual interaction stays blocked from open-hand entry", falhas);
            }
            catch (Exception excecao)
            {
                falhas.Add("Exceção inesperada: " + excecao.GetBaseException().Message);
            }
            finally
            {
                if (raiz != null)
                    UnityEngine.Object.DestroyImmediate(raiz);
            }

            if (falhas.Count == 0)
                Debug.Log("[VR SNC] PASS: liberação de uma mão movida arma entrada com mão aberta; liberação bimanual permanece bloqueada.");
            else
                Debug.LogError("[VR SNC] FAIL: barreira de entrada com mão aberta\n- " + string.Join("\n- ", falhas));
        }

        private static void VerificarCenario(GameObject raiz, GerenciadorSelecaoHierarquica selecao, NoInteracaoAnatomica foco, NoInteracaoAnatomica grupo, bool usadoNoAgarreSecundario, bool esperadoArmado, string descricao, List<string> falhas)
        {
            var ponteObjeto = new GameObject("Bridge")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            ponteObjeto.transform.SetParent(raiz.transform, false);
            var ponte = ponteObjeto.AddComponent<PonteInteracaoMao>();
            var responsavelObjeto = new GameObject("Owner")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            responsavelObjeto.transform.SetParent(raiz.transform, false);
            var responsavel = responsavelObjeto.AddComponent<ElementoInteragivelMao>();
            DefinirCampo(ponte, "selecao", selecao);
            DefinirCampo(ponte, "proprietario", responsavel);
            DefinirCampo(ponte, "noAgarrado", grupo);
            DefinirCampo(ponte, "focoAgarre", foco);
            DefinirCampo(ponte, "primarioMovido", true);
            DefinirCampo(ponte, "primarioUsadoNoAgarreSecundario", usadoNoAgarreSecundario);
            InvocarPrivado(ponte, "EncerrarPrimarioEPossivelmenteArmarAbrirEntrada", (object)null);
            var armadoNo = ObterCampo<NoInteracaoAnatomica>(ponte, "aguardandoAbrirNo");
            if ((armadoNo == grupo) != esperadoArmado)
                falhas.Add(descricao);
            UnityEngine.Object.DestroyImmediate(responsavelObjeto);
            UnityEngine.Object.DestroyImmediate(ponteObjeto);
        }

        private static void InvocarPrivado(object instancia, string nome, params object[] argumentos)
        {
            var metodo = instancia.GetType().GetMethod(nome, BindingFlags.Instance | BindingFlags.NonPublic);
            if (metodo == null)
                throw new MissingMethodException(instancia.GetType().Name, nome);
            metodo.Invoke(instancia, argumentos);
        }

        private static void DefinirCampo(object instancia, string nome, object valor)
        {
            var campo = instancia.GetType().GetField(nome, BindingFlags.Instance | BindingFlags.NonPublic);
            if (campo == null)
                throw new MissingFieldException(instancia.GetType().Name, nome);
            campo.SetValue(instancia, valor);
        }

        private static T ObterCampo<T>(object instancia, string nome)
            where T : class
        {
            var campo = instancia.GetType().GetField(nome, BindingFlags.Instance | BindingFlags.NonPublic);
            if (campo == null)
                throw new MissingFieldException(instancia.GetType().Name, nome);
            return campo.GetValue(instancia) as T;
        }
    }
}
