using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using VRSNC.Interface;

namespace VRSNC.FerramentasEditor
{
    /// <summary>Prova de ciclo de vida, exclusiva do modo de execução, para a janela espacial principal. Nunca altera a cena criada.</summary>
    internal static class MenuRegressaoJanelaEspacialPrincipal
    {
        [MenuItem("Ferramentas/VR SNC/Interface/Executar Teste de Regressão da Janela Espacial Principal (Modo de Jogo)", false, 214)]
        private static void Executar()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[VR SNC] A regressão de MainSpatialWindow deve executar no Modo de Jogo para que XRI Awake/OnEnable inicializem normalmente.");
                return;
            }

            GameObject painel = null;
            GameObject primeiraMao = null;
            GameObject segundaMao = null;
            GameObject raioMao = null;
            GameObject controladorIntermediario = null;
            try
            {
                painel = new GameObject("MainSpatialWindowRegression_Panel", typeof(RectTransform), typeof(Rigidbody), typeof(XRGrabInteractable), typeof(ControlePainelMundo), typeof(JanelaEspacialPrincipal));
                painel.SetActive(false);
                var retangulo = (RectTransform)painel.transform;
                retangulo.sizeDelta = new Vector2(560f, 650f);
                painel.transform.localScale = Vector3.one * .001f;
                var agarrar = painel.GetComponent<XRGrabInteractable>();
                var alca = painel.GetComponent<ControlePainelMundo>();
                var janela = painel.GetComponent<JanelaEspacialPrincipal>();
                janela.Configurar(retangulo, null, agarrar, null, null);
                primeiraMao = CriarMaoDireta("MainSpatialWindowRegression_FirstHand");
                segundaMao = CriarMaoDireta("MainSpatialWindowRegression_SecondHand");
                var primeiro = primeiraMao.GetComponent<XRDirectInteractor>();
                var segundo = segundaMao.GetComponent<XRDirectInteractor>();
                painel.SetActive(true);
                // Borda direita/superior: o movimento de profundidade/posição com uma mão altera e limita o tamanho sem vazar uma pose movida.
                primeiraMao.transform.position = Vector3.zero;
                var posturaOriginal = new Pose(new Vector3(1.2f, 1.4f, .8f), Quaternion.Euler(0f, 35f, 0f));
                painel.transform.SetPositionAndRotation(posturaOriginal.position, posturaOriginal.rotation);
                Exigir(janela.IniciarRedimensionamento(primeiro), "o redimensionamento pela borda não adquiriu o proprietário compartilhado do painel");
                Exigir(ControlePainelMundo.AgarrePainelAtivoParaMao(primeiraMao.transform), "o redimensionamento pela borda não ficou visível para a arbitragem de controle/anatomia");
                primeiraMao.transform.position = new Vector3(3f, 3f, 0f);
                painel.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                janela.AtualizarRedimensionamento(primeiro, Vector2.one);
                Exigir(janela.TamanhoAtual.x == 980f && janela.TamanhoAtual.y == 980f, "o redimensionamento pelo canto não limitou independentemente as duas dimensões");
                Exigir(Vector3.Distance(painel.transform.position, posturaOriginal.position) < .0001f && Quaternion.Angle(painel.transform.rotation, posturaOriginal.rotation) < .01f, "o redimensionamento vazou um deslocamento de pose no mundo");
                janela.EncerrarRedimensionamento(primeiro);
                Exigir(!ControlePainelMundo.AgarrePainelAtivoParaMao(primeiraMao.transform) && agarrar.enabled, "a liberação da borda não devolveu a propriedade compartilhada do painel exatamente uma vez");
                // Uma segunda mão não pode tomar a raiz enquanto uma borda a possui; o cancelamento restaura apenas a pose de redimensionamento.
                primeiraMao.transform.position = Vector3.zero;
                Exigir(janela.IniciarRedimensionamento(primeiro), "a fixture de propriedade em par não conseguiu adquirir a primeira mão");
                Exigir(!janela.IniciarRedimensionamento(segundo), "a segunda mão tomou a propriedade ativa de redimensionamento");
                painel.transform.position += Vector3.forward;
                janela.CancelarRedimensionamento(primeiro);
                Exigir(!janela.EstaRedimensionando && !ControlePainelMundo.AgarrePainelAtivoParaMao(primeiraMao.transform), "resize cancellation leaked ownership");
                Exigir(Vector3.Distance(painel.transform.position, posturaOriginal.position) < .0001f, "o cancelamento do redimensionamento não reverteu a pose");
                // Maximizar é espacial, não tela cheia do sistema operacional; restaurar retorna precisamente ao tamanho e à pose de mundo salvos.
                retangulo.sizeDelta = new Vector2(610f, 540f);
                painel.transform.SetPositionAndRotation(new Vector3(.3f, 1.1f, .9f), Quaternion.Euler(0f, -20f, 0f));
                var antesMaximoPosicao = painel.transform.position;
                var antesMaximoRotacao = painel.transform.rotation;
                janela.AlternarMaximizacao();
                Exigir(janela.EstaMaximizado && janela.TamanhoAtual == new Vector2(980f, 980f), "maximizar não entrou no tamanho máximo espacial");
                painel.transform.position += Vector3.left;
                janela.AlternarMaximizacao();
                Exigir(!janela.EstaMaximizado && janela.TamanhoAtual == new Vector2(610f, 540f) && Vector3.Distance(painel.transform.position, antesMaximoPosicao) < .0001f && Quaternion.Angle(painel.transform.rotation, antesMaximoRotacao) < .01f, "restaurar não devolveu o tamanho e a pose anteriores à maximização");
                // Um raio distante continua intersectando o plano capturado na aquisição após sair
                // do collider da borda; nenhum acerto físico atual é necessário para redimensionar continuamente.
                raioMao = new GameObject("MainSpatialWindowRegression_FarRay", typeof(XRRayInteractor));
                var raio = raioMao.GetComponent<XRRayInteractor>();
                raio.rayOriginTransform = raioMao.transform;
                painel.transform.SetPositionAndRotation(new Vector3(0f, 0f, .8f), Quaternion.identity);
                retangulo.sizeDelta = new Vector2(560f, 650f);
                raioMao.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                Exigir(janela.IniciarRedimensionamento(raio), "o raio distante não conseguiu adquirir uma borda de redimensionamento");
                raioMao.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
                janela.AtualizarRedimensionamento(raio, Vector2.right);
                Exigir(janela.TamanhoAtual.x > 560f, "o raio distante que saiu da borda não continuou no plano fixo de redimensionamento");
                janela.EncerrarRedimensionamento(raio);
                // O gatilho é um leitor aditivo apenas enquanto esta affordance do painel está em foco; após
                // a aquisição, permanece verdadeiro apesar da perda de foco até soltar o gatilho. Grip continua global.
                controladorIntermediario = new GameObject("MainSpatialWindowRegression_ControllerRelay");
                controladorIntermediario.SetActive(false);
                var proximoDistante = controladorIntermediario.AddComponent<NearFarInteractor>();
                var intermediario = controladorIntermediario.AddComponent<IntermediarioSelecaoControladorJanelaPrincipal>();
                var agarre = new XRInputButtonReader("Regression Grip", inputSourceMode: XRInputButtonReader.InputSourceMode.ManualValue);
                var gatilho = new XRInputButtonReader("Regression Trigger", inputSourceMode: XRInputButtonReader.InputSourceMode.ManualValue);
                proximoDistante.selectInput = agarre;
                proximoDistante.uiPressInput = gatilho;
                intermediario.Configurar(proximoDistante);
                controladorIntermediario.SetActive(true);
                Exigir(ReferenceEquals(proximoDistante.selectInput.GetObjectReference(), intermediario), "o intermediario do controlador nao foi instalado como leitor ObjectReference do XRI");
                Exigir(!LerIntermediario(intermediario), "a selecao normal permaneceu verdadeira com grip e gatilho soltos");
                gatilho.manualPerformed = true;
                Exigir(!LerIntermediario(intermediario), "o gatilho fora do painel iniciou uma selecao");
                intermediario.DefinirFocoNoPainel(painel, true);
                Exigir(LerIntermediario(intermediario), "o gatilho sobre o controle do painel nao iniciou a selecao");
                intermediario.DefinirFocoNoPainel(painel, false);
                Exigir(LerIntermediario(intermediario), "a selecao pelo gatilho foi cancelada quando o hover saiu do painel");
                gatilho.manualPerformed = false;
                Exigir(!LerIntermediario(intermediario), "soltar o gatilho nao concluiu a selecao do painel");
                agarre.manualPerformed = true;
                Exigir(LerIntermediario(intermediario), "a selecao original pelo grip nao foi preservada fora do painel");
                agarre.manualPerformed = false;
                // Um puxão em profundidade que retorna ao ponto inicial continua sendo arrasto; não pode se tornar clique duplo.
                Invocar(janela, "EmMoverSelecionarEntrou", new SelectEnterEventArgs { interactorObject = primeiro });
                painel.transform.position += Vector3.forward * .08f;
                Invocar(janela, "LateUpdate");
                painel.transform.position -= Vector3.forward * .08f;
                Invocar(janela, "EmMoverSelecionarSair", new SelectExitEventArgs { interactorObject = primeiro, isCanceled = false });
                Invocar(janela, "EmMoverSelecionarEntrou", new SelectEnterEventArgs { interactorObject = primeiro });
                Invocar(janela, "EmMoverSelecionarSair", new SelectExitEventArgs { interactorObject = primeiro, isCanceled = false });
                Exigir(!janela.EstaMaximizado, "um arrasto concluido foi classificado incorretamente como clique duplo");
                // Dois cliques comuns da mesma mão maximizam; uma seleção cancelada limpa a sequência de cliques.
                Invocar(janela, "EmMoverSelecionarEntrou", new SelectEnterEventArgs { interactorObject = primeiro });
                Invocar(janela, "EmMoverSelecionarSair", new SelectExitEventArgs { interactorObject = primeiro, isCanceled = true });
                Invocar(janela, "EmMoverSelecionarEntrou", new SelectEnterEventArgs { interactorObject = primeiro });
                Invocar(janela, "EmMoverSelecionarSair", new SelectExitEventArgs { interactorObject = primeiro, isCanceled = false });
                Invocar(janela, "EmMoverSelecionarEntrou", new SelectEnterEventArgs { interactorObject = primeiro });
                Invocar(janela, "EmMoverSelecionarSair", new SelectExitEventArgs { interactorObject = primeiro, isCanceled = false });
                Exigir(janela.EstaMaximizado, "dois cliques sem arrasto da mesma mao nao maximizaram");
                Debug.Log("[VR SNC] APROVADO MainSpatialWindow: limite de borda/canto, arrasto em profundidade, reversão de pose, restauração após maximizar e propriedade compartilhada de redimensionamento.");
            }
            catch (Exception excecao)
            {
                Debug.LogError("[VR SNC] FALHA na regressão da janela espacial principal: " + excecao.Message);
            }
            finally
            {
                if (painel != null)
                    UnityEngine.Object.Destroy(painel);
                if (primeiraMao != null)
                    UnityEngine.Object.Destroy(primeiraMao);
                if (segundaMao != null)
                    UnityEngine.Object.Destroy(segundaMao);
                if (raioMao != null)
                    UnityEngine.Object.Destroy(raioMao);
                if (controladorIntermediario != null)
                    UnityEngine.Object.Destroy(controladorIntermediario);
            }
        }

        private static void Invocar(JanelaEspacialPrincipal alvo, string metodo, object argumento = null)
        {
            var informacao = typeof(JanelaEspacialPrincipal).GetMethod(metodo, BindingFlags.Instance | BindingFlags.NonPublic);
            if (informacao == null)
                throw new InvalidOperationException("Método ausente do ciclo de vida da janela " + metodo + ".");
            informacao.Invoke(alvo, argumento == null ? null : new[] { argumento });
        }

        private static GameObject CriarMaoDireta(string nome)
        {
            var mao = new GameObject(nome, typeof(SphereCollider), typeof(Rigidbody), typeof(XRDirectInteractor));
            var colisor = mao.GetComponent<SphereCollider>();
            colisor.isTrigger = true;
            colisor.radius = .04f;
            var corpo = mao.GetComponent<Rigidbody>();
            corpo.isKinematic = true;
            corpo.useGravity = false;
            return mao;
        }

        private static bool LerIntermediario(IntermediarioSelecaoControladorJanelaPrincipal intermediario)
        {
            var campo = typeof(IntermediarioSelecaoControladorJanelaPrincipal).GetField("atualizadoQuadro", BindingFlags.Instance | BindingFlags.NonPublic);
            if (campo == null)
                throw new InvalidOperationException("Não foi possível invalidar o quadro de entrada do relay.");
            campo.SetValue(intermediario, -1);
            return intermediario.ReadIsPerformed();
        }

        private static void Exigir(bool condicao, string mensagem)
        {
            if (!condicao)
                throw new InvalidOperationException(mensagem);
        }
    }
}
