using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRSNC.Interacao;
using VRSNC.Interface;

namespace VRSNC.FerramentasEditor
{
    /// <summary>Regressão determinística de posse e cálculo de pivô para a captura legada por controle.</summary>
    public static class MenuRegressaoAgarreControlador
    {
        [MenuItem("Ferramentas/VR SNC/Executar Teste de Regressão de Agarrar com Controle")]
        private static void Executar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Execute este teste de regressão do controle fora do Modo de Jogo.");
            var cenariosTeste = new List<GameObject>();
            try
            {
                AgarreControladorMantemPontoDeAgarreEProprietarioUnico(cenariosTeste);
                PropriedadePainelEncerraAnatomiaEBloqueiaReaquisicao(cenariosTeste);
                Debug.Log("[VR SNC] APROVADO — teste de captura com o controle: posição do pivô, sem salto ao pressionar, controle exclusivo e soltura correta.");
            }
            catch (Exception excecao)
            {
                Debug.LogException(excecao);
                throw;
            }
            finally
            {
                foreach (var cenarioTeste in cenariosTeste)
                {
                    if (cenarioTeste != null)
                        UnityEngine.Object.DestroyImmediate(cenarioTeste);
                }
            }
        }

        private static void AgarreControladorMantemPontoDeAgarreEProprietarioUnico(List<GameObject> cenariosTeste)
        {
            var alvo = new GameObject("ControllerGrabRegression_Target");
            cenariosTeste.Add(alvo);
            alvo.transform.SetPositionAndRotation(new Vector3(0.14f, -0.08f, 0.31f), Quaternion.Euler(4f, -13f, 7f));
            var controlador = new GameObject("ControllerGrabRegression_RightController");
            cenariosTeste.Add(controlador);
            controlador.transform.SetPositionAndRotation(new Vector3(-0.22f, 0.17f, -0.26f), Quaternion.Euler(2f, 11f, -3f));
            var manipulador = alvo.AddComponent<ManipuladorObjetoSelecionado>();
            var mapeador = alvo.AddComponent<MapeadorEntradaMetaQuest2>();
            mapeador.Configurar(null, null, null, null, manipulador, null, null, null, null, null, controlador.transform, null, null, null);
            var posicaoInicialAlvo = alvo.transform.position;
            var rotacaoInicialAlvo = alvo.transform.rotation;
            var posicaoInicialControlador = controlador.transform.position;
            var rotacaoInicialControlador = controlador.transform.rotation;
            mapeador.IniciarAgarreDireito();
            Exigir(manipulador.EstaAgarrando, "O mapeador não assumiu o controle da captura.");
            Exigir(Aproximadamente(alvo.transform.position, posicaoInicialAlvo) && Aproximadamente(alvo.transform.rotation, rotacaoInicialAlvo), "Pressionar o botão de empunhadura alterou a posição do alvo antes da atualização do controle.");
            var primeiraVersao = manipulador.VersaoAgarre;
            mapeador.IniciarAgarreDireito();
            Exigir(manipulador.VersaoAgarre == primeiraVersao, "Um segundo acionamento atribuiu o controle a outro responsável ao mesmo tempo.");
            controlador.transform.SetPositionAndRotation(new Vector3(0.43f, 0.24f, -0.12f), Quaternion.Euler(8f, 79f, -6f));
            manipulador.AtualizarAgarre(controlador.transform, 1f);
            var variacaoRotacao = controlador.transform.rotation * Quaternion.Inverse(rotacaoInicialControlador);
            var rotacaoCombinacao = 1f - Mathf.Exp(-18f);
            var esperadoRotacao = Quaternion.Slerp(rotacaoInicialAlvo, variacaoRotacao * rotacaoInicialAlvo, rotacaoCombinacao);
            var esperadoPosicao = controlador.transform.position + (esperadoRotacao * Quaternion.Inverse(rotacaoInicialAlvo)) * (posicaoInicialAlvo - posicaoInicialControlador);
            Exigir(Aproximadamente(alvo.transform.rotation, esperadoRotacao), "A rotação do controle não usou o cálculo compartilhado do ponto de empunhadura da mão.");
            Exigir(Aproximadamente(alvo.transform.position, esperadoPosicao), "A posição do controle não preservou o ponto de empunhadura original.");
            var antesSoltarPosicao = alvo.transform.position;
            var antesSoltarRotacao = alvo.transform.rotation;
            mapeador.EncerrarAgarreDireito();
            Exigir(!manipulador.EstaAgarrando, "Soltar o controle não encerrou a captura que ele iniciou.");
            Exigir(Aproximadamente(alvo.transform.position, antesSoltarPosicao) && Aproximadamente(alvo.transform.rotation, antesSoltarRotacao), "Soltar o controle alterou a posição do alvo.");
        }

