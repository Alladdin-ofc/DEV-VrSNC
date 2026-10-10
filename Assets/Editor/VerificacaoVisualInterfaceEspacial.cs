using System;
using UnityEditor;
using UnityEngine;
using VRSNC.Interacao;
using VRSNC.Interface;

namespace VRSNC.FerramentasEditor
{
    /// <summary>
    /// Auxílio explícito de composição, exclusivo do modo de execução, para uma captura legível da UI anatômica selecionada real.
    /// Nunca salva uma cena, cria conteúdo, altera rastreamento ou é executado automaticamente.
    /// </summary>
    internal static class VerificacaoVisualInterfaceEspacial
    {
        private static InstantaneoPrevia instantaneo;
        [MenuItem("Ferramentas/VR SNC/Interface/Pré-visualizar Interface Espacial Selecionada (Modo de Jogo)", false, 243)]
        private static void PrevisualizacaoSelecionadoEspacialInterface()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("A prévia da UI espacial requer o Modo de Jogo; nenhum estado da cena foi alterado.");
                return;
            }

            if (instantaneo != null)
            {
                Debug.LogWarning("A prévia da UI espacial já está ativa. Restaure-a antes de criar outra prévia.");
                return;
            }

            var camera = Camera.main != null ? Camera.main.transform : null;
            var raiz = GameObject.Find("Sistema_Nervoso_Central");
            var selecao = raiz != null ? raiz.GetComponent<GerenciadorSelecaoHierarquica>() : null;
            var principal = GameObject.Find("Painel_Anatomico_VR_SNC");
            var principalAlca = principal != null ? principal.GetComponentInChildren<ControlePainelMundo>(true) : null;
            var janelaFlutuante = GameObject.Find("Oraculo_Anatomico_Punho");
            var janelaFlutuanteAncora = janelaFlutuante != null ? janelaFlutuante.GetComponentInChildren<AncoraEspacialPulso>(true) : null;
            if (camera == null || selecao == null || principal == null || principalAlca == null || janelaFlutuanteAncora == null)
            {
                Debug.LogError("A prévia da UI espacial requer Camera.main, o gerenciador de seleção do SNC, a alça do painel principal e a âncora do pulso.");
                return;
            }

            if (principalAlca.EstadoPostura == ControlePainelMundo.EstadoPosturaPainel.Agarrado || janelaFlutuanteAncora.EstaAgarrado)
            {
                Debug.LogWarning("Solte a alça de UI selecionada antes da prévia; a prévia nunca substitui uma captura ativa.");
                return;
            }

            if (PossuiInteracaoAnatomicaAtiva(selecao))
            {
                Debug.LogWarning("A prévia da UI espacial requer um estado inicial estável no Modo de Jogo: primeiro solte capturas de anatomia, gestos, isolamento e vistas explodidas.");
                return;
            }

            var candidato = EncontrarCandidatoPrevisualizado(selecao);
            if (candidato == null)
            {
                Debug.LogError("A prévia da UI espacial não encontrou um nó Cerebelo real selecionável nem seu filho direto.");
                return;
            }