        private static void PropriedadePainelEncerraAnatomiaEBloqueiaReaquisicao(List<GameObject> cenariosTeste)
        {
            // Cria inativo para atribuir cada dependência antes dos callbacks de ciclo de vida do XRI.
            var cenarioTeste = new GameObject("ControllerPanelOwnershipRegression");
            cenariosTeste.Add(cenarioTeste);
            cenarioTeste.SetActive(false);
            var controladorRaiz = new GameObject("ControllerRoot");
            controladorRaiz.transform.SetParent(cenarioTeste.transform, false);
            var proximoDistante = new GameObject("NearFarChild");
            proximoDistante.transform.SetParent(controladorRaiz.transform, false);
            var interatorColisor = proximoDistante.AddComponent<SphereCollider>();
            interatorColisor.isTrigger = true;
            var interatorCorpo = proximoDistante.AddComponent<Rigidbody>();
            interatorCorpo.isKinematic = true;
            interatorCorpo.useGravity = false;
            var painelInterator = proximoDistante.AddComponent<XRDirectInteractor>();
            var gerenciadorModalidade = cenarioTeste.AddComponent<XRInputModalityManager>();
            gerenciadorModalidade.rightController = controladorRaiz;
            gerenciadorModalidade.enabled = false;
            var anatomia = new GameObject("Anatomy");
            anatomia.transform.SetParent(cenarioTeste.transform, false);
            var manipulador = anatomia.AddComponent<ManipuladorObjetoSelecionado>();
            var mapeador = anatomia.AddComponent<MapeadorEntradaMetaQuest2>();
            mapeador.Configurar(null, null, null, null, manipulador, null, null, null, null, null, proximoDistante.transform, null, null, null, gerenciadorModalidade);
            var painelAlcaObjeto = new GameObject("PanelHeader");
            painelAlcaObjeto.transform.SetParent(cenarioTeste.transform, false);
            painelAlcaObjeto.AddComponent<BoxCollider>();
            var painelCorpo = painelAlcaObjeto.AddComponent<Rigidbody>();
            painelCorpo.isKinematic = true;
            painelCorpo.useGravity = false;
            var agarrePainel = painelAlcaObjeto.AddComponent<XRGrabInteractable>();
            var painelAlca = painelAlcaObjeto.AddComponent<ControlePainelMundo>();
            cenarioTeste.SetActive(true);
            // O modo de edição não inicializa o estado interno de execução do XRGrabInteractable
            // (sobretudo seu transform em cache); o ciclo físico completo de captura pertence
            // a um teste em modo de execução. Registra e invoca diretamente o callback do painel
            // de produção para verificar deterministicamente o contrato de posse.
            InvocarPainelManipularCicloVida(painelAlca, "OnEnable");
            Exigir(painelInterator.isActiveAndEnabled && painelAlca.isActiveAndEnabled && painelAlca.InteragivelAgarre == agarrePainel, "O cenário de teste do painel não ativou o interator XRI nem o ouvinte da alça.");
            mapeador.IniciarAgarreDireito();
            Exigir(manipulador.EstaAgarrando, "O controle não selecionou a anatomia antes de selecionar o painel.");
            AvancarMapeador(mapeador);
            Exigir(manipulador.EstaAgarrando, "Um elemento de fundo da interface, sem seleção do painel, interrompeu a captura da anatomia iniciada pelo controle.");
            InvocarPainelSelecao(painelAlca, painelInterator, agarrePainel, iniciado: true);
            Exigir(painelAlca.QuantidadeInteratoresSelecionando == 1, "A rotina de seleção do painel não registrou o interator XRI.");
            Exigir(ControlePainelMundo.AgarrePainelAtivoParaMao(controladorRaiz.transform), "A seleção do painel não foi vinculada à raiz do controle de modalidade.");
            AvancarMapeador(mapeador);
            Exigir(!manipulador.EstaAgarrando, "Ao assumir o painel, o sistema não encerrou corretamente a captura da anatomia.");
            mapeador.IniciarAgarreDireito();
            Exigir(!manipulador.EstaAgarrando, "Com o painel selecionado, o mesmo controle conseguiu iniciar outra captura da anatomia.");
            InvocarPainelSelecao(painelAlca, painelInterator, agarrePainel, iniciado: false);
            Exigir(painelAlca.QuantidadeInteratoresSelecionando == 0, "A rotina de soltura do painel não removeu o registro do interator XRI.");
            Exigir(!ControlePainelMundo.AgarrePainelAtivoParaMao(controladorRaiz.transform), "Depois de soltar o painel, a raiz do controle continuou marcada como responsável.");
            // As bordas de redimensionamento não têm seleção XRGrabInteractable própria, mas usam
            // o mesmo registro de posse do painel para continuar recusando entrada anatômica do controle.
            Exigir(painelAlca.TentarIniciarGestoExterno(painelInterator), "A borda de redimensionamento não conseguiu registrar sua responsabilidade compartilhada pelo painel.");
            Exigir(ControlePainelMundo.AgarrePainelAtivoParaMao(controladorRaiz.transform), "A responsabilidade pela borda de redimensionamento não foi vinculada à raiz do controle de modalidade.");
            mapeador.IniciarAgarreDireito();
            Exigir(!manipulador.EstaAgarrando, "Com a borda de redimensionamento ativa, o controle ainda conseguiu capturar uma estrutura anatômica.");
            painelAlca.EncerrarGestoExterno(painelInterator);
            Exigir(!ControlePainelMundo.AgarrePainelAtivoParaMao(controladorRaiz.transform), "Depois de soltar a borda de redimensionamento, a raiz do controle continuou marcada como responsável.");
        }

        private static void AvancarMapeador(MapeadorEntradaMetaQuest2 mapeador)
        {
            var atualizar = typeof(MapeadorEntradaMetaQuest2).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Exigir(atualizar != null, "Não foi possível localizar o método de atualização do mapeador em tempo de execução.");
            atualizar.Invoke(mapeador, null);
        }

        private static void InvocarPainelManipularCicloVida(ControlePainelMundo alca, string metodoNome)
        {
            var metodo = typeof(ControlePainelMundo).GetMethod(metodoNome, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Exigir(metodo != null, $"Não foi possível localizar BrainWorldPanelHandle.{metodoNome}.");
            metodo.Invoke(alca, null);
        }

        private static void InvocarPainelSelecao(ControlePainelMundo alca, XRDirectInteractor interator, XRGrabInteractable agarrar, bool iniciado)
        {
            var metodoNome = iniciado ? "EmAgarrarIniciado" : "EmAgarrarEncerrado";
            var metodo = typeof(ControlePainelMundo).GetMethod(metodoNome, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Exigir(metodo != null, $"Não foi possível localizar BrainWorldPanelHandle.{metodoNome}.");
            if (iniciado)
            {
                metodo.Invoke(alca, new object[] { new SelectEnterEventArgs { interactorObject = interator, interactableObject = agarrar, } });
            }
            else
            {
                metodo.Invoke(alca, new object[] { new SelectExitEventArgs { interactorObject = interator, interactableObject = agarrar, } });
            }
        }

        private static bool Aproximadamente(Vector3 primeiro, Vector3 segundo) => Vector3.SqrMagnitude(primeiro - segundo) <= 0.00000001f;
        private static bool Aproximadamente(Quaternion primeiro, Quaternion segundo) => Quaternion.Angle(primeiro, segundo) <= 0.0001f;
        private static void Exigir(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException("[VR SNC] O teste de captura com o controle falhou: " + mensagem);
        }
    }
}