            var principalSeguir = principal.GetComponentInChildren<AcompanhamentoCameraPainelInterface>(true);
            instantaneo = new InstantaneoPrevia(principal.transform, principalAlca.EstadoPostura, principalSeguir, janelaFlutuanteAncora.transform, janelaFlutuanteAncora.Estado, LerAcoplarAncora(janelaFlutuanteAncora), selecao.NoEmFoco, selecao.NoSelecionado, selecao.EntrarAutomaticamenteNoSelecionado, selecao.EhIsolando);
            try
            {
                // A seleção vem apenas da autoridade de produção; nenhum texto sintético é inserido.
                selecao.DefinirEntradaAutomaticaNoSelecionado(false);
                candidato = EncontrarCandidatoPrevisualizado(selecao);
                if (candidato == null || !selecao.EhCandidatoValido(candidato))
                    throw new InvalidOperationException("O candidato da prévia não é válido sob o foco anatômico atual.");
                selecao.SelecionarNo(candidato);
                if (selecao.NoSelecionado != candidato)
                    throw new InvalidOperationException("O gerenciador de seleção de produção não aceitou o candidato real da prévia.");
                principalAlca.DefinirAcoplamento(false);
                var principalPosicao = camera.position + camera.right * 0.50f + camera.forward * 1.35f;
                principal.transform.SetPositionAndRotation(principalPosicao, OrientarCamera(principalPosicao, camera));
                janelaFlutuanteAncora.Desacoplar();
                var janelaFlutuantePosicao = camera.position + camera.forward * 0.70f - camera.right * 0.25f - camera.up * 0.05f;
                janelaFlutuanteAncora.transform.SetPositionAndRotation(janelaFlutuantePosicao, OrientarCamera(janelaFlutuantePosicao, camera));
                Debug.Log("[Prévia da UI espacial] Nó real selecionado: " + candidato.NomeExibicao + ". Use o menu de restauração explícita antes de sair deste enquadramento.");
            }
            catch (Exception excecao)
            {
                Debug.LogError("A prévia da UI espacial falhou: " + excecao.Message);
                RestaurarEspacialInterfacePrevisualizacao();
            }
        }

        [MenuItem("Ferramentas/VR SNC/Interface/Restaurar Pré-visualização da Interface Espacial (Modo de Jogo)", false, 244)]
        private static void RestaurarEspacialInterfacePrevisualizacao()
        {
            if (!EditorApplication.isPlaying)
            {
                instantaneo = null;
                Debug.LogWarning("O estado da prévia da UI espacial foi limpo fora do Modo de Jogo; nenhuma cena foi salva.");
                return;
            }

            if (instantaneo == null)
            {
                Debug.LogWarning("Não há instantâneo ativo da prévia da UI espacial para restaurar.");
                return;
            }

            try
            {
                RestaurarSelecao(instantaneo);
                var principalAlca = instantaneo.TransformacaoPrincipal.GetComponentInChildren<ControlePainelMundo>(true);
                if (principalAlca != null && principalAlca.EstadoPostura != ControlePainelMundo.EstadoPosturaPainel.Agarrado)
                {
                    var seguir = instantaneo.AcompanhamentoPrincipal;
                    principalAlca.DefinirAcoplamento(instantaneo.EstadoPrincipal == ControlePainelMundo.EstadoPosturaPainel.Acoplado);
                    if (seguir != null)
                        seguir.DefinirAcompanhamentoAtivo(instantaneo.AcompanhamentoPrincipalAtivo);
                    RestaurarTransformacao(instantaneo.TransformacaoPrincipal, instantaneo.PaiPrincipal, instantaneo.PosicaoLocalPrincipal, instantaneo.RotacaoLocalPrincipal, instantaneo.EscalaLocalPrincipal);
                }

                var janelaFlutuanteAncora = instantaneo.JanelaFlutuanteTransformacao.GetComponent<AncoraEspacialPulso>();
                if (janelaFlutuanteAncora != null && !janelaFlutuanteAncora.EstaAgarrado)
                {
                    if (instantaneo.JanelaFlutuanteEstado == AncoraEspacialPulso.EstadoPostura.Acoplado)
                    {
                        if (instantaneo.JanelaFlutuanteAcoplarAncora != null)
                            janelaFlutuanteAncora.AcoplarAoPulso(instantaneo.JanelaFlutuanteAcoplarAncora);
                        else
                            janelaFlutuanteAncora.AcoplarAoPulso();
                    }
                    else
                        janelaFlutuanteAncora.Desacoplar();
                    RestaurarTransformacao(instantaneo.JanelaFlutuanteTransformacao, instantaneo.JanelaFlutuantePai, instantaneo.JanelaFlutuanteLocalPosicao, instantaneo.JanelaFlutuanteLocalRotacao, instantaneo.JanelaFlutuanteLocalEscala);
                }

                Debug.Log("[Prévia da UI espacial] O instantâneo explícito do Modo de Jogo foi restaurado; a persistência da cena não foi alterada.");
            }
            finally
            {
                instantaneo = null;
            }
        }

        private static NoInteracaoAnatomica EncontrarCandidatoPrevisualizado(GerenciadorSelecaoHierarquica selecao)
        {
            var foco = selecao.NoEmFoco != null ? selecao.NoEmFoco : selecao.NoRaiz;
            if (foco == null)
                return null;
            NoInteracaoAnatomica alternativa = null;
            foreach (var filho in foco.NosFilhos)
            {
                if (!selecao.EhCandidatoValido(filho))
                    continue;
                if (alternativa == null)
                    alternativa = filho;
                if (string.Equals(filho.NomeNo, "Cerebelo", StringComparison.OrdinalIgnoreCase) || string.Equals(filho.NomeExibicao, "Cerebelo", StringComparison.OrdinalIgnoreCase))
                    return filho;
            }

            return alternativa;
        }

        private static Quaternion OrientarCamera(Vector3 painelPosicao, Transform camera)
        {
            var paraFora = painelPosicao - camera.position;
            if (paraFora.sqrMagnitude < 0.0001f)
                paraFora = camera.forward;
            return Quaternion.LookRotation(paraFora.normalized, camera.up);
        }

        private static bool PossuiInteracaoAnatomicaAtiva(GerenciadorSelecaoHierarquica selecao)
        {
            if (selecao.EhIsolando || LerBooleano(selecao, "isolarEmSelecionar"))
                return true;
            var raiz = selecao.gameObject;
            var agarrePainel = ControlePainelMundo.EhQualquerPainelAgarreAtivo;
            var maoPonte = raiz.GetComponent<PonteInteracaoMao>();
            var manipuladorSelecionado = raiz.GetComponent<ManipuladorObjetoSelecionado>();
            var manipuladorModelo = raiz.GetComponent<ManipuladorModeloAnatomico>();
            var gesto = raiz.GetComponent<CoordenadorGestoAnatomico>();
            var localExpandir = raiz.GetComponent<ControladorVisaoExplodida>();
            var expansaoGlobal = raiz.GetComponent<ControladorExpansaoGlobal>();
            return agarrePainel || (maoPonte != null && (maoPonte.EstaAgarrando || maoPonte.EstaSeparandoComDuasMaos)) || (manipuladorSelecionado != null && manipuladorSelecionado.EstaAgarrando) || (manipuladorModelo != null && manipuladorModelo.EstaAgarrado) || (gesto != null && gesto.EstaAtivo) || (localExpandir != null && localExpandir.EstaExpandido) || (expansaoGlobal != null && (expansaoGlobal.EstaExpandido || expansaoGlobal.GestoAtivo));
        }

        private static bool LerBooleano(UnityEngine.Object alvo, string propriedadeNome)
        {
            if (alvo == null)
                return false;
            var serializado = new SerializedObject(alvo);
            var propriedade = serializado.FindProperty(propriedadeNome);
            return propriedade != null && propriedade.propertyType == SerializedPropertyType.Boolean && propriedade.boolValue;
        }

        private static Transform LerAcoplarAncora(AncoraEspacialPulso ancora)
        {
            var serializado = new SerializedObject(ancora);
            var propriedade = serializado.FindProperty("acoplarAncora");
            return propriedade != null ? propriedade.objectReferenceValue as Transform : null;
        }

        private static void RestaurarTransformacao(Transform transformacao, Transform pai, Vector3 localPosicao, Quaternion localRotacao, Vector3 localEscala)
        {
            transformacao.SetParent(pai, false);
            transformacao.localPosition = localPosicao;
            transformacao.localRotation = localRotacao;
            transformacao.localScale = localEscala;
        }

        private static void RestaurarSelecao(InstantaneoPrevia previsualizacao)
        {
            var selecao = previsualizacao.Foco != null ? previsualizacao.Foco.GetComponentInParent<GerenciadorSelecaoHierarquica>() : previsualizacao.Selecionado != null ? previsualizacao.Selecionado.GetComponentInParent<GerenciadorSelecaoHierarquica>() : null;
            if (selecao == null)
            {
                var raiz = GameObject.Find("Sistema_Nervoso_Central");
                selecao = raiz != null ? raiz.GetComponent<GerenciadorSelecaoHierarquica>() : null;
            }

            if (selecao == null)
                return;
            selecao.DefinirEntradaAutomaticaNoSelecionado(false);
            selecao.LimparSelecao();
            selecao.RedefinirFoco();
            if (previsualizacao.Foco != null && previsualizacao.Foco != selecao.NoRaiz)
                selecao.EntrarNo(previsualizacao.Foco);
            if (previsualizacao.Selecionado != null && selecao.EhCandidatoValido(previsualizacao.Selecionado))
                selecao.SelecionarNo(previsualizacao.Selecionado);
            selecao.DefinirEntradaAutomaticaNoSelecionado(previsualizacao.EntrarAutomaticamente);
        }

        private sealed class InstantaneoPrevia
        {
            public readonly Transform TransformacaoPrincipal;
            public readonly ControlePainelMundo.EstadoPosturaPainel EstadoPrincipal;
            public readonly AcompanhamentoCameraPainelInterface AcompanhamentoPrincipal;
            public readonly bool AcompanhamentoPrincipalAtivo;
            public readonly Transform PaiPrincipal;
            public readonly Vector3 PosicaoLocalPrincipal;
            public readonly Quaternion RotacaoLocalPrincipal;
            public readonly Vector3 EscalaLocalPrincipal;
            public readonly Transform JanelaFlutuanteTransformacao;
            public readonly AncoraEspacialPulso.EstadoPostura JanelaFlutuanteEstado;
            public readonly Transform JanelaFlutuanteAcoplarAncora;
            public readonly Transform JanelaFlutuantePai;
            public readonly Vector3 JanelaFlutuanteLocalPosicao;
            public readonly Quaternion JanelaFlutuanteLocalRotacao;
            public readonly Vector3 JanelaFlutuanteLocalEscala;
            public readonly NoInteracaoAnatomica Foco;
            public readonly NoInteracaoAnatomica Selecionado;
            public readonly bool EntrarAutomaticamente;
            public readonly bool EstaIsolando;
            public InstantaneoPrevia(Transform principal, ControlePainelMundo.EstadoPosturaPainel principalEstado, AcompanhamentoCameraPainelInterface principalSeguir, Transform janelaFlutuante, AncoraEspacialPulso.EstadoPostura janelaFlutuanteEstado, Transform janelaFlutuanteAcoplarAncora, NoInteracaoAnatomica foco, NoInteracaoAnatomica selecionado, bool entrarAutomaticamente, bool isolando)
            {
                TransformacaoPrincipal = principal;
                EstadoPrincipal = principalEstado;
                AcompanhamentoPrincipal = principalSeguir;
                AcompanhamentoPrincipalAtivo = principalSeguir != null ? LerBooleano(principalSeguir, "acompanharCamera") : principalEstado == ControlePainelMundo.EstadoPosturaPainel.Acoplado;
                PaiPrincipal = principal.parent;
                PosicaoLocalPrincipal = principal.localPosition;
                RotacaoLocalPrincipal = principal.localRotation;
                EscalaLocalPrincipal = principal.localScale;
                JanelaFlutuanteTransformacao = janelaFlutuante;
                JanelaFlutuanteEstado = janelaFlutuanteEstado;
                JanelaFlutuanteAcoplarAncora = janelaFlutuanteAcoplarAncora;
                JanelaFlutuantePai = janelaFlutuante.parent;
                JanelaFlutuanteLocalPosicao = janelaFlutuante.localPosition;
                JanelaFlutuanteLocalRotacao = janelaFlutuante.localRotation;
                JanelaFlutuanteLocalEscala = janelaFlutuante.localScale;
                Foco = foco;
                Selecionado = selecionado;
                EntrarAutomaticamente = entrarAutomaticamente;
                EstaIsolando = isolando;
            }
        }
    }
}
